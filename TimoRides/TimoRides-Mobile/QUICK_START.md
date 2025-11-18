# TimoRides Mobile - Quick Start Guide

## 🎉 Project Created!

The React Native project structure has been created and the foundation is ready for development.

## 📍 Location

```
/Volumes/Storage/OASIS_CLEAN/TimoRides/TimoRides-Mobile/TimoRidesMobile/
```

## ✅ What's Been Done

### Foundation Setup
- ✅ React Native project structure with TypeScript
- ✅ Theme system with TimoRides branding (colors, typography)
- ✅ Navigation structure (React Navigation)
- ✅ API service layer (Axios with authentication)
- ✅ Type definitions for TypeScript

### Screens Implemented
- ✅ Splash Screen (with animations)
- ✅ Onboarding Screen (3 slides)
- ✅ Login Screen (Phone + Email/Password tabs)
- ✅ OTP Verification Screen (6-digit code input)

### Configuration Files
- ✅ `package.json` - Dependencies configured
- ✅ `tsconfig.json` - TypeScript configuration
- ✅ `babel.config.js` - Babel with module resolver
- ✅ `metro.config.js` - Metro bundler config
- ✅ `.gitignore` - Git ignore rules

## 🚀 Next Steps to Run the App

### 1. Install Dependencies

```bash
cd /Volumes/Storage/OASIS_CLEAN/TimoRides/TimoRides-Mobile/TimoRidesMobile
npm install
```

### 2. Initialize Native Projects

**Important:** You need to initialize the iOS and Android native projects. You have two options:

#### Option A: Use React Native CLI (Recommended)
```bash
# Make sure you're in the TimoRidesMobile directory
cd /Volumes/Storage/OASIS_CLEAN/TimoRides/TimoRides-Mobile/TimoRidesMobile

# Initialize React Native project (this will create ios/ and android/ folders)
npx @react-native-community/cli init TimoRidesMobileTemp --template react-native-template-typescript

# Copy the native folders
cp -r TimoRidesMobileTemp/ios .
cp -r TimoRidesMobileTemp/android .

# Copy our src/ folder and config files
cp -r TimoRidesMobileTemp/.watchmanconfig . 2>/dev/null || true
cp -r TimoRidesMobileTemp/.gitattributes . 2>/dev/null || true

# Clean up temp folder
rm -rf TimoRidesMobileTemp
```

#### Option B: Use React Native Init
```bash
# Navigate to parent directory
cd /Volumes/Storage/OASIS_CLEAN/TimoRides/TimoRides-Mobile

# Create a fresh React Native project
npx react-native init TimoRidesMobileNew --template react-native-template-typescript

# Copy our src/ folder and configs to the new project
cp -r TimoRidesMobile/src TimoRidesMobileNew/
cp TimoRidesMobile/App.tsx TimoRidesMobileNew/
cp TimoRidesMobile/package.json TimoRidesMobileNew/
cp TimoRidesMobile/tsconfig.json TimoRidesMobileNew/
cp TimoRidesMobile/babel.config.js TimoRidesMobileNew/
cp TimoRidesMobile/metro.config.js TimoRidesMobileNew/

# Replace old folder
rm -rf TimoRidesMobile
mv TimoRidesMobileNew TimoRidesMobile
```

### 3. Install iOS Dependencies (if on macOS)

```bash
cd ios
pod install
cd ..
```

### 4. Update Native Configuration

#### iOS (`ios/TimoRidesMobile/Info.plist`)
Add these permissions:
```xml
<key>NSLocationWhenInUseUsageDescription</key>
<string>We need your location to show nearby drivers and help you book rides.</string>
<key>NSLocationAlwaysAndWhenInUseUsageDescription</key>
<string>We need your location to track your ride in real-time.</string>
<key>NSPhotoLibraryUsageDescription</key>
<string>We need access to your photos to upload profile pictures.</string>
```

#### Android (`android/app/src/main/AndroidManifest.xml`)
Add these permissions:
```xml
<uses-permission android:name="android.permission.INTERNET" />
<uses-permission android:name="android.permission.ACCESS_FINE_LOCATION" />
<uses-permission android:name="android.permission.ACCESS_COARSE_LOCATION" />
```

### 5. Start Metro Bundler

```bash
npm start
```

### 6. Run on iOS

```bash
npm run ios
```

### 7. Run on Android

```bash
npm run android
```

## 📱 What Works Now

1. **Splash Screen** - Shows TimoRides logo and animates
2. **Onboarding** - 3 slides explaining the app
3. **Login** - Phone number or email/password login
4. **OTP Verification** - 6-digit code input for phone login

## 🔧 Configuration Needed

### Backend API URL

Update `src/services/api.ts` with your backend URL:
- Development: `http://localhost:4205/api` (iOS Simulator)
- Android Emulator: `http://10.0.2.2:4205/api`
- Production: `https://ride-scheduler-be.onrender.com/api`

### Google Maps API Key (when you implement maps)

1. Get API key from Google Cloud Console
2. iOS: Add to `ios/TimoRidesMobile/AppDelegate.m`
3. Android: Add to `android/app/src/main/AndroidManifest.xml`

## 📋 Remaining Work

### High Priority
- [ ] Home Screen with Maps
- [ ] Driver Selection Screen
- [ ] Booking Flow
- [ ] Real-time Tracking

### Medium Priority
- [ ] Payment Integration
- [ ] History Screen
- [ ] Settings Screen
- [ ] Chat Screen

### Low Priority
- [ ] Profile Management
- [ ] Invite Friends
- [ ] Notifications
- [ ] Offline Support

## 🐛 Troubleshooting

### Metro Bundler Issues
```bash
npm start -- --reset-cache
```

### iOS Build Issues
```bash
cd ios
pod deintegrate
pod install
cd ..
```

### Android Build Issues
```bash
cd android
./gradlew clean
cd ..
```

### TypeScript Errors
```bash
npx tsc --noEmit
```

## 📚 Resources

- **React Native Docs:** https://reactnative.dev/
- **React Navigation:** https://reactnavigation.org/
- **React Native Paper:** https://callstack.github.io/react-native-paper/
- **Backend API:** `../ride-scheduler-be/README.md`

## 🎯 Success Criteria

Once you can:
- ✅ Run `npm install` successfully
- ✅ Initialize native projects (ios/ and android/ folders exist)
- ✅ Run `npm run ios` and see the splash screen
- ✅ Navigate through onboarding → login → OTP screens

You're ready to continue building the remaining screens!

---

**Created:** January 2025  
**Status:** Foundation Complete ✅

