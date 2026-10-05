# TimoRides Driver App - Web Mirror

Web version of the TimoRides driver app for rapid frontend development and testing.

## 🚀 Quick Start

1. **Install dependencies:**
```bash
npm install
```

2. **Start development server:**
```bash
npm run dev
```

3. **Open in browser:**
```
http://localhost:3001
```

## 📱 Features

- ✅ Authentication (Login/Register)
- ✅ Home screen with availability toggle
- ✅ Ride request management
- ✅ Earnings dashboard
- ✅ Profile management
- ✅ Real-time booking updates (polling)

## 🎨 Design

- Material-UI (MUI) components
- Timo brand colors and styling
- Futuristic glow effects on buttons
- Glassmorphism cards
- Responsive design

## 🔌 Backend Integration

Connects to TimoRides backend API:
- Default: `http://localhost:4205`
- Set via `VITE_API_URL` environment variable

## 📝 Notes

- This is a web mirror for development/testing
- Use "Skip Login (Testing)" button to bypass authentication
- Polls for bookings every 10 seconds when online
- Matches React Native app functionality

