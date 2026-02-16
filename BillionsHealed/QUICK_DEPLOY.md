# Quick Deploy to billionshealed.com

## 🚀 Fastest Path to Deployment

### Step 1: Prepare Files
```bash
cd "/Volumes/Storage 1/OASIS_CLEAN/BillionsHealed"
./deploy.sh
```

### Step 2: Deploy Frontend to GoDaddy
1. Log into GoDaddy → Web Hosting → File Manager
2. Navigate to `public_html` folder
3. Upload these files from `deploy/frontend/`:
   - `index.html`
   - `styles.css` 
   - `app.js`

### Step 3: Deploy Backend to Heroku
```bash
cd deploy/backend
heroku login
heroku create billionshealed-api
git init
git add .
git commit -m "Deploy"
git push heroku main
heroku config:set CORS_ORIGIN=https://billionshealed.com
```

### Step 4: Update Frontend API URL
Edit `app.js` in your GoDaddy File Manager:
```javascript
// Change this line:
const API_BASE_URL = 'https://your-backend-url.herokuapp.com/api';
// To this:
const API_BASE_URL = 'https://billionshealed-api.herokuapp.com/api';
```

### Step 5: Test
- Visit `https://billionshealed.com`
- Try minting a thermometer
- Check browser console for errors

## 🎯 That's It!

Your BillionsHealed site will be live at billionshealed.com with:
- ✅ Interactive thermometer
- ✅ Twitter feed (demo mode)
- ✅ Progressive NFT pricing
- ✅ Mobile responsive design

## 📞 Need Help?

- Check `DEPLOYMENT_GUIDE.md` for detailed instructions
- The app works in demo mode without Twitter API keys
- Backend logs: `heroku logs --tail`

