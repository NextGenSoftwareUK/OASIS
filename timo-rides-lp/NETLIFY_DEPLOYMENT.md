# Netlify Deployment Guide

## Repository
✅ Site is now in: `timo-org/pathpulse_landingpage`

## Deploy to Netlify

### Step 1: Import Project in Netlify

1. Go to https://app.netlify.com
2. Click "Add new site" → "Import an existing project"
3. Click "Deploy with GitHub"
4. Authorize Netlify to access your GitHub account
5. Select repository: `timo-org/pathpulse_landingpage`

### Step 2: Configure Build Settings

Netlify should auto-detect Astro, but verify these settings:

- **Build command:** `npm run build`
- **Publish directory:** `dist`
- **Node version:** 18 (or latest)

Click "Deploy site"

### Step 3: Add Custom Domain

1. After deployment succeeds, go to **Site settings** → **Domain management**
2. Click "Add custom domain"
3. Enter: `timorides.com`
4. Click "Verify"

### Step 4: Update DNS in Hostinger

Netlify will show you DNS settings. You have two options:

#### Option A: Use Netlify Nameservers (Recommended)

1. In Netlify, go to Domain settings → DNS
2. Copy the nameservers (usually something like):
   - `dns1.p01.nsone.net`
   - `dns2.p01.nsone.net`
   - `dns3.p01.nsone.net`
   - `dns4.p01.nsone.net`

3. In Hostinger:
   - Go to DNS settings for `timorides.com`
   - Replace current nameservers with Netlify's nameservers
   - Save changes

#### Option B: Use DNS Records (Keep Current Nameservers)

If you want to keep Vercel nameservers or use Hostinger's:

1. In Netlify Domain settings, you'll see DNS records needed
2. Add these records in Hostinger:
   - **A Record:** `@` → Netlify IP (they'll provide)
   - **CNAME:** `www` → `your-site.netlify.app`

### Step 5: Wait for DNS Propagation

- DNS changes can take 24-48 hours (usually much faster)
- Netlify will show domain status
- Once verified, your site will be live at `timorides.com`

## Automatic Deployments

Netlify will automatically deploy:
- Every push to `main` branch → Production
- Pull requests → Preview deployments

## Files Already Configured

✅ `netlify.toml` - Build configuration
✅ `_redirects` - SPA routing support

## Troubleshooting

- **Build fails:** Check build logs in Netlify dashboard
- **Domain not working:** Verify DNS settings match Netlify's requirements
- **404 errors:** Check that `_redirects` file is in place

## Next Steps

1. Deploy to Netlify (follow steps above)
2. Add domain `timorides.com`
3. Update DNS in Hostinger
4. Wait for propagation
5. Site will be live! 🎉




