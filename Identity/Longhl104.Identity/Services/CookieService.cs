using System.Text.Json;
using Longhl104.Identity.Models;

namespace Longhl104.Identity.Services;

public interface ICookieService
{
    void SetJwtAuthenticationCookies(HttpContext httpContext, string accessToken, string refreshToken, UserProfile user);
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
    /// Clear all authentication cookies
    /// </summary>
    public void ClearAuthenticationCookies(HttpContext httpContext)
    {
        // Clear JWT cookies
        httpContext.Response.Cookies.Delete("accessToken");
        httpContext.Response.Cookies.Delete("refreshToken");
        httpContext.Response.Cookies.Delete("userInfo");
    }
}
