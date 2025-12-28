# API HTTPS Configuration Requirement

## Issue Summary

The OASIS Portal (`https://oportal.oasisweb4.com`) requires HTTPS access to the OASIS API (`api.oasisweb4.com`) to function properly. Currently, the API only supports HTTP, which causes authentication failures due to browser mixed content security restrictions.

## Problem

- **Portal**: Served over HTTPS at `https://oportal.oasisweb4.com`
- **API**: Currently only accessible via HTTP at `http://api.oasisweb4.com` (port 80)
- **Browser Security**: Modern browsers block HTTP requests from HTTPS pages (mixed content policy)
- **Result**: Portal authentication fails with `ERR_CONNECTION_REFUSED` when attempting to use HTTPS

## Current Status (Verified)

✅ **HTTP API**: Working correctly on port 80 via ALB  
❌ **HTTPS API**: Not configured - ALB only has HTTP listener (port 80)  
✅ **API Code**: Ready for HTTPS - configured to handle forwarded headers from ALB  
✅ **ALB**: `oasis-api-alb-2011847064.us-east-1.elb.amazonaws.com` exists and is working

## Required Action

Configure HTTPS/TLS for `api.oasisweb4.com` to enable secure API access from the HTTPS portal.

## Technical Requirements

1. **SSL/TLS Certificate**: Valid certificate for `api.oasisweb4.com`
2. **Port 443**: HTTPS endpoint must be accessible on port 443
3. **API Endpoint**: `https://api.oasisweb4.com` must respond to requests
4. **CORS**: Ensure CORS headers allow requests from `https://oportal.oasisweb4.com`

## Implementation Steps (AWS ALB - Recommended)

The API is already deployed on AWS ECS behind an Application Load Balancer. We need to add an HTTPS listener to the existing ALB.

### Quick Setup (If Certificate Already Exists)

If you already have an ACM certificate for `api.oasisweb4.com`, add the HTTPS listener:

```bash
cd /Volumes/Storage/OASIS_CLEAN

# Get certificate ARN (requires ACM permissions)
CERT_ARN=$(aws acm list-certificates --region us-east-1 \
  --query "CertificateSummaryList[?DomainName=='api.oasisweb4.com'].CertificateArn" \
  --output text)

# Add HTTPS listener
./docker/add-https-listener.sh $CERT_ARN
```

Or provide the certificate ARN directly:
```bash
./docker/add-https-listener.sh arn:aws:acm:us-east-1:881490134703:certificate/YOUR-CERT-ID
```

### Manual Setup (Step-by-Step)

### Step 1: Request SSL Certificate in ACM

```bash
# Request certificate for api.oasisweb4.com
aws acm request-certificate \
  --domain-name api.oasisweb4.com \
  --validation-method DNS \
  --region us-east-1 \
  --query 'CertificateArn' \
  --output text
```

**Note**: Save the CertificateArn returned - you'll need it in Step 3.

### Step 2: Add DNS Validation Record

1. Get the validation record from ACM:
   ```bash
   aws acm describe-certificate \
     --certificate-arn <CERTIFICATE_ARN> \
     --region us-east-1 \
     --query 'Certificate.DomainValidationOptions[0].ResourceRecord' \
     --output json
   ```

2. Add the DNS validation record to your DNS provider (Route53 or other):
   - **Type**: CNAME
   - **Name**: The `Name` from the validation record
   - **Value**: The `Value` from the validation record

3. Wait for validation (usually 5-30 minutes):
   ```bash
   aws acm describe-certificate \
     --certificate-arn <CERTIFICATE_ARN> \
     --region us-east-1 \
     --query 'Certificate.Status' \
     --output text
   ```
   Wait until status is `ISSUED`.

### Step 3: Add HTTPS Listener to ALB

```bash
# Get the ALB ARN
ALB_ARN=$(aws elbv2 describe-load-balancers \
  --region us-east-1 \
  --query "LoadBalancers[?contains(DNSName, 'oasis-api-alb')].LoadBalancerArn" \
  --output text)

# Get the target group ARN (used by HTTP listener)
TARGET_GROUP_ARN=$(aws elbv2 describe-listeners \
  --load-balancer-arn $ALB_ARN \
  --region us-east-1 \
  --query "Listeners[?Port==\`80\`].DefaultActions[0].TargetGroupArn" \
  --output text)

# Add HTTPS listener on port 443
aws elbv2 create-listener \
  --load-balancer-arn $ALB_ARN \
  --protocol HTTPS \
  --port 443 \
  --certificates CertificateArn=<CERTIFICATE_ARN> \
  --default-actions Type=forward,TargetGroupArn=$TARGET_GROUP_ARN \
  --region us-east-1
```

### Step 4: (Optional) Redirect HTTP to HTTPS

Update the HTTP listener to redirect to HTTPS:

```bash
aws elbv2 modify-listener \
  --listener-arn $(aws elbv2 describe-listeners \
    --load-balancer-arn $ALB_ARN \
    --region us-east-1 \
    --query "Listeners[?Port==\`80\`].ListenerArn" \
    --output text) \
  --default-actions Type=redirect,RedirectConfig='{Protocol=HTTPS,Port=443,StatusCode=HTTP_301}' \
  --region us-east-1
```

### Alternative: AWS Console Method

1. Go to **EC2 Console** → **Load Balancers**
2. Select `oasis-api-alb`
3. Click **Listeners** tab → **Add listener**
4. Configure:
   - **Protocol**: HTTPS
   - **Port**: 443
   - **Default action**: Forward to existing target group
   - **Certificate**: Select the ACM certificate for `api.oasisweb4.com`
5. Click **Save**
6. (Optional) Edit HTTP listener → Change action to **Redirect to HTTPS**

## Testing

Once HTTPS is configured, verify with:

```bash
# Health check
curl https://api.oasisweb4.com/api/avatar/health

# Authentication test
curl -X POST https://api.oasisweb4.com/api/avatar/authenticate \
  -H "Content-Type: application/json" \
  -d '{"username":"test@example.com","password":"test"}'
```

## Expected Behavior After Fix

- Portal can successfully authenticate users
- All API calls from portal work correctly
- No mixed content errors in browser console
- Secure end-to-end communication

## Portal Code Status

✅ Portal code is ready and correctly configured to use HTTPS when available  
✅ Automatic protocol detection: Uses HTTPS when portal is HTTPS, HTTP when portal is HTTP  
✅ Error handling: Clear error messages if HTTPS API is unavailable

## Priority

**High** - Portal authentication is currently non-functional for production users.

## Contact

For questions or implementation details, refer to the OASIS API deployment documentation or infrastructure team.

---

**Document Created**: 2025-12-20  
**Last Updated**: 2025-12-22  
**Status**: ⚠️ **Action Required** - HTTPS listener needs to be added to ALB

## Quick Reference

- **ALB ARN**: `arn:aws:elasticloadbalancing:us-east-1:881490134703:loadbalancer/app/oasis-api-alb/c71e804d6193e11c`
- **ALB DNS**: `oasis-api-alb-2011847064.us-east-1.elb.amazonaws.com`
- **Current Listeners**: HTTP (port 80) only
- **Required**: Add HTTPS listener (port 443) with ACM certificate

