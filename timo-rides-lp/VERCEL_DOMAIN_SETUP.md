# Vercel Domain Setup for timorides.com

## Current Situation
- ✅ Site deployed to Vercel successfully
- ❌ Domain `timorides.com` is associated with a different Vercel account (timo-org team)
- Your deployment is on your personal Vercel account

## Solutions

### Option 1: Transfer Project to timo-org Team (Recommended)

1. **Get access to timo-org Vercel team**
   - Ask team admin to add you to the `timo-org` Vercel team
   - Or have them deploy the project from the team account

2. **Transfer the project**
   - In Vercel dashboard, go to Project Settings → General
   - Transfer project to `timo-org` team
   - Or redeploy from the team account

3. **Add domain in team account**
   - Once in timo-org team, go to Project Settings → Domains
   - Add `timorides.com`
   - Since DNS already points to Vercel, it should work automatically

### Option 2: Configure Nameservers Manually

If you need to point the domain to your personal Vercel account:

1. **Get Vercel nameservers from your account**
   - Go to Vercel Dashboard → Domains
   - Or check your project's domain settings
   - Vercel will show you the nameservers (usually something like):
     - `ns1.vercel-dns.com`
     - `ns2.vercel-dns.com`

2. **Update nameservers in Hostinger**
   - Log into Hostinger
   - Go to DNS settings for timorides.com
   - Update nameservers to match your Vercel account's nameservers
   - Wait for DNS propagation (can take up to 48 hours, usually much faster)

3. **Add domain in your Vercel project**
   - Go to Project Settings → Domains
   - Add `timorides.com`
   - Vercel will verify domain ownership

### Option 3: Use CNAME Record (Alternative)

Instead of changing nameservers, you can use a CNAME record:

1. **Get your Vercel deployment URL**
   - Your deployment should have a URL like: `your-project.vercel.app`
   - Or a custom domain if already set

2. **Add CNAME in Hostinger**
   - Go to DNS settings
   - Add CNAME record:
     - Name: `@` (or leave blank for root domain)
     - Value: `cname.vercel-dns.com` (or your specific VNAME from Vercel)
   - Vercel will provide the exact CNAME value when you add the domain

3. **Add domain in Vercel**
   - Project Settings → Domains → Add `timorides.com`
   - Vercel will show you the exact DNS records needed

## Recommended Approach

**Best option**: Transfer the project to the `timo-org` Vercel team account where the domain is already configured. This is the cleanest solution.

**Alternative**: If you need to use your personal account, you'll need to:
1. Get the nameservers from your Vercel account
2. Update them in Hostinger
3. Add the domain in your Vercel project

## Checking Current Deployment

Your site is currently deployed at a URL like:
- `https://your-project-name.vercel.app`
- Or check with: `vercel ls` or in Vercel dashboard

## Next Steps

1. Decide which Vercel account to use (timo-org team or personal)
2. If using timo-org team: Get added to team and transfer/redeploy
3. If using personal: Update nameservers in Hostinger
4. Add domain in the correct Vercel account
5. Wait for DNS propagation
6. Site will be live at timorides.com!




