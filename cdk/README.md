# PawfectMatch CDK Infrastructure

This directory contains the AWS CDK infrastructure code for the PawfectMatch application, including Route 53 domain configuration, SSL certificates, and multi-environment support.

## Architecture Overview

The infrastructure is organized into multiple stacks:

- **SharedStack**: Route 53 hosted zones, SSL certificates, and shared resources
- **EnvironmentStack**: VPC, networking, and environment-specific resources
- **IdentityStack**: AWS Cognito User Pools and authentication
- **ShelterHubStack**: DynamoDB tables and S3 buckets for shelter management
- **MatcherStack**: Resources for the pet matching service

## Domain Configuration

### Production Domains
- **Identity Service**: `id.pawfectmatchnow.com`
- **Matcher Service**: `adopter.pawfectmatchnow.com`
- **Shelter Hub**: `shelter.pawfectmatchnow.com`

### Development Domains
- **Identity Service**: `id.development.pawfectmatchnow.com`
- **Matcher Service**: `adopter.development.pawfectmatchnow.com`
- **Shelter Hub**: `shelter.development.pawfectmatchnow.com`

## Quick Start

### Prerequisites
- AWS CLI configured with appropriate permissions
- Node.js 18+ installed
- CDK CLI installed: `npm install -g aws-cdk`

### Setup

1. **Install dependencies**:
   ```bash
   npm install
   ```

2. **Create environment configuration**:
   ```bash
   cp .env.template .env
   # Edit .env with your AWS account IDs and preferences
   ```

3. **Build the project**:
   ```bash
   npm run build
   ```

4. **Bootstrap CDK** (first time only):
   ```bash
   cdk bootstrap
   ```

### Deployment

#### Option 1: Using the Deployment Script (Recommended)

```bash
# For development environment
../scripts/deploy-with-domains.sh --stage development --profile your-dev-profile

# For production environment
../scripts/deploy-with-domains.sh --stage production --profile your-prod-profile
```

#### Option 2: Manual Deployment

```bash
# Set environment variables
export CDK_STAGE=development
export CDK_DEFAULT_REGION=us-east-1

# Deploy stacks in order
cdk deploy PawfectMatch-Shared
cdk deploy PawfectMatch-development-Environment
cdk deploy PawfectMatch-development-Identity
cdk deploy PawfectMatch-development-ShelterHub
cdk deploy PawfectMatch-development-Matcher
```

## Testing Domain Configuration

After deployment, test your domain setup:

```bash
../scripts/test-domains.sh --stage development
```

## Available Commands

| Command | Description |
|---------|-------------|
| `npm run build` | Compile TypeScript to JavaScript |
| `npm run watch` | Watch for changes and compile |
| `npm run test` | Run unit tests |
| `cdk deploy` | Deploy this stack to your default AWS account/region |
| `cdk diff` | Compare deployed stack with current state |
| `cdk synth` | Emits the synthesized CloudFormation template |

## Environment Configuration

The infrastructure supports multiple environments through environment variables:

### Required Variables
- `CDK_STAGE`: deployment stage (development|production)
- `CDK_DEFAULT_REGION`: AWS region

### Optional Variables
- `CDK_DEFAULT_ACCOUNT`: AWS account ID (auto-detected from profile if not set)

## SSL Certificates

SSL certificates are automatically created and validated using DNS validation:
- Production: `*.pawfectmatchnow.com`
- Development: `*.development.pawfectmatchnow.com`

## Route 53 Configuration

### Production Environment
- Creates the main hosted zone for `pawfectmatchnow.com`
- Manages all DNS records
- Creates wildcard SSL certificates

### Other Environments
- References the production hosted zone
- Creates environment-specific subdomains
- Uses separate SSL certificates

## Cost Considerations

| Resource | Estimated Monthly Cost |
|----------|----------------------|
| Route 53 Hosted Zone | $0.50 |
| SSL Certificates | Free (ACM) |
| DNS Queries | $0.40 per million |
| DynamoDB | Pay per request |
| Cognito | Free tier available |

## Security Features

- **HTTPS Everywhere**: All services use SSL/TLS
- **IAM Least Privilege**: Minimal required permissions
- **VPC Isolation**: Private subnets for databases
- **Certificate Auto-Renewal**: Handled by ACM
- **CORS Protection**: Configured for each service

## Troubleshooting

### Common Issues

1. **Domain not resolving**:
   - Check Route 53 hosted zone configuration
   - Verify domain name servers point to Route 53
   - Allow time for DNS propagation (up to 48 hours)

2. **SSL certificate validation pending**:
   - Ensure DNS records are properly configured
   - Check Certificate Manager console
   - Verify domain ownership

3. **Stack deployment failures**:
   - Check CloudFormation events in AWS Console
   - Verify IAM permissions
   - Ensure account limits aren't exceeded

### Useful Commands

```bash
# Check differences before deployment
cdk diff PawfectMatch-development-Identity

# View stack outputs
cdk outputs PawfectMatch-development-Identity

# Check DNS propagation
dig pawfectmatchnow.com NS
nslookup id.development.pawfectmatchnow.com

# Test SSL certificate
openssl s_client -connect id.development.pawfectmatchnow.com:443
```

## Development

### Project Structure

```
cdk/
├── bin/
│   └── cdk.ts              # CDK app entry point
├── lib/
│   ├── utils/              # Utility classes and types
│   │   ├── domain-config.ts
│   │   ├── domain-utils.ts
│   │   └── stack-props.ts
│   ├── base-stack.ts       # Base stack with common functionality
│   ├── shared-stack.ts     # Route 53 and shared resources
│   ├── environment-stack.ts # VPC and networking
│   ├── identity-stack.ts   # Cognito and authentication
│   ├── shelter-hub.stack.ts # Shelter management resources
│   └── matcher-stack.ts    # Pet matching resources
├── test/                   # Unit tests
└── cdk.json               # CDK configuration
```

### Adding New Stacks

1. Create a new stack class extending `BaseStack`
2. Add domain configuration if needed
3. Update the main CDK app in `bin/cdk.ts`
4. Add appropriate dependencies
5. Test thoroughly in development

### Contributing

1. Follow TypeScript and CDK best practices
2. Add unit tests for new functionality
3. Update documentation for any changes
4. Test in development before production deployment

## Support

For infrastructure-related issues:
1. Check the troubleshooting section above
2. Review CloudFormation events in AWS Console
3. Check the deployment logs
4. Consult the main project documentation

## Related Documentation

- [Route 53 Deployment Guide](../ROUTE53_DEPLOYMENT_GUIDE.md)
- [Project README](../README.md)
- [AWS CDK Documentation](https://docs.aws.amazon.com/cdk/)
- [Route 53 Documentation](https://docs.aws.amazon.com/route53/)our CDK TypeScript project

This is a blank project for CDK development with TypeScript.

The `cdk.json` file tells the CDK Toolkit how to execute your app.

## Useful commands

* `npm run build`   compile typescript to js
* `npm run watch`   watch for changes and compile
* `npm run test`    perform the jest unit tests
* `npx cdk deploy`  deploy this stack to your default AWS account/region
* `npx cdk diff`    compare deployed stack with current state
* `npx cdk synth`   emits the synthesized CloudFormation template
