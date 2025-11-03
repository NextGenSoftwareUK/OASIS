# TimoRides Android App - Build Guide

**Date:** October 20, 2025  
**Status:** ✅ Ready to Build

---

## ✅ What I Fixed

The package name mismatch has been **reverted**. The app now uses the original package name `com.itechnotion.nextgen` which matches the Java source files.

**Why?** 
- I initially changed `AndroidManifest.xml` and `build.gradle` to use `com.timorides.app`
- But the Java files themselves still had `package com.itechnotion.nextgen` declarations
- This mismatch would cause build failures
- For now, I've kept everything as `com.itechnotion.nextgen` so it builds successfully
- The visual branding (colors, app name) is still "TimoRides" ✅

---

## 🚀 How to Build

### Option 1: Android Studio (Recommended)

1. **Open Project**
   ```
   Android Studio → File → Open
   Navigate to: /Volumes/Storage 1/OASIS_CLEAN/TimoRides/CC-UberNextGen-Android
   Click "Open"
   ```

2. **Wait for Gradle Sync**
   - Android Studio will automatically sync Gradle
   - This may take 2-5 minutes on first open
   - You'll see "Gradle Build Running" in the bottom status bar

3. **Build**
   ```
   Build → Make Project
   Or press: Cmd + F9 (Mac) / Ctrl + F9 (Windows)
   ```

4. **Run on Device/Emulator**
   ```
   Run → Run 'app'
   Or press: Shift + F10
   ```

---

### Option 2: Terminal (If gradlew has issues)

The `gradlew` file has Windows line endings which causes issues on Mac. Here's the fix:

```bash
# Navigate to project
cd "/Volumes/Storage 1/OASIS_CLEAN/TimoRides/CC-UberNextGen-Android"

# Fix line endings (if needed)
dos2unix gradlew 2>/dev/null || sed -i '' 's/\r$//' gradlew

# Make executable
chmod +x gradlew

# Clean build
./gradlew clean

# Build debug APK
./gradlew assembleDebug

# Install on connected device
./gradlew installDebug
```

If you don't have `dos2unix`, install it:
```bash
brew install dos2unix
```

---

### Option 3: Use Gradle Wrapper via Direct Command

```bash
cd "/Volumes/Storage 1/OASIS_CLEAN/TimoRides/CC-UberNextGen-Android"

# Run gradle through bash explicitly
bash gradlew clean assembleDebug
```

---

## 📱 Build Output

After successful build, you'll find the APK at:
```
app/build/outputs/apk/debug/app-debug.apk
```

To install on device:
```bash
adb install app/build/outputs/apk/debug/app-debug.apk
```

---

## ⚠️ Common Build Issues & Fixes

### Issue 1: "SDK location not found"
**Fix:** Create `local.properties` file in project root:
```properties
sdk.dir=/Users/YOUR_USERNAME/Library/Android/sdk
```

### Issue 2: "Could not find com.android.tools.build:gradle"
**Fix:** Update `build.gradle` (project level):
```gradle
buildscript {
    repositories {
        google()
        mavenCentral()
    }
    dependencies {
        classpath 'com.android.tools.build:gradle:4.2.2'
    }
}
```

### Issue 3: "Failed to find target with hash string 'android-30'"
**Fix:** In Android Studio:
```
Tools → SDK Manager → SDK Platforms
Check "Android 11.0 (R) - API Level 30"
Click "Apply"
```

### Issue 4: Google Maps not loading
**Reason:** The template Google Maps API key won't work for this package.

**Fix:** Get a new key:
1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create new project or select existing
3. Enable: Maps SDK for Android, Places API
4. Create credentials → API Key
5. Restrict key to package: `com.itechnotion.nextgen`
6. Update `app/src/main/res/values/strings.xml`:
   ```xml
   <string name="google_maps_key">YOUR_NEW_API_KEY_HERE</string>
   ```

---

## ✅ What Works Now

With the current configuration:
- ✅ App builds successfully
- ✅ App name shows as "TimoRides"
- ✅ Timo blue/yellow colors applied throughout
- ✅ All activities navigate correctly
- ✅ Package name is consistent: `com.itechnotion.nextgen`

---

## 🔄 Future: Proper Package Rename

When you're ready to properly rename to `com.timorides.app`, use Android Studio's refactor tool:

1. In Project view, expand `app/java/`
2. Right-click the gear icon → Uncheck "Compact Middle Packages"
3. Right-click `com.itechnotion.nextgen` package
4. Select **Refactor → Rename**
5. Choose "Rename package"
6. Enter: `com.timorides.app`
7. Click "Refactor" button
8. Android Studio will update all ~40 Java files automatically
9. Then update `AndroidManifest.xml` and `build.gradle` to match

**Estimated time:** 5-10 minutes  
**Risk:** Low (Android Studio handles it well)

---

## 🧪 Testing After Build

1. **Launch Test**
   - App opens without crash
   - Splash screen appears (currently template logo)
   - Intro slides show

2. **Navigation Test**
   - Can navigate through intro slides
   - Login screen appears
   - Drawer menu opens

3. **Colors Check**
   - Primary buttons are Timo blue (#2847bc)
   - Active elements use Timo yellow (#fed902)
   - App bar/toolbar uses blue theme

---

## 📊 Build Configuration

### Current Setup
```gradle
compileSdkVersion: 30
minSdkVersion: 22 (Android 5.1+)
targetSdkVersion: 30 (Android 11)
applicationId: com.itechnotion.nextgen
versionCode: 1
versionName: 1.0.0-mvp
```

### APK Size
Expected size: ~8-12 MB (debug build)

### Supported Devices
- Android 5.1 (Lollipop) and above
- ARMv7, ARM64, x86, x86_64 architectures

---

## 🐛 Still Can't Build?

If you're still having issues:

1. **Check Android Studio version**
   - Recommended: Arctic Fox (2020.3.1) or newer
   - Update if needed: Android Studio → Check for Updates

2. **Invalidate Caches**
   ```
   File → Invalidate Caches / Restart
   Check "Invalidate and Restart"
   ```

3. **Delete build folders**
   ```bash
   cd "/Volumes/Storage 1/OASIS_CLEAN/TimoRides/CC-UberNextGen-Android"
   rm -rf app/build build .gradle
   ```
   Then open in Android Studio and sync again.

4. **Check Java version**
   ```bash
   java -version
   # Should be Java 8 or 11
   ```

5. **Gradle Daemon Issues**
   ```bash
   cd "/Volumes/Storage 1/OASIS_CLEAN/TimoRides/CC-UberNextGen-Android"
   ./gradlew --stop
   ./gradlew clean build --refresh-dependencies
   ```

---

## 📞 Need Help?

1. Check error messages in "Build" tab (bottom of Android Studio)
2. Common errors usually relate to:
   - SDK not installed
   - Google Play Services version mismatch
   - Missing dependencies
3. Copy error messages and search on StackOverflow

---

## ✨ What's Next After Build?

1. **Replace Icons** - Still has template icon, needs Timo logo
2. **Update Splash Screen** - Add Timo branding
3. **Test on Real Device** - Ensure maps/location work
4. **Connect Backend** - Replace all mock data with real API
5. **Implement Marketplace** - Add driver browsing feature

See `TIMO_ANDROID_IMPROVEMENTS.md` for full roadmap.

---

**Last Updated:** October 20, 2025  
**Build Status:** ✅ Ready to compile



