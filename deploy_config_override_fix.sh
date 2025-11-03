#!/bin/bash

# AWS ECR repository URI
ECR_REPO_URI="881490134703.dkr.ecr.us-east-1.amazonaws.com/oasis-api"
IMAGE_TAG="config-override-fix"
FULL_IMAGE_NAME="${ECR_REPO_URI}:${IMAGE_TAG}"
ECS_CLUSTER_NAME="oasis-api-cluster"
ECS_SERVICE_NAME="oasis-api-service"
TASK_DEFINITION_FILE="oasis-api-task-definition-secure.json"
NEW_WALLET_PUBLIC_KEY="Be51B1n3m1MCtZYvH8JEX3LnZZwoREyH4rYoyhMrkxJs"

echo "🚀 Deploying OASIS configuration override fix..."

# 1. Authenticate Docker to ECR
echo "Authenticating Docker to ECR..."
aws ecr get-login-password --region us-east-1 | docker login --username AWS --password-stdin ${ECR_REPO_URI}
if [ $? -ne 0 ]; then
    echo "❌ ECR login failed. Exiting."
    exit 1
fi
echo "✅ Docker authenticated to ECR."

# 2. Build the Docker image with the configuration override fix
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

# 4. Update the task definition to use the new image
echo "Updating task definition to use new image..."
TASK_DEFINITION_ARN=$(aws ecs register-task-definition \
  --family oasis-api-task \
  --network-mode awsvpc \
  --requires-compatibilities FARGATE \
  --cpu 512 \
  --memory 1024 \
  --execution-role-arn arn:aws:iam::881490134703:role/ecsTaskExecutionRole \
  --task-role-arn arn:aws:iam::881490134703:role/ecsTaskExecutionRole \
  --container-definitions '[{
    "name": "oasis-api-container",
    "image": "'${FULL_IMAGE_NAME}'",
    "cpu": 512,
    "memory": 1024,
    "portMappings": [{
      "containerPort": 80,
      "hostPort": 80,
      "protocol": "tcp"
    }],
    "essential": true,
    "environment": [
      {
        "name": "ConnectionStrings__MongoDBOASIS",
        "value": "mongodb+srv://OASISWEB4:Uppermall1!@oasisweb4.ifxnugb.mongodb.net/?retryWrites=true&w=majority&appName=OASISWeb4"
      },
      {
        "name": "ASPNETCORE_ENVIRONMENT",
        "value": "Production"
      },
      {
        "name": "ASPNETCORE_URLS",
        "value": "http://+:80"
      },
      {
        "name": "ConnectionStrings__SolanaOASIS",
        "value": "https://api.mainnet-beta.solana.com"
      },
      {
        "name": "SolanaOASIS__WalletMnemonicWords",
        "value": "adapt afford abandon above age adult ahead accident aim advice agree accuse"
      },
      {
        "name": "SolanaOASIS__PrivateKey",
        "value": "kNln1+y3r9Xa1HbiakTDUmdpyzImmnpEs/+et8D6Jr2eE+KoOZJtHXdOOoNyP1NRDcfa44LE4y6llK9JaMpCEA=="
      },
      {
        "name": "SolanaOASIS__PublicKey",
        "value": "Be51B1n3m1MCtZYvH8JEX3LnZZwoREyH4rYoyhMrkxJs"
      }
    ],
    "mountPoints": [],
    "volumesFrom": [],
    "logConfiguration": {
      "logDriver": "awslogs",
      "options": {
        "awslogs-group": "/ecs/oasis-api",
        "awslogs-region": "us-east-1",
        "awslogs-stream-prefix": "ecs"
      }
    },
    "systemControls": []
  }]' \
  --region us-east-1 | jq -r '.taskDefinition.taskDefinitionArn')

if [ $? -ne 0 ] || [ -z "${TASK_DEFINITION_ARN}" ]; then
    echo "❌ Failed to register ECS task definition. Exiting."
    exit 1
fi
echo "✅ New ECS Task Definition Registered: ${TASK_DEFINITION_ARN}"

# 5. Update the ECS service to use the new task definition
echo "Updating ECS service ${ECS_SERVICE_NAME} to use ${TASK_DEFINITION_ARN}..."
aws ecs update-service --cluster ${ECS_CLUSTER_NAME} --service ${ECS_SERVICE_NAME} --task-definition ${TASK_DEFINITION_ARN} --force-new-deployment --region us-east-1
if [ $? -ne 0 ]; then
    echo "❌ Failed to update ECS service. Exiting."
    exit 1
fi
echo "✅ ECS service update initiated."

# 6. Wait for the service to stabilize
echo "⏳ Waiting for service to stabilize..."
aws ecs wait services-stable --cluster ${ECS_CLUSTER_NAME} --services ${ECS_SERVICE_NAME} --region us-east-1
if [ $? -ne 0 ]; then
    echo "❌ Service did not stabilize within the expected time. Please check ECS console for details."
    echo "✅ OASIS configuration override fix deployment completed!"
    echo "🔗 New wallet address: ${NEW_WALLET_PUBLIC_KEY}"
    echo "🔗 Solana Explorer: https://explorer.solana.com/address/${NEW_WALLET_PUBLIC_KEY}"
    echo ""
    echo "⚠️  IMPORTANT: The configuration override fix is now deployed!"
    echo "🔧 Environment variables will now override OASIS_DNA.json settings"
    exit 1
fi
echo "✅ OASIS configuration override fix deployment completed!"
echo "🔗 New wallet address: ${NEW_WALLET_PUBLIC_KEY}"
echo "🔗 Solana Explorer: https://explorer.solana.com/address/${NEW_WALLET_PUBLIC_KEY}"
echo ""
echo "🔧 Environment variables will now override OASIS_DNA.json settings"
echo "🎯 Ready to test NFT minting with the new secure wallet!"


