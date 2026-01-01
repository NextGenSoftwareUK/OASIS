#!/bin/bash

# OASIS API Docker Build Script
# Builds the Docker image locally for testing
# Usage: ./docker/build.sh [tag]

set -e  # Exit on error

# Get the script directory and project root
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
cd "${PROJECT_ROOT}"

# Configuration
IMAGE_NAME="oasis-api"
IMAGE_TAG="${1:-latest}"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}🐳 Building OASIS API Docker Image${NC}"
echo "=================================="
echo "Image Name: ${IMAGE_NAME}"
echo "Image Tag: ${IMAGE_TAG}"
echo "Dockerfile: docker/Dockerfile"
echo "Build Context: ${PROJECT_ROOT}"
echo ""

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo -e "${RED}❌ Error: Docker is not running. Please start Docker and try again.${NC}"
    exit 1
fi

# Check if Dockerfile exists
if [ ! -f "${SCRIPT_DIR}/Dockerfile" ]; then
    echo -e "${RED}❌ Error: Dockerfile not found at ${SCRIPT_DIR}/Dockerfile${NC}"
    exit 1
fi

# Build the Docker image
echo -e "${YELLOW}📋 Building Docker image...${NC}"
echo ""

docker build \
    -f "${SCRIPT_DIR}/Dockerfile" \
    -t ${IMAGE_NAME}:${IMAGE_TAG} \
    "${PROJECT_ROOT}"

if [ $? -eq 0 ]; then
    echo ""
    echo -e "${GREEN}✅ Docker image built successfully!${NC}"
    echo "=================================="
    echo "Image: ${IMAGE_NAME}:${IMAGE_TAG}"
    echo ""
    echo -e "${YELLOW}📋 Next Steps:${NC}"
    echo "1. Run the container:"
    echo "   docker run -p 5003:80 ${IMAGE_NAME}:${IMAGE_TAG}"
    echo ""
    echo "2. Or use docker-compose:"
    echo "   docker-compose up"
    echo ""
    echo "3. Access the API:"
    echo "   http://localhost:5003/swagger"
    echo ""
else
    echo -e "${RED}❌ Docker build failed${NC}"
    exit 1
fi

