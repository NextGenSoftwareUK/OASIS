#!/bin/bash

# Deploy Secure OASIS Configuration
# This script builds and deploys the OASIS API with the new secure wallet configuration

echo "🔐 Deploying Secure OASIS Configuration"
echo "========================================"

# Set variables
AWS_REGION="us-east-1"
ECR_REPOSITORY="881490134703.dkr.ecr.us-east-1.amazonaws.com/oasis-api"
IMAGE_TAG="secure-wallet"
CLUSTER_NAME="oasis-api-cluster"
SERVICE_NAME="oasis-api-service"

echo "📍 Building Docker image with secure wallet configuration..."

# Build the Docker image
docker build -t $ECR_REPOSITORY:$IMAGE_TAG .

echo "🔑 Logging into ECR..."
aws ecr get-login-password --region $AWS_REGION | docker login --username AWS --password-stdin $ECR_REPOSITORY

echo "📤 Pushing image to ECR..."
docker push $ECR_REPOSITORY:$IMAGE_TAG

echo "📋 Registering new task definition..."
TASK_DEFINITION_ARN=$(aws ecs register-task-definition \
    --cli-input-json file://oasis-api-task-definition-secure.json \
    --query 'taskDefinition.taskDefinitionArn' \
    --output text)

echo "🔄 Updating ECS service..."
aws ecs update-service \
    --cluster $CLUSTER_NAME \
    --service $SERVICE_NAME \
    --task-definition $TASK_DEFINITION_ARN

echo "⏳ Waiting for service to stabilize..."
aws ecs wait services-stable \
    --cluster $CLUSTER_NAME \
    --services $SERVICE_NAME

echo "✅ Secure OASIS deployment completed!"
echo "🔗 New wallet address: Be51B1n3m1MCtZYvH8JEX3LnZZwoREyH4rYoyhMrkxJs"
echo "🔗 Solana Explorer: https://explorer.solana.com/address/Be51B1n3m1MCtZYvH8JEX3LnZZwoREyH4rYoyhMrkxJs"
echo ""
echo "⚠️  IMPORTANT: Fund the new wallet with SOL before testing NFT minting!"
echo "💰 Recommended initial funding: 0.1 SOL for testing"


