# Quick Start Guide - Deploying to Vercel from max-build2

## For David - Quick Deployment Steps

### 1. Setup (One Time Only)

```bash
# Clone the repository
git clone https://github.com/NextGenSoftwareUK/OASIS.git
cd OASIS

# Switch to max-build2 branch
git checkout max-build2

# Navigate to site directory (IMPORTANT: use oasisweb4.com, NOT new-v2)
cd oasisweb4.com

# Install Vercel CLI (if not already installed)
npm i -g vercel

# Login to Vercel
vercel login

# Link to the project
vercel link
# When prompted:
# - Select: max-gershfields-projects
# - Select: oasisweb4.com
```

### 2. Make Changes

Edit files in the `oasisweb4.com/` directory:
- `index.html` - Main page content
- `styles.css` - Styling
- `script.js` - JavaScript

### 3. Deploy to Vercel

**Option A: Automatic (if Vercel is connected to GitHub)**
```bash
# Commit and push
git add .
git commit -m "Your changes"
git push origin max-build2
# Vercel will auto-deploy
```

**Option B: Manual Deployment**
```bash
# From the oasisweb4.com directory
vercel --prod
```

### 4. Verify

- Visit: https://www.oasisweb4.com
- Check: https://vercel.com/max-gershfields-projects/oasisweb4.com

## Important Notes

- **Site Location**: `/oasisweb4.com/` directory in the OASIS repository
- **Branch**: `max-build2`
- **Project**: `oasisweb4.com` on Vercel
- **No build needed**: It's a static site, just HTML/CSS/JS

## Need Help?

See `DEPLOYMENT_GUIDE.md` for detailed instructions.

