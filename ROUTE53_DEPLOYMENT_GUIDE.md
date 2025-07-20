# PawfectMatch Route 53 Deployment Guide

This guide explains how to deploy PawfectMatch with custom domains using AWS Route 53, even before purchasing the `pawfectmatchnow.com` domain.

## Domain Structure

### Production Domains
- **Identity Service**: `id.pawfectmatchnow.com`
- **Matcher Service**: `adopter.pawfectmatchnow.com`
- **Shelter Hub**: `shelter.pawfectmatchnow.com`

### Development/Staging Domains
For non-production environments, the environment name is added as a subdomain:
- **Identity Service**: `id.development.pawfectmatchnow.com`
- **Matcher Service**: `adopter.development.pawfectmatchnow.com`
- **Shelter Hub**: `shelter.development.pawfectmatchnow.com`

## Deployment Steps

### 1. Domain Purchase (When Ready)
When you're ready to purchase the domain:
1. Purchase `pawfectmatchnow.com` through AWS Route 53 or any domain registrar
2. If purchased outside AWS, configure DNS to point to AWS Route 53 name servers

### 2. Environment Setup

Set the following environment variables:

```bash
# For development
export CDK_STAGE=development
export CDK_DEFAULT_ACCOUNT=your-dev-account-id
export CDK_DEFAULT_REGION=us-east-1

# For production
export CDK_STAGE=production
export CDK_PRODUCTION_ACCOUNT=your-prod-account-id
export CDK_DEFAULT_REGION=us-east-1
```

### 3. Deploy Infrastructure

Deploy in the following order:

```bash
# Navigate to CDK directory
cd cdk

# Install dependencies
npm install

# Build the project
npm run build

# Deploy Shared Stack first (creates hosted zone and certificates)
cdk deploy PawfectMatch-Shared --profile your-aws-profile

# Deploy Environment Stack
cdk deploy PawfectMatch-{stage}-Environment --profile your-aws-profile

# Deploy Service Stacks
cdk deploy PawfectMatch-{stage}-Identity --profile your-aws-profile
cdk deploy PawfectMatch-{stage}-ShelterHub --profile your-aws-profile
cdk deploy PawfectMatch-{stage}-Matcher --profile your-aws-profile
```

### 4. DNS Configuration

#### If Domain is Already Purchased
The CDK will automatically create all necessary DNS records.

#### If Domain is Not Yet Purchased
1. The hosted zone will be created with the correct configuration
2. After purchasing the domain, update the domain's name servers to point to the Route 53 hosted zone
3. Note down the name servers from the Route 53 console

### 5. SSL Certificates

The CDK automatically creates SSL certificates for:
- Production: `pawfectmatchnow.com` and `*.pawfectmatchnow.com`
- Development: `development.pawfectmatchnow.com` and `*.development.pawfectmatchnow.com`

Certificates are validated using DNS validation, so they'll be ready once the domain is properly configured.

## Architecture Overview

### Shared Stack
- Creates the Route 53 hosted zone
- Manages SSL certificates
- Stores domain configuration in SSM parameters

### Service Stacks
Each service stack (Identity, Matcher, ShelterHub) can:
- Create API Gateway custom domains
- Set up CloudFront distributions for SPAs
- Configure Route 53 A records automatically

## Manual Steps After Domain Purchase

1. **Update Domain Name Servers**: Point your domain to the Route 53 hosted zone name servers
2. **Verify SSL Certificates**: Ensure DNS validation completes successfully
3. **Test Domains**: Verify all subdomains resolve correctly

## Environment-Specific Configuration

### Development Environment
- Uses subdomain structure: `service.development.pawfectmatchnow.com`
- Shares the same Route 53 hosted zone as production
- Separate SSL certificate for the development subdomain

### Production Environment
- Uses primary domain structure: `service.pawfectmatchnow.com`
- Creates the main Route 53 hosted zone
- Primary SSL certificate for the root domain

## Cost Considerations

- **Route 53 Hosted Zone**: ~$0.50/month per hosted zone
- **SSL Certificates**: Free through AWS Certificate Manager
- **DNS Queries**: $0.40 per million queries
- **Domain Registration**: Variable cost depending on TLD

## Security Features

- **HTTPS Everywhere**: All services use SSL/TLS encryption
- **DNS Security**: DNSSEC can be enabled for additional security
- **Certificate Auto-Renewal**: AWS Certificate Manager handles automatic renewal

## Troubleshooting

### Common Issues

1. **Certificate Validation Pending**
   - Ensure domain name servers point to Route 53
   - Check DNS propagation (can take up to 24-48 hours)

2. **Domain Not Resolving**
   - Verify Route 53 A records are created
   - Check CloudFront/API Gateway health

3. **CORS Issues**
   - Update allowed origins in S3 bucket CORS configuration
   - Update API Gateway CORS settings

### Useful Commands

```bash
# Check CDK diff before deployment
cdk diff PawfectMatch-{stage}-{service}

# View CDK outputs
cdk outputs PawfectMatch-{stage}-{service}

# Check domain propagation
dig pawfectmatchnow.com NS
nslookup id.pawfectmatchnow.com

# Test SSL certificate
openssl s_client -connect id.pawfectmatchnow.com:443 -servername id.pawfectmatchnow.com
```

## Next Steps After Implementation

1. **Monitor DNS Resolution**: Use AWS CloudWatch to monitor DNS query patterns
2. **Set Up Alerts**: Configure CloudWatch alarms for certificate expiration (though auto-renewal is enabled)
3. **Performance Optimization**: Consider using CloudFront for global content delivery
4. **Backup Strategy**: Implement Route 53 configuration backups

This setup provides a robust, scalable domain infrastructure that can grow with your application while maintaining security and performance best practices.
