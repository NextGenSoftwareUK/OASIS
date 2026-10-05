# OASIS API Docker Deployment Update Guide

## Current Image Status

**Current Deployed Image:**
- **Digest**: `sha256:3be9fbbd667475a86adf1215e28d67885bf98ff480d049e4039a937e5951b5f0`
- **Tags**: `latest`, `v20251209-214104`
- **Pushed**: December 9, 2025
- **Size**: ~259 MB
- **Location**: `881490134703.dkr.ecr.us-east-1.amazonaws.com/oasis-api`

**ECS Task Definition:**
- **File**: `oasis-api-task-definition.json`
- **Family**: `oasis-api-task`
- **Cluster**: `oasis-api-cluster`
- **Service**: `oasis-api-service`

## Dockerfile Location

The production Dockerfile is located at:
```
/Volumes/Storage/OASIS_CLEAN/docker/Dockerfile
```

**Note**: The Dockerfile in the `docker/` directory was previously empty. It has now been updated to build the ONODE WebAPI with proper external library handling.

## What Changed

### Previous Setup
- The root `Dockerfile` was building the STAR WebAPI (not ONODE WebAPI)
- The `docker/Dockerfile` was empty
- External libraries handling was unclear

### Current Setup
- ✅ `docker/Dockerfile` now builds ONODE WebAPI correctly
- ✅ Handles external libraries properly:
  - `External Libs/` (IPFS client)
  - `holochain-client-csharp/` (Holochain client)
  - `NextGenSoftware-Libraries/` (Utilities, logging, etc.)
- ✅ Uses .NET 9.0 (matching current build)
- ✅ Includes all provider projects
- ✅ Proper multi-stage build for optimization

## How to Update the Deployed Image

### Step 1: Build and Push New Image

```bash
cd /Volumes/Storage/OASIS_CLEAN
./docker/deploy.sh
```

This script will:
1. Authenticate with AWS ECR
2. Build the Docker image with tags: `latest` and `v{timestamp}`
3. Push both tags to ECR
4. Display the new image digest

### Step 2: Update ECS Service

After the image is pushed, update the ECS service:

```bash
# Option 1: Use the 'latest' tag
./docker/update-ecs.sh latest

# Option 2: Use a specific version tag
./docker/update-ecs.sh v20251217-120000

# Option 3: Use the image digest (most specific)
./docker/update-ecs.sh sha256:NEW_DIGEST_HERE
```

### Step 3: Verify Deployment

Check the ECS service status:

```bash
aws ecs describe-services \
  --cluster oasis-api-cluster \
  --services oasis-api-service \
  --region us-east-1
```

## Dockerfile Structure

The Dockerfile uses a multi-stage build:

1. **Base Stage**: .NET 9.0 runtime with curl for health checks
2. **Build Stage**: 
   - Copies solution and project files
   - Copies external libraries
   - Restores dependencies
   - Copies all source code
   - Builds the application
3. **Publish Stage**: Publishes the application
4. **Final Stage**: Copies published files to runtime image

## Key Configuration

### Ports
- **80**: HTTP (container)
- **443**: HTTPS (container)

### Environment Variables
Set in ECS task definition:
- `ASPNETCORE_ENVIRONMENT=Production`
- `ASPNETCORE_URLS=http://+:80`
- `ConnectionStrings__MongoDBOASIS=...`

### Health Check
- **Endpoint**: `/swagger/index.html`
- **Interval**: 30s
- **Timeout**: 10s
- **Start Period**: 60s
- **Retries**: 3

## External Libraries Handling

The Dockerfile properly handles:

1. **External Libs/** - IPFS HTTP client
   - Copied before restore to ensure availability

2. **holochain-client-csharp/** - Holochain client
   - Copied before restore

3. **NextGenSoftware-Libraries/** - Core utilities
   - Copied before restore
   - Includes: Utilities, ErrorHandling, Logging, WebSocket, etc.

4. **Provider Projects** - All 30+ blockchain providers
   - Key providers copied explicitly
   - Remaining providers included via `COPY . .`

## Troubleshooting

### Build Fails with Missing Dependencies

Ensure all external libraries are present:
```bash
ls -la "External Libs/"
ls -la "holochain-client-csharp/"
ls -la "NextGenSoftware-Libraries/"
```

### Build Context Too Large

Check `.dockerignore` in the `docker/` directory. It should exclude:
- Test projects
- Documentation
- Build outputs
- Large unrelated projects

### Image Push Fails

1. Verify AWS credentials:
   ```bash
   aws sts get-caller-identity
   ```

2. Verify ECR access:
   ```bash
   aws ecr describe-repositories --repository-names oasis-api --region us-east-1
   ```

3. Re-authenticate Docker:
   ```bash
   aws ecr get-login-password --region us-east-1 | docker login --username AWS --password-stdin 881490134703.dkr.ecr.us-east-1.amazonaws.com
   ```

## Next Steps

1. **Test Build Locally** (optional):
   ```bash
   cd /Volumes/Storage/OASIS_CLEAN
   docker build -f docker/Dockerfile -t oasis-api:test .
   docker run -p 5000:80 oasis-api:test
   ```

2. **Deploy to AWS**:
   ```bash
   ./docker/deploy.sh
   ./docker/update-ecs.sh latest
   ```

3. **Monitor Deployment**:
   - Check ECS service logs in CloudWatch
   - Verify health checks are passing
   - Test API endpoints

## Related Files

- `docker/Dockerfile` - Production Dockerfile
- `docker/deploy.sh` - Build and push script
- `docker/update-ecs.sh` - ECS service update script
- `docker/.dockerignore` - Build context exclusions
- `oasis-api-task-definition.json` - ECS task definition
- `DOCKER_CONTEXT.md` - Detailed Docker context documentation

---

**Last Updated**: December 17, 2025
**Status**: Ready for deployment





