using Longhl104.PawfectMatch.HttpClient;
using Longhl104.PawfectMatch.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Longhl104.PawfectMatch.Extensions;

/// <summary>
/// Extension methods for configuring internal HTTP client services
/// </summary>
public static class InternalHttpClientExtensions
{
    /// <summary>
    /// Adds the internal HTTP client factory to the service collection.
    /// This enables service-to-service communication using the InternalOnly policy.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddInternalHttpClient(this IServiceCollection services)
    {
        // Register the standard IHttpClientFactory if not already registered
        services.AddHttpClient();

        // Register our internal HTTP client factory
        services.AddScoped<IInternalHttpClientFactory, InternalHttpClientFactory>();

        return services;
    }

    /// <summary>
    /// Adds the internal HTTP client factory with named HTTP client configurations.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureClients">Action to configure named HTTP clients</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddInternalHttpClient(
        this IServiceCollection services,
        Action<IServiceCollection> configureClients)
    {
        // Add the basic internal HTTP client
        services.AddInternalHttpClient();

        // Allow additional client configuration
        configureClients(services);

        return services;
    }

    /// <summary>
    /// Adds a named internal HTTP client for a specific service.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="serviceName">Name of the service</param>
    /// <param name="baseUrl">Base URL of the service</param>
    /// <returns>The service collection for chaining</returns>
    private static IServiceCollection AddInternalHttpClientForService(
        this IServiceCollection services,
        string serviceName,
        string baseUrl
        )
    {
        if (string.IsNullOrWhiteSpace(serviceName))
            throw new ArgumentException("Service name cannot be null or empty", nameof(serviceName));

        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new ArgumentException("Base URL cannot be null or empty", nameof(baseUrl));

        // Add the basic internal HTTP client factory
        services.AddInternalHttpClient();

        // Register a named HTTP client for the specific service
        services.AddHttpClient(serviceName, client =>
        {
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(30); // Default timeout
        });

        return services;
    }

    /// <summary>
    /// Adds named internal HTTP clients for common PawfectMatch services.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="identityServiceUrl">URL of the Identity service (optional)</param>
    /// <param name="matcherServiceUrl">URL of the Matcher service (optional)</param>
    /// <param name="shelterHubServiceUrl">URL of the ShelterHub service (optional)</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddPawfectMatchInternalHttpClients(
        this IServiceCollection services,
        List<PawfectMatchServices> serviceNames
        )
    {
        // Add the basic internal HTTP client factory
        services.AddInternalHttpClient();

        // Register named clients for known services
        foreach (var service in serviceNames)
        {
            var serviceName = service.GetServiceName();
            var baseUrl = service.GetBaseUrl();

            services.AddInternalHttpClientForService(serviceName, baseUrl);
        }

        return services;
    }
}
