# x402 Revenue Sharing - Natural Language Guide

## Overview

x402 revenue sharing is now integrated into the MCP NFT minting flow with **natural language prompts** that make it easy to understand and configure.

## What is x402 Revenue Sharing?

x402 allows your NFT to automatically distribute payments to all holders. When someone pays to use your NFT, the payment is automatically split among everyone who owns it. This is perfect for:
- Revenue-generating NFTs
- Royalty-sharing collections
- Community-owned assets
- Subscription-based NFTs

## How It Works (Natural Language)

When you mint an NFT via MCP, you'll be asked simple questions:

### Question 1: Enable Revenue Sharing?

**The system asks:**
> "Would you like to enable automatic revenue sharing for this NFT? When enabled, any payments your NFT receives will be automatically distributed to all NFT holders. This is great for creating revenue-generating NFTs where holders earn from usage. Answer 'yes' to enable revenue sharing, or 'no' to skip this feature."

**You can answer:**
- ✅ `"yes"` or `"y"` - Enable revenue sharing
- ✅ `"no"` or `"n"` - Skip (NFT will be minted without revenue sharing)

### Question 2: Payment Endpoint (if you said "yes")

**The system asks:**
> "To enable revenue sharing, we need a payment endpoint URL. This is where payment notifications will be sent when your NFT receives payments. You have three options: 1) Say 'create endpoint' or 'use oasis' to automatically generate an endpoint using the OASIS API (recommended - easiest option), 2) Provide your own custom endpoint URL (e.g., 'https://api.yourservice.com/x402/revenue'), or 3) Say 'skip' to disable revenue sharing. What would you like to do?"

**You can answer:**
- ✅ `"create endpoint"` or `"use oasis"` - **Easiest option!** Automatically generates an endpoint like `https://localhost:5004/api/x402/revenue/torus`
- ✅ `"https://your-custom-endpoint.com/x402/revenue"` - Use your own endpoint
- ✅ `"skip"` - Disable revenue sharing

## Example Conversation Flow

**You:** "Mint a Solana NFT"

**System:** "I need some information. Symbol?"

**You:** "TORUS"

**System:** "Image URL?"

**You:** "NFT_Content/torus.gif"

**System:** "Would you like to enable automatic revenue sharing for this NFT? Answer 'yes' or 'no'."

**You:** "yes"

**System:** "To enable revenue sharing, we need a payment endpoint URL. Say 'create endpoint' to auto-generate one, provide your own URL, or say 'skip'."

**You:** "create endpoint"

**System:** "✅ Auto-generated x402 payment endpoint: https://localhost:5004/api/x402/revenue/torus"

**System:** "🎨 NFT minted successfully!"

## Auto-Generated Endpoints

When you say **"create endpoint"** or **"use oasis"**, the system automatically creates:
```
https://localhost:5004/api/x402/revenue/{your-nft-symbol}
```

For example, if your NFT symbol is "TORUS":
```
https://localhost:5004/api/x402/revenue/torus
```

This endpoint will be automatically handled by the OASIS API - you don't need to set up anything else!

## Revenue Distribution Models

When revenue sharing is enabled, you can choose how payments are distributed:

- **"equal"** (default) - Equal split among all holders
- **"weighted"** - Split based on ownership percentage
- **"creator-split"** - Creator gets a percentage, rest split among holders

## Complete Example

**Natural Language:**
```
You: "Mint an NFT with symbol ART123, title 'My Artwork', 
      image at NFT_Content/art.png, enable revenue sharing, 
      and create the endpoint automatically"
```

**What happens:**
1. NFT is minted with symbol "ART123"
2. Revenue sharing is enabled
3. Endpoint is auto-generated: `https://localhost:5004/api/x402/revenue/art123`
4. x402 config is stored in NFT metadata
5. When payments are received, they're automatically distributed to holders

## Benefits of Natural Language

✅ **No technical knowledge needed** - Just answer simple questions  
✅ **Auto-endpoint creation** - Say "create endpoint" and it's done  
✅ **Clear explanations** - Understand what each option does  
✅ **Flexible** - Use your own endpoint or auto-generate one  

## Technical Details (For Developers)

The x402 configuration is stored in the NFT's `MetaData.x402Config`:

```json
{
  "x402Config": {
    "enabled": true,
    "paymentEndpoint": "https://localhost:5004/api/x402/revenue/torus",
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

## Next Steps

1. **Mint an NFT** via MCP
2. **Answer "yes"** when asked about revenue sharing
3. **Say "create endpoint"** to auto-generate the endpoint
4. **Done!** Your NFT now has revenue sharing enabled

The OASIS backend will handle the rest automatically when payments are received!
