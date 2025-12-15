#!/bin/bash

# OASIS API Docker Deployment Script
# This script builds and pushes the latest OASIS API to AWS ECR
# Run from the OASIS_CLEAN root directory: ./docker/deploy.sh

set -e  # Exit on error

# Get the script directory and project root
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
cd "${PROJECT_ROOT}"

# Configuration
AWS_REGION="us-east-1"
AWS_ACCOUNT_ID="881490134703"
ECR_REPOSITORY="oasis-api"
ECR_URI="${AWS_ACCOUNT_ID}.dkr.ecr.${AWS_REGION}.amazonaws.com/${ECR_REPOSITORY}"
IMAGE_TAG="latest"
TIMESTAMP=$(date +%Y%m%d-%H%M%S)
VERSION_TAG="v${TIMESTAMP}"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}🚀 OASIS API Docker Deployment${NC}"
echo "=================================="
echo "ECR Repository: ${ECR_URI}"
echo "Image Tag: ${IMAGE_TAG}"
echo "Version Tag: ${VERSION_TAG}"
echo "Dockerfile: docker/Dockerfile"
echo "Build Context: ${PROJECT_ROOT}"
echo ""

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo -e "${RED}❌ Error: Docker is not running. Please start Docker and try again.${NC}"
    exit 1
fi

# Check if AWS CLI is installed
if ! command -v aws &> /dev/null; then
    echo -e "${RED}❌ Error: AWS CLI is not installed. Please install it first.${NC}"
    exit 1
fi

# Check if Dockerfile exists
if [ ! -f "${SCRIPT_DIR}/Dockerfile" ]; then
    echo -e "${RED}❌ Error: Dockerfile not found at ${SCRIPT_DIR}/Dockerfile${NC}"
    exit 1
fi

# Step 1: Authenticate with AWS ECR
echo ""
echo -e "${YELLOW}📋 Step 1: Authenticating with AWS ECR...${NC}"
aws ecr get-login-password --region ${AWS_REGION} | docker login --username AWS --password-stdin ${ECR_URI}
if [ $? -eq 0 ]; then
    echo -e "${GREEN}✅ Successfully authenticated with ECR${NC}"
else
    echo -e "${RED}❌ Failed to authenticate with ECR${NC}"
    exit 1
fi

# Step 2: Check if ECR repository exists, create if not
echo ""
echo -e "${YELLOW}📋 Step 2: Checking ECR repository...${NC}"
if aws ecr describe-repositories --repository-names ${ECR_REPOSITORY} --region ${AWS_REGION} > /dev/null 2>&1; then
    echo -e "${GREEN}✅ ECR repository exists${NC}"
else
    echo -e "${YELLOW}⚠️  ECR repository does not exist. Creating...${NC}"
    aws ecr create-repository --repository-name ${ECR_REPOSITORY} --region ${AWS_REGION}
    echo -e "${GREEN}✅ ECR repository created${NC}"
fi

# Step 3: Build the Docker image
echo ""
echo -e "${YELLOW}📋 Step 3: Building Docker image...${NC}"
echo "Build context: ${PROJECT_ROOT}"
echo "Dockerfile: ${SCRIPT_DIR}/Dockerfile"
echo ""

# Build with both latest and version tags
docker build \
    -f "${SCRIPT_DIR}/Dockerfile" \
    -t ${ECR_REPOSITORY}:${IMAGE_TAG} \
    -t ${ECR_REPOSITORY}:${VERSION_TAG} \
    -t ${ECR_URI}:${IMAGE_TAG} \
    -t ${ECR_URI}:${VERSION_TAG} \
    "${PROJECT_ROOT}"

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✅ Docker image built successfully${NC}"
else
    echo -e "${RED}❌ Docker build failed${NC}"
    exit 1
fi

# Step 4: Push the image to ECR
echo ""
echo -e "${YELLOW}📋 Step 4: Pushing image to ECR...${NC}"
echo "Pushing ${ECR_URI}:${IMAGE_TAG}..."
docker push ${ECR_URI}:${IMAGE_TAG}

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✅ Successfully pushed ${IMAGE_TAG} tag${NC}"
else
    echo -e "${RED}❌ Failed to push ${IMAGE_TAG} tag${NC}"
    exit 1
fi

echo "Pushing ${ECR_URI}:${VERSION_TAG}..."
docker push ${ECR_URI}:${VERSION_TAG}

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✅ Successfully pushed ${VERSION_TAG} tag${NC}"
else
    echo -e "${RED}❌ Failed to push ${VERSION_TAG} tag${NC}"
    exit 1
fi

# Step 5: Get the image digest
echo ""
echo -e "${YELLOW}📋 Step 5: Getting image digest...${NC}"
IMAGE_DIGEST=$(docker inspect ${ECR_URI}:${IMAGE_TAG} --format='{{index .RepoDigests 0}}' | cut -d'@' -f2)
echo -e "${GREEN}✅ Image digest: ${IMAGE_DIGEST}${NC}"

# Step 6: Summary
echo ""
echo -e "${GREEN}✅ Deployment Complete!${NC}"
echo "=================================="
echo "ECR Repository: ${ECR_URI}"
echo "Image Tags:"
echo "  - ${IMAGE_TAG}"
echo "  - ${VERSION_TAG}"
echo "Image Digest: ${IMAGE_DIGEST}"
echo ""
echo -e "${YELLOW}📋 Next Steps:${NC}"
echo "1. Update ECS task definition with new image:"
echo "   ${ECR_URI}@${IMAGE_DIGEST}"
echo ""
echo "2. Or use the latest tag:"
echo "   ${ECR_URI}:${IMAGE_TAG}"
echo ""
echo "3. Update ECS service:"
echo "   ./docker/update-ecs.sh"
echo ""





