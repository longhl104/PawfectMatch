#!/bin/bash

# PawfectMatch Identity API Startup Script
echo "Starting PawfectMatch Identity API..."

# Navigate to the project directory
cd "$(dirname "$0")"

# Build the project
echo "Building the project..."
dotnet build

if [ $? -eq 0 ]; then
    echo "Build successful. Starting the API..."
    echo "The API will be available at:"
    echo "  HTTP:  http://localhost:5209"
    echo "  HTTPS: https://localhost:7027"
    echo ""
    echo "Available endpoints:"
    echo "  GET  /health                     - Health check"
    echo "  POST /api/auth/login             - User login"
    echo "  POST /api/auth/refresh           - Refresh access token"
    echo "  POST /api/auth/logout            - User logout"
    echo "  POST /api/registration/adopter   - Register new adopter"
    echo ""
    echo "Press Ctrl+C to stop the server"
    echo "================================================"

    # Run the application
    dotnet run
else
    echo "Build failed. Please check the errors above."
    exit 1
fi
