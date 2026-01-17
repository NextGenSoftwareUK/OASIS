# Interactive NFT Minting via MCP

## Overview

The `oasis_mint_nft` tool now supports an **interactive mode** that guides you through the minting process step-by-step. Simply say "Mint a Solana NFT" and you'll be prompted for any missing information.

## How It Works

### Step 1: Start the Process
You can initiate NFT minting with minimal information:
- "Mint a Solana NFT"
- "Create an NFT for me"
- "I want to mint an NFT"

### Step 2: Interactive Prompts
The AI assistant will automatically:
1. Check what information you've provided
2. Identify what's missing
3. Prompt you in priority order:

**Required Fields:**
   - **Symbol** (e.g., "MYNFT", "ART123")
   - **JSONMetaDataURL** (metadata JSON file URL)

**Recommended Fields:**
   - **NumberToMint** - How many NFTs to create (default: 1)
   - **Title** - Name of your NFT
   - **ImageUrl** - Image URL for your NFT

**Optional but Useful Fields:**
   - **Description** - Detailed description of your NFT
   - **Attributes/Traits** - Custom traits (e.g., Color: Blue, Rarity: Common)
   - **External URL** - Link to your project/collection page
   - **Category** - Type of content (image, video, audio, 3d)
   - **Price** - Initial sale price (default: 0)

### Step 3: Optional Information
You can also provide optional details at any time:
- **Title** - Name of your NFT
- **Description** - Description of your NFT
- **ImageUrl** - Direct link to your image
- **MetaData** - Custom attributes and traits

### Step 4: Minting
Once all required information is provided, the NFT will be minted automatically.

## Example Interactive Flow

### User Request:
```
"Mint a Solana NFT"
```

### AI Response:
```
I'd be happy to help you mint a Solana NFT! I need a couple of pieces of information:

**Required:**
1. **Symbol**: What symbol/ticker would you like for your NFT? (e.g., "MYNFT", "ART123")
2. **Metadata URL**: Do you have a metadata JSON file URL, or would you like to use a placeholder for testing?

**Recommended:**
3. **How many to mint**: How many NFTs would you like to create? (Default: 1)
4. **Title**: What would you like to name your NFT?
5. **Image URL**: What image would you like to use? (You can use a local image from NFT_Content folder or any public URL)

**Optional:**
- Description
- Attributes/Traits (e.g., Color: Blue, Rarity: Common)
- External URL (link to your project page)
- Category (image, video, audio, 3d)
- Price (if selling)
```

### User Provides Information:
```
Symbol: "TESTNFT"
Title: "My First NFT"
Number to mint: 1
Use a placeholder for the metadata URL
Image: http://localhost:8000/my-image.png
Description: "A test NFT for my collection"
```

### AI Response:
```
Perfect! I'll mint your NFT with:
- Symbol: TESTNFT
- Title: My First NFT
- Number to mint: 1
- Metadata URL: https://jsonplaceholder.typicode.com/posts/1 (placeholder)
- Image: http://localhost:8000/my-image.png
- Description: A test NFT for my collection

Minting your NFT now...
```

### Final Result:
```
✅ NFT minted successfully!

📋 NFT Details:
   Token Address: HH2AcY6p626r4CWDGi7RgdGxkroSCUrrB6Bjy4BsrPXF
   Mint Hash: dFz9epvgWgJAReZtcQVJY8tjvpxrxLo7CPjLCDETu8EE...
   Sent To: 8bFhmkao9SJ6axVNcNRoeo85aNG45HP94oMtnQKGSUuz
```

## Quick Start Examples

### Minimal Request
```
"Mint a Solana NFT"
```
→ You'll be prompted for:
- Symbol (required)
- Metadata URL (required)
- Number to mint (recommended)
- Title (recommended)
- Image URL (recommended)
- Plus optional fields if you want them

### With Some Information
```
"Mint a Solana NFT called 'My Artwork' with symbol ART123"
```
→ You'll only be prompted for Metadata URL

### Complete Request
```
"Mint a Solana NFT with:
- Symbol: MYNFT
- Title: Digital Masterpiece
- Description: A beautiful digital artwork
- Number to mint: 1
- Image: http://localhost:8000/my-image.png
- Metadata URL: https://jsonplaceholder.typicode.com/posts/1
- Attributes: Color: Blue, Rarity: Rare
- Category: image"
```
→ NFT will be minted immediately

### Batch Minting Example
```
"Mint 5 Solana NFTs with symbol COLLECTION"
```
→ You'll be prompted for the remaining details, and 5 NFTs will be created

## Using Images

You have several options for providing images:

### Option 1: Upload to Pinata/IPFS (Recommended)
Simply say: **"Upload my image from NFT_Content/my-image.png"**

The AI will:
1. Upload the file to Pinata/IPFS
2. Get the IPFS URL
3. Use it automatically in your NFT

**Benefits:**
- Decentralized storage on IPFS
- Permanent URLs (IPFS hashes)
- No local server needed
- Reliable Pinata pinning service

### Option 2: Local Image Server
1. **Start the image server:**
   ```bash
   cd NFT_Content
   ./serve-images.sh
   ```

2. **Reference the image:**
   ```
   Image URL: http://localhost:8000/my-image.png
   ```

### Option 3: Public URL
Use any publicly accessible image URL:
```
Image URL: https://example.com/my-image.png
```

### Option 4: Placeholder (Testing)
For quick testing:
```
Image URL: https://via.placeholder.com/512
```

## Default Values

If you don't specify these, they'll be set automatically:
- **OnChainProvider**: `SolanaOASIS`
- **OffChainProvider**: `MongoDBOASIS`
- **NFTOffChainMetaType**: `OASIS`
- **NFTStandardType**: `SPL`
- **NumberToMint**: `1`
- **Price**: `0`

## Tips

1. **For Testing**: Use placeholder URLs like `https://jsonplaceholder.typicode.com/posts/1` for metadata
2. **Symbol Format**: Keep symbols short and alphanumeric (e.g., "MYNFT", "ART123")
3. **Image URLs**: Can be local (with server running) or public URLs
4. **Batch Minting**: Use `NumberToMint` to create multiple NFTs at once (useful for collections)
5. **Attributes/Traits**: Add custom traits for rarity systems and filtering:
   ```json
   {
     "attributes": [
       {"trait_type": "Color", "value": "Blue"},
       {"trait_type": "Rarity", "value": "Legendary"},
       {"trait_type": "Artist", "value": "Your Name"}
     ]
   }
   ```
6. **Categories**: Use standard categories like "image", "video", "audio", "3d" for better platform compatibility
7. **External URL**: Link to your project page, collection, or community
8. **Price**: Set to 0 if not for sale, or specify a price in SOL

## Troubleshooting

### "I need more information" Response
This means required fields are missing. Just provide the Symbol and Metadata URL when prompted.

### Authentication Required
If you get an authentication error, the AI will help you authenticate first before minting.

### Image Not Loading
Make sure:
- Local server is running (if using local images)
- Image URL is publicly accessible
- Image format is supported (PNG, JPG, GIF, etc.)
