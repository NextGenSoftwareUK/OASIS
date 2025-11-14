# 🚀 Deploy BillionsHealed Backend to Heroku (GitHub Method)

Your Heroku app is already set up: **billionshealed-api**
URL: https://billionshealed-api-aa03f99f1c28.herokuapp.com

Since git push is having issues, use GitHub deployment instead:

---

## Step 1: Deploy via Heroku Dashboard

1. Go to https://dashboard.heroku.com/apps/billionshealed-api

2. Click **"Deploy"** tab

3. Under **"Deployment method"**, select **"GitHub"**

4. Click **"Connect to GitHub"**

5. Search for repository: **"OASIS"** or **"NextGenSoftwareUK/OASIS"**

6. Click **"Connect"**

7. Under **"Manual deploy"**:
   - Branch: `max-build2`
   - Click **"Deploy Branch"**

8. Wait for deployment to complete (you'll see build logs)

---

## Step 2: Configure Environment Variables

In the Heroku dashboard:

1. Go to **"Settings"** tab

2. Click **"Reveal Config Vars"**

3. Add these variables:

```
TWITTER_BEARER_TOKEN = AAAAAAAAAAAAAAAAAAAAALpN4wEAAAAAkpF8OpifhMmGmkFyip4whzmM5Qk%3DEDPTLhrrIEwfDVnCRv4UPOsULGJOWC8NBrvxWMulDXPvKEzvFc
NODE_ENV = production
PORT = 3002
TWITTER_HASHTAG = #billionshealed
```

4. Click **"Add"** for each

---

## Step 3: Verify Backend is Running

```bash
curl https://billionshealed-api-aa03f99f1c28.herokuapp.com/health
```

Should return: `{"status":"healthy"}`

---

## Step 4: Update Frontend API URL

Edit: `/Volumes/Storage/OASIS_CLEAN/BillionsHealed/frontend/app.js`

Change line 2:
```javascript
const API_BASE_URL = 'https://billionshealed-api-aa03f99f1c28.herokuapp.com/api';
```

---

## Step 5: Deploy Frontend to Surge

```bash
cd /Volumes/Storage/OASIS_CLEAN/BillionsHealed/frontend
surge . billionshealed.com
```

Done! Your site will be live at https://billionshealed.com

---

## Quick Verification

1. Backend health: https://billionshealed-api-aa03f99f1c28.herokuapp.com/health
2. Get tweets: https://billionshealed-api-aa03f99f1c28.herokuapp.com/api/twitter/recent-tweets
3. Frontend: https://billionshealed.com

---

**Much simpler than Railway! Just use the Heroku dashboard.** 🎯




