# Docker Update Guide for AI Agents

## Overview

This guide provides step-by-step instructions for AI agents to update the OASIS API Docker image and deploy it to AWS ECS. Follow these steps whenever code changes are made that require a new Docker build.

---

## Quick Reference: Complete Update Process

```bash
# 1. Navigate to project root
cd /Volumes/Storage/OASIS_CLEAN

# 2. Build and push Docker image to AWS ECR
./docker/deploy.sh

# 3. Update ECS service to use new image
./docker/update-ecs.sh
```

**That's it!** The scripts handle everything automatically.

---

## Detailed Process

### Step 1: Build and Push Docker Image

**Script**: `./docker/deploy.sh`

**What it does**:
1. Authenticates with AWS ECR
2. Checks/creates ECR repository if needed
3. Builds Docker image from `docker/Dockerfile`
4. Tags image with both `latest` and version tag (e.g., `v20251220-003204`)
5. Pushes both tags to AWS ECR
6. Outputs image digest for reference

**Location**: Run from project root (`/Volumes/Storage/OASIS_CLEAN`)

**Expected Output**:
```
🚀 OASIS API Docker Deployment
==================================
ECR Repository: 881490134703.dkr.ecr.us-east-1.amazonaws.com/oasis-api
✅ Successfully authenticated with ECR
✅ Docker image built successfully
✅ Successfully pushed latest tag
✅ Successfully pushed v20251220-003204 tag
✅ Image digest: sha256:79d9202a32e47edc0c75d79ce7e561f6d2d53a321f8d188b360b8c4df53a2343
```

**Common Issues**:
- **Docker not running**: Start Docker Desktop
- **AWS credentials not configured**: Run `aws configure`
- **Build fails**: Check Docker logs, ensure all dependencies are present
- **No space on device**: Run `docker system prune -a` to free space

---

### Step 2: Update ECS Service

**Script**: `./docker/update-ecs.sh [optional-image-tag]`

**What it does**:
1. Retrieves current ECS task definition
2. Updates task definition with new Docker image
3. Registers new task definition revision
4. Updates ECS service to use new task definition
5. Waits for service to stabilize

**Location**: Run from project root (`/Volumes/Storage/OASIS_CLEAN`)

**Usage**:
```bash
# Use latest tag (default)
./docker/update-ecs.sh

# Use specific version tag
./docker/update-ecs.sh v20251220-003204

# Use image digest (most specific)
./docker/update-ecs.sh sha256:79d9202a32e47edc0c75d79ce7e561f6d2d53a321f8d188b360b8c4df53a2343
```

**Expected Output**:
```
🔄 ECS Service Update
==========================
✅ Retrieved task definition
✅ New task definition registered: arn:aws:ecs:us-east-1:881490134703:task-definition/oasis-api-task:19
✅ Service update initiated
✅ ECS Service Update Complete!
```

**Service Details**:
- **Cluster**: `oasis-api-cluster`
- **Service**: `oasis-api-service`
- **Task Family**: `oasis-api-task`
- **Region**: `us-east-1`

---

## When to Update Docker

Update Docker whenever:

1. **Code changes** are made to:
   - `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/` (WebAPI project)
   - `ONODE/NextGenSoftware.OASIS.API.ONODE.Core/` (Core project)
   - `OASIS Architecture/` (Core OASIS libraries)
   - `Providers/` (Provider implementations)

2. **Configuration changes** in:
   - `appsettings.json` or `appsettings.Production.json`
   - `OASIS_DNA.json` (if included in image)
   - `Dockerfile` itself

3. **Dependency updates**:
   - NuGet package updates
   - External library changes
   - .NET SDK/runtime updates

4. **New features** requiring:
   - New endpoints
   - New services
   - New dependencies

---

## Docker Configuration

### Dockerfile Location
- **Path**: `docker/Dockerfile`
- **Build Context**: Project root (`/Volumes/Storage/OASIS_CLEAN`)

### Key Configuration

**Base Images**:
- Runtime: `mcr.microsoft.com/dotnet/aspnet:9.0`
- Build: `mcr.microsoft.com/dotnet/sdk:9.0`

**Ports**:
- Container: `80` (HTTP)
- Health Check: `http://localhost:80/swagger/index.html`

**Environment Variables**:
- `ASPNETCORE_ENVIRONMENT=Production`
- `ASPNETCORE_URLS=http://+:80`

**Build Process**:
1. Copy external libraries (`External Libs/`, `external-libs/`, `NextGenSoftware-Libraries/`)
2. Copy project files (`.csproj` files)
3. Copy source code
4. Restore NuGet packages
5. Build solution
6. Publish WebAPI project
7. Copy published files to runtime image

---

## AWS Configuration

### ECR Repository
- **URI**: `881490134703.dkr.ecr.us-east-1.amazonaws.com/oasis-api`
- **Region**: `us-east-1`
- **Account ID**: `881490134703`

### ECS Configuration
- **Cluster**: `oasis-api-cluster`
- **Service**: `oasis-api-service`
- **Task Definition**: `oasis-api-task`
- **Load Balancer**: `oasis-api-alb-2011847064.us-east-1.elb.amazonaws.com`
- **Target Group**: `oasis-api-tg-v5`

### Domain
- **API Domain**: `api.oasisweb4.com`
- Points to ALB → ECS service

---

## Testing After Update

### 1. Check ECS Service Status

```bash
aws ecs describe-services \
  --cluster oasis-api-cluster \
  --services oasis-api-service \
  --region us-east-1 \
  --query 'services[0].{Status:status,RunningCount:runningCount,DesiredCount:desiredCount,TaskDefinition:taskDefinition}' \
  --output json
```

**Expected**: `RunningCount` should equal `DesiredCount` (typically 1)

### 2. Check Running Task

```bash
aws ecs describe-tasks \
  --cluster oasis-api-cluster \
  --tasks $(aws ecs list-tasks --cluster oasis-api-cluster --service-name oasis-api-service --region us-east-1 --query 'taskArns[0]' --output text) \
  --region us-east-1 \
  --query 'tasks[0].{Image:containers[0].image,LastStatus:lastStatus,TaskDefinitionArn:taskDefinitionArn}' \
  --output json
```

**Expected**: `LastStatus` should be `RUNNING`, `Image` should match the new image

### 3. Test API Endpoint

```bash
# Health check
curl http://api.oasisweb4.com/api/avatar/health

# Swagger UI
curl http://api.oasisweb4.com/swagger/index.html

# Authentication test
curl -X POST http://api.oasisweb4.com/api/avatar/authenticate \
  -H "Content-Type: application/json" \
  -d '{"username":"OASIS_ADMIN","password":"Uppermall1!"}'
```

---

## Troubleshooting

### Build Fails

**Issue**: Docker build fails with compilation errors

**Solution**:
1. Check build logs for specific errors
2. Ensure all project references are correct
3. Verify `.csproj` files don't reference excluded projects
4. Check that all required files are in build context

**Common Build Errors**:
- Missing project references → Check `.csproj` files
- STARNET dependencies → Already excluded, should not appear
- Missing external libraries → Ensure `External Libs/` and `external-libs/` are copied

### Push Fails

**Issue**: `docker push` fails with authentication error

**Solution**:
```bash
# Re-authenticate with ECR
aws ecr get-login-password --region us-east-1 | \
  docker login --username AWS --password-stdin \
  881490134703.dkr.ecr.us-east-1.amazonaws.com
```

### ECS Service Won't Update

**Issue**: Service update fails or doesn't start new tasks

**Solution**:
1. Check task definition for errors:
   ```bash
   aws ecs describe-task-definition \
     --task-definition oasis-api-task \
     --region us-east-1 \
     --query 'taskDefinition.{Status:status,Image:containerDefinitions[0].image}'
   ```

2. Check service events:
   ```bash
   aws ecs describe-services \
     --cluster oasis-api-cluster \
     --services oasis-api-service \
     --region us-east-1 \
     --query 'services[0].events[0:5]'
   ```

3. Check task logs:
   ```bash
   aws logs tail /ecs/oasis-api --follow --region us-east-1
   ```

### API Not Responding

**Issue**: After update, API returns errors or doesn't respond

**Solution**:
1. Check if service is running:
   ```bash
   aws ecs describe-services --cluster oasis-api-cluster --services oasis-api-service --region us-east-1
   ```

2. Check task logs for errors:
   ```bash
   aws logs tail /ecs/oasis-api --follow --region us-east-1
   ```

3. Verify image was built correctly:
   ```bash
   docker run --rm -p 8080:80 oasis-api:latest
   # Then test: curl http://localhost:8080/api/avatar/health
   ```

4. Rollback to previous version if needed:
   ```bash
   # Get previous task definition revision
   aws ecs describe-task-definition --task-definition oasis-api-task --region us-east-1 --query 'taskDefinition.revision'
   
   # Update service to use previous revision
   aws ecs update-service \
     --cluster oasis-api-cluster \
     --service oasis-api-service \
     --task-definition oasis-api-task:18 \
     --region us-east-1
   ```

---

## Local Testing Before Deployment

### Build Locally

```bash
cd /Volumes/Storage/OASIS_CLEAN
docker build -f docker/Dockerfile -t oasis-api:latest .
```

### Run Locally

```bash
# Copy OASIS_DNA.json into container (required)
docker run -d \
  --name oasis-api-test \
  -p 8080:80 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  oasis-api:latest

# Copy OASIS_DNA.json
docker cp OASIS_DNA.json oasis-api-test:/app/OASIS_DNA.json

# Restart container
docker restart oasis-api-test

# Test
curl http://localhost:8080/api/avatar/health
```

### Test Script

Use the provided test script:
```bash
./docker/test-local.sh
```

---

## Build Optimization

### Docker Build Cache

Docker uses layer caching. To force a clean build:
```bash
docker build --no-cache -f docker/Dockerfile -t oasis-api:latest .
```

### Build Context Size

The build context includes the entire project. To reduce size:
1. Check `.dockerignore` excludes unnecessary files
2. Ensure `bin/`, `obj/`, `.git/` are excluded
3. Large directories like `holochain-client-csharp/` should be excluded

### Build Time

Typical build times:
- **First build**: 20-30 minutes (downloads all dependencies)
- **Incremental build**: 5-15 minutes (uses cache)
- **Clean build**: 20-30 minutes

To speed up:
- Use Docker BuildKit: `DOCKER_BUILDKIT=1 docker build ...`
- Use build cache mount (if supported)

---

## Rollback Procedure

If a deployment causes issues:

### 1. Identify Previous Working Version

```bash
# List ECR images
aws ecr describe-images \
  --repository-name oasis-api \
  --region us-east-1 \
  --query 'imageDetails[*].{Tags:imageTags,PushedAt:imagePushedAt}' \
  --output json | jq 'sort_by(.PushedAt) | reverse | .[0:5]'
```

### 2. Get Previous Task Definition

```bash
# List task definition revisions
aws ecs list-task-definitions \
  --family-prefix oasis-api-task \
  --region us-east-1 \
  --sort DESC \
  --max-items 5
```

### 3. Rollback Service

```bash
# Update service to use previous task definition
aws ecs update-service \
  --cluster oasis-api-cluster \
  --service oasis-api-service \
  --task-definition oasis-api-task:18 \
  --region us-east-1
```

---

## Important Files

### Scripts
- `docker/deploy.sh` - Build and push to ECR
- `docker/update-ecs.sh` - Update ECS service
- `docker/test-local.sh` - Test Docker image locally
- `docker/build.sh` - Build locally only

### Configuration
- `docker/Dockerfile` - Docker build instructions
- `.dockerignore` - Files to exclude from build
- `docker/docker-compose.yml` - Local development setup

### Documentation
- `docker/README.md` - General Docker documentation
- `docker/AGENT_DOCKER_UPDATE_GUIDE.md` - This file (for AI agents)
- `DOCKER_DEPLOYMENT_GUIDE.md` - General deployment guide

---

## Checklist for AI Agents

Before updating Docker:

- [ ] Verify code changes are complete and tested locally
- [ ] Check that Docker is running (`docker info`)
- [ ] Verify AWS credentials are configured (`aws sts get-caller-identity`)
- [ ] Ensure sufficient disk space (`docker system df`)
- [ ] Review any changes to `Dockerfile` or `.dockerignore`

During update:

- [ ] Run `./docker/deploy.sh` and wait for completion
- [ ] Verify image was pushed successfully (check output)
- [ ] Run `./docker/update-ecs.sh` to update service
- [ ] Wait for service to stabilize (script does this automatically)

After update:

- [ ] Verify ECS service is running new task
- [ ] Test API endpoint: `curl http://api.oasisweb4.com/api/avatar/health`
- [ ] Test authentication: Use test credentials from `Authentication_Process.md`
- [ ] Check logs if issues occur: `aws logs tail /ecs/oasis-api --follow`

---

## Summary

**To update Docker**:
1. `./docker/deploy.sh` - Builds and pushes image
2. `./docker/update-ecs.sh` - Updates ECS service

**Total time**: ~25-35 minutes (build) + ~2-5 minutes (deploy)

**Verification**: Test `http://api.oasisweb4.com/api/avatar/health`

The scripts handle all the complexity automatically. Just run them in order from the project root.



