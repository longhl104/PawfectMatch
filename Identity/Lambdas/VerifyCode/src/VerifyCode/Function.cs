using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using System.Text.Json;
using System.Net;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace VerifyCode;

public class Function
{
    private readonly IAmazonDynamoDB _dynamoDbClient;
    private readonly IAmazonCognitoIdentityProvider _cognitoClient;
    private readonly string _userPoolId;
    private readonly string _adoptersTableName;

    public Function()
    {
        _dynamoDbClient = new AmazonDynamoDBClient();
        _cognitoClient = new AmazonCognitoIdentityProviderClient();
        _userPoolId = Environment.GetEnvironmentVariable("USER_POOL_ID") ?? throw new InvalidOperationException("USER_POOL_ID environment variable is required");
        _adoptersTableName = Environment.GetEnvironmentVariable("ADOPTERS_TABLE_NAME") ?? throw new InvalidOperationException("ADOPTERS_TABLE_NAME environment variable is required");
    }

    // Constructor for testing with dependency injection
    public Function(IAmazonDynamoDB dynamoDbClient, IAmazonCognitoIdentityProvider cognitoClient, string userPoolId, string adoptersTableName)
    {
        _dynamoDbClient = dynamoDbClient;
        _cognitoClient = cognitoClient;
        _userPoolId = userPoolId;
        _adoptersTableName = adoptersTableName;
    }

    /// <summary>
    /// Lambda function handler to verify email confirmation codes
    /// </summary>
    /// <param name="request">The API Gateway proxy request</param>
    /// <param name="context">The ILambdaContext that provides methods for logging and describing the Lambda environment.</param>
    /// <returns>API Gateway proxy response</returns>
    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        try
        {
            context.Logger.LogInformation($"Processing verify code request: {request.Body}");

            // Parse request body
            var verifyRequest = JsonSerializer.Deserialize<VerifyCodeRequest>(request.Body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (verifyRequest == null || string.IsNullOrEmpty(verifyRequest.Email) || string.IsNullOrEmpty(verifyRequest.Code))
            {
                return CreateErrorResponse(HttpStatusCode.BadRequest, "Email and verification code are required");
            }

            // Verify the code with Cognito
            try
            {
                var confirmSignUpRequest = new ConfirmSignUpRequest
                {
                    ClientId = Environment.GetEnvironmentVariable("USER_POOL_CLIENT_ID"),
                    Username = verifyRequest.Email,
                    ConfirmationCode = verifyRequest.Code
                };

                await _cognitoClient.ConfirmSignUpAsync(confirmSignUpRequest);
                context.Logger.LogInformation($"Successfully verified code for user: {verifyRequest.Email}");
            }
            catch (CodeMismatchException)
            {
                return CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid verification code");
            }
            catch (ExpiredCodeException)
            {
                return CreateErrorResponse(HttpStatusCode.BadRequest, "Verification code has expired");
            }
            catch (UserNotFoundException)
            {
                return CreateErrorResponse(HttpStatusCode.NotFound, "User not found");
            }
            catch (Exception ex)
            {
                context.Logger.LogError($"Error verifying code with Cognito: {ex.Message}");
                return CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to verify code");
            }

            // Get the user's Cognito sub (user ID)
            string userId;
            try
            {
                var getUserRequest = new AdminGetUserRequest
                {
                    UserPoolId = _userPoolId,
                    Username = verifyRequest.Email
                };

                var getUserResponse = await _cognitoClient.AdminGetUserAsync(getUserRequest);
                userId = getUserResponse.UserAttributes.FirstOrDefault(attr => attr.Name == "sub")?.Value
                    ?? throw new InvalidOperationException("User sub not found");
            }
            catch (Exception ex)
            {
                context.Logger.LogError($"Error getting user from Cognito: {ex.Message}");
                return CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to get user information");
            }

            // Update DynamoDB record to set IsVerified = true
            try
            {
                var updateRequest = new UpdateItemRequest
                {
                    TableName = _adoptersTableName,
                    Key = new Dictionary<string, AttributeValue>
                    {
                        ["UserId"] = new AttributeValue { S = userId }
                    },
                    UpdateExpression = "SET IsVerified = :verified, VerifiedAt = :verifiedAt",
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        [":verified"] = new AttributeValue { BOOL = true },
                        [":verifiedAt"] = new AttributeValue { S = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") }
                    },
                    ConditionExpression = "attribute_exists(UserId)", // Ensure the record exists
                    ReturnValues = ReturnValue.UPDATED_NEW
                };

                var updateResponse = await _dynamoDbClient.UpdateItemAsync(updateRequest);
                context.Logger.LogInformation($"Successfully updated verification status for user: {userId}");
            }
            catch (ConditionalCheckFailedException)
            {
                context.Logger.LogError($"User record not found in DynamoDB for userId: {userId}");
                return CreateErrorResponse(HttpStatusCode.NotFound, "User record not found");
            }
            catch (Exception ex)
            {
                context.Logger.LogError($"Error updating DynamoDB: {ex.Message}");
                return CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to update verification status");
            }

            // Return success response
            var response = new VerifyCodeResponse
            {
                Success = true,
                Message = "Email verification successful",
                UserId = userId
            };

            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Headers = new Dictionary<string, string>
                {
                    ["Content-Type"] = "application/json",
                    ["Access-Control-Allow-Origin"] = "*",
                    ["Access-Control-Allow-Methods"] = "POST, OPTIONS",
                    ["Access-Control-Allow-Headers"] = "Content-Type, Authorization"
                },
                Body = JsonSerializer.Serialize(response, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                })
            };
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Unexpected error in FunctionHandler: {ex.Message}\n{ex.StackTrace}");
            return CreateErrorResponse(HttpStatusCode.InternalServerError, "An unexpected error occurred");
        }
    }

    private static APIGatewayProxyResponse CreateErrorResponse(HttpStatusCode statusCode, string message)
    {
        var errorResponse = new ErrorResponse
        {
            Error = message,
            StatusCode = (int)statusCode
        };

        return new APIGatewayProxyResponse
        {
            StatusCode = (int)statusCode,
            Headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json",
                ["Access-Control-Allow-Origin"] = "*",
                ["Access-Control-Allow-Methods"] = "POST, OPTIONS",
                ["Access-Control-Allow-Headers"] = "Content-Type, Authorization"
            },
            Body = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            })
        };
    }
}

public class VerifyCodeRequest
{
    public string Email { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

public class VerifyCodeResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
}

public class ErrorResponse
{
    public string Error { get; set; } = string.Empty;
    public int StatusCode { get; set; }
}
