# Wallet Integration Plan: Zypherpunk Wallet UI → OASIS Portal

## Overview
Integrate the Zypherpunk Wallet UI (Next.js/React) into the OASIS Portal (vanilla JS/HTML/CSS) while maintaining the portal's look and feel.

## Current State Analysis

### Portal Architecture
- **Tech Stack**: Vanilla JavaScript, HTML5, CSS3
- **Design System**: 
  - CSS Variables in `styles.css`
  - Dark theme (`--bg-primary: #0a0a0a`, `--text-primary: #ffffff`)
  - Portal-specific components: `portal-card`, `portal-section`, `portal-section-title`
  - Font: Inter (Google Fonts)
- **Module Pattern**: Each feature has its own JS file (e.g., `bridge.js`, `avatar-dashboard.js`)
- **Tab System**: `switchTab('wallets')` loads content into `#tab-wallets`
- **Current Wallet Section**: Empty placeholder at `#tab-wallets`

### Zypherpunk Wallet UI Architecture
- **Tech Stack**: Next.js 14, React 18, TypeScript
- **Styling**: Tailwind CSS with custom theme
- **State Management**: Zustand
- **Key Routes**: `/wallet`, `/privacy`, `/security`
- **Main Components**: 
  - `MobileWalletHome`
  - `StablecoinDashboard`
  - `CreateWalletScreen`
  - `SendScreen`, `ReceiveScreen`, etc.

## Integration Approaches

### Option 1: React Embedding (Recommended)
**Approach**: Build Next.js app as standalone bundle, embed React app in portal via iframe or React mount point.

**Pros**:
- Maintains full React functionality
- Preserves existing wallet UI codebase
- Easier to maintain two separate codebases

**Cons**:
- Requires React runtime (~150KB)
- Styling isolation challenges
- iframe approach has UX limitations

**Implementation**:
1. Build Next.js app with output: 'standalone'
2. Create React mount point in portal
3. Load React bundle and mount wallet components
4. Share authentication state via localStorage/API
5. Apply portal CSS overrides to match design system

### Option 2: Vanilla JS Rewrite
**Approach**: Rewrite wallet functionality in vanilla JS matching portal module patterns.

**Pros**:
- Perfect style integration
- No React dependency
- Consistent with portal architecture

**Cons**:
- Significant development time
- Duplicate codebase maintenance
- Loss of React ecosystem benefits

### Option 3: Hybrid Approach (Best Long-term)
**Approach**: Extract wallet core logic, create portal module using shared API client, rebuild UI in vanilla JS matching portal styles.

**Pros**:
- Best user experience
- Consistent design
- Reusable API integration
- Maintainable long-term

**Cons**:
- Initial development effort
- Requires API abstraction layer

## Recommended Implementation Plan (Option 3 - Hybrid)

### Phase 1: Preparation
1. ✅ Analyze portal design system (CSS variables, components)
2. ✅ Review wallet UI structure and identify core features
3. Extract API client logic from zypherpunk-wallet-ui
4. Create shared API utilities that both can use

### Phase 2: Portal Wallet Module Structure
```
portal/
├── wallet.js                 # Main wallet module (like bridge.js)
├── wallet.css                # Wallet-specific styles (optional, or use styles.css)
└── api/
    └── walletApi.js          # Shared wallet API client
```

### Phase 3: Feature Mapping

#### Core Features to Implement:
1. **Wallet List/Dashboard**
   - Multi-chain wallet display
   - Balance overview
   - Quick actions (Send, Receive, Swap)

2. **Wallet Creation**
   - Unified wallet creation
   - Individual chain wallet creation
   - Import existing wallets

3. **Wallet Management**
   - View wallet details
   - Transaction history
   - Address management

4. **Transactions**
   - Send (transparent/shielded)
   - Receive
   - Swap functionality

5. **Advanced Features** (Future)
   - Stablecoin (zUSD) minting/redeeming
   - Privacy bridge
   - Privacy drops

### Phase 4: Styling Integration

#### CSS Variable Mapping:
```css
/* Portal → Wallet UI equivalents */
--bg-primary → background: #0a0a0a
--bg-secondary → background: #111111
--text-primary → color: #ffffff
--text-secondary → color: #999999
--text-tertiary → color: #666666
--border-color → border: #333333
--accent → white buttons
--hover-bg → #1a1a1a
```

#### Component Classes to Use:
- `.portal-section` - Main container
- `.portal-section-title` - Page titles
- `.portal-section-subtitle` - Page subtitles
- `.portal-card` - Card containers
- `.portal-card-header` - Card headers
- `.btn-primary` - Primary buttons
- `.btn-secondary` - Secondary buttons

### Phase 5: API Integration

#### Shared API Client Pattern:
```javascript
// api/walletApi.js
const walletAPI = {
    // Wallet operations
    loadWallets: async (avatarId) => { ... },
    createWallet: async (avatarId, providerType) => { ... },
    sendTransaction: async (request) => { ... },
    
    // Balance operations
    getBalance: async (walletId) => { ... },
    
    // Transaction operations
    getTransactions: async (walletId) => { ... },
};
```

### Phase 6: Module Integration

#### wallet.js Structure:
```javascript
let walletState = {
    wallets: {},
    selectedWallet: null,
    loading: false,
    error: null
};

async function loadWallets() {
    const container = document.getElementById('tab-wallets');
    // Render wallet dashboard
    container.innerHTML = renderWalletDashboard();
    attachEventListeners();
}

function renderWalletDashboard() {
    return `
        <div class="portal-section">
            <h2 class="portal-section-title">Multi-Chain Wallets</h2>
            <p class="portal-section-subtitle">Manage your wallets across all supported blockchains</p>
            
            <div class="portal-card">
                <!-- Wallet list -->
            </div>
        </div>
    `;
}
```

#### Integration Point in portal.html:
```html
<div id="tab-wallets" class="portal-tab-content" style="display: none;">
    <div id="wallet-content">
        <!-- Content loaded by wallet.js -->
    </div>
</div>
```

#### Tab Switch Handler:
```javascript
// In portal.html switchTab function
if (tabName === 'wallets') {
    if (typeof loadWallets === 'function') {
        loadWallets();
    }
}
```

## File Changes Required

### New Files:
1. `portal/wallet.js` - Main wallet module
2. `portal/api/walletApi.js` - Wallet API client (can share with zypherpunk-ui)

### Modified Files:
1. `portal/portal.html` - Add wallet.js script, update tab-wallets content
2. `portal/styles.css` - Add wallet-specific styles if needed (or use existing classes)

## Design Guidelines

### Typography:
- Use Inter font (already loaded in portal)
- Section titles: `portal-section-title` class
- Subtitles: `portal-section-subtitle` class
- Body text: Default font styling

### Colors:
- Background: `var(--bg-primary)` (#0a0a0a)
- Cards: `var(--bg-secondary)` (#111111)
- Text: `var(--text-primary)` (#ffffff)
- Secondary text: `var(--text-secondary)` (#999999)
- Borders: `var(--border-color)` (#333333)

### Spacing:
- Follow portal's spacing patterns
- Use consistent padding/margins

### Components:
- Use existing portal button styles
- Match card styling with `.portal-card`
- Follow modal/dialog patterns from bridge.js

## Testing Plan

1. **Visual Consistency**: Ensure wallet UI matches portal styling
2. **Functionality**: Test all wallet operations (create, send, receive)
3. **Responsive Design**: Verify mobile/tablet compatibility
4. **Integration**: Test tab switching, state management
5. **API Integration**: Verify API calls work with portal auth

## Next Steps

1. ✅ Complete analysis (this document)
2. Create wallet.js module skeleton
3. Implement wallet list/dashboard
4. Add wallet creation flow
5. Integrate transaction functionality
6. Style matching and refinement
7. Testing and bug fixes

## Notes

- Portal uses localStorage for auth token (same as wallet UI)
- Portal uses `oasisAPI` client for API calls (can extend or create wallet-specific client)
- Portal modules follow similar patterns - use bridge.js as reference
