#!/bin/bash

# AWS Parameter Store Setup Script for PawfectMatch Identity Service
# This script helps you set up the required parameters in AWS Parameter Store

set -e

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
print_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check if AWS CLI is installed
if ! command -v aws &> /dev/null; then
    print_error "AWS CLI is not installed. Please install it first."
    exit 1
fi

# Check if AWS credentials are configured
if ! aws sts get-caller-identity &> /dev/null; then
    print_error "AWS credentials are not configured. Please run 'aws configure' first."
    exit 1
fi

print_info "Setting up AWS Parameter Store for PawfectMatch Identity Service..."

# Prompt for values or use defaults
read -p "Enter JWT signing key (minimum 32 characters) [default: generate random]: " JWT_KEY
if [ -z "$JWT_KEY" ]; then
    JWT_KEY=$(openssl rand -base64 32)
    print_info "Generated random JWT key: $JWT_KEY"
fi

read -p "Enter JWT issuer [default: PawfectMatch]: " JWT_ISSUER
JWT_ISSUER=${JWT_ISSUER:-PawfectMatch}

read -p "Enter JWT audience [default: PawfectMatch]: " JWT_AUDIENCE
JWT_AUDIENCE=${JWT_AUDIENCE:-PawfectMatch}

read -p "Enter Cognito User Pool ID: " USER_POOL_ID
if [ -z "$USER_POOL_ID" ]; then
    print_error "User Pool ID is required"
    exit 1
fi

read -p "Enter Adopters table name [default: PawfectMatch-Adopters]: " ADOPTERS_TABLE
ADOPTERS_TABLE=${ADOPTERS_TABLE:-PawfectMatch-Adopters}

read -p "Enter Refresh Tokens table name [default: PawfectMatch-RefreshTokens]: " REFRESH_TOKENS_TABLE
REFRESH_TOKENS_TABLE=${REFRESH_TOKENS_TABLE:-PawfectMatch-RefreshTokens}

read -p "Enter first allowed origin [default: http://localhost:4200]: " ORIGIN_1
ORIGIN_1=${ORIGIN_1:-http://localhost:4200}

read -p "Enter second allowed origin (optional): " ORIGIN_2

# Function to create parameter
create_parameter() {
    local name=$1
    local value=$2
    local type=$3
    local description=$4

    if aws ssm put-parameter \
        --name "$name" \
        --value "$value" \
        --type "$type" \
        --description "$description" \
        --overwrite &> /dev/null; then
        print_success "Created parameter: $name"
    else
        print_error "Failed to create parameter: $name"
    fi
}

print_info "Creating parameters in AWS Parameter Store..."

# JWT Configuration
create_parameter "/PawfectMatch/Identity/JWT/Key" "$JWT_KEY" "SecureString" "JWT signing key for PawfectMatch Identity service"
create_parameter "/PawfectMatch/Identity/JWT/Issuer" "$JWT_ISSUER" "String" "JWT issuer for PawfectMatch Identity service"
create_parameter "/PawfectMatch/Identity/JWT/Audience" "$JWT_AUDIENCE" "String" "JWT audience for PawfectMatch Identity service"

# AWS Configuration
create_parameter "/PawfectMatch/Identity/AWS/UserPoolId" "$USER_POOL_ID" "String" "Cognito User Pool ID for PawfectMatch"
create_parameter "/PawfectMatch/Identity/AWS/AdoptersTableName" "$ADOPTERS_TABLE" "String" "DynamoDB table name for adopters"
create_parameter "/PawfectMatch/Identity/AWS/RefreshTokensTableName" "$REFRESH_TOKENS_TABLE" "String" "DynamoDB table name for refresh tokens"

# Allowed Origins Configuration
create_parameter "/PawfectMatch/Identity/AllowedOrigins/0" "$ORIGIN_1" "String" "First allowed origin for CORS"

if [ ! -z "$ORIGIN_2" ]; then
    create_parameter "/PawfectMatch/Identity/AllowedOrigins/1" "$ORIGIN_2" "String" "Second allowed origin for CORS"
fi

print_success "Parameter Store setup completed!"
print_info "You can view all parameters with:"
print_info "aws ssm get-parameters-by-path --path '/PawfectMatch/Identity' --recursive"

print_warning "Make sure your application has the necessary IAM permissions to read these parameters."
print_info "See ParameterStore-Setup.md for required IAM permissions."
