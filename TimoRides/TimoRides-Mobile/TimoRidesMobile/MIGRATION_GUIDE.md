# TimoRides Android to React Native Migration Guide

This document outlines the migration progress and next steps for completing the React Native version of TimoRides.

## ✅ Completed

### Project Setup
- [x] React Native project structure created
- [x] TypeScript configuration
- [x] Theme system with TimoRides branding
- [x] Navigation structure
- [x] API service layer
- [x] Base screens (Splash, Onboarding, Login, OTP)

### Screens Migrated
- [x] Splash Screen
- [x] Onboarding Screen (3 slides)
- [x] Login Screen (Phone + Email/Password)
- [x] OTP Verification Screen

## 🔲 Next Steps

### 1. Initialize Native Projects

You need to initialize the iOS and Android native projects. Run:

```bash
cd TimoRides-Mobile/TimoRidesMobile
npx react-native init TimoRidesMobile --skip-install
```

Or use the React Native CLI to generate native projects:

```bash
npx @react-native-community/cli init TimoRidesMobile
```

Then copy your `src/` folder and configuration files into the generated project.

### 2. Install Dependencies

```bash
npm install

# iOS dependencies
cd ios && pod install && cd ..
```

### 3. Screens to Port

#### Priority 1: Core Functionality
- [ ] Home Screen (with Maps integration)
- [ ] Driver Selection Screen (marketplace model)
- [ ] Booking Flow Screen
- [ ] Booking Status/Tracking Screen

#### Priority 2: Additional Features
- [ ] History Screen
- [ ] Settings Screen
- [ ] Profile Screen
- [ ] Payment Screen
- [ ] Chat Screen

### 4. Components to Create

- [ ] DriverCard component
- [ ] BottomSheet component
- [ ] MapView wrapper
- [ ] Rating component
- [ ] Filter components (for driver selection)
- [ ] Loading states
- [ ] Error states

### 5. Services to Implement

- [ ] Location Service (Geolocation)
- [ ] Push Notifications
- [ ] Offline Storage (AsyncStorage with queue)
- [ ] Payment Service
- [ ] Chat Service (WebSocket)

### 6. Platform-Specific Configuration

#### iOS (`ios/`)
- [ ] Configure Info.plist (permissions, URL schemes)
- [ ] Add Google Maps SDK (if using)
- [ ] Configure push notifications (APNs)
- [ ] Add app icons and splash screens
- [ ] Update AppDelegate for deep linking

#### Android (`android/`)
- [ ] Configure AndroidManifest.xml (permissions, API keys)
- [ ] Add Google Maps API key
- [ ] Configure push notifications (FCM)
- [ ] Add app icons and splash screens
- [ ] Update MainActivity for deep linking

### 7. Features from Android App

#### From Android App Structure:
```
Timo-Android-App/app/src/main/java/com/itechnotion/nextgen/
├── home/
│   ├── HomepageActivity.java          → HomeScreen.tsx
│   ├── AddressLocationActivity.java   → AddressSelection component
│   └── MainActivity.java              → AppNavigator (already done)
├── ride/
│   ├── SelecteRideActivity.java       → DriverSelectionScreen.tsx
│   ├── DetailSelecRideActivity.java   → DriverDetailScreen.tsx
│   ├── BookingRequestActivity.java    → BookingScreen.tsx
│   └── BookingCancelActivity.java     → CancelBooking component
├── payment/
│   ├── PaymentActivity.java           → PaymentScreen.tsx
│   └── MyWalletActivity.java          → WalletScreen.tsx
├── history/
│   ├── HistoryActivity.java           → HistoryScreen.tsx
│   ├── RatingActivity.java            → RatingScreen.tsx
│   └── TipsActivity.java              → TipsScreen.tsx
├── chat/
│   └── ChatActivity.java              → ChatScreen.tsx
├── setting/
│   ├── SettingActivity.java           → SettingsScreen.tsx
│   └── MyAccountActivity.java         → ProfileScreen.tsx
└── invitefriend/
    └── InviteFrndActivity.java        → InviteScreen.tsx
```

### 8. Key Differences from Android App

1. **UI Framework:**
   - Android: XML layouts + Java Activities
   - React Native: JSX components + React hooks

2. **State Management:**
   - Android: Local variables, SharedPreferences, ViewModels
   - React Native: React hooks (useState, useContext), AsyncStorage

3. **Navigation:**
   - Android: Activities + Intents
   - React Native: React Navigation

4. **Maps:**
   - Android: Google Maps SDK for Android
   - React Native: react-native-maps (works on both platforms)

5. **Styling:**
   - Android: XML styles, dimensions.xml
   - React Native: StyleSheet API, React Native Paper theming

## 📝 Migration Checklist

### Week 1-2: Foundation ✅
- [x] Project setup
- [x] Theme system
- [x] Navigation
- [x] API service
- [x] Auth screens

### Week 3-4: Core Screens
- [ ] Home screen with maps
- [ ] Driver selection (marketplace)
- [ ] Booking flow
- [ ] Location services

### Week 5-6: Advanced Features
- [ ] Real-time tracking
- [ ] Payment integration
- [ ] History & ratings
- [ ] Settings & profile

### Week 7: Polish & Testing
- [ ] Error handling
- [ ] Loading states
- [ ] Offline support
- [ ] Testing on devices
- [ ] App Store preparation

## 🔗 Key Resources

### Android App Reference
- Location: `/Volumes/Storage/OASIS_CLEAN/TimoRides/Timo-Android-App/`
- Key files:
  - `app/src/main/java/com/itechnotion/nextgen/` - Java Activities
  - `app/src/main/res/layout/` - XML layouts
  - `app/src/main/res/values/` - Colors, strings, dimensions

### Web Mirror Reference
- Location: `/Volumes/Storage/OASIS_CLEAN/TimoRides/timo-android-web-mirror/`
- Key files:
  - `src/screens/` - React components (similar structure to RN)
  - `src/theme.js` - Theme configuration (already adapted)

### Backend API
- Location: `/Volumes/Storage/OASIS_CLEAN/TimoRides/ride-scheduler-be/`
- Base URL: `http://localhost:4205/api` (dev)
- Documentation: Available at `/api-docs` endpoint

## 🚨 Important Notes

1. **Package Name:** Use `com.timorides.app` for both iOS and Android
2. **Maps API:** You'll need Google Maps API key (or use Apple Maps for iOS)
3. **Permissions:** Request location, notifications, camera permissions
4. **Offline Support:** Implement queue system for offline bookings
5. **Platform Differences:** Some UI/UX differences between iOS and Android are expected

## 📱 Testing Strategy

1. **iOS Simulator:** Test on various iPhone models
2. **Android Emulator:** Test on various Android versions
3. **Physical Devices:** Test on real devices for location services
4. **Backend Integration:** Test with actual backend API

## 🎯 Success Criteria

- [ ] App runs on both iOS and Android
- [ ] All core features work (auth, booking, payment)
- [ ] Maps integration works on both platforms
- [ ] Offline mode queues requests
- [ ] App Store ready (iOS)
- [ ] Google Play ready (Android)

---

**Last Updated:** January 2025  
**Status:** Foundation Complete, Core Screens in Progress

