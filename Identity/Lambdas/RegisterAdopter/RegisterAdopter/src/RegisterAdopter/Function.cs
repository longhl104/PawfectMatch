using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using System.Text.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace RegisterAdopter;

public class AdopterRegistrationRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string Address { get; set; } = string.Empty;
    public string? Bio { get; set; }
}

public class AdopterRegistrationResponse
{
    public string Message { get; set; } = string.Empty;
    public string AdopterId { get; set; } = string.Empty;
}

public class Function
{
    private readonly AmazonDynamoDBClient _dynamoDbClient;
    private readonly AmazonCognitoIdentityProviderClient _cognitoClient;
    private readonly string _tableName;
    private readonly string _userPoolId;

    public Function()
    {
        _dynamoDbClient = new AmazonDynamoDBClient();
        _cognitoClient = new AmazonCognitoIdentityProviderClient();
        _tableName = Environment.GetEnvironmentVariable("ADOPTERS_TABLE_NAME") ?? "PawfectMatch-Adopters";
        _userPoolId = Environment.GetEnvironmentVariable("USER_POOL_ID") ?? throw new InvalidOperationException("USER_POOL_ID environment variable is required");
    }

    /// <summary>
    /// Lambda function handler for registering adopters
    /// </summary>
    /// <param name="request">The API Gateway request</param>
    /// <param name="context">The Lambda context</param>
    /// <returns>API Gateway response</returns>
    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        try
        {
            context.Logger.LogInformation($"Processing adopter registration request: {request.Body}");

            // Validate request
            if (string.IsNullOrEmpty(request.Body))
            {
                return CreateErrorResponse(400, "Request body is required");
            }

            // Parse request body
            var registrationRequest = JsonSerializer.Deserialize<AdopterRegistrationRequest>(request.Body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (registrationRequest == null)
            {
                return CreateErrorResponse(400, "Invalid request body");
            }

            // Validate required fields
            var validationError = ValidateRegistrationRequest(registrationRequest);
            if (!string.IsNullOrEmpty(validationError))
            {
                return CreateErrorResponse(400, validationError);
            }

            // Check if email already exists
            var existingUser = await CheckIfEmailExists(registrationRequest.Email);
            if (existingUser)
            {
                return CreateErrorResponse(409, "An account with this email already exists");
            }

            // Generate unique adopter ID
            var adopterId = Guid.NewGuid().ToString();

            // Create user in Cognito
            await CreateCognitoUser(registrationRequest, adopterId);

            // Save adopter profile to DynamoDB
            await SaveAdopterProfile(registrationRequest, adopterId);

            // Return success response
            var response = new AdopterRegistrationResponse
            {
                Message = "Registration successful! Please check your email to verify your account.",
                AdopterId = adopterId
            };

            return new APIGatewayProxyResponse
            {
                StatusCode = 201,
                Headers = new Dictionary<string, string>
                {
                    ["Content-Type"] = "application/json",
                    ["Access-Control-Allow-Origin"] = "*",
                    ["Access-Control-Allow-Methods"] = "POST, OPTIONS",
                    ["Access-Control-Allow-Headers"] = "Content-Type, Authorization"
                },
                Body = JsonSerializer.Serialize(response)
            };
        }
        catch (UsernameExistsException)
        {
            return CreateErrorResponse(409, "An account with this email already exists");
        }
        catch (InvalidParameterException ex)
        {
            context.Logger.LogError($"Invalid parameter: {ex.Message}");
            return CreateErrorResponse(400, "Invalid registration data provided");
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error processing registration: {ex.Message}");
            return CreateErrorResponse(500, "An error occurred while processing your registration");
        }
    }

    private string ValidateRegistrationRequest(AdopterRegistrationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
            return "Full name is required";

        if (string.IsNullOrWhiteSpace(request.Email))
            return "Email is required";

        if (!IsValidEmail(request.Email))
            return "Please provide a valid email address";

        if (string.IsNullOrWhiteSpace(request.Password))
            return "Password is required";

        if (request.Password.Length < 8)
            return "Password must be at least 8 characters long";

        if (string.IsNullOrWhiteSpace(request.Address))
            return "Address is required";

        return string.Empty;
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> CheckIfEmailExists(string email)
    {
        try
        {
            var request = new AdminGetUserRequest
            {
                UserPoolId = _userPoolId,
                Username = email
            };

            await _cognitoClient.AdminGetUserAsync(request);
            return true; // User exists
        }
        catch (UserNotFoundException)
        {
            return false; // User doesn't exist
        }
    }

    private async Task CreateCognitoUser(AdopterRegistrationRequest request, string adopterId)
    {
        var userAttributes = new List<AttributeType>
        {
            new() { Name = "email", Value = request.Email },
            new() { Name = "email_verified", Value = "false" },
            new() { Name = "name", Value = request.FullName },
            new() { Name = "custom:user_type", Value = "adopter" },
            new() { Name = "custom:adopter_id", Value = adopterId }
        };

        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            userAttributes.Add(new AttributeType { Name = "phone_number", Value = request.PhoneNumber });
        }

        var createUserRequest = new AdminCreateUserRequest
        {
            UserPoolId = _userPoolId,
            Username = request.Email,
            UserAttributes = userAttributes,
            TemporaryPassword = request.Password,
            MessageAction = MessageActionType.SUPPRESS, // We'll handle email verification separately
            ForceAliasCreation = false
        };

        var createUserResponse = await _cognitoClient.AdminCreateUserAsync(createUserRequest);

        // Set permanent password
        var setPasswordRequest = new AdminSetUserPasswordRequest
        {
            UserPoolId = _userPoolId,
            Username = request.Email,
            Password = request.Password,
            Permanent = true
        };

        await _cognitoClient.AdminSetUserPasswordAsync(setPasswordRequest);
    }

    private async Task SaveAdopterProfile(AdopterRegistrationRequest request, string adopterId)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            ["AdopterId"] = new AttributeValue { S = adopterId },
            ["FullName"] = new AttributeValue { S = request.FullName },
            ["Email"] = new AttributeValue { S = request.Email },
            ["Address"] = new AttributeValue { S = request.Address },
            ["IsVerified"] = new AttributeValue { BOOL = false },
            ["DateJoined"] = new AttributeValue { S = DateTime.UtcNow.ToString("O") },
            ["LastActive"] = new AttributeValue { S = DateTime.UtcNow.ToString("O") },
            ["AdoptionHistory"] = new AttributeValue { L = new List<AttributeValue>() },
            ["Preferences"] = new AttributeValue
            {
                M = new Dictionary<string, AttributeValue>
                {
                    ["PetTypes"] = new AttributeValue { L = new List<AttributeValue>() },
                    ["PetSizes"] = new AttributeValue { L = new List<AttributeValue>() },
                    ["AgeRanges"] = new AttributeValue { L = new List<AttributeValue>() },
                    ["ActivityLevel"] = new AttributeValue { S = "" },
                    ["HasChildren"] = new AttributeValue { BOOL = false },
                    ["HasOtherPets"] = new AttributeValue { BOOL = false },
                    ["LivingArrangement"] = new AttributeValue { S = "" },
                    ["MaxTravelDistance"] = new AttributeValue { N = "50" },
                    ["Notifications"] = new AttributeValue
                    {
                        M = new Dictionary<string, AttributeValue>
                        {
                            ["NewMatches"] = new AttributeValue { BOOL = true },
                            ["AdoptionUpdates"] = new AttributeValue { BOOL = true },
                            ["Newsletter"] = new AttributeValue { BOOL = false },
                            ["Events"] = new AttributeValue { BOOL = false }
                        }
                    }
                }
            }
        };

        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            item["PhoneNumber"] = new AttributeValue { S = request.PhoneNumber };
        }

        if (!string.IsNullOrWhiteSpace(request.Bio))
        {
            item["Bio"] = new AttributeValue { S = request.Bio };
        }

        var putItemRequest = new PutItemRequest
        {
            TableName = _tableName,
            Item = item,
            ConditionExpression = "attribute_not_exists(AdopterId)" // Prevent duplicates
        };

        await _dynamoDbClient.PutItemAsync(putItemRequest);
    }

    private APIGatewayProxyResponse CreateErrorResponse(int statusCode, string message)
    {
        var errorResponse = new { error = message };

        return new APIGatewayProxyResponse
        {
            StatusCode = statusCode,
            Headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json",
                ["Access-Control-Allow-Origin"] = "*",
                ["Access-Control-Allow-Methods"] = "POST, OPTIONS",
                ["Access-Control-Allow-Headers"] = "Content-Type, Authorization"
            },
            Body = JsonSerializer.Serialize(errorResponse)
        };
    }
}
