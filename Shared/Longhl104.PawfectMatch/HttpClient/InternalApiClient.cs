using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Longhl104.PawfectMatch.HttpClient;

/// <summary>
/// Interface for internal API client that provides convenient methods for service-to-service communication
/// </summary>
public interface IInternalApiClient
{
    /// <summary>
    /// Sends a GET request to the specified URI
    /// </summary>
    /// <typeparam name="T">The type to deserialize the response to</typeparam>
    /// <param name="requestUri">The URI to send the request to</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The deserialized response</returns>
    Task<T?> GetAsync<T>(string requestUri, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a POST request with JSON content
    /// </summary>
    /// <typeparam name="TRequest">The type of the request object</typeparam>
    /// <typeparam name="TResponse">The type to deserialize the response to</typeparam>
    /// <param name="requestUri">The URI to send the request to</param>
    /// <param name="content">The object to serialize as JSON content</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The deserialized response</returns>
    Task<TResponse?> PostAsync<TRequest, TResponse>(string requestUri, TRequest content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a PUT request with JSON content
    /// </summary>
    /// <typeparam name="TRequest">The type of the request object</typeparam>
    /// <typeparam name="TResponse">The type to deserialize the response to</typeparam>
    /// <param name="requestUri">The URI to send the request to</param>
    /// <param name="content">The object to serialize as JSON content</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The deserialized response</returns>
    Task<TResponse?> PutAsync<TRequest, TResponse>(string requestUri, TRequest content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a DELETE request
    /// </summary>
    /// <param name="requestUri">The URI to send the request to</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the request was successful</returns>
    Task<bool> DeleteAsync(string requestUri, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the underlying HTTP client for custom operations
    /// </summary>
    /// <returns>The configured HTTP client</returns>
    System.Net.Http.HttpClient GetHttpClient();
}

/// <summary>
/// Internal API client for making authenticated service-to-service calls
/// </summary>
public class InternalApiClient : IInternalApiClient, IDisposable
{
    private readonly System.Net.Http.HttpClient _httpClient;
    private readonly ILogger<InternalApiClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _disposed;

    public InternalApiClient(IInternalHttpClientFactory httpClientFactory, ILogger<InternalApiClient> logger)
    {
        _httpClient = httpClientFactory.CreateClient("InternalApiClient");
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    /// <summary>
    /// Constructor that accepts a specific service name and base URL
    /// </summary>
    /// <param name="httpClientFactory">HTTP client factory</param>
    /// <param name="baseUrl">Base URL of the target service</param>
    /// <param name="serviceName">Name of the target service</param>
    /// <param name="logger">Logger instance</param>
    public InternalApiClient(
        IInternalHttpClientFactory httpClientFactory,
        string baseUrl,
        string serviceName,
        ILogger<InternalApiClient> logger)
    {
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
        _httpClient = httpClientFactory.CreateClient(baseUrl, serviceName);
    }

    /// <summary>
    /// Sends a GET request to the specified URI
    /// </summary>
    /// <typeparam name="T">The type to deserialize the response to</typeparam>
    /// <param name="requestUri">The URI to send the request to</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The deserialized response</returns>
    public async Task<T?> GetAsync<T>(string requestUri, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Sending internal GET request to: {RequestUri}", requestUri);

            var response = await _httpClient.GetAsync(requestUri, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<T>(json, _jsonOptions);

                _logger.LogDebug("Internal GET request successful: {RequestUri}", requestUri);
                return result;
            }

            await LogErrorResponse(response, requestUri, "GET");
            return default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during internal GET request to: {RequestUri}", requestUri);
            throw;
        }
    }

    /// <summary>
    /// Sends a POST request with JSON content
    /// </summary>
    /// <typeparam name="TRequest">The type of the request object</typeparam>
    /// <typeparam name="TResponse">The type to deserialize the response to</typeparam>
    /// <param name="requestUri">The URI to send the request to</param>
    /// <param name="content">The object to serialize as JSON content</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The deserialized response</returns>
    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string requestUri, TRequest content, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Sending internal POST request to: {RequestUri}", requestUri);

            var json = JsonSerializer.Serialize(content, _jsonOptions);
            var stringContent = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(requestUri, stringContent, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<TResponse>(responseJson, _jsonOptions);

                _logger.LogDebug("Internal POST request successful: {RequestUri}", requestUri);
                return result;
            }

            await LogErrorResponse(response, requestUri, "POST");
            return default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during internal POST request to: {RequestUri}", requestUri);
            throw;
        }
    }

    /// <summary>
    /// Sends a PUT request with JSON content
    /// </summary>
    /// <typeparam name="TRequest">The type of the request object</typeparam>
    /// <typeparam name="TResponse">The type to deserialize the response to</typeparam>
    /// <param name="requestUri">The URI to send the request to</param>
    /// <param name="content">The object to serialize as JSON content</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The deserialized response</returns>
    public async Task<TResponse?> PutAsync<TRequest, TResponse>(string requestUri, TRequest content, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Sending internal PUT request to: {RequestUri}", requestUri);

            var json = JsonSerializer.Serialize(content, _jsonOptions);
            var stringContent = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync(requestUri, stringContent, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<TResponse>(responseJson, _jsonOptions);

                _logger.LogDebug("Internal PUT request successful: {RequestUri}", requestUri);
                return result;
            }

            await LogErrorResponse(response, requestUri, "PUT");
            return default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during internal PUT request to: {RequestUri}", requestUri);
            throw;
        }
    }

    /// <summary>
    /// Sends a DELETE request
    /// </summary>
    /// <param name="requestUri">The URI to send the request to</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the request was successful</returns>
    public async Task<bool> DeleteAsync(string requestUri, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Sending internal DELETE request to: {RequestUri}", requestUri);

            var response = await _httpClient.DeleteAsync(requestUri, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Internal DELETE request successful: {RequestUri}", requestUri);
                return true;
            }

            await LogErrorResponse(response, requestUri, "DELETE");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during internal DELETE request to: {RequestUri}", requestUri);
            throw;
        }
    }

    /// <summary>
    /// Gets the underlying HTTP client for custom operations
    /// </summary>
    /// <returns>The configured HTTP client</returns>
    public System.Net.Http.HttpClient GetHttpClient()
    {
        return _httpClient;
    }

    /// <summary>
    /// Logs error response details
    /// </summary>
    /// <param name="response">The HTTP response</param>
    /// <param name="requestUri">The request URI</param>
    /// <param name="method">The HTTP method</param>
    private async Task LogErrorResponse(HttpResponseMessage response, string requestUri, string method)
    {
        var responseContent = await response.Content.ReadAsStringAsync();
        _logger.LogWarning(
            "Internal {Method} request failed: {RequestUri} - Status: {StatusCode}, Content: {ResponseContent}",
            method, requestUri, response.StatusCode, responseContent);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _httpClient?.Dispose();
            _disposed = true;
        }
    }
}
