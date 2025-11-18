# TimoRides Mobile (React Native)

Cross-platform mobile application for TimoRides, built with React Native. This app works on both iOS and Android.

## 🚀 Getting Started

### Prerequisites

- Node.js 18+ (20+ recommended for latest React Native)
- npm or yarn
- React Native CLI
- Xcode (for iOS development)
- Android Studio (for Android development)
- CocoaPods (for iOS)

### Installation

1. **Install dependencies:**
   ```bash
   cd TimoRides-Mobile/TimoRidesMobile
   npm install
   ```

2. **iOS Setup:**
   ```bash
   cd ios
   pod install
   cd ..
   ```

3. **Start Metro Bundler:**
   ```bash
   npm start
   ```

4. **Run on iOS:**
   ```bash
   npm run ios
   ```

5. **Run on Android:**
   ```bash
   npm run android
   ```

## 📁 Project Structure

```
TimoRidesMobile/
├── src/
│   ├── screens/          # Screen components
│   ├── components/       # Reusable components
│   ├── navigation/       # Navigation setup
│   ├── services/         # API services
│   ├── utils/           # Utility functions
│   ├── theme/           # Theme configuration
│   ├── types/           # TypeScript types
│   └── assets/          # Images, fonts, etc.
├── App.tsx              # Root component
└── index.js             # Entry point
```

## 🎨 Features

- ✅ Material Design 3 with React Native Paper
- ✅ TimoRides branding (colors, typography)
- ✅ Splash screen with animations
- ✅ Onboarding flow
- ✅ Authentication (Phone OTP, Email/Password)
- ✅ Navigation structure
- ✅ API service layer
- 🔲 Home screen with maps
- 🔲 Driver selection (marketplace)
- 🔲 Booking flow
- 🔲 Payment integration
- 🔲 Ride history
- 🔲 Settings

## 🔧 Configuration

### Backend API

Update the API base URL in `src/services/api.ts`:

```typescript
const API_BASE_URL = __DEV__
  ? 'http://localhost:4205/api' // Development
  : 'https://your-production-url.com/api'; // Production
```

For Android emulator, use `http://10.0.2.2:4205/api`
For iOS simulator, use `http://localhost:4205/api`

### Google Maps (when implemented)

Add your Google Maps API key to:
- iOS: `ios/TimoRidesMobile/AppDelegate.m`
- Android: `android/app/src/main/AndroidManifest.xml`

## 🧪 Development

- **Linting:** `npm run lint`
- **Testing:** `npm test`
- **Type checking:** TypeScript is configured for strict mode

## 📱 Platform-Specific Notes

### iOS
- Minimum iOS version: 14.0
- Uses CocoaPods for native dependencies

### Android
- Minimum SDK: 22 (Android 5.1)
- Target SDK: 33+
- Uses Gradle for build

## 📚 Key Libraries

- **React Native Paper:** Material Design 3 components
- **React Navigation:** Navigation framework
- **React Native Maps:** Maps integration
- **Axios:** HTTP client
- **AsyncStorage:** Local storage
- **React Native Reanimated:** Animations

## 🔗 Related Projects

- **Backend API:** `../ride-scheduler-be/`
- **Android App:** `../Timo-Android-App/`
- **Web Mirror:** `../timo-android-web-mirror/`

## 📄 License

Proprietary - TimoRides © 2025

---

**Status:** 🚧 In Development  
**Last Updated:** January 2025

