using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using Longhl104.PawfectMatch.Models;

namespace Longhl104.PawfectMatch.HttpClient;

/// <summary>
/// Interface for internal HTTP client factory for service-to-service communication
/// </summary>
public interface IInternalHttpClientFactory
{
    /// <summary>
    /// Creates an HTTP client configured for a specific PawfectMatch service
    /// </summary>
    /// <param name="service">The target service</param>
    /// <returns>HttpClient configured with internal authentication headers and service URL</returns>
    System.Net.Http.HttpClient CreateClient(PawfectMatchServices service);
}

/// <summary>
/// HTTP client factory for internal service-to-service communication.
/// Automatically adds the required internal API key header for InternalOnly policy endpoints.
/// </summary>
public class InternalHttpClientFactory : IInternalHttpClientFactory
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<InternalHttpClientFactory> _logger;
    private readonly string _internalApiKey;

    public InternalHttpClientFactory(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<InternalHttpClientFactory> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
        _internalApiKey = _configuration["InternalApiKey"] ??
            throw new InvalidOperationException("InternalApiKey not configured. Please set this in your app settings or environment variables.");
    }

    /// <summary>
    /// Creates an HTTP client configured for a specific PawfectMatch service
    /// </summary>
    /// <param name="service">The target service</param>
    /// <returns>HttpClient configured with internal authentication headers and service URL</returns>
    public System.Net.Http.HttpClient CreateClient(PawfectMatchServices service)
    {
        var serviceName = service.GetServiceName();
        var baseUrl = GetServiceUrl(service);

        return CreateClient(baseUrl, serviceName);
    }

    /// <summary>
    /// Creates an HTTP client configured for a custom service URL
    /// </summary>
    /// <param name="baseUrl">Base URL of the target service</param>
    /// <param name="serviceName">Name of the target service (optional)</param>
    /// <returns>HttpClient configured with internal authentication headers</returns>
    private System.Net.Http.HttpClient CreateClient(string baseUrl, string? serviceName = null)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new ArgumentException("Base URL cannot be null or empty", nameof(baseUrl));

        var httpClient = _httpClientFactory.CreateClient(serviceName ?? "internal");

        ConfigureInternalHeaders(httpClient, serviceName);

        try
        {
            httpClient.BaseAddress = new Uri(baseUrl);
            _logger.LogDebug("Created internal HTTP client for service: {ServiceName} at {BaseUrl}",
                serviceName ?? "unnamed", baseUrl);
        }
        catch (UriFormatException ex)
        {
            _logger.LogError(ex, "Invalid base URL provided: {BaseUrl}", baseUrl);
            throw new ArgumentException($"Invalid base URL format: {baseUrl}", nameof(baseUrl), ex);
        }

        return httpClient;
    }

    /// <summary>
    /// Gets the service URL from configuration with fallback to default development URL
    /// </summary>
    /// <param name="service">The service to get URL for</param>
    /// <returns>Service URL</returns>
    private string GetServiceUrl(PawfectMatchServices service)
    {
        var configKey = service.GetUrlConfigKey();
        var configuredUrl = _configuration[configKey];

        if (!string.IsNullOrWhiteSpace(configuredUrl))
        {
            _logger.LogDebug("Using configured URL for {Service}: {Url}", service, configuredUrl);
            return configuredUrl;
        }

        var defaultUrl = service.GetDefaultDevelopmentUrl();
        _logger.LogDebug("Using default development URL for {Service}: {Url}", service, defaultUrl);
        return defaultUrl;
    }

    /// <summary>
    /// Configures the HTTP client with internal authentication headers
    /// </summary>
    /// <param name="httpClient">The HTTP client to configure</param>
    /// <param name="serviceName">Name of the target service (optional)</param>
    private void ConfigureInternalHeaders(System.Net.Http.HttpClient httpClient, string? serviceName)
    {
        // Add the internal API key header for authentication
        httpClient.DefaultRequestHeaders.Add("X-Internal-API-Key", _internalApiKey);

        // Add user agent header to identify the calling service
        var userAgent = $"PawfectMatch-Internal-Client/1.0";
        if (!string.IsNullOrWhiteSpace(serviceName))
        {
            userAgent += $" ({serviceName})";
        }

        httpClient.DefaultRequestHeaders.UserAgent.Clear();
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);

        // Set content type for JSON by default
        httpClient.DefaultRequestHeaders.Accept.Clear();
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Add request ID header for tracing
        httpClient.DefaultRequestHeaders.Add("X-Request-Source", "internal-service");

        _logger.LogDebug("Configured internal HTTP client with authentication headers for service: {ServiceName}",
            serviceName ?? "unnamed");
    }
}
