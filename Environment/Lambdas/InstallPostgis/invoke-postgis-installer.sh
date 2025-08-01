#!/bin/bash

# PostGIS Installation Lambda Function Invocation Script
# This script invokes the PostGIS installer Lambda function

set -e

# Configuration
STAGE=${1:-development}
REGION=${2:-us-east-1}
FUNCTION_NAME="pawfect-match-Environment-InstallPostgis-${STAGE}"

echo "=== PostGIS Installer Lambda Function Invocation ==="
echo "Stage: $STAGE"
echo "Region: $REGION"
echo "Function Name: $FUNCTION_NAME"
echo

# Create payload
PAYLOAD="{\"Stage\":\"$STAGE\"}"

echo "Payload: $PAYLOAD"
echo

# Invoke the Lambda function
echo "Invoking Lambda function..."
aws lambda invoke \
  --region "$REGION" \
  --function-name "$FUNCTION_NAME" \
  --payload "$PAYLOAD" \
  --cli-binary-format raw-in-base64-out \
  response.json

echo
echo "Lambda function invocation completed."
echo

# Display the response
if [ -f response.json ]; then
  echo "Response:"
  cat response.json | jq '.'
  echo

  # Check if the installation was successful
  SUCCESS=$(cat response.json | jq -r '.Success')
  if [ "$SUCCESS" = "true" ]; then
    echo "✅ PostGIS extension installed successfully!"
    VERSION=$(cat response.json | jq -r '.ExtensionVersion')
    if [ "$VERSION" != "null" ]; then
      echo "PostGIS Version: $VERSION"
    fi
  else
    echo "❌ PostGIS extension installation failed."
    MESSAGE=$(cat response.json | jq -r '.Message')
    echo "Error: $MESSAGE"
    exit 1
  fi
else
  echo "❌ No response file found."
  exit 1
fi

# Clean up
rm -f response.json

echo "Done."
