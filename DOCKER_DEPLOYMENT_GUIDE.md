# OASIS API Docker Deployment Guide

## Overview
This guide explains how to build and deploy the OASIS API to AWS ECR and ECS.

## Prerequisites

1. **Docker** - Must be installed and running
2. **AWS CLI** - Must be installed and configured with appropriate credentials
3. **AWS Permissions** - Need permissions for:
   - ECR (Elastic Container Registry)
   - ECS (Elastic Container Service)
   - IAM (for task execution roles)

## Quick Start

### Step 1: Build and Push Docker Image

```bash
cd /Volumes/Storage/OASIS_CLEAN
./deploy-docker.sh
```

This script will:
- ✅ Authenticate with AWS ECR
- ✅ Check/create ECR repository
- ✅ Build Docker image with latest code
- ✅ Tag image with `latest` and version timestamp
- ✅ Push image to ECR

### Step 2: Update ECS Service (Optional)

After pushing the image, update the ECS service to use the new image:

```bash
# Using latest tag
./update-ecs-service.sh latest

# Using specific version tag
./update-ecs-service.sh v20250122-143000

# Using image digest (most specific)
./update-ecs-service.sh sha256:179026ff5404c86f135d83120f842418b7749e0bfb7b01b7dd8d7fc8892f21fe
```

## Manual Deployment Steps

### 1. Authenticate with AWS ECR

```bash
aws ecr get-login-password --region us-east-1 | \
  docker login --username AWS --password-stdin \
  881490134703.dkr.ecr.us-east-1.amazonaws.com
```

### 2. Build Docker Image

```bash
# Using production Dockerfile (recommended)
docker build -f Dockerfile.production -t oasis-api:latest .

# Or using ONODE Dockerfile
docker build -f ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Dockerfile \
  -t oasis-api:latest ONODE
```

### 3. Tag Image for ECR

```bash
docker tag oasis-api:latest \
  881490134703.dkr.ecr.us-east-1.amazonaws.com/oasis-api:latest
```

### 4. Push to ECR

```bash
docker push 881490134703.dkr.ecr.us-east-1.amazonaws.com/oasis-api:latest
```

### 5. Get Image Digest

```bash
docker inspect 881490134703.dkr.ecr.us-east-1.amazonaws.com/oasis-api:latest \
  --format='{{index .RepoDigests 0}}'
```

## Dockerfile Options

### Production Dockerfile (`Dockerfile.production`)
- ✅ Optimized for AWS ECS Fargate
- ✅ Uses port 80 (matches ECS task definition)
- ✅ Includes all necessary dependencies
- ✅ Multi-stage build for smaller image size
- **Recommended for production deployments**

### ONODE Dockerfile (`ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Dockerfile`)
- Uses port 8080 (can be overridden with environment variable)
- Simpler build context
- Good for local development

### Root Dockerfile (`Dockerfile`)
- Builds STAR WebAPI (different project)
- Not used for OASIS API ONODE deployment

## ECS Configuration

### Current Task Definition
- **Family**: `oasis-api-task`
- **Cluster**: `oasis-api-cluster`
- **Service**: `oasis-api-service`
- **Container Port**: 80
- **CPU**: 512
- **Memory**: 1024 MB

### Environment Variables
- `ASPNETCORE_ENVIRONMENT=Production`
- `ASPNETCORE_URLS=http://+:80`
- `ConnectionStrings__MongoDBOASIS=mongodb+srv://...`

## Updating ECS Task Definition

### Option 1: Using Script (Recommended)

```bash
./update-ecs-service.sh latest
```

### Option 2: Manual Update

1. Get current task definition:
```bash
aws ecs describe-task-definition \
  --task-definition oasis-api-task \
  --region us-east-1 \
  --query 'taskDefinition' > current-task-def.json
```

2. Edit `current-task-def.json` to update the image:
```json
{
  "containerDefinitions": [{
    "image": "881490134703.dkr.ecr.us-east-1.amazonaws.com/oasis-api:latest"
  }]
}
```

3. Register new task definition:
```bash
aws ecs register-task-definition \
  --cli-input-json file://current-task-def.json \
  --region us-east-1
```

4. Update service:
```bash
aws ecs update-service \
  --cluster oasis-api-cluster \
  --service oasis-api-service \
  --task-definition oasis-api-task \
  --force-new-deployment \
  --region us-east-1
```

## Verification

### Check ECR Image
```bash
aws ecr describe-images \
  --repository-name oasis-api \
  --region us-east-1
```

### Check ECS Service Status
```bash
aws ecs describe-services \
  --cluster oasis-api-cluster \
  --services oasis-api-service \
  --region us-east-1
```

### Check Running Tasks
```bash
aws ecs list-tasks \
  --cluster oasis-api-cluster \
  --service-name oasis-api-service \
  --region us-east-1
```

### View Logs
```bash
aws logs tail /ecs/oasis-api --follow --region us-east-1
```

## Troubleshooting

### Build Fails
- Check Docker is running: `docker info`
- Verify all project files are present
- Check for .NET SDK version compatibility

### Push Fails
- Verify AWS credentials: `aws sts get-caller-identity`
- Check ECR repository exists
- Ensure IAM permissions for ECR

### ECS Service Not Updating
- Check task definition was registered successfully
- Verify service is in correct cluster
- Check CloudWatch logs for errors

### Container Not Starting
- Check environment variables are set correctly
- Verify MongoDB connection string
- Check health check endpoint is accessible

## Image Tags

The deployment script creates two tags:
- `latest` - Always points to most recent build
- `v{timestamp}` - Versioned tag (e.g., `v20250122-143000`)

**Recommendation**: Use versioned tags or image digests for production deployments to ensure reproducibility.

## Image Digest

For maximum specificity, use image digests:
```bash
IMAGE_DIGEST=$(docker inspect 881490134703.dkr.ecr.us-east-1.amazonaws.com/oasis-api:latest \
  --format='{{index .RepoDigests 0}}' | cut -d'@' -f2)

echo "Image: 881490134703.dkr.ecr.us-east-1.amazonaws.com/oasis-api@${IMAGE_DIGEST}"
```

## Best Practices

1. **Always test locally first**: Build and run the Docker image locally before pushing
2. **Use versioned tags**: Don't rely solely on `latest` tag
3. **Monitor deployments**: Watch CloudWatch logs during deployment
4. **Rollback plan**: Keep previous task definition revisions
5. **Health checks**: Ensure health check endpoint is working
6. **Resource limits**: Monitor CPU and memory usage

## Local Testing

Before deploying to AWS, test the Docker image locally:

```bash
# Build image
docker build -f Dockerfile.production -t oasis-api:test .

# Run container
docker run -p 5000:80 \
  -e "ConnectionStrings__MongoDBOASIS=mongodb+srv://..." \
  oasis-api:test

# Test API
curl http://localhost:5000/swagger/index.html
```

## Next Steps

After successful deployment:
1. ✅ Verify API is accessible at `https://oasisweb4.one`
2. ✅ Test NFT minting functionality
3. ✅ Monitor CloudWatch logs for errors
4. ✅ Check ECS service metrics

---

**Last Updated**: January 2025
**Maintained By**: OASIS Development Team
