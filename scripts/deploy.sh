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

	# Ask for confirmation
	echo
	read -p "Do you want to proceed with the deployment? (y/N): " -n 1 -r
	echo
	if [[ ! $REPLY =~ ^[Yy]$ ]]; then
		print_warning "Deployment cancelled by user"
		cd ../scripts
		exit 0
	fi

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

# Function to get RDS endpoint from CDK outputs
get_rds_endpoint() {
	print_info "Retrieving RDS endpoint from CDK outputs..."

	# Get stack outputs - use the correct stack name format from CDK
	local stack_name="PawfectMatch-Environment-${ENVIRONMENT}"
	local endpoint=$(aws cloudformation describe-stacks \
		--stack-name $stack_name \
		--profile $AWS_PROFILE \
		--query 'Stacks[0].Outputs[?OutputKey==`DatabaseEndpoint`].OutputValue' \
		--output text 2>/dev/null)

	if [ -n "$endpoint" ] && [ "$endpoint" != "None" ]; then
		echo $endpoint
	else
		print_warning "Could not retrieve RDS endpoint from CDK outputs"
		echo ""
	fi
}

# Function to install PostGIS extension
install_postgis() {
	print_info "Installing PostGIS extension..."

	local rds_endpoint=$(get_rds_endpoint)

	if [ -z "$rds_endpoint" ]; then
		print_warning "RDS endpoint not available. Skipping PostGIS installation."
		print_info "You can manually install PostGIS later using:"
		print_info "psql -h <RDS_ENDPOINT> -U <USERNAME> -d <DATABASE> -c 'CREATE EXTENSION IF NOT EXISTS postgis;'"
		return 0
	fi

	# Create PostGIS installation script
	local postgis_script="/tmp/install_postgis.sql"
	cat >$postgis_script <<EOF
-- Install PostGIS extension
CREATE EXTENSION IF NOT EXISTS postgis;
CREATE EXTENSION IF NOT EXISTS postgis_topology;
CREATE EXTENSION IF NOT EXISTS fuzzystrmatch;
CREATE EXTENSION IF NOT EXISTS postgis_tiger_geocoder;

-- Verify installation
SELECT PostGIS_version();
EOF

	# Prompt for database connection details
	echo
	print_info "PostGIS installation requires database connection details."
	read -p "Database name [pawfectmatch]: " db_name
	db_name=${db_name:-pawfectmatch}

	read -p "Database username [postgres]: " db_user
	db_user=${db_user:-postgres}

	read -s -p "Database password: " db_password
	echo

	if [ -z "$db_password" ]; then
		print_error "Database password is required"
		return 1
	fi

	# Set password for psql
	export PGPASSWORD=$db_password

	print_info "Connecting to database and installing PostGIS..."
	if psql -h $rds_endpoint -U $db_user -d $db_name -f $postgis_script; then
		print_success "PostGIS installed successfully"
	else
		print_error "Failed to install PostGIS"
		return 1
	fi

	# Clean up
	rm -f $postgis_script
	unset PGPASSWORD
}

# Function to display stack outputs
show_outputs() {
	print_info "Displaying stack outputs..."

	local stack_name="PawfectMatch-Environment-${ENVIRONMENT}"
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

		install_postgis

		echo
		print_success "Deployment completed successfully!"
	else
		print_error "Deployment failed!"
		exit 1
	fi
}

# Run main function with all arguments
main "$@"
