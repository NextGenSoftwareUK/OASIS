# TimoRides Android App - Rebranding Summary

**Date:** October 20, 2025  
**Status:** ✅ Initial Rebranding Complete

---

## ✅ What Was Changed

### 1. **Brand Colors** (`app/src/main/res/values/colors.xml`)
```xml
<!-- BEFORE -->
<color name="colorPrimary">#008577</color>  <!-- Teal -->
<color name="colorAccent">#000000</color>   <!-- Black -->

<!-- AFTER -->
<color name="colorPrimary">#2847bc</color>  <!-- Timo Blue -->
<color name="colorAccent">#fed902</color>   <!-- Timo Yellow -->
<color name="colorPrimaryDark">#1534aa</color>
<color name="colorActionYellow">#fab700</color>
```

**Visual Impact:**
- All buttons, headers, and primary UI elements now use Timo's signature blue
- Accent elements (highlights, active states) use Timo's yellow
- Matches the existing Angular web app styling

---

### 2. **App Name & Identity** (`app/src/main/res/values/strings.xml`)
```xml
<!-- BEFORE -->
<string name="app_name">UberNexGen</string>
<string name="request_nearby_texi_driver_and_get_pickup_within_10_minutes">
    Request nearby texi driver and get\n pickup within 10 minutes
</string>

<!-- AFTER -->
<string name="app_name">TimoRides</string>
<string name="request_nearby_texi_driver_and_get_pickup_within_10_minutes">
    Choose your premium driver and get\n picked up within minutes
</string>
```

**Messaging Changes:**
- Emphasizes **"Choose your premium driver"** (marketplace model)
- Removes generic "taxi driver" language
- Aligns with Timo's premium positioning

---

### 3. **Package Name** (`app/build.gradle` + `AndroidManifest.xml`)
```gradle
// BEFORE
applicationId "com.itechnotion.nextgen"

// AFTER  
applicationId "com.timorides.app"
```

**All Activity References Updated:**
- `AndroidManifest.xml` → All 24 activities updated to `com.timorides.app.*`
- Package structure ready for Play Store submission as TimoRides

**Version Reset:**
```gradle
versionCode 1
versionName "1.0.0-mvp"
```

---

## ⚠️ What Still Needs to Be Done

### Critical (Required Before Launch)

1. **Refactor Java Package Names**
   - All `.java` files still declare `package com.itechnotion.nextgen`
   - Files need to be physically moved to `com/timorides/app/` directory structure
   - All package import statements need updating
   - **Estimated Time:** 2-3 hours

2. **Replace App Icons & Logo**
   - Launcher icons (`ic_launcher.png`) still show template icon
   - Splash screen (`splash_screen.png`) needs Timo logo
   - Intro slides (`slied1.png`, `slied2.png`, `slied3.png`) need custom content
   - **Required Assets:**
     - `ic_launcher.png` (multiple densities: mdpi, hdpi, xhdpi, xxhdpi, xxxhdpi)
     - `splash_screen.png` (1080x1920 recommended)
     - Intro slide images (3 images matching Timo's value propositions)

3. **Google Maps API Key**
   - Current key (`AIzaSyDCF9ev6kg9bBtr4_nXwFA1SzEzvT8uc-s`) is from template
   - Generate new key for `com.timorides.app` package
   - Enable required APIs: Maps SDK, Places API, Directions API, Distance Matrix API
   - Update `strings.xml` with new key

---

## 📁 Files Modified

```
CC-UberNextGen-Android/
├── app/
│   ├── build.gradle                          ✅ Updated package & version
│   └── src/main/
│       ├── AndroidManifest.xml               ✅ Updated all 24 activity references
│       └── res/
│           └── values/
│               ├── colors.xml                ✅ Timo brand colors applied
│               └── strings.xml               ✅ App name & messaging updated
```

---

## 🚀 Next Steps

### Immediate (This Week)
1. Obtain Timo logo files and design assets from design team
2. Generate Google Maps API key for TimoRides
3. Refactor Java package names (can be done with Android Studio refactoring tool)
4. Test app builds and launches successfully

### Short-Term (Week 2-3)
5. Implement driver marketplace UX (see `TIMO_ANDROID_IMPROVEMENTS.md`)
6. Integrate with TimoRides backend API
7. Remove template payment methods, add mobile money options

### Medium-Term (Week 4-8)
8. Add offline-first capabilities with Room database
9. Integrate OASIS Avatar, Karma, and Wallet APIs
10. Implement trust & safety features (SOS button, enhanced ratings)

---

## 📊 Brand Consistency Checklist

- [x] Primary blue (#2847bc) applied to all main UI elements
- [x] Accent yellow (#fed902) used for CTAs and highlights
- [x] App name shows as "TimoRides" throughout
- [x] Package name changed to com.timorides.app
- [ ] Logo and icons updated (pending design assets)
- [ ] Intro slides reflect Timo's value propositions
- [ ] All references to "Uber" or "NextGen" removed from UI
- [ ] Currency changed from USD to ZAR (South African Rand)
- [ ] Location coordinates updated from India to Durban, South Africa

---

## 🧪 Testing Recommendations

Before deploying to users:

1. **Build & Install Test**
   ```bash
   cd /Volumes/Storage\ 1/OASIS_CLEAN/TimoRides/CC-UberNextGen-Android
   ./gradlew clean
   ./gradlew assembleDebug
   adb install -r app/build/outputs/apk/debug/app-debug.apk
   ```

2. **Visual Inspection**
   - Launch app and verify splash screen shows Timo branding
   - Check all screens use Timo blue/yellow color scheme
   - Verify no remnants of template branding visible

3. **Functional Test**
   - App launches without crashes
   - All navigation flows work
   - Maps load correctly (once new API key added)

---

## 📞 Questions for Stakeholders

1. **Design Assets:** When can we receive the official Timo logo files and app icons?
2. **Messaging:** Is the new tagline "Choose your premium driver and get picked up within minutes" approved?
3. **Scope:** Should we keep the "Bike" ride option or focus only on premium cars for Durban MVP?
4. **Timeline:** What is the target launch date for the MVP?

---

## 💡 Pro Tips

### Using Android Studio Refactor Tool
To rename the package from `com.itechnotion.nextgen` to `com.timorides.app`:

1. In Android Studio, right-click on the `java` folder
2. Uncheck "Compact Middle Packages"
3. Right-click on `com.itechnotion.nextgen` package
4. Select **Refactor → Rename**
5. Choose "Rename package"
6. Enter new name: `com.timorides.app`
7. Click "Refactor" → Android Studio will update all files automatically

### Generating App Icons
Use [Android Asset Studio](https://romannurik.github.io/AndroidAssetStudio/icons-launcher.html):
1. Upload Timo logo
2. Set background color to #2847bc (Timo blue)
3. Download all densities
4. Replace files in `app/src/main/res/mipmap-*` folders

---

**Document Version:** 1.0  
**Prepared By:** AI Development Assistant  
**For:** TimoRides Android Development Team



