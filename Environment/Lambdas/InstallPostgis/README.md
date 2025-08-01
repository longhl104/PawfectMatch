# InstallPostgis Lambda Function

This AWS Lambda function installs the PostGIS extension in a PostgreSQL database. PostGIS is a spatial database extender for PostgreSQL that adds support for geographic objects and spatial queries.

## Overview

The function connects to a PostgreSQL database using credentials stored in AWS Secrets Manager and installs the PostGIS extension if it's not already present.

## Features

- **Secure Database Access**: Uses AWS Secrets Manager to retrieve database credentials
- **Idempotent**: Checks if PostGIS is already installed before attempting installation
- **Error Handling**: Comprehensive error handling with detailed logging
- **Version Reporting**: Returns the installed PostGIS version

## Prerequisites

1. PostgreSQL database with PostGIS packages available
2. Database credentials stored in AWS Secrets Manager
3. Lambda function must have appropriate IAM permissions:
   - `secretsmanager:GetSecretValue` for database credentials
   - Network access to the PostgreSQL database

## Input

```json
{
  "Stage": "development"
}
```

- `Stage`: The deployment stage (e.g., "development", "production")

## Output

```json
{
  "Success": true,
  "Message": "PostGIS extension installed successfully",
  "ExtensionVersion": "3.4.0"
}
```

- `Success`: Boolean indicating if the operation was successful
- `Message`: Descriptive message about the operation result
- `ExtensionVersion`: The version of PostGIS installed (if successful)

## Database Secret Format

The function expects the database secret in AWS Secrets Manager to have the following format:

```json
{
  "engine": "postgres",
  "host": "database-host",
  "username": "dbadmin",
  "password": "database-password",
  "dbname": "pawfectmatch",
  "port": 5432
}
```

## Building and Deployment

### Local Development

1. Install the .NET 9 SDK
2. Build the project:
   ```bash
   dotnet build
   ```
3. Run tests:
   ```bash
   dotnet test
   ```

### Deployment

The function is designed to be deployed as part of the PawfectMatch CDK infrastructure. It will be created using the `LambdaUtils.createFunction` method in the CDK stack.

### Manual Deployment (if needed)

1. Publish the function:
   ```bash
   dotnet publish -c Release
   ```
2. Package for Lambda:
   ```bash
   cd src/InstallPostgis/bin/Release/net9.0/publish
   zip -r ../InstallPostgis.zip .
   ```

## Usage in CDK

To use this Lambda function in your CDK stack:

```typescript
import { LambdaUtils } from '../utils/lambda-utils';

const postgisInstaller = LambdaUtils.createFunction(
  this,
  'PostgisInstaller',
  'Environment',
  stage,
  {
    functionName: 'InstallPostgis',
    description: 'Installs PostGIS extension in PostgreSQL database',
    timeout: Duration.minutes(1),
    memorySize: 256,
    environment: {
      // Add any environment variables if needed
    }
  }
);

// Grant permissions to access Secrets Manager
postgisInstaller.addToRolePolicy(new iam.PolicyStatement({
  effect: iam.Effect.ALLOW,
  actions: [
    'secretsmanager:GetSecretValue',
    'secretsmanager:DescribeSecret'
  ],
  resources: [
    `arn:aws:secretsmanager:${this.region}:${this.account}:secret:pawfectmatch-${stage}-db-credentials-*`
  ]
}));
```

## PostGIS Extension

PostGIS adds the following capabilities to PostgreSQL:

- **Geometry Types**: Points, lines, polygons, and complex geometries
- **Spatial Functions**: Distance calculations, area calculations, intersections, etc.
- **Spatial Indexing**: R-tree indexes for efficient spatial queries
- **Coordinate System Support**: Support for various coordinate reference systems

This is essential for the PawfectMatch application if it needs to handle location-based features such as:
- Shelter locations
- User proximity searches
- Geographic matching algorithms

## Security Considerations

- The function uses SSL/TLS connections to the database
- Database credentials are never logged or exposed
- The function follows the principle of least privilege for IAM permissions
- Network access should be restricted to the VPC where the database resides

## Troubleshooting

1. **Connection Timeouts**: Ensure the Lambda function is in the same VPC as the database
2. **Permission Errors**: Verify IAM permissions for Secrets Manager access
3. **Extension Installation Fails**: Check if the PostgreSQL instance has PostGIS packages installed
4. **Secret Not Found**: Verify the secret name matches the expected pattern

## Dependencies

- **Amazon.Lambda.Core**: AWS Lambda runtime support
- **Amazon.Lambda.Serialization.SystemTextJson**: JSON serialization
- **AWSSDK.SecretsManager**: AWS Secrets Manager client
- **Npgsql**: PostgreSQL .NET driver
- **AWS.Logger.Core**: AWS CloudWatch logging
