# Natural Language NFT Minting via MCP

## Overview

Yes! You can mint NFTs using **natural language** through the MCP tools. The AI assistant (me) can interpret your natural language requests and handle all the technical details automatically.

## How It Works

### Example Natural Language Requests:

**Simple:**
- "Mint an NFT called 'My Artwork' with this image: https://example.com/art.jpg"
- "Create a test NFT for me"
- "Mint an NFT with symbol TEST123"

**Detailed:**
- "Mint an NFT titled 'Digital Masterpiece', description 'A beautiful digital artwork', using Solana, price 0.5 SOL"
- "Create 3 NFTs with symbol MYCOLLECTION, send them to avatar ID abc-123"
- "Mint an NFT on Ethereum, ERC721 standard, with metadata at https://ipfs.io/..."

## What the AI Handles Automatically

1. **Authentication** - If needed, I'll authenticate using your credentials
2. **Parameter Mapping** - Converts your natural language to API parameters:
   - "My Artwork" → `Title: "My Artwork"`
   - "test NFT" → `Symbol: "TESTNFT"` (auto-generated)
   - "Solana" → `OnChainProvider: "SolanaOASIS"`
3. **Defaults** - Applies sensible defaults:
   - `OnChainProvider`: "SolanaOASIS" (if not specified)
   - `NFTStandardType`: "SPL" (if not specified)
   - `NumberToMint`: 1 (if not specified)
4. **Metadata** - Can create or reference metadata URLs
5. **Error Handling** - Provides helpful error messages

## Current Capabilities

### ✅ What Works Now:

```typescript
// Natural language → MCP tool call
"Create an NFT called 'Test NFT'" 
→ oasis_mint_nft({
    JSONMetaDataURL: "https://example.com/metadata.json",
    Symbol: "TESTNFT",
    Title: "Test NFT",
    // ... defaults applied automatically
})
```

### 🔧 What I Can Do:

1. **Generate Symbols** - From your description
   - "My Artwork" → "MYART" or "MYARTWORK"
   - "Test NFT #1" → "TESTNFT1"

2. **Create Metadata URLs** - If you provide:
   - Image URL → I can create/use metadata
   - Description → I can structure it properly

3. **Handle Provider Selection**:
   - "Solana" → SolanaOASIS
   - "Ethereum" → EthereumOASIS
   - "Polygon" → PolygonOASIS

4. **Smart Defaults**:
   - If you don't specify provider → Uses Solana (fast, cheap)
   - If you don't specify standard → Uses SPL (Solana standard)
   - If you don't specify price → Sets to 0

## Example Workflow

### User Request (Natural Language):
```
"Create a test NFT for me called 'My First NFT' with this image: 
https://example.com/nft-image.png"
```

### What Happens Behind the Scenes:

1. **I interpret your request:**
   - Title: "My First NFT"
   - Image: https://example.com/nft-image.png
   - Need to create metadata or use existing URL

2. **I check authentication:**
   - If not authenticated → Authenticate first
   - If authenticated → Proceed

3. **I prepare the mint request:**
   ```json
   {
     "Title": "My First NFT",
     "Symbol": "MYFIRSTNFT",
     "ImageUrl": "https://example.com/nft-image.png",
     "JSONMetaDataURL": "https://example.com/metadata.json",
     "OnChainProvider": "SolanaOASIS",
     "OffChainProvider": "None",
     "NFTOffChainMetaType": "ExternalJsonURL",
     "NFTStandardType": "SPL",
     "NumberToMint": 1
   }
   ```

4. **I call the MCP tool:**
   ```typescript
   oasis_mint_nft({
     JSONMetaDataURL: "...",
     Symbol: "MYFIRSTNFT",
     Title: "My First NFT",
     ImageUrl: "https://example.com/nft-image.png",
     // ... defaults
   })
   ```

5. **I return the result:**
   - Success → NFT minted, transaction hash, etc.
   - Error → Helpful error message with next steps

## Limitations & Requirements

### ⚠️ Current Requirements:

1. **Authentication** - Must authenticate first:
   ```
   "Authenticate me as username: testuser, password: testpass"
   ```

2. **Metadata URL** - Need either:
   - Existing metadata JSON URL, OR
   - Image URL (I can help create metadata)

3. **Symbol** - Can be:
   - Provided by you, OR
   - Auto-generated from title/description

### 🚀 Future Enhancements:

- Auto-generate metadata from image + description
- IPFS upload for metadata/images
- Batch minting from descriptions
- Collection creation from natural language

## Try It Now!

You can ask me things like:

- "Mint a test NFT"
- "Create an NFT called 'My Art' with image at https://..."
- "Mint 5 NFTs with symbol MYCOLLECTION"
- "Create an NFT on Ethereum, ERC721 standard"

I'll handle all the technical details! 🎨

---

**Bottom Line:** Yes, you can mint NFTs through natural language. Just tell me what you want, and I'll make it happen! ✨








