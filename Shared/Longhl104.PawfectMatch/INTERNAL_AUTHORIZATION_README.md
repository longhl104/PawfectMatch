# Internal Authorization - Service-to-Service Communication

This document explains how to use the `[Authorize(Policy = "InternalOnly")]` attribute for backend-to-backend communication in the PawfectMatch system.

## Overview

The `InternalOnly` policy allows secure communication between internal services without requiring user authentication. It uses API key-based authentication via HTTP headers.

## Setup

### 1. Configure API Key

Set the internal API key in your configuration or environment variables:

**appsettings.json:**
```json
{
  "InternalApiKey": "your-secure-api-key-here"
}
```

**Environment Variable:**
```bash
export INTERNAL_API_KEY="your-secure-api-key-here"
```

### 2. Register Authentication Services

The `AddPawfectMatchAuthenticationAndAuthorization` extension method automatically registers the internal authentication handler:

```csharp
builder.Services.AddPawfectMatchAuthenticationAndAuthorization("adopter");
```

## Usage

### 1. Protecting Endpoints

Use the `InternalOnly` policy to restrict endpoints to internal services only:

```csharp
[ApiController]
[Route("api/internal/[controller]")]
public class MyInternalController : ControllerBase
{
    [HttpGet("data")]
    [Authorize(Policy = "InternalOnly")]
    public IActionResult GetInternalData()
    {
        // Only accessible by internal services
        return Ok(new { data = "sensitive internal data" });
    }
}
```

### 2. Making Internal Requests

When calling internal endpoints, include the API key in the request headers:

```csharp
using var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Add("X-Internal-API-Key", "your-secure-api-key-here");

var response = await httpClient.GetAsync("https://other-service/api/internal/data");
```

### 3. Mixed Access Endpoints

You can create endpoints accessible by both users and internal services:

```csharp
[HttpGet("mixed")]
[Authorize] // Uses default policy, but internal auth works too
public IActionResult GetMixedData()
{
    var isInternal = HttpContext.IsInternalRequest();
    
    if (isInternal)
    {
        // Handle internal service request
        return Ok(new { data = "full internal data" });
    }
    else
    {
        // Handle user request
        var user = HttpContext.GetCurrentUser();
        return Ok(new { data = "user data", userEmail = user?.Email });
    }
}
```

## Helper Methods

The library provides extension methods to check request type:

```csharp
// Check if request is from internal service
bool isInternal = HttpContext.IsInternalRequest();

// Get authentication type
string authType = HttpContext.GetAuthenticationType(); // "Internal" or "PawfectMatch"
```

## Security Considerations

1. **API Key Security**: Store API keys securely and rotate them regularly
2. **Network Security**: Use HTTPS for all internal communication
3. **Key Management**: Consider using Azure Key Vault or similar for production
4. **Logging**: Monitor internal API usage for security auditing

## Configuration Examples

### Development
```json
{
  "InternalApiKey": "dev-internal-key-12345"
}
```

### Production (using environment variables)
```bash
# Set in deployment configuration
INTERNAL_API_KEY="prod-secure-key-with-high-entropy-xyz789"
```

## Error Responses

### Missing API Key
```json
{
  "statusCode": 401,
  "message": "Unauthorized"
}
```

### Invalid API Key
```json
{
  "statusCode": 401,
  "message": "Unauthorized"
}
```

### Access Denied
```json
{
  "statusCode": 403,
  "message": "Forbidden"
}
```

## Testing

Test internal endpoints using HTTP tools:

```bash
# Valid request
curl -H "X-Internal-API-Key: your-api-key" \
     https://localhost:7001/api/internal/sample/status

# Invalid request (missing key)
curl https://localhost:7001/api/internal/sample/status
```

## Sample Implementation

See `InternalSampleController.cs` for complete examples of:
- Internal-only endpoints
- Mixed access endpoints
- Error handling
- Request type detection

## Architecture Benefits

1. **Service Isolation**: Clear separation between user-facing and internal APIs
2. **Security**: API key-based authentication for service communication
3. **Flexibility**: Support for both internal and user access in single endpoints
4. **Monitoring**: Easy tracking of internal vs external API usage
5. **Scalability**: Efficient authentication without user session overhead
