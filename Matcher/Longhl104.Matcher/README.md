# Longhl104.Matcher Authentication API

This API provides authentication checking functionality for the PawfectMatch Matcher application, validating user authentication based on cookies set by the Identity service.

## Features

- **Cookie-based Authentication**: Validates JWT tokens stored in HTTP-only cookies
- **Automatic Redirects**: Redirects unauthenticated users to the Identity login page
- **User Context**: Provides current user information to controllers
- **Token Validation**: Checks JWT format and expiration
- **CORS Support**: Configured for cross-origin requests

## API Endpoints

### Authentication Check

**GET** `/api/authcheck/status`

Returns the authentication status of the current user.

#### Response (Authenticated)
```json
{
  "isAuthenticated": true,
  "message": "User is authenticated",
  "user": {
    "userId": "user-123",
    "email": "user@example.com",
    "userType": "adopter"
  },
  "tokenExpiresAt": "2025-07-03T14:30:00Z"
}
```

#### Response (Not Authenticated)
```json
{
  "isAuthenticated": false,
  "message": "No authentication cookies found",
  "redirectUrl": "https://localhost:4200/auth/login"
}
```

### Login Redirect

**GET** `/api/authcheck/login`

Redirects the user to the Identity login page.

### Logout

**POST** `/api/authcheck/logout`

Clears authentication cookies and returns logout confirmation.

#### Response
```json
{
  "isAuthenticated": false,
  "message": "Logged out successfully",
  "redirectUrl": "https://localhost:4200/auth/login"
}
```

## User Profile Endpoints

### Get User Profile

**GET** `/api/user/profile`

Returns the current user's profile information (requires authentication).

#### Response
```json
{
  "success": true,
  "message": "User profile retrieved successfully",
  "data": {
    "userId": "user-123",
    "email": "user@example.com",
    "userType": "adopter",
    "createdAt": "2025-01-01T00:00:00Z",
    "lastLoginAt": "2025-07-03T12:00:00Z"
  }
}
```

### Get User ID

**GET** `/api/user/id`

Returns the current user's ID (requires authentication).

#### Response
```json
{
  "success": true,
  "data": {
    "userId": "user-123"
  }
}
```

## Authentication Flow

1. **User Requests Protected Resource**: User tries to access a protected endpoint
2. **Middleware Checks Cookies**: `AuthenticationMiddleware` validates cookies
3. **Token Validation**: JWT token is validated for format and expiration
4. **User Context**: User information is added to HTTP context
5. **Redirect if Unauthenticated**: User is redirected to Identity login page

## Configuration

### appsettings.json
```json
{
  "IdentityUrl": "https://localhost:4200",
  "AllowedOrigins": [
    "https://localhost:4200"
  ]
}
```

### Environment Variables
- `IdentityUrl`: URL of the Identity application (default: https://localhost:4200)

## Cookies Used

- **accessToken**: JWT access token (HttpOnly, Secure)
- **refreshToken**: JWT refresh token (HttpOnly, Secure)
- **userInfo**: Base64-encoded user profile JSON (not HttpOnly for client access)

## Middleware

The `AuthenticationMiddleware` automatically:
- Checks authentication for all requests except excluded paths
- Adds user context to HTTP context
- Redirects unauthenticated web requests to login
- Returns JSON error responses for API requests

### Excluded Paths
- `/health`
- `/api/authcheck/*`
- `/swagger/*`
- `/openapi/*`
- `/.well-known/*`

## Usage in Controllers

```csharp
using Longhl104.Matcher.Extensions;

[ApiController]
public class MyController : ControllerBase
{
    [HttpGet("protected")]
    public IActionResult ProtectedEndpoint()
    {
        var user = HttpContext.GetCurrentUser();
        var userId = HttpContext.GetCurrentUserId();
        var userEmail = HttpContext.GetCurrentUserEmail();
        
        // Use user information...
        return Ok(new { user = user });
    }
}
```

## Error Handling

The API returns appropriate HTTP status codes:
- **200 OK**: Authentication status returned (even if not authenticated)
- **401 Unauthorized**: For API requests when authentication is required
- **302 Found**: Redirect to login page for web requests
- **500 Internal Server Error**: For unexpected errors

## Security Considerations

- JWT tokens are validated for format and expiration
- Cookies are secured with HttpOnly, Secure, and SameSite attributes
- CORS is configured for specific origins
- User context is isolated per request
- No sensitive information is logged
