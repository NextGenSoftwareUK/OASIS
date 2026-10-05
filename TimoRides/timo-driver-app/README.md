# TimoRides Driver App

React Native driver application for TimoRides ride-sharing platform.

## 🚀 Getting Started

### Prerequisites

- Node.js (v16 or higher)
- npm or yarn
- Expo CLI (`npm install -g expo-cli`)
- iOS Simulator (for iOS) or Android Emulator (for Android)
- Or Expo Go app on your physical device

### Installation

1. Install dependencies:
```bash
npm install
```

2. Start the development server:
```bash
npm start
```

3. Run on your platform:
- Press `i` for iOS simulator
- Press `a` for Android emulator
- Scan QR code with Expo Go app for physical device

## 📱 Features

- ✅ Authentication (Login/Register)
- ✅ Driver availability toggle (Online/Offline)
- ✅ Real-time location tracking
- ✅ Ride request notifications
- ✅ Accept/Decline rides
- ✅ Active ride management
- ✅ Earnings dashboard
- ✅ Profile management

## 🏗️ Project Structure

```
src/
├── screens/          # Screen components
│   ├── Auth/        # Authentication screens
│   ├── Home/        # Home screen
│   ├── Rides/       # Ride management screens
│   ├── Earnings/    # Earnings screens
│   └── Profile/      # Profile screens
├── components/      # Reusable components
├── services/        # API services
│   └── api/         # API client and endpoints
├── store/           # Redux store
│   └── slices/      # Redux slices
├── navigation/      # Navigation configuration
└── utils/           # Utilities (theme, constants)
```

## 🔌 Backend Integration

The app connects to the TimoRides backend API:
- **Development:** `http://localhost:4205`
- **Production:** Update in `src/utils/constants.js`

## 📦 Key Dependencies

- **React Native Paper** - Material Design 3 components
- **React Navigation** - Navigation
- **Redux Toolkit** - State management
- **Axios** - HTTP client
- **React Native Maps** - Maps integration
- **Expo Location** - Location services

## 🎨 Design System

The app uses the TimoRides design system:
- **Primary Color:** #2847bc (Timo Blue)
- **Accent Color:** #fed902 (Timo Yellow)
- **Material Design 3** components
- **Futuristic glow effects** on buttons and cards

## 🔐 Environment Variables

Create a `.env` file (optional):
```
API_BASE_URL=http://localhost:4205
```

## 📝 Development Notes

- The app polls for new bookings every 10 seconds when online
- Location updates are sent every 5 seconds when online
- Authentication tokens are stored securely using AsyncStorage

## 🚧 TODO

- [ ] Add push notifications
- [ ] Implement OTP trip confirmation
- [ ] Add navigation integration
- [ ] Implement chat functionality
- [ ] Add vehicle management
- [ ] Add document upload

## 📄 License

Private - TimoRides

