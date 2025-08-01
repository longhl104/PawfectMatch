#!/bin/bash

# Cleanup script for production migration instance

set -e

# Load saved environment variables
if [ -f /tmp/migration-instance.env ]; then
  source /tmp/migration-instance.env
fi

# Override with command line arguments if provided
INSTANCE_ID=${1:-$INSTANCE_ID}
MIGRATION_SG_ID=${2:-$MIGRATION_SG_ID}
PROD_DB_SG_ID=${3:-$PROD_DB_SG_ID}
AWS_PROFILE=${AWS_PROFILE:-longhl104}

if [ -z "$INSTANCE_ID" ] || [ -z "$MIGRATION_SG_ID" ] || [ -z "$PROD_DB_SG_ID" ]; then
  echo "❌ Error: Missing required parameters"
  echo "Usage: $0 [INSTANCE_ID] [MIGRATION_SG_ID] [PROD_DB_SG_ID]"
  echo "Or ensure /tmp/migration-instance.env exists with the required variables"
  exit 1
fi

export AWS_PROFILE

echo "🧹 Cleaning up production migration resources..."
echo "Instance ID: $INSTANCE_ID"
echo "Migration SG: $MIGRATION_SG_ID"
echo "Database SG: $PROD_DB_SG_ID"

echo "🔒 Removing database access rule..."
aws ec2 revoke-security-group-ingress \
  --group-id $PROD_DB_SG_ID \
  --protocol tcp \
  --port 5432 \
  --source-group $MIGRATION_SG_ID || echo "⚠️  Rule may already be removed"

echo "🛑 Terminating migration instance..."
aws ec2 terminate-instances --instance-ids $INSTANCE_ID

echo "⏳ Waiting for instance termination..."
aws ec2 wait instance-terminated --instance-ids $INSTANCE_ID

echo "🗑️  Deleting migration security group..."
aws ec2 delete-security-group --group-id $MIGRATION_SG_ID

echo "🧹 Removing temporary files..."
rm -f /tmp/migration-instance.env

echo "✅ Cleanup complete!"
echo "🔒 Production database access has been revoked"
echo "💾 All temporary resources have been removed"
