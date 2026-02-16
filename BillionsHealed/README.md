# 🌡️ BillionsHealed

**A community-driven thermometer that rises with every #billionshealed tweet**

## Overview

BillionsHealed is a standalone web application that visualizes global healing through social media engagement. As people tweet with the hashtag #billionshealed, a virtual thermometer fills up, creating a powerful visual representation of collective healing.

### Key Features

- 🌡️ **Interactive Thermometer** - Visual thermometer that fills from 0-100°
- 🐦 **Twitter Integration** - Real-time monitoring of #billionshealed tweets
- 🎨 **Beautiful UI** - Modern, responsive design with animations
- 📊 **Live Statistics** - Track tweets, engagement, and thermometer progress
- 🔥 **Dynamic Levels** - Temperature changes from Cold → Warm → Hot → Boiling
- 💎 **NFT Ready** - Optional integration with OASIS for minting thermometer NFTs

## Project Structure

```
BillionsHealed/
├── frontend/              # Static HTML/CSS/JS frontend
│   ├── index.html        # Main page
│   ├── styles.css        # Styling
│   └── app.js            # Frontend logic
├── backend/              # Node.js/Express API
│   ├── server.js         # Main server
│   ├── TwitterService.js # Twitter API integration
│   ├── ThermometerService.js # Thermometer logic
│   ├── providers/        # Twitter provider
│   └── package.json      # Dependencies
└── README.md            # This file
```

## Quick Start

### Prerequisites

- Node.js 18+ and npm
- (Optional) Twitter Developer Account with Bearer Token

### 1. Install Backend Dependencies

```bash
cd backend
npm install
```

### 2. Start the Backend Server

```bash
npm start
```

The API will be available at `http://localhost:3002`

### 3. Open the Frontend

Simply open `frontend/index.html` in your browser, or serve it with:

```bash
cd frontend
python3 -m http.server 8000
```

Then visit `http://localhost:8000`

## Configuration

### Twitter API (Optional)

To connect real Twitter data instead of mock tweets:

1. Get a Twitter Developer Account at [developer.twitter.com](https://developer.twitter.com)
2. Create an app and get your Bearer Token
3. Initialize the service via API:

```bash
curl -X POST http://localhost:3002/api/twitter/initialize \
  -H "Content-Type: application/json" \
  -d '{
    "bearerToken": "YOUR_TWITTER_BEARER_TOKEN",
    "hashtag": "#billionshealed"
  }'
```

**Note**: Twitter Essential tier only allows 1 search request per 24 hours. For production use, apply for Elevated access.

## API Endpoints

### Twitter Endpoints

- `GET /api/twitter/status` - Get Twitter service status
- `GET /api/twitter/recent-tweets?limit=10` - Get recent tweets
- `POST /api/twitter/initialize` - Initialize Twitter service

### Thermometer Endpoints

- `GET /api/thermometer/status` - Get thermometer status
- `GET /api/thermometer/statistics` - Get detailed statistics
- `GET /api/thermometer/data` - Get all thermometer data
- `POST /api/thermometer/mint` - Mint a new thermometer
- `POST /api/thermometer/reset` - Reset thermometer data

### Health Check

- `GET /health` - Server health check

## Temperature Levels

The thermometer progresses through 4 temperature levels:

| Level | Range | Color | NFT Price |
|-------|-------|-------|-----------|
| ❄️ Cold | 0-25 | Blue (#4FC3F7) | 0.01 SOL |
| 🌡️ Warm | 26-50 | Yellow (#FFEB3B) | 0.05 SOL |
| 🔥 Hot | 51-75 | Orange (#FF9800) | 0.10 SOL |
| 🌋 Boiling | 76-100 | Red (#F44336) | 0.25 SOL |

## How It Works

### Thermometer Logic

1. **100 Total Thermometers**: The system tracks up to 100 thermometer NFTs
2. **Progressive Pricing**: Price increases as temperature rises
3. **Visual Feedback**: Liquid color changes with each level
4. **Community Driven**: Each tweet contributes to the temperature

### Twitter Integration

1. **Hashtag Monitoring**: Service checks for #billionshealed tweets
2. **Engagement Scoring**: High-engagement tweets contribute more
3. **Auto-Minting**: (When enabled) Automatically mints thermometers based on tweet impact
4. **Real-Time Updates**: Frontend refreshes every minute

## OASIS Integration (Optional)

BillionsHealed can integrate with the OASIS ecosystem to:

- **Mint NFTs**: Create thermometer NFTs on Solana blockchain
- **Track Achievements**: Award users for social impact
- **Link Accounts**: Connect Twitter accounts to OASIS Avatars
- **Store Data**: Persist thermometer data in OASIS storage
- **Cross-Platform**: Integrate with TelegramOASIS and other providers

To enable OASIS features, you'll need:
- Running OASIS API instance
- OASIS Avatar credentials
- Solana wallet integration

## Mock Data

By default, the system uses mock tweets for demo purposes. This allows you to test the application without hitting Twitter API rate limits.

Mock tweets include:
- Realistic healing journey messages
- Varied engagement levels
- Different time stamps
- Author information

## Deployment

### Backend (Node.js)

Deploy to Heroku, Railway, or any Node.js hosting:

```bash
# Heroku example
heroku create billionshealed-api
git push heroku main
```

### Frontend (Static)

Deploy to Netlify, Vercel, or any static hosting:

```bash
# Netlify example
cd frontend
netlify deploy --prod
```

Update `API_BASE_URL` in `app.js` to point to your deployed backend.

## Development

### Running in Development Mode

```bash
# Backend with auto-reload
cd backend
npm run dev

# Frontend
cd frontend
python3 -m http.server 8000
```

### Adding New Features

1. **Backend**: Add endpoints in `server.js`
2. **Services**: Modify `ThermometerService.js` or `TwitterService.js`
3. **Frontend**: Update `app.js` and `styles.css`

## Browser Compatibility

- Chrome 90+
- Firefox 88+
- Safari 14+
- Edge 90+

## Performance

- Lightweight: < 100KB total assets
- Fast: < 1s initial load
- Responsive: Works on mobile and desktop
- Efficient: Minimal API calls with caching

## Security

- CORS enabled for frontend access
- No sensitive data in frontend
- Twitter Bearer Token stored server-side only
- Rate limiting on API endpoints (recommended for production)

## Roadmap

- [ ] Real-time WebSocket updates
- [ ] User authentication and profiles
- [ ] Social sharing features
- [ ] Multi-language support
- [ ] Mobile app (React Native)
- [ ] Analytics dashboard
- [ ] Leaderboards
- [ ] OASIS NFT integration
- [ ] Multi-platform support (Instagram, TikTok)

## Contributing

Contributions are welcome! Please:

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

## License

MIT License - See LICENSE file for details

## Support

For questions or issues:
- Open an issue on GitHub
- Contact the OASIS team
- Join our community Discord

## Acknowledgments

- Built with ❤️ by the OASIS community
- Inspired by the global healing movement
- Powered by Twitter API v2
- Part of the #billionshealed initiative

---

**Together, we rise. Together, we heal.** 🌡️💙

#billionshealed

