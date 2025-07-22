using Longhl104.PawfectMatch.Utils;

namespace Longhl104.PawfectMatch.Models;

/// <summary>
/// Enumeration of PawfectMatch internal services for service-to-service communication
/// </summary>
public enum PawfectMatchServices
{
    /// <summary>
    /// Identity service - handles user authentication, registration, and profile management
    /// </summary>
    Identity,

    /// <summary>
    /// ShelterHub service - handles shelter administration and pet management
    /// </summary>
    ShelterHub,

    /// <summary>
    /// Matcher service - handles pet matching and adoption processes
    /// </summary>
    Matcher
}

/// <summary>
/// Extension methods for PawfectMatchService enum
/// </summary>
public static class PawfectMatchServiceExtensions
{
    /// <summary>
    /// Gets the service name as a string
    /// </summary>
    /// <param name="service">The service enum value</param>
    /// <returns>Service name as string</returns>
    public static string GetServiceName(this PawfectMatchServices service)
    {
        return service.ToString();
    }

    /// <summary>
    /// Gets the base URL for the service based on environment (local vs deployed)
    /// </summary>
    /// <param name="service">The service enum value</param>
    /// <returns>Base URL for the service</returns>
    public static string GetBaseUrl(this PawfectMatchServices service)
    {
        const string apiSuffix = "/api/internal/";

        if (EnvironmentUrlHelper.IsRunningLocally())
        {
            // Local development URLs
            var localUrl = service switch
            {
                PawfectMatchServices.Identity => "https://localhost:7000",
                PawfectMatchServices.Matcher => "https://localhost:7001",
                PawfectMatchServices.ShelterHub => "https://localhost:7002",
                _ => throw new ArgumentOutOfRangeException(nameof(service), service, "Unknown service")
            };
            return localUrl + apiSuffix;
        }
        else
        {
            // Deployed environment URLs
            var serviceName = service switch
            {
                PawfectMatchServices.Identity => "api-id",
                PawfectMatchServices.Matcher => "api-matcher",
                PawfectMatchServices.ShelterHub => "api-shelter",
                _ => throw new ArgumentOutOfRangeException(nameof(service), service, "Unknown service")
            };

            return EnvironmentUrlHelper.BuildServiceUrl(serviceName, pathSuffix: apiSuffix);
        }
    }
}
