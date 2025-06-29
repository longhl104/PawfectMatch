# Longhl104.PawfectMatch

A shared library containing common services and models for the PawfectMatch application.

## Features

- **CognitoService**: AWS Cognito user authentication and profile management
- **JwtService**: JWT token generation and validation
- **RefreshTokenService**: Refresh token management with DynamoDB storage
- **Common Models**: Shared data models across the application

## Installation

```bash
dotnet add package Longhl104.PawfectMatch
```

## Usage

### CognitoService

```csharp
using Longhl104.PawfectMatch.Services;
using Longhl104.PawfectMatch.Models;

// Initialize the service (reads from environment variables)
var cognitoService = new CognitoService();

// Authenticate user
var (success, message, user) = await cognitoService.AuthenticateUserAsync("user@example.com", "password");

// Get user profile
var userProfile = await cognitoService.GetUserProfileAsync("user@example.com");
```

### JwtService

```csharp
using Longhl104.PawfectMatch.Services;
using Longhl104.PawfectMatch.Models;

// Initialize the service
var jwtService = new JwtService();

// Generate access token
var userProfile = new UserProfile { /* ... */ };
var accessToken = jwtService.GenerateAccessToken(userProfile);

// Generate refresh token
var refreshToken = jwtService.GenerateRefreshToken();

// Validate token
var claims = jwtService.ValidateToken(accessToken);
```

### RefreshTokenService

```csharp
using Longhl104.PawfectMatch.Services;

// Initialize the service
var refreshTokenService = new RefreshTokenService();

// Store refresh token
await refreshTokenService.StoreRefreshTokenAsync("userId", "refreshToken", DateTime.UtcNow.AddDays(30));

// Validate refresh token
var isValid = await refreshTokenService.ValidateRefreshTokenAsync("userId", "refreshToken");

// Revoke refresh token
await refreshTokenService.RevokeRefreshTokenAsync("userId", "refreshToken");
```

## Environment Variables

The following environment variables are required:

- `USER_POOL_ID`: AWS Cognito User Pool ID
- `USER_POOL_CLIENT_ID`: AWS Cognito User Pool Client ID
- `JWT_SECRET`: Secret key for JWT token signing
- `JWT_EXPIRES_IN`: JWT token expiry in minutes (default: 60)
- `REFRESH_TOKEN_EXPIRES_IN`: Refresh token expiry in days (default: 30)
- `STAGE`: Environment stage (dev, prod, etc.) for DynamoDB table naming

## Dependencies

- AWSSDK.CognitoIdentityProvider
- AWSSDK.DynamoDBv2
- Microsoft.IdentityModel.Tokens
- System.IdentityModel.Tokens.Jwt

## License

MIT
