# PawfectMatch ECS Fargate Deployment Guide

This document explains the ECS Fargate deployment setup for the PawfectMatch backend services.

## Overview

The PawfectMatch backend consists of three ASP.NET Core Web API services:

- **Identity Service** (`Longhl104.Identity`) - Handles user authentication and authorization
- **Matcher Service** (`Longhl104.Matcher`) - Handles pet matching logic for adopters
- **ShelterHub Service** (`Longhl104.ShelterHub`) - Manages shelter operations and pet data

Each service is containerized using Docker and deployed to AWS ECS Fargate.

## Architecture

### Infrastructure Components

1. **VPC** - Custom VPC with public and private subnets across 2 AZs
2. **ECS Cluster** - Fargate cluster to run containerized services
3. **ECR Repositories** - Docker image repositories for each service
4. **Application Load Balancer** - Routes traffic to services with health checks
5. **Route 53** - Custom domain routing for each service
6. **CloudWatch** - Centralized logging and monitoring

### Service Configuration

Each service is deployed with:
- **CPU**: 512 (0.5 vCPU)
- **Memory**: 1024 MB (1 GB)
- **Container Port**: 80
- **Health Check**: `/health` endpoint
- **Auto Scaling**: 1 instance (development), 2 instances (production)

### Domain Structure

- Identity: `api-id.{stage}.pawfectmatchnow.com`
- Matcher: `api-matcher.{stage}.pawfectmatchnow.com`
- ShelterHub: `api-shelter.{stage}.pawfectmatchnow.com`

Where `{stage}` is:
- `development` for development environment
- Production uses root domain (no stage prefix)

## Deployment Process

### Prerequisites

1. AWS CLI v2 configured with SSO
2. Docker Desktop running
3. Node.js and npm
4. .NET 9 SDK

### Manual Deployment Steps

1. **Build and Push Docker Images**:
   ```bash
   cd scripts
   ./deploy.sh development --skip-angular-build --skip-lambda-build
   ```

2. **Deploy Infrastructure Only**:
   ```bash
   cd scripts
   ./deploy.sh development --skip-backend-build --skip-angular-build --skip-lambda-build
   ```

### Automated Deployment

The `deploy.sh` script handles the complete deployment:

```bash
cd scripts
./deploy.sh development
```

This will:
1. Build the CDK project
2. Build Angular clients (if not skipped)
3. Build and push Docker images to ECR
4. Deploy CDK stacks (infrastructure + services)

### Deployment Order

1. **Shared Stack** - Route 53 hosted zone and SSL certificates
2. **Environment Stack** - VPC, ECS cluster, ECR repositories
3. **Service Stacks** - Individual services with their ECS services and load balancers

## Docker Configuration

### Dockerfiles

Each service has a multi-stage Dockerfile:

1. **Base Stage** - Runtime image (mcr.microsoft.com/dotnet/aspnet:9.0)
2. **Build Stage** - SDK image for building (mcr.microsoft.com/dotnet/sdk:9.0)
3. **Publish Stage** - Publishes the application
4. **Final Stage** - Production runtime with non-root user

### Environment Variables

Each container receives:
- `ASPNETCORE_ENVIRONMENT`: "Development" or "Production"
- `ASPNETCORE_URLS`: "http://+:80"
- `PawfectMatch__Environment`: Current stage
- `PawfectMatch__ServiceName`: Service name

### Health Checks

All services expose a `/health` endpoint that returns:
```json
{
  "Status": "Healthy",
  "Timestamp": "2025-01-01T00:00:00.000Z"
}
```

## Security

### Network Security

- Services run in private subnets
- Only ALB has public access
- Security groups restrict traffic to necessary ports
- All inter-service communication through internal load balancers

### Authentication

- Services use JWT tokens for authentication
- Tokens are validated using shared JWT configuration
- Cognito integration for user management

### IAM Permissions

ECS tasks have permissions for:
- DynamoDB read/write operations
- S3 object operations
- Cognito user pool operations
- CloudWatch logging

## Monitoring and Logging

### CloudWatch Logs

All container logs are sent to CloudWatch with:
- Log Group: `/aws/ecs/pawfectmatch-{stage}`
- Retention: 1 week (dev), 1 month (production)
- Log streams per service

### Health Monitoring

- ALB performs health checks every 30 seconds
- Unhealthy containers are automatically replaced
- CloudWatch alarms for service failures

## Troubleshooting

### Common Issues

1. **Image Build Failures**:
   - Check Docker daemon is running
   - Verify .NET project builds locally
   - Check Dockerfile paths

2. **ECS Service Deployment Failures**:
   - Check ECS service events in AWS Console
   - Verify ECR repository exists and has images
   - Check task definition configuration

3. **Health Check Failures**:
   - Verify `/health` endpoint is accessible
   - Check container logs for startup errors
   - Verify security group allows ALB access

### Debugging Commands

```bash
# Check ECS service status
aws ecs describe-services --cluster pawfectmatch-{stage}-cluster --services {service-name}

# View container logs
aws logs tail /aws/ecs/pawfectmatch-{stage} --follow

# Check ALB target health
aws elbv2 describe-target-health --target-group-arn {target-group-arn}
```

## Scaling and Performance

### Auto Scaling

- Development: Fixed 1 instance per service
- Production: 2-10 instances based on CPU/memory usage
- Scale-up threshold: 70% CPU or memory
- Scale-down threshold: 30% CPU or memory

### Resource Optimization

- Container resources are right-sized for workload
- ALB connection draining for graceful shutdowns
- ECS service deployment strategies for zero-downtime updates

## Cost Optimization

- Development environment uses single NAT gateway
- Spot instances for non-critical workloads
- CloudWatch log retention policies
- ECR lifecycle policies (keep last 10 images)

## Future Enhancements

1. **CI/CD Pipeline** - GitHub Actions for automated deployments
2. **Blue/Green Deployments** - Zero-downtime deployments
3. **Service Mesh** - AWS App Mesh for advanced traffic management
4. **Database** - RDS or Aurora for persistent data
5. **Caching** - ElastiCache for improved performance
