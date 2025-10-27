# OASIS Website Deployment Report

## Current Status (September 26, 2025)

### API Infrastructure
- **Domain**: `oasisweb4.one`
- **Current DNS**: Points to AWS Load Balancer (`oasis-api-alb-2011847064.us-east-1.elb.amazonaws.com`)
- **API Endpoint**: `http://oasisweb4.one/swagger/index.html`
- **Status**: ✅ Live and operational
- **Infrastructure**: AWS ECS with Application Load Balancer

### Website Infrastructure
- **Website Files**: Located in `oasisweb4 site/` directory
- **GitHub Actions**: Configured to deploy to GitHub Pages
- **Branch**: `max-build2` (workflow updated to trigger on this branch)
- **Local Testing**: ✅ Working at `http://localhost:8080`
- **Status**: Ready for deployment

### DNS Configuration Files
- `oasisweb4-dns-record.json` - Main domain A record pointing to ALB
- `oasisweb4-www-dns-record.json` - WWW subdomain CNAME to main domain

## Deployment Options

### Option 1: Subdomain Architecture (Recommended)
**Structure:**
- `oasisweb4.one` → Website (GitHub Pages)
- `api.oasisweb4.one` → API (AWS ALB)
- `www.oasisweb4.one` → Website (CNAME to main domain)

**Pros:**
- Clean separation of concerns
- Easy to manage and scale independently
- Standard web architecture pattern
- No conflicts between API and website

**Cons:**
- Requires DNS changes
- API moves from root domain

**Implementation:**
1. Deploy website to GitHub Pages
2. Create `api.oasisweb4.one` A record pointing to ALB
3. Update `oasisweb4.one` A record to point to GitHub Pages
4. Keep `www.oasisweb4.one` CNAME to main domain

### Option 2: Path-Based Routing
**Structure:**
- `oasisweb4.one` → Website (GitHub Pages)
- `oasisweb4.one/api` → API (via reverse proxy)

**Pros:**
- Single domain for everything
- No subdomain management

**Cons:**
- Requires reverse proxy setup
- More complex configuration
- Potential conflicts with API paths

**Implementation:**
1. Deploy website to GitHub Pages
2. Set up Cloudflare or similar reverse proxy
3. Route `/api/*` to AWS ALB
4. Route everything else to GitHub Pages

### Option 3: Keep Current Setup + Separate Website
**Structure:**
- `oasisweb4.one` → API (current setup)
- `www.oasisweb4.one` → Website (GitHub Pages)
- `site.oasisweb4.one` → Website (alternative)

**Pros:**
- No changes to existing API
- Minimal disruption

**Cons:**
- API not on root domain for website
- Less intuitive for users

**Implementation:**
1. Deploy website to GitHub Pages
2. Create new subdomain for website
3. Update DNS records for new subdomain

### Option 4: Full Migration to GitHub Pages
**Structure:**
- `oasisweb4.one` → Website (GitHub Pages)
- `api.oasisweb4.one` → API (AWS ALB)

**Pros:**
- Website on root domain
- Clean, professional setup

**Cons:**
- Requires API migration to subdomain
- Potential downtime during transition

## Technical Details

### GitHub Actions Workflow
```yaml
name: Deploy OASIS Web4 Site
on:
  push:
    branches: [ max-build2 ]
    paths: [ 'oasisweb4 site/**' ]
  workflow_dispatch:

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
    - name: Checkout
      uses: actions/checkout@v4
    - name: Create deployment directory
      run: |
        mkdir -p deploy
        cp -r "oasisweb4 site"/* deploy/
    - name: Remove large files
      run: |
        find deploy -name "*.mp4" -delete
        find deploy -name "*.webm" -delete
        find deploy -name "*.mov" -delete
        find deploy -name "*.avi" -delete
        find deploy -name "*.pdf" -size +5M -delete
    - name: Deploy to GitHub Pages
      uses: peaceiris/actions-gh-pages@v3
      with:
        github_token: ${{ secrets.GITHUB_TOKEN }}
        publish_dir: ./deploy
        force_orphan: true
```

### Website Files Structure
```
oasisweb4 site/
├── index.html (main website)
├── index-simple.html (simplified version)
├── css/
│   ├── normalize.css
│   ├── webflow.css
│   └── oasisweb4.webflow.css
├── js/
│   └── webflow.js
└── images/ (259 files)
    ├── logos and branding
    ├── UI elements
    └── background images
```

### Current DNS Records
```json
{
  "oasisweb4.one": "A record → oasis-api-alb-2011847064.us-east-1.elb.amazonaws.com",
  "www.oasisweb4.one": "CNAME → oasisweb4.one"
}
```

## Recommendations

### Immediate Actions
1. **Deploy website to GitHub Pages** (workflow is ready)
2. **Test website functionality** on GitHub Pages
3. **Choose deployment option** based on business requirements

### Recommended Approach: Option 1 (Subdomain Architecture)
1. **Phase 1**: Deploy website to GitHub Pages
2. **Phase 2**: Create `api.oasisweb4.one` subdomain
3. **Phase 3**: Update main domain to point to website
4. **Phase 4**: Test and verify all functionality

### Risk Mitigation
- Test all changes in staging environment
- Have rollback plan ready
- Monitor API functionality during transition
- Update all documentation and links

## Next Steps

1. **Review this report** and choose deployment option
2. **Execute chosen option** with proper testing
3. **Update documentation** and notify stakeholders
4. **Monitor performance** and user experience

## Files Modified
- `.github/workflows/deploy-oasisweb4.yml` - Updated to trigger on max-build2
- `oasisweb4 site/` - Website files committed and ready for deployment

## Commands Used
```bash
# Restore website files from git history
git checkout 0134d7ab -- "oasisweb4 site"

# Commit website files
git commit -m "Add OASIS Web4 website files for deployment"

# Update workflow
git commit -m "Update deployment workflow to trigger on max-build2 branch"

# Push to repository
git push origin max-build2
```

---
*Report generated on September 26, 2025*
*Status: Ready for deployment decision*



