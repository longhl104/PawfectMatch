#!/bin/bash

# PawfectMatch CDK Deployment Script
# This script helps deploy the PawfectMatch application with Route 53 configuration

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Default values
STAGE=""
PROFILE=""
REGION="ap-southeast-2" # Default AWS region
ACCOUNT=""

usage() {
    echo "Usage: $0 [OPTIONS]"
    echo "Options:"
    echo "  -s, --stage STAGE        Deployment stage (development|production)"
    echo "  -p, --profile PROFILE    AWS CLI profile name"
    echo "  -r, --region REGION      AWS region (default: ap-southeast-2)"
    echo "  -h, --help               Display this help message"
    echo ""
    echo "Example:"
    echo "  $0 --stage development --profile dev"
    echo "  $0 --stage production --profile prod"
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -s|--stage)
            STAGE="$2"
            shift 2
            ;;
        -p|--profile)
            PROFILE="$2"
            shift 2
            ;;
        -r|--region)
            REGION="$2"
            shift 2
            ;;
        -h|--help)
            usage
            exit 0
            ;;
        *)
            echo -e "${RED}Unknown option: $1${NC}"
            usage
            exit 1
            ;;
    esac
done

# Validate required parameters
if [ -z "$STAGE" ]; then
    echo -e "${RED}Error: Stage is required${NC}"
    usage
    exit 1
fi

if [ -z "$PROFILE" ]; then
    echo -e "${RED}Error: AWS profile is required${NC}"
    usage
    exit 1
fi

if [[ "$STAGE" != "development" && "$STAGE" != "production" ]]; then
    echo -e "${RED}Error: Stage must be 'development' or 'production'${NC}"
    exit 1
fi

echo -e "${GREEN}=== PawfectMatch CDK Deployment ===${NC}"
echo -e "${YELLOW}Stage:${NC} $STAGE"
echo -e "${YELLOW}Profile:${NC} $PROFILE"
echo -e "${YELLOW}Region:${NC} $REGION"

# Get AWS account ID from the profile
echo -e "${GREEN}Getting AWS account information from profile...${NC}"
ACCOUNT=$(aws sts get-caller-identity --profile "$PROFILE" --query Account --output text 2>/dev/null)

if [ -z "$ACCOUNT" ] || [ "$ACCOUNT" = "None" ]; then
    echo -e "${RED}Error: Unable to get AWS account ID from profile '$PROFILE'${NC}"
    echo -e "${YELLOW}Please ensure:${NC}"
    echo "1. AWS profile '$PROFILE' is configured"
    echo "2. You are logged in to AWS SSO (run: aws sso login --profile $PROFILE)"
    echo "3. Profile has necessary permissions"
    exit 1
fi

echo -e "${YELLOW}Account:${NC} $ACCOUNT"
echo ""

# Set environment variables
export CDK_STAGE="$STAGE"
export CDK_DEFAULT_REGION="$REGION"
export CDK_DEFAULT_ACCOUNT="$ACCOUNT"

# Navigate to CDK directory
if [ ! -d "cdk" ]; then
    echo -e "${RED}Error: CDK directory not found. Please run this script from the project root.${NC}"
    exit 1
fi

cd cdk

echo -e "${GREEN}Installing dependencies...${NC}"
npm install

echo -e "${GREEN}Building CDK project...${NC}"
npm run build

echo -e "${GREEN}Bootstrapping CDK (if needed)...${NC}"
npx cdk bootstrap --profile "$PROFILE"

echo -e "${GREEN}Deploying stacks...${NC}"

# Function to deploy a stack with error handling
deploy_stack() {
    local stack_name=$1
    echo -e "${YELLOW}Deploying $stack_name...${NC}"
    if npx cdk deploy "$stack_name" --profile "$PROFILE" --require-approval never; then
        echo -e "${GREEN}✓ $stack_name deployed successfully${NC}"
    else
        echo -e "${RED}✗ Failed to deploy $stack_name${NC}"
        exit 1
    fi
}

# Deploy in the correct order
deploy_stack "PawfectMatch-Shared"
deploy_stack "PawfectMatch-$STAGE-Environment"
deploy_stack "PawfectMatch-$STAGE-Identity"
deploy_stack "PawfectMatch-$STAGE-ShelterHub"
deploy_stack "PawfectMatch-$STAGE-Matcher"

echo ""
echo -e "${GREEN}=== Deployment Complete ===${NC}"
echo ""
echo -e "${YELLOW}Domain Configuration:${NC}"
if [ "$STAGE" = "production" ]; then
    echo "  Identity: https://id.pawfectmatchnow.com"
    echo "  Matcher:  https://adopter.pawfectmatchnow.com"
    echo "  Shelter:  https://shelter.pawfectmatchnow.com"
else
    echo "  Identity: https://id.$STAGE.pawfectmatchnow.com"
    echo "  Matcher:  https://adopter.$STAGE.pawfectmatchnow.com"
    echo "  Shelter:  https://shelter.$STAGE.pawfectmatchnow.com"
fi

echo ""
echo -e "${YELLOW}Next Steps:${NC}"
echo "1. If you haven't purchased pawfectmatchnow.com yet:"
echo "   - Purchase the domain through AWS Route 53 or your preferred registrar"
echo "   - Update DNS settings to point to Route 53 name servers"
echo ""
echo "2. Check Route 53 Console for:"
echo "   - Hosted zone configuration"
echo "   - SSL certificate validation status"
echo ""
echo "3. Test domain resolution:"
echo "   - Use 'dig' or 'nslookup' to verify DNS propagation"
echo "   - Verify SSL certificates are properly installed"
echo ""
echo -e "${GREEN}For detailed instructions, see ROUTE53_DEPLOYMENT_GUIDE.md${NC}"
