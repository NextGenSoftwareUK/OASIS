# Deploy BillionsHealed to Surge

## Quick Deploy to billionshealed.com

### Prerequisites
```bash
# Install surge if you haven't already
npm install -g surge
```

### Option 1: Deploy from frontend folder
```bash
cd "/Volumes/Storage 1/OASIS_CLEAN/BillionsHealed/frontend"
surge . billionshealed.com
```

### Option 2: Deploy from deploy folder
```bash
cd "/Volumes/Storage 1/OASIS_CLEAN/BillionsHealed/deploy/frontend"
surge . billionshealed.com
```

### First Time Setup
1. If this is your first time using Surge, it will ask for:
   - Email address
   - Password
2. Confirm the domain: `billionshealed.com`
3. Press Enter to deploy

### Update/Redeploy
Simply run the same command again:
```bash
surge . billionshealed.com
```

### Configure Custom Domain (GoDaddy DNS)
1. Log into your GoDaddy account
2. Go to DNS Management for billionshealed.com
3. Add these DNS records:

```
Type    Name    Value                   TTL
CNAME   www     na-west1.surge.sh       1 Hour
A       @       138.197.235.123         1 Hour
```

### Verify Deployment
- Visit: https://billionshealed.com
- Test thermometer functionality
- Check Twitter feed popup
- Test mint button

### Surge Commands

**Deploy:**
```bash
surge . billionshealed.com
```

**List your sites:**
```bash
surge list
```

**Delete a deployment:**
```bash
surge teardown billionshealed.com
```

**Check domain:**
```bash
surge whoami
```

## Features After Surge Deployment

✅ **HTTPS enabled** - Automatic SSL certificate  
✅ **CDN delivery** - Fast global loading  
✅ **Custom domain** - billionshealed.com  
✅ **Instant updates** - Just run `surge` again  
✅ **Free hosting** - No cost for basic usage  

## Demo Mode Features

Since this is static hosting, the app includes:
- Mock thermometer data
- Sample #billionshealed tweets
- Simulated NFT minting
- Full UI/UX functionality

## Adding Backend Later

When you're ready to add real backend functionality:
1. Deploy backend to Heroku/Railway
2. Update `app.js` with backend URL
3. Redeploy to Surge

## Troubleshooting

**Domain not working?**
- Wait 24-48 hours for DNS propagation
- Verify DNS records in GoDaddy
- Check surge status: `surge list`

**Need to update?**
- Just run `surge . billionshealed.com` again
- No need to teardown first

**CNAME file issues?**
- Ensure CNAME file contains only: `billionshealed.com`
- No http://, no trailing slash

## Cost

**Surge Free Plan:**
- Custom domain support
- HTTPS/SSL included
- Unlimited bandwidth
- Perfect for static sites

**Upgrade ($30/month) includes:**
- Multiple domains
- Password protection
- Custom redirects
- Priority support

---

**Ready?** Just run:
```bash
cd "/Volumes/Storage 1/OASIS_CLEAN/BillionsHealed/frontend"
surge . billionshealed.com
```

Your BillionsHealed thermometer will be live in seconds! 🌡️✨

