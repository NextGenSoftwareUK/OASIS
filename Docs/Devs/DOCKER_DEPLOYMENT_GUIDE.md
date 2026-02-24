# 🐳 Docker Deployment Guide for OASIS Ecosystem

## Overview
This guide covers Docker deployment for all OASIS ecosystem components:
- **OASIS API Web API** (.NET 8)
- **STAR API Web API** (.NET 9)
- **OASIS OPORTAL** (.NET 8 + React)
- **STAR WEB UI** (.NET 8 + React)
- **OASIS WEB UI** (React + Nginx)

## Prerequisites
- Docker Desktop installed
- Docker Compose installed
- Git repository cloned

## 🚀 Quick Start

### 1. Build All Images
```bash
# Build all services
docker-compose build

# Or build specific service
docker-compose build oasis-api
docker-compose build star-api
docker-compose build oasis-oportal
docker-compose build star-web-ui
```

### 2. Run All Services
```bash
# Start all services
docker-compose up -d

# View logs
docker-compose logs -f

# Stop all services
docker-compose down
```

### 3. Access Services
- **OASIS API**: http://localhost:5000
- **STAR API**: http://localhost:5001
- **OASIS OPORTAL**: http://localhost:5002
- **STAR WEB UI**: http://localhost:5003
- **OASIS WEB UI**: http://localhost:3000

## 📋 Individual Service Deployment

### OASIS API Web API (.NET 8)
```bash
# Build image
docker build -f ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Dockerfile -t oasis-api .

# Run container
docker run -p 5000:8080 -e ASPNETCORE_ENVIRONMENT=Production oasis-api

# Health check
curl http://localhost:5000/api/health
```

### STAR API Web API (.NET 9)
```bash
# Build image
docker build -f "STAR ODK/NextGenSoftware.OASIS.STAR.WebAPI/Dockerfile" -t star-api .

# Run container
docker run -p 5001:8080 -e ASPNETCORE_ENVIRONMENT=Production star-api

# Health check
curl http://localhost:5001/api/health
```

### OASIS OPORTAL (.NET 8 + React)
```bash
# Build image
docker build -f ONODE/NextGenSoftware.OASIS.API.ONODE.OPORTAL/Dockerfile -t oasis-oportal .

# Run container
docker run -p 5002:8080 -e ASPNETCORE_ENVIRONMENT=Production oasis-oportal
```

### STAR WEB UI (.NET 8 + React)
```bash
# Build image
docker build -f "STAR ODK/NextGenSoftware.OASIS.STAR.WebUI/Dockerfile" -t star-web-ui .

# Run container
docker run -p 5003:8080 -e ASPNETCORE_ENVIRONMENT=Production star-web-ui

# Health check
curl http://localhost:5003/
```

### OASIS WEB UI (React + Nginx)
```bash
# Build image
docker build -f oasisweb4.com/Dockerfile -t oasis-web-ui .

# Run container
docker run -p 3000:80 oasis-web-ui

# Health check
curl http://localhost:3000/health
```

## 🔧 Configuration

### Environment Variables
Create a `.env` file in the root directory:
```env
# Database
POSTGRES_DB=oasis
POSTGRES_USER=oasis
POSTGRES_PASSWORD=oasis123

# API URLs
OASIS_API_URL=http://oasis-api:8080
STAR_API_URL=http://star-api:8080

# Environment
ASPNETCORE_ENVIRONMENT=Production
```

### Custom Ports
Modify `docker-compose.yml` to change ports:
```yaml
services:
  oasis-api:
    ports:
      - "8080:8080"  # Change first number for host port
```

## 🏗️ Production Deployment

### 1. Docker Swarm
```bash
# Initialize swarm
docker swarm init

# Deploy stack
docker stack deploy -c docker-compose.yml oasis-stack

# Scale services
docker service scale oasis-stack_oasis-api=3
```

### 2. Kubernetes
```bash
# Convert compose to k8s manifests
kompose convert

# Apply to cluster
kubectl apply -f .
```

### 3. Cloud Platforms

#### Railway
- Push to GitHub
- Connect repository to Railway
- Railway auto-detects Dockerfiles

#### AWS ECS
- Build and push to ECR
- Create ECS task definitions
- Deploy using ECS service

#### Azure Container Instances
- Build and push to ACR
- Deploy using Azure CLI

## 📊 Monitoring & Health Checks

### Health Check Endpoints
- **OASIS API**: `/api/health`
- **STAR API**: `/api/health`
- **OPORTAL**: `/`
- **STAR WEB UI**: `/`
- **OASIS WEB UI**: `/health`

### Logs
```bash
# View all logs
docker-compose logs

# View specific service logs
docker-compose logs oasis-api

# Follow logs in real-time
docker-compose logs -f star-api
```

### Resource Monitoring
```bash
# Container stats
docker stats

# Service status
docker-compose ps
```

## 🔒 Security Best Practices

### 1. Use Multi-stage Builds
All Dockerfiles use multi-stage builds to minimize image size.

### 2. Non-root User
```dockerfile
# Add to Dockerfile
RUN adduser -D -s /bin/sh appuser
USER appuser
```

### 3. Security Scanning
```bash
# Scan images for vulnerabilities
docker scan oasis-api
docker scan star-api
```

### 4. Secrets Management
```bash
# Use Docker secrets
echo "mysecret" | docker secret create db_password -
```

## 🚨 Troubleshooting

### Common Issues

#### Build Failures
```bash
# Clean build
docker-compose build --no-cache

# Check build logs
docker-compose build oasis-api
```

#### Port Conflicts
```bash
# Check port usage
netstat -tulpn | grep :5000

# Change ports in docker-compose.yml
```

#### Memory Issues
```bash
# Increase Docker memory limit
# Docker Desktop → Settings → Resources → Memory
```

#### Database Connection
```bash
# Check database logs
docker-compose logs postgres

# Connect to database
docker-compose exec postgres psql -U oasis -d oasis
```

### Debug Commands
```bash
# Enter container
docker-compose exec oasis-api sh

# Check container processes
docker-compose exec oasis-api ps aux

# Check network connectivity
docker-compose exec oasis-api ping star-api
```

## 📈 Performance Optimization

### 1. Image Optimization
- Use Alpine Linux base images
- Multi-stage builds
- Remove unnecessary packages

### 2. Caching
- Layer caching for faster builds
- Use .dockerignore files
- Order Dockerfile commands by frequency of change

### 3. Resource Limits
```yaml
services:
  oasis-api:
    deploy:
      resources:
        limits:
          memory: 512M
          cpus: '0.5'
```

## 🔄 CI/CD Integration

### GitHub Actions
```yaml
name: Build and Push Docker Images
on: [push]
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2
      - name: Build and push
        run: |
          docker-compose build
          docker-compose push
```

### GitLab CI
```yaml
build:
  stage: build
  script:
    - docker-compose build
    - docker-compose push
```

## 📚 Additional Resources

- [Docker Documentation](https://docs.docker.com/)
- [Docker Compose Reference](https://docs.docker.com/compose/)
- [.NET Docker Images](https://hub.docker.com/_/microsoft-dotnet)
- [Nginx Docker Image](https://hub.docker.com/_/nginx)

---

**Happy Containerizing! 🐳**
