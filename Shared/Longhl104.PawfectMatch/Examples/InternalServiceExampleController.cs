using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Longhl104.PawfectMatch.HttpClient;
using Longhl104.PawfectMatch.Models.Identity;

namespace Longhl104.PawfectMatch.Examples;

/// <summary>
/// Example controller demonstrating how to use internal HTTP client for service-to-service communication
/// </summary>
[ApiController]
[Route("api/example/[controller]")]
public class InternalServiceExampleController : ControllerBase
{
    private readonly IInternalApiClient _internalApiClient;
    private readonly IInternalHttpClientFactory _httpClientFactory;
    private readonly ILogger<InternalServiceExampleController> _logger;

    public InternalServiceExampleController(
        IInternalApiClient internalApiClient,
        IInternalHttpClientFactory httpClientFactory,
        ILogger<InternalServiceExampleController> logger)
    {
        _internalApiClient = internalApiClient;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Example 1: Using IInternalApiClient (Recommended approach)
    /// This endpoint demonstrates calling another service's internal API
    /// </summary>
    [HttpGet("user/{userId}/profile")]
    [Authorize] // Regular user authentication for external callers
    public async Task<IActionResult> GetUserProfile(string userId)
    {
        try
        {
            // This would call the Identity service's internal endpoint
            // The internal API client automatically adds the X-Internal-API-Key header
            var userProfile = await _internalApiClient.GetAsync<UserProfile>(
                $"https://localhost:5001/api/internal/users/{userId}");

            if (userProfile == null)
            {
                return NotFound($"User {userId} not found");
            }

            return Ok(userProfile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user profile for {UserId}", userId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Example 2: Using IInternalHttpClientFactory for more control
    /// </summary>
    [HttpPost("user/{userId}/preferences")]
    [Authorize]
    public async Task<IActionResult> UpdateUserPreferences(string userId, [FromBody] object preferences)
    {
        try
        {
            // Create a client with specific configuration
            using var httpClient = _httpClientFactory.CreateClient("https://localhost:5001", "Identity");

            var json = System.Text.Json.JsonSerializer.Serialize(preferences);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await httpClient.PutAsync($"/api/internal/users/{userId}/preferences", content);

            if (response.IsSuccessStatusCode)
            {
                return Ok(new { Message = "Preferences updated successfully" });
            }

            return StatusCode((int)response.StatusCode, "Failed to update preferences");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating preferences for user {UserId}", userId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Example 3: Internal-only endpoint that can only be called by other services
    /// This demonstrates how to create endpoints that accept internal calls
    /// </summary>
    [HttpGet("internal/health-check")]
    [Authorize(Policy = "InternalOnly")] // Only internal services can call this
    public IActionResult InternalHealthCheck()
    {
        return Ok(new
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Service = "ExampleService",
            Version = "1.0.0"
        });
    }

    /// <summary>
    /// Example 4: Calling multiple internal services
    /// </summary>
    [HttpGet("user/{userId}/dashboard")]
    [Authorize]
    public async Task<IActionResult> GetUserDashboard(string userId)
    {
        try
        {
            // Parallel calls to multiple internal services
            var userProfileTask = _internalApiClient.GetAsync<UserProfile>(
                $"https://localhost:5001/api/internal/users/{userId}");

            var userPreferencesTask = _internalApiClient.GetAsync<object>(
                $"https://localhost:5002/api/internal/preferences/{userId}");

            var userActivityTask = _internalApiClient.GetAsync<object>(
                $"https://localhost:5003/api/internal/activity/{userId}");

            // Wait for all calls to complete
            await Task.WhenAll(userProfileTask, userPreferencesTask, userActivityTask);

            var dashboard = new
            {
                Profile = userProfileTask.Result,
                Preferences = userPreferencesTask.Result,
                Activity = userActivityTask.Result
            };

            return Ok(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building dashboard for user {UserId}", userId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Example 5: Error handling with internal API calls
    /// </summary>
    [HttpGet("user/{userId}/safe-profile")]
    [Authorize]
    public async Task<IActionResult> GetUserProfileSafe(string userId)
    {
        try
        {
            var userProfile = await _internalApiClient.GetAsync<UserProfile>(
                $"https://localhost:5001/api/internal/users/{userId}");

            return Ok(userProfile ?? new UserProfile
            {
                UserId = userId,
                Email = "unknown@example.com",
                FullName = "Unknown User"
            });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Network error calling Identity service for user {UserId}", userId);
            return StatusCode(503, "Identity service unavailable");
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Timeout calling Identity service for user {UserId}", userId);
            return StatusCode(504, "Identity service timeout");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling Identity service for user {UserId}", userId);
            return StatusCode(500, "Internal server error");
        }
    }
}

/// <summary>
/// Example service class showing dependency injection usage
/// </summary>
public interface IUserIntegrationService
{
    Task<UserProfile?> GetUserAsync(string userId);
    Task<bool> UpdateUserAsync(string userId, UserProfile userProfile);
}

public class UserIntegrationService : IUserIntegrationService
{
    private readonly IInternalApiClient _apiClient;
    private readonly ILogger<UserIntegrationService> _logger;

    public UserIntegrationService(IInternalApiClient apiClient, ILogger<UserIntegrationService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<UserProfile?> GetUserAsync(string userId)
    {
        try
        {
            return await _apiClient.GetAsync<UserProfile>($"https://localhost:5001/api/internal/users/{userId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user {UserId}", userId);
            return null;
        }
    }

    public async Task<bool> UpdateUserAsync(string userId, UserProfile userProfile)
    {
        try
        {
            var response = await _apiClient.PutAsync<UserProfile, object>(
                $"https://localhost:5001/api/internal/users/{userId}", userProfile);
            return response != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update user {UserId}", userId);
            return false;
        }
    }
}
