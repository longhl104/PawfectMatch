using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using Longhl104.PawfectMatch.Models.Identity;

namespace Longhl104.Matcher.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthCheckController(ILogger<AuthCheckController> logger) : ControllerBase
{
    /// <summary>
    /// Check if the current user is authenticated based on cookies
    /// </summary>
    [HttpGet("status")]
    public IActionResult CheckAuthStatus()
    {
        logger.LogInformation("Checking authentication status for user");

        try
        {
            // Check for access token in cookies
            var accessToken = Request.Cookies["accessToken"];
            var userInfoCookie = Request.Cookies["userInfo"];

            if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(userInfoCookie))
            {
                logger.LogInformation("No authentication cookies found");
                return Ok(new AuthStatusResponse
                {
                    IsAuthenticated = false,
                    Message = "No authentication cookies found",
                    RedirectUrl = GetIdentityLoginUrl()
                });
            }

            // Validate JWT token format (basic validation)
            if (!IsValidJwtFormat(accessToken))
            {
                logger.LogWarning("Invalid JWT token format");
                return Ok(new AuthStatusResponse
                {
                    IsAuthenticated = false,
                    Message = "Invalid token format",
                    RedirectUrl = GetIdentityLoginUrl()
                });
            }

            // Parse JWT token to check expiration
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwt = tokenHandler.ReadJwtToken(accessToken);

            // Check if token is expired
            if (jwt.ValidTo < DateTime.UtcNow)
            {
                logger.LogInformation("Access token has expired");
                return Ok(new AuthStatusResponse
                {
                    IsAuthenticated = false,
                    Message = "Access token expired",
                    RedirectUrl = GetIdentityLoginUrl(),
                    RequiresRefresh = true
                });
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
                logger.LogWarning(ex, "Failed to decode user info cookie");
            }

            // User is authenticated
            logger.LogInformation("User is authenticated: {Email}", userProfile?.Email ?? "Unknown");
            return Ok(new AuthStatusResponse
            {
                IsAuthenticated = true,
                Message = "User is authenticated",
                User = userProfile,
                TokenExpiresAt = jwt.ValidTo
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking authentication status");
            return Ok(new AuthStatusResponse
            {
                IsAuthenticated = false,
                Message = "Authentication check failed",
                RedirectUrl = GetIdentityLoginUrl()
            });
        }
    }

    /// <summary>
    /// Redirect to Identity login page
    /// </summary>
    [HttpGet("login")]
    public IActionResult RedirectToLogin()
    {
        var loginUrl = GetIdentityLoginUrl();
        logger.LogInformation("Redirecting to login page: {LoginUrl}", loginUrl);
        return Redirect(loginUrl);
    }

    /// <summary>
    /// Clear authentication cookies and redirect to login
    /// </summary>
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        logger.LogInformation("Logging out user");

        // Clear authentication cookies
        var cookieOptions = new CookieOptions
        {
            Path = "/",
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = Request.IsHttps ? SameSiteMode.None : SameSiteMode.Lax,
            Expires = DateTime.UtcNow.AddDays(-1) // Set expiration to past date
        };

        Response.Cookies.Append("accessToken", "", cookieOptions);
        Response.Cookies.Append("refreshToken", "", cookieOptions);
        Response.Cookies.Append("userInfo", "", new CookieOptions
        {
            Path = "/",
            HttpOnly = false,
            Secure = Request.IsHttps,
            SameSite = Request.IsHttps ? SameSiteMode.None : SameSiteMode.Lax,
            Expires = DateTime.UtcNow.AddDays(-1)
        });

        return Ok(new AuthStatusResponse
        {
            IsAuthenticated = false,
            Message = "Logged out successfully",
            RedirectUrl = GetIdentityLoginUrl()
        });
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

    private string GetIdentityLoginUrl()
    {
        // Get the Identity application URL from configuration
        var identityUrl = HttpContext.RequestServices
            .GetRequiredService<IConfiguration>()
            .GetValue<string>("IdentityUrl") ?? "https://localhost:4200";

        return $"{identityUrl}/auth/login";
    }
}

/// <summary>
/// Response model for authentication status check
/// </summary>
public class AuthStatusResponse
{
    public bool IsAuthenticated { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? RedirectUrl { get; set; }
    public bool RequiresRefresh { get; set; } = false;
    public UserProfile? User { get; set; }
    public DateTime? TokenExpiresAt { get; set; }
}
