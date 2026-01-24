# How x402 Payment Endpoints Work

## Overview

The x402 payment endpoint is a **webhook URL** that gets notified when your NFT receives payments. The endpoint then distributes those payments to all NFT holders according to your configured revenue model.

## The Complete Flow

### 1. **NFT Minting (Configuration Phase)**

When you mint an NFT with x402 enabled:

```
You mint NFT → x402Config stored in NFT metadata
```

**What gets stored:**
```json
{
  "x402Config": {
    "enabled": true,
    "paymentEndpoint": "https://localhost:5004/api/x402/revenue/torus",
    "revenueModel": "equal",
    "treasuryWallet": "",
    "metadata": {
      "distributionFrequency": "realtime",
      "revenueSharePercentage": 100
    }
  }
}
```

The `paymentEndpoint` is stored in the NFT's metadata on-chain and off-chain (MongoDB).

### 2. **Payment Received (Trigger Phase)**

When someone pays to use your NFT (e.g., to access premium features, unlock content, etc.):

```
Payment transaction → OASIS API detects payment → Reads NFT metadata → Finds x402Config → Triggers webhook
```

**What happens:**
1. A payment transaction occurs on Solana (or other blockchain)
2. The OASIS API detects the payment (via blockchain monitoring or API call)
3. The API looks up the NFT that received the payment
4. The API reads the NFT's `MetaData.x402Config` field
5. If `x402Config.enabled === true`, the API triggers a POST request to `paymentEndpoint`

### 3. **Webhook Notification (Distribution Phase)**

The payment endpoint receives a webhook with payment details:

**Webhook Payload Example:**
```json
{
  "signature": "5j7s8K9m...",           // Transaction signature
  "amount": 0.1,                        // Payment amount in SOL
  "nftTokenAddress": "ABC123...",       // NFT token address
  "nftSymbol": "TORUS",                 // NFT symbol
  "timestamp": 1699123456,              // Unix timestamp
  "treasury": "7aDvb3FFPm2XZ3x5mgtdT5qKnjyfpCnKDQ9wGAAqJi1Q",  // Treasury wallet
  "revenueModel": "equal",              // Distribution model
  "metadata": {
    "operation": "premium_access",       // What the payment was for
    "blockchain": "Solana",
    "price": 0.1
  }
}
```

### 4. **Revenue Distribution (Execution Phase)**

The payment endpoint (your service or OASIS API) then:

1. **Identifies NFT Holders**: Queries the blockchain to find all wallets holding the NFT
2. **Calculates Distribution**: Based on `revenueModel`:
   - **"equal"**: Split payment equally among all holders
   - **"weighted"**: Split based on ownership percentage (if multiple NFTs per holder)
   - **"creator-split"**: Creator gets a percentage, rest split among holders
3. **Executes Transfers**: Sends SOL (or other tokens) to each holder's wallet
4. **Logs Results**: Records the distribution for transparency

## Current Implementation Status

### ✅ What's Working

1. **MCP Integration**: You can enable x402 when minting NFTs via MCP
2. **Metadata Storage**: x402 config is stored in NFT metadata
3. **Natural Language Flow**: Easy prompts for users to configure x402
4. **Auto-Endpoint Generation**: Can auto-generate endpoint URLs

### ⚠️ What Needs to Be Built

The OASIS API needs to implement:

1. **Payment Detection**: Monitor blockchain for payments to NFTs with x402 enabled
2. **Metadata Reading**: Read `x402Config` from NFT metadata when payments detected
3. **Webhook Triggering**: POST to the `paymentEndpoint` URL with payment details
4. **Distribution Service**: Endpoint handler that receives webhooks and distributes payments

## Auto-Generated Endpoints

When you say **"create endpoint"** during NFT minting, MCP generates:

```
https://localhost:5004/api/x402/revenue/{symbol}
```

**Example:** For NFT symbol "TORUS":
```
https://localhost:5004/api/x402/revenue/torus
```

### What This Endpoint Should Do

This endpoint should be implemented in the OASIS API to:

1. **Receive Webhooks**: Accept POST requests with payment information
2. **Validate Payment**: Verify the payment transaction on-chain
3. **Get NFT Holders**: Query blockchain for all wallets holding the NFT
4. **Calculate Splits**: Use the `revenueModel` to calculate distribution
5. **Execute Transfers**: Send payments to each holder
6. **Return Status**: Confirm distribution success/failure

## Example Implementation Flow

### Scenario: User pays 0.1 SOL to use TORUS NFT

```
1. User sends 0.1 SOL to treasury wallet
   Transaction: 5j7s8K9m...

2. OASIS API detects payment
   → Checks: Is this payment for an NFT?
   → Looks up: Which NFT?

3. OASIS API reads NFT metadata
   → Finds: x402Config.enabled = true
   → Finds: paymentEndpoint = "https://localhost:5004/api/x402/revenue/torus"
   → Finds: revenueModel = "equal"

4. OASIS API triggers webhook
   POST https://localhost:5004/api/x402/revenue/torus
   {
     "signature": "5j7s8K9m...",
     "amount": 0.1,
     "nftSymbol": "TORUS",
     "revenueModel": "equal"
   }

5. Payment endpoint receives webhook
   → Queries Solana: Who holds TORUS NFT?
   → Finds: 10 holders
   → Calculates: 0.1 SOL / 10 = 0.01 SOL per holder

6. Payment endpoint distributes
   → Sends 0.01 SOL to each of 10 holders
   → Returns: { "distributed": true, "recipients": 10, "amountPerRecipient": 0.01 }
```

## Custom Endpoints

You can also provide your own custom endpoint:

```
X402PaymentEndpoint: "https://api.yourservice.com/x402/revenue"
```

**Your service must:**
- Accept POST requests
- Handle the webhook payload format
- Implement distribution logic
- Return success/failure status

## Security Considerations

1. **Webhook Authentication**: Endpoints should verify webhook signatures
2. **Payment Verification**: Always verify payments on-chain before distributing
3. **Rate Limiting**: Prevent webhook spam
4. **Idempotency**: Handle duplicate webhook calls gracefully

## Configuration Options

### Revenue Models

- **"equal"**: Each holder gets the same amount
  - Example: 0.1 SOL payment, 10 holders → 0.01 SOL each

- **"weighted"**: Split based on ownership percentage
  - Example: Holder A has 3 NFTs, Holder B has 1 NFT
  - Total: 4 NFTs → A gets 75%, B gets 25%

- **"creator-split"**: Creator gets a percentage first
  - Example: Creator gets 20%, remaining 80% split among holders

### Treasury Wallet

The `treasuryWallet` is where payments are initially received. The endpoint then distributes from this wallet to holders.

## Next Steps for Full Implementation

To make x402 fully functional, the OASIS API needs:

1. **Payment Monitoring Service**: 
   - Monitor blockchain for payments to NFT treasury wallets
   - Detect when payments are received

2. **x402 Webhook Service**:
   - Read NFT metadata to get `x402Config`
   - Trigger webhooks to `paymentEndpoint` URLs

3. **Distribution Endpoint** (`/api/x402/revenue/{symbol}`):
   - Receive webhook notifications
   - Query NFT holders from blockchain
   - Calculate distribution based on `revenueModel`
   - Execute transfers to holders
   - Return distribution results

4. **Error Handling**:
   - Retry failed webhooks
   - Log distribution failures
   - Handle edge cases (no holders, zero payments, etc.)

## Summary

The payment endpoint is a **webhook URL** that:
- Gets stored in NFT metadata when minting
- Receives notifications when the NFT gets paid
- Distributes payments to all NFT holders
- Can be auto-generated or custom
- Supports multiple revenue distribution models

The endpoint acts as the **bridge** between receiving payments and distributing them to holders, enabling automatic revenue sharing for NFTs.

---

## Drip / Vesting: Set Amount from a Designated Fund Over Time

The flow above is **reactive**: it runs when **someone pays** to use the NFT. There is no built-in way to **schedule** a set amount from a **designated fund** to the NFT (and thus to holders) over time.

**It is possible** to add that by:

1. **`dripConfig`** (in NFT `x402Config`) or a **Drip Registry**: `sourceFundWallet`, `amountPerPeriod`, `period` (e.g. monthly), optional `capTotal` / `capPeriods`.
2. **A Drip Service** (cron): on each period, for each enrolled NFT, send `amountPerPeriod` SOL **from** `sourceFundWallet` to holders, using the same **holder lookup and `revenueModel`** as the x402 payment endpoint.

The NFT still uses `paymentEndpoint` and `revenueModel` for **usage-based** revenue; drip reuses those distribution rules with a **different trigger** (schedule) and **source of funds** (the designated fund).

See **[X402_DRIP_VESTING_DESIGN.md](./X402_DRIP_VESTING_DESIGN.md)** for the full design, `dripConfig` schema, Drip Service behavior, and integration with higher-tier Pass NFTs.
