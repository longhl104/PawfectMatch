# AWS Parameter Store Configuration Setup

This document explains how to configure AWS Parameter Store for the PawfectMatch Identity service.

## Parameter Store Structure

The application expects parameters to be stored in AWS Systems Manager Parameter Store with the following hierarchy:

```
/PawfectMatch/Identity/
├── JWT/
│   ├── Key          (SecureString) - JWT signing key (at least 32 characters)
│   ├── Issuer       (String) - JWT issuer (e.g., "PawfectMatch")
│   └── Audience     (String) - JWT audience (e.g., "PawfectMatch")
├── AWS/
│   ├── UserPoolId                (String) - Cognito User Pool ID
│   ├── AdoptersTableName         (String) - DynamoDB table name for adopters
│   └── RefreshTokensTableName    (String) - DynamoDB table name for refresh tokens
└── AllowedOrigins/
    ├── 0            (String) - First allowed origin (e.g., "http://localhost:4200")
    └── 1            (String) - Second allowed origin (if needed)
```

## Setting Up Parameters

### Using AWS CLI

```bash
# JWT Configuration
aws ssm put-parameter \
    --name "/PawfectMatch/Identity/JWT/Key" \
    --value "Your-Super-Secret-JWT-Key-That-Should-Be-At-Least-32-Characters-Long" \
    --type "SecureString" \
    --description "JWT signing key for PawfectMatch Identity service"

aws ssm put-parameter \
    --name "/PawfectMatch/Identity/JWT/Issuer" \
    --value "PawfectMatch" \
    --type "String" \
    --description "JWT issuer for PawfectMatch Identity service"

aws ssm put-parameter \
    --name "/PawfectMatch/Identity/JWT/Audience" \
    --value "PawfectMatch" \
    --type "String" \
    --description "JWT audience for PawfectMatch Identity service"

# AWS Configuration
aws ssm put-parameter \
    --name "/PawfectMatch/Identity/AWS/UserPoolId" \
    --value "us-east-1_XXXXXXXXX" \
    --type "String" \
    --description "Cognito User Pool ID for PawfectMatch"

aws ssm put-parameter \
    --name "/PawfectMatch/Identity/AWS/AdoptersTableName" \
    --value "PawfectMatch-Adopters" \
    --type "String" \
    --description "DynamoDB table name for adopters"

aws ssm put-parameter \
    --name "/PawfectMatch/Identity/AWS/RefreshTokensTableName" \
    --value "PawfectMatch-RefreshTokens" \
    --type "String" \
    --description "DynamoDB table name for refresh tokens"

# Allowed Origins Configuration
aws ssm put-parameter \
    --name "/PawfectMatch/Identity/AllowedOrigins/0" \
    --value "http://localhost:4200" \
    --type "String" \
    --description "First allowed origin for CORS"

# Add more origins if needed
aws ssm put-parameter \
    --name "/PawfectMatch/Identity/AllowedOrigins/1" \
    --value "https://yourproductiondomain.com" \
    --type "String" \
    --description "Production allowed origin for CORS"
```

### Using AWS Console

1. Navigate to AWS Systems Manager → Parameter Store
2. Create parameters with the paths shown above
3. For sensitive values like JWT keys, use `SecureString` type
4. For non-sensitive values, use `String` type

## Environment-Specific Configuration

The application loads Parameter Store configuration only in non-development environments. For development, it continues to use the local `appsettings.json` and `appsettings.Development.json` files.

## IAM Permissions

The application/EC2 instance/Lambda function needs the following IAM permissions:

```json
{
    "Version": "2012-10-17",
    "Statement": [
        {
            "Effect": "Allow",
            "Action": [
                "ssm:GetParametersByPath",
                "ssm:GetParameter",
                "ssm:GetParameters"
            ],
            "Resource": [
                "arn:aws:ssm:*:*:parameter/PawfectMatch/Identity/*"
            ]
        }
    ]
}
```

## Migration from appsettings.json

To migrate your existing configuration:

1. Copy values from your current `appsettings.json`
2. Create corresponding parameters in Parameter Store
3. Update the parameter hierarchy as shown above
4. Test in a non-development environment

## Configuration Precedence

The configuration follows this precedence (highest to lowest):
1. Environment variables
2. AWS Parameter Store (non-development environments only)
3. appsettings.{Environment}.json
4. appsettings.json

## Troubleshooting

- Ensure AWS credentials are properly configured
- Check IAM permissions for Parameter Store access
- Verify parameter paths match exactly (case-sensitive)
- Check AWS region configuration
- Review application logs for Parameter Store loading errors
