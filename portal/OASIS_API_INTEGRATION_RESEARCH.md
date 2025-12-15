# OASIS API Integration Research for Portal

## Executive Summary

This document outlines the OASIS API architecture and how to integrate it with the portal to populate real data. The research is based on analysis of:
- `zypherpunk-wallet-ui` - Reference implementation for avatar API usage
- `SmartContractGenerator` - Smart contract API structure
- Various OASIS API endpoints discovered across the codebase

---

## 1. Authentication Architecture

### 1.1 Avatar Authentication Flow

The OASIS platform uses JWT-based authentication with avatar management:

**Base URL**: `https://api.oasisweb4.one` (production) or `http://localhost:5004` (local)

#### Authentication Endpoints

```typescript
// Login
POST /api/avatar/authenticate
Body: {
  username: string,  // Can be username or email
  password: string
}

Response: {
  avatar: {
    avatarId: string,
    id: string,
    username: string,
    email: string,
    firstName?: string,
    lastName?: string,
    karma?: number,
    // ... other avatar fields
  },
  jwtToken: string,
  refreshToken?: string,
  expiresIn?: number
}

// Register
POST /api/avatar/register
Body: {
  username: string,
  email: string,
  password: string,
  confirmPassword: string,
  firstName?: string,
  lastName?: string,
  title?: string,
  avatarType: string,  // e.g., "User"
  acceptTerms: boolean,
  privacyMode?: boolean
}

// Get Avatar by ID
GET /api/avatar/{avatarId}

// Get Avatar by Username
GET /api/avatar/username/{username}
```

#### Token Management

- JWT tokens are stored in `localStorage` as `oasis_auth`
- Tokens should be included in `Authorization: Bearer {token}` header
- Token expiration: ~15 minutes (needs refresh mechanism)
- All authenticated API calls require the JWT token

**Reference Implementation** (`zypherpunk-wallet-ui/lib/avatarApi.ts`):
```typescript
class OASISAvatarAPI {
  async login(username: string, password: string): Promise<AvatarAuthResponse> {
    const response = await fetch('/api/authenticate', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ username, password, baseUrl: this.baseUrl })
    });
    
    const data = await response.json();
    return {
      avatar: normalizeAvatar(data.avatar),
      jwtToken: data.token,
      refreshToken: undefined,
      expiresIn: undefined
    };
  }
}
```

---

## 2. Wallet API

### 2.1 Wallet Endpoints

**Base Path**: `/api/wallet`

#### Load Wallets

```typescript
// By Avatar ID
GET /api/wallet/avatar/{avatarId}/wallets?providerType={providerType}

Response: {
  isError: boolean,
  result: {
    SolanaOASIS: Wallet[],
    EthereumOASIS: Wallet[],
    PolygonOASIS: Wallet[],
    ArbitrumOASIS: Wallet[],
    ZcashOASIS: Wallet[],
    AztecOASIS: Wallet[],
    MidenOASIS: Wallet[],
    StarknetOASIS: Wallet[],
    // ... other providers
  }
}

// By Username
GET /api/wallet/avatar/username/{username}/wallets?providerType={providerType}

// By Email
GET /api/wallet/avatar/email/{email}/wallets?providerType={providerType}
```

#### Wallet Operations

```typescript
// Get Default Wallet
GET /api/wallet/default_wallet_by_id/{avatarId}?providerType={providerType}

// Set Default Wallet
POST /api/wallet/set_default_wallet_by_id/{avatarId}/{walletId}?providerType={providerType}

// Get Wallet Balance
GET /api/wallet/balance/{walletId}?providerType={providerType}

// Send Token Transaction
POST /api/wallet/send_token
Body: {
  fromAvatarId: string,
  toAvatarId?: string,
  toWalletAddress?: string,
  amount: string,
  token: string,
  providerType: ProviderType,
  // ... other transaction params
}

// Import Wallet
POST /api/wallet/import_wallet_private_key_by_id/{avatarId}
Body: {
  providerType: ProviderType,
  privateKey: string,
  // ... other params
}
```

#### Wallet Data Structure

```typescript
interface Wallet {
  id: string;
  avatarId: string;
  providerType: ProviderType;
  publicKey: string;
  privateKey?: string;  // Only for display/import, not stored
  address: string;
  balance?: number;
  isDefault?: boolean;
  createdAt?: string;
  updatedAt?: string;
}
```

**Reference Implementation** (`zypherpunk-wallet-ui/lib/api.ts`):
```typescript
class OASISWalletAPI {
  setAuthToken(token: string | null) {
    this.authToken = token || undefined;
  }

  private mergeHeaders(optionsHeaders?: HeadersInit): Headers {
    const headers = new Headers({
      'Content-Type': 'application/json',
      'Accept': 'application/json',
    });
    
    if (this.authToken) {
      headers.set('Authorization', `Bearer ${this.authToken}`);
    }
    
    return headers;
  }

  async loadWalletsById(avatarId: string, providerType?: ProviderType) {
    const params = providerType ? `?providerType=${providerType}` : '';
    return this.request(`avatar/${avatarId}/wallets${params}`);
  }
}
```

---

## 3. NFT API

### 3.1 NFT Endpoints

**Base Path**: `/api/nft` or `/api/Nft` (case may vary)

#### NFT Operations

```typescript
// Mint NFT (Solana example)
POST /api/Solana/Mint
Body: {
  mintWalletAddress: string,
  mintedByAvatarId: string,
  title: string,
  symbol: string,
  jsonUrl: string,  // IPFS metadata URL
  imageUrl: string,
  thumbnailUrl?: string,
  price: number,
  numberToMint: number,
  storeNFTMetaDataOnChain: boolean,
  memoText?: string,
  sendToAddressAfterMinting?: string,
  waitTillNFTSent?: boolean
}

Response: {
  isError: boolean,
  message: string,
  result: {
    oasisnft: {
      id: string,
      nftTokenAddress: string,
      mintTransactionHash: string,
      sendNFTTransactionHash?: string,
      mintedByAvatarId: string,
      mintedOn: string,
      title: string,
      description: string,
      imageUrl: string,
      jsonMetaDataURL: string,
      price: number,
      // ... other NFT fields
    }
  }
}

// Send/Transfer NFT
POST /api/Nft/send-nft
Body: {
  fromWalletAddress: string,
  toWalletAddress: string,
  nftId: string,
  tokenAddress: string,
  amount: number,
  providerType: ProviderType
}

// Get NFTs by Avatar
GET /api/nft/avatar/{avatarId}/nfts?providerType={providerType}

// Get NFT by ID
GET /api/nft/{nftId}
```

#### NFT Data Structure

```typescript
interface NFT {
  id: string;
  avatarId: string;
  providerType: ProviderType;
  nftTokenAddress: string;
  mintTransactionHash: string;
  title: string;
  description: string;
  imageUrl: string;
  jsonMetaDataURL: string;
  price: number;
  mintedOn: string;
  mintedByAvatarId: string;
  // ... other NFT fields
}
```

---

## 4. Smart Contract API

### 4.1 Smart Contract Generator API

**Base Path**: `/api/contracts` (from SmartContractGenerator)

#### Contract Operations

```typescript
// Generate Contract
POST /api/contracts/generate
Content-Type: multipart/form-data
Body: {
  jsonFile: File,  // JSON schema file
  language: SmartContractLanguage  // Rust, Solidity, etc.
}

Response: File download (generated contract source)

// Compile Contract
POST /api/contracts/compile
Content-Type: multipart/form-data
Body: {
  source: File,  // Contract source file
  language: SmartContractLanguage
}

Response: File download (ZIP with compiled artifacts)

// Deploy Contract
POST /api/contracts/deploy
Content-Type: multipart/form-data
Body: {
  compiledContractFile: File,
  walletKeypair?: File,  // For Solana
  schema?: File,  // For other chains
  language: SmartContractLanguage
}

Response: {
  isError: boolean,
  result: {
    contractAddress: string,
    transactionHash: string,
    // ... deployment details
  }
}

// Get Cache Stats
GET /api/contracts/cache-stats
```

**Reference Implementation** (`SmartContractGenerator/src/SmartContractGen/ScGen.API/Infrastructure/Controllers/V1/ContractGeneratorController.cs`):
```csharp
[HttpPost("generate")]
public async Task<IActionResult> ContractGenerateAsync([FromForm] GenerateContractRequest request)

[HttpPost("compile")]
public async Task<IActionResult> ContractCompileAsync([FromForm] CompileContractRequest request)

[HttpPost("deploy")]
public async Task<IActionResult> ContractDeployAsync([FromForm] DeployContractRequest request)
```

---

## 5. Data Holons API

### 5.1 Data Holon Endpoints

**Base Path**: `/api/data`

#### Holon Operations

```typescript
// Load Holon Data
GET /api/data/holon/{holonId}

Response: {
  isError: boolean,
  result: {
    id: string,
    holonType: string,
    data: any,  // JSON data
    // ... other holon fields
  }
}

// Save Holon Data
POST /api/data/holon
Body: {
  holonType: string,
  data: any,
  avatarId: string,
  // ... other fields
}

// Get Holons by Avatar
GET /api/data/avatar/{avatarId}/holons?holonType={holonType}
```

**Reference Implementation** (`meta-bricks-main/src/app/components/metabricks-wallet/oasis-wallet.service.ts`):
```typescript
async loadHolonData(holonId: string): Promise<OASISWalletResult<any>> {
  const response = await this.http.get(
    `${this.baseUrl}/api/data/holon/${holonId}`,
    this.getHeaders()
  );
  return response;
}
```

---

## 6. Transaction History API

### 6.1 Transaction Endpoints

```typescript
// Get Transactions by Avatar
GET /api/transaction/avatar/{avatarId}/transactions?providerType={providerType}&limit={limit}

Response: {
  isError: boolean,
  result: Transaction[]
}

// Get Transaction by Hash
GET /api/transaction/{transactionHash}?providerType={providerType}
```

#### Transaction Data Structure

```typescript
interface Transaction {
  id: string;
  avatarId: string;
  providerType: ProviderType;
  transactionHash: string;
  fromAddress: string;
  toAddress: string;
  amount: string;
  token: string;
  status: 'pending' | 'confirmed' | 'failed';
  timestamp: string;
  blockNumber?: number;
  gasUsed?: number;
  // ... other transaction fields
}
```

---

## 7. Bridge API

### 7.1 Bridge Endpoints

```typescript
// Get Bridge Transactions
GET /api/bridge/avatar/{avatarId}/transactions

// Initiate Bridge Transaction
POST /api/bridge/transfer
Body: {
  fromChain: ProviderType,
  toChain: ProviderType,
  fromAddress: string,
  toAddress: string,
  amount: string,
  token: string,
  avatarId: string
}
```

---

## 8. OAPP (OASIS Application) API

### 8.1 OAPP Endpoints

```typescript
// Get Installed OAPPs
GET /api/oapp/avatar/{avatarId}/installed

// Install OAPP
POST /api/oapp/install
Body: {
  avatarId: string,
  oappId: string,
  oappVersion?: string
}

// Uninstall OAPP
POST /api/oapp/uninstall
Body: {
  avatarId: string,
  oappId: string
}
```

---

## 9. Portal Integration Strategy

### 9.1 Authentication Integration

**Current Portal State**: Has login modal but uses demo data

**Integration Steps**:

1. **Create API Client** (`portal/api/oasisApi.js`):
```javascript
const OASIS_API_URL = 'https://api.oasisweb4.one';

class OASISAPI {
  constructor() {
    this.baseUrl = OASIS_API_URL;
    this.token = null;
  }

  setToken(token) {
    this.token = token;
    localStorage.setItem('oasis_auth', JSON.stringify({
      token,
      timestamp: Date.now()
    }));
  }

  getToken() {
    if (!this.token) {
      const stored = localStorage.getItem('oasis_auth');
      if (stored) {
        const auth = JSON.parse(stored);
        this.token = auth.token;
      }
    }
    return this.token;
  }

  async request(endpoint, options = {}) {
    const url = `${this.baseUrl}${endpoint}`;
    const headers = {
      'Content-Type': 'application/json',
      'Accept': 'application/json',
      ...options.headers
    };

    if (this.getToken()) {
      headers['Authorization'] = `Bearer ${this.getToken()}`;
    }

    const response = await fetch(url, {
      ...options,
      headers
    });

    if (!response.ok) {
      throw new Error(`API error: ${response.status}`);
    }

    return response.json();
  }

  // Avatar API
  async login(username, password) {
    const response = await this.request('/api/avatar/authenticate', {
      method: 'POST',
      body: JSON.stringify({ username, password })
    });
    
    if (response.isError) {
      throw new Error(response.message);
    }

    const token = response.result?.jwtToken || response.jwtToken || response.token;
    const avatar = response.result?.avatar || response.avatar || response.result;
    
    this.setToken(token);
    
    return { avatar, token };
  }

  async register(data) {
    const response = await this.request('/api/avatar/register', {
      method: 'POST',
      body: JSON.stringify(data)
    });
    
    if (response.isError) {
      throw new Error(response.message);
    }

    const token = response.result?.jwtToken || response.jwtToken;
    const avatar = response.result?.avatar || response.avatar;
    
    this.setToken(token);
    
    return { avatar, token };
  }

  async getAvatar(avatarId) {
    return this.request(`/api/avatar/${avatarId}`);
  }

  // Wallet API
  async getWallets(avatarId, providerType) {
    const params = providerType ? `?providerType=${providerType}` : '';
    const response = await this.request(`/api/wallet/avatar/${avatarId}/wallets${params}`);
    return response.result || {};
  }

  async getWalletBalance(walletId, providerType) {
    return this.request(`/api/wallet/balance/${walletId}?providerType=${providerType}`);
  }

  // NFT API
  async getNFTs(avatarId, providerType) {
    const params = providerType ? `?providerType=${providerType}` : '';
    const response = await this.request(`/api/nft/avatar/${avatarId}/nfts${params}`);
    return response.result || [];
  }

  // Transaction API
  async getTransactions(avatarId, providerType, limit = 10) {
    const params = new URLSearchParams({
      limit: limit.toString()
    });
    if (providerType) params.append('providerType', providerType);
    
    const response = await this.request(`/api/transaction/avatar/${avatarId}/transactions?${params}`);
    return response.result || [];
  }

  // Data Holons API
  async getHolons(avatarId, holonType) {
    const params = holonType ? `?holonType=${holonType}` : '';
    const response = await this.request(`/api/data/avatar/${avatarId}/holons${params}`);
    return response.result || [];
  }

  // OAPP API
  async getInstalledOAPPs(avatarId) {
    const response = await this.request(`/api/oapp/avatar/${avatarId}/installed`);
    return response.result || [];
  }
}

const oasisAPI = new OASISAPI();
```

### 9.2 Portal Data Loading

**Update `portal.html` script section**:

```javascript
// Replace loadPortalData() function
async function loadPortalData() {
    const authData = localStorage.getItem('oasis_auth');
    if (authData) {
        try {
            const auth = JSON.parse(authData);
            const avatar = auth.avatar;
            const token = auth.token;
            
            if (avatar && token) {
                oasisAPI.setToken(token);
                
                // Update header
                const initials = (avatar.firstName?.[0] || '') + (avatar.lastName?.[0] || '') || avatar.username?.[0]?.toUpperCase() || 'OA';
                document.getElementById('avatarInitials').textContent = initials;
                document.getElementById('avatarName').textContent = avatar.firstName && avatar.lastName 
                    ? `${avatar.firstName} ${avatar.lastName}` 
                    : avatar.username || 'OASIS User';
                document.getElementById('avatarId').textContent = avatar.avatarId || avatar.id || '-';
                
                // Load real data
                await loadDashboardData(avatar.id || avatar.avatarId);
            }
        } catch (e) {
            console.error('Error loading portal data:', e);
            loadSampleData(); // Fallback to demo data
        }
    } else {
        loadSampleData();
    }
}

async function loadDashboardData(avatarId) {
    try {
        // Load wallets
        const walletsResult = await oasisAPI.getWallets(avatarId);
        const allWallets = Object.values(walletsResult).flat();
        document.getElementById('statWallets').textContent = allWallets.length;
        
        // Calculate portfolio value (simplified - would need price API)
        let totalValue = 0;
        for (const wallet of allWallets) {
            if (wallet.id) {
                try {
                    const balanceResult = await oasisAPI.getWalletBalance(wallet.id, wallet.providerType);
                    totalValue += balanceResult.result || 0;
                } catch (e) {
                    console.error('Error loading balance:', e);
                }
            }
        }
        document.getElementById('statPortfolio').textContent = `$${totalValue.toFixed(2)}`;
        
        // Load karma
        const avatar = await oasisAPI.getAvatar(avatarId);
        if (avatar.result?.karma) {
            document.getElementById('statKarma').textContent = avatar.result.karma;
        }
        
        // Load NFTs
        const nftsResult = await oasisAPI.getNFTs(avatarId);
        const allNFTs = Array.isArray(nftsResult) ? nftsResult : Object.values(nftsResult).flat();
        document.getElementById('statNFTs').textContent = allNFTs.length;
        
        // Load transactions
        const transactions = await oasisAPI.getTransactions(avatarId, null, 10);
        displayTransactions(transactions);
        
        // Load OAPPs
        const oapps = await oasisAPI.getInstalledOAPPs(avatarId);
        displayOAPPs(oapps);
        
        // Load holons
        const holons = await oasisAPI.getHolons(avatarId);
        document.getElementById('statHolons').textContent = holons.length;
        
    } catch (error) {
        console.error('Error loading dashboard data:', error);
        // Show error message to user
    }
}

function displayTransactions(transactions) {
    const transactionList = document.getElementById('transactionList');
    if (transactions.length === 0) {
        transactionList.innerHTML = `
            <div class="empty-state">
                <p class="empty-state-text">No transactions yet</p>
            </div>
        `;
        return;
    }
    
    transactionList.innerHTML = transactions.map(tx => `
        <div class="transaction-item">
            <div class="transaction-info">
                <div class="transaction-type">${tx.token || 'Transaction'}</div>
                <div class="transaction-details">${tx.transactionHash?.substring(0, 10)}...${tx.transactionHash?.substring(tx.transactionHash.length - 8)}</div>
                <div class="transaction-time">${new Date(tx.timestamp).toLocaleString()}</div>
            </div>
            <div class="transaction-amount">${tx.amount} ${tx.token || ''}</div>
        </div>
    `).join('');
}

function displayOAPPs(oapps) {
    const oappList = document.getElementById('oappList');
    if (oapps.length === 0) {
        oappList.innerHTML = `
            <div class="empty-state">
                <p class="empty-state-text">No OAPPs installed. <a href="#" class="empty-state-link" onclick="switchTab('developer'); return false;">Browse OAPPs</a></p>
            </div>
        `;
        return;
    }
    
    oappList.innerHTML = oapps.map(oapp => `
        <div class="oapp-item">
            <div class="oapp-info">
                <div class="oapp-name">${oapp.name || oapp.id}</div>
                <div class="oapp-desc">${oapp.description || 'OASIS Application'}</div>
            </div>
            <div class="oapp-status">${oapp.status || 'Active'}</div>
        </div>
    `).join('');
}
```

### 9.3 Update Login Handler

**Update `handleAuthSubmit` in portal.html**:

```javascript
async function handleAuthSubmit(event) {
    event.preventDefault();
    const errorDiv = document.getElementById('authError');
    const submitBtn = document.getElementById('authSubmitBtn');
    const submitText = document.getElementById('authSubmitText');
    
    errorDiv.style.display = 'none';
    submitBtn.disabled = true;
    submitText.textContent = currentAuthMode === 'login' ? 'Signing in...' : 'Creating avatar...';
    
    try {
        const username = document.getElementById('username').value.trim();
        const password = document.getElementById('password').value;
        
        let authData;
        
        if (currentAuthMode === 'login') {
            authData = await oasisAPI.login(username, password);
        } else {
            const email = document.getElementById('email').value.trim();
            const firstName = document.getElementById('firstName').value.trim();
            const lastName = document.getElementById('lastName').value.trim();
            
            authData = await oasisAPI.register({
                username: username,
                email: email,
                password: password,
                confirmPassword: password,
                firstName: firstName || undefined,
                lastName: lastName || undefined,
                avatarType: 'User',
                acceptTerms: true
            });
        }
        
        // Store auth data
        localStorage.setItem('oasis_auth', JSON.stringify({
            avatar: authData.avatar,
            token: authData.token,
            timestamp: Date.now()
        }));
        
        // Close modal and reload portal data
        closeLoginModal();
        await loadPortalData();
        
    } catch (error) {
        errorDiv.textContent = error.message || 'An error occurred. Please try again.';
        errorDiv.style.display = 'block';
    } finally {
        submitBtn.disabled = false;
        submitText.textContent = currentAuthMode === 'login' ? 'Sign in' : 'Create avatar';
    }
}
```

---

## 10. Provider Types

The OASIS platform supports multiple blockchain providers:

```typescript
enum ProviderType {
  SolanaOASIS = 'SolanaOASIS',
  EthereumOASIS = 'EthereumOASIS',
  PolygonOASIS = 'PolygonOASIS',
  ArbitrumOASIS = 'ArbitrumOASIS',
  ZcashOASIS = 'ZcashOASIS',
  AztecOASIS = 'AztecOASIS',
  MidenOASIS = 'MidenOASIS',
  StarknetOASIS = 'StarknetOASIS',
  // ... other providers
}
```

---

## 11. Error Handling

### 11.1 Common Error Patterns

```typescript
interface OASISResult<T> {
  isError: boolean;
  isWarning?: boolean;
  message: string;
  result?: T;
  detailedMessage?: string;
}
```

### 11.2 Error Handling Strategy

```javascript
async function safeAPICall(apiCall) {
    try {
        const response = await apiCall();
        if (response.isError) {
            throw new Error(response.message || 'API error');
        }
        return response.result || response;
    } catch (error) {
        console.error('API call failed:', error);
        throw error;
    }
}
```

---

## 12. CORS and Proxy Considerations

### 12.1 Development Proxy

For local development, the portal may need a proxy to avoid CORS issues:

**Option 1**: Use Next.js API routes (like zypherpunk-wallet-ui)
**Option 2**: Use Python/Node proxy server
**Option 3**: Configure CORS on OASIS API server

### 12.2 Production Setup

In production, ensure:
- CORS headers are properly configured on OASIS API
- HTTPS is used for all API calls
- Token storage is secure (consider httpOnly cookies)

---

## 13. Implementation Checklist

### Phase 1: Authentication
- [ ] Create `portal/api/oasisApi.js` with API client
- [ ] Update login modal to use real API
- [ ] Implement token storage and management
- [ ] Add error handling for auth failures

### Phase 2: Dashboard Data
- [ ] Load wallet statistics
- [ ] Load portfolio value (requires price API integration)
- [ ] Load karma points
- [ ] Load NFT count
- [ ] Load transaction history
- [ ] Load OAPP list
- [ ] Load holon count

### Phase 3: Tab Implementations
- [ ] Wallets tab - display all wallets with balances
- [ ] NFTs tab - display NFT collections
- [ ] Smart Contracts tab - integrate with SmartContractGenerator API
- [ ] Data tab - display holons
- [ ] Bridges tab - display bridge transactions
- [ ] Developer tab - OAPP management
- [ ] Settings tab - avatar profile management

### Phase 4: Real-time Updates
- [ ] Implement polling for balance updates
- [ ] Add WebSocket support for real-time transaction updates (if available)
- [ ] Cache management for API responses

---

## 14. Testing Credentials

For testing purposes:

```
Username: metabricks_admin
Password: Uppermall1!
```

**Note**: These credentials are for development/testing only.

---

## 15. API Base URLs

### Production
- **OASIS API**: `https://api.oasisweb4.one`
- **Alternative**: `https://api.oasisweb4.one`

### Development
- **Local OASIS API**: `http://localhost:5004` or `https://localhost:5002`
- **SmartContractGenerator**: `http://localhost:5000` (if running locally)

---

## 16. Next Steps

1. **Create API Client Module**: Implement `portal/api/oasisApi.js` based on the structure above
2. **Update Portal HTML**: Integrate API calls into existing portal functions
3. **Test Authentication**: Verify login/register flow works
4. **Populate Dashboard**: Load real data for all dashboard sections
5. **Implement Tabs**: Build out each tab with real API integration
6. **Add Error Handling**: Implement comprehensive error handling and user feedback
7. **Optimize Performance**: Add caching and request batching
8. **Add Loading States**: Show loading indicators during API calls

---

## 17. References

- **zypherpunk-wallet-ui**: `/Volumes/Storage/OASIS_CLEAN/zypherpunk-wallet-ui`
  - `lib/avatarApi.ts` - Avatar authentication
  - `lib/api.ts` - Wallet API client
  - `AUTHENTICATION_WALLET_API_GUIDE.md` - Detailed guide

- **SmartContractGenerator**: `/Volumes/Storage/OASIS_CLEAN/SmartContractGenerator`
  - `src/SmartContractGen/ScGen.API/Infrastructure/Controllers/V1/ContractGeneratorController.cs`

- **Portal**: `/Volumes/Storage/OASIS_CLEAN/portal`
  - `portal.html` - Main portal interface

---

## 18. Notes

- The OASIS API uses a consistent response structure with `isError`, `message`, and `result` fields
- Some endpoints may have nested `result.result` structures - handle both cases
- JWT tokens expire after ~15 minutes - implement refresh mechanism
- Provider types are case-sensitive (e.g., `SolanaOASIS` not `solanaOASIS`)
- Some endpoints support both `avatarId` and `username`/`email` identifiers
- Always check `isError` flag before using `result` field
- Use proper error handling and user feedback for all API calls
