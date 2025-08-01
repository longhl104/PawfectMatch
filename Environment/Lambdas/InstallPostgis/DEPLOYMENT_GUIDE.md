# PostGIS Lambda Function Deployment Guide

This guide covers the deployment and usage of the PostGIS installer Lambda function.

## Overview

The PostGIS installer Lambda function automatically installs the PostGIS extension in your PostgreSQL database. It's designed to be invoked after the database is created and before applications that require geospatial functionality are deployed.

## Prerequisites

1. **PostgreSQL Database**: The target database must be running PostgreSQL 12+ with PostGIS packages available
2. **AWS Infrastructure**: VPC, subnets, security groups, and database credentials in Secrets Manager
3. **Lambda Build**: The Lambda function must be built and published (see build-and-publish.sh)

## Deployment Process

### 1. Deploy Infrastructure

The Lambda function is automatically deployed as part of the Environment stack:

```bash
cd cdk
npm install
npx cdk deploy PawfectMatch-Environment-{stage} --profile {your-profile}
```

### 2. Build and Deploy Lambda Code

```bash
cd Environment/Lambdas/InstallPostgis
./build-and-publish.sh
```

### 3. Update Lambda Function

After building, update the deployed Lambda function:

```bash
# Package the function
cd src/InstallPostgis/bin/Release/net9.0/publish
zip -r ../../../../../InstallPostgis.zip .

# Update the function
aws lambda update-function-code \
  --function-name pawfect-match-Environment-InstallPostgis-{stage} \
  --zip-file fileb://InstallPostgis.zip \
  --region {your-region}
```

## Usage

### Manual Invocation

Use the provided script to manually invoke the function:

```bash
./invoke-postgis-installer.sh development us-east-1
```

### Programmatic Invocation

You can invoke the Lambda function from other AWS services or applications:

```bash
aws lambda invoke \
  --function-name pawfect-match-Environment-InstallPostgis-development \
  --payload '{"Stage":"development"}' \
  --cli-binary-format raw-in-base64-out \
  response.json
```

### CDK Custom Resource (Recommended)

For automated installation during stack deployment, create a custom resource:

```typescript
import * as custom from 'aws-cdk-lib/custom-resources';

// Create custom resource to invoke PostGIS installer
const postgisInstaller = new custom.AwsCustomResource(this, 'PostgresPostGISInstaller', {
  onCreate: {
    service: 'Lambda',
    action: 'invoke',
    parameters: {
      FunctionName: this.postgisInstallerFunction.functionName,
      Payload: JSON.stringify({ Stage: stage }),
    },
    physicalResourceId: custom.PhysicalResourceId.of('postgis-installer-' + stage)
  },
  policy: custom.AwsCustomResourcePolicy.fromSdkCalls({
    resources: [this.postgisInstallerFunction.functionArn]
  })
});

// Ensure the custom resource runs after the database is ready
postgisInstaller.node.addDependency(this.database);
```

## Configuration

### Environment Variables

The Lambda function accepts the following environment variables:

- `STAGE`: The deployment stage (automatically set by CDK)

### Input Parameters

```json
{
  "Stage": "development"
}
```

### Output Format

Success response:
```json
{
  "Success": true,
  "Message": "PostGIS extension installed successfully",
  "ExtensionVersion": "3.4.0"
}
```

Error response:
```json
{
  "Success": false,
  "Message": "Failed to install PostGIS extension: Connection timeout",
  "ExtensionVersion": null
}
```

## Network Configuration

The Lambda function is deployed in a VPC with the following configuration:

- **Subnets**: Private subnets with internet access (PRIVATE_WITH_EGRESS)
- **Security Group**: Allows outbound traffic and access to the database
- **Database Access**: Configured to connect to PostgreSQL on port 5432

## Security

### IAM Permissions

The Lambda function has the following permissions:

- `secretsmanager:GetSecretValue` - To retrieve database credentials
- `secretsmanager:DescribeSecret` - To describe the secret
- VPC permissions for network interface management

### Network Security

- Function runs in private subnets
- Security group allows outbound HTTPS (443) for AWS API calls
- Database security group allows inbound connections from Lambda security group

## Troubleshooting

### Common Issues

1. **Timeout Errors**
   - Increase Lambda timeout (currently set to 1 minute)
   - Check VPC configuration and NAT Gateway

2. **Connection Refused**
   - Verify security group rules
   - Ensure Lambda and database are in the same VPC
   - Check database is running and accessible

3. **Permission Denied**
   - Verify IAM permissions for Secrets Manager
   - Check database user has CREATE EXTENSION privileges

4. **Extension Already Exists**
   - This is normal behavior; the function is idempotent
   - Check the response message for confirmation

### Monitoring

- **CloudWatch Logs**: Check `/aws/lambda/pawfect-match-Environment-InstallPostgis-{stage}`
- **CloudWatch Metrics**: Monitor function duration, errors, and invocations
- **X-Ray Tracing**: Enable for detailed execution tracing (optional)

### Debugging

To debug issues, check the CloudWatch logs:

```bash
aws logs tail /aws/lambda/pawfect-match-Environment-InstallPostgis-development --follow
```

## Database Verification

After installation, verify PostGIS is installed:

```sql
-- Connect to your database and run:
SELECT name, default_version, installed_version 
FROM pg_available_extensions 
WHERE name = 'postgis';

-- Check PostGIS version
SELECT PostGIS_Version();

-- List all spatial reference systems (should return results)
SELECT COUNT(*) FROM spatial_ref_sys;
```

## Integration with Applications

Once PostGIS is installed, your applications can use geospatial features:

```sql
-- Create a table with geometry column
CREATE TABLE locations (
  id SERIAL PRIMARY KEY,
  name VARCHAR(100),
  point GEOMETRY(POINT, 4326)
);

-- Insert a location
INSERT INTO locations (name, point) 
VALUES ('Shelter A', ST_GeomFromText('POINT(-74.006 40.7128)', 4326));

-- Find locations within a radius
SELECT name 
FROM locations 
WHERE ST_DWithin(point, ST_GeomFromText('POINT(-74.006 40.7128)', 4326), 1000);
```

## Cleanup

To remove the Lambda function:

```bash
aws lambda delete-function --function-name pawfect-match-Environment-InstallPostgis-development
```

Note: The function will be automatically removed when the CDK stack is destroyed.
