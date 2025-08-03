#!/bin/bash

# Development Migration Script
# This script runs EF migrations against the development PostgreSQL database
# Since the development RDS instance is public, we can connect directly

set -e

ENVIRONMENT="development"
PROFILE="longhl104"
REGION="ap-southeast-2"

echo "🚀 Starting development migration process..."

# Set AWS profile
export AWS_PROFILE=$PROFILE

echo "📋 Getting development database connection details..."

# Get database connection information from AWS Systems Manager
DB_HOST=$(aws ssm get-parameter \
    --name "/PawfectMatch/Development/Common/Database/Host" \
--query "Parameter.Value" --output text 2>/dev/null || echo "")

DB_PORT=$(aws ssm get-parameter \
    --name "/PawfectMatch/Development/Common/Database/Port" \
--query "Parameter.Value" --output text 2>/dev/null || echo "5432")

DB_NAME=$(aws ssm get-parameter \
    --name "/PawfectMatch/Development/Common/Database/Name" \
--query "Parameter.Value" --output text 2>/dev/null || echo "")

if [ -z "$DB_HOST" ] || [ -z "$DB_NAME" ]; then
    echo "❌ Error: Could not get database connection details from Systems Manager."
    echo "   Make sure the development stack is deployed and parameters are set:"
    echo "   - /PawfectMatch/Development/Common/Database/Host"
    echo "   - /PawfectMatch/Development/Common/Database/Name"
    exit 1
fi

echo "✅ Found development database:"
echo "   Host: $DB_HOST"
echo "   Port: $DB_PORT"
echo "   Database: $DB_NAME"

echo "🔐 Getting database credentials from Secrets Manager..."

# Get database credentials from AWS Secrets Manager
SECRET_VALUE=$(aws secretsmanager get-secret-value \
    --secret-id "pawfectmatch-development-db-credentials" \
--query "SecretString" --output text 2>/dev/null || echo "")

if [ -z "$SECRET_VALUE" ]; then
    echo "❌ Error: Could not get database credentials from Secrets Manager."
    echo "   Make sure the secret 'pawfectmatch-development-db-credentials' exists."
    exit 1
fi

# Parse JSON credentials (assuming they're in format: {"username":"...","password":"..."})
DB_USERNAME=$(echo $SECRET_VALUE | jq -r '.username // .Username // .user')
DB_PASSWORD=$(echo $SECRET_VALUE | jq -r '.password // .Password')

echo "🔍 Debug: DB_USERNAME='$DB_USERNAME'"
echo "🔍 Debug: DB_PASSWORD='$DB_PASSWORD'"

if [ "$DB_USERNAME" = "null" ] || [ "$DB_PASSWORD" = "null" ] || [ -z "$DB_USERNAME" ] || [ -z "$DB_PASSWORD" ]; then
    echo "❌ Error: Could not parse database credentials from secret."
    echo "   Expected JSON format: {\"username\":\"...\",\"password\":\"...\"}"
    echo "🔍 Debug: Raw secret value: $SECRET_VALUE"
    exit 1
fi

echo "✅ Retrieved database credentials"

echo "🔗 Testing database connection..."

# Build connection string
CONNECTION_STRING="Host=$DB_HOST;Port=$DB_PORT;Database=$DB_NAME;Username=$DB_USERNAME;Password=$DB_PASSWORD;SSL Mode=Require;"

# Test connection using psql if available
if command -v psql &> /dev/null; then
    echo "   Testing with psql..."
    PGPASSWORD="$DB_PASSWORD" psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USERNAME" -d "$DB_NAME" -c "SELECT version();" > /dev/null
    echo "✅ Database connection successful"
else
    echo "   ⚠️  psql not found, skipping connection test"
fi

echo "🏗️  Running Entity Framework migrations..."

# Navigate to the ShelterHub project directory
SCRIPT_DIR=$(dirname "$0")
PROJECT_DIR="$SCRIPT_DIR/../ShelterHub/Longhl104.ShelterHub"

if [ ! -d "$PROJECT_DIR" ]; then
    echo "❌ Error: ShelterHub project directory not found at $PROJECT_DIR"
    exit 1
fi

cd "$PROJECT_DIR"

# Set environment variables for EF Core
export ConnectionStrings__DefaultConnection="$CONNECTION_STRING"
export ASPNETCORE_ENVIRONMENT="Development"

echo "📁 Current directory: $(pwd)"

# Check if we need to add an initial migration
if [ ! -d "Migrations" ] || [ -z "$(ls -A Migrations 2>/dev/null)" ]; then
    echo "📝 Creating initial migration..."
    dotnet ef migrations add InitialCreate
else
    echo "📝 Migrations directory exists, checking for new changes..."

    # Check if there are pending model changes
    if dotnet ef migrations has-pending-model-changes 2>/dev/null; then
        echo "📝 Found pending model changes, creating new migration..."

        # Generate migration name based on current timestamp
        MIGRATION_NAME="Update_$(date +%Y%m%d_%H%M%S)"
        dotnet ef migrations add "$MIGRATION_NAME"
    else
        echo "✅ No pending model changes found"
    fi
fi

echo "🚀 Applying migrations to database..."
dotnet ef database update

echo "✅ Database migrations completed successfully!"

echo ""
echo "🎯 Migration Summary:"
echo "   Environment: $ENVIRONMENT"
echo "   Database: $DB_HOST:$DB_PORT/$DB_NAME"
echo "   Migrations applied to development database"
echo ""

# Optionally show migration history
echo "📋 Current migration history:"
dotnet ef migrations list

echo ""
echo "🎉 Development migration process completed!"
