# Deploy BillionsHealed Backend to Railway

Railway is much simpler than Heroku - no CLI needed!

## 🚂 Step-by-Step Railway Deployment

### Step 1: Create Railway Account
1. Go to https://railway.app
2. Click "Login" → "Login with GitHub" (or email)
3. Create account if you don't have one

### Step 2: Create New Project
1. Click "New Project"
2. Select "Deploy from GitHub repo" OR "Empty Project"

### Option A: Deploy from GitHub (Recommended)
1. Connect your GitHub account
2. Select your BillionsHealed repository
3. Railway will auto-detect it's a Node.js app
4. Click "Deploy"

### Option B: Deploy from Local Files
1. Click "Empty Project"
2. Click "Deploy from GitHub repo"
3. Or use Railway CLI (see below)

### Step 3: Configure Environment Variables
1. Once deployed, click on your service
2. Go to "Variables" tab
3. Add these variables:
   ```
   NODE_ENV=production
   CORS_ORIGIN=https://billionshealed.com
   PORT=3002
   ```

### Step 4: Get Your Backend URL
1. Go to "Settings" tab
2. Click "Generate Domain"
3. Railway will give you a URL like:
   ```
   https://billionshealed-api.up.railway.app
   ```

### Step 5: Update Frontend
Edit `/BillionsHealed/frontend/app.js`:
```javascript
const API_BASE_URL = 'https://YOUR-RAILWAY-URL.up.railway.app/api';
```

### Step 6: Redeploy Frontend
```bash
cd "/Volumes/Storage 1/OASIS_CLEAN/BillionsHealed/frontend"
surge . billionshealed.com
```

## 🎯 Railway CLI (Alternative)

If you want to use CLI:

```bash
# Install Railway CLI
npm i -g @railway/cli

# Login
railway login

# Initialize
cd "/Volumes/Storage 1/OASIS_CLEAN/BillionsHealed/backend"
railway init

# Deploy
railway up

# Add domain
railway domain
```

## ✅ Benefits of Railway

- ✨ **No git push issues** - Direct deployment
- 🚀 **Auto-deploys** from GitHub
- 💰 **Free tier** ($5 credit per month)
- 📊 **Built-in monitoring** and logs
- 🔧 **Easy environment variables**
- ⚡ **Faster deployments**

## 🔍 Check Deployment

Once deployed:
1. Visit: `https://your-app.up.railway.app/health`
2. Should return: `{"status": "healthy"}`
3. Test tweets: `https://your-app.up.railway.app/api/twitter/recent-tweets`

## 💰 Cost

Railway free tier includes:
- $5 in credits per month
- ~500 hours of runtime
- Perfect for BillionsHealed backend

After free tier: $5/month for hobby plan

---

**Recommendation:** Use Railway dashboard (Option A) - it's the easiest! Just connect GitHub and click deploy. 🚂✨

