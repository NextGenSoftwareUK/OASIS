#!/bin/bash

# ECS Service Update Script
# This script updates the ECS service with the newly pushed Docker image
# Run from the OASIS_CLEAN root directory: ./docker/update-ecs.sh [image-tag]

set -e  # Exit on error

# Configuration
AWS_REGION="us-east-1"
CLUSTER_NAME="oasis-api-cluster"
SERVICE_NAME="oasis-api-service"
TASK_FAMILY="oasis-api-task"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}🔄 ECS Service Update${NC}"
echo "=========================="
echo "Cluster: ${CLUSTER_NAME}"
echo "Service: ${SERVICE_NAME}"
echo "Task Family: ${TASK_FAMILY}"
echo ""

# Check if AWS CLI is installed
if ! command -v aws &> /dev/null; then
    echo -e "${RED}❌ Error: AWS CLI is not installed. Please install it first.${NC}"
    exit 1
fi

# Check if jq is installed
if ! command -v jq &> /dev/null; then
    echo -e "${RED}❌ Error: jq is not installed. Please install it first (brew install jq).${NC}"
    exit 1
fi

# Check if image tag or digest is provided
if [ -z "$1" ]; then
    echo -e "${YELLOW}⚠️  No image tag/digest provided. Using 'latest' tag.${NC}"
    IMAGE_TAG="latest"
else
    IMAGE_TAG="$1"
fi

# Get the current task definition
echo -e "${YELLOW}📋 Step 1: Getting current task definition...${NC}"
TASK_DEF=$(aws ecs describe-task-definition \
    --task-definition ${TASK_FAMILY} \
    --region ${AWS_REGION} \
    --query 'taskDefinition' \
    --output json)

if [ -z "$TASK_DEF" ]; then
    echo -e "${RED}❌ Failed to get task definition${NC}"
    exit 1
fi

echo -e "${GREEN}✅ Retrieved task definition${NC}"

# Extract image URI from task definition
CURRENT_IMAGE=$(echo $TASK_DEF | jq -r '.containerDefinitions[0].image')
echo "Current image: ${CURRENT_IMAGE}"

# Determine new image URI
if [[ "$IMAGE_TAG" == *"@"* ]] || [[ "$IMAGE_TAG" == *"sha256:"* ]]; then
    # Image digest provided
    NEW_IMAGE="881490134703.dkr.ecr.us-east-1.amazonaws.com/oasis-api@${IMAGE_TAG}"
else
    # Image tag provided
    NEW_IMAGE="881490134703.dkr.ecr.us-east-1.amazonaws.com/oasis-api:${IMAGE_TAG}"
fi

echo "New image: ${NEW_IMAGE}"

# Update task definition with new image
echo ""
echo -e "${YELLOW}📋 Step 2: Updating task definition with new image...${NC}"
NEW_TASK_DEF=$(echo $TASK_DEF | jq --arg IMAGE "$NEW_IMAGE" '.containerDefinitions[0].image = $IMAGE | del(.taskDefinitionArn) | del(.revision) | del(.status) | del(.requiresAttributes) | del(.compatibilities) | del(.registeredAt) | del(.registeredBy)')

# Register new task definition
echo -e "${YELLOW}📋 Step 3: Registering new task definition...${NC}"
# Use temporary file to avoid JSON parsing issues with stdin
TEMP_TASK_DEF="/tmp/new-task-def-$(date +%s).json"
echo $NEW_TASK_DEF > $TEMP_TASK_DEF
NEW_TASK_DEF_ARN=$(aws ecs register-task-definition \
    --region ${AWS_REGION} \
    --cli-input-json file://$TEMP_TASK_DEF \
    --query 'taskDefinition.taskDefinitionArn' \
    --output text)
rm -f $TEMP_TASK_DEF

if [ -z "$NEW_TASK_DEF_ARN" ]; then
    echo -e "${RED}❌ Failed to register new task definition${NC}"
    exit 1
fi

echo -e "${GREEN}✅ New task definition registered: ${NEW_TASK_DEF_ARN}${NC}"

# Update ECS service
echo ""
echo -e "${YELLOW}📋 Step 4: Updating ECS service...${NC}"
aws ecs update-service \
    --cluster ${CLUSTER_NAME} \
    --service ${SERVICE_NAME} \
    --task-definition ${NEW_TASK_DEF_ARN} \
    --force-new-deployment \
    --region ${AWS_REGION} > /dev/null

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✅ Service update initiated${NC}"
else
    echo -e "${RED}❌ Failed to update service${NC}"
    exit 1
fi

# Wait for service to stabilize (optional)
echo ""
echo -e "${YELLOW}📋 Step 5: Waiting for service to stabilize...${NC}"
echo "This may take a few minutes. Press Ctrl+C to skip waiting."
aws ecs wait services-stable \
    --cluster ${CLUSTER_NAME} \
    --services ${SERVICE_NAME} \
    --region ${AWS_REGION} 2>/dev/null || echo -e "${YELLOW}⚠️  Service stabilization check skipped or timed out${NC}"

echo ""
echo -e "${GREEN}✅ ECS Service Update Complete!${NC}"
echo "=================================="
echo "New Task Definition: ${NEW_TASK_DEF_ARN}"
echo "Service: ${SERVICE_NAME}"
echo "Cluster: ${CLUSTER_NAME}"
echo ""
echo "You can check the service status with:"
echo "  aws ecs describe-services --cluster ${CLUSTER_NAME} --services ${SERVICE_NAME} --region ${AWS_REGION}"
echo ""





