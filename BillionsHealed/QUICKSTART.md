# 🚀 BillionsHealed - Quick Start Guide

Get BillionsHealed up and running in 5 minutes!

## ⚡ Super Quick Start

```bash
# 1. Navigate to BillionsHealed
cd BillionsHealed

# 2. Run the startup script
./start.sh
```

That's it! The backend will be running on `http://localhost:3002`

## 📱 Viewing the Frontend

### Option 1: Direct File (Simplest)

Just open `frontend/index.html` in your browser!

### Option 2: Local Server (Recommended)

```bash
cd frontend
python3 -m http.server 8000
```

Then visit `http://localhost:8000`

### Option 3: Live Server (VS Code)

1. Install "Live Server" extension in VS Code
2. Right-click `frontend/index.html`
3. Select "Open with Live Server"

## 🧪 Testing the API

```bash
# Health check
curl http://localhost:3002/health

# Get thermometer status
curl http://localhost:3002/api/thermometer/status

# Get tweets
curl "http://localhost:3002/api/twitter/recent-tweets?limit=5"

# Mint a thermometer
curl -X POST http://localhost:3002/api/thermometer/mint \
  -H "Content-Type: application/json" \
  -d '{"source": "manual", "metadata": {}}'
```

## 🎨 What You'll See

1. **Thermometer** (left side):
   - Blue thermometer bulb and tube
   - Temperature from 0-100°
   - Mint button to add thermometers
   - Statistics

2. **Twitter Feed** (right side):
   - Live #billionshealed tweets (mock data by default)
   - Engagement metrics (likes, retweets, replies)
   - Refresh button
   - Feed statistics

## 🐦 Connecting Real Twitter Data

By default, the app uses mock tweets. To connect real Twitter:

1. **Get Twitter Bearer Token**:
   - Go to [developer.twitter.com](https://developer.twitter.com)
   - Create an app
   - Get your Bearer Token

2. **Initialize Twitter Service**:
```bash
curl -X POST http://localhost:3002/api/twitter/initialize \
  -H "Content-Type: application/json" \
  -d '{
    "bearerToken": "YOUR_BEARER_TOKEN",
    "hashtag": "#billionshealed"
  }'
```

**Note**: Twitter Essential tier only allows 1 search request per 24 hours. The app will automatically fall back to mock data if rate limited.

## 🎮 Try These Features

### 1. Mint Thermometers

Click the "Mint Thermometer" button and watch:
- Temperature rise
- Color change
- Statistics update

### 2. Refresh Tweets

Click the 🔄 button to reload the Twitter feed

### 3. Watch Temperature Levels

Mint enough thermometers to see color changes:
- 0-25: ❄️ Cold (Blue)
- 26-50: 🌡️ Warm (Yellow)
- 51-75: 🔥 Hot (Orange)
- 76-100: 🌋 Boiling (Red)

## 🔄 Resetting Data

To reset the thermometer back to 0:

```bash
curl -X POST http://localhost:3002/api/thermometer/reset
```

Then refresh your browser to see the reset.

## 🛑 Stopping the Server

Press `Ctrl+C` in the terminal running the backend

Or:
```bash
pkill -f "node server.js"
```

## 📚 Next Steps

- Read the full [README.md](README.md) for detailed information
- Customize the colors and styling in `frontend/styles.css`
- Modify temperature levels in `backend/ThermometerService.js`
- Add your own features!

## ❓ Troubleshooting

### Port 3002 Already in Use

```bash
# Kill existing process
lsof -ti:3002 | xargs kill -9

# Or use a different port
PORT=3003 npm start
```

### Frontend Can't Connect to Backend

Make sure:
1. Backend is running (`curl http://localhost:3002/health`)
2. `API_BASE_URL` in `frontend/app.js` points to correct URL
3. CORS is enabled (it is by default)

### No Tweets Showing

This is normal! The app uses mock data by default. Real Twitter integration requires a Bearer Token and Elevated access for frequent updates.

## 🎉 You're Ready!

Start minting thermometers and sharing healing tweets with #billionshealed! 

**Together, we rise. Together, we heal.** 🌡️💙

