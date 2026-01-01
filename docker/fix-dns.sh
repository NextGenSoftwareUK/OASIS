#!/bin/bash

# Fix DNS for oasisweb4.one to point to the ALB
# Requires Route53 permissions
# Run: ./docker/fix-dns.sh

set -e

AWS_REGION="us-east-1"
HOSTED_ZONE_ID="Z35SXDOTRQ7X7K"
ALB_DNS_NAME="oasis-api-alb-2011847064.us-east-1.elb.amazonaws.com"
ALB_HOSTED_ZONE_ID="Z35SXDOTRQ7X7K"

echo "🔧 Fixing DNS for oasisweb4.one"
echo "=================================="
echo "Hosted Zone ID: ${HOSTED_ZONE_ID}"
echo "ALB DNS: ${ALB_DNS_NAME}"
echo ""

# Apply the DNS record change
echo "Applying DNS record update..."
CHANGE_ID=$(aws route53 change-resource-record-sets \
  --hosted-zone-id ${HOSTED_ZONE_ID} \
  --change-batch file://oasisweb4-dns-record.json \
  --region ${AWS_REGION} \
  --query 'ChangeInfo.Id' \
  --output text)

if [ $? -eq 0 ]; then
  echo "✅ DNS record update initiated"
  echo "Change ID: ${CHANGE_ID}"
  echo ""
  echo "Checking change status..."
  aws route53 get-change --id ${CHANGE_ID} --region ${AWS_REGION} --query 'ChangeInfo.Status' --output text
  echo ""
  echo "⏳ DNS propagation may take a few minutes (typically 1-5 minutes)"
  echo ""
  echo "After propagation, verify with:"
  echo "  dig oasisweb4.one"
  echo "  curl -I http://oasisweb4.one/swagger/index.html"
else
  echo "❌ Failed to update DNS record"
  exit 1
fi

