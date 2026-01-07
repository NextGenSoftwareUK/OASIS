# Solana RPC MCP Endpoints

**Date:** 2026-01-07  
**Status:** ✅ Implemented

## Overview

These MCP endpoints provide direct access to Solana blockchain operations via RPC, allowing you to send SOL transactions, check balances, and query transaction details without going through the OASIS API.

## Available Endpoints

### 1. `solana_send_sol` - Send SOL Tokens

Send SOL from one wallet to another using Solana RPC directly.

**Parameters:**
- `fromPrivateKey` (string, required) - Base58 encoded private key of the sender wallet
- `toAddress` (string, required) - Recipient Solana public key address
- `amount` (number, required) - Amount to send in SOL (not lamports)
- `network` (string, optional) - Network: `"devnet"`, `"mainnet-beta"`, or `"testnet"` (default: `"devnet"`)

**Example:**
```typescript
await solana_send_sol({
  fromPrivateKey: "2Q8VeG9GbotTQU97RGJH3SHE7BsVYvdp8zamze2JpAGCg2X6aXXMoFQiKsfcLqrSoUpRLdEVXHLV4hFtWeLyRD3o",
  toAddress: "7N7GhumDJMktqHGRazsCHY797H6geiV2MFd2sUwvW6Vs",
  amount: 4,
  network: "devnet"
});
```

**Response:**
```json
{
  "success": true,
  "transactionSignature": "3K9jxqrj3KkjFQ3wC67psc2HvK65QJg5DzVj8etYTJ1RBrT4pnTXZzN5YJat83X91TuLugbZMxdZjXwJ2aPE2New",
  "fromAddress": "Cwy7Xxg4HbwNrhPJoTwwyY9S52BkDev49XGPfoz4SD6h",
  "toAddress": "7N7GhumDJMktqHGRazsCHY797H6geiV2MFd2sUwvW6Vs",
  "amount": 4,
  "network": "devnet"
}
```

### 2. `solana_get_balance` - Get Wallet Balance

Get the SOL balance for a Solana wallet address.

**Parameters:**
- `address` (string, required) - Solana public key address
- `network` (string, optional) - Network: `"devnet"`, `"mainnet-beta"`, or `"testnet"` (default: `"devnet"`)

**Example:**
```typescript
await solana_get_balance({
  address: "7N7GhumDJMktqHGRazsCHY797H6geiV2MFd2sUwvW6Vs",
  network: "devnet"
});
```

**Response:**
```json
{
  "balance": 8.0,
  "lamports": 8000000000,
  "address": "7N7GhumDJMktqHGRazsCHY797H6geiV2MFd2sUwvW6Vs"
}
```

### 3. `solana_get_transaction` - Get Transaction Details

Get detailed information about a transaction by its signature.

**Parameters:**
- `signature` (string, required) - Transaction signature
- `network` (string, optional) - Network: `"devnet"`, `"mainnet-beta"`, or `"testnet"` (default: `"devnet"`)

**Example:**
```typescript
await solana_get_transaction({
  signature: "3K9jxqrj3KkjFQ3wC67psc2HvK65QJg5DzVj8etYTJ1RBrT4pnTXZzN5YJat83X91TuLugbZMxdZjXwJ2aPE2New",
  network: "devnet"
});
```

**Response:**
Returns the full Solana transaction object with details including:
- Transaction status
- Fee paid
- Block time
- Signatures
- Account changes
- And more

## Comparison with OASIS Endpoints

### `solana_send_sol` vs `oasis_send_transaction`

| Feature | `solana_send_sol` | `oasis_send_transaction` |
|---------|-------------------|--------------------------|
| **Method** | Direct Solana RPC | OASIS API wrapper |
| **Authentication** | None (uses private key) | Requires JWT token |
| **Speed** | Faster (direct) | Slower (API layer) |
| **Network** | Configurable (devnet/mainnet) | Uses OASIS provider config |
| **Use Case** | Direct blockchain operations | Integrated with OASIS avatars |

### When to Use Each

**Use `solana_send_sol` when:**
- You have the private key directly
- You want to bypass OASIS API layer
- You need to specify network explicitly
- You're doing direct blockchain operations

**Use `oasis_send_transaction` when:**
- You want to use OASIS avatar wallets
- You need OASIS integration features
- You want transaction history in OASIS
- You're working within the OASIS ecosystem

## Integration with OASIS Wallets

You can combine both approaches:

1. **Get wallet from OASIS:**
   ```typescript
   const wallet = await oasis_create_wallet_full({
     avatarId: "...",
     WalletProviderType: "SolanaOASIS",
     GenerateKeyPair: true,
     providerType: "LocalFileOASIS"
   });
   ```

2. **Use private key for direct RPC:**
   ```typescript
   await solana_send_sol({
     fromPrivateKey: wallet.result.privateKey,
     toAddress: "...",
     amount: 4,
     network: "devnet"
   });
   ```

## Security Considerations

⚠️ **Important Security Notes:**

1. **Private Key Handling:**
   - Never expose private keys in logs or error messages
   - Store private keys securely
   - Consider using environment variables for sensitive keys

2. **Network Selection:**
   - Always verify you're using the correct network (devnet vs mainnet)
   - Mainnet transactions are irreversible and cost real SOL

3. **Amount Validation:**
   - Double-check amounts before sending
   - Remember: amounts are in SOL, not lamports (1 SOL = 1,000,000,000 lamports)

4. **Transaction Fees:**
   - Each transaction requires a small fee (typically ~0.000005 SOL)
   - Ensure sender wallet has enough balance for amount + fees

## Error Handling

All endpoints return structured error responses:

```json
{
  "success": false,
  "error": "Error message here",
  "toAddress": "...",
  "amount": 4,
  "network": "devnet"
}
```

Common errors:
- `Invalid from private key` - Private key format is incorrect
- `Invalid to address` - Recipient address is not a valid Solana public key
- `Amount must be greater than 0` - Invalid amount specified
- `Failed to get balance` - Network error or invalid address
- `Failed to get transaction` - Transaction not found or network error

## RPC Endpoints Used

- **Devnet:** `https://api.devnet.solana.com`
- **Mainnet:** `https://api.mainnet-beta.solana.com`
- **Testnet:** `https://api.testnet.solana.com`

## Dependencies

These endpoints require:
- `@solana/web3.js` - Solana JavaScript SDK
- `bs58` - Base58 encoding/decoding for private keys

## Example Workflow

```typescript
// 1. Create wallet via OASIS
const wallet = await oasis_create_wallet_full({
  avatarId: "f489231f-73c8-4642-9fc9-11efeb9698fa",
  WalletProviderType: "SolanaOASIS",
  GenerateKeyPair: true,
  providerType: "LocalFileOASIS"
});

// 2. Check balance
const balance = await solana_get_balance({
  address: wallet.result.walletAddress,
  network: "devnet"
});

// 3. Send SOL
const result = await solana_send_sol({
  fromPrivateKey: wallet.result.privateKey,
  toAddress: "7N7GhumDJMktqHGRazsCHY797H6geiV2MFd2sUwvW6Vs",
  amount: 4,
  network: "devnet"
});

// 4. Verify transaction
const tx = await solana_get_transaction({
  signature: result.transactionSignature,
  network: "devnet"
});
```

## Related Documentation

- [WALLET_CREATION_GUIDE.md](./WALLET_CREATION_GUIDE.md) - Creating wallets via OASIS
- [ENDPOINT_INVENTORY.md](./ENDPOINT_INVENTORY.md) - Complete endpoint list
- [README.md](./README.md) - General MCP server documentation







