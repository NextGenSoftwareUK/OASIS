# 🚀 Deploy BillionsHealed - Step by Step

All changes have been committed and pushed to GitHub. Here's how to deploy:

---

## Step 1: Deploy Backend to Railway

### 1a. Login to Railway
```bash
cd /Volumes/Storage/OASIS_CLEAN/BillionsHealed/backend
railway login
```
(This will open a browser - click "Authorize")

### 1b. Link or Create Project
```bash
# If you already have a Railway project:
railway link

# OR create a new one:
railway init
```

### 1c. Set Environment Variables
```bash
# Required
railway variables set TWITTER_BEARER_TOKEN=AAAAAAAAAAAAAAAAAAAAALpN4wEAAAAAkpF8OpifhMmGmkFyip4whzmM5Qk%3DEDPTLhrrIEwfDVnCRv4UPOsULGJOWC8NBrvxWMulDXPvKEzvFc
railway variables set NODE_ENV=production
railway variables set PORT=3002

# Optional - for CORS (update with your domain)
railway variables set BACKEND_URL=https://your-app.up.railway.app
```

### 1d. Deploy
```bash
railway up
```

### 1e. Get Your Backend URL
```bash
railway domain
```

Note the URL - you'll need it for the frontend!
Example: `https://billionshealed-api.up.railway.app`

---

## Step 2: Update Frontend API URL

Edit the file: `/Volumes/Storage/OASIS_CLEAN/BillionsHealed/frontend/app.js`

Change line 2 from:
```javascript
const API_BASE_URL = 'http://localhost:3002/api';
```

To:
```javascript
const API_BASE_URL = 'https://YOUR-RAILWAY-URL.up.railway.app/api';
```

(Replace `YOUR-RAILWAY-URL` with your actual Railway domain)

---

## Step 3: Deploy Frontend to Surge

```bash
cd /Volumes/Storage/OASIS_CLEAN/BillionsHealed/frontend
surge . billionshealed.com
```

**First time?**
- Enter your email
- Create a password
- Confirm domain: billionshealed.com
- Press Enter

**Already deployed before?**
- Just runs and updates instantly!

---

## Step 4: Verify Deployment

### Check Backend
```bash
curl https://YOUR-RAILWAY-URL.up.railway.app/health
```
Should return: `{"status":"healthy"}`

### Check Frontend
Visit: https://billionshealed.com

You should see:
- ✅ Thermometer loads
- ✅ Twitter feed visible (bottom left)
- ✅ Markers on thermometer
- ✅ "Refresh from Twitter" button works

---

## Step 5: Initialize Twitter on Production

Once deployed, initialize the Twitter service:

```bash
curl -X POST https://YOUR-RAILWAY-URL.up.railway.app/api/twitter/initialize \
  -H "Content-Type: application/json" \
  -d '{"bearerToken": "AAAAAAAAAAAAAAAAAAAAALpN4wEAAAAAkpF8OpifhMmGmkFyip4whzmM5Qk%3DEDPTLhrrIEwfDVnCRv4UPOsULGJOWC8NBrvxWMulDXPvKEzvFc", "hashtag": "#billionshealed"}'
```

---

## Quick Commands Summary

```bash
# 1. Backend to Railway
cd /Volumes/Storage/OASIS_CLEAN/BillionsHealed/backend
railway login
railway link  # or railway init
railway variables set TWITTER_BEARER_TOKEN=AAAAAAAAAAAAAAAAAAAAALpN4wEAAAAAkpF8OpifhMmGmkFyip4whzmM5Qk%3DEDPTLhrrIEwfDVnCRv4UPOsULGJOWC8NBrvxWMulDXPvKEzvFc
railway up
railway domain  # Note this URL!

# 2. Update frontend/app.js with Railway URL

# 3. Frontend to Surge
cd /Volumes/Storage/OASIS_CLEAN/BillionsHealed/frontend
surge . billionshealed.com
```

---

## What's Included in Deployment

✅ Twitter API integration with caching
✅ NitterService (backup option)
✅ Visual timeline with markers
✅ Expandable tweet cards on hover
✅ Share to Twitter functionality
✅ Auto-temperature calculation from tweets
✅ Responsive design
✅ No emojis (clean professional look)

---

## Cost

**Railway Backend:**
- Free tier: $5 credit/month (~500 hours)
- After free: $5/month

**Surge Frontend:**
- Free for billionshealed.com
- Includes SSL/HTTPS

**Total: FREE** (or ~$5/month if you exceed Railway free tier)

---

## Troubleshooting

### Railway login not working?
- Try: `railway logout` then `railway login` again

### Surge domain issues?
- Make sure CNAME file contains only: `billionshealed.com`
- Update GoDaddy DNS as shown in SURGE_DEPLOY.md

### Backend not responding?
- Check Railway logs: `railway logs`
- Verify environment variables: `railway variables`

---

**Ready to deploy! Start with Step 1 above.** 🚀




