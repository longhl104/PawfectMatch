# Matcher Service - Authorization Implementation

## Overview
The Matcher service has been configured with a global authorization policy that requires all API endpoints to be accessed only by users with the "Adopter" role.

## Implementation Details

### 1. Global Authorization Policy
- **Location**: `Program.cs`
- **Policy**: All API endpoints require authenticated users with `UserType` claim equal to "Adopter"
- **Fallback Policy**: Applied to all endpoints by default

### 2. Claims-Based Authentication
- **Location**: `AuthenticationMiddleware.cs`
- **Claims Added**:
  - `NameIdentifier`: User ID
  - `Email`: User email address
  - `UserType`: User role (must be "Adopter" for access)
  - `Name`: Full name (if available)

### 3. Authorization Enforcement
- **Middleware Check**: Authentication middleware validates user type before allowing API access
- **HTTP Responses**:
  - `401 Unauthorized`: User not authenticated
  - `403 Forbidden`: User authenticated but not an Adopter
  - `200 OK`: Adopter user with valid access

## Protected Endpoints

All API endpoints under `/api/*` require Adopter role:

- `GET /api/AuthCheck/status` - Check authentication status
- `POST /api/AuthCheck/logout` - User logout
- `GET /api/AuthCheck/adopter-only` - Test endpoint demonstrating role restriction

## Unprotected Endpoints

The following endpoints skip authentication:

- `/health` - Health check
- `/swagger` - API documentation
- `/openapi` - OpenAPI specification
- `/.well-known` - Well-known endpoints

## Testing Authorization

### Using HTTP Client
Use the provided `Longhl104.Matcher.http` file to test endpoints:

1. **Without Authentication**: Should return 401 Unauthorized
2. **With Non-Adopter User**: Should return 403 Forbidden
3. **With Adopter User**: Should return 200 OK

### Expected Responses

#### Unauthorized (No Authentication)
```json
{
  "success": false,
  "message": "No authentication cookies found",
  "isAuthenticated": false,
  "redirectUrl": "https://localhost:4200/auth/login",
  "requiresRefresh": false
}
```

#### Forbidden (Non-Adopter User)
```json
{
  "success": false,
  "message": "Access denied. Only users with Adopter role can access this service.",
  "isAuthenticated": true,
  "redirectUrl": null,
  "requiresRefresh": false
}
```

#### Success (Adopter User)
```json
{
  "isAuthenticated": true,
  "message": "User is authenticated",
  "user": {
    "userId": "123",
    "email": "adopter@example.com",
    "userType": "Adopter",
    // ... other user properties
  }
}
```

## Adding New Endpoints

Any new API endpoints added to the Matcher service will automatically inherit the global authorization policy requiring the Adopter role. No additional configuration is needed.

### Example New Controller
```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize] // Will use the global Adopter-only policy
public class PetMatchController : ControllerBase
{
    [HttpGet]
    public IActionResult GetMatches()
    {
        // This endpoint automatically requires Adopter role
        return Ok();
    }
}
```

## Configuration

### Environment Variables
- `IdentityUrl`: URL of the Identity service for login redirects (default: `https://localhost:4200`)

### Claims Configuration
The authorization relies on the following claim in the JWT token:
- **Claim Type**: `UserType`
- **Required Value**: `Adopter`

## Security Notes

1. **Token Validation**: JWT tokens are validated for format and expiration
2. **Cookie Security**: Authentication cookies use appropriate security settings
3. **Role Enforcement**: Both middleware and ASP.NET Core authorization enforce role requirements
4. **Logging**: Authorization failures are logged for monitoring

## Troubleshooting

### Common Issues

1. **403 Forbidden for Adopter Users**
   - Check that the JWT token contains `UserType: "Adopter"` claim
   - Verify token is not expired

2. **401 Unauthorized**
   - Ensure authentication cookies are present
   - Check JWT token format and validity

3. **Redirect Loops**
   - Verify Identity service URL configuration
   - Check CORS settings for cross-origin requests
