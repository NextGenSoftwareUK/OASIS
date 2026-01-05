#!/bin/bash
# OASIS Cleanup Script - Stops and removes containers/images
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

echo -e "\033[36mStopping and removing OASIS Services...\033[0m"

# Stop and remove containers and volumes
# Removed '--rmi local' to avoid "image name or ID" errors with manual builds
echo -e "\n\033[33m1. Stopping STAR API...\033[0m"
$COMPOSE -f docker/star-api/docker-compose.yml down --volumes

echo -e "\n\033[33m2. Stopping OASIS API...\033[0m"
$COMPOSE -f docker/oasis-api/docker-compose.yml down --volumes

echo -e "\n\033[33m3. Stopping Infrastructure...\033[0m"
$COMPOSE -f docker/infrastructure/docker-compose.yml down --volumes

# Remove custom images explicitly
echo -e "\n\033[33m4. Removing OASIS images...\033[0m"
$DOCKER rmi oasis-api:latest star-api:latest oasis-monorepo-base:latest 2>/dev/null || true

# Optional: Remove network
echo -e "\n\033[33m5. Removing network...\033[0m"
$DOCKER network rm oasis-network 2>/dev/null || true

echo -e "\n\033[32m--- OASIS Cleaned Up! ---\033[0m"
