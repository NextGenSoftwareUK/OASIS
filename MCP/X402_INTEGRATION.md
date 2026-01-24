# x402 Revenue Sharing Integration for MCP

## Overview

x402 is a payment protocol that enables automatic revenue distribution to NFT holders. This integration allows you to enable x402 revenue sharing when minting NFTs via MCP.

## What is x402?

x402 enables:
- **Automatic Payment Distribution**: Payments are automatically distributed to NFT holders
- **Revenue Sharing**: Configure how revenue is split among holders
- **Real-time Distribution**: Payments are distributed in real-time via webhooks
- **Multiple Revenue Models**: Equal split, weighted by ownership, or creator-split

## MCP Integration

The `oasis_mint_nft` tool now supports x402 configuration through the following parameters:

### x402 Parameters

1. **X402Enabled** (boolean, optional)
   - Enable or disable x402 revenue sharing
   - Default: `false`
   - Example: `true`

2. **X402PaymentEndpoint** (string, optional but required if X402Enabled=true)
   - URL endpoint for x402 revenue distribution webhooks
   - Example: `https://api.yourservice.com/x402/revenue`

3. **X402RevenueModel** (string, optional)
   - Revenue distribution model
   - Options: `"equal"`, `"weighted"`, `"creator-split"`
   - Default: `"equal"`
   - `equal`: Equal split among all holders
   - `weighted`: Split based on ownership percentage
   - `creator-split`: Creator gets a percentage, rest split among holders

4. **X402TreasuryWallet** (string, optional)
   - Treasury wallet address that receives payments for distribution
   - Example: `7aDvb3FFPm2XZ3x5mgtdT5qKnjyfpCnKDQ9wGAAqJi1Q`

## How It Works

When x402 is enabled:
1. The x402 configuration is stored in the NFT's `MetaData.x402Config` field
2. The backend can use this configuration to trigger revenue distribution webhooks
3. When payments are received, the x402 system distributes them to NFT holders according to the configured model

## Usage Examples

### Natural Language Interactive Flow (Recommended)

When using the interactive MCP flow, you'll be prompted in natural language:

**Prompt 1: Enable Revenue Sharing?**
```
Would you like to enable automatic revenue sharing for this NFT? 
When enabled, any payments your NFT receives will be automatically 
distributed to all NFT holders. This is great for creating 
revenue-generating NFTs where holders earn from usage. 
Answer "yes" to enable revenue sharing, or "no" to skip this feature.
```

**Response options:**
- `"yes"` or `"y"` - Enable revenue sharing
- `"no"` or `"n"` - Skip revenue sharing

**Prompt 2: Payment Endpoint (if enabled)**
```
To enable revenue sharing, we need a payment endpoint URL. 
This is where payment notifications will be sent when your NFT receives payments. 
You have three options: 
1) Say "create endpoint" or "use oasis" to automatically generate an endpoint 
   using the OASIS API (recommended - easiest option)
2) Provide your own custom endpoint URL (e.g., "https://api.yourservice.com/x402/revenue")
3) Say "skip" to disable revenue sharing
What would you like to do?
```

**Response options:**
- `"create endpoint"` or `"use oasis"` - Auto-generate endpoint (e.g., `https://localhost:5004/api/x402/revenue/torus`)
- `"https://api.yourservice.com/x402/revenue"` - Your custom endpoint URL
- `"skip"` - Disable revenue sharing

### Programmatic Usage

```json
{
  "Symbol": "TORUS",
  "Title": "TORUS NFT",
  "Description": "Toroidal data flows on OASIS",
  "ImageUrl": "NFT_Content/torus.gif",
  "JSONMetaDataURL": "https://jsonplaceholder.typicode.com/posts/1",
  "NumberToMint": 1,
  "X402Enabled": true,
  "X402PaymentEndpoint": "https://api.yourservice.com/x402/revenue",
  "X402RevenueModel": "equal"
}
```

### Auto-Generated Endpoint Example

If you say "create endpoint" or "use oasis", the system will automatically generate:
```
https://localhost:5004/api/x402/revenue/torus
```

This endpoint will be created automatically by the OASIS API when payments are received.

### Advanced Configuration

```json
{
  "Symbol": "PREMIUM",
  "Title": "Premium NFT Collection",
  "ImageUrl": "NFT_Content/premium.png",
  "JSONMetaDataURL": "https://example.com/metadata.json",
  "X402Enabled": true,
  "X402PaymentEndpoint": "https://api.yourservice.com/x402/revenue",
  "X402RevenueModel": "weighted",
  "X402TreasuryWallet": "7aDvb3FFPm2XZ3x5mgtdT5qKnjyfpCnKDQ9wGAAqJi1Q"
}
```

## Storage

The x402 configuration is stored in the NFT's metadata as:

```json
{
  "x402Config": {
    "enabled": true,
    "paymentEndpoint": "https://api.yourservice.com/x402/revenue",
    "revenueModel": "equal",
    "treasuryWallet": "",
    "preAuthorizeDistributions": false,
    "metadata": {
      "contentType": "other",
      "distributionFrequency": "realtime",
      "revenueSharePercentage": 100
    }
  }
}
```

## Backend Integration

The backend should:
1. Read `x402Config` from the NFT's `MetaData` field
2. When payments are received, trigger the `paymentEndpoint` webhook
3. The webhook payload should include:
   - NFT token address
   - Payment amount
   - Transaction signature
   - Distribution model
   - Treasury address

## Related Files

- **MCP Tool**: `/MCP/src/tools/oasisTools.ts` - `oasis_mint_nft` handler
- **Frontend UI**: `/oportal-repo/nft-mint-studio.js` - x402 configuration UI
- **Backend Service**: `/SmartContractGenerator/src/.../X402PaymentService.cs` - Payment verification
- **Backend Options**: `/SmartContractGenerator/src/.../X402Options.cs` - Configuration

## Notes

- x402 is **optional** - NFTs can be minted without it
- The payment endpoint must be a valid HTTPS URL
- Revenue distribution happens via webhooks to the specified endpoint
- The backend is responsible for implementing the actual distribution logic
