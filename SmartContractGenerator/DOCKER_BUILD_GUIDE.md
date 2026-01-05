# Docker Build Guide - Smart Contract Generator API

**Version:** 2.0 (with X402 Payment & Credits)  
**Date:** November 2, 2025  
**Status:** Ready to build

---

## 🚀 **Quick Build & Deploy**

### **Step 1: Build the Docker Image**

From the `smart-contract-generator` directory:

```bash
# Build the image with X402 integration
docker build \
  -t assetrail/smart-contract-generator:2.0-x402 \
  -t assetrail/smart-contract-generator:latest \
  -f src/SmartContractGen/ScGen.API/Dockerfile \
  .

# This will take 5-10 minutes for the first build
```

### **Step 2: Test Locally**

```bash
# Run the container
docker run -d \
  --name sc-gen-api \
  -p 5000:80 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -v $(pwd)/appsettings.production.json:/app/appsettings.json \
  assetrail/smart-contract-generator:latest

# Test the API
curl http://localhost:5000/health
# Expected: {"status":"healthy","timestamp":"..."}

# Test credits endpoint
curl http://localhost:5000/api/v1/credits/packs
# Expected: [{"name":"Starter","credits":10,"priceSOL":0.15,...}]

# Stop container
docker stop sc-gen-api
docker rm sc-gen-api
```

### **Step 3: Push to Registry**

```bash
# Tag for your registry (update with your registry)
docker tag assetrail/smart-contract-generator:latest \
  YOUR_REGISTRY/assetrail/smart-contract-generator:2.0-x402

# Push to Docker Hub (or your registry)
docker push YOUR_REGISTRY/assetrail/smart-contract-generator:2.0-x402
docker push YOUR_REGISTRY/assetrail/smart-contract-generator:latest
```

---

## 📋 **What's Included in This Image**

### **New Features (v2.0)**
- ✅ X402 payment protocol integration
- ✅ Credits system (4 tier packs)
- ✅ Payment verification via Solana RPC
- ✅ JWT authentication for paid operations
- ✅ Webhook triggers for NFT distribution
- ✅ CORS configured for frontend

### **Core Features (v1.x)**
- ✅ Smart contract generation (Solana, Ethereum, Radix)
- ✅ Contract compilation (Anchor, Solc, Scrypto)
- ✅ Contract deployment
- ✅ Handlebars templating engine
- ✅ Swagger API documentation

---

## ⚙️ **Configuration**

### **Environment Variables**

```bash
# Required
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:80

# Optional (via appsettings.json or env vars)
X402__SolanaRpcUrl=https://api.mainnet-beta.solana.com
X402__TreasuryAddress=YOUR_MAINNET_TREASURY_ADDRESS
X402__RequirePayment=true
X402__DistributionWebhookUrl=https://your-webhook.com/distribute
X402__JwtSecret=YOUR_SECURE_SECRET_KEY_HERE
```

### **Production Config File**

Create `appsettings.production.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Ethereum": {
    "RpcUrl": "https://mainnet.infura.io/v3/YOUR_KEY",
    "PrivateKey": "YOUR_PRIVATE_KEY",
    "GasLimit": 3000000
  },
  "Solana": {
    "RpcUrl": "https://api.mainnet-beta.solana.com",
    "KeyPairPath": "/app/config/keypair.json",
    "UseLocalValidator": false,
    "Pubkey": "YOUR_PUBKEY"
  },
  "Radix": {
    "UseResim": false,
    "NetworkUrl": "https://mainnet.radixdlt.com",
    "AccountAddress": "YOUR_ACCOUNT"
  },
  "X402": {
    "SolanaRpcUrl": "https://api.mainnet-beta.solana.com",
    "TreasuryAddress": "YOUR_MAINNET_TREASURY_ADDRESS",
    "RequirePayment": true,
    "FreeTierLimit": 3,
    "JwtSecret": "YOUR_PRODUCTION_SECRET_MINIMUM_32_CHARACTERS",
    "PaymentTokenExpirationHours": 24,
    "DistributionWebhookUrl": "https://api.oasis.one/webhooks/x402/distribute"
  }
}
```

---

## 🔐 **Security Notes**

### **Before Production:**

1. **Change JWT Secret**
   ```bash
   # Generate a secure secret (32+ characters)
   openssl rand -base64 32
   ```

2. **Use Secrets Management**
   ```bash
   # Don't put secrets in config files
   # Use Docker secrets or environment variables
   docker run -d \
     --name sc-gen-api \
     -e X402__JwtSecret="$(cat /path/to/secret)" \
     -e Ethereum__PrivateKey="$(cat /path/to/eth-key)" \
     ...
   ```

3. **Update CORS for Production**
   - In `RegisterServices.cs`, update allowed origins
   - Add your production domain

4. **HTTPS Only**
   - Use reverse proxy (nginx, Caddy)
   - Or configure HTTPS in ASP.NET Core

---

## 📦 **Docker Compose (Optional)**

Create `docker-compose.yml` in the root:

```yaml
version: '3.8'

services:
  api:
    build:
      context: .
      dockerfile: src/SmartContractGen/ScGen.API/Dockerfile
    image: assetrail/smart-contract-generator:latest
    container_name: sc-gen-api
    ports:
      - "5000:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - X402__SolanaRpcUrl=https://api.mainnet-beta.solana.com
      - X402__RequirePayment=true
    volumes:
      - ./config/appsettings.production.json:/app/appsettings.json:ro
      - ./config/solana-keypair.json:/app/config/keypair.json:ro
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:80/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s

  # Optional: Add nginx reverse proxy
  nginx:
    image: nginx:alpine
    container_name: sc-gen-nginx
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf:ro
      - ./ssl:/etc/nginx/ssl:ro
    depends_on:
      - api
    restart: unless-stopped
```

**Usage:**
```bash
# Build and start
docker-compose up -d

# View logs
docker-compose logs -f api

# Stop
docker-compose down
```

---

## 🧪 **Testing the Docker Image**

### **Test 1: Health Check**
```bash
curl http://localhost:5000/health
```

### **Test 2: Credits API**
```bash
curl http://localhost:5000/api/v1/credits/packs
```

### **Test 3: Payment Required**
```bash
curl -X POST http://localhost:5000/api/v1/contracts/generate \
  -F "Language=Rust" \
  -F "JsonFile=@/dev/null"

# Should return 402 Payment Required with pricing info
```

### **Test 4: Swagger UI**
```bash
# Visit in browser:
http://localhost:5000/swagger
```

---

## 📊 **Image Info**

**Base Images:**
- Runtime: `mcr.microsoft.com/dotnet/aspnet:9.0`
- Build: `mcr.microsoft.com/dotnet/sdk:9.0`

**Image Size:**
- ~250MB (runtime only)
- ~700MB (with build layer, multi-stage removes it)

**What's Included:**
- .NET 9.0 Runtime
- ScGen.API.dll (compiled application)
- All dependencies (Nethereum, Handlebars, JWT, etc.)
- Handlebars templates
- Project scaffolds

**What's NOT Included (needs to be added):**
- Solidity compiler (`solc`) - install separately
- Rust/Cargo toolchain - install separately
- Anchor CLI - install separately
- Scrypto toolchain - install separately

---

## 🔧 **Production Deployment Options**

### **Option 1: AWS ECS/Fargate**
```bash
# Tag for ECR
docker tag assetrail/smart-contract-generator:latest \
  YOUR_AWS_ACCOUNT.dkr.ecr.us-east-1.amazonaws.com/sc-gen:latest

# Push to ECR
aws ecr get-login-password --region us-east-1 | docker login --username AWS --password-stdin YOUR_AWS_ACCOUNT.dkr.ecr.us-east-1.amazonaws.com
docker push YOUR_AWS_ACCOUNT.dkr.ecr.us-east-1.amazonaws.com/sc-gen:latest
```

### **Option 2: Docker Hub**
```bash
docker login
docker push assetrail/smart-contract-generator:latest
```

### **Option 3: Google Cloud Run**
```bash
gcloud builds submit --tag gcr.io/PROJECT_ID/sc-gen
gcloud run deploy sc-gen --image gcr.io/PROJECT_ID/sc-gen --platform managed
```

### **Option 4: Railway.app / Render.com**
- Connect GitHub repo
- Railway/Render auto-detects Dockerfile
- Set environment variables in dashboard
- Deploy automatically

---

## 🚀 **Recommended Deployment Strategy**

1. **Build image locally** (test everything works)
2. **Push to Docker Hub** (public or private)
3. **Deploy to cloud** (AWS/Railway/Render)
4. **Configure domain** (api.assetrail.com)
5. **Set up SSL** (Let's Encrypt/Cloudflare)
6. **Update frontend** (point to production API)

---

## 📝 **Build Script**

Save as `build-docker.sh`:

```bash
#!/bin/bash
set -e

VERSION="2.0-x402"
IMAGE_NAME="assetrail/smart-contract-generator"

echo "🐳 Building Docker image: $IMAGE_NAME:$VERSION"

# Build image
docker build \
  -t $IMAGE_NAME:$VERSION \
  -t $IMAGE_NAME:latest \
  -f src/SmartContractGen/ScGen.API/Dockerfile \
  .

echo "✅ Build complete!"
echo ""
echo "🧪 To test locally:"
echo "  docker run -d -p 5000:80 --name sc-gen-test $IMAGE_NAME:latest"
echo "  curl http://localhost:5000/health"
echo ""
echo "🚀 To push to registry:"
echo "  docker push $IMAGE_NAME:$VERSION"
echo "  docker push $IMAGE_NAME:latest"
```

Make executable:
```bash
chmod +x build-docker.sh
```

---

**I've prepared the Docker build guide. Ready to build when you are!** 🐳

