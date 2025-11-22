# Manual Heroku Deployment Steps

There's an issue with the API key authentication. Here's how to deploy manually:

## Step 1: Login to Heroku

Open a new terminal and run:
```bash
cd "/Volumes/Storage 1/OASIS_CLEAN/BillionsHealed/backend"
heroku login
```

- It will open a browser
- Login with: **oasisweb4@gmail.com**
- Enter your password

## Step 2: Verify Login
```bash
heroku auth:whoami
```
Should show: `oasisweb4@gmail.com`

## Step 3: Set Git Remote (if needed)
```bash
heroku git:remote -a billionshealed-api
```

## Step 4: Deploy
```bash
git push heroku main
```

## Step 5: Set Environment Variables
```bash
heroku config:set NODE_ENV=production
heroku config:set CORS_ORIGIN=https://billionshealed.com
```

## Step 6: Check Deployment
```bash
heroku open
heroku logs --tail
```

## Step 7: Get Your Backend URL
Your backend will be at:
```
https://billionshealed-api-aa03f99f1c28.herokuapp.com
```

## Step 8: Update Frontend

Edit `/BillionsHealed/frontend/app.js` line 2:
```javascript
const API_BASE_URL = 'https://billionshealed-api-aa03f99f1c28.herokuapp.com/api';
```

## Step 9: Redeploy Frontend
```bash
cd "/Volumes/Storage 1/OASIS_CLEAN/BillionsHealed/frontend"
surge . billionshealed.com
```

---

**Note:** The Heroku app is already created at `billionshealed-api`. You just need to login and push!

