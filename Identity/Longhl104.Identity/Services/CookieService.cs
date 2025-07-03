using System.Text.Json;
using Longhl104.Identity.Models;

namespace Longhl104.Identity.Services;

public interface ICookieService
{
    void SetJwtAuthenticationCookies(HttpContext httpContext, string accessToken, string refreshToken, UserProfile user);
    void SetSimpleAuthenticationCookies(HttpContext httpContext, string userId, string email);
    void ClearAuthenticationCookies(HttpContext httpContext);
}

public class CookieService : ICookieService
{
    private static readonly JsonSerializerOptions CamelCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Set JWT-based authentication cookies for full authentication
    /// </summary>
    public void SetJwtAuthenticationCookies(HttpContext httpContext, string accessToken, string refreshToken, UserProfile user)
    {
        var cookieDomain = httpContext.Request.Host.Host;
        var isSecure = httpContext.Request.IsHttps;
        var sameSite = isSecure ? SameSiteMode.None : SameSiteMode.Lax;

        // Access token cookie
        httpContext.Response.Cookies.Append("accessToken", accessToken, new CookieOptions
        {
            Path = "/",
            Domain = cookieDomain.Equals("localhost", StringComparison.OrdinalIgnoreCase) ? null : cookieDomain,
            MaxAge = TimeSpan.FromHours(1),
            HttpOnly = true,
            Secure = isSecure,
            SameSite = sameSite
        });

        // Refresh token cookie
        httpContext.Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
        {
            Path = "/",
            Domain = cookieDomain.Equals("localhost", StringComparison.OrdinalIgnoreCase) ? null : cookieDomain,
            MaxAge = TimeSpan.FromDays(30),
            HttpOnly = true,
            Secure = isSecure,
            SameSite = sameSite
        });

        // User info cookie (non-HttpOnly for client access)
        var userInfoJson = JsonSerializer.Serialize(user, CamelCaseOptions);
        var userInfoBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(userInfoJson));
        httpContext.Response.Cookies.Append("userInfo", userInfoBase64, new CookieOptions
        {
            Path = "/",
            Domain = cookieDomain.Equals("localhost", StringComparison.OrdinalIgnoreCase) ? null : cookieDomain,
            MaxAge = TimeSpan.FromHours(1),
            HttpOnly = false,
            Secure = isSecure,
            SameSite = sameSite
        });
    }

    /// <summary>
    /// Set simple authentication cookies for basic authentication status
    /// </summary>
    public void SetSimpleAuthenticationCookies(HttpContext httpContext, string userId, string email)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = httpContext.Request.IsHttps,
            SameSite = httpContext.Request.IsHttps ? SameSiteMode.None : SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(30)
        };

        // Set user ID cookie
        httpContext.Response.Cookies.Append("PawfectMatch_UserId", userId, cookieOptions);

        // Set email cookie
        httpContext.Response.Cookies.Append("PawfectMatch_Email", email, cookieOptions);

        // Set authentication status cookie
        httpContext.Response.Cookies.Append("PawfectMatch_Authenticated", "true", cookieOptions);

        // Set user type cookie
        httpContext.Response.Cookies.Append("PawfectMatch_UserType", "adopter", cookieOptions);
    }

    /// <summary>
    /// Clear all authentication cookies
    /// </summary>
    public void ClearAuthenticationCookies(HttpContext httpContext)
    {
        // Clear JWT cookies
        httpContext.Response.Cookies.Delete("accessToken");
        httpContext.Response.Cookies.Delete("refreshToken");
        httpContext.Response.Cookies.Delete("userInfo");

        // Clear simple auth cookies
        httpContext.Response.Cookies.Delete("PawfectMatch_UserId");
        httpContext.Response.Cookies.Delete("PawfectMatch_Email");
        httpContext.Response.Cookies.Delete("PawfectMatch_Authenticated");
        httpContext.Response.Cookies.Delete("PawfectMatch_UserType");
    }
}
