# Vercel Deployment Guide for timorides.com

## Prerequisites
- GitHub repository: `maxgershfield/timo-rides-pp-lp` (or transfer to `timo-org/timo-rides-lp`)
- Vercel account (sign up at https://vercel.com if needed)
- Domain: timorides.com (DNS already pointing to Vercel nameservers)

## Deployment Steps

### Option 1: Deploy via Vercel Dashboard (Recommended)

1. **Go to Vercel Dashboard**
   - Visit https://vercel.com/dashboard
   - Sign in with your GitHub account

2. **Import Project**
   - Click "Add New" → "Project"
   - Import from GitHub: `maxgershfield/timo-rides-pp-lp`
   - Vercel will auto-detect it's an Astro project

3. **Configure Project Settings**
   - **Framework Preset**: Astro (auto-detected)
   - **Root Directory**: `./` (root)
   - **Build Command**: `npm run build` (default)
   - **Output Directory**: `dist` (Astro default)
   - **Install Command**: `npm install` (default)

4. **Add Domain**
   - After deployment, go to Project Settings → Domains
   - Add domain: `timorides.com`
   - Also add: `www.timorides.com` (optional, for www redirect)
   - Vercel will provide DNS records if needed, but since your DNS is already pointing to Vercel nameservers, it should work automatically

5. **Deploy**
   - Click "Deploy"
   - Wait for build to complete
   - Your site will be live at timorides.com!

### Option 2: Deploy via Vercel CLI

1. **Install Vercel CLI** (if not installed):
   ```bash
   npm install -g vercel
   ```

2. **Login to Vercel**:
   ```bash
   vercel login
   ```

3. **Deploy**:
   ```bash
   cd /Volumes/Storage/OASIS_CLEAN/timo-rides-lp
   vercel
   ```

4. **Add Domain**:
   ```bash
   vercel domains add timorides.com
   ```

5. **Production Deploy**:
   ```bash
   vercel --prod
   ```

## Domain Configuration

Since your Hostinger DNS settings already point to Vercel nameservers, you just need to:

1. Add the domain in Vercel dashboard (Project Settings → Domains)
2. Vercel will verify domain ownership
3. Once verified, traffic will automatically route to your deployment

## Environment Variables

If you need any environment variables later, add them in:
- Vercel Dashboard → Project Settings → Environment Variables

## Automatic Deployments

Vercel will automatically deploy:
- Every push to `main` branch → Production
- Pull requests → Preview deployments

## Troubleshooting

- **Build fails**: Check build logs in Vercel dashboard
- **Domain not working**: Verify DNS settings in Hostinger match Vercel's requirements
- **404 errors**: Ensure `vercel.json` is configured correctly (already set up)

## Notes

- The `vercel.json` file is already configured for Astro
- Build output goes to `dist/` directory
- Vercel has built-in support for Astro, so it should work out of the box




