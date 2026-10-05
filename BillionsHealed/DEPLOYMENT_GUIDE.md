# BillionsHealed Deployment Guide

This guide will help you deploy BillionsHealed to billionshealed.com on GoDaddy hosting.

## Overview

BillionsHealed consists of two parts:
1. **Frontend** (Static HTML/CSS/JS) - Can be hosted on GoDaddy's standard web hosting
2. **Backend** (Node.js API) - Requires a separate hosting solution

## Option 1: GoDaddy + Heroku (Recommended)

### Frontend Deployment to GoDaddy

1. **Access your GoDaddy hosting account**
   - Log into your GoDaddy account
   - Go to "My Products" → "Web Hosting"
   - Click "Manage" on your hosting plan

2. **Upload frontend files**
   - Use File Manager or FTP to upload files from `/deploy/frontend/` to your domain's `public_html` folder
   - Files to upload:
     - `index.html`
     - `styles.css`
     - `app.js`

3. **Configure the domain**
   - Ensure `billionshealed.com` points to your hosting account
   - Update DNS if necessary

### Backend Deployment to Heroku

1. **Create a Heroku account** (free tier available)
   - Sign up at https://heroku.com

2. **Install Heroku CLI**
   ```bash
   # macOS
   brew install heroku/brew/heroku
   
   # Or download from https://devcenter.heroku.com/articles/heroku-cli
   ```

3. **Deploy the backend**
   ```bash
   cd "/Volumes/Storage 1/OASIS_CLEAN/BillionsHealed/deploy/backend"
   heroku login
   heroku create billionshealed-api
   git init
   git add .
   git commit -m "Initial deployment"
   git push heroku main
   ```

4. **Configure environment variables**
   ```bash
   heroku config:set NODE_ENV=production
   heroku config:set CORS_ORIGIN=https://billionshealed.com
   # Add Twitter API keys if you have them:
   # heroku config:set TWITTER_BEARER_TOKEN=your_token_here
   ```

5. **Update frontend API URL**
   - Edit `app.js` in your GoDaddy hosting
   - Change `API_BASE_URL` to your Heroku URL:
   ```javascript
   const API_BASE_URL = 'https://billionshealed-api.herokuapp.com/api';
   ```

## Option 2: GoDaddy + Railway (Alternative)

### Backend Deployment to Railway

1. **Create a Railway account**
   - Sign up at https://railway.app

2. **Connect GitHub**
   - Create a GitHub repository with your backend code
   - Connect Railway to your GitHub account

3. **Deploy from GitHub**
   - Select your repository
   - Railway will automatically detect it's a Node.js app
   - Set environment variables in Railway dashboard

4. **Update frontend**
   - Use Railway's provided URL as your `API_BASE_URL`

## Option 3: GoDaddy + Vercel (Alternative)

### Backend Deployment to Vercel

1. **Create a Vercel account**
   - Sign up at https://vercel.com

2. **Deploy via CLI**
   ```bash
   npm i -g vercel
   cd "/Volumes/Storage 1/OASIS_CLEAN/BillionsHealed/deploy/backend"
   vercel
   ```

3. **Configure environment variables**
   - Set variables in Vercel dashboard

## Testing Your Deployment

1. **Test the frontend**
   - Visit `https://billionshealed.com`
   - Verify the thermometer loads
   - Check that the Twitter feed shows (demo mode if no API keys)

2. **Test the backend**
   - Visit `https://your-backend-url.herokuapp.com/health`
   - Should return `{"status": "healthy"}`

3. **Test the connection**
   - Try minting a thermometer on the frontend
   - Check browser console for any API errors

## Troubleshooting

### Common Issues

1. **CORS Errors**
   - Ensure `CORS_ORIGIN` is set correctly in your backend
   - Check that your domain matches exactly

2. **API Not Responding**
   - Check Heroku logs: `heroku logs --tail`
   - Verify environment variables are set
   - Ensure the backend is running

3. **Frontend Not Loading**
   - Check file permissions on GoDaddy
   - Verify all files are uploaded correctly
   - Check browser console for errors

### GoDaddy-Specific Issues

1. **File Upload Issues**
   - Use File Manager instead of FTP if having issues
   - Ensure files are in the `public_html` folder

2. **Domain Configuration**
   - Check DNS settings in GoDaddy
   - Ensure domain is pointing to the correct hosting account

## Cost Breakdown

### Option 1: GoDaddy + Heroku
- **GoDaddy Hosting**: ~$5-10/month (basic plan)
- **Heroku**: Free tier (with limitations) or $7/month (hobby tier)
- **Total**: ~$5-17/month

### Option 2: GoDaddy + Railway
- **GoDaddy Hosting**: ~$5-10/month
- **Railway**: $5/month (pro plan)
- **Total**: ~$10-15/month

### Option 3: GoDaddy + Vercel
- **GoDaddy Hosting**: ~$5-10/month
- **Vercel**: Free tier (with limitations) or $20/month (pro)
- **Total**: ~$5-30/month

## Security Considerations

1. **Environment Variables**
   - Never commit API keys to version control
   - Use environment variables for all sensitive data

2. **CORS Configuration**
   - Only allow your domain in production
   - Use HTTPS in production

3. **Rate Limiting**
   - Consider adding rate limiting for API endpoints
   - Monitor usage to prevent abuse

## Next Steps After Deployment

1. **Monitor Performance**
   - Set up monitoring for your backend
   - Monitor error rates and response times

2. **Add Twitter API Integration**
   - Get Twitter API keys if you want live data
   - Update environment variables

3. **SSL Certificate**
   - Ensure HTTPS is enabled on both frontend and backend
   - GoDaddy usually provides SSL certificates

4. **Backup Strategy**
   - Regular backups of your code
   - Database backups if you add one later

## Support

If you encounter issues:
1. Check the browser console for errors
2. Check backend logs
3. Verify all environment variables are set
4. Test API endpoints directly

The application includes a demo mode that works without Twitter API keys, so you can deploy and test immediately.