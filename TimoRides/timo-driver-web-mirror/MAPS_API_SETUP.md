# Maps API Setup Guide

## Overview

The TimoRides Driver App has **two different implementations** that require different map APIs:

1. **Web Mirror** (Development/Testing) - Uses Google Maps JavaScript API
2. **React Native App** (Production) - Uses react-native-maps with native SDKs

---

## 1. Web Mirror (Current Implementation)

### API Required:
- **Google Maps JavaScript API** ✅
- **Directions API** (for route calculation)
- **Geocoding API** (for address conversion)

### Setup:
1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Enable:
   - Maps JavaScript API
   - Directions API
   - Geocoding API
3. Add API key to `.env`:
   ```env
   VITE_GOOGLE_MAPS_API_KEY=your_key_here
   ```

### Current Status:
✅ MapView component uses Google Maps JavaScript API
✅ Embedded interactive maps work in browser
✅ Routes and markers display correctly

---

## 2. React Native App (Production)

### Libraries Used:
- **react-native-maps** (already in package.json)
- Native map SDKs (automatically used by react-native-maps)

### APIs Required:

#### For Android:
- **Google Maps SDK for Android**
  - Requires API key in `android/app/src/main/AndroidManifest.xml`
  - Enable in Google Cloud Console: "Maps SDK for Android"

#### For iOS:
- **Apple Maps** (default, no API key needed)
  - OR **Google Maps SDK for iOS** (if you want Google Maps on iOS)
  - If using Google Maps on iOS, enable "Maps SDK for iOS" in Google Cloud Console

### Setup Steps:

#### Android Setup:
1. Enable **Maps SDK for Android** in Google Cloud Console
2. Get your Android API key
3. Add to `android/app/src/main/AndroidManifest.xml`:
   ```xml
   <application>
     <meta-data
       android:name="com.google.android.geo.API_KEY"
       android:value="YOUR_ANDROID_API_KEY"/>
   </application>
   ```

#### iOS Setup (if using Google Maps):
1. Enable **Maps SDK for iOS** in Google Cloud Console
2. Get your iOS API key
3. Add to `ios/AppDelegate.m` or `Info.plist`

#### iOS Setup (using Apple Maps - default):
- No API key needed!
- Apple Maps works out of the box with react-native-maps

### Code Implementation:

The React Native app will use `react-native-maps` like this:

```javascript
import MapView, { Marker, Polyline } from 'react-native-maps';

<MapView
  style={{ flex: 1 }}
  initialRegion={{
    latitude: -29.8587,
    longitude: 31.0218,
    latitudeDelta: 0.0922,
    longitudeDelta: 0.0421,
  }}
>
  <Marker
    coordinate={{ latitude: pickup.lat, longitude: pickup.lng }}
    title="Pickup"
    pinColor="green"
  />
  <Marker
    coordinate={{ latitude: dest.lat, longitude: dest.lng }}
    title="Destination"
    pinColor="red"
  />
  <Polyline
    coordinates={routeCoordinates}
    strokeColor="#2847bc"
    strokeWidth={3}
  />
</MapView>
```

---

## Summary

| Platform | Library | API Required | API Key Location |
|----------|---------|--------------|-----------------|
| **Web Mirror** | Google Maps JavaScript API | Maps JavaScript API, Directions API, Geocoding API | `.env` file |
| **React Native (Android)** | react-native-maps | Maps SDK for Android | `AndroidManifest.xml` |
| **React Native (iOS)** | react-native-maps | Apple Maps (default) or Maps SDK for iOS | `Info.plist` or `AppDelegate.m` |

---

## Next Steps

1. ✅ **Web Mirror**: Already set up with JavaScript API
2. ⏳ **React Native**: Will need native SDK setup when building for Android/iOS
3. 📝 **Note**: The React Native app code will be different from the web mirror, using `react-native-maps` components instead of the JavaScript API

---

## Important Notes

- **Different APIs**: Web uses JavaScript API, React Native uses native SDKs
- **Different Keys**: You may need separate API keys for web vs Android vs iOS
- **Different Code**: The map components will be different between web and React Native
- **Same Functionality**: Both will show maps, routes, and markers, just implemented differently

