# Internal HTTP Client Factory

This document explains how to use the internal HTTP client factory for secure service-to-service communication in the PawfectMatch system.

## Overview

The Internal HTTP Client Factory provides a secure way for PawfectMatch services to communicate with each other using the `InternalOnly` authorization policy. It automatically handles the required authentication headers and provides convenient APIs for making HTTP requests.

## Features

- **Automatic Authentication**: Automatically adds the `X-Internal-API-Key` header for internal authentication
- **Typed HTTP Client**: Strongly-typed HTTP client with JSON serialization/deserialization
- **Service Discovery**: Support for named clients for specific services
- **Logging**: Built-in logging for debugging and monitoring
- **Error Handling**: Comprehensive error handling and logging
- **Flexible Configuration**: Multiple ways to configure and use the clients

## Setup

### 1. Basic Registration

The internal HTTP client is automatically registered when you use the PawfectMatch authentication extensions:

```csharp
// In Program.cs or Startup.cs
builder.Services.AddPawfectMatchAuthenticationAndAuthorization("adopter");
// This automatically includes AddInternalHttpClient()
```

### 2. Manual Registration

You can also register it manually:

```csharp
builder.Services.AddInternalHttpClient();
```

### 3. Configuration

Ensure you have the internal API key configured in your app settings:

**appsettings.json:**

```json
{
  "InternalApiKey": "your-secure-internal-api-key-here"
}
```

**Environment Variable:**

```bash
export INTERNAL_API_KEY="your-secure-internal-api-key-here"
```

## Usage

### 1. Using IInternalHttpClientFactory

The factory provides direct access to configured HTTP clients:

```csharp
public class MyService
{
    private readonly IInternalHttpClientFactory _httpClientFactory;

    public MyService(IInternalHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<UserProfile?> GetUserAsync(string userId)
    {
        using var httpClient = _httpClientFactory.CreateClient("https://localhost:5001", "Identity");
        
        var response = await httpClient.GetAsync($"/api/users/{userId}");
        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<UserProfile>(json);
        }
        
        return null;
    }
}
```

### 2. Using IInternalApiClient (Recommended)

The API client provides a more convenient, strongly-typed interface:

```csharp
public class UserService
{
    private readonly IInternalApiClient _apiClient;

    public UserService(IInternalApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    // GET request
    public async Task<UserProfile?> GetUserAsync(string userId)
    {
        return await _apiClient.GetAsync<UserProfile>($"https://localhost:5001/api/users/{userId}");
    }

    // POST request
    public async Task<CreateUserResponse?> CreateUserAsync(CreateUserRequest request)
    {
        return await _apiClient.PostAsync<CreateUserRequest, CreateUserResponse>(
            "https://localhost:5001/api/users", request);
    }

    // PUT request
    public async Task<UpdateUserResponse?> UpdateUserAsync(string userId, UpdateUserRequest request)
    {
        return await _apiClient.PutAsync<UpdateUserRequest, UpdateUserResponse>(
            $"https://localhost:5001/api/users/{userId}", request);
    }

    // DELETE request
    public async Task<bool> DeleteUserAsync(string userId)
    {
        return await _apiClient.DeleteAsync($"https://localhost:5001/api/users/{userId}");
    }
}
```

### 3. Named Service Clients

Register clients for specific services:

```csharp
// In Program.cs
builder.Services.AddPawfectMatchInternalHttpClients(
    identityServiceUrl: "https://localhost:5001",
    matcherServiceUrl: "https://localhost:5002",
    shelterHubServiceUrl: "https://localhost:5003"
);
```

Then use named clients:

```csharp
public class IntegratedService
{
    private readonly IInternalHttpClientFactory _httpClientFactory;

    public IntegratedService(IInternalHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<UserProfile?> GetUserFromIdentityService(string userId)
    {
        using var httpClient = _httpClientFactory.CreateClient("Identity");
        // httpClient.BaseAddress is already set to https://localhost:5001
        
        var response = await httpClient.GetAsync($"/api/users/{userId}");
        // ... handle response
    }
}
```

## Service-to-Service Examples

### Matcher Service Calling Identity Service

```csharp
// In Matcher service
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdopterOnly")] // Regular user authentication
public class MatchingController : ControllerBase
{
    private readonly IInternalApiClient _internalApiClient;

    public MatchingController(IInternalApiClient internalApiClient)
    {
        _internalApiClient = internalApiClient;
    }

    [HttpGet("user-preferences/{userId}")]
    public async Task<IActionResult> GetUserPreferences(string userId)
    {
        // This call uses internal authentication to call Identity service
        var userProfile = await _internalApiClient.GetAsync<UserProfile>(
            $"https://identity-service/api/internal/users/{userId}");
        
        if (userProfile == null)
        {
            return NotFound("User not found");
        }

        // Process user preferences...
        return Ok(userProfile);
    }
}
```

### Identity Service Internal Endpoint

```csharp
// In Identity service
[ApiController]
[Route("api/internal/[controller]")]
[Authorize(Policy = "InternalOnly")] // Internal authentication only
public class UsersController : ControllerBase
{
    [HttpGet("{userId}")]
    public async Task<IActionResult> GetUser(string userId)
    {
        // This endpoint only accepts requests with valid internal API key
        var user = await _userService.GetUserAsync(userId);
        return Ok(user);
    }
}
```

## Configuration Options

### Custom HTTP Client Configuration

```csharp
builder.Services.AddInternalHttpClient(services =>
{
    // Add custom named clients
    services.AddHttpClient("CustomService", client =>
    {
        client.BaseAddress = new Uri("https://custom-service.example.com");
        client.Timeout = TimeSpan.FromSeconds(60);
    });
});
```

### Service-Specific Configuration

```csharp
builder.Services.AddInternalHttpClientForService("PaymentService", "https://payment-service.local");
```

## Security Considerations

1. **API Key Protection**: Keep the internal API key secure and rotate it regularly
2. **Network Security**: Use HTTPS for all internal communications
3. **Service Isolation**: Only expose internal endpoints that need to be called by other services
4. **Monitoring**: Monitor internal API usage for suspicious activity
5. **Timeouts**: Configure appropriate timeouts to prevent hanging requests

## Error Handling

The internal HTTP client includes comprehensive error handling:

```csharp
try
{
    var result = await _apiClient.GetAsync<UserProfile>("/api/users/123");
    if (result == null)
    {
        // Handle case where request succeeded but returned null/empty
        _logger.LogWarning("User not found or empty response");
    }
}
catch (HttpRequestException ex)
{
    // Handle network-related errors
    _logger.LogError(ex, "Network error calling internal service");
}
catch (TaskCanceledException ex)
{
    // Handle timeout errors
    _logger.LogError(ex, "Request timeout calling internal service");
}
catch (Exception ex)
{
    // Handle other unexpected errors
    _logger.LogError(ex, "Unexpected error calling internal service");
}
```

## Testing

### Unit Testing

Mock the interfaces for unit testing:

```csharp
[Test]
public async Task GetUser_ReturnsUserProfile()
{
    // Arrange
    var mockApiClient = new Mock<IInternalApiClient>();
    var expectedUser = new UserProfile { UserId = "123", Email = "test@example.com" };
    
    mockApiClient.Setup(x => x.GetAsync<UserProfile>(It.IsAny<string>(), default))
              .ReturnsAsync(expectedUser);

    var service = new UserService(mockApiClient.Object);

    // Act
    var result = await service.GetUserAsync("123");

    // Assert
    Assert.AreEqual(expectedUser.Email, result?.Email);
}
```

### Integration Testing

```csharp
[Test]
public async Task InternalApiCall_WithValidApiKey_ReturnsSuccess()
{
    // Arrange
    var factory = new WebApplicationFactory<Program>();
    var client = factory.CreateClient();
    
    // Add internal API key header
    client.DefaultRequestHeaders.Add("X-Internal-API-Key", "test-api-key");

    // Act
    var response = await client.GetAsync("/api/internal/users/123");

    // Assert
    Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
}
```

## Troubleshooting

### Common Issues

1. **401 Unauthorized**
   - Check that the internal API key is correctly configured
   - Verify the API key matches between services
   - Ensure the target endpoint uses `[Authorize(Policy = "InternalOnly")]`

2. **Configuration Missing**
   - Ensure `InternalApiKey` is set in configuration
   - Check that `AddInternalHttpClient()` is called in DI setup

3. **Network Errors**
   - Verify service URLs are correct and accessible
   - Check firewall and network configuration
   - Ensure target service is running

4. **Serialization Issues**
   - Verify request/response models match between services
   - Check JSON property naming conventions (camelCase vs PascalCase)

### Debugging

Enable debug logging to see HTTP client activity:

```json
{
  "Logging": {
    "LogLevel": {
      "Longhl104.PawfectMatch.HttpClient": "Debug",
      "System.Net.Http.HttpClient": "Information"
    }
  }
}
```

This will show detailed logs of internal HTTP requests and responses.
