# OASIS API Docker Deployment

This folder contains all Docker-related files for building and deploying the OASIS API.

**Production API URL:** `https://api.oasisweb4.com` (configured in OASIS_DNA.json as `OASISAPIURL` and in Swagger/Postman links in Startup).

## 📁 Files

- **Dockerfile** - Main production Dockerfile for building the OASIS API (ONODE)
- **Dockerfile.star** - Dockerfile for building the STAR API (Missions, Quests, GeoNFTs, etc.)
- **Dockerfile.mcp** - Dockerfile for the OASIS MCP HTTP server (Streamable HTTP, for hosted deployment)
- **.dockerignore** - Files and directories to exclude from Docker build context
- **.dockerignore.mcp** - MCP-only context for `Dockerfile.mcp` (used by deploy-mcp.sh)
- **docker-compose.yml** - Docker Compose configuration for local development
- **build.sh** - Script to build OASIS API (ONODE) Docker image locally
- **PRE_BUILD.md** - Pre-flight checklist and build/run commands
- **build-star.sh** - Script to build STAR API Docker image locally
- **deploy.sh** - Script to build and push OASIS API Docker image to AWS ECR
- **deploy-star.sh** - Script to build and push STAR API image to AWS ECR
- **deploy-mcp.sh** - Script to build and push OASIS MCP HTTP server image to AWS ECR
- **update-ecs.sh** - Script to update AWS ECS service with new image
- **COMPARISON.md** - Comparison between previous working image and current setup
- **BUILD_STATUS.md** - Build status and configuration details

## 🚀 Quick Start

### Build Locally

```bash
cd /path/to/OASIS_CLEAN   # repo root
./docker/build.sh
```

This will build the Docker image with tag `oasis-api:latest`.

### Run Locally

```bash
# Using Docker directly
docker run -p 5003:80 oasis-api:latest

# Or using Docker Compose (both ONODE and STAR API)
docker-compose up
```

Then access:
- OASIS API (ONODE): http://localhost:5003 | Swagger: http://localhost:5003/swagger
- STAR API: http://localhost:50564 | Health: http://localhost:50564/api/health | Swagger: http://localhost:50564/swagger

### Build STAR API Only

```bash
./docker/build-star.sh
docker run -p 50564:80 star-api:latest
```

### Build and Deploy MCP (HTTP server for hosted MCP)

```bash
./docker/deploy-mcp.sh
```

Builds the MCP Streamable HTTP server (from `MCP/`) and pushes to ECR as `oasis-mcp`. See **RUN_MCP_ON_EC2_STEPS.md** for pull/run on EC2 and Cursor URL config.

### Build and Deploy to AWS ECR

```bash
cd /path/to/OASIS_CLEAN   # repo root
./docker/deploy.sh
```

### Update ECS Service

```bash
./docker/update-ecs.sh
```

## 📊 Configuration

### Environment Variables

- `ASPNETCORE_ENVIRONMENT=Production`
- `ASPNETCORE_URLS=http://+:80`

### Ports

- **80**: HTTP (internal)
- **443**: HTTPS (internal)
- **5003**: HTTP (host mapping)
- **5004**: HTTPS (host mapping)

### Health Check

- **Endpoint**: `/api/health`
- **Interval**: 30s
- **Timeout**: 10s
- **Retries**: 3
- **Start Period**: 60s

## 📝 Build Process

1. **Base Stage**: Uses .NET 9.0 runtime image
2. **Build Stage**: Uses .NET 9.0 SDK to build the application
3. **Publish Stage**: Publishes the application
4. **Final Stage**: Copies published files and sets up runtime

## 🔍 Key Features

- ✅ .NET 9.0 compatible
- ✅ Multi-stage build for optimized image size
- ✅ Includes all 30+ OASIS providers
- ✅ Health check configured
- ✅ Production-ready configuration

## 🐛 Troubleshooting

### Build Context Too Large

If the build context is too large, check `.dockerignore` to ensure unnecessary directories are excluded.

### Missing Dependencies

Ensure `NextGenSoftware-Libraries` and other dependencies are available in the build context.

### OASIS_DNA.json

The `OASIS_DNA.json` file should be in the WebAPI project directory. If not present, configuration can be provided via environment variables.

## 📚 Related Documentation

- `BUILD_STATUS.md` - Build status and updates
- `COMPARISON.md` - Comparison with previous working image
- `SUMMARY.md` - Summary of Docker setup



