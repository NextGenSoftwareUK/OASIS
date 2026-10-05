#!/bin/bash

# AWS ECR repository URI
ECR_REPO_URI="881490134703.dkr.ecr.us-east-1.amazonaws.com/oasis-api"
IMAGE_TAG="subdomain-config"
FULL_IMAGE_NAME="${ECR_REPO_URI}:${IMAGE_TAG}"
ECS_CLUSTER_NAME="oasis-api-cluster"
ECS_SERVICE_NAME="oasis-api-service"
TASK_DEFINITION_FAMILY="oasis-api-task"

echo "🚀 Deploying OASIS API with Subdomain Configuration..."

# 1. Authenticate Docker to ECR
echo "Authenticating Docker to ECR..."
aws ecr get-login-password --region us-east-1 | docker login --username AWS --password-stdin ${ECR_REPO_URI}
if [ $? -ne 0 ]; then
    echo "❌ ECR login failed. Exiting."
    exit 1
fi
echo "✅ Docker authenticated to ECR."

# 2. Build the Docker image with subdomain configuration
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

# 4. Get the current task definition
echo "Getting current task definition for family ${TASK_DEFINITION_FAMILY}..."
CURRENT_TASK_DEFINITION=$(aws ecs describe-task-definition --task-definition ${TASK_DEFINITION_FAMILY} --region us-east-1)
if [ $? -ne 0 ]; then
    echo "❌ Failed to describe current task definition. Exiting."
    exit 1
fi

# 5. Extract container definitions and update image
CONTAINER_DEFINITIONS=$(echo ${CURRENT_TASK_DEFINITION} | jq '.taskDefinition.containerDefinitions')
UPDATED_CONTAINER_DEFINITIONS=$(echo ${CONTAINER_DEFINITIONS} | jq --arg IMAGE "${FULL_IMAGE_NAME}" '.[0].image = $IMAGE')

# 6. Register a new ECS task definition with the updated image
echo "Registering new ECS task definition..."
TASK_DEFINITION_ARN=$(aws ecs register-task-definition \
  --family ${TASK_DEFINITION_FAMILY} \
  --network-mode awsvpc \
  --requires-compatibilities FARGATE \
  --cpu 512 \
  --memory 1024 \
  --execution-role-arn arn:aws:iam::881490134703:role/ecsTaskExecutionRole \
  --task-role-arn arn:aws:iam::881490134703:role/ecsTaskExecutionRole \
  --container-definitions "${UPDATED_CONTAINER_DEFINITIONS}" \
  --region us-east-1 | grep -o '"taskDefinitionArn": "[^"]*"' | cut -d'"' -f4)

if [ $? -ne 0 ] || [ -z "${TASK_DEFINITION_ARN}" ]; then
    echo "❌ Failed to register ECS task definition. Exiting."
    exit 1
fi
echo "✅ New ECS Task Definition Registered: ${TASK_DEFINITION_ARN}"

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
    echo "✅ OASIS API subdomain configuration deployment completed!"
    echo ""
    echo "🌐 Endpoints:"
    echo "   Mainnet: https://oasisweb4.one"
    echo "   Devnet:  https://devnet.oasisweb4.one (when DNS is configured)"
    echo ""
    echo "🔧 Configuration:"
    echo "   - OASIS_DNA.json (mainnet)"
    echo "   - OASIS_DNA_devnet.json (devnet)"
    echo "   - Network detection via hostname"
    echo ""
    echo "📋 Next Steps:"
    echo "   1. Configure DNS for devnet.oasisweb4.one"
    echo "   2. Update ALB listener rules if needed"
    echo "   3. Test both endpoints"
    exit 1
fi
echo "✅ OASIS API subdomain configuration deployment completed!"
echo ""
echo "🌐 Endpoints:"
echo "   Mainnet: https://oasisweb4.one"
echo "   Devnet:  https://devnet.oasisweb4.one (when DNS is configured)"
echo ""
echo "🔧 Configuration:"
echo "   - OASIS_DNA.json (mainnet)"
echo "   - OASIS_DNA_devnet.json (devnet)"
echo "   - Network detection via hostname"
echo ""
echo "📋 Next Steps:"
echo "   1. Configure DNS for devnet.oasisweb4.one"
echo "   2. Update ALB listener rules if needed"
echo "   3. Test both endpoints"

