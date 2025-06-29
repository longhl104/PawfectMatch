using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using System.Text.Json;
using Longhl104.PawfectMatch.Services;
using Longhl104.PawfectMatch.Models.Identity;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Login;

public class Function
{
    private readonly ICognitoService _cognitoService;
    private readonly IJwtService _jwtService;
    private readonly IRefreshTokenService _refreshTokenService;
    private static readonly JsonSerializerOptions jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public Function()
    {
        _cognitoService = new CognitoService();
        _jwtService = new JwtService();
        _refreshTokenService = new RefreshTokenService();
    }

    public Function(ICognitoService cognitoService, IJwtService jwtService, IRefreshTokenService refreshTokenService)
    {
        _cognitoService = cognitoService;
        _jwtService = jwtService;
        _refreshTokenService = refreshTokenService;
    }

    /// <summary>
    /// Lambda function handler for user login
    /// </summary>
    /// <param name="request">API Gateway proxy request</param>
    /// <param name="context">Lambda context</param>
    /// <returns>API Gateway proxy response</returns>
    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        context.Logger.LogInformation($"Login request: {request.HttpMethod} {request.Path}");

        try
        {
            // Validate HTTP method
            if (!request.HttpMethod.Equals("POST", StringComparison.CurrentCultureIgnoreCase))
            {
                return CreateErrorResponse(405, "Method not allowed");
            }

            // Parse request body
            if (string.IsNullOrEmpty(request.Body))
            {
                return CreateErrorResponse(400, "Request body is required");
            }

            var loginRequest = JsonSerializer.Deserialize<LoginRequest>(request.Body, jsonSerializerOptions);

            if (loginRequest == null)
            {
                return CreateErrorResponse(400, "Invalid request format");
            }

            // Validate input
            var (IsValid, ErrorMessage) = ValidateLoginRequest(loginRequest);
            if (!IsValid)
            {
                return CreateErrorResponse(400, ErrorMessage);
            }

            // Authenticate user with Cognito
            var (success, message, user) = await _cognitoService.AuthenticateUserAsync(
                loginRequest.Email,
                loginRequest.Password
                );

            if (!success || user == null)
            {
                context.Logger.LogWarning($"Authentication failed for user {loginRequest.Email}: {message}");
                return CreateErrorResponse(401, message);
            }

            // Generate tokens
            var accessToken = _jwtService.GenerateAccessToken(user);
            var refreshToken = _jwtService.GenerateRefreshToken();
            var expiresAt = DateTime.UtcNow.AddMinutes(60); // 1 hour for access token

            // Store refresh token
            var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(30); // 30 days for refresh token
            await _refreshTokenService.StoreRefreshTokenAsync(user.UserId, refreshToken, refreshTokenExpiresAt);

            // Create response
            var tokenData = new TokenData
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = expiresAt,
                User = user
            };

            var loginResponse = new LoginResponse
            {
                Success = true,
                Message = "Login successful",
                Data = tokenData
            };

            context.Logger.LogInformation($"User {loginRequest.Email} logged in successfully");

            return new APIGatewayProxyResponse
            {
                StatusCode = 200,
                Headers = new Dictionary<string, string>
                {
                    { "Content-Type", "application/json" },
                    { "Access-Control-Allow-Origin", "*" },
                    { "Access-Control-Allow-Headers", "Content-Type,X-Amz-Date,Authorization,X-Api-Key,X-Amz-Security-Token" },
                    { "Access-Control-Allow-Methods", "POST,OPTIONS" }
                },
                Body = JsonSerializer.Serialize(loginResponse, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                })
            };
        }
        catch (JsonException ex)
        {
            context.Logger.LogError($"JSON parsing error: {ex.Message}");
            return CreateErrorResponse(400, "Invalid JSON format");
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Unexpected error: {ex.Message}\n{ex.StackTrace}");
            return CreateErrorResponse(500, "Internal server error");
        }
    }

    private static (bool IsValid, string ErrorMessage) ValidateLoginRequest(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return (false, "Email is required");
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return (false, "Password is required");
        }

        if (!IsValidEmail(request.Email))
        {
            return (false, "Invalid email format");
        }

        if (request.Password.Length < 8)
        {
            return (false, "Password must be at least 8 characters long");
        }

        return (true, string.Empty);
    }

    private static bool IsValidEmail(string email)
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

    private static APIGatewayProxyResponse CreateErrorResponse(int statusCode, string message)
    {
        var errorResponse = new ApiResponse<object>
        {
            Success = false,
            Message = message,
            Data = null
        };

        return new APIGatewayProxyResponse
        {
            StatusCode = statusCode,
            Headers = new Dictionary<string, string>
            {
                { "Content-Type", "application/json" },
                { "Access-Control-Allow-Origin", "*" },
                { "Access-Control-Allow-Headers", "Content-Type,X-Amz-Date,Authorization,X-Api-Key,X-Amz-Security-Token" },
                { "Access-Control-Allow-Methods", "POST,OPTIONS" }
            },
            Body = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            })
        };
    }
}
