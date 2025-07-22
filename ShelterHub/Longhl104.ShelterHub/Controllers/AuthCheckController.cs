using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Longhl104.PawfectMatch.Models.Identity;
using Longhl104.PawfectMatch.Extensions;
using Longhl104.PawfectMatch.Utils;

namespace Longhl104.ShelterHub.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // This will use the global policy requiring Adopter role
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
            var user = HttpContext.GetCurrentUser();

            if (user == null)
            {
                logger.LogInformation("User is not authenticated");
                return Ok(new AuthStatusResponse
                {
                    IsAuthenticated = false,
                    Message = "User is not authenticated",
                    RedirectUrl = GetIdentityLoginUrl()
                });
            }

            // User is authenticated
            logger.LogInformation("User is authenticated: {Email}", user.Email);
            return Ok(new AuthStatusResponse
            {
                IsAuthenticated = true,
                Message = "User is authenticated",
                User = user,
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
    /// Test endpoint that demonstrates role-based authorization
    /// Only users with Adopter role can access this endpoint
    /// </summary>
    [HttpGet("adopter-only")]
    public IActionResult AdopterOnlyEndpoint()
    {
        logger.LogInformation("Adopter-only endpoint accessed by user: {Email}", User?.FindFirst("Email")?.Value);

        var userType = User?.FindFirst("UserType")?.Value;
        var email = User?.FindFirst("Email")?.Value;

        return Ok(new
        {
            Message = "Success! You have access to this Adopter-only endpoint.",
            UserType = userType,
            Email = email,
            Timestamp = DateTime.UtcNow
        });
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

    private string GetIdentityLoginUrl()
    {
        // Get the Identity application URL from configuration
        // var identityUrl = HttpContext.RequestServices
        //     .GetRequiredService<IConfiguration>()
        //     .GetValue<string>("IdentityUrl") ?? "https://localhost:4200";

        var identityUrl = EnvironmentUrlHelper.BuildServiceUrl("id", "https://localhost:4200");

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
