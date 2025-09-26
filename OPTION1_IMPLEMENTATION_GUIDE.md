# Option 1 Implementation Guide: Subdomain Architecture

## Overview
This guide implements the subdomain architecture where:
- `oasisweb4.one` → Website (GitHub Pages)
- `api.oasisweb4.one` → API (AWS ALB)
- `www.oasisweb4.one` → Website (CNAME to main domain)

## Phase 1: Deploy Website to GitHub Pages

### Step 1: Verify GitHub Actions Deployment
The GitHub Actions workflow is already configured and should trigger automatically when pushing to `max-build2` branch.

**Check deployment status:**
1. Go to GitHub repository: `https://github.com/NextGenSoftwareUK/OASIS`
2. Navigate to Actions tab
3. Look for "Deploy OASIS Web4 Site" workflow
4. Verify it completed successfully

### Step 2: Enable GitHub Pages
1. Go to repository Settings
2. Scroll to Pages section
3. Under Source, select "GitHub Actions"
4. The site will be available at: `https://nextgensoftwareuk.github.io/OASIS/`

### Step 3: Configure Custom Domain
1. In GitHub Pages settings, add custom domain: `oasisweb4.one`
2. GitHub will provide DNS instructions
3. Note: We'll use the DNS records provided in this guide

## Phase 2: Create API Subdomain

### Step 1: Apply DNS Changes
Use the provided DNS configuration files:

```bash
# Apply API subdomain DNS record
aws route53 change-resource-record-sets \
  --hosted-zone-id YOUR_HOSTED_ZONE_ID \
  --change-batch file://api-oasisweb4-dns-record.json

# Apply main domain DNS record (choose one option)
# Option A: A records (recommended)
aws route53 change-resource-record-sets \
  --hosted-zone-id YOUR_HOSTED_ZONE_ID \
  --change-batch file://oasisweb4-github-pages-dns-record.json

# Option B: CNAME record (alternative)
aws route53 change-resource-record-sets \
  --hosted-zone-id YOUR_HOSTED_ZONE_ID \
  --change-batch file://oasisweb4-github-pages-cname.json
```

### Step 2: Verify DNS Propagation
```bash
# Check API subdomain
dig api.oasisweb4.one

# Check main domain
dig oasisweb4.one

# Check www subdomain
dig www.oasisweb4.one
```

## Phase 3: Test and Verify

### Step 1: Test Website
- Visit `https://oasisweb4.one` (should show website)
- Visit `https://www.oasisweb4.one` (should show website)
- Test all website functionality

### Step 2: Test API
- Visit `https://api.oasisweb4.one/swagger/index.html` (should show API docs)
- Test API endpoints
- Verify all API functionality

### Step 3: Test Integration
- Ensure website can communicate with API
- Test any forms or interactive elements
- Verify CORS settings if needed

## Phase 4: Update Documentation

### Step 1: Update API Documentation
- Change all references from `oasisweb4.one` to `api.oasisweb4.one`
- Update Swagger documentation
- Update any API client configurations

### Step 2: Update Website Content
- Update any hardcoded API URLs
- Update documentation links
- Update any configuration files

### Step 3: Update External References
- Update any external documentation
- Notify API users of the change
- Update any integrations or webhooks

## DNS Configuration Files

### 1. API Subdomain (`api-oasisweb4-dns-record.json`)
```json
{
  "Comment": "Create A record for api.oasisweb4.one pointing to ALB",
  "Changes": [
    {
      "Action": "UPSERT",
      "ResourceRecordSet": {
        "Name": "api.oasisweb4.one",
        "Type": "A",
        "AliasTarget": {
          "DNSName": "oasis-api-alb-2011847064.us-east-1.elb.amazonaws.com",
          "EvaluateTargetHealth": true,
          "HostedZoneId": "Z35SXDOTRQ7X7K"
        }
      }
    }
  ]
}
```

### 2. Main Domain - A Records (`oasisweb4-github-pages-dns-record.json`)
```json
{
  "Comment": "Update A record for oasisweb4.one to point to GitHub Pages",
  "Changes": [
    {
      "Action": "UPSERT",
      "ResourceRecordSet": {
        "Name": "oasisweb4.one",
        "Type": "A",
        "TTL": 300,
        "ResourceRecords": [
          {"Value": "185.199.108.153"},
          {"Value": "185.199.109.153"},
          {"Value": "185.199.110.153"},
          {"Value": "185.199.111.153"}
        ]
      }
    }
  ]
}
```

### 3. Main Domain - CNAME (`oasisweb4-github-pages-cname.json`)
```json
{
  "Comment": "Create CNAME record for oasisweb4.one pointing to GitHub Pages",
  "Changes": [
    {
      "Action": "UPSERT",
      "ResourceRecordSet": {
        "Name": "oasisweb4.one",
        "Type": "CNAME",
        "TTL": 300,
        "ResourceRecords": [
          {"Value": "nextgensoftwareuk.github.io"}
        ]
      }
    }
  ]
}
```

## Rollback Plan

If issues occur, rollback by:

1. **Revert DNS changes:**
   ```bash
   # Restore original DNS configuration
   aws route53 change-resource-record-sets \
     --hosted-zone-id YOUR_HOSTED_ZONE_ID \
     --change-batch file://oasisweb4-dns-record.json
   ```

2. **Disable GitHub Pages custom domain**
3. **Test API functionality**
4. **Investigate and fix issues**
5. **Retry implementation**

## Monitoring

### Key Metrics to Monitor
- Website availability and performance
- API availability and performance
- DNS resolution times
- SSL certificate status
- User experience metrics

### Alerts to Set Up
- Website down alerts
- API down alerts
- DNS resolution failures
- SSL certificate expiration warnings

## Timeline

- **Phase 1**: 30 minutes (GitHub Pages deployment)
- **Phase 2**: 15 minutes (DNS changes)
- **Phase 3**: 30 minutes (Testing and verification)
- **Phase 4**: 60 minutes (Documentation updates)

**Total estimated time**: 2-3 hours

## Success Criteria

- [ ] Website loads at `https://oasisweb4.one`
- [ ] API accessible at `https://api.oasisweb4.one/swagger/index.html`
- [ ] All website functionality works
- [ ] All API functionality works
- [ ] No broken links or references
- [ ] SSL certificates valid
- [ ] Performance within acceptable limits

---
*Implementation guide created on September 26, 2025*
*Status: Ready for execution*
