#!/bin/bash

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

# Set up environment variables
export ENVIRONMENT="development"
export AWS_PROFILE="longhl104"

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

	# Map service names to their actual ECS service names
	declare -A service_map=(
		["identity"]="pawfectmatch-${ENVIRONMENT}-identity-service"
		["matcher"]="pawfectmatch-${ENVIRONMENT}-matcher-service"
		["shelterhub"]="pawfectmatch-${ENVIRONMENT}-shelter-hub-service"
	)

	print_info "Using ECS cluster: $cluster_name"

	for service_key in "${!service_map[@]}"; do
		local service_name="${service_map[$service_key]}"

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

# Test the force_ecs_redeployment function
echo "Testing ECS redeployment function..."
force_ecs_redeployment
