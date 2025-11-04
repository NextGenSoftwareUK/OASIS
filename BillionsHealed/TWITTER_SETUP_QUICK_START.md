# 🚀 Twitter Integration - Quick Start

Get real #billionshealed tweets showing in **5 minutes**!

## Prerequisites

✅ Twitter Developer Account (free at [developer.twitter.com](https://developer.twitter.com))  
✅ Bearer Token from Twitter API v2

---

## 3-Step Setup

### Step 1: Get Your Twitter Bearer Token (5 min)

1. Go to [Twitter Developer Portal](https://developer.twitter.com/en/portal/dashboard)
2. Create an app: "BillionsHealed Platform"
3. Go to "Keys and tokens" → Generate "Bearer Token"
4. **Copy the token** (you won't see it again!)

### Step 2: Run Setup Script (1 min)

```bash
cd /Volumes/Storage/OASIS_CLEAN/BillionsHealed
./initialize-twitter.sh
```

Paste your Bearer Token when prompted.

### Step 3: Start & Test (1 min)

**Terminal 1 - Start Backend:**
```bash
cd backend
npm install
npm start
```

**Terminal 2 - Test Integration:**
```bash
cd /Volumes/Storage/OASIS_CLEAN/BillionsHealed
./test-twitter.sh
```

**Open Frontend:**
- Open `frontend/index.html` in your browser
- You should see real tweets with #billionshealed!

---

## Manual Setup (Alternative)

If you prefer manual setup:

### 1. Create `.env` file

```bash
cd backend
cat > .env << 'EOL'
TWITTER_BEARER_TOKEN=YOUR_TOKEN_HERE
TWITTER_HASHTAG=#billionshealed
BACKEND_URL=http://localhost:3002
USE_REAL_TWITTER=true
PORT=3002
EOL
```

### 2. Install Dependencies

```bash
npm install dotenv
```

### 3. Start Server

```bash
npm start
```

### 4. Initialize Twitter Service

```bash
curl -X POST http://localhost:3002/api/twitter/initialize \
  -H "Content-Type: application/json" \
  -d '{
    "bearerToken": "YOUR_TOKEN_HERE",
    "hashtag": "#billionshealed"
  }'
```

---

## Verify It's Working

### Check Service Status
```bash
curl http://localhost:3002/api/twitter/status
```

Should show: `"isInitialized": true`

### Get Recent Tweets
```bash
curl "http://localhost:3002/api/twitter/recent-tweets?limit=5"
```

Should show real tweets (or message if no tweets exist yet)

### View in Browser

Open `frontend/index.html` → Check the Twitter feed on the right side

---

## Twitter API Tiers

### Free (Basic Access)
- ✅ Good for: Testing
- ❌ Limit: 1 search per 24 hours
- 💰 Cost: **FREE**

### Elevated Access (Recommended)
- ✅ Good for: Production
- ✅ Limit: 450 searches per 15 minutes  
- 💰 Cost: **FREE** (just requires approval)
- ⏱️ Approval: 1-2 days

**Apply for Elevated Access:**
1. Go to [Developer Portal](https://developer.twitter.com/en/portal/dashboard)
2. Click "Products" → "Twitter API v2"
3. Click "Apply for Elevated"
4. Fill out the form (takes 5 minutes)

---

## Troubleshooting

### ❌ "Twitter Bearer Token not configured"
- Check `.env` file exists in `backend/`
- Verify `TWITTER_BEARER_TOKEN=` is set

### ❌ "403 Forbidden"
- Bearer Token is invalid
- Generate a new one in Developer Portal

### ❌ "429 Too Many Requests"
- You've hit the rate limit (Free tier: 1 per 24 hours)
- Apply for Elevated access
- Or wait 24 hours

### ℹ️ "No tweets found"
- This is normal if hashtag is new!
- Tweet with #billionshealed and wait 2 minutes
- Check that you're using the exact hashtag (case-insensitive)

### 🐛 Backend not starting
```bash
# Kill existing process
lsof -ti:3002 | xargs kill -9

# Restart
npm start
```

---

## Production Deployment

### Update Frontend API URL

Edit `frontend/app.js` line 2:

```javascript
const API_BASE_URL = 'https://your-backend.herokuapp.com/api';
```

### Deploy Backend with Environment Variables

**Railway:**
```bash
railway up
railway variables set TWITTER_BEARER_TOKEN=your_token
```

**Heroku:**
```bash
heroku config:set TWITTER_BEARER_TOKEN=your_token
git push heroku main
```

**Render/DigitalOcean:**
- Add environment variables in dashboard
- Redeploy

---

## Testing with @BillionsHealed Account

1. Tweet from [@BillionsHealed](https://x.com/BillionsHealed): 
   ```
   Today I'm grateful for the healing journey. 
   Every step forward matters. 🌟 #billionshealed
   ```

2. Wait 2 minutes for backend to detect it

3. Refresh frontend → See your tweet!

4. Watch thermometer rise with engagement

---

## What Happens Next?

Once integrated:

✅ **Auto-Monitoring**: Backend checks for #billionshealed every 2 minutes  
✅ **Real-Time Updates**: Frontend refreshes tweets every 60 seconds  
✅ **Engagement Tracking**: Likes, retweets, replies all tracked  
✅ **Thermometer Updates**: High-engagement tweets contribute more  
✅ **Auto-Processing**: No manual intervention needed  

---

## Support Resources

📖 **Full Guide**: `TWITTER_INTEGRATION_GUIDE.md`  
🐦 **Twitter API Docs**: [developer.twitter.com/docs](https://developer.twitter.com/en/docs/twitter-api)  
🔧 **OASIS Support**: Contact OASIS development team  

---

## Security Reminders

⚠️ **NEVER commit `.env` file to Git**  
⚠️ **NEVER share your Bearer Token publicly**  
⚠️ **Rotate tokens every few months**  

---

**Ready to launch? Let's heal together!** 🌡️💙

#billionshealed

