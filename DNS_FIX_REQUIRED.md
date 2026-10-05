# DNS Fix Required for oasisweb4.one

## Issue
The domain `oasisweb4.one` is currently pointing to a domain parking service (`91.195.240.12`) instead of the AWS Application Load Balancer where the API is running.

## Current Status
- ✅ API is **working perfectly** on the ALB: `oasis-api-alb-2011847064.us-east-1.elb.amazonaws.com`
- ✅ Docker image successfully deployed to ECR
- ✅ ECS service updated with new task definition (revision 18)
- ❌ DNS not pointing to ALB - showing parking page instead

## Solution

### Option 1: Use Route53 (Recommended)
If the domain DNS is managed in Route53, apply the DNS record change:

```bash
# Requires Route53 permissions
cd /Volumes/Storage\ 3/OASIS_CLEAN
./docker/fix-dns.sh
```

Or manually:
```bash
aws route53 change-resource-record-sets \
  --hosted-zone-id Z35SXDOTRQ7X7K \
  --change-batch file://oasisweb4-dns-record.json \
  --region us-east-1
```

### Option 2: Manual DNS Update
If DNS is managed elsewhere (GoDaddy, Namecheap, etc.):

1. Log into your domain registrar/DNS provider
2. Find the A record for `oasisweb4.one`
3. Update it to point to the ALB:
   - **Type**: A (Alias if supported)
   - **Value**: `oasis-api-alb-2011847064.us-east-1.elb.amazonaws.com`
   - Or use ALB IPs: `54.158.72.230` or `44.219.159.30` (note: ALB IPs can change)

### Option 3: Use ALB Directly (Temporary)
While waiting for DNS to update, you can access the API directly:
- **Swagger UI**: `http://oasis-api-alb-2011847064.us-east-1.elb.amazonaws.com/swagger/index.html`
- **API Base**: `http://oasis-api-alb-2011847064.us-east-1.elb.amazonaws.com/api/`

## Verification

After DNS update, verify with:

```bash
# Check DNS resolution
dig oasisweb4.one
# Should show ALB IPs, not 91.195.240.12

# Test API endpoint
curl -I http://oasisweb4.one/swagger/index.html
# Should return HTTP 200 with Kestrel server header, not "Parking/1.0"
```

## DNS Configuration File

The correct DNS configuration is in `oasisweb4-dns-record.json`:
- Points `oasisweb4.one` to ALB alias record
- Hosted Zone ID: `Z35SXDOTRQ7X7K`
- ALB DNS: `oasis-api-alb-2011847064.us-east-1.elb.amazonaws.com`

## Notes

- DNS propagation typically takes 1-5 minutes after update
- The ALB is active and healthy
- The API is fully deployed and working on the ALB
- Current IAM user doesn't have Route53 permissions, so DNS update needs to be done by someone with appropriate access

