# TimoRides iOS Development Brief

**Date:** January 2025  
**Project:** iOS App Development  
**Status:** Planning Phase  
**Target:** Native iOS app for iPhone/iPad

---

## 🎯 Project Goal

Create a **fully functional iOS version** of the TimoRides Android app that:
- ✅ Works natively on iOS devices (iPhone/iPad)
- ✅ Can be submitted to the Apple App Store
- ✅ Provides the same functionality as the Android app
- ✅ Maintains TimoRides branding and design
- ✅ Connects to the same backend API

---

## 📱 Current State

### Existing Assets

1. **Android App** (`Timo-Android-App/`)
   - Location: `/Volumes/Storage/OASIS_CLEAN/TimoRides/Timo-Android-App/`
   - Language: Java
   - Framework: Android SDK with Material Design
   - Status: Functional, needs frontend renovation
   - Key Features: Login, Maps, Driver Selection, Booking, Payment, History

2. **Web Mirror** (`timo-android-web-mirror/`)
   - Location: `/Volumes/Storage/OASIS_CLEAN/TimoRides/timo-android-web-mirror/`
   - Framework: React + Vite + Material-UI (MUI)
   - Status: Functional prototype for rapid design iteration
   - Purpose: Fast frontend development without Android Studio
   - Screens: Splash, Onboarding, Login, Home, Driver Selection, Booking, History, Settings

3. **Backend API**
   - Location: `ride-scheduler-be/`
   - Framework: Node.js/Express
   - Status: Functional
   - Endpoints: Authentication, Booking, Drivers, Payments, etc.

### Design Assets

- **Logo:** `/Volumes/Storage/OASIS_CLEAN/TimoRides/TIMO LOGO.png`
  - Blue square with rounded corners
  - White "Timo" text with distinctive "o" symbol
  - Dimensions: 2299x2484px

- **Brand Colors:**
  - Primary Blue: `#2847bc`
  - Primary Dark: `#1534aa`
  - Primary Light: `#3d5ed9`
  - Accent Yellow: `#fed902`
  - Accent Yellow Dark: `#fab700`
  - Success: `#4ACC12`
  - Error: `#e4033b`

---

## 🎨 Design System

### Brand Identity

**TimoRides** is a premium, choice-first ride-hailing platform for Durban, South Africa. Unlike traditional ride-sharing apps, TimoRides allows **riders to choose their drivers** (marketplace model) rather than algorithmic matching.

### Key Design Principles

1. **Premium Feel:** High-quality UI, smooth animations, professional aesthetics
2. **Choice-First:** Emphasize driver selection and profiles
3. **Trust & Safety:** Karma scores, ratings, verified drivers
4. **South Africa Focus:** ZAR currency, local languages, Durban locations

### Material Design 3

The app should follow **Material Design 3 (Material You)** principles:
- Modern component library
- Dynamic color theming
- Smooth animations
- Consistent spacing (8dp grid)
- Typography scale

### Typography

- **Headlines:** 32sp, 28sp, 24sp (bold)
- **Body:** 16sp, 14sp (regular)
- **Labels:** 14sp, 12sp (medium)
- **Font Family:** System default (San Francisco on iOS)

### Spacing System

- Based on 8dp grid: 4, 8, 12, 16, 20, 24, 32, 40, 48, 56, 64dp
- Component padding: 16-24dp
- Card corner radius: 16dp
- Button corner radius: 28dp

---

## 📋 Core Features

### 1. Authentication
- Phone number + OTP login
- Email + Password signup
- Facebook login (optional)
- Session management

### 2. Onboarding
- 3 intro slides explaining the app
- First-time user experience
- Permission requests (location, notifications)

### 3. Home Screen
- Google Maps integration
- Current location detection
- Nearby drivers display
- Pickup/destination input
- Bottom sheet with driver list

### 4. Driver Selection (Marketplace Model)
- **Key Differentiator:** Show individual drivers, not vehicle types
- Driver cards with:
  - Photo
  - Name
  - Rating (stars + review count)
  - Vehicle info (make, model, color)
  - Languages spoken
  - Amenities (WiFi, Child Seat, etc.)
  - Karma score (trust metric)
  - ETA
  - Fare estimate
- Filters: Price, Rating, ETA, Vehicle Type, Amenities, Languages
- Sort options: Price, Rating, Distance, Karma

### 5. Booking Flow
- Select driver
- Confirm pickup/destination
- Enter phone number
- Enter fare amount
- Submit booking request
- Track booking status (pending → accepted → in-progress → completed)

### 6. Ride Tracking
- Real-time driver location
- Route visualization
- ETA updates
- Chat with driver
- Cancel ride option
- SOS button (safety feature)

### 7. Payment
- Mobile money (M-Pesa, MTN Mobile Money)
- Crypto (USDC)
- Cash
- Wallet balance
- Payment history

### 8. History
- Past rides list
- Ride details
- Rating system (5-star + categories)
- Receipts

### 9. Profile & Settings
- User profile
- Wallet management
- Notifications settings
- Language preferences
- Invite friends (referral system)

---

## 🛠️ Technical Requirements

### Platform
- **Target:** iOS 14.0+ (iPhone and iPad)
- **Language:** Swift 5.0+
- **UI Framework:** SwiftUI (preferred) or UIKit
- **Minimum iOS Version:** iOS 14.0
- **Target iOS Version:** Latest (iOS 17+)

### Key Integrations

1. **Maps:**
   - Apple Maps (native) or Google Maps SDK
   - Location services
   - Geocoding
   - Directions/routing

2. **Backend API:**
   - Base URL: Configurable (currently `http://10.0.2.2:4205` for dev)
   - Authentication: JWT tokens
   - Endpoints: See `ride-scheduler-be/` for API documentation

3. **Push Notifications:**
   - Apple Push Notification Service (APNs)
   - Firebase Cloud Messaging (optional)

4. **Payments:**
   - Mobile money integration
   - Crypto wallet integration
   - OASIS Wallet API (future)

5. **Offline Support:**
   - Local storage (Core Data or SQLite)
   - Queue ride requests when offline
   - Sync when connection restored

### Architecture Recommendations

**Option 1: SwiftUI (Recommended)**
- Modern, declarative UI
- Native iOS feel
- Easier to maintain
- Better for iOS 14+

**Option 2: UIKit**
- More control
- Better for complex custom UI
- More verbose

**Architecture Pattern:**
- MVVM (Model-View-ViewModel)
- Combine framework for reactive programming
- URLSession for networking
- Swift Package Manager for dependencies

---

## 📂 Project Structure

```
TimoRides-iOS/
├── TimoRides/
│   ├── App/
│   │   ├── App.swift
│   │   └── ContentView.swift
│   ├── Screens/
│   │   ├── Splash/
│   │   ├── Onboarding/
│   │   ├── Authentication/
│   │   ├── Home/
│   │   ├── DriverSelection/
│   │   ├── Booking/
│   │   ├── History/
│   │   └── Settings/
│   ├── Components/
│   │   ├── DriverCard/
│   │   ├── BottomSheet/
│   │   ├── MapView/
│   │   └── CustomButtons/
│   ├── Models/
│   │   ├── User.swift
│   │   ├── Driver.swift
│   │   ├── Ride.swift
│   │   └── Payment.swift
│   ├── Services/
│   │   ├── APIService.swift
│   │   ├── LocationService.swift
│   │   ├── AuthService.swift
│   │   └── PaymentService.swift
│   ├── ViewModels/
│   ├── Utils/
│   │   ├── Theme.swift
│   │   ├── Colors.swift
│   │   └── Extensions.swift
│   └── Resources/
│       ├── Assets.xcassets/
│       ├── Colors.xcassets/
│       └── Localizable.strings
└── TimoRidesTests/
```

---

## 🎯 Recommended Approach

### Phase 1: Foundation (Week 1-2)
1. Set up Xcode project
2. Configure SwiftUI structure
3. Implement theme system (colors, typography)
4. Create base navigation
5. Set up API service layer

### Phase 2: Core Screens (Week 3-4)
1. Splash screen with logo animation
2. Onboarding slides (3 screens)
3. Login/Signup screens
4. Home screen with map placeholder

### Phase 3: Driver Selection (Week 5-6)
1. Driver list/cards UI
2. Driver profile view
3. Filters and sorting
4. Integration with backend API

### Phase 4: Booking Flow (Week 7-8)
1. Booking request screen
2. Status tracking
3. Real-time updates
4. Chat functionality

### Phase 5: Additional Features (Week 9-10)
1. Payment integration
2. History screen
3. Settings screen
4. Profile management

### Phase 6: Polish & Testing (Week 11-12)
1. Animations and transitions
2. Error handling
3. Loading states
4. Testing on devices
5. App Store preparation

**Total Timeline:** 12 weeks to MVP

---

## 🔗 Key Resources

### Documentation
- **Android App:** `/Volumes/Storage/OASIS_CLEAN/TimoRides/Timo-Android-App/`
- **Web Mirror:** `/Volumes/Storage/OASIS_CLEAN/TimoRides/timo-android-web-mirror/`
- **Backend API:** `/Volumes/Storage/OASIS_CLEAN/TimoRides/ride-scheduler-be/`
- **Design Plans:**
  - `FRONTEND_DESIGN_RENOVATION_PLAN.md`
  - `MATERIAL_DESIGN_3_RENOVATION.md`
  - `IOS_DEVELOPMENT_OPTIONS.md`

### Design Assets
- **Logo:** `/Volumes/Storage/OASIS_CLEAN/TimoRides/TIMO LOGO.png`
- **Brand Colors:** See Design System section above

### API Endpoints (from backend)
- `POST /api/auth/login` - User login
- `POST /api/auth/register` - User registration
- `GET /api/cars/proximity` - Get nearby drivers
- `POST /api/bookings` - Create booking
- `GET /api/bookings/:id` - Get booking status
- `GET /api/bookings/history` - Get ride history
- `POST /api/payments` - Process payment

### External Services
- **Google Maps API Key:** (See `strings.xml` in Android app)
- **Backend URL:** Configurable (default: `http://10.0.2.2:4205` for simulator)

---

## 📝 Important Notes

### Key Differentiators from Standard Ride-Sharing Apps

1. **Marketplace Model:** Users choose drivers, not vehicle types
2. **Driver Profiles:** Full profiles with photos, ratings, languages, amenities
3. **Karma Scores:** Trust metric from OASIS integration
4. **Mobile Money:** Primary payment method (not just cards)
5. **Offline-First:** Queue requests when offline, sync when connected

### South Africa Specific

- **Currency:** ZAR (South African Rand) - format: R250.00
- **Languages:** English, Zulu, Xhosa, Afrikaans
- **Location:** Durban, South Africa (default coordinates: -29.8587, 31.0218)
- **Phone Format:** South African phone numbers
- **Mobile Money:** M-Pesa, MTN Mobile Money, Vodacom

### Technical Considerations

1. **Location Permissions:** Critical for app functionality
2. **Background Location:** Needed for ride tracking
3. **Push Notifications:** Required for booking updates
4. **Offline Storage:** Core Data for local caching
5. **Network Handling:** Retry logic, offline queue

---

## ✅ Success Criteria

### Functional Requirements
- [ ] App launches and displays splash screen
- [ ] User can complete onboarding
- [ ] User can login/signup
- [ ] Home screen shows map with user location
- [ ] Nearby drivers are displayed
- [ ] User can select a driver
- [ ] User can create a booking
- [ ] Booking status updates in real-time
- [ ] User can view ride history
- [ ] User can make payments
- [ ] App works offline (queues requests)

### Design Requirements
- [ ] TimoRides branding applied (colors, logo)
- [ ] Material Design 3 principles followed
- [ ] Smooth animations and transitions
- [ ] Responsive to different screen sizes
- [ ] Accessible (VoiceOver, Dynamic Type)

### Technical Requirements
- [ ] No crashes or critical bugs
- [ ] Fast load times (< 2 seconds)
- [ ] Smooth 60fps animations
- [ ] Proper error handling
- [ ] App Store compliant

---

## 🚀 Getting Started

### Prerequisites
1. **Mac with Xcode 15+** (required for iOS development)
2. **Apple Developer Account** ($99/year for App Store submission)
3. **iOS Device** (for testing, or use Simulator)
4. **Backend API Access** (for testing API calls)

### First Steps
1. Review this brief and all referenced documents
2. Explore the Android app to understand functionality
3. Review the web mirror for UI/UX patterns
4. Set up Xcode project
5. Implement theme system
6. Start with splash screen and onboarding

### Questions to Clarify
- Preferred UI framework (SwiftUI vs UIKit)?
- Specific iOS version target?
- Timeline constraints?
- Team size and expertise?
- Budget for third-party services (maps, analytics)?

---

## 📞 Contact & Support

### Project Context
- **Project Name:** TimoRides
- **Platform:** iOS (iPhone/iPad)
- **Target Market:** Durban, South Africa
- **Business Model:** Premium ride-hailing marketplace

### Key Files to Review
1. `Timo-Android-App/README.md` - Android app overview
2. `Timo-Android-App/FRONTEND_DESIGN_RENOVATION_PLAN.md` - Design system
3. `Timo-Android-App/MATERIAL_DESIGN_3_RENOVATION.md` - Material Design 3 guide
4. `IOS_DEVELOPMENT_OPTIONS.md` - Technical approach comparison
5. `timo-android-web-mirror/README.md` - Web mirror reference

---

## 🎯 Deliverables

### Phase 1 Deliverables
- [ ] Xcode project setup
- [ ] Theme system implementation
- [ ] Navigation structure
- [ ] API service layer

### Final Deliverables
- [ ] Complete iOS app (.ipa file)
- [ ] App Store listing materials
- [ ] Documentation
- [ ] Test flight build for beta testing
- [ ] Source code repository

---

**This brief provides all the context needed to start iOS development. Review the referenced documents and assets for detailed implementation guidance.**

**Last Updated:** January 2025  
**Version:** 1.0

