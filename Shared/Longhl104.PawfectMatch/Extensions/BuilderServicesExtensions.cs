using Longhl104.PawfectMatch.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.DataProtection;
using Amazon.AspNetCore.DataProtection.SSM;

namespace Longhl104.PawfectMatch.Extensions;

public static class BuilderServicesExtensions
{
    public static IServiceCollection AddPawfectMatchAuthenticationAndAuthorization(this IServiceCollection services, string userType)
    {
        // Add authentication and authorization services
        // Configure custom authentication scheme to work with the AuthenticationMiddleware
        services.AddAuthentication("PawfectMatch")
            .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, PawfectMatchAuthenticationHandler>(
                "PawfectMatch", options => { })
            .AddScheme<InternalAuthenticationOptions, InternalAuthenticationHandler>(
                "Internal", options => { });

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder("PawfectMatch")
                .RequireAuthenticatedUser()
                .RequireClaim("UserType", userType)
                .Build())
            .SetDefaultPolicy(new AuthorizationPolicyBuilder("PawfectMatch")
                .RequireAuthenticatedUser()
                .RequireClaim("UserType", userType)
                .Build())
            .AddPolicy("AdopterOnly", policy =>
                policy.RequireAuthenticatedUser()
                    .RequireClaim("UserType", userType))
            .AddPolicy("InternalOnly", policy =>
                policy.AddAuthenticationSchemes("Internal")
                    .RequireAuthenticatedUser()
                    .RequireClaim("UserType", "Internal"));

        // Add internal HTTP client for service-to-service communication
        services.AddInternalHttpClient();

        return services;
    }

    /// <summary>
    /// Adds AWS-based Data Protection configuration for containerized ASP.NET Core applications
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="applicationName">The application name for key isolation</param>
    /// <param name="stage">The deployment stage (development/production)</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddPawfectMatchDataProtection(this IServiceCollection services, string applicationName, string stage)
    {
        services.AddDataProtection()
            .SetApplicationName($"PawfectMatch-{applicationName}-{stage}")
            .PersistKeysToAWSSystemsManager($"/PawfectMatch/{stage}/DataProtection/{applicationName}");

        return services;
    }
}
