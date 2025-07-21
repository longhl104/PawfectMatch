#!/bin/bash

# Test deployment validation script
set -e

ROOT_DIR=$(dirname "$(pwd)")
echo "Root directory: $ROOT_DIR"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

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

print_info "Validating PawfectMatch ECS deployment setup..."

# Check if all Dockerfiles exist
print_info "Checking Dockerfiles..."
services=("Identity" "Matcher" "ShelterHub")
for service in "${services[@]}"; do
    dockerfile_path="$ROOT_DIR/$service/Longhl104.$service/Dockerfile"
    if [ -f "$dockerfile_path" ]; then
        print_success "✓ Dockerfile found for $service"
    else
        print_error "✗ Dockerfile missing for $service at $dockerfile_path"
        exit 1
    fi
done

# Check if health endpoints exist in Program.cs files
print_info "Checking health endpoints..."
for service in "${services[@]}"; do
    program_cs_path="$ROOT_DIR/$service/Longhl104.$service/Program.cs"
    if [ -f "$program_cs_path" ]; then
        if grep -q "/health" "$program_cs_path"; then
            print_success "✓ Health endpoint found in $service"
        else
            print_warning "⚠ Health endpoint not found in $service Program.cs"
        fi
    else
        print_error "✗ Program.cs not found for $service"
    fi
done

# Check CDK compilation
print_info "Checking CDK compilation..."
cd "$ROOT_DIR/cdk"
if npm run build > /dev/null 2>&1; then
    print_success "✓ CDK project compiles successfully"
else
    print_error "✗ CDK project compilation failed"
    exit 1
fi

# Check if deploy script is executable
print_info "Checking deploy script..."
deploy_script="$ROOT_DIR/scripts/deploy.sh"
if [ -f "$deploy_script" ]; then
    if [ -x "$deploy_script" ]; then
        print_success "✓ Deploy script is executable"
    else
        print_warning "⚠ Deploy script exists but is not executable"
        chmod +x "$deploy_script"
        print_success "✓ Made deploy script executable"
    fi
else
    print_error "✗ Deploy script not found at $deploy_script"
    exit 1
fi

# Check for prerequisites
print_info "Checking prerequisites..."
prerequisites=("aws" "docker" "npm" "dotnet")
for cmd in "${prerequisites[@]}"; do
    if command -v "$cmd" >/dev/null 2>&1; then
        print_success "✓ $cmd is installed"
    else
        print_error "✗ $cmd is not installed"
        exit 1
    fi
done

# Check Docker daemon
if docker info >/dev/null 2>&1; then
    print_success "✓ Docker daemon is running"
else
    print_error "✗ Docker daemon is not running"
    exit 1
fi

echo
print_success "🎉 All validation checks passed!"
echo
print_info "To deploy the application:"
echo "  cd scripts"
echo "  ./deploy.sh development"
echo
print_info "To skip certain build steps:"
echo "  ./deploy.sh development --skip-angular-build"
echo "  ./deploy.sh development --skip-backend-build"
echo "  ./deploy.sh development --skip-lambda-build"
