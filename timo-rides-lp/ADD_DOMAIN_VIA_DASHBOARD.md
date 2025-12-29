# Add timorides.com Domain via Vercel Dashboard

## Steps

1. **Go to Vercel Dashboard**
   - Visit: https://vercel.com/dashboard
   - Make sure you're logged into your personal account (not timo-org team)

2. **Open Your Project**
   - Find project: `timo-rides-lp`
   - Click on it to open

3. **Go to Settings → Domains**
   - Click "Settings" tab
   - Click "Domains" in the sidebar
   - Click "Add Domain" button

4. **Add Domain**
   - Enter: `timorides.com`
   - Click "Add"
   - Vercel will check if domain is available

5. **If Domain is Already Claimed**
   - You'll see an error that domain is already in use
   - You'll need to:
     a. Remove it from timo-org account first, OR
     b. Transfer your project to timo-org team

## Alternative: Use CNAME Instead

If you can't claim the root domain, you could:

1. **Add as CNAME in Hostinger**
   - Instead of using nameservers, add a CNAME record:
   - Name: `@` (or blank for root)
   - Value: `cname.vercel-dns.com` (Vercel will provide exact value)

2. **Get CNAME from Vercel**
   - When you try to add domain, Vercel will show you the exact DNS records needed
   - Use those instead of nameservers

## Recommended Solution

Since nameservers are already pointing to Vercel, the easiest solution is:

1. **Get access to timo-org Vercel team** (or have admin do it)
2. **Transfer/redeploy your project to timo-org team**
3. **Add domain in timo-org account** (should work since DNS is already correct)

This avoids any DNS changes and uses the existing setup.




