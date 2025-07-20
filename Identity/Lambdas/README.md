# PawfectMatch Login API

This module provides JWT-based authentication for the PawfectMatch application using AWS Cognito as the identity provider and .NET Lambda functions.

## Overview

The login system consists of two main endpoints:

1. **POST /identity/users/login** - Authenticate users and return JWT tokens
2. **POST /identity/users/refresh-token** - Refresh access tokens using refresh tokens

## Architecture

- **AWS Cognito User Pool**: Handles user authentication and user data storage
- **DynamoDB Tables**:
  - `Adopters Table`: Stores adopter-specific profile information
  - `Refresh Tokens Table`: Stores refresh tokens with expiration tracking
- **.NET Lambda Functions**:
  - `Login`: Handles user authentication
  - `RefreshToken`: Handles token refresh

## API Endpoints

### Login Endpoint

**POST** `/identity/users/login`

#### Request Body
```json
{
  "email": "user@example.com",
  "password": "userPassword123!"
}
```

#### Response (Success - 200)
```json
{
  "success": true,
  "message": "Login successful",
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "base64encodedrefreshtoken...",
    "expiresAt": "2025-06-28T15:30:00Z",
    "user": {
      "userId": "user123",
      "email": "user@example.com",
      "userType": "adopter",
      "phoneNumber": "+1234567890",
      "createdAt": "2025-01-01T00:00:00Z",
      "lastLoginAt": "2025-06-28T14:30:00Z"
    }
  }
}
```

#### Response (Error - 401)
```json
{
  "success": false,
  "message": "Invalid email or password",
  "data": null
}
```

### Refresh Token Endpoint

**POST** `/identity/users/refresh-token`

#### Request Body
```json
{
  "refreshToken": "base64encodedrefreshtoken..."
}
```

#### Response (Success - 200)
```json
{
  "success": true,
  "message": "Token refreshed successfully",
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "newbase64encodedrefreshtoken...",
    "expiresAt": "2025-06-28T16:30:00Z",
    "user": {
      "userId": "user123",
      "email": "user@example.com",
      "userType": "adopter",
      "phoneNumber": "+1234567890",
      "createdAt": "2025-01-01T00:00:00Z",
      "lastLoginAt": "2025-06-28T15:30:00Z"
    }
  }
}
```

## JWT Token Structure

### Access Token Claims
- **sub** (Subject): User ID
- **email**: User's email address
- **user_type**: Type of user (e.g., "adopter", "shelter")
- **phone_number**: User's phone number (if available)
- **jti** (JWT ID): Unique token identifier
- **iat** (Issued At): Token issuance timestamp
- **exp** (Expiration): Token expiration timestamp
- **iss** (Issuer): "PawfectMatch"
- **aud** (Audience): "PawfectMatch-App"

### Token Expiration
- **Access Token**: 1 hour (3600 seconds)
- **Refresh Token**: 30 days (2,592,000 seconds)

## Security Features

1. **JWT Tokens**: Stateless authentication with signed tokens
2. **Refresh Token Rotation**: New refresh token issued on each refresh
3. **Token Expiration**: Automatic token cleanup via DynamoDB TTL
4. **Secure Storage**: Refresh tokens stored in DynamoDB with encryption at rest
5. **CORS Support**: Proper CORS headers for web applications

## Environment Variables

### Required for Login Lambda
- `USER_POOL_ID`: AWS Cognito User Pool ID
- `USER_POOL_CLIENT_ID`: AWS Cognito User Pool Client ID
- `ADOPTERS_TABLE_NAME`: DynamoDB table name for adopters
- `REFRESH_TOKENS_TABLE_NAME`: DynamoDB table name for refresh tokens
- `STAGE`: Deployment stage (dev, staging, production)
- `JWT_SECRET`: Secret key for JWT signing
- `JWT_EXPIRES_IN`: Access token expiration in seconds (default: 3600)
- `REFRESH_TOKEN_EXPIRES_IN`: Refresh token expiration in seconds (default: 2592000)

## Error Codes

| Status Code | Description |
|-------------|-------------|
| 200 | Success |
| 400 | Bad Request (invalid input, malformed JSON) |
| 401 | Unauthorized (invalid credentials, expired token) |
| 404 | Not Found (user not found) |
| 405 | Method Not Allowed |
| 500 | Internal Server Error |

## Usage Examples

### Login Flow
```bash
# 1. Login with email and password
curl -X POST https://api.pawfectmatchnow.com/identity/users/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "adopter@example.com",
    "password": "SecurePassword123!"
  }'

# 2. Use the access token for authenticated requests
curl -X GET https://api.pawfectmatchnow.com/protected-endpoint \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."

# 3. Refresh the token when it expires
curl -X POST https://api.pawfectmatchnow.com/identity/users/refresh-token \
  -H "Content-Type: application/json" \
  -d '{
    "refreshToken": "base64encodedrefreshtoken..."
  }'
```

### JavaScript Client Example
```javascript
class AuthService {
  async login(email, password) {
    const response = await fetch('/identity/users/login', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({ email, password })
    });
    
    const result = await response.json();
    
    if (result.success) {
      localStorage.setItem('accessToken', result.data.accessToken);
      localStorage.setItem('refreshToken', result.data.refreshToken);
      return result.data;
    }
    
    throw new Error(result.message);
  }
  
  async refreshToken() {
    const refreshToken = localStorage.getItem('refreshToken');
    
    const response = await fetch('/identity/users/refresh-token', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({ refreshToken })
    });
    
    const result = await response.json();
    
    if (result.success) {
      localStorage.setItem('accessToken', result.data.accessToken);
      localStorage.setItem('refreshToken', result.data.refreshToken);
      return result.data;
    }
    
    throw new Error(result.message);
  }
}
```

## Deployment

1. Deploy the CDK stack which creates the infrastructure
2. The Lambda functions are automatically built and deployed
3. API Gateway endpoints are configured with proper CORS

## Testing

Run the unit tests:

```bash
cd Identity/Lambdas/Login/test/Login.Tests
dotnet test

cd ../../../RefreshToken/test/RefreshToken.Tests
dotnet test
```

## Security Considerations

1. **JWT Secret**: Use AWS Systems Manager Parameter Store for production secrets
2. **HTTPS Only**: All API calls must use HTTPS in production
3. **Token Storage**: Store tokens securely on the client side
4. **Token Validation**: Always validate JWT tokens on protected endpoints
5. **Refresh Token Security**: Treat refresh tokens as sensitive credentials
