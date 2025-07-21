#!/bin/bash

# Quick script to check ECS services
ENVIRONMENT="development"
AWS_PROFILE="longhl104"

# Get AWS region
aws_region=$(aws configure get region --profile $AWS_PROFILE)
if [ -z "$aws_region" ]; then
    aws_region="us-east-1"
fi

echo "Checking ECS services in region: $aws_region"
echo "Environment: $ENVIRONMENT"
echo "AWS Profile: $AWS_PROFILE"
echo "========================================="

# List all clusters
echo "Available ECS Clusters:"
aws ecs list-clusters --profile $AWS_PROFILE --region $aws_region --query 'clusterArns[*]' --output table

echo ""
echo "Trying different cluster name patterns:"

# Try different cluster naming patterns
cluster_patterns=(
    "pawfectmatch-${ENVIRONMENT}"
    "pawfectmatch-development"
    "PawfectMatch-${ENVIRONMENT}"
    "PawfectMatch-development"
    "pawfectmatch"
    "PawfectMatch"
)

for cluster_name in "${cluster_patterns[@]}"; do
    echo ""
    echo "Checking cluster: $cluster_name"

    # Check if cluster exists and list services
    if aws ecs describe-clusters --clusters $cluster_name --profile $AWS_PROFILE --region $aws_region --query 'clusters[0].clusterName' --output text 2>/dev/null | grep -q "$cluster_name"; then
        echo "✓ Cluster exists: $cluster_name"

        echo "Services in cluster $cluster_name:"
        aws ecs list-services --cluster $cluster_name --profile $AWS_PROFILE --region $aws_region --query 'serviceArns[*]' --output table

        echo "Service details:"
        aws ecs describe-services --cluster $cluster_name --services $(aws ecs list-services --cluster $cluster_name --profile $AWS_PROFILE --region $aws_region --query 'serviceArns[*]' --output text) --profile $AWS_PROFILE --region $aws_region --query 'services[*].[serviceName,status]' --output table 2>/dev/null || echo "No services found or error occurred"
    else
        echo "✗ Cluster not found: $cluster_name"
    fi
done
