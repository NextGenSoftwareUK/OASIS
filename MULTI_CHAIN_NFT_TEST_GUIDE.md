# Testing Multi-Chain Web4 NFTs - Complete Guide

## Overview

David's latest implementation allows you to create a **single Web4 OASIS NFT** that wraps **multiple Web3 NFTs** on different blockchains. Each blockchain variant can:
- Share the same parent metadata
- Override specific properties to create unique variants
- Maintain the same core NFT identity across chains

## Architecture

```
Web4 OASIS NFT (Parent)
├── Metadata: { title, description, image, price, etc. }
├── 
├── Web3 NFT Variant #1 (Solana)
│   ├── Inherits parent metadata
│   ├── Override: price = 0.5 SOL
│   └── OnChainProvider: SolanaOASIS
│
├── Web3 NFT Variant #2 (Ethereum)
│   ├── Inherits parent metadata
│   ├── Override: price = 0.01 ETH
│   └── OnChainProvider: EthereumOASIS
│
├── Web3 NFT Variant #3 (Polygon)
│   ├── Inherits parent metadata
│   ├── Override: different image, lower price
│   └── OnChainProvider: PolygonOASIS
│
└── Web3 NFT Variant #4 (Arbitrum)
    ├── Inherits parent metadata
    └── OnChainProvider: ArbitrumOASIS
```

## Prerequisites

1. **OASIS API Running** on port 5003
2. **Avatar Account** with authentication token
3. **Provider Configurations** in OASIS_DNA.json for:
   - SolanaOASIS (devnet)
   - ArbitrumOASIS (testnet)
   - PolygonOASIS (testnet)
   - EthereumOASIS (testnet)

## Step 1: Start the OASIS API

```bash
cd /Volumes/Storage/OASIS_CLEAN/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI
dotnet run
```

API will be available at: `http://localhost:5003`

## Step 2: Authenticate

First, get an authentication token:

```bash
curl -X POST http://localhost:5003/api/avatar/authenticate \
  -H "Content-Type: application/json" \
  -d '{
    "username": "your_username",
    "password": "your_password"
  }'
```

Save the `token` from the response.

## Step 3: Mint a Multi-Chain Web4 NFT

### Example 1: Simple Multi-Chain NFT (Same metadata on all chains)

```json
POST http://localhost:5003/api/nft/mint-nft
Authorization: Bearer YOUR_TOKEN_HERE
Content-Type: application/json

{
  "title": "My First Multi-Chain NFT",
  "description": "This NFT exists on Solana, Arbitrum, and Polygon simultaneously!",
  "imageUrl": "https://example.com/my-nft-image.png",
  "price": 1.0,
  "discount": 0,
  "royaltyPercentage": 10,
  
  // Default settings for all Web3 NFTs
  "onChainProvider": "SolanaOASIS",
  "offChainProvider": "IPFSOASIS", 
  "nftStandardType": "ERC1155",
  "nftOffChainMetaType": "OASIS",
  "storeNFTMetaDataOnChain": false,
  "numberToMint": 1,
  
  // Define variants for different chains
  "web3NFTs": [
    {
      "onChainProvider": "SolanaOASIS",
      "price": 0.5,
      "nftMetaDataMergeStrategy": "Merge"
    },
    {
      "onChainProvider": "ArbitrumOASIS", 
      "price": 0.01,
      "nftMetaDataMergeStrategy": "Merge"
    },
    {
      "onChainProvider": "PolygonOASIS",
      "price": 0.05,
      "nftMetaDataMergeStrategy": "Merge"
    }
  ]
}
```

### Example 2: Advanced Multi-Chain NFT (Unique variants per chain)

```json
POST http://localhost:5003/api/nft/mint-nft
Authorization: Bearer YOUR_TOKEN_HERE
Content-Type: application/json

{
  // Parent Web4 NFT metadata
  "title": "Cosmic Dragon Collection",
  "description": "Epic dragon NFT with unique variants on each blockchain",
  "imageUrl": "https://example.com/dragon-base.png",
  "price": 10.0,
  "discount": 0,
  "royaltyPercentage": 15,
  "tags": ["dragon", "fantasy", "multichain", "web4"],
  "metaData": {
    "artist": "CryptoArtist",
    "collection": "Cosmic Dragons",
    "rarity": "legendary",
    "power": 9000
  },
  
  // Global defaults
  "onChainProvider": "SolanaOASIS",
  "offChainProvider": "IPFSOASIS",
  "nftStandardType": "ERC721",
  "nftOffChainMetaType": "OASIS",
  "storeNFTMetaDataOnChain": false,
  
  // Unique variants per chain
  "web3NFTs": [
    {
      // Solana variant - Fire Dragon
      "onChainProvider": "SolanaOASIS",
      "title": "Cosmic Dragon - Fire Variant",
      "description": "Fire-breathing Solana dragon",
      "imageUrl": "https://example.com/dragon-fire.png",
      "price": 2.5,
      "metaData": {
        "element": "fire",
        "color": "red",
        "special_ability": "inferno_blast"
      },
      "tags": ["fire", "solana"],
      "nftMetaDataMergeStrategy": "MergeAndOverwrite",
      "nftTagsMergeStrategy": "Merge"
    },
    {
      // Arbitrum variant - Ice Dragon
      "onChainProvider": "ArbitrumOASIS",
      "title": "Cosmic Dragon - Ice Variant", 
      "description": "Frost-powered Arbitrum dragon",
      "imageUrl": "https://example.com/dragon-ice.png",
      "price": 0.05,
      "metaData": {
        "element": "ice",
        "color": "blue",
        "special_ability": "arctic_freeze"
      },
      "tags": ["ice", "arbitrum"],
      "nftMetaDataMergeStrategy": "MergeAndOverwrite",
      "nftTagsMergeStrategy": "Merge"
    },
    {
      // Polygon variant - Lightning Dragon
      "onChainProvider": "PolygonOASIS",
      "title": "Cosmic Dragon - Lightning Variant",
      "description": "Electric Polygon dragon",
      "imageUrl": "https://example.com/dragon-lightning.png", 
      "price": 0.5,
      "metaData": {
        "element": "lightning",
        "color": "yellow",
        "special_ability": "thunder_strike"
      },
      "tags": ["lightning", "polygon"],
      "nftMetaDataMergeStrategy": "MergeAndOverwrite",
      "nftTagsMergeStrategy": "Merge"
    }
  ]
}
```

## Step 4: Query Your Multi-Chain NFT

After minting, you'll receive a Web4 NFT with an ID. You can load it to see all variants:

```bash
curl -X GET "http://localhost:5003/api/nft/load-nft-by-id/{nft_id}" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

**Response structure:**
```json
{
  "result": {
    "id": "guid-here",
    "title": "Cosmic Dragon Collection",
    "description": "Epic dragon NFT...",
    "imageUrl": "https://example.com/dragon-base.png",
    "price": 10.0,
    "metaData": { ... },
    "tags": ["dragon", "fantasy", "multichain", "web4"],
    
    // All Web3 NFT variants
    "web3NFTs": [
      {
        "title": "Cosmic Dragon - Fire Variant",
        "onChainProvider": "SolanaOASIS",
        "mintTransactionHash": "solana_tx_hash",
        "nftTokenAddress": "solana_mint_address",
        "price": 2.5,
        "metaData": {
          "artist": "CryptoArtist",  // inherited from parent
          "collection": "Cosmic Dragons",  // inherited
          "rarity": "legendary",  // inherited
          "power": 9000,  // inherited
          "element": "fire",  // unique to this variant
          "color": "red",  // unique
          "special_ability": "inferno_blast"  // unique
        }
      },
      {
        "title": "Cosmic Dragon - Ice Variant",
        "onChainProvider": "ArbitrumOASIS",
        "mintTransactionHash": "arbitrum_tx_hash",
        "price": 0.05,
        "metaData": { ... }
      },
      {
        "title": "Cosmic Dragon - Lightning Variant",
        "onChainProvider": "PolygonOASIS",
        "mintTransactionHash": "polygon_tx_hash",
        "price": 0.5,
        "metaData": { ... }
      }
    ]
  },
  "isError": false,
  "message": "NFT loaded successfully"
}
```

## Metadata Merge Strategies

### 1. **Merge** (Default)
- Keeps parent values if key exists
- Only adds new keys from child
- Parent metadata takes precedence

### 2. **MergeAndOverwrite**
- Child values override parent values
- Combines both metadata dictionaries
- Child metadata takes precedence

### 3. **Replace**
- Completely replaces parent metadata
- Uses only child metadata
- Parent metadata ignored

## Use Cases

### 1. **Same NFT, Different Chains**
Perfect for NFTs that should exist identically on multiple chains:
- Same artwork, same metadata
- Different prices per chain (gas optimization)
- Shared across Solana (fast), Ethereum (established), Polygon (cheap)

### 2. **Variant Collections**
Create themed variants of the same base NFT:
- Pokemon-style elemental variants
- Color variants (Red Dragon on Solana, Blue Dragon on Arbitrum)
- Rarity tiers (Common on Polygon, Rare on Ethereum)

### 3. **Cross-Chain Trading**
- User buys on Solana (cheap minting)
- Redeems on Ethereum (established marketplace)
- Same NFT identity across chains

### 4. **Chain-Specific Features**
- Different utilities per chain
- Solana variant: in-game item
- Ethereum variant: governance rights
- Polygon variant: staking rewards

## Testing Checklist

- [ ] Start OASIS API
- [ ] Authenticate and get token
- [ ] Mint simple multi-chain NFT (2-3 chains)
- [ ] Verify all Web3 NFTs were minted
- [ ] Check transaction hashes on block explorers
- [ ] Load NFT and inspect Web3NFTs array
- [ ] Test with metadata overrides
- [ ] Test different merge strategies
- [ ] Query all NFTs for avatar
- [ ] Send multi-chain NFT to another address

## Available Providers

Based on OASIS configuration:
- ✅ **SolanaOASIS** (devnet)
- ✅ **ArbitrumOASIS** (sepolia testnet)
- ✅ **PolygonOASIS** (amoy testnet)  
- ✅ **EthereumOASIS** (testnet)
- ⚠️ **TelosOASIS** (needs provider reference)

## Tips

1. **Start Simple:** Test with 2 chains first (Solana + Arbitrum)
2. **Use Testnets:** All providers should point to testnets
3. **Check Balances:** Ensure your wallets have testnet tokens
4. **Monitor Logs:** Watch API console for minting progress
5. **Verify On-Chain:** Check block explorers to verify NFTs were actually minted

## Next Steps

After successful testing, you can:
1. Add more blockchain providers
2. Import existing Web3 NFTs into Web4 wrapper
3. Build marketplace for multi-chain trading
4. Create collection management tools
5. Integrate with your HyperDrive platform

## Troubleshooting

**NFT minting fails:**
- Check provider configurations in OASIS_DNA.json
- Verify wallet has sufficient testnet tokens
- Check API logs for specific errors

**Web3NFT not appearing:**
- Ensure WaitTillNFTMinted is true
- Increase WaitForNFTToMintInSeconds
- Check blockchain explorer for transaction

**Authentication errors:**
- Get fresh token (tokens expire)
- Verify avatar exists in database
- Check OASIS_DNA.json for correct settings

