# PostGIS Auto-Installation Custom Resource Guide

## Overview

The PostGIS installer has been enhanced with an AWS Custom Resource that automatically invokes the Lambda function during stack deployment. This ensures PostGIS is installed immediately after the database is created, without requiring manual intervention.

## How It Works

### 1. Deployment Flow
```
1. CDK deploys database infrastructure
2. CDK deploys PostGIS installer Lambda function
3. Custom Resource automatically invokes Lambda function
4. Lambda function installs PostGIS extension
5. Deployment completes with PostGIS ready for use
```

### 2. Custom Resource Configuration

The custom resource is configured to:
- **onCreate**: Install PostGIS when the stack is first deployed
- **onUpdate**: Reinstall PostGIS when the stack is updated (idempotent)
- **Dependencies**: Wait for database to be ready before executing
- **Timeout**: 5 minutes to allow for Lambda execution
- **Retries**: Automatic retries on failure

### 3. Benefits

- **Automated**: No manual steps required after deployment
- **Reliable**: Guaranteed to run after database creation
- **Idempotent**: Safe to run multiple times
- **Integrated**: Part of the CloudFormation stack lifecycle
- **Monitored**: CloudWatch logs for troubleshooting

## Deployment

### Deploy the Environment Stack

```bash
cd cdk
npm install
npx cdk deploy PawfectMatch-Environment-{stage} --profile {your-profile}
```

The PostGIS installation will happen automatically during deployment.

### Monitor Installation

#### CloudWatch Logs

Check the custom resource logs:
```bash
aws logs tail /aws/cdk/PostgisInstallerCustomResource --follow
```

Check the Lambda function logs:
```bash
aws logs tail /aws/lambda/pawfect-match-Environment-InstallPostgis-{stage} --follow
```

#### CloudFormation Events

Monitor the deployment in the AWS Console:
1. Go to CloudFormation
2. Select your stack: `PawfectMatch-Environment-{stage}`
3. Watch the Events tab for PostGIS installer progress

### Verify Installation

After deployment, verify PostGIS is installed:

```sql
-- Connect to your database and run:
SELECT name, default_version, installed_version 
FROM pg_available_extensions 
WHERE name = 'postgis';

-- Should return PostGIS version info
SELECT PostGIS_Version();
```

## Custom Resource Behavior

### On Create (First Deployment)
- Custom resource invokes Lambda function
- Lambda connects to database
- Installs PostGIS extension if not present
- Returns success/failure status

### On Update (Stack Updates)
- Custom resource re-invokes Lambda function
- Lambda checks if PostGIS is already installed
- Skips installation if already present (idempotent)
- Returns current PostGIS version

### On Delete (Stack Deletion)
- Custom resource is deleted
- PostGIS extension remains in database
- Lambda function is deleted with the stack

## Troubleshooting

### Common Issues

1. **Custom Resource Timeout**
   ```
   Error: Custom resource operation timed out
   ```
   - Check Lambda function logs for errors
   - Verify VPC configuration allows database access
   - Increase timeout if needed

2. **Lambda Function Errors**
   ```
   Error: Connection refused
   ```
   - Verify security group configuration
   - Check database is running and accessible
   - Verify Secrets Manager permissions

3. **PostGIS Installation Failed**
   ```
   Error: permission denied to create extension
   ```
   - Verify database user has SUPERUSER privileges
   - Check PostgreSQL version compatibility

### Debugging Steps

1. **Check Custom Resource Status**
   ```bash
   aws cloudformation describe-stacks \
     --stack-name PawfectMatch-Environment-{stage} \
     --query 'Stacks[0].StackStatus'
   ```

2. **View Custom Resource Logs**
   ```bash
   aws logs describe-log-groups --log-group-name-prefix /aws/cdk/
   ```

3. **Check Lambda Function**
   ```bash
   aws lambda get-function \
     --function-name pawfect-match-Environment-InstallPostgis-{stage}
   ```

4. **Manual Lambda Invoke (if needed)**
   ```bash
   aws lambda invoke \
     --function-name pawfect-match-Environment-InstallPostgis-{stage} \
     --payload '{"Stage":"{stage}"}' \
     --cli-binary-format raw-in-base64-out \
     response.json
   ```

## Configuration Options

### Custom Resource Properties

The custom resource can be customized by modifying the CDK code:

```typescript
// Increase timeout for slow networks
timeout: cdk.Duration.minutes(10),

// Change log retention
logRetention: logs.RetentionDays.ONE_MONTH,

// Add additional parameters
parameters: {
  FunctionName: this.postgisInstallerFunction.functionName,
  Payload: JSON.stringify({ 
    Stage: stage,
    RetryAttempts: 3,
    Timeout: 30
  }),
}
```

### Lambda Function Configuration

Modify the Lambda function in the CDK:

```typescript
{
  functionName: 'InstallPostgis',
  timeout: cdk.Duration.minutes(2), // Increase if needed
  memorySize: 512, // Increase for better performance
  environment: {
    STAGE: stage,
    RETRY_ATTEMPTS: '3',
    CONNECTION_TIMEOUT: '30'
  }
}
```

## Stack Outputs

After deployment, the following outputs are available:

- `PostgisInstallerFunctionArn`: Lambda function ARN
- `PostgisInstallerCustomResourceId`: Custom resource ID
- `DatabaseEndpoint`: Database connection endpoint
- `DatabaseSecretName`: Credentials secret name

## SSM Parameters

The following parameters are stored for reference:

- `/PawfectMatch/{Stage}/Lambda/PostgisInstallerArn`
- `/PawfectMatch/{Stage}/Lambda/PostgisInstallerCustomResourceId`
- `/PawfectMatch/{Stage}/Database/Host`
- `/PawfectMatch/{Stage}/Database/Port`

## Best Practices

1. **Monitor Deployments**: Always check CloudFormation events during deployment
2. **Test in Development**: Verify the process works in development before production
3. **Keep Logs**: Retain CloudWatch logs for troubleshooting
4. **Database Permissions**: Ensure database user has appropriate privileges
5. **Network Access**: Verify Lambda can reach database through security groups

## Rollback Scenarios

If the PostGIS installation fails:

1. **Stack will fail to deploy**: CloudFormation will rollback automatically
2. **Check logs**: Review Lambda and custom resource logs
3. **Fix issues**: Update configuration and redeploy
4. **Manual recovery**: If needed, manually invoke Lambda after fixing issues

The custom resource ensures PostGIS installation is part of the infrastructure deployment lifecycle, making it reliable and automated.
