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
	echo "Press Ctrl+C to stop the server"
	echo "================================================"

	# Run the application with HTTPS profile
	dotnet run --launch-profile https
else
	echo "Build failed. Please check the errors above."
	exit 1
fi
