# Verify Code Lambda Function

This Lambda function handles email verification code confirmation for the PawfectMatch application.

## Functionality

1. **Verifies email confirmation codes** using AWS Cognito
2. **Updates DynamoDB record** to set `IsVerified = true` and `VerifiedAt` timestamp
3. **Returns appropriate success/error responses**

## API Endpoint

**POST** `/identity/adopters/verify-code`

### Request Body

```json
{
  "email": "user@example.com",
  "code": "123456"
}
```

### Success Response (200)

```json
{
  "success": true,
  "message": "Email verification successful",
  "userId": "cognito-user-sub-id"
}
```

### Error Responses

#### 400 Bad Request

```json
{
  "error": "Email and verification code are required",
  "statusCode": 400
}
```

```json
{
  "error": "Invalid verification code",
  "statusCode": 400
}
```

```json
{
  "error": "Verification code has expired",
  "statusCode": 400
}
```

#### 404 Not Found

```json
{
  "error": "User not found",
  "statusCode": 404
}
```

```json
{
  "error": "User record not found",
  "statusCode": 404
}
```

#### 500 Internal Server Error

```json
{
  "error": "Failed to verify code",
  "statusCode": 500
}
```

## Environment Variables

- `USER_POOL_ID`: AWS Cognito User Pool ID
- `USER_POOL_CLIENT_ID`: AWS Cognito User Pool Client ID
- `ADOPTERS_TABLE_NAME`: DynamoDB table name for adopters
- `STAGE`: Deployment stage (development, staging, production)

## DynamoDB Updates

When verification is successful, the function updates the adopter record with:

- `IsVerified`: Set to `true`
- `VerifiedAt`: ISO 8601 timestamp of verification

## Error Handling

The function handles various error scenarios:

- **Cognito Errors**: Invalid codes, expired codes, user not found
- **DynamoDB Errors**: Record not found, update failures
- **General Errors**: Malformed requests, unexpected exceptions

## CORS Support

The API includes CORS headers to allow cross-origin requests from the frontend application.

## Testing

Run tests using:

```bash
cd test/VerifyCode.Tests
dotnet test
```

## Deployment

This function is deployed as part of the Identity stack in the CDK infrastructure.
