# Manual Railway Deployment

If Railway isn't auto-detecting changes, you can manually trigger a deployment:

## Option 1: Railway Dashboard (Recommended)

1. Go to Railway dashboard: https://railway.app/dashboard
2. Click on your backend service
3. Go to "Deployments" tab
4. Click "Redeploy" button (or three dots → Redeploy)

## Option 2: Railway CLI

```bash
# Make sure you're in the backend directory
cd /Volumes/Storage/OASIS_CLEAN/pangea/backend

# Login to Railway (if not already)
railway login

# Link to your project (if not already linked)
railway link

# Trigger a deployment
railway up
```

## Option 3: Force Push (if needed)

Sometimes Railway needs a new commit to trigger:

```bash
cd /Volumes/Storage/OASIS_CLEAN/pangea
git commit --allow-empty -m "Trigger Railway deployment"
git push origin main
```

## Verify Deployment Settings

In Railway dashboard → Service Settings → Source:

- ✅ Repository: `maxgershfield/pangea`
- ✅ Branch: `main`
- ✅ Root Directory: `backend`
- ✅ Auto Deploy: Enabled

If Auto Deploy is disabled, enable it in Settings → Source.








