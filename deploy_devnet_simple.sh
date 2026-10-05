#!/bin/bash

# Simple deployment script that forces devnet mode via environment variables
ECS_CLUSTER_NAME="oasis-api-cluster"
ECS_SERVICE_NAME="oasis-api-service"
TASK_DEFINITION_FAMILY="oasis-api-task"

echo "🚀 Deploying OASIS API with Devnet Override..."

# 1. Register a new ECS task definition with devnet environment variables
echo "Registering new ECS task definition with devnet configuration..."
TASK_DEFINITION_ARN=$(aws ecs register-task-definition \
  --family ${TASK_DEFINITION_FAMILY} \
  --network-mode awsvpc \
  --requires-compatibilities FARGATE \
  --cpu 512 \
  --memory 1024 \
  --execution-role-arn arn:aws:iam::881490134703:role/ecsTaskExecutionRole \
  --task-role-arn arn:aws:iam::881490134703:role/ecsTaskExecutionRole \
  --container-definitions '[
    {
      "name": "oasis-api-container",
      "image": "881490134703.dkr.ecr.us-east-1.amazonaws.com/oasis-api:latest",
      "cpu": 512,
      "memory": 1024,
      "portMappings": [
        {
          "containerPort": 80,
          "hostPort": 80,
          "protocol": "tcp"
        }
      ],
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
          "name": "OASIS_NETWORK",
          "value": "devnet"
        },
        {
          "name": "ConnectionStrings__SolanaOASIS",
          "value": "https://api.devnet.solana.com"
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
    }
  ]' \
  --region us-east-1 | grep -o '"taskDefinitionArn": "[^"]*"' | cut -d'"' -f4)

if [ $? -ne 0 ] || [ -z "${TASK_DEFINITION_ARN}" ]; then
    echo "❌ Failed to register ECS task definition. Exiting."
    exit 1
fi
echo "✅ New ECS Task Definition Registered: ${TASK_DEFINITION_ARN}"

# 2. Update the ECS service to use the new task definition
echo "Updating ECS service ${ECS_SERVICE_NAME} to use ${TASK_DEFINITION_ARN}..."
aws ecs update-service --cluster ${ECS_CLUSTER_NAME} --service ${ECS_SERVICE_NAME} --task-definition ${TASK_DEFINITION_ARN} --force-new-deployment --region us-east-1
if [ $? -ne 0 ]; then
    echo "❌ Failed to update ECS service. Exiting."
    exit 1
fi
echo "✅ ECS service update initiated."

# 3. Wait for the service to stabilize
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

