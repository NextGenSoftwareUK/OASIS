# Docker Folder Summary

## 📁 Created Files

### Core Files
- **Dockerfile** - Main production Dockerfile based on working image `96a7bd158ecb`
- **.dockerignore** - Optimized ignore file to reduce build context size
- **deploy.sh** - Script to build and push Docker image to AWS ECR
- **update-ecs.sh** - Script to update AWS ECS service with new image

### Documentation
- **README.md** - Complete guide for using the Docker setup
- **COMPARISON.md** - Detailed comparison between previous working image and current setup
- **SUMMARY.md** - This file

## 🔍 Key Improvements Based on Working Image

### Previous Working Image (`mainnet-update`)
- Image ID: `96a7bd158ecb`
- Digest: `sha256:96a7bd158ecb93b459fca9e65155fed14e233328d921a42d6b2a7f69bb0cf3d6`
- .NET 9.0.9, Ports 80/443, Included OASIS_DNA.json

### Current Dockerfile Improvements
1. ✅ **Solution File Restoration**: Uses `The OASIS.sln` to restore all dependencies
2. ✅ **Fallback Strategy**: Falls back to project restore if solution restore fails
3. ✅ **Optimized Build Context**: Better `.dockerignore` to reduce build size
4. ✅ **Provider Inclusion**: Ensures all 30+ providers are included
5. ✅ **Configuration**: Handles OASIS_DNA.json (can use env vars as fallback)

## 🚀 Usage

### Build and Deploy
```bash
cd /Volumes/Storage/OASIS_CLEAN
./docker/deploy.sh
```

### Update ECS Service
```bash
./docker/update-ecs.sh [image-tag]
# Example: ./docker/update-ecs.sh latest
# Example: ./docker/update-ecs.sh sha256:abc123...
```

## 📊 Build Context Optimization

The `.dockerignore` file excludes:
- Test projects and harnesses
- Documentation files
- Build outputs (bin/, obj/)
- Large unrelated projects (meta-bricks-main, TimoRides, etc.)
- IDE files

**Included** (needed for build):
- ONODE/ (WebAPI and Core)
- OASIS Architecture/ (Core, DNA, BootLoader, Common)
- Providers/ (all provider projects)
- External Libs/ (dependencies)
- NextGenSoftware-Libraries/ (utilities, logging)
- holochain-client-csharp/ (referenced in solution)

## 🔧 Configuration

### Environment Variables
- `ASPNETCORE_ENVIRONMENT=Production`
- `ASPNETCORE_URLS=http://+:80`

### Ports
- **80**: HTTP
- **443**: HTTPS

### Health Check
- Endpoint: `/swagger/index.html`
- Interval: 30s
- Timeout: 3s
- Retries: 3

## 📝 Next Steps

1. Test the build locally:
   ```bash
   docker build -f docker/Dockerfile -t oasis-api:test .
   ```

2. If build succeeds, deploy:
   ```bash
   ./docker/deploy.sh
   ```

3. Update ECS service:
   ```bash
   ./docker/update-ecs.sh latest
   ```

## 🐛 Troubleshooting

### Build Context Too Large
- Check `.dockerignore` to ensure unnecessary directories are excluded
- Current optimized size: ~6MB (down from 1.44GB)

### Missing Dependencies
- Ensure `NextGenSoftware-Libraries` and `holochain-client-csharp` are checked out locally
- These are not Git submodules but regular directories

### Solution File Errors
- The Dockerfile has a fallback to project restore if solution restore fails
- Test projects are excluded via `.dockerignore` to avoid missing project errors

## 📚 Related Files

- Root `Dockerfile.production` - Can be updated to use `docker/Dockerfile`
- Root `deploy-docker.sh` - Can be updated to use `docker/deploy.sh`
- Root `update-ecs-service.sh` - Can be updated to use `docker/update-ecs.sh`


