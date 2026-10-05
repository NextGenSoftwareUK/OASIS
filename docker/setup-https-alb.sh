#!/bin/bash

# Script to add HTTPS listener to OASIS API ALB
# This enables HTTPS access to api.oasisweb4.com

set -e

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo -e "${GREEN}🔒 OASIS API HTTPS Configuration${NC}"
echo "=================================="
echo ""

# Configuration
REGION="us-east-1"
DOMAIN="api.oasisweb4.com"
ALB_NAME="oasis-api-alb"

# Get ALB ARN
echo -e "${YELLOW}📋 Step 1: Finding ALB...${NC}"
ALB_ARN=$(aws elbv2 describe-load-balancers \
  --region $REGION \
  --query "LoadBalancers[?contains(LoadBalancerName, '$ALB_NAME')].LoadBalancerArn" \
  --output text)

if [ -z "$ALB_ARN" ]; then
  echo -e "${RED}❌ Error: ALB not found${NC}"
  exit 1
fi

echo -e "${GREEN}✅ Found ALB: ${ALB_ARN}${NC}"
echo ""

# Check existing listeners
echo -e "${YELLOW}📋 Step 2: Checking existing listeners...${NC}"
EXISTING_HTTPS=$(aws elbv2 describe-listeners \
  --load-balancer-arn $ALB_ARN \
  --region $REGION \
  --query "Listeners[?Port==\`443\`].ListenerArn" \
  --output text)

if [ ! -z "$EXISTING_HTTPS" ]; then
  echo -e "${GREEN}✅ HTTPS listener already exists on port 443${NC}"
  echo "Listener ARN: $EXISTING_HTTPS"
  exit 0
fi

# Get target group from HTTP listener
echo -e "${YELLOW}📋 Step 3: Getting target group from HTTP listener...${NC}"
TARGET_GROUP_ARN=$(aws elbv2 describe-listeners \
  --load-balancer-arn $ALB_ARN \
  --region $REGION \
  --query "Listeners[?Port==\`80\`].DefaultActions[0].TargetGroupArn" \
  --output text)

if [ -z "$TARGET_GROUP_ARN" ]; then
  echo -e "${RED}❌ Error: Could not find target group from HTTP listener${NC}"
  exit 1
fi

echo -e "${GREEN}✅ Target Group: ${TARGET_GROUP_ARN}${NC}"
echo ""

# Check for existing certificate
echo -e "${YELLOW}📋 Step 4: Checking for SSL certificate...${NC}"
CERT_ARN=$(aws acm list-certificates \
  --region $REGION \
  --query "CertificateSummaryList[?DomainName=='$DOMAIN'].CertificateArn" \
  --output text)

if [ -z "$CERT_ARN" ]; then
  echo -e "${YELLOW}⚠️  No certificate found for $DOMAIN${NC}"
  echo ""
  echo "Would you like to request a new certificate? (y/n)"
  read -r RESPONSE
  
  if [ "$RESPONSE" != "y" ]; then
    echo "Exiting. Please request a certificate first:"
    echo "  aws acm request-certificate --domain-name $DOMAIN --validation-method DNS --region $REGION"
    exit 1
  fi
  
  echo -e "${YELLOW}📋 Requesting certificate...${NC}"
  CERT_ARN=$(aws acm request-certificate \
    --domain-name $DOMAIN \
    --validation-method DNS \
    --region $REGION \
    --query 'CertificateArn' \
    --output text)
  
  echo -e "${GREEN}✅ Certificate requested: ${CERT_ARN}${NC}"
  echo ""
  echo -e "${YELLOW}⚠️  IMPORTANT: You must add DNS validation records before the certificate can be used.${NC}"
  echo ""
  echo "Get validation record:"
  echo "  aws acm describe-certificate --certificate-arn $CERT_ARN --region $REGION --query 'Certificate.DomainValidationOptions[0].ResourceRecord' --output json"
  echo ""
  echo "After adding DNS records and certificate is ISSUED, run this script again."
  exit 0
fi

# Check certificate status
CERT_STATUS=$(aws acm describe-certificate \
  --certificate-arn $CERT_ARN \
  --region $REGION \
  --query 'Certificate.Status' \
  --output text)

if [ "$CERT_STATUS" != "ISSUED" ]; then
  echo -e "${RED}❌ Certificate status: $CERT_STATUS (must be ISSUED)${NC}"
  echo "Certificate ARN: $CERT_ARN"
  echo ""
  echo "Please wait for certificate validation to complete, then run this script again."
  exit 1
fi

echo -e "${GREEN}✅ Certificate found and validated: ${CERT_ARN}${NC}"
echo ""

# Add HTTPS listener
echo -e "${YELLOW}📋 Step 5: Adding HTTPS listener (port 443)...${NC}"
LISTENER_ARN=$(aws elbv2 create-listener \
  --load-balancer-arn $ALB_ARN \
  --protocol HTTPS \
  --port 443 \
  --certificates CertificateArn=$CERT_ARN \
  --default-actions Type=forward,TargetGroupArn=$TARGET_GROUP_ARN \
  --region $REGION \
  --query 'Listeners[0].ListenerArn' \
  --output text)

if [ $? -eq 0 ]; then
  echo -e "${GREEN}✅ HTTPS listener created successfully!${NC}"
  echo "Listener ARN: $LISTENER_ARN"
  echo ""
  echo -e "${GREEN}✅ HTTPS is now enabled for $DOMAIN${NC}"
  echo ""
  echo "Test with:"
  echo "  curl https://$DOMAIN/api/avatar/health"
  echo ""
  echo -e "${YELLOW}💡 Optional: Update HTTP listener to redirect to HTTPS? (y/n)${NC}"
  read -r REDIRECT_RESPONSE
  
  if [ "$REDIRECT_RESPONSE" == "y" ]; then
    HTTP_LISTENER_ARN=$(aws elbv2 describe-listeners \
      --load-balancer-arn $ALB_ARN \
      --region $REGION \
      --query "Listeners[?Port==\`80\`].ListenerArn" \
      --output text)
    
    aws elbv2 modify-listener \
      --listener-arn $HTTP_LISTENER_ARN \
      --default-actions Type=redirect,RedirectConfig='{Protocol=HTTPS,Port=443,StatusCode=HTTP_301}' \
      --region $REGION
    
    echo -e "${GREEN}✅ HTTP to HTTPS redirect configured${NC}"
  fi
else
  echo -e "${RED}❌ Failed to create HTTPS listener${NC}"
  exit 1
fi

echo ""
echo -e "${GREEN}✅ HTTPS Configuration Complete!${NC}"



