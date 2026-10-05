# Zypherpunk Privacy Wallet UI - Implementation Plan

## 🎯 Overview

This document outlines the plan to create a Zypherpunk-themed privacy-focused wallet UI, either by adapting the existing `oasis-wallet-ui` or building a new themed version.

## ✅ Assessment: Can We Use Existing Wallet UI?

**YES!** The existing `oasis-wallet-ui` is an excellent foundation because:

1. ✅ **Already has Zcash & Aztec support** - Both `ZcashOASIS` and `AztecOASIS` are in the `ProviderType` enum
2. ✅ **Bridge integration exists** - `BridgeSwapModal` component already handles cross-chain swaps
3. ✅ **Security features** - JWT auth, encrypted private keys, read-only mode
4. ✅ **Mobile-first design** - Perfect for privacy-focused UX
5. ✅ **OASIS API integration** - Full integration with wallet APIs
6. ✅ **Modern stack** - Next.js 14, TypeScript, Tailwind CSS

**Recommendation:** Adapt the existing UI with Zypherpunk theme and privacy enhancements rather than building from scratch.

---

## 🎨 Zypherpunk Theme Design

### Color Palette
```css
/* Dark, privacy-focused theme */
--zypherpunk-bg: #0a0a0a;           /* Deep black */
--zypherpunk-surface: #1a1a1a;       /* Dark gray */
--zypherpunk-border: #2a2a2a;       /* Subtle borders */
--zypherpunk-primary: #00ff88;      /* Cyberpunk green */
--zypherpunk-secondary: #ff0080;    /* Neon pink */
--zypherpunk-accent: #00d4ff;       /* Cyan accent */
--zypherpunk-warning: #ffaa00;      /* Amber warnings */
--zypherpunk-text: #ffffff;          /* White text */
--zypherpunk-text-muted: #888888;   /* Muted text */
```

### Visual Elements
- **Glitch effects** on privacy indicators
- **Neon borders** on active privacy features
- **Animated gradients** for shielded balances
- **Minimalist design** with focus on privacy
- **Dark mode only** (privacy-first)

---

## 🔐 Privacy Features to Add

### 1. Shielded Transaction UI
**New Component:** `ShieldedSendScreen.tsx`

```typescript
interface ShieldedSendScreenProps {
  wallet: Wallet;
  onBack: () => void;
  onSuccess?: () => void;
}

// Features:
// - Shielded address input (Zcash z-addresses)
// - Memo field (encrypted)
// - Privacy level selector (low/medium/high)
// - Partial notes option
// - Viewing key generation toggle
// - Privacy indicator (shield icon)
```

**Key Features:**
- Visual distinction between transparent and shielded addresses
- Privacy level selector (affects partial notes)
- Memo encryption indicator
- Shielded transaction confirmation screen

### 2. Viewing Key Management
**New Component:** `ViewingKeyManager.tsx`

```typescript
interface ViewingKey {
  id: string;
  address: string;
  key: string; // Encrypted
  purpose: 'audit' | 'compliance' | 'personal';
  createdAt: string;
  lastUsed?: string;
}

// Features:
// - Generate viewing keys
// - View encrypted keys (never show full key)
// - Export for auditors
// - Revoke viewing keys
// - Usage history
```

**Security:**
- Viewing keys stored encrypted in holons
- Never display full key in UI
- Export requires biometric/auth
- Audit trail for key usage

### 3. Privacy Dashboard
**New Component:** `PrivacyDashboard.tsx`

```typescript
interface PrivacyMetrics {
  shieldedBalance: number;
  transparentBalance: number;
  privacyScore: number; // 0-100
  recentShieldedTxs: number;
  viewingKeysActive: number;
  privacyLevel: 'low' | 'medium' | 'high' | 'maximum';
}

// Features:
// - Privacy score visualization
// - Shielded vs transparent balance breakdown
// - Privacy recommendations
// - Recent privacy activity
// - Privacy settings quick access
```

### 4. Private Bridge Interface
**Enhanced Component:** `PrivateBridgeModal.tsx` (extends `BridgeSwapModal`)

**New Features:**
- Privacy mode toggle (shielded bridge)
- Viewing key generation for bridge transactions
- Partial notes option
- Bridge privacy indicator
- Cross-chain privacy status

### 5. Wallet Hiding Features
**New Component:** `WalletHidingSettings.tsx`

```typescript
interface WalletHidingOptions {
  hideBalances: boolean;
  hideTransactionHistory: boolean;
  hideWalletAddresses: boolean;
  useTor: boolean; // Future: Tor integration
  privacyLevel: 'standard' | 'enhanced' | 'maximum';
}

// Features:
// - Toggle balance visibility
// - Hide transaction details
// - Mask wallet addresses
// - Privacy mode presets
```

---

## 🏗️ Implementation Structure

### New Components to Create

```
zypherpunk-wallet-ui/
├── components/
│   ├── privacy/
│   │   ├── ShieldedSendScreen.tsx       # Shielded transactions
│   │   ├── ViewingKeyManager.tsx        # Viewing key management
│   │   ├── PrivacyDashboard.tsx         # Privacy metrics
│   │   ├── PrivacySettings.tsx          # Privacy configuration
│   │   ├── WalletHidingSettings.tsx      # Wallet hiding options
│   │   └── PrivacyIndicator.tsx         # Privacy status badge
│   ├── bridge/
│   │   ├── PrivateBridgeModal.tsx       # Enhanced bridge with privacy
│   │   └── BridgePrivacySettings.tsx    # Bridge privacy options
│   └── wallet/
│       ├── ShieldedWalletCard.tsx       # Shielded wallet display
│       └── PrivacyWalletHome.tsx        # Privacy-focused home screen
├── lib/
│   ├── privacy/
│   │   ├── viewingKey.ts                # Viewing key utilities
│   │   ├── shieldedTx.ts                # Shielded transaction helpers
│   │   └── privacyScore.ts              # Privacy score calculation
│   └── api/
│       └── privacyApi.ts                # Privacy-specific API calls
└── app/
    └── privacy/
        └── page.tsx                     # Privacy dashboard page
```

---

## 🔌 API Integration

### New API Endpoints Needed

```typescript
// Privacy-specific endpoints
POST /api/wallet/create_shielded_transaction
POST /api/wallet/generate_viewing_key
GET  /api/wallet/viewing_keys/{walletId}
POST /api/wallet/revoke_viewing_key
GET  /api/wallet/privacy_metrics/{avatarId}
POST /api/bridge/private_bridge_order  // Enhanced bridge with privacy
```

### Enhanced Existing Endpoints

```typescript
// Extend existing wallet API
GET /api/wallet/load_wallets_by_id/{avatarId}
  // Add: ?includePrivacy=true&includeViewingKeys=true

POST /api/wallet/send_token
  // Add: privacyMode, viewingKey, partialNotes options
```

---

## 🎨 UI/UX Enhancements

### 1. Privacy Indicators
- **Shield icon** next to shielded balances
- **Lock icon** for private transactions
- **Eye icon** for viewing key enabled
- **Glitch effect** when privacy is active

### 2. Visual Feedback
- **Animated shield** when creating shielded transaction
- **Progress indicator** for privacy operations
- **Success animations** for privacy actions
- **Warning indicators** for privacy risks

### 3. Privacy-First Navigation
- **Privacy dashboard** as main screen
- **Quick privacy toggle** in header
- **Privacy settings** easily accessible
- **Privacy tips** and education

---

## 🔒 Security Enhancements (OASIS API Features)

### 1. **JWT Authentication** ✅ Already Implemented
- All wallet endpoints protected with `[Authorize]` attribute
- JWT middleware validates tokens automatically
- Avatar context injected from JWT token
- Zero clock skew for precise token expiration

**Usage:**
```typescript
// Frontend automatically sends JWT in Authorization header
const response = await fetch('/api/wallet/send_token', {
  headers: {
    'Authorization': `Bearer ${jwtToken}`,
    'Content-Type': 'application/json'
  }
});
```

### 2. **AES-256 Encryption** ✅ Already Implemented
- Private keys encrypted using Rijndael AES-256
- Encryption key stored in `OASIS_DNA.json` (never in code)
- Selective decryption: keys only decrypted when `decryptPrivateKeys = true`
- Secret recovery phrases also encrypted

**Usage:**
```typescript
// Private keys are always encrypted in API responses
// Only decrypt when absolutely necessary (e.g., signing transactions)
const wallets = await loadWallets(avatarId, {
  decryptPrivateKeys: false  // Default: encrypted (privacy-first!)
});
```

### 3. **Secure Key Generation** ✅ Already Implemented
- Cryptographically secure random key generation
- Uses `Secp256K1Manager` for key generation
- WIF format for keys
- Provider-specific key prefixes

### 4. **Avatar Context Security** ✅ Already Implemented
- Authenticated avatar automatically available
- Request-scoped avatar context
- Type-safe avatar access
- Prevents cross-user data access

### 5. **Enhanced Key Management**
```typescript
// Never display full private keys
// Always encrypt before storage (handled by API)
// Use biometric auth for sensitive operations (frontend)
// Session timeout for privacy features (frontend)
```

### 6. **Privacy-First Defaults**
- Default to shielded transactions
- Auto-generate viewing keys for compliance
- Hide balances by default
- Privacy mode enabled by default
- **Never request decrypted private keys unless signing**

### 7. **Audit Trail**
- Log all privacy operations
- Track viewing key usage
- Monitor privacy score changes
- Compliance reporting
- All operations tied to authenticated avatar (automatic)

### 8. **Future Security Enhancements** (Planned by OASIS)
- 🔜 Full wallet encryption (additional layer)
- 🔜 Quantum encryption (third level of protection)

---

## 📱 Mobile-First Privacy Features

### 1. Biometric Authentication
- Face ID / Touch ID for sensitive operations
- Biometric auth for viewing key export
- Biometric auth for privacy settings

### 2. Privacy Quick Actions
- Swipe to hide/show balances
- Long press for privacy options
- Shake to enable privacy mode
- Quick privacy toggle in notification

### 3. Offline Privacy
- Viewing keys cached locally (encrypted)
- Privacy settings work offline
- Shielded transaction queue

---

## 🚀 Implementation Phases

### Phase 1: Theme & Base Privacy (Week 1)
- [ ] Apply Zypherpunk theme (colors, fonts, animations)
- [ ] Create `PrivacyIndicator` component
- [ ] Add privacy settings page
- [ ] Enhance existing wallet cards with privacy badges

### Phase 2: Shielded Transactions (Week 1-2)
- [ ] Create `ShieldedSendScreen` component
- [ ] Integrate with Zcash provider for shielded txs
- [ ] Add privacy level selector
- [ ] Implement partial notes UI

### Phase 3: Viewing Keys (Week 2)
- [ ] Create `ViewingKeyManager` component
- [ ] Integrate viewing key API endpoints
- [ ] Add viewing key generation flow
- [ ] Implement viewing key export (encrypted)

### Phase 4: Privacy Dashboard (Week 2)
- [ ] Create `PrivacyDashboard` component
- [ ] Implement privacy score calculation
- [ ] Add privacy metrics visualization
- [ ] Create privacy recommendations

### Phase 5: Private Bridge (Week 2-3)
- [ ] Enhance `BridgeSwapModal` with privacy features
- [ ] Add viewing key generation for bridges
- [ ] Implement private bridge flow
- [ ] Add bridge privacy indicators

### Phase 6: Wallet Hiding (Week 3)
- [ ] Create `WalletHidingSettings` component
- [ ] Implement balance hiding
- [ ] Add transaction history hiding
- [ ] Create privacy mode presets

### Phase 7: Polish & Testing (Week 3)
- [ ] Security audit
- [ ] Privacy testing
- [ ] UI/UX polish
- [ ] Documentation

---

## 🎯 Key Differentiators

### Privacy-First Design
- Every feature considers privacy
- Default to most private option
- Clear privacy indicators
- Privacy education built-in

### OASIS Integration
- Leverages OASIS wallet API security
- Uses holonic architecture for privacy data
- HyperDrive ensures privacy operations always work
- Provider abstraction for privacy chains

### User Experience
- Simple privacy controls
- Clear privacy feedback
- Educational privacy tips
- Progressive privacy enhancement

---

## 📊 Success Metrics

- ✅ All privacy features functional
- ✅ Privacy score calculation accurate
- ✅ Viewing keys properly encrypted
- ✅ Shielded transactions working
- ✅ Private bridge operational
- ✅ Security audit passed
- ✅ User testing positive

---

## 🔗 Integration Points

### OASIS Wallet API ✅ Security Features Available
- **JWT Authentication** - All endpoints already protected
- **AES-256 Encryption** - Private keys always encrypted
- **Selective Decryption** - Keys only decrypted when needed
- **Secure Key Generation** - Cryptographically secure
- **Avatar Context** - User isolation built-in
- **Authorization** - Avatar-based access control

**Key Integration Points:**
- Use existing `/api/wallet/load_wallets_by_id/{avatarId}` with `decryptPrivateKeys: false`
- Use existing `/api/wallet/send_token` with JWT authentication
- Extend with privacy-specific endpoints for viewing keys, shielded transactions
- Leverage existing encryption for viewing key storage

### OASIS Bridge API
- Extend bridge endpoints for privacy
- Add viewing key support
- Implement private bridge flow
- Use existing bridge infrastructure

### OASIS Provider System
- Use ZcashOASIS provider
- Use AztecOASIS provider
- Leverage provider abstraction
- Use HyperDrive for reliability

---

## 📝 Next Steps

1. **Review & Approve Plan** - Confirm approach and features
2. **Set Up Development Environment** - Clone and configure
3. **Create Theme** - Apply Zypherpunk colors and styles
4. **Build Privacy Components** - Start with shielded transactions
5. **Integrate APIs** - Connect to OASIS wallet APIs
6. **Test & Iterate** - Security testing and UX improvements

---

**Status:** Ready for Implementation  
**Estimated Time:** 3 weeks  
**Priority:** High (for Zypherpunk hackathon)

