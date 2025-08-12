using System.Text.Json;
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
    /// Get the appropriate cookie domain for cross-subdomain sharing
    /// </summary>
    private static string? GetCookieDomain(string host)
    {
        // For localhost, don't set domain (allows local development)
        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
            host.StartsWith("127.0.0.1") ||
            host.StartsWith("::1"))
        {
            return null;
        }

        // For pawfectmatchnow.com domains, set to parent domain for cross-subdomain sharing
        if (host.EndsWith(".pawfectmatchnow.com", StringComparison.OrdinalIgnoreCase))
        {
            // Extract the environment and set domain to .{environment}.pawfectmatchnow.com
            // Examples:
            // id.development.pawfectmatchnow.com -> .development.pawfectmatchnow.com
            // shelter.production.pawfectmatchnow.com -> .production.pawfectmatchnow.com
            var parts = host.Split('.');
            if (parts.Length >= 3)
            {
                // Skip the first part (service name) and use the rest with leading dot
                var domain = "." + string.Join(".", parts.Skip(1));
                return domain;
            }
        }

        // For other domains, use the full host
        return host;
    }

    /// <summary>
    /// Set JWT-based authentication cookies for full authentication
    /// </summary>
    public void SetJwtAuthenticationCookies(HttpContext httpContext, string accessToken, UserProfile user)
    {
        var cookieDomain = GetCookieDomain(httpContext.Request.Host.Host);
        var host = httpContext.Request.Host.Host;

        // For production domains (.pawfectmatchnow.com), always use HTTPS settings
        // For local development, adjust based on actual protocol
        var isProductionDomain = host.EndsWith(".pawfectmatchnow.com", StringComparison.OrdinalIgnoreCase);

        // Special handling for cross-domain cookies:
        // - If using production domains, we MUST use Secure + SameSite=None for cross-domain sharing
        // - If localhost, use Lax (no cross-domain needed)
        SameSiteMode sameSite;
        bool forceSecure;

        if (isProductionDomain)
        {
            // Production domains MUST use Secure + None for cross-domain sharing
            sameSite = SameSiteMode.None;
            forceSecure = true; // Always secure for production domains
        }
        else
        {
            // Local development: use Lax (works without HTTPS)
            sameSite = SameSiteMode.Lax;
            forceSecure = httpContext.Request.IsHttps;
        }

        Console.WriteLine($"Setting cookies for domain: {cookieDomain}, secure: {forceSecure}, sameSite: {sameSite}, host: {host}");
        Console.WriteLine($"Request.IsHttps: {httpContext.Request.IsHttps}, Request.Scheme: {httpContext.Request.Scheme}");
        Console.WriteLine($"IsProductionDomain: {isProductionDomain}");

        // Access token cookie
        httpContext.Response.Cookies.Append("accessToken", accessToken, new CookieOptions
        {
            Path = "/",
            Domain = cookieDomain,
            MaxAge = TimeSpan.FromDays(7),
            HttpOnly = true,
            Secure = forceSecure,
            SameSite = sameSite
        });

        // User info cookie (non-HttpOnly for client access)
        var userInfoJson = JsonSerializer.Serialize(user, CamelCaseOptions);
        var userInfoBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(userInfoJson));
        httpContext.Response.Cookies.Append("userInfo", userInfoBase64, new CookieOptions
        {
            Path = "/",
            Domain = cookieDomain,
            MaxAge = TimeSpan.FromDays(7),
            HttpOnly = false,
            Secure = forceSecure,
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
