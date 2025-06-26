using Amazon.Lambda.Core;
using Amazon.Lambda.DynamoDBEvents;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace SendVerificationEmail;

public class Function
{
    private readonly IAmazonSimpleEmailServiceV2 _sesClient;
    private readonly IAmazonCognitoIdentityProvider _cognitoClient;
    private readonly string _userPoolId;
    private readonly string _fromEmailAddress;
    private readonly string _frontendBaseUrl;

    public Function()
    {
        _sesClient = new AmazonSimpleEmailServiceV2Client();
        _cognitoClient = new AmazonCognitoIdentityProviderClient();
        _userPoolId = Environment.GetEnvironmentVariable("USER_POOL_ID") ?? string.Empty;
        _fromEmailAddress = Environment.GetEnvironmentVariable("FROM_EMAIL_ADDRESS") ?? "noreply@example.com";
        _frontendBaseUrl = Environment.GetEnvironmentVariable("FRONTEND_BASE_URL") ?? "https://www.pawfectmatch.com";
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

    private bool IsEmailVerified(User user)
    {
        var emailVerifiedAttribute = user.UserAttributes?.FirstOrDefault(attr => attr.Name == "email_verified");
        return emailVerifiedAttribute?.Value?.ToLower() == "true";
    }

    private async Task SendVerificationEmail(string toEmail, string? fullName, string userId, ILambdaContext context)
    {
        // Validate email addresses
        if (string.IsNullOrEmpty(_fromEmailAddress))
        {
            throw new InvalidOperationException("FROM_EMAIL_ADDRESS environment variable is not set");
        }

        if (string.IsNullOrEmpty(toEmail))
        {
            throw new ArgumentException("Recipient email address cannot be null or empty", nameof(toEmail));
        }

        context.Logger.LogInformation($"Sending verification email from '{_fromEmailAddress}' to '{toEmail}'");

        var displayName = !string.IsNullOrEmpty(fullName) ? fullName : "New Adopter";
        var verificationUrl = $"{_frontendBaseUrl}/verify-email?userId={userId}";

        var subject = "Welcome to PawfectMatch - Please Verify Your Email";
        var htmlBody = CreateHtmlEmailBody(displayName, verificationUrl);
        var textBody = CreateTextEmailBody(displayName, verificationUrl);

        var sendRequest = new SendEmailRequest
        {
            FromEmailAddress = _fromEmailAddress.Trim(), // Trim any whitespace
            Destination = new Destination
            {
                ToAddresses = [toEmail.Trim()] // Trim any whitespace
            },
            Content = new EmailContent
            {
                Simple = new Message
                {
                    Subject = new Content { Data = subject },
                    Body = new Body
                    {
                        Html = new Content { Data = htmlBody },
                        Text = new Content { Data = textBody }
                    }
                }
            }
        };

        await _sesClient.SendEmailAsync(sendRequest);
    }

    private string CreateHtmlEmailBody(string displayName, string verificationUrl)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <title>Welcome to PawfectMatch</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #4f46e5; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; }}
        .content {{ background-color: #f8fafc; padding: 30px; border-radius: 0 0 8px 8px; }}
        .button {{ display: inline-block; background-color: #10b981; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: bold; margin: 20px 0; }}
        .footer {{ text-align: center; margin-top: 30px; color: #6b7280; font-size: 14px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>🐾 Welcome to PawfectMatch!</h1>
        </div>
        <div class=""content"">
            <h2>Hello {displayName},</h2>
            <p>Thank you for joining PawfectMatch! We're excited to help you find your perfect furry companion.</p>
            <p>To get started, please verify your email address by clicking the button below:</p>
            <div style=""text-align: center;"">
                <a href=""{verificationUrl}"" class=""button"">Verify Email Address</a>
            </div>
            <p>If the button doesn't work, you can copy and paste this link into your browser:</p>
            <p style=""word-break: break-all; color: #6b7280;"">{verificationUrl}</p>
            <p>Once verified, you'll be able to:</p>
            <ul>
                <li>Browse available pets for adoption</li>
                <li>Create and manage your adoption preferences</li>
                <li>Connect with pet shelters and rescue organizations</li>
                <li>Track your adoption journey</li>
            </ul>
            <p>Welcome to the PawfectMatch family!</p>
        </div>
        <div class=""footer"">
            <p>If you didn't create an account with PawfectMatch, you can safely ignore this email.</p>
            <p>&copy; 2025 PawfectMatch. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
    }

    private string CreateTextEmailBody(string displayName, string verificationUrl)
    {
        return $@"
Welcome to PawfectMatch!

Hello {displayName},

Thank you for joining PawfectMatch! We're excited to help you find your perfect furry companion.

To get started, please verify your email address by visiting this link:
{verificationUrl}

Once verified, you'll be able to:
- Browse available pets for adoption
- Create and manage your adoption preferences
- Connect with pet shelters and rescue organizations
- Track your adoption journey

Welcome to the PawfectMatch family!

If you didn't create an account with PawfectMatch, you can safely ignore this email.

© 2025 PawfectMatch. All rights reserved.
";
    }

    public class User
    {
        public string? Username { get; set; }
        public UserStatusType? UserStatus { get; set; }
        public List<AttributeType>? UserAttributes { get; set; }
    }
}
