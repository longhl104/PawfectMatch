#!/bin/bash

# Production Migration Script
# This script creates a temporary EC2 instance to run EF migrations against production database

set -e

ENVIRONMENT="production"
PROFILE="longhl104"
REGION="ap-southeast-2"

echo "🚀 Starting production migration process..."

# Set AWS profile
export AWS_PROFILE=$PROFILE

echo "📋 Getting production infrastructure details..."

# Get production VPC and subnet information
PROD_VPC_ID=$(aws ec2 describe-vpcs \
  --filters "Name=tag:Name,Values=*PawfectMatch-production*" \
  --query "Vpcs[0].VpcId" --output text)

if [ "$PROD_VPC_ID" = "None" ] || [ -z "$PROD_VPC_ID" ]; then
  echo "❌ Error: Could not find production VPC. Make sure the production stack is deployed."
  exit 1
fi

PROD_SUBNET_ID=$(aws ec2 describe-subnets \
  --filters "Name=vpc-id,Values=$PROD_VPC_ID" "Name=tag:Name,Values=*Private*" \
  --query "Subnets[0].SubnetId" --output text)

PROD_DB_SG_ID=$(aws ssm get-parameter \
  --name "/PawfectMatch/Production/Common/DatabaseSecurityGroupId" \
  --query "Parameter.Value" --output text 2>/dev/null || echo "")

if [ -z "$PROD_DB_SG_ID" ]; then
  echo "❌ Error: Could not get database security group ID. Make sure the production stack is deployed."
  exit 1
fi

echo "✅ Found production infrastructure:"
echo "   VPC ID: $PROD_VPC_ID"
echo "   Subnet ID: $PROD_SUBNET_ID"
echo "   DB Security Group: $PROD_DB_SG_ID"

echo "🔒 Creating migration security group..."

# Create security group for migration instance
MIGRATION_SG_ID=$(aws ec2 create-security-group \
  --group-name "pawfectmatch-production-migration-$(date +%s)" \
  --description "Temporary security group for production database migrations" \
  --vpc-id $PROD_VPC_ID \
  --query "GroupId" --output text)

echo "✅ Created migration security group: $MIGRATION_SG_ID"

echo "🖥️  Creating migration EC2 instance..."

# Get the latest Amazon Linux 2023 AMI
AMI_ID=$(aws ec2 describe-images \
  --owners amazon \
  --filters "Name=name,Values=al2023-ami-*-x86_64" "Name=state,Values=available" \
  --query "Images | sort_by(@, &CreationDate) | [-1].ImageId" --output text)

# Create the EC2 instance
INSTANCE_ID=$(aws ec2 run-instances \
  --image-id $AMI_ID \
  --instance-type t3.micro \
  --subnet-id $PROD_SUBNET_ID \
  --security-group-ids $MIGRATION_SG_ID \
  --iam-instance-profile Name=AmazonSSMRoleForInstancesQuickSetup \
  --tag-specifications "ResourceType=instance,Tags=[{Key=Name,Value=pawfectmatch-production-migration}]" \
  --user-data file://$(dirname "$0")/migration-userdata.sh \
  --query "Instances[0].InstanceId" --output text)

echo "✅ Created migration instance: $INSTANCE_ID"

echo "🔗 Adding database access rule..."

# Allow migration instance to connect to database
aws ec2 authorize-security-group-ingress \
  --group-id $PROD_DB_SG_ID \
  --protocol tcp \
  --port 5432 \
  --source-group $MIGRATION_SG_ID

echo "⏳ Waiting for instance to be ready..."

# Wait for instance to be running
aws ec2 wait instance-running --instance-ids $INSTANCE_ID

# Wait a bit more for SSM agent to be ready
echo "⏳ Waiting for SSM agent to be ready (60 seconds)..."
sleep 60

echo "✅ Instance is ready!"
echo ""
echo "🎯 Next steps:"
echo "1. Connect to the instance:"
echo "   aws ssm start-session --target $INSTANCE_ID --profile $PROFILE"
echo ""
echo "2. Inside the instance, run:"
echo "   cd /home/ec2-user/PawfectMatch/ShelterHub/Longhl104.ShelterHub"
echo "   dotnet ef migrations add InitialCreate"
echo "   dotnet ef database update"
echo ""
echo "3. When done, run the cleanup script:"
echo "   ./scripts/cleanup-migration-instance.sh $INSTANCE_ID $MIGRATION_SG_ID $PROD_DB_SG_ID"
echo ""
echo "📝 Instance details saved to /tmp/migration-instance.env"

# Save instance details for cleanup
cat > /tmp/migration-instance.env << EOF
INSTANCE_ID=$INSTANCE_ID
MIGRATION_SG_ID=$MIGRATION_SG_ID
PROD_DB_SG_ID=$PROD_DB_SG_ID
AWS_PROFILE=$PROFILE
EOF

echo "🎉 Migration environment ready!"
