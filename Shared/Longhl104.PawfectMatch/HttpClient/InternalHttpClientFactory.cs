using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;

namespace Longhl104.PawfectMatch.HttpClient;

/// <summary>
/// Interface for internal HTTP client factory for service-to-service communication
/// </summary>
public interface IInternalHttpClientFactory
{
    /// <summary>
    /// Creates an HTTP client configured for internal service communication
    /// </summary>
    /// <param name="serviceName">Name of the target service (optional)</param>
    /// <returns>HttpClient configured with internal authentication headers</returns>
    System.Net.Http.HttpClient CreateClient(string? serviceName = null);

    /// <summary>
    /// Creates an HTTP client configured for a specific service URL
    /// </summary>
    /// <param name="baseUrl">Base URL of the target service</param>
    /// <param name="serviceName">Name of the target service (optional)</param>
    /// <returns>HttpClient configured with internal authentication headers</returns>
    System.Net.Http.HttpClient CreateClient(string baseUrl, string? serviceName = null);
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
    /// Creates an HTTP client configured for internal service communication
    /// </summary>
    /// <param name="serviceName">Name of the target service (optional)</param>
    /// <returns>HttpClient configured with internal authentication headers</returns>
    public System.Net.Http.HttpClient CreateClient(string? serviceName = null)
    {
        var clientName = serviceName ?? "internal-client";
        var httpClient = _httpClientFactory.CreateClient(clientName);

        ConfigureInternalHeaders(httpClient, serviceName);

        return httpClient;
    }

    /// <summary>
    /// Creates an HTTP client configured for a specific service URL
    /// </summary>
    /// <param name="baseUrl">Base URL of the target service</param>
    /// <param name="serviceName">Name of the target service (optional)</param>
    /// <returns>HttpClient configured with internal authentication headers</returns>
    public System.Net.Http.HttpClient CreateClient(string baseUrl, string? serviceName = null)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new ArgumentException("Base URL cannot be null or empty", nameof(baseUrl));

        var httpClient = CreateClient(serviceName);

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
