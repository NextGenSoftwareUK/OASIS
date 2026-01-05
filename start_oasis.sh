#!/bin/bash
# OASIS Startup Script - Uses docker/ folder structure
# Auto-detect Podman
DOCKER="docker"
COMPOSE="docker-compose"

if ! command -v docker &> /dev/null && command -v podman &> /dev/null; then
    echo -e "\033[33mNote: Docker not found. Using Podman.\033[0m"
    DOCKER="podman"
fi

# Check for docker-compose or docker compose (v2) or podman-compose
if command -v docker-compose &> /dev/null; then
    COMPOSE="docker-compose"
elif docker compose version &> /dev/null; then
    COMPOSE="docker compose"
elif command -v podman-compose &> /dev/null; then
    echo -e "\033[33mNote: Docker-compose not found. Using Podman-compose.\033[0m"
    COMPOSE="podman-compose"
fi

echo -e "\033[36mStarting OASIS Infrastructure...\033[0m"

# 1. Create network if needed
echo -e "\n\033[33m1. Ensuring oasis-network exists...\033[0m"
if ! $DOCKER network inspect oasis-network >/dev/null 2>&1; then
    echo -e "\033[32mCreating network...\033[0m"
    $DOCKER network create oasis-network
fi

# 2. Build Base Image
echo -e "\n\033[33m2. Building Base Image...\033[0m"
$DOCKER build -f docker/base/Dockerfile -t oasis-monorepo-base:latest .
if [ $? -ne 0 ]; then echo -e "\033[31mBase build failed\033[0m"; exit 1; fi

# 3. Build OASIS API Image
echo -e "\n\033[33m3. Building OASIS API...\033[0m"
$DOCKER build -f docker/oasis-api/Dockerfile -t oasis-api:latest .
if [ $? -ne 0 ]; then echo -e "\033[31mOASIS API build failed\033[0m"; exit 1; fi

# 4. Build STAR API Image
echo -e "\n\033[33m4. Building STAR API...\033[0m"
$DOCKER build -f docker/star-api/Dockerfile -t star-api:latest .
if [ $? -ne 0 ]; then echo -e "\033[31mSTAR API build failed\033[0m"; exit 1; fi

# 5. Start Infrastructure (DBs)
echo -e "\n\033[33m5. Starting Infrastructure...\033[0m"
$COMPOSE -f docker/infrastructure/docker-compose.yml up -d

# 6. Start APIs (no build - images already exist)
echo -e "\n\033[33m6. Starting OASIS API...\033[0m"
$COMPOSE -f docker/oasis-api/docker-compose.yml up -d

echo -e "\n\033[33m7. Starting STAR API...\033[0m"
$COMPOSE -f docker/star-api/docker-compose.yml up -d

echo -e "\n\033[32m--- OASIS Started! ---\033[0m"
echo "OASIS API:     http://localhost:5002"
echo "STAR API:      http://localhost:5003"
echo "Mongo Express: http://localhost:8081"
