# Authentication Comparison: Working Projects vs Test Script

**Date:** January 2026  
**Purpose:** Understanding why authentication works in nft-mint-studio.js but fails in test_simple_nft_mint.py

---

## Key Finding: `oasisAPI.request()` Automatically Adds Auth Headers

The working projects (`nft-mint-studio.js`, `oportal-repo`) use a centralized `oasisAPI.request()` method that **automatically adds the Authorization header** from localStorage.

---

## How Working Projects Handle Authentication

### 1. Authentication Flow (nft-mint-studio.js)

```javascript
// Step 1: Authenticate
async function handleNFTAuthenticate() {
    const response = await oasisAPI.authenticateAvatar(username, password);
    
    // Extract token from nested response structure
    const token = response.result?.jwtToken || response.jwtToken || response.token;
    
    // Store in state AND localStorage
    nftMintStudioState.authToken = token;
    localStorage.setItem('oasis_auth', JSON.stringify({
        token: token,
        avatar: normalizedAvatar,
        timestamp: Date.now()
    }));
}
```

**Key Points:**
- Token is stored in **both** state (`nftMintStudioState.authToken`) **and** localStorage
- Response structure is normalized (handles nested `result.result.jwtToken`)

### 2. Provider Registration/Activation (nft-mint-studio.js)

```javascript
// Step 2: Register Provider
async function handleProviderRegister(providerId) {
    // Uses oasisAPI.request() which AUTOMATICALLY adds Authorization header
    const response = await oasisAPI.request(provider.registerEndpoint, { 
        method: 'POST' 
    });
    // provider.registerEndpoint = '/api/provider/register-provider-type/SolanaOASIS'
}

// Step 3: Activate Provider
async function handleProviderActivate(providerId) {
    // Uses oasisAPI.request() which AUTOMATICALLY adds Authorization header
    const response = await oasisAPI.request(provider.activateEndpoint, { 
        method: 'POST' 
    });
    // provider.activateEndpoint = '/api/provider/activate-provider/SolanaOASIS'
}
```

**Key Point:** No manual header setting needed - `oasisAPI.request()` handles it automatically.

### 3. NFT Minting (nft-mint-studio.js)

```javascript
// Step 4: Mint NFT
async function handleNFTMint() {
    const payload = { /* NFT data */ };
    
    // Uses oasisAPI.request() which AUTOMATICALLY adds Authorization header
    const response = await oasisAPI.request('/api/nft/mint-nft', {
        method: 'POST',
        body: JSON.stringify(payload)
    });
}
```

---

## How `oasisAPI.request()` Works (api/oasisApi.js)

```javascript
const oasisAPI = {
    /**
     * Get authentication headers
     * Automatically reads from localStorage or authStore
     */
    getAuthHeaders() {
        const headers = {
            'Content-Type': 'application/json'
        };

        // Try centralized authStore first
        if (typeof authStore !== 'undefined' && authStore.getAuthHeader()) {
            headers['Authorization'] = authStore.getAuthHeader();
            return headers;
        }

        // Fallback to localStorage
        try {
            const authData = localStorage.getItem('oasis_auth');
            if (authData) {
                const auth = JSON.parse(authData);
                if (auth.token) {
                    headers['Authorization'] = `Bearer ${auth.token}`;
                }
            }
        } catch (error) {
            console.error('Error getting auth headers:', error);
        }

        return headers;
    },

    /**
     * Generic request handler
     * AUTOMATICALLY adds Authorization header via getAuthHeaders()
     */
    async request(endpoint, options = {}) {
        const url = `${this.baseURL}${endpoint}`;
        const config = {
            ...options,
            headers: {
                ...this.getAuthHeaders(),  // <-- AUTOMATICALLY ADDS AUTH HEADER
                ...(options.headers || {})
            }
        };

        const response = await fetch(url, config);
        // ... handle response
    }
};
```

**Key Points:**
1. `getAuthHeaders()` automatically reads token from localStorage
2. `request()` automatically merges auth headers into every request
3. No manual `Authorization: Bearer {token}` needed in each call

---

## How Test Script Handles Authentication (test_simple_nft_mint.py)

```python
# Step 1: Authenticate
def authenticate():
    response = requests.post(
        f"{BASE_URL}/api/avatar/authenticate",
        json={"username": username, "password": password},
        verify=False,
        timeout=30
    )
    token = response.json()["result"]["jwtToken"]
    return token

# Step 2: Register Provider
def register_provider(provider_type="SolanaOASIS"):
    headers = {
        "Authorization": f"Bearer {token}",  # <-- MANUALLY SET
        "Content-Type": "application/json"
    }
    response = requests.post(
        f"{BASE_URL}/api/provider/register-provider-type/{provider_type}",
        headers=headers,
        verify=False,
        timeout=30
    )
```

**Key Points:**
- Token is manually extracted and stored in variable
- Headers are manually set for each request
- Should work, but might have issues with:
  - Token format/validation
  - Response structure parsing
  - Header format

---

## Differences That Could Cause Issues

### 1. Token Extraction

**Working (JavaScript):**
```javascript
const token = response.result?.jwtToken || response.jwtToken || response.token;
// Handles multiple response structures
```

**Test Script (Python):**
```python
token = response.json()["result"]["jwtToken"]
# Assumes specific structure - might fail if structure differs
```

### 2. Header Format

**Working (JavaScript):**
```javascript
headers['Authorization'] = `Bearer ${auth.token}`;
// Always uses "Bearer " prefix
```

**Test Script (Python):**
```python
headers = {"Authorization": f"Bearer {token}"}
# Should be correct, but verify token doesn't already include "Bearer "
```

### 3. Token Storage

**Working (JavaScript):**
- Token stored in localStorage
- Persists across page reloads
- Automatically retrieved by `getAuthHeaders()`

**Test Script (Python):**
- Token stored in variable
- Lost if script restarts
- Must manually pass to each function

---

## Potential Issues in Test Script

### Issue 1: Token Already Includes "Bearer "
```python
# If token already includes "Bearer ", this would be wrong:
headers = {"Authorization": f"Bearer {token}"}
# Would result in: "Bearer Bearer eyJhbGc..."

# Check if token already has "Bearer " prefix
if not token.startswith("Bearer "):
    auth_header = f"Bearer {token}"
else:
    auth_header = token
```

### Issue 2: Response Structure Mismatch
```python
# Current (assumes specific structure):
token = response.json()["result"]["jwtToken"]

# Should handle multiple structures (like JavaScript):
data = response.json()
token = (
    data.get("result", {}).get("jwtToken") or
    data.get("jwtToken") or
    data.get("token")
)
```

### Issue 3: Token Not Persisted Between Calls
```python
# If token is extracted but not properly stored:
token = authenticate()
# Later calls might use None or old token
```

---

## Recommended Fixes for Test Script

### Fix 1: Robust Token Extraction
```python
def extract_token(response):
    """Extract token from response, handling multiple structures"""
    data = response.json()
    
    # Try nested result structure first
    if "result" in data:
        result = data["result"]
        if isinstance(result, dict):
            if "jwtToken" in result:
                return result["jwtToken"]
            if "token" in result:
                return result["token"]
    
    # Try top-level
    if "jwtToken" in data:
        return data["jwtToken"]
    if "token" in data:
        return data["token"]
    
    raise ValueError("No token found in response")
```

### Fix 2: Ensure Correct Header Format
```python
def get_auth_headers(token):
    """Get headers with properly formatted Authorization"""
    if not token:
        return {"Content-Type": "application/json"}
    
    # Ensure token doesn't already include "Bearer "
    if token.startswith("Bearer "):
        auth_header = token
    else:
        auth_header = f"Bearer {token}"
    
    return {
        "Authorization": auth_header,
        "Content-Type": "application/json"
    }
```

### Fix 3: Verify Token Before Use
```python
def verify_token(token):
    """Verify token format"""
    if not token:
        return False
    
    # JWT tokens are base64 encoded and have 3 parts separated by dots
    parts = token.replace("Bearer ", "").split(".")
    if len(parts) != 3:
        return False
    
    return True
```

---

## Debugging Steps

### 1. Print Token After Authentication
```python
token = authenticate()
print(f"Token (first 50 chars): {token[:50]}")
print(f"Token length: {len(token)}")
print(f"Token starts with 'Bearer ': {token.startswith('Bearer ')}")
```

### 2. Print Headers Before Request
```python
headers = get_auth_headers(token)
print(f"Headers: {headers}")
print(f"Auth header value: {headers['Authorization'][:50]}...")
```

### 3. Check Response Status
```python
response = requests.post(url, headers=headers, ...)
print(f"Status Code: {response.status_code}")
print(f"Response: {response.text[:200]}")
```

### 4. Compare with Working Request
Use browser DevTools to capture a working request from nft-mint-studio.js and compare:
- Exact header format
- Token value (first/last few characters)
- Request URL
- Request body

---

## Summary

**Working Projects:**
- ✅ Use centralized `oasisAPI.request()` that auto-adds auth headers
- ✅ Store token in localStorage (persists)
- ✅ Handle multiple response structures
- ✅ Normalize token format

**Test Script:**
- ⚠️ Manually sets headers (should work, but verify format)
- ⚠️ Token in variable (might be lost)
- ⚠️ Assumes specific response structure
- ⚠️ No token format validation

**Most Likely Issue:**
1. Token format (missing/extra "Bearer " prefix)
2. Response structure mismatch (token not where expected)
3. Token not properly passed between function calls

---

**Status:** ✅ Analysis Complete  
**Last Updated:** January 2026
