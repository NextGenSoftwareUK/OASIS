#!/bin/bash

# Simple deployment script that forces devnet mode via environment variables
ECS_CLUSTER_NAME="oasis-api-cluster"
ECS_SERVICE_NAME="oasis-api-service"
TASK_DEFINITION_FAMILY="oasis-api-task"

echo "🚀 Deploying OASIS API with Devnet Override..."

# 1. Get the current task definition
echo "Getting current task definition for family ${TASK_DEFINITION_FAMILY}..."
CURRENT_TASK_DEFINITION=$(aws ecs describe-task-definition --task-definition ${TASK_DEFINITION_FAMILY} --region us-east-1)
if [ $? -ne 0 ]; then
    echo "❌ Failed to describe current task definition. Exiting."
    exit 1
fi

# 2. Extract container definitions and add devnet environment variables
CONTAINER_DEFINITIONS=$(echo ${CURRENT_TASK_DEFINITION} | jq '.taskDefinition.containerDefinitions')
UPDATED_CONTAINER_DEFINITIONS=$(echo ${CONTAINER_DEFINITIONS} | jq '
  .[0].environment = (
    .[0].environment | 
    map(select(.name != "OASIS_NETWORK")) |
    . + [
      {"name": "OASIS_NETWORK", "value": "devnet"},
      {"name": "ConnectionStrings__SolanaOASIS", "value": "https://api.devnet.solana.com"}
    ]
  )
')

# 3. Register a new ECS task definition with devnet environment variables
echo "Registering new ECS task definition with devnet configuration..."
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

# 4. Update the ECS service to use the new task definition
echo "Updating ECS service ${ECS_SERVICE_NAME} to use ${TASK_DEFINITION_ARN}..."
aws ecs update-service --cluster ${ECS_CLUSTER_NAME} --service ${ECS_SERVICE_NAME} --task-definition ${TASK_DEFINITION_ARN} --force-new-deployment --region us-east-1
if [ $? -ne 0 ]; then
    echo "❌ Failed to update ECS service. Exiting."
    exit 1
fi
echo "✅ ECS service update initiated."

# 5. Wait for the service to stabilize
echo "⏳ Waiting for service to stabilize..."
aws ecs wait services-stable --cluster ${ECS_CLUSTER_NAME} --services ${ECS_SERVICE_NAME} --region us-east-1
if [ $? -ne 0 ]; then
    echo "❌ Service did not stabilize within the expected time. Please check ECS console for details."
    echo "✅ OASIS API devnet override deployment completed!"
    echo ""
    echo "🔧 Configuration:"
    echo "   - OASIS_NETWORK=devnet"
    echo "   - Solana RPC: https://api.devnet.solana.com"
    echo ""
    echo "🎯 Ready for testing on Solana Devnet!"
    exit 1
fi
echo "✅ OASIS API devnet override deployment completed!"
echo ""
echo "🔧 Configuration:"
echo "   - OASIS_NETWORK=devnet"
echo "   - Solana RPC: https://api.devnet.solana.com"
echo ""
echo "🎯 Ready for testing on Solana Devnet!"

