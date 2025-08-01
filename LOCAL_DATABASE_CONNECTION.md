# Local Database Connection Guide

## Development Environment Database Access

When deploying the `development` stage, the PostgreSQL database is configured to allow connections from your local machine for easier development and debugging.

## Configuration Changes Made

### 1. **Security Group Rules**

- **Development**: Allows connections from any IP address (0.0.0.0/0) on port 5432
- **Production**: Only allows connections from within the VPC

### 2. **Subnet Configuration**

- **Development**: Database deployed in public subnets for internet accessibility
- **Production**: Database deployed in private isolated subnets for security

### 3. **Public Accessibility**

- **Development**: `publiclyAccessible: true`
- **Production**: `publiclyAccessible: false` (default)

## How to Connect from Local Machine

### Step 1: Get Database Connection Information

After deploying the development stack, get the database endpoint and credentials:

```bash
# Get database endpoint
aws ssm get-parameter --name "/PawfectMatch/Development/Common/Database/Host" --query "Parameter.Value" --output text --profile longhl104

# Get database port
aws ssm get-parameter --name "/PawfectMatch/Development/Common/Database/Port" --query "Parameter.Value" --output text --profile longhl104

# Get database name
aws ssm get-parameter --name "/PawfectMatch/Development/Common/Database/Name" --query "Parameter.Value" --output text --profile longhl104

# Get secret ARN for credentials
aws ssm get-parameter --name "/PawfectMatch/Development/Common/Database/SecretArn" --query "Parameter.Value" --output text --profile longhl104
```

### Step 2: Get Database Credentials

```bash
# Get the secret ARN from step 1, then:
aws secretsmanager get-secret-value --secret-id "SECRET_ARN_FROM_STEP_1" --query "SecretString" --output text --profile longhl104
```

This will return JSON like:

```json
{
  "engine": "postgres",
  "host": "your-db-endpoint.rds.amazonaws.com",
  "username": "dbadmin",
  "password": "your-generated-password",
  "dbname": "pawfectmatch",
  "port": 5432
}
```

### Step 3: Connect Using Your Preferred Tool

#### Using psql Command Line

```bash
psql -h your-db-endpoint.rds.amazonaws.com -p 5432 -U dbadmin -d pawfectmatch
# Enter password when prompted
```

#### Using pgAdmin

1. Create new server connection
2. **Host**: Database endpoint from Step 1
3. **Port**: 5432
4. **Database**: pawfectmatch
5. **Username**: dbadmin
6. **Password**: From Step 2

#### Using VS Code PostgreSQL Extension

1. Install "PostgreSQL" extension
2. Add new connection with details from above

#### Using Connection String

```
postgresql://dbadmin:PASSWORD@ENDPOINT:5432/pawfectmatch
```

### Step 4: Run Migrations from Local Machine

With the database accessible, you can run EF migrations directly:

```bash
# Navigate to ShelterHub project
cd /Volumes/T7Shield/Projects/PawfectMatch/ShelterHub/Longhl104.ShelterHub

# Set AWS profile
export AWS_PROFILE=longhl104

# Run migrations against the development database
dotnet ef database update
```

## Security Considerations

⚠️ **Important Security Notes:**

1. **Development Only**: This configuration is **only enabled for development** stage
2. **Production Safety**: Production databases remain in private subnets with no public access
3. **Credentials**: Always use AWS Secrets Manager to retrieve credentials
4. **Network**: Consider using VPN or IP whitelisting for additional security even in development

## Troubleshooting

### Common Issues

1. **Connection Timeout**

   - Ensure the development stack is deployed
   - Check that security group allows your IP (0.0.0.0/0 in development)

2. **Authentication Failed**

   - Verify credentials from AWS Secrets Manager
   - Ensure you're using the correct username/password

3. **Database Not Found**
   - Confirm database name is "pawfectmatch"
   - Check that PostGIS installer ran successfully

### Verification Commands

```bash
# Test network connectivity
telnet your-db-endpoint.rds.amazonaws.com 5432

# Check security group rules
aws ec2 describe-security-groups --group-ids sg-xxxxxxxxx --profile longhl104
```

## Migration Workflow

1. **Develop Locally**: Create and test migrations using the development database
2. **Test Migration**: Run `dotnet ef database update` against development
3. **Deploy**: Let the application apply migrations automatically in production via the shared database extension

This setup provides the best of both worlds: easy local development access while maintaining production security.
