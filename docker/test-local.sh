#!/bin/bash

# Test script to run the Docker image locally and verify it works
# This simulates how the API runs in production

set -e

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

echo "🧪 Testing OASIS API Docker Image Locally"
echo "=========================================="
echo ""

# Configuration
ECR_REPO="881490134703.dkr.ecr.us-east-1.amazonaws.com/oasis-api"
IMAGE_TAG="${1:-latest}"
CONTAINER_NAME="oasis-api-test"
LOCAL_PORT=8080
API_URL="http://localhost:${LOCAL_PORT}"

# Step 1: Pull the image (or use local if already built)
echo "📥 Step 1: Pulling Docker image..."
if docker pull "${ECR_REPO}:${IMAGE_TAG}" 2>/dev/null; then
    echo -e "${GREEN}✅ Successfully pulled ${ECR_REPO}:${IMAGE_TAG}${NC}"
else
    echo -e "${YELLOW}⚠️  Could not pull from ECR, using local image if available${NC}"
    # Try to use local image
    if ! docker images | grep -q "oasis-api.*${IMAGE_TAG}"; then
        echo -e "${RED}❌ Image not found locally. Please build it first or ensure you're logged into ECR.${NC}"
        exit 1
    fi
fi

# Step 2: Stop and remove existing container if running
echo ""
echo "🧹 Step 2: Cleaning up existing containers..."
docker stop "${CONTAINER_NAME}" 2>/dev/null || true
docker rm "${CONTAINER_NAME}" 2>/dev/null || true
echo -e "${GREEN}✅ Cleanup complete${NC}"

# Step 3: Run the container
echo ""
echo "🚀 Step 3: Starting container..."
echo "   Container name: ${CONTAINER_NAME}"
echo "   Local port: ${LOCAL_PORT}"
echo "   API URL: ${API_URL}"

# Check if OASIS_DNA.json exists locally to copy it
DNA_COPY=""
if [ -f "OASIS_DNA.json" ]; then
    echo "   Will copy OASIS_DNA.json into container after startup"
    DNA_COPY="true"
fi

docker run -d \
    --name "${CONTAINER_NAME}" \
    -p "${LOCAL_PORT}:80" \
    -e ASPNETCORE_ENVIRONMENT=Production \
    "${ECR_REPO}:${IMAGE_TAG}"

# Copy OASIS_DNA.json into container if it exists locally
if [ "$DNA_COPY" = "true" ]; then
    echo "   Copying OASIS_DNA.json into container..."
    sleep 2  # Wait for container to be ready
    docker cp OASIS_DNA.json "${CONTAINER_NAME}:/app/OASIS_DNA.json"
fi

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✅ Container started successfully${NC}"
else
    echo -e "${RED}❌ Failed to start container${NC}"
    exit 1
fi

# Step 4: Wait for API to be ready
echo ""
echo "⏳ Step 4: Waiting for API to be ready..."
MAX_WAIT=60
WAIT_TIME=0
while [ $WAIT_TIME -lt $MAX_WAIT ]; do
    if curl -s -f "${API_URL}/swagger/index.html" > /dev/null 2>&1; then
        echo -e "${GREEN}✅ API is ready!${NC}"
        break
    fi
    echo -n "."
    sleep 2
    WAIT_TIME=$((WAIT_TIME + 2))
done

if [ $WAIT_TIME -ge $MAX_WAIT ]; then
    echo ""
    echo -e "${RED}❌ API did not become ready within ${MAX_WAIT} seconds${NC}"
    echo "Container logs:"
    docker logs "${CONTAINER_NAME}" --tail 50
    exit 1
fi

# Step 5: Test endpoints
echo ""
echo "🧪 Step 5: Testing API endpoints..."
echo ""

# Test 1: Swagger UI
echo "Test 1: Swagger UI"
if curl -s -f "${API_URL}/swagger/index.html" > /dev/null; then
    echo -e "${GREEN}✅ Swagger UI accessible${NC}"
else
    echo -e "${RED}❌ Swagger UI not accessible${NC}"
fi

# Test 2: Health check (if available)
echo ""
echo "Test 2: Health check"
HEALTH_RESPONSE=$(curl -s "${API_URL}/api/health" 2>/dev/null || echo "")
if [ -n "$HEALTH_RESPONSE" ]; then
    echo -e "${GREEN}✅ Health endpoint responded${NC}"
    echo "   Response: $HEALTH_RESPONSE"
else
    echo -e "${YELLOW}⚠️  Health endpoint not available (this is OK)${NC}"
fi

# Test 3: API version/info endpoint
echo ""
echo "Test 3: API Info"
INFO_RESPONSE=$(curl -s "${API_URL}/api/settings/version" 2>/dev/null || echo "")
if [ -n "$INFO_RESPONSE" ]; then
    echo -e "${GREEN}✅ API info endpoint responded${NC}"
    echo "   Response: $INFO_RESPONSE"
else
    echo -e "${YELLOW}⚠️  API info endpoint not available${NC}"
fi

# Test 4: Avatar endpoint (list)
echo ""
echo "Test 4: Avatar endpoint (GET /api/avatar)"
AVATAR_RESPONSE=$(curl -s -w "\nHTTP_CODE:%{http_code}" "${API_URL}/api/avatar" 2>/dev/null || echo "")
HTTP_CODE=$(echo "$AVATAR_RESPONSE" | grep "HTTP_CODE" | cut -d: -f2)
if [ "$HTTP_CODE" = "200" ] || [ "$HTTP_CODE" = "401" ] || [ "$HTTP_CODE" = "404" ]; then
    echo -e "${GREEN}✅ Avatar endpoint responded (HTTP ${HTTP_CODE})${NC}"
    if [ "$HTTP_CODE" = "200" ]; then
        echo "   Response preview: $(echo "$AVATAR_RESPONSE" | head -n 1 | cut -c1-100)..."
    fi
else
    echo -e "${YELLOW}⚠️  Avatar endpoint returned unexpected status: ${HTTP_CODE}${NC}"
fi

# Step 6: Show container logs
echo ""
echo "📋 Step 6: Recent container logs (last 20 lines):"
echo "----------------------------------------"
docker logs "${CONTAINER_NAME}" --tail 20

# Step 7: Summary
echo ""
echo "=========================================="
echo -e "${GREEN}✅ Testing Complete!${NC}"
echo ""
echo "Container Information:"
echo "  Name: ${CONTAINER_NAME}"
echo "  Status: $(docker ps --filter name=${CONTAINER_NAME} --format '{{.Status}}')"
echo ""
echo "API Access:"
echo "  Swagger UI: ${API_URL}/swagger"
echo "  API Base: ${API_URL}/api"
echo ""
echo "Useful Commands:"
echo "  View logs:     docker logs -f ${CONTAINER_NAME}"
echo "  Stop container: docker stop ${CONTAINER_NAME}"
echo "  Remove container: docker rm ${CONTAINER_NAME}"
echo "  Shell into container: docker exec -it ${CONTAINER_NAME} /bin/bash"
echo ""

