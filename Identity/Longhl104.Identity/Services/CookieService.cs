using System.Text.Json;
using Longhl104.Identity.Models;
using Longhl104.PawfectMatch.Models.Identity;

namespace Longhl104.Identity.Services;

public interface ICookieService
{
    void SetJwtAuthenticationCookies(HttpContext httpContext, string accessToken, UserProfile user);
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
    public void SetJwtAuthenticationCookies(HttpContext httpContext, string accessToken, UserProfile user)
    {
        var cookieDomain = httpContext.Request.Host.Host;
        var isSecure = httpContext.Request.IsHttps;
        var sameSite = isSecure ? SameSiteMode.None : SameSiteMode.Lax;

        // Access token cookie
        httpContext.Response.Cookies.Append("accessToken", accessToken, new CookieOptions
        {
            Path = "/",
            Domain = cookieDomain.Equals("localhost", StringComparison.OrdinalIgnoreCase) ? null : cookieDomain,
            MaxAge = TimeSpan.FromDays(7),
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
            MaxAge = TimeSpan.FromDays(7),
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
        // Clear JWT/OIDC cookies
        httpContext.Response.Cookies.Delete("accessToken");
        httpContext.Response.Cookies.Delete("userInfo");
    }
}
