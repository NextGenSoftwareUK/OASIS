# 🚂 Railway Deployment Guide for OASIS Ecosystem

## Overview
This guide will help you deploy your OASIS ecosystem to Railway, including:
- OASIS API Web API (.NET 8)
- OASIS OPORTAL (React app)
- STAR WEB UI (React app)

## Prerequisites
- GitHub account with your OASIS repository
- Railway account (free trial available)

## Step 1: Railway Setup

### 1.1 Create Railway Account
1. Go to [railway.app](https://railway.app)
2. Sign up with your GitHub account
3. Connect your OASIS repository

### 1.2 Create New Project
1. Click "New Project"
2. Select "Deploy from GitHub repo"
3. Choose your OASIS repository

## Step 2: Deploy OASIS API Web API

### 2.1 Add Web API Service
1. In your Railway project, click "New Service"
2. Select "GitHub Repo"
3. Choose your OASIS repository
4. Set the **Root Directory** to: `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI`

### 2.2 Configure Environment Variables
Add these environment variables in Railway dashboard:
```
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:$PORT
```

### 2.3 Deploy
Railway will automatically:
- Detect it's a .NET project
- Build using `dotnet build`
- Run using the Procfile command

## Step 3: Deploy STAR WEB UI (React App)

### 3.1 Add React Service
1. Click "New Service" again
2. Select "GitHub Repo"
3. Choose your OASIS repository
4. Set the **Root Directory** to: `oasisweb4.com`

### 3.2 Configure Build Settings
Railway will auto-detect React, but you can verify:
- **Build Command**: `npm run build`
- **Start Command**: `npm run preview`

### 3.3 Deploy
Railway will:
- Install dependencies with `npm install`
- Build the React app
- Serve it as a static site

## Step 4: Deploy OASIS OPORTAL (Optional)

### 4.1 Add OPORTAL Service
1. Click "New Service"
2. Select "GitHub Repo"
3. Choose your OASIS repository
4. Set the **Root Directory** to: `ONODE/NextGenSoftware.OASIS.API.ONODE.OPORTAL`

## Step 5: Configure Custom Domains (Optional)

### 5.1 Add Custom Domain
1. Go to each service settings
2. Click "Domains"
3. Add your custom domain
4. Update DNS records as instructed

## Step 6: Environment Configuration

### 6.1 API Configuration
Update your React apps to use the Railway API URLs:
```typescript
// In your React app
const API_BASE_URL = process.env.REACT_APP_API_URL || 'https://your-api.railway.app';
```

### 6.2 Database (If Needed)
Railway provides PostgreSQL and MySQL databases:
1. Add a database service
2. Update connection strings in your API

## Step 7: Monitoring & Logs

### 7.1 View Logs
- Go to each service
- Click "Logs" tab
- Monitor deployment and runtime logs

### 7.2 Health Checks
- API health endpoint: `https://your-api.railway.app/api/health`
- React app: `https://your-react.railway.app/`

## Cost Estimation

### Railway Pricing (Monthly)
- **Web API**: ~$5-10 (Basic plan)
- **React App**: ~$5 (Static hosting)
- **OPORTAL**: ~$5-10 (if deployed)
- **Database**: ~$5 (PostgreSQL)
- **Total**: ~$20-30/month

### Free Tier
- $5 credit monthly
- Perfect for testing and development

## Troubleshooting

### Common Issues
1. **Build Failures**: Check logs for missing dependencies
2. **Port Issues**: Ensure your app uses `$PORT` environment variable
3. **Database Connection**: Verify connection strings
4. **CORS Issues**: Update CORS settings for production URLs

### Useful Commands
```bash
# Check Railway CLI (if installed)
railway status

# View logs
railway logs

# Connect to database
railway connect
```

## Next Steps

1. **Set up CI/CD**: Railway auto-deploys on git push
2. **Add monitoring**: Consider adding application monitoring
3. **Scale up**: Upgrade plans as your app grows
4. **Add SSL**: Railway provides free SSL certificates

## Support
- Railway Documentation: [docs.railway.app](https://docs.railway.app)
- Railway Discord: [discord.gg/railway](https://discord.gg/railway)

---

**Happy Deploying! 🚀**
