# Lambda Utils Usage Guide

The `LambdaUtils` class provides reusable utilities for creating AWS Lambda functions in a consistent way across the PawfectMatch CDK stacks.

## Overview

The utility provides three main functions:
- `createFunction()` - Generic Lambda function creation
- `createDotNetFunction()` - Specialized for .NET Lambda functions with project conventions
- Helper methods for common IAM policies

## Usage Examples

### 1. Creating a .NET Lambda Function (Recommended)

```typescript
import { LambdaUtils } from './utils';

// In your stack constructor
const myLambdaFunction = LambdaUtils.createDotNetFunction(
  this,
  'MyLambdaFunction',
  stage,
  {
    functionBaseName: 'my-function', // Will become: pawfect-match-my-function-{stage}
    lambdaPath: 'Identity/Lambdas/MyFunction/MyFunction/src/MyFunction',
    handler: 'MyFunction::MyFunction.Function::FunctionHandler',
    environment: {
      TABLE_NAME: myTable.tableName,
      API_URL: 'https://api.example.com',
    },
    timeout: Duration.minutes(2),
    memorySize: 256,
    description: 'Handles my specific business logic',
    policies: [
      LambdaUtils.createCognitoPolicy(userPool.userPoolArn),
      LambdaUtils.createDynamoDBReadWritePolicy(myTable.tableArn),
    ],
  }
);
```

### 2. Creating a Generic Lambda Function

```typescript
import { LambdaUtils } from './utils';

const customLambda = LambdaUtils.createFunction(
  this,
  'CustomFunction',
  {
    functionName: `custom-lambda-${stage}`,
    handler: 'index.handler',
    codePath: path.join(__dirname, '../lambdas/custom'),
    runtime: lambda.Runtime.NODEJS_18_X,
    timeout: Duration.seconds(45),
    environment: {
      NODE_ENV: stage,
    },
    policies: [
      new iam.PolicyStatement({
        effect: iam.Effect.ALLOW,
        actions: ['s3:GetObject'],
        resources: ['arn:aws:s3:::my-bucket/*'],
      }),
    ],
  }
);
```

### 3. Using Pre-built IAM Policies

```typescript
// Cognito permissions
const cognitoPolicy = LambdaUtils.createCognitoPolicy(userPool.userPoolArn);

// DynamoDB permissions
const dynamoPolicy = LambdaUtils.createDynamoDBReadWritePolicy(table.tableArn);

// Apply to existing function
myFunction.addToRolePolicy(cognitoPolicy);
myFunction.addToRolePolicy(dynamoPolicy);
```

## Configuration Options

### LambdaFunctionConfig Interface

| Property | Type | Required | Default | Description |
|----------|------|----------|---------|-------------|
| `functionName` | string | Yes | - | AWS Lambda function name |
| `handler` | string | Yes | - | Function entry point |
| `codePath` | string | Yes | - | Path to compiled code |
| `timeout` | Duration | No | 30 seconds | Function timeout |
| `environment` | object | No | {} | Environment variables |
| `memorySize` | number | No | 128 | Memory allocation in MB |
| `runtime` | Runtime | No | DOTNET_8 | Lambda runtime |
| `description` | string | No | - | Function description |
| `policies` | PolicyStatement[] | No | [] | Additional IAM policies |

### DotNetFunction Options

| Property | Type | Required | Default | Description |
|----------|------|----------|---------|-------------|
| `functionBaseName` | string | Yes | - | Base name (prefix will be added) |
| `lambdaPath` | string | Yes | - | Relative path from project root |
| `handler` | string | Yes | - | .NET handler specification |
| `environment` | object | No | {} | Environment variables (STAGE auto-added) |
| `timeout` | Duration | No | 30 seconds | Function timeout |
| `memorySize` | number | No | 128 | Memory allocation in MB |
| `description` | string | No | - | Function description |
| `policies` | PolicyStatement[] | No | [] | Additional IAM policies |

## Best Practices

1. **Use `createDotNetFunction()` for .NET Lambdas** - It follows project conventions and handles common patterns.

2. **Group related environment variables** - Pass them as a single object for better organization.

3. **Use descriptive function base names** - They become part of the AWS resource name.

4. **Leverage pre-built policies** - Use `createCognitoPolicy()` and `createDynamoDBReadWritePolicy()` when possible.

5. **Set appropriate timeouts and memory** - Consider the function's workload when configuring resources.

## Migration from Manual Creation

### Before (Manual)
```typescript
const myFunction = new lambda.Function(this, 'MyFunction', {
  functionName: `pawfect-match-my-function-${stage}`,
  runtime: lambda.Runtime.DOTNET_8,
  handler: 'MyFunction::MyFunction.Function::FunctionHandler',
  code: lambda.Code.fromAsset(
    path.join(__dirname, '../../path/to/function/bin/Release/net8.0/publish')
  ),
  timeout: Duration.seconds(30),
  environment: {
    STAGE: stage,
    TABLE_NAME: table.tableName,
  },
});

myFunction.addToRolePolicy(
  new iam.PolicyStatement({
    effect: iam.Effect.ALLOW,
    actions: ['cognito-idp:*'],
    resources: [userPool.userPoolArn],
  })
);
```

### After (Using Utility)
```typescript
const myFunction = LambdaUtils.createDotNetFunction(
  this,
  'MyFunction',
  stage,
  {
    functionBaseName: 'my-function',
    lambdaPath: 'path/to/function',
    handler: 'MyFunction::MyFunction.Function::FunctionHandler',
    environment: {
      TABLE_NAME: table.tableName,
    },
    policies: [
      LambdaUtils.createCognitoPolicy(userPool.userPoolArn),
    ],
  }
);
```

## Adding New Utility Methods

To extend the utility with new common patterns:

```typescript
// In lambda-utils.ts
static createS3Policy(bucketArn: string): iam.PolicyStatement {
  return new iam.PolicyStatement({
    effect: iam.Effect.ALLOW,
    actions: ['s3:GetObject', 's3:PutObject', 's3:DeleteObject'],
    resources: [`${bucketArn}/*`],
  });
}
```

This approach ensures consistency, reduces boilerplate, and makes Lambda function creation more maintainable across the entire CDK project.
