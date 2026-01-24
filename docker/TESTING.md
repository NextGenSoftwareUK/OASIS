# Testing the OASIS API Docker Image

## Quick Test

Run the full test suite:
```bash
./docker/test-local.sh
```

Or test with a specific image tag:
```bash
./docker/test-local.sh v20251219-151443
```

## Manual Testing

### 1. Run the Container Locally

```bash
# Pull and run the image
docker run -d \
  --name oasis-api-test \
  -p 8080:80 \
  -v $(pwd)/OASIS_DNA.json:/app/OASIS_DNA.json:ro \
  -e ASPNETCORE_ENVIRONMENT=Development \
  881490134703.dkr.ecr.us-east-1.amazonaws.com/oasis-api:latest
```

### 2. Test Endpoints

```bash
# Swagger UI
open http://localhost:8080/swagger

# Health check
curl http://localhost:8080/api/health

# API version
curl http://localhost:8080/api/settings/version

# Avatar list (may require auth)
curl http://localhost:8080/api/avatar
```

### 3. Compare with Local API

**Local API** (runs on port 5000/5003):
```bash
# Start local API (from project root)
cd ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI
dotnet run

# Test endpoints
curl http://localhost:5000/swagger
curl http://localhost:5000/api/settings/version
```

**Docker API** (runs on port 8080):
```bash
# Test endpoints
curl http://localhost:8080/swagger
curl http://localhost:8080/api/settings/version
```

### 4. View Logs

```bash
# Docker container logs
docker logs -f oasis-api-test

# Compare with local API console output
```

### 5. Test Key Functionality

Test the same endpoints on both:

1. **Swagger UI**: Should be identical
2. **API Version**: Should return same version info
3. **Avatar Endpoints**: Should work the same way
4. **NFT Endpoints**: Should work (NFTManager is included)
5. **Wallet Endpoints**: Should work

### 6. Cleanup

```bash
# Stop and remove test container
docker stop oasis-api-test
docker rm oasis-api-test
```

## Comparing Behavior

### Expected Differences

1. **Environment**: Docker runs in `Production` mode by default (can override with `-e ASPNETCORE_ENVIRONMENT=Development`)
2. **Port**: Docker uses port 80 internally, mapped to 8080 locally
3. **Configuration**: Uses `OASIS_DNA.json` from container or mounted volume

### Should Be Identical

1. **API Endpoints**: All endpoints should work the same
2. **Response Format**: JSON responses should be identical
3. **Error Handling**: Should behave the same way
4. **Swagger Documentation**: Should be identical

## Troubleshooting

### Container won't start
```bash
# Check logs
docker logs oasis-api-test

# Check if port is already in use
lsof -i :8080
```

### API not responding
```bash
# Check if container is running
docker ps | grep oasis-api-test

# Check container health
docker inspect oasis-api-test | grep -A 10 Health
```

### Missing OASIS_DNA.json
```bash
# Copy OASIS_DNA.json to container
docker cp OASIS_DNA.json oasis-api-test:/app/OASIS_DNA.json

# Or mount it when starting
docker run ... -v $(pwd)/OASIS_DNA.json:/app/OASIS_DNA.json:ro ...
```

## Production Testing

After testing locally, you can update the ECS service:

```bash
# Update ECS service with new image
./docker/update-ecs.sh
```

Then test the production API at your ECS service endpoint.




