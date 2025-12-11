#!/bin/bash

# AWS ECR repository URI for NFT Mint Frontend
ECR_REPO_URI="881490134703.dkr.ecr.us-east-1.amazonaws.com/nft-mint-frontend"
IMAGE_TAG="latest"
FULL_IMAGE_NAME="${ECR_REPO_URI}:${IMAGE_TAG}"
ECS_CLUSTER_NAME="oasis-api-cluster"
ECS_SERVICE_NAME="nft-mint-frontend-service"
TASK_DEFINITION_FAMILY="nft-mint-frontend-task"

echo "🚀 Deploying NFT Mint Studio Frontend to nft.oasisweb4.one..."

# 1. Create ECR repository if it doesn't exist
echo "Checking if ECR repository exists..."
aws ecr describe-repositories --repository-names nft-mint-frontend --region us-east-1 2>/dev/null
if [ $? -ne 0 ]; then
    echo "Creating ECR repository..."
    aws ecr create-repository --repository-name nft-mint-frontend --region us-east-1
    echo "✅ ECR repository created."
else
    echo "✅ ECR repository already exists."
fi

# 2. Authenticate Docker to ECR
echo "Authenticating Docker to ECR..."
aws ecr get-login-password --region us-east-1 | docker login --username AWS --password-stdin 881490134703.dkr.ecr.us-east-1.amazonaws.com
if [ $? -ne 0 ]; then
    echo "❌ ECR login failed. Exiting."
    exit 1
fi
echo "✅ Docker authenticated to ECR."

# 3. Build the Docker image
echo "Building Docker image: ${FULL_IMAGE_NAME}..."
docker build -t ${FULL_IMAGE_NAME} .
if [ $? -ne 0 ]; then
    echo "❌ Docker build failed. Exiting."
    exit 1
fi
echo "✅ Docker image built successfully."

# 4. Push the Docker image to ECR
echo "Pushing Docker image to ECR..."
docker push ${FULL_IMAGE_NAME}
if [ $? -ne 0 ]; then
    echo "❌ Docker push failed. Exiting."
    exit 1
fi
echo "✅ Docker image pushed to ECR."

# 5. Register a new ECS task definition
echo "Registering new ECS task definition..."
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
      "name": "nft-mint-frontend",
      "image": "'"${FULL_IMAGE_NAME}"'",
      "essential": true,
      "portMappings": [
        {
          "containerPort": 3000,
          "protocol": "tcp"
        }
      ],
      "environment": [
        {
          "name": "NODE_ENV",
          "value": "production"
        },
        {
          "name": "NEXT_PUBLIC_OASIS_API_URL",
          "value": "https://oasisweb4.one"
        }
      ],
      "logConfiguration": {
        "logDriver": "awslogs",
        "options": {
          "awslogs-group": "/ecs/nft-mint-frontend",
          "awslogs-region": "us-east-1",
          "awslogs-stream-prefix": "ecs",
          "awslogs-create-group": "true"
        }
      }
    }
  ]' \
  --region us-east-1 | jq -r '.taskDefinition.taskDefinitionArn')

if [ $? -ne 0 ] || [ -z "${TASK_DEFINITION_ARN}" ]; then
    echo "❌ Failed to register ECS task definition. Exiting."
    exit 1
fi
echo "✅ New ECS Task Definition Registered: ${TASK_DEFINITION_ARN}"

# 6. Check if ECS service exists
aws ecs describe-services --cluster ${ECS_CLUSTER_NAME} --services ${ECS_SERVICE_NAME} --region us-east-1 | grep -q ACTIVE
SERVICE_EXISTS=$?

if [ $SERVICE_EXISTS -ne 0 ]; then
    # Create new service
    echo "Creating new ECS service ${ECS_SERVICE_NAME}..."
    
    # Get VPC and subnet info from existing OASIS API service
    OASIS_SERVICE_INFO=$(aws ecs describe-services --cluster ${ECS_CLUSTER_NAME} --services oasis-api-service --region us-east-1)
    SUBNETS=$(echo $OASIS_SERVICE_INFO | jq -r '.services[0].networkConfiguration.awsvpcConfiguration.subnets | join(",")')
    SECURITY_GROUPS=$(echo $OASIS_SERVICE_INFO | jq -r '.services[0].networkConfiguration.awsvpcConfiguration.securityGroups | join(",")')
    
    aws ecs create-service \
      --cluster ${ECS_CLUSTER_NAME} \
      --service-name ${ECS_SERVICE_NAME} \
      --task-definition ${TASK_DEFINITION_ARN} \
      --desired-count 1 \
      --launch-type FARGATE \
      --network-configuration "awsvpcConfiguration={subnets=[${SUBNETS}],securityGroups=[${SECURITY_GROUPS}],assignPublicIp=ENABLED}" \
      --load-balancers "targetGroupArn=arn:aws:elasticloadbalancing:us-east-1:881490134703:targetgroup/nft-frontend-tg/xxx,containerName=nft-mint-frontend,containerPort=3000" \
      --region us-east-1
    
    echo "✅ ECS service created."
else
    # Update existing service
    echo "Updating ECS service ${ECS_SERVICE_NAME} to use ${TASK_DEFINITION_ARN}..."
    aws ecs update-service --cluster ${ECS_CLUSTER_NAME} --service ${ECS_SERVICE_NAME} --task-definition ${TASK_DEFINITION_ARN} --force-new-deployment --region us-east-1
    if [ $? -ne 0 ]; then
        echo "❌ Failed to update ECS service. Exiting."
        exit 1
    fi
    echo "✅ ECS service update initiated."
fi

# 7. Wait for the service to stabilize
echo "⏳ Waiting for service to stabilize (this may take 5-10 minutes)..."
aws ecs wait services-stable --cluster ${ECS_CLUSTER_NAME} --services ${ECS_SERVICE_NAME} --region us-east-1
if [ $? -ne 0 ]; then
    echo "⚠️  Service did not stabilize within expected time. Check ECS console."
else
    echo "✅ Service stable!"
fi

echo ""
echo "✅ NFT Mint Studio Frontend deployment completed!"
echo ""
echo "🌐 Endpoints:"
echo "   Production: https://nft.oasisweb4.one (when DNS is configured)"
echo "   API Backend: https://oasisweb4.one"
echo ""
echo "📋 Next Steps:"
echo "   1. Configure DNS for nft.oasisweb4.one (use add_nft_subdomain.json)"
echo "   2. Update ALB target group and listener rules"
echo "   3. Test the frontend at https://nft.oasisweb4.one"
echo "   4. Verify NFT minting works end-to-end"
echo ""



