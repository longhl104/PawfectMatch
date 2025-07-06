#!/bin/bash

# Publish script for @longhl104/pawfect-match-ng
# This script builds and publishes the Angular library to npm

set -e  # Exit on any error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
LIBRARY_NAME="pawfect-match-ng"
PACKAGE_NAME="@longhl104/pawfect-match-ng"
WORKSPACE_DIR="/Volumes/T7Shield/Projects/PawfectMatch/Shared/pawfect-match-angular"
DIST_DIR="$WORKSPACE_DIR/dist/longhl104/pawfect-match-ng"

# Function to print colored output
print_status() {
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

# Function to check if we're in the correct directory
check_directory() {
    print_status "Checking current directory..."
    if [ ! -f "$WORKSPACE_DIR/angular.json" ]; then
        print_error "Not in the correct Angular workspace directory!"
        print_error "Expected to find angular.json in: $WORKSPACE_DIR"
        exit 1
    fi
    print_success "Directory check passed"
}

# Function to check if user is logged into npm
check_npm_auth() {
    print_status "Checking npm authentication..."
    if ! npm whoami > /dev/null 2>&1; then
        print_error "You are not logged into npm!"
        print_status "Please run: npm login"
        exit 1
    fi
    local npm_user=$(npm whoami)
    print_success "Logged into npm as: $npm_user"
}

# Function to check for uncommitted changes
check_git_status() {
    print_status "Checking git status..."
    cd "$WORKSPACE_DIR"

    if [ -d .git ]; then
        if ! git diff-index --quiet HEAD --; then
            print_warning "You have uncommitted changes!"
            read -p "Do you want to continue anyway? (y/N): " -n 1 -r
            echo
            if [[ ! $REPLY =~ ^[Yy]$ ]]; then
                print_status "Aborting publish..."
                exit 1
            fi
        else
            print_success "Git status is clean"
        fi
    else
        print_warning "Not a git repository, skipping git checks"
    fi
}

# Function to install dependencies
install_dependencies() {
    print_status "Installing dependencies..."
    cd "$WORKSPACE_DIR"

    if [ ! -d "node_modules" ]; then
        print_status "node_modules not found, running npm install..."
        npm install
    else
        print_status "Dependencies already installed"
    fi
    print_success "Dependencies ready"
}

# Function to lint the code (if available)
run_lint() {
    print_status "Running lint checks..."
    cd "$WORKSPACE_DIR"

    # Check if lint script exists and run it
    if npm run lint --dry-run > /dev/null 2>&1; then
        print_status "Running npm run lint..."
        npm run lint || {
            print_warning "Linting issues found!"
            read -p "Do you want to continue publishing despite lint issues? (y/N): " -n 1 -r
            echo
            if [[ ! $REPLY =~ ^[Yy]$ ]]; then
                exit 1
            fi
        }
        print_success "Linting completed"
    else
        print_warning "No lint script found, skipping lint checks"
    fi
}

# Function to build the library
build_library() {
    print_status "Building the library..."
    cd "$WORKSPACE_DIR"

    # Remove existing dist directory
    if [ -d "dist" ]; then
        print_status "Removing existing dist directory..."
        rm -rf dist
    fi

    # Build the library
    print_status "Running ng build $LIBRARY_NAME..."
    ng build @longhl104/$LIBRARY_NAME --configuration production

    if [ ! -d "$DIST_DIR" ]; then
        print_error "Build failed! Dist directory not found: $DIST_DIR"
        exit 1
    fi

    print_success "Library built successfully"
}

# Function to check the built package
check_package() {
    print_status "Checking the built package..."
    cd "$DIST_DIR"

    # Verify package.json exists
    if [ ! -f "package.json" ]; then
        print_error "package.json not found in dist directory!"
        exit 1
    fi

    # Show package info
    print_status "Package information:"
    echo "$(cat package.json | grep -E '\"name\"|\"version\"')"

    # Check for required files
    local required_files=("package.json" "README.md")
    for file in "${required_files[@]}"; do
        if [ ! -f "$file" ]; then
            print_warning "Required file missing: $file"
        fi
    done

    print_success "Package check completed"
}

# Function to bump version
bump_version() {
    print_status "Current version management..."
    cd "$WORKSPACE_DIR/projects/longhl104/pawfect-match-ng"

    local current_version=$(node -p "require('./package.json').version")
    print_status "Current version: $current_version"

    echo "Select version bump type:"
    echo "1) patch (x.x.X)"
    echo "2) minor (x.X.x)"
    echo "3) major (X.x.x)"
    echo "4) custom"
    echo "5) keep current version"

    read -p "Enter choice (1-5): " -n 1 -r
    echo

    case $REPLY in
        1)
            npm version patch --no-git-tag-version
            ;;
        2)
            npm version minor --no-git-tag-version
            ;;
        3)
            npm version major --no-git-tag-version
            ;;
        4)
            read -p "Enter new version: " new_version
            npm version "$new_version" --no-git-tag-version
            ;;
        5)
            print_status "Keeping current version: $current_version"
            ;;
        *)
            print_error "Invalid choice"
            exit 1
            ;;
    esac

    local new_version=$(node -p "require('./package.json').version")
    print_success "Version set to: $new_version"
}

# Function to dry run the publish
dry_run_publish() {
    print_status "Running publish dry run..."
    cd "$DIST_DIR"

    npm publish --dry-run

    print_success "Dry run completed successfully"
    print_status "Review the output above before proceeding with actual publish"
}

# Function to publish the package
publish_package() {
    print_status "Publishing package to npm..."
    cd "$DIST_DIR"

    # Ask for confirmation
    echo
    print_warning "About to publish $PACKAGE_NAME to npm!"
    read -p "Are you sure you want to continue? (y/N): " -n 1 -r
    echo

    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        print_status "Publish cancelled"
        exit 0
    fi

    # Publish the package
    npm publish --access public

    local version=$(node -p "require('./package.json').version")
    print_success "Successfully published $PACKAGE_NAME@$version to npm!"

    # Show install command
    echo
    print_status "To install this package, run:"
    echo "npm install $PACKAGE_NAME@$version"
}

# Function to show usage
show_usage() {
    echo "Usage: $0 [options]"
    echo
    echo "Options:"
    echo "  --skip-tests    Skip running tests"
    echo "  --skip-lint     Skip running lint checks"
    echo "  --skip-build    Skip building (use existing dist)"
    echo "  --dry-run       Only run dry-run, don't actually publish"
    echo "  --help          Show this help message"
    echo
    echo "This script will:"
    echo "1. Check prerequisites (directory, npm auth, git status)"
    echo "2. Install dependencies"
    echo "3. Run tests and lint (unless skipped)"
    echo "4. Build the library"
    echo "5. Bump version (optional)"
    echo "6. Publish to npm"
}

# Parse command line arguments
SKIP_TESTS=false
SKIP_LINT=false
SKIP_BUILD=false
DRY_RUN_ONLY=false

while [[ $# -gt 0 ]]; do
    case $1 in
        --skip-tests)
            SKIP_TESTS=true
            shift
            ;;
        --skip-lint)
            SKIP_LINT=true
            shift
            ;;
        --skip-build)
            SKIP_BUILD=true
            shift
            ;;
        --dry-run)
            DRY_RUN_ONLY=true
            shift
            ;;
        --help)
            show_usage
            exit 0
            ;;
        *)
            print_error "Unknown option: $1"
            show_usage
            exit 1
            ;;
    esac
done

# Main execution
main() {
    print_status "Starting publish process for $PACKAGE_NAME"
    echo "=============================================="

    # Prerequisites
    check_directory
    check_npm_auth
    check_git_status
    install_dependencies

    # Quality checks
    if [ "$SKIP_LINT" = false ]; then
        run_lint
    else
        print_warning "Skipping lint checks"
    fi

    # Build
    if [ "$SKIP_BUILD" = false ]; then
        build_library
    else
        print_warning "Skipping build - using existing dist"
        if [ ! -d "$DIST_DIR" ]; then
            print_error "No existing build found in $DIST_DIR"
            exit 1
        fi
    fi

    # Package checks
    check_package

    # Version management
    bump_version

    # Rebuild after version bump
    if [ "$SKIP_BUILD" = false ]; then
        print_status "Rebuilding after version bump..."
        build_library
    fi

    # Dry run
    dry_run_publish

    # Publish
    if [ "$DRY_RUN_ONLY" = false ]; then
        publish_package
    else
        print_status "Dry run only - not publishing to npm"
    fi

    print_success "Publish process completed!"
}

# Run main function
main "$@"
