# TimoRides Android App (MVP)

**Status:** 🎨 Rebranded | 🚧 Development in Progress  
**Target Platform:** Android 5.1+ (API 22+)  
**Package:** `com.timorides.app`  
**Version:** 1.0.0-mvp

---

## 📱 About This App

This is the **TimoRides Android rider application** - a premium, choice-first ride-hailing platform for Durban, South Africa. Built on a template foundation but customized for Timo's unique marketplace model where **riders choose their drivers**, not the other way around.

---

## 🎨 Recent Changes

### ✅ October 20, 2025 - Initial Rebranding
- **Brand colors applied:** Timo blue (#2847bc) and yellow (#fed902)
- **App name changed:** UberNexGen → TimoRides
- **Package renamed:** com.itechnotion.nextgen → com.timorides.app
- **Messaging updated:** Emphasizes "Choose your premium driver"

See [`REBRANDING_SUMMARY.md`](./REBRANDING_SUMMARY.md) for complete details.

---

## 📂 Project Structure

```
CC-UberNextGen-Android/
├── app/
│   ├── src/main/
│   │   ├── java/com/itechnotion/nextgen/  ⚠️ Needs refactoring to com.timorides.app
│   │   │   ├── home/                       # Homepage, map, address selection
│   │   │   ├── ride/                       # Ride selection, booking, tracking
│   │   │   ├── loginsignup/                # Authentication flows
│   │   │   ├── payment/                    # Wallet & payment options
│   │   │   ├── history/                    # Ride history & ratings
│   │   │   ├── setting/                    # User settings & account
│   │   │   ├── notification/               # Notifications
│   │   │   ├── invitefriend/               # Referral system
│   │   │   ├── chat/                       # Driver-rider messaging
│   │   │   └── utils/                      # Shared utilities
│   │   ├── res/
│   │   │   ├── layout/                     # XML layouts (39 screens)
│   │   │   ├── drawable/                   # Icons, backgrounds
│   │   │   ├── values/
│   │   │   │   ├── colors.xml              ✅ Timo brand colors applied
│   │   │   │   ├── strings.xml             ✅ Timo branding applied
│   │   │   │   └── styles.xml
│   │   │   └── mipmap-*/                   ⚠️ Needs Timo logo
│   │   └── AndroidManifest.xml             ✅ Package name updated
│   ├── build.gradle                        ✅ Updated package & version
│   └── proguard-rules.pro
├── gradle/
├── build.gradle
├── settings.gradle
├── TIMO_ANDROID_IMPROVEMENTS.md            📋 Comprehensive improvement roadmap
├── REBRANDING_SUMMARY.md                   📋 Rebranding changelog
└── README.md                               📋 This file
```

---

## 🚀 Getting Started

### Prerequisites
- **Android Studio:** Arctic Fox or later
- **JDK:** 8 or higher
- **Android SDK:** API 22 (Lollipop) through API 30
- **Google Maps API Key:** Required for maps functionality

### Setup Instructions

1. **Clone/Open Project**
   ```bash
   cd "/Volumes/Storage 1/OASIS_CLEAN/TimoRides/CC-UberNextGen-Android"
   # Open in Android Studio
   ```

2. **Update Google Maps API Key**
   - Generate a new key at [Google Cloud Console](https://console.cloud.google.com/)
   - Enable: Maps SDK, Places API, Directions API, Distance Matrix API
   - Update `app/src/main/res/values/strings.xml`:
     ```xml
     <string name="google_maps_key">YOUR_NEW_KEY_HERE</string>
     ```

3. **Sync Gradle**
   - Android Studio → File → Sync Project with Gradle Files

4. **Run the App**
   - Connect Android device or start emulator
   - Click Run button or `Shift + F10`

---

## 🔑 Key Features

### Current (Template Features)
- ✅ User authentication (phone + OTP)
- ✅ Google Maps integration
- ✅ Ride booking flow
- ✅ Payment options (cards, wallet)
- ✅ Ride history
- ✅ User settings
- ✅ Notifications
- ✅ Invite/referral system
- ✅ Chat with driver

### To Be Implemented (Timo-Specific)
- 🔲 **Marketplace UX:** Browse and choose individual drivers (not vehicle types)
- 🔲 **Driver Profiles:** Photos, ratings, languages, amenities, Karma scores
- 🔲 **Offline Mode:** Queue ride requests when offline, sync when connected
- 🔲 **Mobile Money:** M-Pesa, MTN Mobile Money integration
- 🔲 **OASIS Integration:** Avatar (identity), Karma (trust), Wallet (payments)
- 🔲 **Trust & Safety:** Enhanced ratings, SOS button, trust badges
- 🔲 **South Africa Localization:** ZAR currency, local languages, Durban locations

See [`TIMO_ANDROID_IMPROVEMENTS.md`](./TIMO_ANDROID_IMPROVEMENTS.md) for complete roadmap.

---

## 🎯 Core Differentiators (Timo vs Template)

| Feature | Template (Uber-style) | TimoRides Vision |
|---------|----------------------|------------------|
| **Matching** | Algorithmic (nearest driver) | User choice (marketplace) |
| **Driver Info** | Name + rating only | Full profile: photo, reviews, languages, amenities |
| **Payments** | Cards, PayPal | Mobile money, crypto (low fees) |
| **Offline** | Requires connectivity | Offline-first (queue & sync) |
| **Trust** | Basic star rating | Karma scores + detailed breakdowns |
| **Target Market** | Generic | Premium Durban riders |

---

## 📊 Technology Stack

### Core
- **Language:** Java
- **Min SDK:** 22 (Android 5.1 Lollipop)
- **Target SDK:** 30 (Android 11)
- **Build System:** Gradle

### Key Dependencies
```gradle
// UI & Layout
implementation 'com.google.android.material:material:1.4.0'
implementation 'androidx.constraintlayout:constraintlayout:2.0.4'
implementation 'de.hdodenhof:circleimageview:3.1.0'

// Maps & Location
implementation 'com.google.android.gms:play-services-maps:17.0.0'
implementation 'com.google.android.gms:play-services-location:18.0.0'

// View Binding
implementation 'com.jakewharton:butterknife:10.2.3'
annotationProcessor 'com.jakewharton:butterknife-compiler:10.2.3'

// Image Loading
implementation 'com.github.bumptech.glide:glide:4.12.0'

// Permissions
implementation 'com.karumi:dexter:4.2.0'
```

### To Be Added
```gradle
// Offline Database
implementation 'androidx.room:room-runtime:2.5.0'
annotationProcessor 'androidx.room:room-compiler:2.5.0'

// Network (Retrofit)
implementation 'com.squareup.retrofit2:retrofit:2.9.0'
implementation 'com.squareup.retrofit2:converter-gson:2.9.0'

// OASIS Integration
// implementation 'oasis.api:avatar:1.0.0'  // To be provided
// implementation 'oasis.api:karma:1.0.0'
// implementation 'oasis.api:wallet:1.0.0'
```

---

## 🧪 Testing

### Manual Testing Checklist
- [ ] App launches successfully
- [ ] Splash screen shows Timo branding
- [ ] Login/signup flow works
- [ ] Maps load with user location
- [ ] Ride selection bottom sheet appears
- [ ] Navigation drawer opens/closes
- [ ] Payment options display
- [ ] History screen shows rides
- [ ] Settings can be modified

### Known Issues
1. **Package names not refactored:** Java files still use `com.itechnotion.nextgen`
2. **Hardcoded locations:** Map initializes to Ahmedabad, India (needs Durban coordinates)
3. **Deprecated APIs:** Uses old `FusedLocationApi` (should migrate to `FusedLocationProviderClient`)
4. **No real backend:** All data is mock/hardcoded
5. **Template icons:** Launcher icon and splash screen need Timo branding

---

## 🔧 Development Tasks

### Priority 1: Essential (Before Any Testing)
1. Refactor package names from `com.itechnotion.nextgen` to `com.timorides.app`
2. Replace app icons and splash screen with Timo branding
3. Update Google Maps API key
4. Change default map coordinates to Durban: `(-29.8587, 31.0218)`
5. Connect to TimoRides backend API (replace all mock data)

### Priority 2: Marketplace Features
6. Redesign ride selection screen to show individual drivers
7. Create driver profile detail view
8. Implement driver filtering (by price, rating, vehicle type, amenities)
9. Add driver search functionality

### Priority 3: Offline & Reliability
10. Implement Room database for local storage
11. Create sync service for offline ride requests
12. Add connectivity monitoring
13. Display offline mode indicators

### Priority 4: Payments & Trust
14. Remove template payment methods
15. Add mobile money options (M-Pesa, MTN)
16. Integrate OASIS Wallet API
17. Implement Karma score display
18. Add enhanced rating system
19. Create SOS/emergency button

---

## 📚 Documentation

- **[TIMO_ANDROID_IMPROVEMENTS.md](./TIMO_ANDROID_IMPROVEMENTS.md)** - Comprehensive improvement roadmap with code examples
- **[REBRANDING_SUMMARY.md](./REBRANDING_SUMMARY.md)** - Complete rebranding changelog
- **[../PathPulse_OASIS_Integration_Guide.md](../PathPulse_OASIS_Integration_Guide.md)** - Backend routing integration guide
- **[../Timo_MVP_Core_Priorities.md](../Timo_MVP_Core_Priorities.md)** - MVP business requirements
- **[../Timo_MVP_Roadmap.md](../Timo_MVP_Roadmap.md)** - Overall project timeline

---

## 🤝 Contributing

### Code Style
- Follow standard Java conventions
- Use meaningful variable names
- Add comments for complex logic
- Keep methods under 50 lines when possible

### Git Workflow
```bash
# Create feature branch
git checkout -b feature/driver-marketplace

# Make changes, commit often
git commit -m "feat: Add driver profile RecyclerView adapter"

# Push and create PR
git push origin feature/driver-marketplace
```

### Commit Message Format
- `feat:` New feature
- `fix:` Bug fix
- `refactor:` Code refactoring
- `style:` UI/styling changes
- `docs:` Documentation updates
- `test:` Test additions/modifications

---

## 🐛 Troubleshooting

### Build Fails
```bash
# Clean and rebuild
./gradlew clean
./gradlew build

# Clear Android Studio cache
File → Invalidate Caches / Restart
```

### Map Not Loading
- Check Google Maps API key is valid
- Ensure Maps SDK for Android is enabled in Google Cloud Console
- Verify package name matches (`com.timorides.app`)
- Check location permissions are granted

### Gradle Sync Issues
- Update Android Studio to latest version
- Update Gradle plugin in `build.gradle`
- Check internet connection (downloads dependencies)

---

## 📞 Support & Contact

- **Technical Issues:** See [TIMO_ANDROID_IMPROVEMENTS.md](./TIMO_ANDROID_IMPROVEMENTS.md)
- **Backend API:** Coordinate with TimoRides backend team
- **OASIS Integration:** Contact OASIS integration team
- **Design Assets:** Request from TimoRides design team

---

## 📄 License

Proprietary - TimoRides © 2025

---

**Last Updated:** October 20, 2025  
**Maintained By:** TimoRides Development Team
