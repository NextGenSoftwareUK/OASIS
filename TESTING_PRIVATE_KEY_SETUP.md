# Testing Private Key Setup - Quick Guide

**Date:** 2026-01-09  
**Purpose:** Make private keys accessible for testing (NFT minting, smart contracts)

---

## ✅ What Was Done

1. **Set Test Encryption Key** in `OASIS_DNA.json`:
   - Key: `TEST_KEY_FOR_DEVELOPMENT_ONLY_DO_NOT_USE_IN_PRODUCTION_256BIT`
   - Location: `OASIS Architecture/NextGenSoftware.OASIS.API.DNA/OASIS_DNA.json`
   - Section: `OASIS.Security.OASISProviderPrivateKeys.Rijndael256Key`

2. **Created Security Guide**: `PRIVATE_KEY_SECURITY_GUIDE.md`
   - Documents current testing setup
   - Provides production security requirements
   - Includes migration path

---

## 🔄 Next Steps (Required)

### 1. Restart the API Server

The API needs to be restarted to load the new encryption key from `OASIS_DNA.json`.

```bash
# Stop the current API server
# Then restart it
cd ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI
dotnet run
```

### 2. Link Private Key Again

After restart, try linking the private key again:

```bash
TOKEN="<your-jwt-token>"
WALLET_ID="ec42a998-9c87-4920-9b08-c82c4662ae03"
PRIVATE_KEY="q9AgoRmqsS0XqNbZpAC5CkL3TKfuJ0R+KACm5gg3Eq0EaMfMAIG7BJhH0deWMAj4KGUnNM+QdcSFnmJzyauvMA=="

curl -k -X POST "https://127.0.0.1:5004/api/keys/link_provider_private_key_to_avatar_by_id" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"WalletId\": \"$WALLET_ID\",
    \"AvatarID\": \"d42b8448-52a9-4579-a6b1-b7c624616459\",
    \"ProviderType\": \"SolanaOASIS\",
    \"ProviderKey\": \"$PRIVATE_KEY\",
    \"ShowPrivateKey\": false,
    \"ShowSecretRecoveryWords\": false
  }"
```

### 3. Verify Wallet File Created

Check if the wallet file was created:

```bash
# Wallet file location
ls -la ~/Library/Application\ Support/OASIS/LocalFileOASIS/wallets_d42b8448-52a9-4579-a6b1-b7c624616459.json

# Or check all wallet files
ls -la ~/Library/Application\ Support/OASIS/LocalFileOASIS/wallets_*.json
```

---

## 📍 Current Wallet Information

- **Wallet ID:** `ec42a998-9c87-4920-9b08-c82c4662ae03`
- **Wallet Address:** `JDJMNKRj8RyaPyf1k8eN2fhwz5U2aaJrbfk6dg233tT`
- **Public Key:** `JDJMNKRj8RyaPyf1k8eN2fhwz5U2aaJrbfk6dg233tT`
- **Private Key:** `q9AgoRmqsS0XqNbZpAC5CkL3TKfuJ0R+KACm5gg3Eq0EaMfMAIG7BJhH0deWMAj4KGUnNM+QdcSFnmJzyauvMA==`
- **Provider:** SolanaOASIS
- **Avatar ID:** `d42b8448-52a9-4579-a6b1-b7c624616459`
- **Avatar Username:** `OASIS_ADMIN`

---

## 🔍 How Private Keys Are Stored

### Storage Flow:
1. **Encryption:** Private key is encrypted with Rijndael256 (AES-256) using the key from `OASIS_DNA.json`
2. **Storage:** Encrypted private key is saved to LocalFileOASIS
3. **File Location:** `{ApplicationData}/OASIS/LocalFileOASIS/wallets_{avatarId}.json`
4. **Format:** JSON file with dictionary of `ProviderType -> List<IProviderWallet>`

### File Structure:
```json
{
  "SolanaOASIS": [
    {
      "walletId": "ec42a998-9c87-4920-9b08-c82c4662ae03",
      "walletAddress": "JDJMNKRj8RyaPyf1k8eN2fhwz5U2aaJrbfk6dg233tT",
      "publicKey": "JDJMNKRj8RyaPyf1k8eN2fhwz5U2aaJrbfk6dg233tT",
      "privateKey": "<encrypted-with-test-key>",
      "providerType": 3,
      "isDefaultWallet": false
    }
  ]
}
```

---

## ✅ Testing Checklist

- [ ] API server restarted with new encryption key
- [ ] Private key successfully linked (no padding error)
- [ ] Wallet file created at expected location
- [ ] Can retrieve private key via API (if needed)
- [ ] Can sign Solana transactions
- [ ] Can mint NFTs
- [ ] Can interact with smart contracts

---

## 🔒 Security Notes for Testing

**Current Configuration:**
- ✅ Private keys are encrypted (even with test key)
- ✅ Stored locally only (not replicated to network providers)
- ⚠️ Test encryption key is hardcoded (NOT secure for production)
- ⚠️ File permissions should be set (chmod 600)

**For Production:**
- See `PRIVATE_KEY_SECURITY_GUIDE.md` for full security requirements
- Must use cryptographically secure key
- Must use secure key management system
- Must set proper file permissions
- Must implement access controls

---

## 🐛 Troubleshooting

### Issue: "Padding is invalid and cannot be removed"
**Solution:** 
1. Ensure API server was restarted after updating `OASIS_DNA.json`
2. Verify encryption key is set correctly in config
3. Check that key is exactly 32 bytes (256 bits)

### Issue: Wallet file not found
**Solution:**
1. Check file location: `~/Library/Application Support/OASIS/LocalFileOASIS/`
2. Verify avatar ID is correct
3. Check file permissions

### Issue: Cannot sign transactions
**Solution:**
1. Verify private key was successfully linked
2. Check wallet file contains encrypted private key
3. Ensure Solana provider is activated
4. Verify network connection to Solana RPC

---

## 📚 Related Documents

- `PRIVATE_KEY_SECURITY_GUIDE.md` - Full security guide for production
- `WALLET_CREATION_VIA_KEYS_API_SOLUTION.md` - Wallet creation workflow
- `SOLANA_WALLET_CREATION_GUIDE.md` - Solana-specific wallet creation

---

## ⚠️ Important Warnings

1. **This is a TEST configuration** - Do NOT use in production
2. **Test encryption key is NOT secure** - Anyone with access to the config can decrypt
3. **Never commit encryption keys to git** - Use environment variables or key management services
4. **File permissions matter** - Set wallet files to 600 (owner read/write only)
5. **Backup securely** - If backing up wallet files, encrypt the backups

---

**Status:** ⚠️ Waiting for API restart to test private key linking
