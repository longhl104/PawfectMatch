#!/bin/bash

# PawfectMatch Certificate Setup Guide
# This script helps you create and configure SSL certificates manually for CloudFront

set -e

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

# Function to validate environment parameter
validate_environment() {
    local env=$1
    case $env in
    dev | development)
        export ENVIRONMENT="development"
        export DOMAIN="development.pawfectmatchnow.com"
        export SSM_PATH="/PawfectMatch/development/Shared/CertificateArn"
        ;;
    prod | production)
        export ENVIRONMENT="production"
        export DOMAIN="pawfectmatchnow.com"
        export SSM_PATH="/PawfectMatch/Shared/CertificateArn"
        ;;
    *)
        print_error "Invalid environment: $env"
        print_info "Valid environments: dev, development, prod, production"
        exit 1
        ;;
    esac
}

# Function to create certificate in ACM
create_certificate() {
    print_info "Creating SSL certificate for domain: $DOMAIN"
    print_warning "This must be done in us-east-1 region for CloudFront compatibility"

    echo
    print_info "Running AWS CLI command to create certificate..."

    # Create the certificate
    CERT_ARN=$(aws acm request-certificate \
        --domain-name "$DOMAIN" \
        --subject-alternative-names "*.$DOMAIN" \
        --validation-method DNS \
        --region us-east-1 \
        --query 'CertificateArn' \
        --output text)

    if [ $? -eq 0 ] && [ ! -z "$CERT_ARN" ]; then
        print_success "Certificate requested successfully!"
        print_info "Certificate ARN: $CERT_ARN"
        echo

        # Store in SSM
        print_info "Storing certificate ARN in SSM Parameter Store..."
        aws ssm put-parameter \
            --name "$SSM_PATH" \
            --value "$CERT_ARN" \
            --type "String" \
            --description "SSL Certificate ARN for PawfectMatch $ENVIRONMENT environment" \
            --overwrite \
            --region us-east-1

        if [ $? -eq 0 ]; then
            print_success "Certificate ARN stored in SSM: $SSM_PATH"
        else
            print_error "Failed to store certificate ARN in SSM"
            exit 1
        fi

        echo
        print_warning "IMPORTANT: You need to validate the certificate by adding DNS records!"
        print_info "Run the following command to get validation records:"
        echo
        echo "aws acm describe-certificate --certificate-arn $CERT_ARN --region us-east-1"
        echo
        print_info "Add the CNAME records to your DNS zone, then wait for validation to complete."

    else
        print_error "Failed to create certificate"
        exit 1
    fi
}

# Function to check certificate status
check_certificate() {
    if [ -z "$1" ]; then
        print_error "Certificate ARN required"
        exit 1
    fi

    local cert_arn=$1
    print_info "Checking certificate status for: $cert_arn"

    aws acm describe-certificate \
        --certificate-arn "$cert_arn" \
        --region us-east-1 \
        --query 'Certificate.{Status:Status,DomainName:DomainName,ValidationRecords:DomainValidationOptions[].ResourceRecord}' \
        --output table
}

# Function to list existing certificates
list_certificates() {
    print_info "Listing existing certificates in us-east-1..."
    aws acm list-certificates \
        --region us-east-1 \
        --query 'CertificateSummaryList[].{DomainName:DomainName,Status:Status,CertificateArn:CertificateArn}' \
        --output table
}

# Main execution
main() {
    echo "========================================="
    echo "   PawfectMatch Certificate Setup"
    echo "========================================="
    echo

    case "${1:-}" in
        create)
            if [ -z "$2" ]; then
                print_error "Environment parameter required for create command"
                echo "Usage: $0 create <environment>"
                echo "Environments: dev, development, prod, production"
                exit 1
            fi
            validate_environment "$2"
            create_certificate
            ;;
        check)
            if [ -z "$2" ]; then
                print_error "Certificate ARN required for check command"
                echo "Usage: $0 check <certificate-arn>"
                exit 1
            fi
            check_certificate "$2"
            ;;
        list)
            list_certificates
            ;;
        *)
            echo "Usage: $0 {create|check|list} [args...]"
            echo
            echo "Commands:"
            echo "  create <environment>    Create a new certificate for the environment"
            echo "  check <cert-arn>       Check the status of a certificate"
            echo "  list                   List all certificates in us-east-1"
            echo
            echo "Examples:"
            echo "  $0 create production"
            echo "  $0 create development"
            echo "  $0 list"
            echo "  $0 check arn:aws:acm:us-east-1:123456789012:certificate/12345678-1234-1234-1234-123456789012"
            exit 1
            ;;
    esac
}

main "$@"
