#!/bin/bash

set -e # Exit on any error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color
ROOT_DIR=$(dirname "$(pwd)")

echo "Root directory: $ROOT_DIR"

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
	cd "$ROOT_DIR/cdk"

	if npm run build; then
		print_success "Build completed successfully"
		# Return to scripts directory
		cd "$ROOT_DIR/scripts"
	else
		print_error "Build failed"
		cd "$ROOT_DIR/scripts"
		exit 1
	fi
}

# Function to build .NET Lambda functions
build_lambda_functions() {
	print_info "Building .NET Lambda functions..."

	# Change to root directory of the project
	cd "$ROOT_DIR"

	# Find all Lambda function directories
	local lambda_dirs=$(find . -name "*.csproj" -type f -not -path "./cdk/*" | xargs dirname | sort -u)

	if [ -z "$lambda_dirs" ]; then
		print_warning "No .NET Lambda functions found"
		cd "$ROOT_DIR/scripts"
		return 0
	fi

	# Build each Lambda function
	for lambda_dir in $lambda_dirs; do
		print_info "Building Lambda function in: $lambda_dir"

		cd "$ROOT_DIR/$lambda_dir"

		# Restore packages and build
		if dotnet restore && dotnet lambda package; then
			print_success "Built Lambda function: $lambda_dir"
		else
			print_error "Failed to build Lambda function: $lambda_dir"
			cd "$ROOT_DIR/scripts"
			exit 1
		fi
	done

	print_success "All .NET Lambda functions built successfully"
	cd "$ROOT_DIR/scripts"
}

# Function to build Angular client applications
build_angular_clients() {
	print_info "Building Angular client applications..."

	# Change to root directory of the project
	cd "$ROOT_DIR"

	# List of client directories
	local client_dirs=("Identity/client" "Matcher/client" "ShelterHub/client")

	for client_dir in "${client_dirs[@]}"; do
		# Always ensure we're in the ROOT_DIR before checking
		cd "$ROOT_DIR"

		if [ -d "$client_dir" ]; then
			print_info "Building Angular client in: $client_dir"

			cd "$ROOT_DIR/$client_dir"

			# Check if package.json exists
			if [ ! -f "package.json" ]; then
				print_warning "No package.json found in $client_dir, skipping..."
				continue
			fi

			# Install dependencies and build
			print_info "Installing dependencies for $client_dir..."
			if npm i; then
				print_success "Dependencies installed for $client_dir"
			else
				print_error "Failed to install dependencies for $client_dir"
				cd "$ROOT_DIR/scripts"
				exit 1
			fi

			print_info "Building production build for $client_dir..."
			if npm run build:prod; then
				print_success "Built Angular client: $client_dir"
			else
				print_error "Failed to build Angular client: $client_dir"
				cd "$ROOT_DIR/scripts"
				exit 1
			fi
		else
			print_warning "Client directory not found: $client_dir"
		fi
	done

	print_success "All Angular clients built successfully"
	cd "$ROOT_DIR/scripts"
}

# Function to deploy CDK stacks
deploy_cdk() {
	print_info "Starting CDK deployment for environment: $ENVIRONMENT"

	# Change to CDK directory
	cd "$ROOT_DIR/cdk"

	# Set CDK stage environment variable
	export CDK_STAGE=$ENVIRONMENT

	# Show diff first
	print_info "Showing deployment diff..."
	cdk diff --all --profile $AWS_PROFILE || true

	# Deploy all stacks
	print_info "Deploying all CDK stacks..."
	if cdk deploy --all --require-approval never --profile $AWS_PROFILE --no-notices; then
		print_success "CDK deployment completed successfully"
		cd "$ROOT_DIR/scripts"
		return 0
	else
		print_error "CDK deployment failed"
		cd "$ROOT_DIR/scripts"
		return 1
	fi
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

	# Parse command line arguments
	local environment=""
	local skip_lambda_build=false
	local skip_angular_build=false

	while [[ $# -gt 0 ]]; do
		case $1 in
		--skip-lambda-build)
			skip_lambda_build=true
			shift
			;;
		--skip-angular-build)
			skip_angular_build=true
			shift
			;;
		-*)
			print_error "Unknown option: $1"
			echo "Usage: $0 <environment> [--skip-lambda-build] [--skip-angular-build]"
			echo "Environments: dev, development, prod, production"
			echo "Options:"
			echo "  --skip-lambda-build     Skip building .NET Lambda functions"
			echo "  --skip-angular-build    Skip building Angular client applications"
			exit 1
			;;
		*)
			if [ -z "$environment" ]; then
				environment=$1
			else
				print_error "Multiple environments specified"
				echo "Usage: $0 <environment> [--skip-lambda-build] [--skip-angular-build]"
				exit 1
			fi
			shift
			;;
		esac
	done

	# Check if environment parameter is provided
	if [ -z "$environment" ]; then
		print_error "Environment parameter is required"
		echo "Usage: $0 <environment> [--skip-lambda-build] [--skip-angular-build]"
		echo "Environments: dev, development, prod, production"
		echo "Options:"
		echo "  --skip-lambda-build     Skip building .NET Lambda functions"
		echo "  --skip-angular-build    Skip building Angular client applications"
		exit 1
	fi

	# Validate inputs and check prerequisites
	validate_environment $environment
	check_prerequisites

	print_info "Deploying to environment: $ENVIRONMENT"
	print_info "Using AWS profile: $AWS_PROFILE"
	if [ "$skip_lambda_build" = true ]; then
		print_warning "Skipping Lambda function builds"
	fi
	if [ "$skip_angular_build" = true ]; then
		print_warning "Skipping Angular client builds"
	fi
	echo

	# AWS authentication
	aws_sso_login
	verify_credentials

	# Build and deploy
	build_project

	if [ "$skip_angular_build" = false ]; then
		build_angular_clients
	fi

	# if [ "$skip_lambda_build" = false ]; then
	# 	build_lambda_functions
	# fi

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
