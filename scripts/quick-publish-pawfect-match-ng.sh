#!/bin/bash

# Quick publish script for @longhl104/pawfect-match-ng
# Simplified version for rapid publishing

set -e

# Colors
GREEN='\033[0;32m'
BLUE='\033[0;34m'
RED='\033[0;31m'
NC='\033[0m'

WORKSPACE_DIR="../Shared/pawfect-match-angular"
LIBRARY_NAME="pawfect-match-ng"

echo -e "${BLUE}🚀 Quick Publishing @longhl104/pawfect-match-ng${NC}"
echo "================================================"

# Navigate to workspace
cd "$WORKSPACE_DIR"

# Build the library
echo -e "${BLUE}📦 Building library...${NC}"
ng build $LIBRARY_NAME --configuration production

# Navigate to dist
cd "dist/longhl104/pawfect-match-ng"

# Show current version
CURRENT_VERSION=$(node -p "require('./package.json').version")
echo -e "${BLUE}📋 Current version: $CURRENT_VERSION${NC}"

# Publish
echo -e "${BLUE}🚀 Publishing to npm...${NC}"
npm publish --access public

echo -e "${GREEN}✅ Successfully published!${NC}"
echo -e "${BLUE}📦 Install with: npm install @longhl104/pawfect-match-ng${NC}"
