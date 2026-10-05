#!/bin/bash

# Script to add HTTPS listener to OASIS API ALB
# Usage: ./add-https-listener.sh <CERTIFICATE_ARN>
# Example: ./add-https-listener.sh arn:aws:acm:us-east-1:881490134703:certificate/12345678-1234-1234-1234-123456789012

set -e

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

if [ -z "$1" ]; then
  echo -e "${RED}❌ Error: Certificate ARN required${NC}"
  echo ""
  echo "Usage: $0 <CERTIFICATE_ARN>"
  echo ""
  echo "Example:"
  echo "  $0 arn:aws:acm:us-east-1:881490134703:certificate/12345678-1234-1234-1234-123456789012"
  echo ""
  echo "To get certificate ARN, use AWS Console or run:"
  echo "  aws acm list-certificates --region us-east-1 --query 'CertificateSummaryList[?DomainName==\`api.oasisweb4.com\`].CertificateArn' --output text"
  exit 1
fi

CERT_ARN=$1
REGION="us-east-1"
ALB_ARN="arn:aws:elasticloadbalancing:us-east-1:881490134703:loadbalancer/app/oasis-api-alb/c71e804d6193e11c"

echo -e "${GREEN}🔒 Adding HTTPS Listener to OASIS API ALB${NC}"
echo "=================================="
echo "Certificate ARN: $CERT_ARN"
echo "ALB ARN: $ALB_ARN"
echo ""

# Check if HTTPS listener already exists
echo -e "${YELLOW}📋 Checking for existing HTTPS listener...${NC}"
EXISTING_HTTPS=$(aws elbv2 describe-listeners \
  --load-balancer-arn $ALB_ARN \
  --region $REGION \
  --query "Listeners[?Port==\`443\`].ListenerArn" \
  --output text 2>/dev/null || echo "")

if [ ! -z "$EXISTING_HTTPS" ]; then
  echo -e "${GREEN}✅ HTTPS listener already exists on port 443${NC}"
  echo "Listener ARN: $EXISTING_HTTPS"
  exit 0
fi

# Get target group from HTTP listener
echo -e "${YELLOW}📋 Getting target group from HTTP listener...${NC}"
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

# Add HTTPS listener
echo -e "${YELLOW}📋 Adding HTTPS listener (port 443)...${NC}"
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
  echo -e "${GREEN}✅ HTTPS is now enabled for api.oasisweb4.com${NC}"
  echo ""
  echo "Test with:"
  echo "  curl https://api.oasisweb4.com/api/avatar/health"
  echo ""
  echo -e "${YELLOW}💡 Optional: Configure HTTP to HTTPS redirect? (y/n)${NC}"
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



