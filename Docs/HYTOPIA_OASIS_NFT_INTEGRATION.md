# HYTOPIA + OASIS NFT API Integration Guide

**Date:** January 22, 2026  
**Status:** 💡 Integration Proposal  
**Goal:** Position OASIS as creator backend for HYTOPIA - enabling asset minting, persistence, and monetization

---

## Overview

This integration enables HYTOPIA creators to:
- **Mint assets** (blocks, items, skins, worlds) as NFTs on Solana/Ethereum
- **Persist assets** across sessions and platforms via OASIS metadata storage
- **Monetize creations** through NFT sales, royalties, and x402 revenue sharing
- **Track ownership** and provenance of creator IP

**Phase 1 Focus:** Creator backend only - no social, no governance, just earning.

---

## Architecture

```
┌─────────────────────────────────────────────────────────┐
│              HYTOPIA Game World (TypeScript)            │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  ┌──────────────────────────────────────────────────┐  │
│  │      OASIS NFT Plugin (HYTOPIA SDK)              │  │
│  │  - mintAsset()                                   │  │
│  │  - loadPlayerNFTs()                             │  │
│  │  - displayNFTGallery()                          │  │
│  │  - sellNFT()                                    │  │
│  └──────────────────────────────────────────────────┘  │
│                          │                              │
│                          ▼                              │
│  ┌──────────────────────────────────────────────────┐  │
│  │      OASIS REST API Client                       │  │
│  │  POST /api/nft/mint-nft                         │  │
│  │  GET  /api/nft/load-all-nfts-for_avatar/{id}    │  │
│  │  GET  /api/nft/load-nft/{id}                    │  │
│  └──────────────────────────────────────────────────┘  │
│                          │                              │
└──────────────────────────┼──────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────┐
│              OASIS API (ONODE Backend)                  │
├─────────────────────────────────────────────────────────┤
│  - NFT Minting (Solana/Ethereum)                       │
│  - Metadata Storage (MongoDB/IPFS)                     │
│  - Avatar Authentication                                │
│  - Wallet Management                                    │
└─────────────────────────────────────────────────────────┘
```

---

## HYTOPIA SDK Integration

### 1. Install OASIS Plugin

```typescript
// In your HYTOPIA world's package.json
{
  "dependencies": {
    "@hytopia/oasis-nft": "^1.0.0"
  }
}
```

### 2. Initialize OASIS Client

```typescript
import { OASISNFT } from "@hytopia/oasis-nft";

// Initialize with your OASIS API credentials
const oasisNFT = new OASISNFT({
  apiUrl: "https://api.oasisweb4.com", // or localhost:5003 for dev
  avatarId: "player-avatar-id",
  authToken: "jwt-token-from-authentication"
});

// Authenticate player
await oasisNFT.authenticate("username", "password");
```

---

## Core Integration Patterns

### Pattern 1: Mint In-Game Assets as NFTs

**Use Case:** Creator builds a custom block/item in HYTOPIA, wants to tokenize it.

```typescript
// In your HYTOPIA world script
import { OASISNFT } from "@hytopia/oasis-nft";

async function mintCustomBlock(blockData: BlockData) {
  // 1. Capture block as image/screenshot
  const blockImage = await captureBlockScreenshot(blockData);
  
  // 2. Upload to IPFS via OASIS
  const imageUrl = await oasisNFT.uploadToIPFS(blockImage);
  
  // 3. Mint NFT
  const nft = await oasisNFT.mintAsset({
    title: blockData.name,
    symbol: blockData.symbol || "HYTOPIA_BLOCK",
    description: `Custom block created in ${world.name}`,
    imageUrl: imageUrl,
    metadata: {
      worldId: world.id,
      creatorId: player.avatarId,
      blockType: blockData.type,
      properties: blockData.properties,
      createdAt: new Date().toISOString()
    },
    onChainProvider: "SolanaOASIS", // or "EthereumOASIS"
    price: blockData.price || 0 // Set price for marketplace
  });
  
  console.log(`✅ Block minted as NFT: ${nft.nftTokenAddress}`);
  return nft;
}
```

### Pattern 2: Display Player's NFT Collection

**Use Case:** Show player's owned NFTs in an in-game gallery.

```typescript
async function displayPlayerGallery() {
  // Load all NFTs owned by player
  const nfts = await oasisNFT.loadPlayerNFTs();
  
  // Create gallery UI
  const gallery = new NFTGallery({
    nfts: nfts,
    onSelect: (nft) => {
      // Player selected an NFT - maybe equip it, display it, etc.
      equipNFTAsSkin(nft);
    },
    onSell: async (nft) => {
      // List NFT for sale
      await oasisNFT.listForSale(nft.id, 100); // Price in SOL/ETH
    }
  });
  
  // Display in world
  world.ui.show(gallery);
}
```

### Pattern 3: Persist World State as NFT

**Use Case:** Creator wants to save their world state and sell it as an NFT.

```typescript
async function mintWorldAsNFT(world: World) {
  // 1. Export world data
  const worldData = {
    blocks: world.getAllBlocks(),
    entities: world.getAllEntities(),
    metadata: world.metadata,
    version: world.version
  };
  
  // 2. Create world preview image
  const previewImage = await world.captureScreenshot();
  const imageUrl = await oasisNFT.uploadToIPFS(previewImage);
  
  // 3. Upload world data to IPFS
  const worldDataUrl = await oasisNFT.uploadToIPFS(
    JSON.stringify(worldData),
    "application/json"
  );
  
  // 4. Mint world NFT
  const worldNFT = await oasisNFT.mintAsset({
    title: world.name,
    symbol: `WORLD_${world.id}`,
    description: world.description,
    imageUrl: imageUrl,
    jsonMetaDataURL: worldDataUrl, // Full world data
    metadata: {
      worldId: world.id,
      creatorId: player.avatarId,
      blockCount: worldData.blocks.length,
      entityCount: worldData.entities.length,
      version: world.version,
      mintedAt: new Date().toISOString()
    },
    price: world.price || 0
  });
  
  return worldNFT;
}
```

### Pattern 4: Revenue Sharing (x402)

**Use Case:** Creator wants to share revenue with NFT holders.

```typescript
async function mintWithRevenueSharing(asset: Asset) {
  const nft = await oasisNFT.mintAsset({
    title: asset.name,
    symbol: asset.symbol,
    imageUrl: asset.imageUrl,
    // Enable x402 revenue sharing
    x402Enabled: true,
    x402PaymentEndpoint: "https://your-game.com/api/payments",
    x402RevenueModel: "equal", // or "weighted", "creator-split"
    x402TreasuryWallet: "your-treasury-wallet-address",
    metadata: {
      gameId: "hytopia-world-123",
      assetType: asset.type
    }
  });
  
  // Now when your game receives payments, they auto-distribute to NFT holders
  return nft;
}
```

---

## HYTOPIA Plugin Implementation

### Plugin Structure

```typescript
// @hytopia/oasis-nft/src/index.ts

export class OASISNFT {
  private apiUrl: string;
  private avatarId: string | null = null;
  private authToken: string | null = null;
  
  constructor(config: OASISConfig) {
    this.apiUrl = config.apiUrl;
  }
  
  /**
   * Authenticate player with OASIS
   */
  async authenticate(username: string, password: string): Promise<void> {
    const response = await fetch(`${this.apiUrl}/api/avatar/authenticate`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ username, password })
    });
    
    const data = await response.json();
    this.authToken = data.result.jwtToken;
    this.avatarId = data.result.avatarId;
  }
  
  /**
   * Mint an asset as NFT
   */
  async mintAsset(options: MintOptions): Promise<MintedNFT> {
    const response = await fetch(`${this.apiUrl}/api/nft/mint-nft`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        "Authorization": `Bearer ${this.authToken}`
      },
      body: JSON.stringify({
        Title: options.title,
        Symbol: options.symbol,
        Description: options.description,
        ImageUrl: options.imageUrl,
        JSONMetaDataURL: options.jsonMetaDataURL || options.imageUrl,
        OnChainProvider: options.onChainProvider || "SolanaOASIS",
        NFTStandardType: options.nftStandardType || "SPL",
        Price: options.price || 0,
        NumberToMint: options.numberToMint || 1,
        MetaData: options.metadata || {},
        SendToAvatarAfterMintingId: this.avatarId,
        // x402 revenue sharing
        X402Enabled: options.x402Enabled || false,
        X402PaymentEndpoint: options.x402PaymentEndpoint,
        X402RevenueModel: options.x402RevenueModel,
        X402TreasuryWallet: options.x402TreasuryWallet
      })
    });
    
    const data = await response.json();
    if (data.isError) {
      throw new Error(data.message);
    }
    
    return {
      id: data.result.id,
      nftTokenAddress: data.result.web3NFTs[0].nftTokenAddress,
      mintTransactionHash: data.result.web3NFTs[0].mintTransactionHash,
      imageUrl: data.result.imageUrl,
      title: data.result.title
    };
  }
  
  /**
   * Load all NFTs owned by player
   */
  async loadPlayerNFTs(): Promise<NFT[]> {
    const response = await fetch(
      `${this.apiUrl}/api/nft/load-all-nfts-for_avatar/${this.avatarId}`,
      {
        headers: {
          "Authorization": `Bearer ${this.authToken}`
        }
      }
    );
    
    const data = await response.json();
    return data.result || [];
  }
  
  /**
   * Upload file to IPFS via OASIS
   */
  async uploadToIPFS(file: File | Blob, mimeType?: string): Promise<string> {
    const formData = new FormData();
    formData.append("file", file);
    
    const response = await fetch(`${this.apiUrl}/api/file/upload`, {
      method: "POST",
      headers: {
        "Authorization": `Bearer ${this.authToken}`
      },
      body: formData
    });
    
    const data = await response.json();
    return data.result.ipfsUrl || data.result.url;
  }
}
```

---

## Integration Examples

### Example 1: Mint Custom Block

```typescript
// In your HYTOPIA world
import { OASISNFT } from "@hytopia/oasis-nft";

const oasis = new OASISNFT({
  apiUrl: "https://api.oasisweb4.com",
  avatarId: player.avatarId
});

await oasis.authenticate(player.username, player.password);

// Player creates custom block
world.onBlockCreated(async (block) => {
  if (block.creatorId === player.avatarId) {
    // Offer to mint as NFT
    const shouldMint = await world.ui.confirm(
      "Mint this block as an NFT?",
      "You can sell it in the marketplace!"
    );
    
    if (shouldMint) {
      const nft = await mintCustomBlock(block);
      world.ui.showNotification(`✅ Block minted! NFT: ${nft.nftTokenAddress}`);
    }
  }
});
```

### Example 2: NFT Marketplace in World

```typescript
// Create in-world NFT marketplace
class NFTMarketplace {
  async display() {
    const nfts = await oasis.loadPlayerNFTs();
    
    // Show owned NFTs
    const ownedNFTs = nfts.filter(nft => nft.currentOwnerAvatarId === player.avatarId);
    
    // Show marketplace listings
    const listings = nfts.filter(nft => nft.isForSale);
    
    world.ui.showMarketplace({
      owned: ownedNFTs,
      marketplace: listings,
      onBuy: async (nft) => {
        await oasis.purchaseNFT(nft.id, nft.price);
      },
      onSell: async (nft) => {
        const price = await world.ui.prompt("Set price (SOL):");
        await oasis.listForSale(nft.id, parseFloat(price));
      }
    });
  }
}
```

### Example 3: Persistent Asset System

```typescript
// Save player's custom assets as NFTs for persistence
async function savePlayerAssets() {
  const playerAssets = {
    customBlocks: player.getCustomBlocks(),
    customSkins: player.getCustomSkins(),
    customItems: player.getCustomItems()
  };
  
  // Mint as collection NFT
  const collectionNFT = await oasis.mintAsset({
    title: `${player.username}'s Asset Collection`,
    symbol: `ASSETS_${player.avatarId}`,
    description: "Persistent asset collection",
    imageUrl: await createCollectionPreview(playerAssets),
    metadata: {
      assets: playerAssets,
      playerId: player.avatarId,
      worldId: world.id
    }
  });
  
  // Assets are now persisted on-chain
  console.log(`Assets saved as NFT: ${collectionNFT.id}`);
}
```

---

## API Endpoints Used

### Authentication
- `POST /api/avatar/authenticate` - Get JWT token
- `POST /api/avatar/register` - Register new avatar

### NFT Operations
- `POST /api/nft/mint-nft` - Mint new NFT
- `GET /api/nft/load-all-nfts-for_avatar/{avatarId}` - Get player's NFTs
- `GET /api/nft/load-nft/{nftId}` - Get specific NFT
- `PUT /api/nft/load-nft/{nftId}` - Update NFT metadata

### File Upload
- `POST /api/file/upload` - Upload to IPFS/Pinata

### Wallet Operations
- `GET /api/wallet/avatar/{avatarId}/wallets` - Get player wallets
- `POST /api/wallet/create` - Create wallet

---

## Monetization Flows

### Flow 1: Creator Sells Asset

```
1. Creator builds asset in HYTOPIA
2. Creator mints asset as NFT (via OASIS)
3. NFT appears in marketplace
4. Buyer purchases NFT
5. Creator receives payment (SOL/ETH)
6. Asset ownership transfers to buyer
```

### Flow 2: Revenue Sharing

```
1. Creator mints asset with x402 enabled
2. Asset sold to multiple holders
3. Game receives payment for asset usage
4. Payment auto-distributes to all NFT holders
5. Creator + holders share revenue
```

### Flow 3: Royalties

```
1. Creator mints asset with royalty percentage
2. Asset sold on secondary market
3. Creator receives royalty on each sale
4. Royalty tracked on-chain
```

---

## Benefits for HYTOPIA Creators

### 1. **Real Money Earnings**
- Mint assets → Sell on marketplace → Earn SOL/ETH
- No platform fees (OASIS handles blockchain)
- Direct creator-to-buyer transactions

### 2. **IP Protection**
- Assets stored on blockchain (immutable)
- Provenance tracked (who created, when, where)
- Ownership verifiable on-chain

### 3. **Asset Persistence**
- Assets survive world resets
- Transferable across platforms
- Portable to other games/metaverses

### 4. **Monetization Options**
- One-time sales
- Royalties on resales
- Revenue sharing (x402)
- Subscription models (NFT-gated access)

---

## Implementation Checklist

### Phase 1: Basic Integration
- [ ] Create `@hytopia/oasis-nft` plugin package
- [ ] Implement authentication flow
- [ ] Implement `mintAsset()` function
- [ ] Implement `loadPlayerNFTs()` function
- [ ] Add IPFS upload support
- [ ] Create example world with NFT minting

### Phase 2: Marketplace
- [ ] Implement NFT marketplace UI
- [ ] Add buy/sell functionality
- [ ] Add price setting
- [ ] Display transaction history

### Phase 3: Advanced Features
- [ ] x402 revenue sharing integration
- [ ] Royalty configuration
- [ ] NFT-gated content access
- [ ] Collection management

---

## Next Steps

1. **Create HYTOPIA Plugin Package**
   - Publish `@hytopia/oasis-nft` to npm
   - Include TypeScript types
   - Add documentation and examples

2. **Build Example World**
   - Demo world showing NFT minting
   - Marketplace example
   - Asset persistence demo

3. **Documentation**
   - HYTOPIA-specific integration guide
   - API reference
   - Video tutorials

4. **Marketing**
   - "Earn Roblox-level money" messaging
   - Creator success stories
   - Revenue sharing examples

---

## References

- [HYTOPIA Developer Docs](https://dev.hytopia.com/)
- [OASIS NFT API Documentation](./NFT-API.md)
- [OASIS API Reference](./OASIS_API_Reference.md)
- [NFT Minting Guide](./SIMPLE_NFT_MINTING_GUIDE.md)
