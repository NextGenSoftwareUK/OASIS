# OASIS Login Integration for Driver App

**Date:** January 2025  
**Status:** Implementation Ready

---

## 🎯 Overview

The driver app now supports **OASIS Avatar authentication** instead of Facebook login. This aligns with TimoRides' OASIS architecture and provides a unified identity system.

---

## 🔐 OASIS Authentication Options

### Option 1: OASIS Avatar Direct Login (Current Implementation)

**How it works:**
- Driver enters OASIS Avatar username/password
- App authenticates with OASIS API
- Receives JWT token and Avatar details
- Avatar ID becomes the driver's identity

**API Endpoints:**
- `POST /api/auth/login` - Authenticate with OASIS
- `GET /api/avatar` - Get Avatar details (with JWT token)
- `POST /api/avatar` - Create new Avatar (registration)

**Benefits:**
- ✅ Unified identity across OASIS ecosystem
- ✅ Karma/reputation system integration
- ✅ Wallet integration (OASIS Wallet)
- ✅ Cross-platform identity (works on all OASIS apps)

---

### Option 2: OASIS OAuth Flow (Future Enhancement)

**How it works:**
- Driver clicks "Continue with OASIS"
- Redirects to OASIS OAuth authorization page
- Driver authorizes TimoRides to access Avatar
- Redirects back with authorization code
- App exchanges code for JWT token

**Benefits:**
- ✅ More secure (no password handling in app)
- ✅ Standard OAuth 2.0 flow
- ✅ Better user experience
- ✅ Supports SSO across OASIS apps

---

### Option 3: OASIS Wallet Connect (Web3 Integration)

**How it works:**
- Driver connects their OASIS Wallet
- Wallet signature proves identity
- No password needed
- Web3-native authentication

**Benefits:**
- ✅ Web3/Web5 native
- ✅ Decentralized identity
- ✅ No password management
- ✅ Blockchain-verified identity

---

## 🔌 Current Implementation

### Service Layer

**File:** `src/services/api/oasis.js`

```javascript
// OASIS API service with:
- authenticate(username, password)
- getAvatar(token)
- createAvatar(avatarData)
```

### Login Screen Integration

**File:** `src/screens/Auth/LoginScreen.jsx`

- "Continue with OASIS" button replaces Facebook
- OASIS icon (AccountCircle)
- Styled with Timo blue colors
- Calls `oasisService.authenticate()`

---

## 📋 Integration Steps

### Step 1: Configure OASIS API URL

**Environment Variable:**
```bash
VITE_OASIS_API_URL=https://api.oasisplatform.io
```

Or set in `src/services/api/oasis.js`:
```javascript
const OASIS_API_BASE_URL = 'https://api.oasisplatform.io';
```

### Step 2: Update Authentication Flow

**Current Flow:**
1. User clicks "Continue with OASIS"
2. Calls `oasisService.authenticate(username, password)`
3. Receives JWT token and Avatar
4. Stores in localStorage
5. Updates Redux state
6. Navigates to home

**Production Flow (with OAuth):**
1. User clicks "Continue with OASIS"
2. Redirects to OASIS OAuth page
3. User authorizes
4. Redirects back with code
5. Exchange code for token
6. Get Avatar details
7. Store and navigate

---

## 🎨 UI/UX Design

### OASIS Login Button

- **Icon:** AccountCircle (OASIS Avatar icon)
- **Color:** Timo Blue (#2847bc)
- **Style:** Outlined button with gradient background
- **Hover:** Glow effect matching Timo brand
- **Text:** "Continue with OASIS"
- **Subtext:** "Login with your OASIS Avatar identity"

### Visual Design

Matches the futuristic design system:
- Glassmorphism effect
- Glowing border on hover
- Smooth animations
- Timo brand colors

---

## 🔗 OASIS Integration Benefits

### For Drivers

1. **Unified Identity:**
   - One Avatar across all OASIS apps
   - No need for separate accounts

2. **Karma System:**
   - Reputation follows them
   - Trust score visible
   - Better matching with riders

3. **OASIS Wallet:**
   - Integrated payments
   - Cross-chain support
   - Secure transactions

4. **Data Ownership:**
   - Driver owns their data
   - Portable identity
   - Privacy-focused

### For TimoRides

1. **Ecosystem Integration:**
   - Part of OASIS network
   - Access to OASIS services
   - Cross-app features

2. **Trust & Safety:**
   - Karma scores
   - Verified identities
   - Reputation system

3. **Payment Integration:**
   - OASIS Wallet support
   - Multi-chain payments
   - Lower fees

---

## 📝 Next Steps

### Immediate (Current)
- ✅ OASIS login button implemented
- ✅ Service layer created
- ✅ UI matches rider app design

### Short-term (Next Sprint)
- [ ] Connect to actual OASIS API
- [ ] Implement Avatar registration flow
- [ ] Add OASIS Avatar display in profile
- [ ] Show Karma score in driver profile

### Long-term (Future)
- [ ] OAuth flow implementation
- [ ] Wallet Connect integration
- [ ] Karma/reputation display
- [ ] OASIS Wallet integration for payments

---

## 🔧 Configuration

### Environment Variables

```bash
# OASIS API Configuration
VITE_OASIS_API_URL=https://api.oasisplatform.io
VITE_OASIS_CLIENT_ID=your-client-id
VITE_OASIS_REDIRECT_URI=http://localhost:3001/auth/oasis/callback
```

### API Endpoints

**Base URL:** `https://api.oasisplatform.io` (or your OASIS API URL)

**Endpoints:**
- `POST /api/auth/login` - Authenticate
- `GET /api/avatar` - Get Avatar (requires JWT)
- `POST /api/avatar` - Create Avatar

---

## 💡 Alternative Login Options

If OASIS isn't ready yet, other good options:

1. **Google Sign-In**
   - Widely used
   - Good UX
   - Easy integration

2. **Apple Sign-In**
   - Privacy-focused
   - iOS native
   - Good for iOS users

3. **Phone Number (OTP)**
   - Common in ride-hailing
   - No email needed
   - Fast verification

4. **Email/Password (Current)**
   - Traditional
   - Works everywhere
   - Full control

**Recommendation:** Keep OASIS as primary, with email/password as fallback.

---

## 📚 Related Documentation

- OASIS Avatar API: `/OASIS Architecture/`
- OASIS Wallet Integration: `/Providers/`
- TimoRides Backend: `/ride-scheduler-be/README.md`

---

**Status:** ✅ OASIS Login Button Implemented  
**Next:** Connect to actual OASIS API endpoint


