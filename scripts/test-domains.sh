#!/bin/bash

# Domain Testing Utility for PawfectMatch
# This script helps test domain configuration and SSL certificates

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

STAGE=""

usage() {
    echo "Usage: $0 [OPTIONS]"
    echo "Options:"
    echo "  -s, --stage STAGE        Environment stage (development|production)"
    echo "  -h, --help               Display this help message"
    echo ""
    echo "Example:"
    echo "  $0 --stage development"
    echo "  $0 --stage production"
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -s|--stage)
            STAGE="$2"
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

if [[ "$STAGE" != "development" && "$STAGE" != "production" ]]; then
    echo -e "${RED}Error: Stage must be 'development' or 'production'${NC}"
    exit 1
fi

# Set domain names based on stage
if [ "$STAGE" = "production" ]; then
    ROOT_DOMAIN="pawfectmatchnow.com"
    IDENTITY_DOMAIN="id.pawfectmatchnow.com"
    MATCHER_DOMAIN="adopter.pawfectmatchnow.com"
    SHELTER_DOMAIN="shelter.pawfectmatchnow.com"
else
    ROOT_DOMAIN="$STAGE.pawfectmatchnow.com"
    IDENTITY_DOMAIN="id.$STAGE.pawfectmatchnow.com"
    MATCHER_DOMAIN="adopter.$STAGE.pawfectmatchnow.com"
    SHELTER_DOMAIN="shelter.$STAGE.pawfectmatchnow.com"
fi

echo -e "${GREEN}=== PawfectMatch Domain Testing ($STAGE) ===${NC}"
echo ""

# Function to test DNS resolution
test_dns() {
    local domain=$1
    local record_type=$2
    echo -e "${BLUE}Testing DNS resolution for $domain ($record_type):${NC}"

    if command -v dig >/dev/null 2>&1; then
        dig +short "$domain" "$record_type"
    elif command -v nslookup >/dev/null 2>&1; then
        nslookup -query="$record_type" "$domain"
    else
        echo -e "${YELLOW}Neither dig nor nslookup found. Please install dig for better DNS testing.${NC}"
    fi
    echo ""
}

# Function to test SSL certificate
test_ssl() {
    local domain=$1
    echo -e "${BLUE}Testing SSL certificate for $domain:${NC}"

    if command -v openssl >/dev/null 2>&1; then
        echo | timeout 5 openssl s_client -connect "$domain:443" -servername "$domain" 2>/dev/null | \
        openssl x509 -noout -subject -issuer -dates 2>/dev/null || \
        echo -e "${YELLOW}SSL certificate test failed or timed out${NC}"
    else
        echo -e "${YELLOW}OpenSSL not found. Cannot test SSL certificate.${NC}"
    fi
    echo ""
}

# Function to test HTTP/HTTPS connectivity
test_connectivity() {
    local domain=$1
    echo -e "${BLUE}Testing connectivity to $domain:${NC}"

    if command -v curl >/dev/null 2>&1; then
        # Test HTTPS
        if curl -Is --connect-timeout 5 "https://$domain" >/dev/null 2>&1; then
            echo -e "${GREEN}✓ HTTPS connection successful${NC}"
        else
            echo -e "${RED}✗ HTTPS connection failed${NC}"
        fi

        # Test HTTP (should redirect to HTTPS)
        if curl -Is --connect-timeout 5 "http://$domain" >/dev/null 2>&1; then
            echo -e "${GREEN}✓ HTTP connection successful (should redirect to HTTPS)${NC}"
        else
            echo -e "${YELLOW}! HTTP connection failed${NC}"
        fi
    else
        echo -e "${YELLOW}curl not found. Cannot test connectivity.${NC}"
    fi
    echo ""
}

# Test root domain
echo -e "${YELLOW}=== Testing Root Domain ===${NC}"
test_dns "$ROOT_DOMAIN" "A"
test_dns "$ROOT_DOMAIN" "NS"
test_ssl "$ROOT_DOMAIN"
test_connectivity "$ROOT_DOMAIN"

# Test service domains
domains=("$IDENTITY_DOMAIN" "$MATCHER_DOMAIN" "$SHELTER_DOMAIN")
service_names=("Identity" "Matcher" "Shelter Hub")

for i in "${!domains[@]}"; do
    echo -e "${YELLOW}=== Testing ${service_names[$i]} Domain ===${NC}"
    test_dns "${domains[$i]}" "A"
    test_ssl "${domains[$i]}"
    test_connectivity "${domains[$i]}"
done

# Test CNAME records for www
echo -e "${YELLOW}=== Testing WWW Redirect ===${NC}"
if [ "$STAGE" = "production" ]; then
    test_dns "www.pawfectmatchnow.com" "CNAME"
    test_connectivity "www.pawfectmatchnow.com"
fi

echo -e "${GREEN}=== Domain Testing Complete ===${NC}"
echo ""
echo -e "${YELLOW}Interpretation Guide:${NC}"
echo "• DNS A records should return IP addresses"
echo "• NS records should show Route 53 name servers"
echo "• SSL certificates should be valid and issued by Amazon"
echo "• HTTPS connections should succeed"
echo "• HTTP should redirect to HTTPS"
echo ""
echo -e "${YELLOW}Common Issues:${NC}"
echo "• DNS not resolving: Check Route 53 configuration and domain name servers"
echo "• SSL certificate issues: Wait for certificate validation (can take time)"
echo "• Connection timeouts: Services may not be deployed yet"
echo ""
echo -e "${BLUE}Next Steps:${NC}"
echo "1. If DNS issues persist, check Route 53 hosted zone configuration"
echo "2. If SSL issues persist, check Certificate Manager in AWS Console"
echo "3. For service connectivity, verify API Gateway/CloudFront deployment"
