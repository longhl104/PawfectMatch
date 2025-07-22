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

	if ! command -v docker &>/dev/null; then
		print_error "Docker is not installed. Please install Docker."
		exit 1
	fi

	# Check if Docker daemon is running
	if ! docker info &>/dev/null; then
		print_error "Docker daemon is not running. Please start Docker."
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

# Function to build and push Docker images for backend services
build_and_push_backend_images() {
	print_info "Building and pushing Docker images for backend services..."

	# Change to root directory
	cd "$ROOT_DIR"

	# Get AWS account ID and region
	local aws_account_id=$(aws sts get-caller-identity --profile $AWS_PROFILE --query Account --output text)
	local aws_region=$(aws configure get region --profile $AWS_PROFILE)

	# Use default region if not configured
	if [ -z "$aws_region" ]; then
		aws_region="us-east-1"
	fi

	# ECR base URL
	local ecr_base_url="${aws_account_id}.dkr.ecr.${aws_region}.amazonaws.com"

	print_info "Using ECR base URL: $ecr_base_url"

	# Login to ECR
	print_info "Logging into ECR..."
	if ! aws ecr get-login-password --region $aws_region --profile $AWS_PROFILE | docker login --username AWS --password-stdin $ecr_base_url; then
		print_error "Failed to login to ECR"
		cd "$ROOT_DIR/scripts"
		exit 1
	fi

	# Backend services to build - filter based on selection
	local all_services=("Identity" "Matcher" "ShelterHub")
	local services=()

	for service in "${all_services[@]}"; do
		case $service in
			"Identity")
				[ "$deploy_identity" = true ] && services+=("$service")
				;;
			"Matcher")
				[ "$deploy_matcher" = true ] && services+=("$service")
				;;
			"ShelterHub")
				[ "$deploy_shelter_hub" = true ] && services+=("$service")
				;;
		esac
	done

	if [ ${#services[@]} -eq 0 ]; then
		print_warning "No backend services selected for deployment, skipping Docker builds..."
		cd "$ROOT_DIR/scripts"
		return 0
	fi

	print_info "Building Docker images for selected services: ${services[*]}"

	for service in "${services[@]}"; do
		print_info "Building Docker image for $service..."

		local service_dir="$service/Longhl104.$service"
		local dockerfile_path="$service_dir/Dockerfile"
		local repo_name="pawfectmatch-$(echo $service | tr '[:upper:]' '[:lower:]')-${ENVIRONMENT}"
		local image_name="$ecr_base_url/$repo_name:latest"

		# Check if Dockerfile exists
		if [ ! -f "$dockerfile_path" ]; then
			print_warning "Dockerfile not found for $service at $dockerfile_path, skipping..."
			continue
		fi

		# Build Docker image
		print_info "Building Docker image: $image_name"
		if docker build --platform linux/arm64 -t $image_name -f $dockerfile_path .; then
			print_success "Docker image built successfully for $service"
		else
			print_error "Failed to build Docker image for $service"
			cd "$ROOT_DIR/scripts"
			exit 1
		fi

		# Create ECR repository if it doesn't exist
		print_info "Ensuring ECR repository exists: $repo_name"
		if ! aws ecr describe-repositories --repository-names $repo_name --region $aws_region --profile $AWS_PROFILE &>/dev/null; then
			print_info "Creating ECR repository: $repo_name"
			if aws ecr create-repository --repository-name $repo_name --region $aws_region --profile $AWS_PROFILE &>/dev/null; then
				print_success "ECR repository created: $repo_name"
			else
				print_error "Failed to create ECR repository: $repo_name"
				cd "$ROOT_DIR/scripts"
				exit 1
			fi
		else
			print_info "ECR repository already exists: $repo_name"
		fi

		# Push Docker image
		print_info "Pushing Docker image: $image_name"
		if docker push $image_name; then
			print_success "Docker image pushed successfully for $service"
		else
			print_error "Failed to push Docker image for $service"
			cd "$ROOT_DIR/scripts"
			exit 1
		fi
	done

	print_success "All backend Docker images built and pushed successfully"
	cd "$ROOT_DIR/scripts"
}

# Function to build Angular client applications
build_angular_clients() {
	print_info "Building Angular client applications..."

	# Change to root directory of the project
	cd "$ROOT_DIR"

	# List of client directories - filter based on selection
	local all_client_dirs=("Identity/client" "Matcher/client" "ShelterHub/client")
	local client_dirs=()

	for client_dir in "${all_client_dirs[@]}"; do
		case $client_dir in
			"Identity/client")
				[ "$deploy_identity" = true ] && client_dirs+=("$client_dir")
				;;
			"Matcher/client")
				[ "$deploy_matcher" = true ] && client_dirs+=("$client_dir")
				;;
			"ShelterHub/client")
				[ "$deploy_shelter_hub" = true ] && client_dirs+=("$client_dir")
				;;
		esac
	done

	if [ ${#client_dirs[@]} -eq 0 ]; then
		print_warning "No Angular clients selected for deployment, skipping builds..."
		cd "$ROOT_DIR/scripts"
		return 0
	fi

	print_info "Building Angular clients for selected services: ${client_dirs[*]}"

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
			if npm run build:${ENVIRONMENT}; then
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

# Function to force ECS redeployment
force_ecs_redeployment() {
	print_info "Forcing ECS service redeployment..."

	# Get AWS region
	local aws_region=$(aws configure get region --profile $AWS_PROFILE)
	if [ -z "$aws_region" ]; then
		aws_region="us-east-1"
	fi

	# ECS cluster and service names based on your actual naming convention
	local cluster_name="pawfectmatch-${ENVIRONMENT}-cluster"

	print_info "Using ECS cluster: $cluster_name"

	# Define services and their actual ECS service names - filter based on selection
	local all_services=(
		"pawfectmatch-${ENVIRONMENT}-identity-service"
		"pawfectmatch-${ENVIRONMENT}-matcher-service"
		"pawfectmatch-${ENVIRONMENT}-shelter-hub-service"
	)
	local services=()

	for service in "${all_services[@]}"; do
		case $service in
			*"identity-service")
				[ "$deploy_identity" = true ] && services+=("$service")
				;;
			*"matcher-service")
				[ "$deploy_matcher" = true ] && services+=("$service")
				;;
			*"shelter-hub-service")
				[ "$deploy_shelter_hub" = true ] && services+=("$service")
				;;
		esac
	done

	if [ ${#services[@]} -eq 0 ]; then
		print_warning "No ECS services selected for redeployment, skipping..."
		return 0
	fi

	print_info "Redeploying selected ECS services: ${services[*]}"

	for service_name in "${services[@]}"; do
		print_info "Forcing redeployment of ECS service: $service_name"

		# Check if service exists by listing services and checking if our service is in the list
		local service_exists=$(aws ecs list-services \
			--cluster $cluster_name \
			--region $aws_region \
			--profile $AWS_PROFILE \
			--output text \
			--query "serviceArns[?contains(@, '$service_name')]" 2>/dev/null)

		if [ -n "$service_exists" ]; then
			# Force new deployment
			print_info "Service found, forcing redeployment..."
			if aws ecs update-service \
				--cluster $cluster_name \
				--service $service_name \
				--force-new-deployment \
				--region $aws_region \
				--profile $AWS_PROFILE \
				--no-cli-pager &>/dev/null; then
				print_success "Forced redeployment of $service_name"
			else
				print_warning "Failed to force redeployment of $service_name"
			fi
		else
			print_warning "ECS service $service_name not found in cluster $cluster_name, skipping..."
		fi
	done

	print_success "ECS redeployment commands completed"
}

# Function to show interactive menu for selecting projects to deploy
show_project_menu() {
	echo
	print_info "Select which projects to deploy (use t to toggle, enter to confirm):"
	echo

	local options=(
		"Deploy Identity service"
		"Deploy Matcher service"
		"Deploy ShelterHub service"
	)

	# Use individual variables instead of array (1 = deploy, 0 = skip)
	local proj0=1 proj1=1 proj2=1  # Default: deploy all
	local current=0

	# Handle input
	while true; do
		# Display menu inline
		clear
		echo "========================================="
		echo "   PawfectMatch Project Selection"
		echo "========================================="
		echo
		print_info "Environment: $ENVIRONMENT"
		print_info "AWS Profile: $AWS_PROFILE"
		echo
		print_info "Select which projects to deploy:"
		echo "Use ↑/↓ or k/j to navigate, T to toggle, ENTER to confirm"
		echo

		for i in "${!options[@]}"; do
			local prefix="  "
			if [ $i -eq $current ]; then
				prefix="▶ "
			fi

			local checkbox="☐"
			case $i in
				0) [ $proj0 -eq 1 ] && checkbox="☑" ;;
				1) [ $proj1 -eq 1 ] && checkbox="☑" ;;
				2) [ $proj2 -eq 1 ] && checkbox="☑" ;;
			esac

			echo -e "${prefix}${checkbox} ${options[$i]}"
		done

		echo
		echo "Press T to toggle, ENTER when done selecting projects..."

		# Read single character
		read -rsn1 key

		# Debug: show what key was pressed (remove this later)
		echo "DEBUG: Project menu key pressed: '$key' (ASCII: $(printf '%d' "'$key"))" >> /tmp/deploy_debug.log

		case $key in
			$'\x1b')  # ESC sequence
				read -rsn2 key
				case $key in
					'[A') # Up arrow
						((current > 0)) && ((current--))
						;;
					'[B') # Down arrow
						((current < ${#options[@]} - 1)) && ((current++))
						;;
				esac
				;;
			'k'|'K') # Vim-style up
				((current > 0)) && ((current--))
				;;
			'j'|'J') # Vim-style down
				((current < ${#options[@]} - 1)) && ((current++))
				;;
			't'|'T') # T to toggle
				case $current in
					0) [ $proj0 -eq 1 ] && proj0=0 || proj0=1 ;;
					1) [ $proj1 -eq 1 ] && proj1=0 || proj1=1 ;;
					2) [ $proj2 -eq 1 ] && proj2=0 || proj2=1 ;;
				esac
				;;
			$'\n'|$'\r'|'') # Enter to confirm - try multiple formats
				break
				;;
			*) # Debug: catch all other keys
				echo "DEBUG: Project menu unhandled key: '$key' (ASCII: $(printf '%d' "'$key"))" >> /tmp/deploy_debug.log
				;;
		esac
	done

	# Set project flags based on selections
	deploy_identity=$( [ $proj0 -eq 1 ] && echo true || echo false )
	deploy_matcher=$( [ $proj1 -eq 1 ] && echo true || echo false )
	deploy_shelter_hub=$( [ $proj2 -eq 1 ] && echo true || echo false )

	clear
	echo "========================================="
	echo "   Selected Projects"
	echo "========================================="
	if [ "$deploy_identity" = true ]; then
		print_success "Will deploy Identity service"
	else
		print_warning "Will skip Identity service"
	fi
	if [ "$deploy_matcher" = true ]; then
		print_success "Will deploy Matcher service"
	else
		print_warning "Will skip Matcher service"
	fi
	if [ "$deploy_shelter_hub" = true ]; then
		print_success "Will deploy ShelterHub service"
	else
		print_warning "Will skip ShelterHub service"
	fi

	if [ "$deploy_identity" = false ] && [ "$deploy_matcher" = false ] && [ "$deploy_shelter_hub" = false ]; then
		print_error "No projects selected for deployment!"
		exit 1
	fi

	echo
}

# Function to show interactive menu for selecting skip options
show_skip_menu() {
	echo
	print_info "Select which build steps to skip (use t to toggle, enter to confirm):"
	echo

	local options=(
		"Skip Lambda build"
		"Skip Angular build"
		"Skip backend Docker build"
		"Skip ECS redeployment"
	)

	# Use individual variables instead of array
	local sel0=0 sel1=0 sel2=0 sel3=0
	local current=0

	# Handle input
	while true; do
		# Display menu inline
		clear
		echo "========================================="
		echo "   PawfectMatch Deployment Options"
		echo "========================================="
		echo
		print_info "Environment: $ENVIRONMENT"
		print_info "AWS Profile: $AWS_PROFILE"
		echo
		print_info "Select which build steps to skip:"
		echo "Use ↑/↓ or k/j to navigate, T to toggle, ENTER to confirm"
		echo

		for i in "${!options[@]}"; do
			local prefix="  "
			if [ $i -eq $current ]; then
				prefix="▶ "
			fi

			local checkbox="☐"
			case $i in
				0) [ $sel0 -eq 1 ] && checkbox="☑" ;;
				1) [ $sel1 -eq 1 ] && checkbox="☑" ;;
				2) [ $sel2 -eq 1 ] && checkbox="☑" ;;
				3) [ $sel3 -eq 1 ] && checkbox="☑" ;;
			esac

			echo -e "${prefix}${checkbox} ${options[$i]}"
		done

		echo
		echo "Press T to toggle, ENTER when done selecting options..."

		# Read single character
		read -rsn1 key

		# Debug: show what key was pressed (remove this later)
		echo "DEBUG: Key pressed: '$key' (ASCII: $(printf '%d' "'$key"))" >> /tmp/deploy_debug.log

		case $key in
			$'\x1b')  # ESC sequence
				read -rsn2 key
				case $key in
					'[A') # Up arrow
						((current > 0)) && ((current--))
						;;
					'[B') # Down arrow
						((current < ${#options[@]} - 1)) && ((current++))
						;;
				esac
				;;
			'k'|'K') # Vim-style up
				((current > 0)) && ((current--))
				;;
			'j'|'J') # Vim-style down
				((current < ${#options[@]} - 1)) && ((current++))
				;;
			't'|'T') # T to toggle
				case $current in
					0) [ $sel0 -eq 1 ] && sel0=0 || sel0=1 ;;
					1) [ $sel1 -eq 1 ] && sel1=0 || sel1=1 ;;
					2) [ $sel2 -eq 1 ] && sel2=0 || sel2=1 ;;
					3) [ $sel3 -eq 1 ] && sel3=0 || sel3=1 ;;
				esac
				;;
			$'\n'|$'\r'|'') # Enter to confirm - try multiple formats
				break
				;;
			*) # Debug: catch all other keys
				echo "DEBUG: Unhandled key: '$key' (ASCII: $(printf '%d' "'$key"))" >> /tmp/deploy_debug.log
				;;
		esac
	done

	# Set skip flags based on selections
	skip_lambda_build=$( [ $sel0 -eq 1 ] && echo true || echo false )
	skip_angular_build=$( [ $sel1 -eq 1 ] && echo true || echo false )
	skip_backend_build=$( [ $sel2 -eq 1 ] && echo true || echo false )
	skip_ecs_redeploy=$( [ $sel3 -eq 1 ] && echo true || echo false )

	clear
	echo "========================================="
	echo "   Selected Options"
	echo "========================================="
	if [ "$skip_lambda_build" = true ]; then
		print_warning "Will skip Lambda function builds"
	fi
	if [ "$skip_angular_build" = true ]; then
		print_warning "Will skip Angular client builds"
	fi
	if [ "$skip_backend_build" = true ]; then
		print_warning "Will skip backend Docker image builds"
	fi
	if [ "$skip_ecs_redeploy" = true ]; then
		print_warning "Will skip ECS service redeployment"
	fi

	if [ "$skip_lambda_build" = false ] && [ "$skip_angular_build" = false ] && [ "$skip_backend_build" = false ] && [ "$skip_ecs_redeploy" = false ]; then
		print_info "Will run all build steps"
	fi

	echo
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
	local interactive_mode=true
	local skip_lambda_build=false
	local skip_angular_build=false
	local skip_backend_build=false
	local skip_ecs_redeploy=false

	# Project deployment flags
	local deploy_identity=true
	local deploy_matcher=true
	local deploy_shelter_hub=true

	while [[ $# -gt 0 ]]; do
		case $1 in
		--skip-lambda-build)
			skip_lambda_build=true
			interactive_mode=false
			shift
			;;
		--skip-angular-build)
			skip_angular_build=true
			interactive_mode=false
			shift
			;;
		--skip-backend-build)
			skip_backend_build=true
			interactive_mode=false
			shift
			;;
		--skip-ecs-redeploy)
			skip_ecs_redeploy=true
			interactive_mode=false
			shift
			;;
		--non-interactive)
			interactive_mode=false
			shift
			;;
		-*)
			print_error "Unknown option: $1"
			echo "Usage: $0 <environment> [OPTIONS]"
			echo "Environments: dev, development, prod, production"
			echo "Options:"
			echo "  --skip-lambda-build     Skip building .NET Lambda functions"
			echo "  --skip-angular-build    Skip building Angular client applications"
			echo "  --skip-backend-build    Skip building and pushing backend Docker images"
			echo "  --skip-ecs-redeploy     Skip forcing ECS service redeployment"
			echo "  --non-interactive       Skip the interactive menu (use with skip flags)"
			exit 1
			;;
		*)
			if [ -z "$environment" ]; then
				environment=$1
			else
				print_error "Multiple environments specified"
				echo "Usage: $0 <environment> [OPTIONS]"
				exit 1
			fi
			shift
			;;
		esac
	done

	# Check if environment parameter is provided
	if [ -z "$environment" ]; then
		print_error "Environment parameter is required"
		echo "Usage: $0 <environment> [OPTIONS]"
		echo "Environments: dev, development, prod, production"
		echo "Options:"
		echo "  --skip-lambda-build     Skip building .NET Lambda functions"
		echo "  --skip-angular-build    Skip building Angular client applications"
		echo "  --skip-backend-build    Skip building and pushing backend Docker images"
		echo "  --skip-ecs-redeploy     Skip forcing ECS service redeployment"
		echo "  --non-interactive       Skip the interactive menu (use with skip flags)"
		exit 1
	fi

	# Validate inputs and check prerequisites
	validate_environment $environment
	check_prerequisites

	# Show interactive menus if no skip flags were provided
	if [ "$interactive_mode" = true ]; then
		show_project_menu
		show_skip_menu
	else
		print_info "Deploying to environment: $ENVIRONMENT"
		print_info "Using AWS profile: $AWS_PROFILE"
		if [ "$skip_lambda_build" = true ]; then
			print_warning "Skipping Lambda function builds"
		fi
		if [ "$skip_angular_build" = true ]; then
			print_warning "Skipping Angular client builds"
		fi
		if [ "$skip_backend_build" = true ]; then
			print_warning "Skipping backend Docker image builds"
		fi
		if [ "$skip_ecs_redeploy" = true ]; then
			print_warning "Skipping ECS service redeployment"
		fi
		echo
	fi

	# AWS authentication
	aws_sso_login
	verify_credentials

	# Build and deploy
	build_project

	if [ "$skip_angular_build" = false ]; then
		build_angular_clients
	fi

	if [ "$skip_backend_build" = false ]; then
		build_and_push_backend_images
	fi

	# if [ "$skip_lambda_build" = false ]; then
	# 	build_lambda_functions
	# fi

	if deploy_cdk; then
		print_success "CDK deployment completed successfully!"

		# Show stack outputs
		show_outputs

		# Force ECS redeployment if not skipped
		if [ "$skip_ecs_redeploy" = false ]; then
			force_ecs_redeployment
		fi

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
