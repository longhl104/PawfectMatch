# SSL Certificate Setup for PawfectMatch

This guide explains how to manually create and manage SSL certificates for the PawfectMatch application.

## Why Manual Certificate Management?

- **Full Control**: You have complete control over certificate lifecycle
- **No CDK Dependencies**: Certificates are created independently of CDK deployments
- **Faster Deployments**: CDK deployments don't wait for certificate validation
- **Reusability**: Same certificate can be used across multiple deployments

## Requirements

- AWS CLI configured with appropriate permissions
- Access to DNS management for domain validation
- Certificates must be created in `us-east-1` region (CloudFront requirement)

## Quick Start

### 1. Create Certificates

Use the provided script to create certificates:

```bash
# For production environment
./scripts/setup-certificates.sh create production

# For development environment  
./scripts/setup-certificates.sh create development
```

### 2. Validate Certificates

After creating certificates, you'll need to add DNS validation records:

```bash
# Check certificate status and get validation records
./scripts/setup-certificates.sh check <certificate-arn>
```

Add the CNAME records shown in the output to your DNS zone.

### 3. Deploy CDK

Once certificates are validated, deploy your CDK stacks:

```bash
./scripts/deploy.sh development
```

## Manual Steps

### Create Certificate via AWS Console

1. Go to **AWS Certificate Manager** in `us-east-1` region
2. Click **Request a certificate**
3. Choose **Request a public certificate**
4. Add domain names:
   - For production: `pawfectmatchnow.com` and `*.pawfectmatchnow.com`
   - For development: `development.pawfectmatchnow.com` and `*.development.pawfectmatchnow.com`
5. Choose **DNS validation**
6. Add tags if needed
7. Review and request

### Create Certificate via AWS CLI

```bash
# Production certificate
aws acm request-certificate \
    --domain-name "pawfectmatchnow.com" \
    --subject-alternative-names "*.pawfectmatchnow.com" \
    --validation-method DNS \
    --region us-east-1

# Development certificate
aws acm request-certificate \
    --domain-name "development.pawfectmatchnow.com" \
    --subject-alternative-names "*.development.pawfectmatchnow.com" \
    --validation-method DNS \
    --region us-east-1
```

### Store Certificate ARN in SSM

Once you have the certificate ARN, store it in SSM Parameter Store:

```bash
# Production
aws ssm put-parameter \
    --name "/PawfectMatch/Shared/CertificateArn" \
    --value "arn:aws:acm:us-east-1:ACCOUNT:certificate/CERT-ID" \
    --type "String" \
    --description "SSL Certificate ARN for PawfectMatch production" \
    --region us-east-1

# Development
aws ssm put-parameter \
    --name "/PawfectMatch/development/Shared/CertificateArn" \
    --value "arn:aws:acm:us-east-1:ACCOUNT:certificate/CERT-ID" \
    --type "String" \
    --description "SSL Certificate ARN for PawfectMatch development" \
    --region us-east-1
```

## Domain Configuration

The following subdomains will be created automatically by CDK when certificates are available:

### Production (`pawfectmatchnow.com`)
- **Identity Client**: `app.pawfectmatchnow.com`
- **Matcher Client**: `match.pawfectmatchnow.com`  
- **ShelterHub Client**: `admin.pawfectmatchnow.com`

### Development (`development.pawfectmatchnow.com`)
- **Identity Client**: `app.development.pawfectmatchnow.com`
- **Matcher Client**: `match.development.pawfectmatchnow.com`
- **ShelterHub Client**: `admin.development.pawfectmatchnow.com`

## Useful Commands

```bash
# List all certificates
./scripts/setup-certificates.sh list

# Check specific certificate status
./scripts/setup-certificates.sh check <certificate-arn>

# View certificate details in AWS CLI
aws acm describe-certificate --certificate-arn <arn> --region us-east-1

# Get validation records
aws acm describe-certificate \
    --certificate-arn <arn> \
    --region us-east-1 \
    --query 'Certificate.DomainValidationOptions[].ResourceRecord'
```

## Troubleshooting

### Certificate Not Found Error
If you get "Certificate not found" errors during deployment:
1. Verify the certificate ARN is stored in the correct SSM parameter
2. Ensure the certificate is in `us-east-1` region
3. Check that the certificate status is "ISSUED"

### DNS Validation Issues
- Ensure CNAME records are added to the correct DNS zone
- Wait for DNS propagation (can take up to 72 hours)
- Verify records using `dig` or `nslookup`

### CDK Deployment Without Certificates
The CDK stacks will deploy successfully without certificates, but:
- CloudFront distributions will use default CloudFront domains
- No custom domain names will be configured
- Route 53 records will not be created

## Certificate Lifecycle

1. **Create**: Request certificate in ACM (us-east-1)
2. **Validate**: Add DNS records to validate domain ownership
3. **Store**: Save certificate ARN in SSM Parameter Store
4. **Deploy**: CDK automatically references the certificate
5. **Renew**: AWS automatically renews certificates before expiration
6. **Monitor**: Set up CloudWatch alarms for certificate expiration

## Security Notes

- Certificates are automatically renewed by AWS
- Private keys never leave AWS infrastructure
- DNS validation is more secure than email validation
- Wildcard certificates cover all subdomains
- All certificates support modern TLS versions
