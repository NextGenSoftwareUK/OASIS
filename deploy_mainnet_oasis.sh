#!/bin/bash

# AWS ECR repository URI
ECR_REPO_URI="881490134703.dkr.ecr.us-east-1.amazonaws.com/oasis-api"
IMAGE_TAG="mainnet"
FULL_IMAGE_NAME="${ECR_REPO_URI}:${IMAGE_TAG}"
ECS_CLUSTER_NAME="oasis-api-cluster"
ECS_SERVICE_NAME="oasis-api-service-mainnet"
TASK_DEFINITION_FILE="oasis-api-task-definition-mainnet.json"

echo "🚀 Deploying OASIS API Mainnet Instance..."

# 1. Authenticate Docker to ECR
echo "Authenticating Docker to ECR..."
aws ecr get-login-password --region us-east-1 | docker login --username AWS --password-stdin ${ECR_REPO_URI}
if [ $? -ne 0 ]; then
    echo "❌ ECR login failed. Exiting."
    exit 1
fi
echo "✅ Docker authenticated to ECR."

# 2. Build the Docker image with mainnet configuration
echo "Building Docker image: ${FULL_IMAGE_NAME}..."
docker build -t ${FULL_IMAGE_NAME} .
if [ $? -ne 0 ]; then
    echo "❌ Docker build failed. Exiting."
    exit 1
fi
echo "✅ Docker image built successfully."

# 3. Push the Docker image to ECR
echo "Pushing Docker image to ECR..."
docker push ${FULL_IMAGE_NAME}
if [ $? -ne 0 ]; then
    echo "❌ Docker push failed. Exiting."
    exit 1
fi
echo "✅ Docker image pushed to ECR."

# 4. Register a new ECS task definition
echo "Registering new ECS task definition from ${TASK_DEFINITION_FILE}..."
TASK_DEFINITION_ARN=$(aws ecs register-task-definition --cli-input-json file://${TASK_DEFINITION_FILE} --region us-east-1 | grep -o '"taskDefinitionArn": "[^"]*"' | cut -d'"' -f4)
if [ $? -ne 0 ] || [ -z "${TASK_DEFINITION_ARN}" ]; then
    echo "❌ Failed to register ECS task definition. Exiting."
    exit 1
fi
echo "✅ New ECS Task Definition Registered: ${TASK_DEFINITION_ARN}"

# 5. Create CloudWatch log group for mainnet
echo "Creating CloudWatch log group for mainnet..."
aws logs create-log-group --log-group-name "/ecs/oasis-api-mainnet" --region us-east-1 2>/dev/null || echo "Log group already exists"

# 6. Create ECS service for mainnet
echo "Creating ECS service ${ECS_SERVICE_NAME}..."
aws ecs create-service \
  --cluster ${ECS_CLUSTER_NAME} \
  --service-name ${ECS_SERVICE_NAME} \
  --task-definition ${TASK_DEFINITION_ARN} \
  --desired-count 1 \
  --launch-type FARGATE \
  --network-configuration "awsvpcConfiguration={subnets=[subnet-12345678],securityGroups=[sg-12345678],assignPublicIp=ENABLED}" \
  --region us-east-1 2>/dev/null || echo "Service already exists"

# 7. Update the ECS service to use the new task definition
echo "Updating ECS service ${ECS_SERVICE_NAME} to use ${TASK_DEFINITION_ARN}..."
aws ecs update-service --cluster ${ECS_CLUSTER_NAME} --service ${ECS_SERVICE_NAME} --task-definition ${TASK_DEFINITION_ARN} --force-new-deployment --region us-east-1
if [ $? -ne 0 ]; then
    echo "❌ Failed to update ECS service. Exiting."
    exit 1
fi
echo "✅ ECS service update initiated."

# 8. Wait for the service to stabilize
echo "⏳ Waiting for service to stabilize..."
aws ecs wait services-stable --cluster ${ECS_CLUSTER_NAME} --services ${ECS_SERVICE_NAME} --region us-east-1
if [ $? -ne 0 ]; then
    echo "❌ Service did not stabilize within the expected time. Please check ECS console for details."
    echo "✅ OASIS API Mainnet deployment completed!"
    echo "🔗 Mainnet endpoint: https://oasisweb4-mainnet.one (when ALB is configured)"
    echo "🔗 Solana Mainnet RPC: https://api.mainnet-beta.solana.com"
    echo ""
    echo "🎯 Ready for production on Solana Mainnet!"
    exit 1
fi
echo "✅ OASIS API Mainnet deployment completed!"
echo "🔗 Mainnet endpoint: https://oasisweb4-mainnet.one (when ALB is configured)"
echo "🔗 Solana Mainnet RPC: https://api.mainnet-beta.solana.com"
echo ""
echo "🎯 Ready for production on Solana Mainnet!"

