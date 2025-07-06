# Internal HTTP Client Factory - Quick Start Guide

The Internal HTTP Client Factory in `Longhl104.PawfectMatch` library provides secure service-to-service communication using the `InternalOnly` authorization policy.

## Quick Setup

### 1. Install the Package

```bash
dotnet add package Longhl104.PawfectMatch
```

### 2. Configure Services

```csharp
// In Program.cs or Startup.cs
builder.Services.AddPawfectMatchAuthenticationAndAuthorization("adopter");
// This automatically includes the internal HTTP client factory
```

### 3. Configure API Key

Add to your `appsettings.json`:

```json
{
  "InternalApiKey": "your-secure-internal-api-key-here"
}
```

## Usage Examples

### Basic Usage with IInternalApiClient

```csharp
public class UserService
{
    private readonly IInternalApiClient _apiClient;

    public UserService(IInternalApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<UserProfile?> GetUserAsync(string userId)
    {
        return await _apiClient.GetAsync<UserProfile>(
            $"https://identity-service/api/internal/users/{userId}");
    }

    public async Task<bool> UpdateUserAsync(string userId, UserProfile user)
    {
        var response = await _apiClient.PutAsync<UserProfile, object>(
            $"https://identity-service/api/internal/users/{userId}", user);
        return response != null;
    }
}
```

### Using IInternalHttpClientFactory

```csharp
public class MyService
{
    private readonly IInternalHttpClientFactory _httpClientFactory;

    public MyService(IInternalHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<string> CallInternalServiceAsync()
    {
        using var httpClient = _httpClientFactory.CreateClient(
            "https://localhost:5001", "IdentityService");

        var response = await httpClient.GetAsync("/api/internal/health");
        return await response.Content.ReadAsStringAsync();
    }
}
```

### Creating Internal-Only Endpoints

```csharp
[ApiController]
[Route("api/internal/[controller]")]
[Authorize(Policy = "InternalOnly")] // Only accepts internal API key authentication
public class UsersController : ControllerBase
{
    [HttpGet("{userId}")]
    public async Task<IActionResult> GetUser(string userId)
    {
        // This endpoint can only be called by other services with valid internal API key
        var user = await _userService.GetUserAsync(userId);
        return Ok(user);
    }
}
```

## Key Features

- **Automatic Authentication**: Adds `X-Internal-API-Key` header automatically
- **Typed Responses**: Built-in JSON serialization/deserialization
- **Error Handling**: Comprehensive logging and error handling
- **Flexible**: Multiple ways to configure and use the clients

## Security Notes

1. Keep the `InternalApiKey` secure and rotate it regularly
2. Use HTTPS for all internal communications
3. Only expose internal endpoints that need to be called by other services
4. The same API key must be configured across all services

For detailed documentation and advanced usage, see the complete README in the HttpClient folder.
