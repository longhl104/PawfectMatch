using Amazon.Lambda.Core;
using Amazon.Lambda.DynamoDBEvents;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace SendVerificationEmail;

public class Function
{
    private readonly IAmazonCognitoIdentityProvider _cognitoClient;
    private readonly string _userPoolId;
    private readonly string _userPoolClientId;

    public Function()
    {
        _cognitoClient = new AmazonCognitoIdentityProviderClient();
        _userPoolId = Environment.GetEnvironmentVariable("USER_POOL_ID") ?? string.Empty;
        _userPoolClientId = Environment.GetEnvironmentVariable("USER_POOL_CLIENT_ID") ?? string.Empty;
    }

    /// <summary>
    /// Lambda function handler to process DynamoDB stream events and send verification emails for new adopters
    /// </summary>
    /// <param name="dynamoEvent">The DynamoDB stream event</param>
    /// <param name="context">The Lambda context</param>
    /// <returns></returns>
    public async Task FunctionHandler(DynamoDBEvent dynamoEvent, ILambdaContext context)
    {
        context.Logger.LogInformation($"Processing {dynamoEvent.Records.Count} DynamoDB records");

        foreach (var record in dynamoEvent.Records)
        {
            try
            {
                // Only process INSERT events (new adopters)
                if (record.EventName == "INSERT")
                {
                    await ProcessNewAdopterRecord(record, context);
                }
            }
            catch (Exception ex)
            {
                context.Logger.LogError($"Error processing record {record.Dynamodb?.Keys?.GetValueOrDefault("UserId")?.S}: {ex.Message}");
                // Continue processing other records even if one fails
            }
        }
    }

    private async Task ProcessNewAdopterRecord(DynamoDBEvent.DynamodbStreamRecord record, ILambdaContext context)
    {
        var newImage = record.Dynamodb?.NewImage;
        if (newImage == null)
        {
            context.Logger.LogWarning("No new image found in DynamoDB record");
            return;
        }

        // Extract adopter information
        var userId = newImage.GetValueOrDefault("UserId")?.S;
        var email = newImage.GetValueOrDefault("Email")?.S;
        var fullName = newImage.GetValueOrDefault("FullName")?.S;

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(email))
        {
            context.Logger.LogWarning($"Missing required fields: UserId={userId}, Email={email}");
            return;
        }

        context.Logger.LogInformation($"Processing new adopter: {email} (UserId: {userId})");

        try
        {
            // Get user details from Cognito to check verification status
            var userDetails = await GetCognitoUserDetails(userId, context);

            // Only send verification email if the user email is not already verified
            if (userDetails != null && !IsEmailVerified(userDetails))
            {
                await SendVerificationEmail(email, fullName, userId, context);
                context.Logger.LogInformation($"Verification email sent successfully to {email}");
            }
            else
            {
                context.Logger.LogInformation($"Email already verified for user {email}, skipping verification email");
            }
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Failed to send verification email to {email}: {ex.Message}");
            throw; // Re-throw to ensure the record is retried
        }
    }

    private async Task<User?> GetCognitoUserDetails(string userId, ILambdaContext context)
    {
        try
        {
            var request = new AdminGetUserRequest
            {
                UserPoolId = _userPoolId,
                Username = userId
            };

            var response = await _cognitoClient.AdminGetUserAsync(request);
            return new User
            {
                Username = response.Username,
                UserStatus = response.UserStatus,
                UserAttributes = response.UserAttributes
            };
        }
        catch (UserNotFoundException)
        {
            context.Logger.LogWarning($"User {userId} not found in Cognito User Pool");
            return null;
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error getting user details from Cognito: {ex.Message}");
            throw;
        }
    }

    private static bool IsEmailVerified(User user)
    {
        var emailVerifiedAttribute = user.UserAttributes?.FirstOrDefault(attr => attr.Name == "email_verified");
        return emailVerifiedAttribute?.Value?.ToLower() == "true";
    }

    private async Task SendVerificationEmail(string toEmail, string? fullName, string userId, ILambdaContext context)
    {
        context.Logger.LogInformation($"Triggering Cognito email verification for user: {userId}");

        if (string.IsNullOrEmpty(_userPoolClientId))
        {
            context.Logger.LogError("USER_POOL_CLIENT_ID environment variable is not set.");
            throw new InvalidOperationException("USER_POOL_CLIENT_ID environment variable is not set.");
        }

        try
        {
            // Use Cognito's built-in email verification
            var resendRequest = new ResendConfirmationCodeRequest
            {
                ClientId = _userPoolClientId,
                Username = userId
            };

            await _cognitoClient.ResendConfirmationCodeAsync(resendRequest);
            context.Logger.LogInformation($"Cognito verification email sent to {toEmail}");
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Failed to send Cognito verification email: {ex.Message}");
            throw;
        }
    }

    public class User
    {
        public string? Username { get; set; }
        public UserStatusType? UserStatus { get; set; }
        public List<AttributeType>? UserAttributes { get; set; }
    }
}
