using Longhl104.PawfectMatch.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

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
}
