#!/bin/bash

# Configuration
AWS_REGION="us-east-1"
AWS_ACCOUNT_ID="881490134703"
ECR_REPOSITORY="assetrail-frontend"
ECS_CLUSTER="oasis-api-cluster"
ECS_SERVICE="assetrail-frontend-service"
TASK_DEFINITION_FAMILY="assetrail-frontend"

echo "🚀 Deploying Asset Rail Frontend to ECS..."

# Build Next.js app
echo "🏗️ Building Next.js app..."
cd ../smart-contract-ui
npm install
npm run build
cd ../frontend-deployment

# Copy built files to deployment directory
echo "📋 Copying built files..."
# Remove everything except Dockerfile and nginx.conf
find . -maxdepth 1 -not -name 'Dockerfile' -not -name 'nginx.conf' -not -name 'deploy.sh' -not -name 'ecs-task-definition.json' -not -name 'cloudformation-template.yaml' -delete
cp -R ../smart-contract-ui/out/* .

# Build and push Docker image
echo "📦 Building Docker image..."
docker build -t $ECR_REPOSITORY .

echo "🏷️ Tagging image..."
docker tag $ECR_REPOSITORY:latest $AWS_ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com/$ECR_REPOSITORY:latest

echo "🔐 Logging into ECR..."
aws ecr get-login-password --region $AWS_REGION | docker login --username AWS --password-stdin $AWS_ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com

echo "⬆️ Pushing image to ECR..."
docker push $AWS_ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com/$ECR_REPOSITORY:latest

# Update ECS service
echo "🔄 Updating ECS service..."
aws ecs update-service \
    --cluster $ECS_CLUSTER \
    --service $ECS_SERVICE \
    --force-new-deployment

echo "✅ Deployment complete!"
echo "🌐 Your frontend should be available at: https://assetrail.xyz"
