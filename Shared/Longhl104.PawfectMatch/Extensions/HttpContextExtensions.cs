using Longhl104.PawfectMatch.Models.Identity;
using Microsoft.AspNetCore.Http;

namespace Longhl104.PawfectMatch.Extensions;

public static class HttpContextExtensions
{
    /// <summary>
    /// Gets the current user from the HTTP context (set by AuthenticationMiddleware)
    /// </summary>
    public static UserProfile? GetCurrentUser(this HttpContext context)
    {
        return context.Items["User"] as UserProfile;
    }

    /// <summary>
    /// Gets the current user ID from the HTTP context
    /// </summary>
    public static string? GetCurrentUserId(this HttpContext context)
    {
        return context.Items["UserId"] as string;
    }

    /// <summary>
    /// Gets the current user email from the HTTP context
    /// </summary>
    public static string? GetCurrentUserEmail(this HttpContext context)
    {
        return context.Items["UserEmail"] as string;
    }

    /// <summary>
    /// Checks if the current user is authenticated
    /// </summary>
    public static bool IsUserAuthenticated(this HttpContext context)
    {
        return context.GetCurrentUser() != null;
    }

    /// <summary>
    /// Checks if the current request is from an internal service
    /// </summary>
    public static bool IsInternalRequest(this HttpContext context)
    {
        return context.User?.FindFirst("AuthenticationType")?.Value == "Internal";
    }

    /// <summary>
    /// Gets the authentication type of the current request
    /// </summary>
    public static string? GetAuthenticationType(this HttpContext context)
    {
        return context.User?.FindFirst("AuthenticationType")?.Value ??
               (context.User?.Identity?.AuthenticationType);
    }
}
