# NFT Mint Studio Frontend Deployment Guide

## 🎯 Overview

Deploy the NFT Mint Studio frontend to `nft.oasisweb4.one` using AWS ECS/Fargate.

**Deployment Stack:**
- **Frontend:** Next.js 15 (production build)
- **Container:** Docker on AWS ECS Fargate
- **Load Balancer:** AWS ALB (shared with oasisweb4.one)
- **Domain:** nft.oasisweb4.one
- **Backend API:** oasisweb4.one

---

## 🚀 Quick Deploy

### Option 1: Using Deployment Script (Recommended)

```bash
cd "/Volumes/Storage 1/OASIS_CLEAN/nft-mint-frontend"
chmod +x deploy_nft_frontend.sh
./deploy_nft_frontend.sh
```

**What it does:**
1. Creates ECR repository (if needed)
2. Builds Docker image
3. Pushes to AWS ECR
4. Registers ECS task definition
5. Creates/updates ECS service
6. Waits for deployment to stabilize

**Timeline:** 10-15 minutes

---

## 📋 Pre-Deployment Checklist

### AWS Requirements
- [ ] AWS CLI configured (`aws configure`)
- [ ] IAM permissions for ECR, ECS, ALB, Route53
- [ ] Docker installed and running
- [ ] jq installed (`brew install jq` on Mac)

### DNS Configuration
- [ ] Route53 hosted zone for oasisweb4.one exists
- [ ] ALB DNS name available
- [ ] SSL certificate for *.oasisweb4.one (wildcard)

### Application Configuration
- [ ] Backend API running at oasisweb4.one
- [ ] OASIS_DNA.json configured
- [ ] Pinata IPFS credentials set up

---

## 🔧 Step-by-Step Manual Deployment

### Step 1: Build Next.js Production App

```bash
cd "/Volumes/Storage 1/OASIS_CLEAN/nft-mint-frontend"
npm run build
```

**Verify build:**
- `.next/` folder created
- No build errors
- Pages optimized

### Step 2: Build Docker Image

```bash
# Build for production
docker build -t nft-mint-frontend:latest .

# Test locally
docker run -p 3000:3000 nft-mint-frontend:latest
# Open http://localhost:3000 to verify
```

### Step 3: Push to AWS ECR

```bash
# Create ECR repository (first time only)
aws ecr create-repository --repository-name nft-mint-frontend --region us-east-1

# Get login credentials
aws ecr get-login-password --region us-east-1 | docker login --username AWS --password-stdin 881490134703.dkr.ecr.us-east-1.amazonaws.com

# Tag image
docker tag nft-mint-frontend:latest 881490134703.dkr.ecr.us-east-1.amazonaws.com/nft-mint-frontend:latest

# Push image
docker push 881490134703.dkr.ecr.us-east-1.amazonaws.com/nft-mint-frontend:latest
```

### Step 4: Create ECS Task Definition

Create file: `nft-frontend-task-definition.json`

```json
{
  "family": "nft-mint-frontend-task",
  "networkMode": "awsvpc",
  "requiresCompatibilities": ["FARGATE"],
  "cpu": "512",
  "memory": "1024",
  "executionRoleArn": "arn:aws:iam::881490134703:role/ecsTaskExecutionRole",
  "taskRoleArn": "arn:aws:iam::881490134703:role/ecsTaskExecutionRole",
  "containerDefinitions": [
    {
      "name": "nft-mint-frontend",
      "image": "881490134703.dkr.ecr.us-east-1.amazonaws.com/nft-mint-frontend:latest",
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
  ]
}
```

Register task definition:
```bash
aws ecs register-task-definition --cli-input-json file://nft-frontend-task-definition.json --region us-east-1
```

### Step 5: Create ALB Target Group

```bash
# Create target group for NFT frontend
aws elbv2 create-target-group \
  --name nft-frontend-tg \
  --protocol HTTP \
  --port 3000 \
  --vpc-id vpc-XXXXXXXX \
  --target-type ip \
  --health-check-enabled \
  --health-check-path / \
  --health-check-interval-seconds 30 \
  --health-check-timeout-seconds 5 \
  --healthy-threshold-count 2 \
  --unhealthy-threshold-count 3 \
  --region us-east-1
```

**Note:** Replace `vpc-XXXXXXXX` with your VPC ID (same as OASIS API)

### Step 6: Add ALB Listener Rule

```bash
# Get ALB ARN (same ALB as oasisweb4.one)
ALB_ARN="arn:aws:elasticloadbalancing:us-east-1:881490134703:loadbalancer/app/oasis-api-alb/..."

# Get listener ARN (HTTPS:443)
LISTENER_ARN=$(aws elbv2 describe-listeners --load-balancer-arn ${ALB_ARN} --region us-east-1 | jq -r '.Listeners[] | select(.Port == 443) | .ListenerArn')

# Get target group ARN
TG_ARN=$(aws elbv2 describe-target-groups --names nft-frontend-tg --region us-east-1 | jq -r '.TargetGroups[0].TargetGroupArn')

# Add listener rule for nft.oasisweb4.one
aws elbv2 create-rule \
  --listener-arn ${LISTENER_ARN} \
  --priority 10 \
  --conditions '[{"Field":"host-header","Values":["nft.oasisweb4.one"]}]' \
  --actions '[{"Type":"forward","TargetGroupArn":"'"${TG_ARN}"'"}]' \
  --region us-east-1
```

### Step 7: Create ECS Service

```bash
# Get subnets and security groups from existing OASIS service
OASIS_SERVICE=$(aws ecs describe-services --cluster oasis-api-cluster --services oasis-api-service --region us-east-1)
SUBNETS=$(echo $OASIS_SERVICE | jq -r '.services[0].networkConfiguration.awsvpcConfiguration.subnets | join(",")')
SECURITY_GROUPS=$(echo $OASIS_SERVICE | jq -r '.services[0].networkConfiguration.awsvpcConfiguration.securityGroups | join(",")')
TG_ARN=$(aws elbv2 describe-target-groups --names nft-frontend-tg --region us-east-1 | jq -r '.TargetGroups[0].TargetGroupArn')

# Create service
aws ecs create-service \
  --cluster oasis-api-cluster \
  --service-name nft-mint-frontend-service \
  --task-definition nft-mint-frontend-task \
  --desired-count 1 \
  --launch-type FARGATE \
  --network-configuration "awsvpcConfiguration={subnets=[${SUBNETS}],securityGroups=[${SECURITY_GROUPS}],assignPublicIp=ENABLED}" \
  --load-balancers "targetGroupArn=${TG_ARN},containerName=nft-mint-frontend,containerPort=3000" \
  --region us-east-1
```

### Step 8: Configure DNS (Route53)

```bash
# Get your Route53 hosted zone ID
HOSTED_ZONE_ID=$(aws route53 list-hosted-zones --region us-east-1 | jq -r '.HostedZones[] | select(.Name == "oasisweb4.one.") | .Id' | cut -d'/' -f3)

# Add DNS record
aws route53 change-resource-record-sets \
  --hosted-zone-id ${HOSTED_ZONE_ID} \
  --change-batch file://add_nft_subdomain.json \
  --region us-east-1
```

**Verify DNS:**
```bash
# Wait a few minutes, then:
nslookup nft.oasisweb4.one
```

---

## 🌐 Simpler Alternative: Vercel Deployment

If you want a faster deployment without managing infrastructure:

### Step 1: Install Vercel CLI

```bash
npm install -g vercel
```

### Step 2: Deploy to Vercel

```bash
cd "/Volumes/Storage 1/OASIS_CLEAN/nft-mint-frontend"
vercel
```

**Follow prompts:**
- Link to project? Yes
- Project name? nft-mint-studio
- Directory? ./
- Override settings? No

### Step 3: Add Custom Domain

```bash
# After initial deployment
vercel domains add nft.oasisweb4.one
```

**Then configure DNS:**
- Type: CNAME
- Name: nft
- Value: cname.vercel-dns.com

**Benefits:**
- ✅ 2-minute deployment
- ✅ Automatic SSL
- ✅ Global CDN
- ✅ Zero infrastructure management
- ✅ Free for hobby projects

---

## 🎯 Recommended Subdomains

Choose the best subdomain for your use case:

| Subdomain | Best For | Example URL |
|-----------|----------|-------------|
| **nft.oasisweb4.one** | Direct & clear | ✅ Recommended |
| **mint.oasisweb4.one** | Action-focused | Good alternative |
| **studio.oasisweb4.one** | Creative focus | For creators |
| **create.oasisweb4.one** | Creation focus | Alternative |

**I recommend: `nft.oasisweb4.one`** — Direct, clear, matches the product name.

---

## 🔧 Configuration Update

Update the frontend to use production API:

**In `nft-mint-frontend/src/app/(routes)/page-content.tsx`:**

```typescript
const DEVNET_URL = "http://devnet.oasisweb4.one";
const PRODUCTION_URL = "https://oasisweb4.one";  // ← Production API

// Use production by default, toggle for testing
const [useLocalApi, setUseLocalApi] = useState(false);
const baseUrl = useLocalApi ? DEVNET_URL : PRODUCTION_URL;
```

Or use environment variable:
```typescript
const baseUrl = process.env.NEXT_PUBLIC_OASIS_API_URL || "https://oasisweb4.one";
```

---

## ✅ Post-Deployment Verification

After deployment, test:

```bash
# 1. Check DNS resolves
nslookup nft.oasisweb4.one

# 2. Check site loads
curl -I https://nft.oasisweb4.one

# 3. Test NFT minting flow
# Open browser → https://nft.oasisweb4.one
# Complete wizard steps
# Verify NFT mints successfully
```

---

## 📊 Which Deployment Should You Choose?

| Option | Cost | Speed | Control | Best For |
|--------|------|-------|---------|----------|
| **AWS ECS** | $20-40/mo | 15 min setup | Full | Production with existing AWS |
| **Vercel** | Free-$20/mo | 2 min setup | Limited | Quick launch, testing |

**For your case:** Since you already have AWS ECS running for the API, I'd recommend **AWS ECS** for consistency.

---

## 🚀 Quick Start (Vercel - Fastest)

If you want to go live in 5 minutes:

```bash
cd "/Volumes/Storage 1/OASIS_CLEAN/nft-mint-frontend"

# Deploy to Vercel
npm install -g vercel
vercel --prod

# You'll get: https://nft-mint-frontend.vercel.app

# Then add custom domain:
vercel domains add nft.oasisweb4.one

# Configure DNS:
# CNAME nft → cname.vercel-dns.com
```

---

## 📞 Need Help?

**AWS ECS Issues:**
- Check CloudWatch logs: `/ecs/nft-mint-frontend`
- Verify security groups allow port 3000
- Check ALB target group health

**DNS Issues:**
- Wait 5-10 minutes for propagation
- Verify hosted zone ID is correct
- Check ALB listener rules

**Application Issues:**
- Check environment variables
- Verify API endpoint accessible
- Review Next.js build logs

---

**Which deployment method would you like to use?**

1. **AWS ECS** (matches your current infrastructure)
2. **Vercel** (fastest, easiest)

Let me know and I'll help you deploy! 🚀



