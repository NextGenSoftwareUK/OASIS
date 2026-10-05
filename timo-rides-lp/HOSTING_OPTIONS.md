# Hosting Options for timorides.com

Since we don't have access to the Vercel account, here are alternative hosting options:

## Option 1: Netlify (Recommended - Easiest)

**Pros:**
- Free tier available
- Easy GitHub integration
- Automatic deployments
- Custom domains supported
- Similar to Vercel

**Setup:**
1. Sign up at https://netlify.com
2. Connect GitHub account
3. Import repository: `maxgershfield/timo-rides-pp-lp`
4. Build settings (auto-detected):
   - Build command: `npm run build`
   - Publish directory: `dist`
5. Add custom domain: `timorides.com`
6. Update DNS in Hostinger:
   - Change nameservers to Netlify's (they'll provide)
   - Or use CNAME/A records (Netlify will show you)

**Files created:** `netlify.toml` (already created)

## Option 2: Cloudflare Pages (Fast & Free)

**Pros:**
- Very fast CDN
- Free tier
- Easy GitHub integration
- Great performance

**Setup:**
1. Sign up at https://pages.cloudflare.com
2. Connect GitHub
3. Select repository: `maxgershfield/timo-rides-pp-lp`
4. Build settings:
   - Framework preset: Astro
   - Build command: `npm run build`
   - Build output directory: `dist`
5. Add custom domain: `timorides.com`
6. Update DNS in Hostinger to Cloudflare nameservers

## Option 3: GitHub Pages (Free but Limited)

**Pros:**
- Free
- Simple
- Already have GitHub repo

**Cons:**
- Limited customization
- No server-side features
- Requires GitHub Actions for Astro

**Setup:**
1. Create `.github/workflows/deploy.yml` (I can create this)
2. Enable GitHub Pages in repo settings
3. Update DNS to point to GitHub Pages

## Option 4: Hostinger Hosting (Since Domain is There)

**Pros:**
- Domain already there
- Might already have hosting

**Cons:**
- Need to upload files manually or via FTP
- Less automated

**Setup:**
1. Upload `dist/` folder contents to Hostinger
2. Point domain to hosting directory

## Option 5: Render (Simple Alternative)

**Pros:**
- Free tier
- Easy setup
- GitHub integration

**Setup:**
1. Sign up at https://render.com
2. New Static Site
3. Connect GitHub repo
4. Build: `npm run build`
5. Publish: `dist`
6. Add custom domain

## Recommended: Netlify

I recommend **Netlify** because:
- Easiest setup
- Best developer experience
- Free tier is generous
- Great documentation
- Similar to Vercel (easy transition)

## DNS Changes Needed

For any hosting provider, you'll need to update DNS in Hostinger:

1. **If using nameservers:**
   - Replace current nameservers with hosting provider's nameservers

2. **If using CNAME/A records:**
   - Keep current nameservers
   - Add CNAME or A record pointing to hosting provider

The hosting provider will give you exact DNS settings when you add the domain.




