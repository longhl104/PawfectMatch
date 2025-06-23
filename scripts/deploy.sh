#!/bin/bash

set -e # Exit on any error

# Colors for output
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

# Function to check if required tools are installed
check_prerequisites() {
	print_info "Checking prerequisites..."

	if ! command -v aws &>/dev/null; then
		print_error "AWS CLI is not installed. Please install AWS CLI v2."
		exit 1
	fi

	if ! command -v npm &>/dev/null; then
		print_error "npm is not installed. Please install Node.js and npm."
		exit 1
	fi

	if ! command -v psql &>/dev/null; then
		print_error "psql is not installed. Please install PostgreSQL client."
		exit 1
	fi

	print_success "All prerequisites are installed."
}

# Function to validate environment parameter
validate_environment() {
	local env=$1
	case $env in
	dev | development)
		export ENVIRONMENT="development"
		export AWS_PROFILE="longhl104"
		;;
	prod | production)
		export ENVIRONMENT="production"
		export AWS_PROFILE="longhl104_production"
		;;
	*)
		print_error "Invalid environment: $env"
		print_info "Valid environments: dev, development, prod, production"
		exit 1
		;;
	esac
}

# Function to perform AWS SSO login
aws_sso_login() {
	print_info "Checking AWS SSO login status for profile: $AWS_PROFILE"

	# Check if already logged in
	if aws sts get-caller-identity --profile $AWS_PROFILE &>/dev/null; then
		print_success "Already logged in to AWS SSO"
		return 0
	fi

	print_info "Performing AWS SSO login..."
	if aws sso login --profile $AWS_PROFILE; then
		print_success "AWS SSO login successful"
	else
		print_error "AWS SSO login failed"
		exit 1
	fi
}

# Function to verify AWS credentials
verify_credentials() {
	print_info "Verifying AWS credentials..."

	local identity=$(aws sts get-caller-identity --profile $AWS_PROFILE --output text --query 'Account')
	if [ $? -eq 0 ]; then
		print_success "AWS credentials verified. Account: $identity"
	else
		print_error "Failed to verify AWS credentials"
		exit 1
	fi
}

# Function to build the CDK project
build_project() {
	print_info "Building CDK project..."

	# Change to CDK directory
	cd ../cdk

	if npm run build; then
		print_success "Build completed successfully"
		# Return to original directory
		cd ../scripts
	else
		print_error "Build failed"
		cd ../scripts
		exit 1
	fi
}

# Function to deploy CDK stacks
deploy_cdk() {
	print_info "Starting CDK deployment for environment: $ENVIRONMENT"

	# Change to CDK directory
	cd ../cdk

	# Set CDK stage environment variable
	export CDK_STAGE=$ENVIRONMENT

	# Show diff first
	print_info "Showing deployment diff..."
	cdk diff --all --profile $AWS_PROFILE || true

	# Deploy all stacks
	print_info "Deploying all CDK stacks..."
	if cdk deploy --all --require-approval never --profile $AWS_PROFILE; then
		print_success "CDK deployment completed successfully"
		cd ../scripts
		return 0
	else
		print_error "CDK deployment failed"
		cd ../scripts
		return 1
	fi
}

# Function to get database connection details from CDK outputs
get_database_details() {
	print_info "Retrieving database connection details from CDK outputs..."

	local stack_name="pawfectmatch-environment-${ENVIRONMENT}"

	# Get database endpoint
	local endpoint=$(aws cloudformation describe-stacks \
		--stack-name $stack_name \
		--profile $AWS_PROFILE \
		--query 'Stacks[0].Outputs[?OutputKey==`DatabaseEndpoint`].OutputValue' \
		--output text 2>/dev/null)

	# Get database name
	local db_name=$(aws cloudformation describe-stacks \
		--stack-name $stack_name \
		--profile $AWS_PROFILE \
		--query 'Stacks[0].Outputs[?OutputKey==`DatabaseName`].OutputValue' \
		--output text 2>/dev/null)

	# Get database username
	local db_user=$(aws cloudformation describe-stacks \
		--stack-name $stack_name \
		--profile $AWS_PROFILE \
		--query 'Stacks[0].Outputs[?OutputKey==`DatabaseUsername`].OutputValue' \
		--output text 2>/dev/null)

	# Get database password from AWS Secrets Manager
	local secret_name="pawfectmatch-db-credentials-${ENVIRONMENT}"

	local db_password=""
	if [ -n "$secret_name" ] && [ "$secret_name" != "None" ]; then
		db_password=$(aws secretsmanager get-secret-value \
			--secret-id $secret_name \
			--profile $AWS_PROFILE \
			--query 'SecretString' \
			--output text 2>/dev/null | jq -r '.password' 2>/dev/null)
	fi

	# Set defaults if values are empty or "None"
	endpoint=${endpoint:-""}
	db_name=${db_name:-"pawfectmatch"}
	db_user=${db_user:-"dbadmin"}

	if [ -z "$endpoint" ] || [ "$endpoint" = "None" ]; then
		print_error "Could not retrieve database endpoint from CDK outputs"
		return 1
	fi

	if [ -z "$db_password" ] || [ "$db_password" = "None" ]; then
		print_error "Could not retrieve database password from AWS Secrets Manager"
		return 1
	fi

	# Export variables for use in other functions
	export DB_ENDPOINT=$endpoint
	export DB_NAME=$db_name
	export DB_USER=$db_user
	export DB_PASSWORD=$db_password

	print_success "Database connection details retrieved successfully"
	print_info "Endpoint: $DB_ENDPOINT"
	print_info "Database: $DB_NAME"
	print_info "Username: $DB_USER"

	return 0
}

# Function to install PostGIS extension
install_postgis() {
	print_info "Installing PostGIS extension..."

	# Get database connection details automatically
	if ! get_database_details; then
		print_warning "Could not retrieve database connection details. Skipping PostGIS installation."
		print_info "You can manually install PostGIS later using:"
		print_info "psql -h <RDS_ENDPOINT> -U <USERNAME> -d <DATABASE> -c 'CREATE EXTENSION IF NOT EXISTS postgis;'"
		return 0
	fi

	# Create PostGIS installation script
	local postgis_script="./install_postgis.sql"

	# Check if the PostGIS installation script exists
	if [ ! -f "$postgis_script" ]; then
		print_error "PostGIS installation script not found: $postgis_script"
		print_info "Please create the file with the necessary PostGIS installation commands."
		return 1
	fi

	# Set password for psql
	export PGPASSWORD=$DB_PASSWORD

	print_info "Connecting to database and installing PostGIS..."
	if psql -h $DB_ENDPOINT -U $DB_USER -d $DB_NAME -f $postgis_script; then
		print_success "PostGIS installed successfully"
	else
		print_error "Failed to install PostGIS"
		return 1
	fi

	# Clean up
	rm -f $postgis_script
	unset PGPASSWORD DB_ENDPOINT DB_NAME DB_USER DB_PASSWORD
}

# Function to display stack outputs
show_outputs() {
	print_info "Displaying stack outputs..."

	local stack_name="pawfectmatch-environment-${ENVIRONMENT}"
	aws cloudformation describe-stacks \
		--stack-name $stack_name \
		--profile $AWS_PROFILE \
		--query 'Stacks[0].Outputs[*].[OutputKey,OutputValue]' \
		--output table 2>/dev/null || print_warning "Could not retrieve stack outputs"
}

# Main execution function
main() {
	echo "========================================="
	echo "   PawfectMatch Deployment Script"
	echo "========================================="
	echo

	# Check if environment parameter is provided
	if [ $# -eq 0 ]; then
		print_error "Environment parameter is required"
		echo "Usage: $0 <environment>"
		echo "Environments: dev, development, prod, production"
		exit 1
	fi

	local environment=$1

	# Validate inputs and check prerequisites
	validate_environment $environment
	check_prerequisites

	print_info "Deploying to environment: $ENVIRONMENT"
	print_info "Using AWS profile: $AWS_PROFILE"
	echo

	# AWS authentication
	aws_sso_login
	verify_credentials

	# Build and deploy
	build_project

	if deploy_cdk; then
		print_success "CDK deployment completed successfully!"

		# Show stack outputs
		show_outputs

		# Install PostGIS
		echo

		echo
		print_success "Deployment completed successfully!"
	else
		print_error "Deployment failed!"
		exit 1
	fi
}

# Run main function with all arguments
main "$@"
