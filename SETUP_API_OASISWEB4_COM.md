# Setup API on api.oasisweb4.com

## Overview
This guide will help you attach the OASIS API to `api.oasisweb4.com` subdomain, which will point to your existing AWS Application Load Balancer.

## Architecture
- **Website**: `oasisweb4.com` → Your website hosting (current setup)
- **API**: `api.oasisweb4.com` → AWS ALB → ECS API container

## Prerequisites
- Domain `oasisweb4.com` is registered and DNS is managed
- AWS ALB is already running: `oasis-api-alb-2011847064.us-east-1.elb.amazonaws.com`
- Access to DNS management for oasisweb4.com

## Setup Steps

### Option 1: Using Route53 (If DNS is managed in Route53)

1. **Find your hosted zone ID:**
   ```bash
   aws route53 list-hosted-zones --query "HostedZones[?contains(Name, 'oasisweb4.com')].{Name:Name,Id:Id}" --output json
   ```

2. **Update the DNS record file with your hosted zone ID:**
   - Edit `api-oasisweb4-com-dns-record.json`
   - Replace `Z35SXDOTRQ7X7K` with your actual hosted zone ID if different

3. **Apply the DNS record:**
   ```bash
   aws route53 change-resource-record-sets \
     --hosted-zone-id YOUR_HOSTED_ZONE_ID \
     --change-batch file://api-oasisweb4-com-dns-record.json \
     --region us-east-1
   ```

### Option 2: Manual DNS Configuration (Any DNS Provider)

Add a new DNS record at your domain registrar/DNS provider:

**Record Type**: A (or Alias if supported, like AWS Route53)

**If using A Record (Alias recommended):**
- **Name/Host**: `api`
- **Type**: A (Alias)
- **Value/Target**: `oasis-api-alb-2011847064.us-east-1.elb.amazonaws.com`
- **TTL**: 300 (or default)

**If your DNS provider doesn't support ALIAS records**, you'll need to use CNAME:
- **Name/Host**: `api`
- **Type**: CNAME
- **Value/Target**: `oasis-api-alb-2011847064.us-east-1.elb.amazonaws.com`
- **TTL**: 300

⚠️ **Note**: Some DNS providers don't allow CNAME on root domain but allow it on subdomains. If CNAME isn't available, you can use A records pointing to ALB IPs, but ALB IPs can change, so ALIAS is preferred.

## ALB Configuration

The ALB is already configured to accept requests on any domain. No changes needed to the ALB listener rules - it will automatically accept requests for `api.oasisweb4.com`.

If you want to add specific routing rules or host-based routing in the future, you can update the ALB listener rules in the AWS Console.

## Verification

After DNS is configured, wait 1-5 minutes for propagation, then verify:

```bash
# Check DNS resolution
dig api.oasisweb4.com
# Should show ALB IPs (54.158.72.230, 44.219.159.30)

# Test API endpoint
curl -I http://api.oasisweb4.com/swagger/index.html
# Should return HTTP 200 with Kestrel server header

# Test with HTTPS (if SSL is configured)
curl -I https://api.oasisweb4.com/swagger/index.html
```

## SSL/HTTPS Setup (Optional but Recommended)

To enable HTTPS for `api.oasisweb4.com`:

1. **Request ACM Certificate:**
   ```bash
   aws acm request-certificate \
     --domain-name api.oasisweb4.com \
     --validation-method DNS \
     --region us-east-1
   ```

2. **Add DNS Validation Record:**
   - AWS ACM will provide DNS validation records
   - Add them to your DNS provider
   - Wait for validation (usually 5-30 minutes)

3. **Update ALB Listener:**
   - Go to AWS Console → EC2 → Load Balancers
   - Select your ALB
   - Add a new HTTPS listener (port 443)
   - Select the ACM certificate
   - Forward to the same target group

4. **Optional: Redirect HTTP to HTTPS:**
   - Update HTTP listener (port 80) to redirect to HTTPS

## Testing

Once DNS is configured:

- **Swagger UI**: `http://api.oasisweb4.com/swagger/index.html`
- **API Base**: `http://api.oasisweb4.com/api/`
- **Health Check**: `http://api.oasisweb4.com/api/health`

## DNS Propagation

DNS changes typically propagate within:
- **1-5 minutes**: Most DNS servers
- **Up to 48 hours**: Full global propagation (rare)

Check propagation status:
- https://dnschecker.org
- `dig api.oasisweb4.com @8.8.8.8` (Google DNS)
- `dig api.oasisweb4.com @1.1.1.1` (Cloudflare DNS)

## Troubleshooting

### DNS Not Resolving
- Wait a few more minutes for propagation
- Clear DNS cache: `sudo dscacheutil -flushcache` (Mac) or `ipconfig /flushdns` (Windows)
- Check DNS records are correct at your registrar

### Connection Refused
- Verify ALB is running: `aws elbv2 describe-load-balancers`
- Check security groups allow traffic on port 80
- Verify ECS tasks are running and healthy

### Wrong Content
- Verify ALB is pointing to correct target group
- Check ECS service is using latest task definition

