# Avatar Wallet Integration for Smart Contract Generator

**Date:** December 23, 2024  
**Status:** ✅ Authenticated and Wallet Retrieved

---

## ✅ Authentication Success

### Avatar Details
- **Username:** OASIS_ADMIN
- **Avatar ID:** `bfbce7c2-708e-40ae-af79-1d2421037eaa`
- **JWT Token:** Retrieved (expires in 15 minutes)

### Solana Wallet
- **Address:** `76Cn3dG3QY55PqQZCx44JxSH3XhojNSLL2z3D66BU3cK`
- **Public Key:** `76Cn3dG3QY55PqQZCx44JxSH3XhojNSLL2z3D66BU3cK`
- **Wallet ID:** `a7a1c78e-47f4-4ba2-9a07-f5e00834f7dd`
- **Provider:** SolanaOASIS

---

## 💰 Devnet SOL Request

**Status:** ⚠️ Airdrop request returned internal error

**Manual Request:**
- Faucet URL: https://faucet.solana.com/?address=76Cn3dG3QY55PqQZCx44JxSH3XhojNSLL2z3D66BU3cK
- Or use: `solana airdrop 2 76Cn3dG3QY55PqQZCx44JxSH3XhojNSLL2z3D66BU3cK --url devnet`

---

## 🔗 Integration Approach

### Option 1: Use Avatar Wallet for All Operations
- Smart contract generation uses avatar's Solana wallet
- Deployment uses avatar's wallet automatically
- No need for separate wallet management

### Option 2: User Selects Wallet
- User authenticates with avatar
- System shows available wallets
- User selects which wallet to use for contract operations

### Option 3: Default Avatar Wallet
- Use avatar's default Solana wallet
- Allow override if user has multiple wallets

**Recommended:** Option 1 - Use avatar wallet automatically

---

## 📝 Implementation Steps

### 1. Update API Configuration
Update `appsettings.json` to use avatar wallet:

```json
{
  "Solana": {
    "RpcUrl": "https://api.devnet.solana.com",
    "UseLocalValidator": false,
    "UseAvatarWallet": true,
    "AvatarId": "bfbce7c2-708e-40ae-af79-1d2421037eaa",
    "OASISApiUrl": "http://api.oasisweb4.com"
  }
}
```

### 2. Create Avatar Wallet Service
Create service to:
- Authenticate avatar
- Retrieve Solana wallet
- Get wallet keypair for signing
- Cache wallet info

### 3. Update Contract Deploy Service
Modify `SolanaContractDeploy` to:
- Check if avatar wallet should be used
- Authenticate and get wallet if needed
- Use wallet keypair for deployment

### 4. Update UI
- Show avatar wallet address
- Display balance
- Link to request devnet SOL
- Show deployment will use avatar wallet

---

## 🔧 API Integration

### Authentication Endpoint
```
POST /api/avatar/authenticate
Body: { "username": "OASIS_ADMIN", "password": "Uppermall1!" }
Response: { result: { jwtToken, avatarId, providerWallets: { SolanaOASIS: [...] } } }
```

### Get Wallet Endpoint
```
GET /api/wallet/avatar/{avatarId}/wallets
Headers: { "Authorization": "Bearer {jwtToken}" }
Response: { result: [{ providerType, address, walletId, ... }] }
```

### Get Wallet Details (with keypair)
```
GET /api/wallet/{walletId}
Headers: { "Authorization": "Bearer {jwtToken}" }
Response: { result: { address, privateKey, keypair, ... } }
```

---

## 🧪 Testing with Avatar Wallet

### Test Script
```bash
# 1. Authenticate and get wallet
source <(./get-avatar-wallet.sh | grep '^export')

# 2. Generate contract
curl -X POST "http://localhost:5000/api/v1/contracts/generate" \
  -F "Language=Rust" \
  -F "JsonFile=@spec.json"

# 3. Compile contract
curl -X POST "http://localhost:5000/api/v1/contracts/compile" \
  -F "Language=Rust" \
  -F "Source=@generated.zip"

# 4. Deploy using avatar wallet
curl -X POST "http://localhost:5000/api/v1/contracts/deploy" \
  -F "Language=Rust" \
  -F "CompiledContractFile=@compiled.so" \
  -F "Schema=@schema.json" \
  -H "X-Avatar-Id: $OASIS_AVATAR_ID" \
  -H "Authorization: Bearer $OASIS_JWT_TOKEN"
```

---

## 🔐 Security Considerations

1. **JWT Token Expiration:** Tokens expire in 15 minutes - implement refresh
2. **Wallet Keypair:** Never expose private keys in logs or responses
3. **Authentication:** Always validate JWT token before wallet access
4. **Authorization:** Verify avatar owns the wallet before using it
5. **Rate Limiting:** Limit wallet access requests

---

## 📊 Current Status

- ✅ Authentication working
- ✅ Solana wallet retrieved
- ⚠️ Devnet SOL airdrop needs manual request
- ⏳ API integration pending
- ⏳ UI integration pending

---

## 🚀 Next Steps

1. **Request Devnet SOL manually** from faucet
2. **Update API** to support avatar wallet authentication
3. **Modify deploy service** to use avatar wallet
4. **Update UI** to show avatar wallet and balance
5. **Test full flow:** Generate → Compile → Deploy with avatar wallet

---

**Wallet Address:** `76Cn3dG3QY55PqQZCx44JxSH3XhojNSLL2z3D66BU3cK`  
**Faucet:** https://faucet.solana.com/?address=76Cn3dG3QY55PqQZCx44JxSH3XhojNSLL2z3D66BU3cK


