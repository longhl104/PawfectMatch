namespace Longhl104.PawfectMatch.Utils;

/// <summary>
/// Helper class for generating environment-specific URLs
/// </summary>
public static class EnvironmentUrlHelper
{
    /// <summary>
    /// Checks if the application is running in local development mode
    /// </summary>
    /// <returns>True if running locally, false otherwise</returns>
    public static bool IsRunningLocally()
    {
        return Environment.GetEnvironmentVariable("DOTNET_RUNNING_LOCALLY") == "true";
    }

    /// <summary>
    /// Gets the current environment name (normalized to lowercase)
    /// </summary>
    /// <returns>Environment name in lowercase</returns>
    public static string GetEnvironmentName()
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        return environment.ToLowerInvariant();
    }

    /// <summary>
    /// Builds a URL for a service based on environment and subdomain
    /// </summary>
    /// <param name="subdomain">The subdomain for the service (e.g., "id", "api-matcher")</param>
    /// <param name="localUrl">The local development URL (optional, defaults to localhost pattern)</param>
    /// <param name="pathSuffix">Additional path to append to the URL (optional)</param>
    /// <returns>Complete URL for the service</returns>
    public static string BuildServiceUrl(string subdomain, string? localUrl = null, string? pathSuffix = null)
    {
        if (IsRunningLocally() && !string.IsNullOrEmpty(localUrl))
        {
            return localUrl + (pathSuffix ?? string.Empty);
        }

        var envName = GetEnvironmentName();
        var baseUrl = string.Equals(envName, "production", StringComparison.OrdinalIgnoreCase)
            ? $"https://{subdomain}.pawfectmatchnow.com"
            : $"https://{subdomain}.{envName}.pawfectmatchnow.com";

        return baseUrl + (pathSuffix ?? string.Empty);
    }
}
