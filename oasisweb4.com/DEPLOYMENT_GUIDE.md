# OASIS Web4 Site - Deployment Guide for David

## Overview

This guide explains how to make changes to the OASIS Web4 website and deploy them to Vercel.

**Live Site**: https://www.oasisweb4.com  
**Repository**: https://github.com/NextGenSoftwareUK/OASIS (max-build2 branch)  
**Site Location**: `/oasisweb4.com/` directory in the repository (NOT `/oasisweb4 site/new-v2/`) repository (NOT `/oasisweb4 site/new-v2/`)

## Prerequisites

1. **Git Access**: You need access to the `NextGenSoftwareUK/OASIS` repository
2. **Vercel CLI**: Install Vercel CLI (if deploying manually)
   ```bash
   npm i -g vercel
   ```
3. **Vercel Account**: Access to the Vercel project (or use CLI with authentication)

## Making Changes

### Option 1: Work with the Repository (Recommended)

1. **Clone the repository** (if you haven't already):
   ```bash
   git clone https://github.com/NextGenSoftwareUK/OASIS.git
   cd OASIS
   git checkout max-build2
   ```

2. **Navigate to the site directory**:
   ```bash
   cd oasisweb4.com
   ```

3. **Make your changes** to:
   - `index.html` - Main homepage
   - `styles.css` - Styling
   - `script.js` - JavaScript functionality
   - `products/*.html` - Product pages
   - `resources.html` - Resources page

4. **Test locally**:
   - Open `index.html` in a browser, or
   - Use a local server: `python -m http.server 8000` then visit `http://localhost:8000`

5. **Commit and push**:
   ```bash
   git add .
   git commit -m "Description of your changes"
   git push origin max-build2
   ```

### Option 2: Work with the Standalone Repository

If you're using the new standalone repository:

1. **Clone the standalone repository**:
   ```bash
   git clone https://github.com/NextGenSoftwareUK/oasisweb4.com.git
   cd oasisweb4.com
   ```

2. **Make your changes** (same files as above)

3. **Commit and push**:
   ```bash
   git add .
   git commit -m "Description of your changes"
   git push origin main
   ```

## Deploying to Vercel

### Method 1: Automatic Deployment (Recommended)

If Vercel is connected to the GitHub repository:

1. **Push your changes** to the repository (as shown above)
2. **Vercel will automatically deploy** when you push to:
   - `max-build2` branch (for OASIS repo)
   - `main` branch (for standalone repo)

3. **Check deployment status**:
   - Go to: https://vercel.com/max-gershfields-projects/oasisweb4.com
   - View the "Deployments" tab to see build status

### Method 2: Manual Deployment via CLI

If you need to deploy manually:

1. **Navigate to the site directory**:
   ```bash
   cd oasisweb4.com
   # OR if using standalone repo:
   cd oasisweb4.com  # (the repo root)
   ```

2. **Login to Vercel** (if not already logged in):
   ```bash
   vercel login
   ```

3. **Link to the project** (first time only):
   ```bash
   vercel link
   # Select: max-gershfields-projects
   # Select: oasisweb4.com
   ```

4. **Deploy to production**:
   ```bash
   vercel --prod
   ```

5. **Verify deployment**:
   - Visit: https://www.oasisweb4.com
   - Check the deployment URL provided by Vercel CLI

## Project Structure

```
oasisweb4.com/
├── index.html          # Main homepage
├── styles.css          # Global styles
├── script.js           # JavaScript functionality
├── resources.html      # Resources page
├── products/          # Product detail pages
│   ├── identity.html
│   ├── data.html
│   ├── nfts.html
│   ├── smart-contracts.html
│   ├── bridges.html
│   └── infrastructure.html
├── logos/             # Ecosystem partner logos
├── oasis-logo.png     # OASIS logo
└── vercel.json        # Vercel deployment configuration
```

## Important Files

### `vercel.json`
Configuration file for Vercel deployment. Contains:
- Build settings
- Rewrite rules
- Headers

**Do not modify** unless you understand Vercel configuration.

### `index.html`
Main homepage. Contains:
- Navigation
- Hero section
- All main sections (Principles, Stats, Products, Ecosystem, Code Examples)
- Footer
- Login modal

### `styles.css`
All styling for the site. Includes:
- Layout styles
- Component styles
- Responsive design
- Animations

### `script.js`
JavaScript functionality:
- Navigation
- Scroll effects
- Modal handling
- Code examples tabs
- Ecosystem filters

## Common Tasks

### Update Documentation Links

All documentation links point to:
- `https://oasis-web4.gitbook.io/oasis-web4-docs/`

To update a link, search for the old URL in `index.html` and replace it.

### Add a New Product Page

1. Create a new HTML file in `products/` directory
2. Follow the structure of existing product pages
3. Add a link in the Products section of `index.html`

### Update Ecosystem Logos

1. Add logo image to `logos/` directory
2. Update the ecosystem grid in `index.html` (around line 327)
3. Add the new ecosystem item with proper category

### Change Site Content

- **Hero text**: Edit `index.html` around line 73-88
- **Section content**: Each section has a `data-section` attribute
- **Footer links**: Edit footer section starting around line 566

## Troubleshooting

### Changes Not Showing

1. **Clear browser cache**: Hard refresh (Cmd+Shift+R or Ctrl+Shift+R)
2. **Check Vercel deployment**: Go to Vercel dashboard and verify the latest deployment succeeded
3. **Check file paths**: Ensure all file paths are correct (case-sensitive)

### Deployment Fails

1. **Check Vercel logs**: Go to deployment → "View Build Logs"
2. **Verify file structure**: Ensure all referenced files exist
3. **Check `vercel.json`**: Ensure configuration is valid JSON

### Local Testing Issues

1. **Use a local server**: Don't open HTML files directly (use `python -m http.server` or similar)
2. **Check browser console**: Look for JavaScript errors
3. **Verify file paths**: Relative paths should work from the root directory

## Vercel Project Details

- **Project Name**: `oasisweb4.com`
- **Organization**: `max-gershfields-projects`
- **Production URL**: https://www.oasisweb4.com
- **Project ID**: `prj_OgEtpZjsPzSx1IiIBOaGynKyY4Sn`

## Getting Help

- **Vercel Dashboard**: https://vercel.com/max-gershfields-projects/oasisweb4.com
- **Vercel Documentation**: https://vercel.com/docs
- **GitHub Repository**: https://github.com/NextGenSoftwareUK/OASIS

## Quick Reference Commands

```bash
# Navigate to site directory
cd oasisweb4.com

# Check git status
git status

# Add all changes
git add .

# Commit changes
git commit -m "Your commit message"

# Push to repository
git push origin max-build2

# Deploy to Vercel (manual)
vercel --prod

# Check Vercel project
vercel project ls

# View deployment
vercel inspect
```

## Notes

- The site is a static HTML/CSS/JavaScript site (no build process needed)
- All assets (images, logos) are in the repository
- Vercel automatically serves static files
- The `vercel.json` file handles routing for single-page navigation

---

**Last Updated**: December 16, 2025

