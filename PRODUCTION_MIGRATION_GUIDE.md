# Production Database Migration Guide

## Overview

Since production databases are deployed in private subnets for security, direct connections from local machines are not possible. This guide shows how to run EF migrations against production databases using secure methods.

## Method 1: Using AWS Systems Manager Session Manager (Recommended)

### Step 1: Create a temporary EC2 instance for migrations

Create a temporary EC2 instance in the production VPC that can connect to the database:

```bash
# Set AWS profile for production
export AWS_PROFILE=longhl104

# Get production VPC ID and subnet ID
PROD_VPC_ID=$(aws ec2 describe-vpcs --filters "Name=tag:Name,Values=*PawfectMatch-production*" --query "Vpcs[0].VpcId" --output text)
PROD_SUBNET_ID=$(aws ec2 describe-subnets --filters "Name=vpc-id,Values=$PROD_VPC_ID" "Name=tag:Name,Values=*Private*" --query "Subnets[0].SubnetId" --output text)
PROD_SG_ID=$(aws ssm get-parameter --name "/PawfectMatch/Production/Common/DatabaseSecurityGroupId" --query "Parameter.Value" --output text)

# Create security group for migration instance
MIGRATION_SG_ID=$(aws ec2 create-security-group \
  --group-name pawfectmatch-production-migration-sg \
  --description "Temporary security group for production database migrations" \
  --vpc-id $PROD_VPC_ID \
  --query "GroupId" --output text)

# Allow SSH access via Session Manager (no inbound rules needed)
# Session Manager works without SSH ports open

# Create the EC2 instance
INSTANCE_ID=$(aws ec2 run-instances \
  --image-id ami-0c02fb55956c7d316 \
  --instance-type t3.micro \
  --subnet-id $PROD_SUBNET_ID \
  --security-group-ids $MIGRATION_SG_ID \
  --iam-instance-profile Name=AmazonSSMRoleForInstancesQuickSetup \
  --tag-specifications 'ResourceType=instance,Tags=[{Key=Name,Value=pawfectmatch-production-migration}]' \
  --query "Instances[0].InstanceId" --output text)

echo "Created instance: $INSTANCE_ID"
```

### Step 2: Allow migration instance to connect to database

```bash
# Add migration instance security group to database security group
aws ec2 authorize-security-group-ingress \
  --group-id $PROD_SG_ID \
  --protocol tcp \
  --port 5432 \
  --source-group $MIGRATION_SG_ID
```

### Step 3: Wait for instance to be ready and connect

```bash
# Wait for instance to be running
aws ec2 wait instance-running --instance-ids $INSTANCE_ID

# Connect using Session Manager
aws ssm start-session --target $INSTANCE_ID
```

### Step 4: Install .NET and tools on the migration instance

```bash
# Inside the EC2 instance session:

# Update the system
sudo yum update -y

# Install .NET 9
sudo rpm --import https://packages.microsoft.com/keys/microsoft.asc
sudo wget -O /etc/yum.repos.d/microsoft-prod.repo https://packages.microsoft.com/config/rhel/9/prod.repo
sudo yum install -y dotnet-sdk-9.0

# Install git
sudo yum install -y git

# Install EF Core tools
dotnet tool install --global dotnet-ef

# Add dotnet tools to PATH
echo 'export PATH="$PATH:/home/ec2-user/.dotnet/tools"' >> ~/.bashrc
source ~/.bashrc
```

### Step 5: Clone your repository and run migrations

```bash
# Clone the repository
git clone https://github.com/longhl104/PawfectMatch.git
cd PawfectMatch/ShelterHub/Longhl104.ShelterHub

# Configure AWS credentials (if needed)
aws configure set region ap-southeast-2
# The instance should have IAM role permissions already

# Build the project
dotnet restore
dotnet build

# Run the migration
dotnet ef migrations add InitialCreate --verbose

# Apply migrations to production database
dotnet ef database update --verbose
```

### Step 6: Clean up

```bash
# Exit the session
exit

# Back on your local machine:
# Terminate the migration instance
aws ec2 terminate-instances --instance-ids $INSTANCE_ID

# Remove the security group rule
aws ec2 revoke-security-group-ingress \
  --group-id $PROD_SG_ID \
  --protocol tcp \
  --port 5432 \
  --source-group $MIGRATION_SG_ID

# Wait for instance termination, then delete security group
aws ec2 wait instance-terminated --instance-ids $INSTANCE_ID
aws ec2 delete-security-group --group-id $MIGRATION_SG_ID
```

## Method 2: Using ECS Task (Alternative)

### Create an ECS task specifically for migrations:

```typescript
// Add to your CDK stack for production migrations
const migrationTask = new ecs.FargateTaskDefinition(this, 'MigrationTask', {
  memoryLimitMiB: 512,
  cpu: 256,
  executionRole: this.taskExecutionRole,
  taskRole: this.taskRole,
});

migrationTask.addContainer('migration-container', {
  image: ecs.ContainerImage.fromRegistry('mcr.microsoft.com/dotnet/sdk:9.0'),
  command: ['/bin/bash', '-c', 'echo "Ready for migration commands"'],
  logging: ecs.LogDrivers.awsLogs({
    streamPrefix: 'migration',
    logGroup: this.logGroup,
  }),
});
```

### Run migration using ECS task:

```bash
# Run the migration task
aws ecs run-task \
  --cluster pawfectmatch-production-cluster \
  --task-definition migration-task \
  --network-configuration "awsvpcConfiguration={subnets=[$PROD_SUBNET_ID],securityGroups=[$MIGRATION_SG_ID]}" \
  --launch-type FARGATE

# Then exec into the running task to run migrations
```

## Method 3: Automated Migration via Lambda (Recommended for CI/CD)

Create a Lambda function that runs migrations automatically:

```typescript
// In your CDK stack
const migrationFunction = new lambda.Function(this, 'DatabaseMigration', {
  runtime: lambda.Runtime.DOTNET_8,
  handler: 'Migration::Migration.Function::FunctionHandler',
  code: lambda.Code.fromAsset('path/to/migration/lambda'),
  vpc: this.vpc,
  vpcSubnets: {
    subnetType: ec2.SubnetType.PRIVATE_WITH_EGRESS,
  },
  securityGroups: [migrationSecurityGroup],
  timeout: cdk.Duration.minutes(15),
});
```

## Security Considerations

1. **Temporary Access**: All migration methods use temporary access that can be revoked
2. **No Public Access**: Production databases remain in private subnets
3. **IAM Roles**: Use IAM roles instead of hardcoded credentials
4. **Audit Trail**: All actions are logged in CloudTrail
5. **Minimal Permissions**: Grant only necessary permissions for migrations

## Quick Commands Summary

```bash
# Set production environment
export AWS_PROFILE=longhl104
export ENVIRONMENT=production

# Create migration instance (automated script)
./scripts/create-migration-instance.sh production

# Connect and run migrations
aws ssm start-session --target $INSTANCE_ID
# ... run migration commands inside the instance

# Clean up
./scripts/cleanup-migration-instance.sh production
```

## Troubleshooting

### Common Issues:

1. **IAM Permissions**: Ensure the EC2 instance has SSM permissions
2. **Network Access**: Verify security group rules allow database access
3. **EF Tools**: Make sure dotnet-ef tools are installed and in PATH
4. **AWS Credentials**: Instance should use IAM role, not access keys

### Verification:

```bash
# Test database connection
telnet $DB_ENDPOINT 5432

# Check EF tools
dotnet ef --version

# Verify AWS access
aws sts get-caller-identity
```

This approach ensures secure, auditable database migrations for production while maintaining the security of your private database infrastructure.
