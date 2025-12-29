# Wallet Integration Module

This module implements OASIS Wallet Integration (Task Brief 04) for the Pangea Markets backend.

## Overview

The wallet module provides:
- OASIS Wallet API integration for multi-chain wallet management
- Phantom (Solana) and MetaMask (Ethereum) wallet connection and verification
- Balance synchronization between OASIS and local database
- Transaction history retrieval

## Services

### OasisWalletService

Service that wraps OASIS Wallet API calls:

- `generateWallet()` - Generate new wallet for an avatar
- `getWallets()` - Get all wallets for an avatar
- `getDefaultWallet()` - Get default wallet
- `setDefaultWallet()` - Set default wallet
- `getBalance()` - Get wallet balance
- `refreshBalance()` - Refresh wallet balance
- `sendToken()` - Send tokens between avatars
- `getTransactions()` - Get transaction history
- `getWalletByPublicKey()` - Get wallet by public key

### WalletConnectionService

Service for wallet connection and verification:

- `verifySolanaOwnership()` - Verify Solana (Phantom) wallet ownership via signature
- `verifyEthereumOwnership()` - Verify Ethereum (MetaMask) wallet ownership via signature
- `verifyOwnership()` - Generic wallet verification method
- `connectPhantom()` - Connect Phantom wallet
- `connectMetaMask()` - Connect MetaMask wallet
- `generateVerificationMessage()` - Generate message for user to sign

### BalanceSyncService

Service for synchronizing balances:

- `syncUserBalances()` - Sync all balances for a user
- `syncAssetBalance()` - Sync balance for a specific asset
- `refreshWalletBalance()` - Refresh balance for a specific wallet

## API Endpoints

### GET /api/wallet/balance
Get all balances for the authenticated user.

**Response:**
```json
{
  "success": true,
  "balances": [
    {
      "walletId": "wallet-id",
      "walletAddress": "wallet-address",
      "providerType": "SolanaOASIS",
      "balance": 100.5,
      "tokenSymbol": "SOL"
    }
  ]
}
```

### GET /api/wallet/balance/:assetId
Get balance for a specific asset.

### POST /api/wallet/connect
Connect a wallet (Phantom or MetaMask).

**Request:**
```json
{
  "walletAddress": "wallet-address",
  "signature": "signature-string",
  "message": "message-to-sign",
  "blockchain": "solana" | "ethereum"
}
```

### POST /api/wallet/verify
Verify wallet ownership.

**Request:**
```json
{
  "walletAddress": "wallet-address",
  "signature": "signature-string",
  "message": "message-to-sign",
  "blockchain": "solana" | "ethereum"
}
```

### POST /api/wallet/sync
Manually trigger balance synchronization.

### GET /api/wallet/transactions/:walletId
Get transaction history for a wallet.

## Environment Variables

Required environment variables:

```env
OASIS_API_URL=https://api.oasisplatform.world
OASIS_API_KEY=your-api-key
```

## Dependencies

- `axios` - HTTP client for OASIS API
- `tweetnacl` - Solana signature verification
- `bs58` - Base58 encoding/decoding for Solana
- `ethers` - Ethereum signature verification

## Integration Notes

1. **JWT Authentication**: The wallet controller uses `JwtAuthGuard` (placeholder currently). This will be replaced with the actual guard from Task 03.

2. **Database Integration**: The `BalanceSyncService` has placeholder code for database integration. This will be completed once the UserBalance entity is available from Task 02.

3. **User Linking**: After wallet verification, you'll need to link the wallet to the user account in the database. This is marked as TODO in the controller.

## Testing

Unit tests are included:
- `oasis-wallet.service.spec.ts`
- `wallet-connection.service.spec.ts`

Run tests with:
```bash
npm test
```

## Frontend Integration

### Phantom Connection Example

```typescript
// Frontend code (Next.js)
const connectPhantom = async () => {
  if (typeof window !== 'undefined' && window.solana?.isPhantom) {
    // Connect to Phantom
    const response = await window.solana.connect();
    const publicKey = response.publicKey.toString();
    
    // Request verification message from backend
    const messageResponse = await fetch('/api/wallet/verification-message', {
      method: 'GET',
      headers: { 'Authorization': `Bearer ${token}` },
      body: JSON.stringify({ walletAddress: publicKey, blockchain: 'solana' })
    });
    const { message } = await messageResponse.json();
    
    // Sign message
    const encodedMessage = new TextEncoder().encode(message);
    const signature = await window.solana.signMessage(encodedMessage);
    
    // Send to backend for verification
    await fetch('/api/wallet/connect', {
      method: 'POST',
      headers: { 'Authorization': `Bearer ${token}`, 'Content-Type': 'application/json' },
      body: JSON.stringify({
        walletAddress: publicKey,
        signature: bs58.encode(signature.signature),
        message,
        blockchain: 'solana'
      })
    });
  }
};
```

### MetaMask Connection Example

```typescript
// Frontend code (Next.js)
const connectMetaMask = async () => {
  if (typeof window.ethereum !== 'undefined') {
    // Request account access
    const accounts = await window.ethereum.request({ 
      method: 'eth_requestAccounts' 
    });
    const walletAddress = accounts[0];
    
    // Request verification message from backend
    const messageResponse = await fetch('/api/wallet/verification-message', {
      method: 'GET',
      headers: { 'Authorization': `Bearer ${token}` },
      body: JSON.stringify({ walletAddress, blockchain: 'ethereum' })
    });
    const { message } = await messageResponse.json();
    
    // Sign message
    const signature = await window.ethereum.request({
      method: 'personal_sign',
      params: [message, walletAddress]
    });
    
    // Send to backend for verification
    await fetch('/api/wallet/connect', {
      method: 'POST',
      headers: { 'Authorization': `Bearer ${token}`, 'Content-Type': 'application/json' },
      body: JSON.stringify({
        walletAddress,
        signature,
        message,
        blockchain: 'ethereum'
      })
    });
  }
};
```

## Next Steps

1. Complete JWT authentication integration (Task 03)
2. Complete database schema and UserBalance entity (Task 02)
3. Link verified wallets to user accounts
4. Implement periodic balance sync (cron job)
5. Add error handling and retry logic
6. Add rate limiting for OASIS API calls












