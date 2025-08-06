using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Longhl104.PawfectMatch.Models.Identity;
using Longhl104.PawfectMatch.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Longhl104.PawfectMatch.Middleware;

public class AuthenticationMiddleware(
    RequestDelegate _next,
    ILogger<AuthenticationMiddleware> _logger
    )
{
    /// <summary>
    /// Gets the Identity URL based on environment (local vs deployed)
    /// </summary>
    private static string GetIdentityUrl()
    {
        return EnvironmentUrlHelper.BuildServiceUrl("id", "https://localhost:4200");
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip authentication check for certain paths
        if (ShouldSkipAuthCheck(context.Request.Path))
        {
            await _next(context);
            return;
        }

        // Check for [AllowAnonymous] attribute on the endpoint
        var endpoint = context.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<IAllowAnonymous>() != null)
        {
            await _next(context);
            return;
        }

        // Check for internal authentication first
        if (context.Request.Headers.ContainsKey("X-Internal-API-Key"))
        {
            // Let the internal authentication handler handle this
            await _next(context);
            return;
        }

        var authResult = CheckAuthentication(context);

        if (!authResult.IsAuthenticated)
        {
            _logger.LogInformation("User not authenticated for path: {Path}", context.Request.Path);

            // For API requests, return JSON response
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                await WriteJsonResponse(context, authResult, 401);
                return;
            }

            // For web requests, redirect to login
            var loginUrl = $"{GetIdentityUrl()}/auth/login";

            _logger.LogInformation("Redirecting to login: {LoginUrl}", loginUrl);
            context.Response.Redirect(loginUrl);
            return;
        }

        // Check if user has the required role for API endpoints
        if (context.Request.Path.StartsWithSegments("/api") &&
            !ShouldSkipAuthCheck(context.Request.Path) &&
            authResult.User != null &&
            !string.Equals(authResult.User.UserType, "Adopter", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(authResult.User.UserType, "Shelter_Admin", StringComparison.OrdinalIgnoreCase)
            )
        {
            _logger.LogWarning("User {Email} with role {UserType} attempted to access API endpoint {Path}",
                authResult.User.Email, authResult.User.UserType, context.Request.Path);

            var forbiddenResult = new AuthCheckResult
            {
                IsAuthenticated = true,
                Message = "Access denied. You do not have permission to access this resource.",
                User = authResult.User
            };

            await WriteJsonResponse(context, forbiddenResult, 403);
            return;
        }

        // Add user information to context for downstream use
        if (authResult.User != null)
        {
            context.Items["User"] = authResult.User;
            context.Items["UserId"] = authResult.User.UserId;
            context.Items["UserEmail"] = authResult.User.Email;

            // Create claims identity for authorization
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, authResult.User.UserId.ToString()),
                new(ClaimTypes.Email, authResult.User.Email),
                new("UserType", authResult.User.UserType)
            };

            if (!string.IsNullOrEmpty(authResult.User.FullName))
            {
                claims.Add(new Claim(ClaimTypes.Name, authResult.User.FullName));
            }

            var identity = new ClaimsIdentity(claims, "PawfectMatch");
            context.User = new ClaimsPrincipal(identity);
        }

        await _next(context);
    }

    private static bool ShouldSkipAuthCheck(PathString path)
    {
        var skipPaths = new[]
        {
            "/health",
            "/swagger",
            "/openapi",
            "/.well-known"
        };

        return skipPaths.Any(skipPath => path.StartsWithSegments(skipPath, StringComparison.OrdinalIgnoreCase));
    }

    private AuthCheckResult CheckAuthentication(HttpContext context)
    {
        try
        {
            var accessToken = context.Request.Cookies["accessToken"];
            var userInfoCookie = context.Request.Cookies["userInfo"];

            if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(userInfoCookie))
            {
                return new AuthCheckResult
                {
                    IsAuthenticated = false,
                    Message = "No authentication cookies found"
                };
            }

            // Validate JWT token format
            if (!IsValidJwtFormat(accessToken))
            {
                return new AuthCheckResult
                {
                    IsAuthenticated = false,
                    Message = "Invalid token format"
                };
            }

            // Parse JWT token to check expiration
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwt = tokenHandler.ReadJwtToken(accessToken);

            // Check if token is expired
            if (jwt.ValidTo < DateTime.UtcNow)
            {
                return new AuthCheckResult
                {
                    IsAuthenticated = false,
                    Message = "Access token expired",
                    RequiresRefresh = true
                };
            }

            // Decode user information from cookie
            UserProfile? userProfile = null;
            try
            {
                var userInfoJson = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(userInfoCookie));
                userProfile = JsonSerializer.Deserialize<UserProfile>(userInfoJson, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to decode user info cookie");
            }

            return new AuthCheckResult
            {
                IsAuthenticated = true,
                Message = "User is authenticated",
                User = userProfile,
                TokenExpiresAt = jwt.ValidTo
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking authentication");
            return new AuthCheckResult
            {
                IsAuthenticated = false,
                Message = "Authentication check failed"
            };
        }
    }

    private static bool IsValidJwtFormat(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            return tokenHandler.CanReadToken(token);
        }
        catch
        {
            return false;
        }
    }

    private static async Task WriteJsonResponse(HttpContext context, AuthCheckResult result, int statusCode = 401)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = new
        {
            success = false,
            message = result.Message,
            isAuthenticated = result.IsAuthenticated,
            redirectUrl = statusCode == 401 ? $"{GetIdentityUrl()}/auth/login" : null,
            requiresRefresh = result.RequiresRefresh
        };

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}

public class AuthCheckResult
{
    public bool IsAuthenticated { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool RequiresRefresh { get; set; } = false;
    public UserProfile? User { get; set; }
    public DateTime? TokenExpiresAt { get; set; }
}
