using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using System.Text.Json;
using RefreshToken.Models;
using RefreshToken.Services;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace RefreshToken;

public class Function
{
    private readonly ICognitoService _cognitoService;
    private readonly IJwtService _jwtService;
    private readonly IRefreshTokenService _refreshTokenService;

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
    /// Lambda function handler for refresh token
    /// </summary>
    /// <param name="request">API Gateway proxy request</param>
    /// <param name="context">Lambda context</param>
    /// <returns>API Gateway proxy response</returns>
    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        context.Logger.LogInformation($"RefreshToken request: {request.HttpMethod} {request.Path}");

        try
        {
            // Validate HTTP method
            if (request.HttpMethod.ToUpper() != "POST")
            {
                return CreateErrorResponse(405, "Method not allowed");
            }

            // Parse request body
            if (string.IsNullOrEmpty(request.Body))
            {
                return CreateErrorResponse(400, "Request body is required");
            }

            var refreshTokenRequest = JsonSerializer.Deserialize<RefreshTokenRequest>(request.Body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (refreshTokenRequest == null)
            {
                return CreateErrorResponse(400, "Invalid request format");
            }

            // Validate input
            if (string.IsNullOrWhiteSpace(refreshTokenRequest.RefreshToken))
            {
                return CreateErrorResponse(400, "Refresh token is required");
            }

            // Get user ID from refresh token
            var userId = await _refreshTokenService.GetUserIdFromRefreshTokenAsync(refreshTokenRequest.RefreshToken);
            if (string.IsNullOrEmpty(userId))
            {
                context.Logger.LogWarning("Invalid or expired refresh token");
                return CreateErrorResponse(401, "Invalid or expired refresh token");
            }

            // Validate refresh token
            var isValidToken = await _refreshTokenService.ValidateRefreshTokenAsync(userId, refreshTokenRequest.RefreshToken);
            if (!isValidToken)
            {
                context.Logger.LogWarning($"Invalid refresh token for user {userId}");
                return CreateErrorResponse(401, "Invalid or expired refresh token");
            }

            // Get user profile (using userId as email for Cognito lookup)
            var userProfile = await _cognitoService.GetUserProfileAsync(userId);
            if (userProfile == null)
            {
                context.Logger.LogWarning($"User profile not found for user {userId}");
                return CreateErrorResponse(404, "User not found");
            }

            // Generate new tokens
            var newAccessToken = _jwtService.GenerateAccessToken(userProfile);
            var newRefreshToken = _jwtService.GenerateRefreshToken();
            var expiresAt = DateTime.UtcNow.AddMinutes(60); // 1 hour for access token

            // Revoke old refresh token
            await _refreshTokenService.RevokeRefreshTokenAsync(userId, refreshTokenRequest.RefreshToken);

            // Store new refresh token
            var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(30); // 30 days for refresh token
            await _refreshTokenService.StoreRefreshTokenAsync(userId, newRefreshToken, refreshTokenExpiresAt);

            // Create response
            var tokenData = new TokenData
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = expiresAt,
                User = userProfile
            };

            var refreshTokenResponse = new RefreshTokenResponse
            {
                Success = true,
                Message = "Token refreshed successfully",
                Data = tokenData
            };

            context.Logger.LogInformation($"Token refreshed successfully for user {userId}");

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
                Body = JsonSerializer.Serialize(refreshTokenResponse, new JsonSerializerOptions
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
