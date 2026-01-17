# NFT Minting Fields Reference

Complete reference of all fields available when minting NFTs via the interactive MCP flow.

## Field Categories

Fields are organized by priority: **Required** → **Recommended** → **Optional**

---

## Required Fields

These fields must be provided before minting can proceed.

| Field | Type | Description | Example |
|-------|------|-------------|---------|
| `Symbol` | string | Short ticker/symbol for your NFT | `"MYNFT"`, `"ART123"` |
| `JSONMetaDataURL` | string | URL to metadata JSON file | `"https://jsonplaceholder.typicode.com/posts/1"` |

---

## Recommended Fields

Highly recommended for a complete NFT. You'll be prompted for these if not provided.

| Field | Type | Description | Example | Default |
|-------|------|-------------|---------|---------|
| `NumberToMint` | number | How many NFTs to create | `1`, `5`, `100` | `1` |
| `Title` | string | Name/title of your NFT | `"My Awesome Artwork"` | `"Untitled NFT"` |
| `ImageUrl` | string | URL to the NFT image | `"http://localhost:8000/image.png"` | Uses JSONMetaDataURL |

---

## Optional Fields

These enhance your NFT but aren't required. You'll be asked if you want to provide them.

### Basic Information

| Field | Type | Description | Example |
|-------|------|-------------|---------|
| `Description` | string | Detailed description of your NFT | `"A beautiful digital artwork..."` |
| `Price` | number | Initial sale price (in SOL) | `0.5`, `10` | `0` (free) |

### Metadata & Attributes

| Field | Type | Description | Example |
|-------|------|-------------|---------|
| `MetaData.attributes` | array | Custom traits/attributes | `[{"trait_type": "Color", "value": "Blue"}]` |
| `MetaData.external_url` | string | Link to project/collection page | `"https://myproject.com"` |
| `MetaData.category` | string | Content category | `"image"`, `"video"`, `"audio"`, `"3d"` |
| `MetaData.animation_url` | string | URL to animated content (GIF/video) | `"https://example.com/animation.mp4"` |

### Advanced Options

| Field | Type | Description | Example | Default |
|-------|------|-------------|---------|---------|
| `ThumbnailUrl` | string | Thumbnail image URL | `"https://example.com/thumb.png"` | - |
| `OnChainProvider` | string | Blockchain provider | `"SolanaOASIS"` | `"SolanaOASIS"` |
| `OffChainProvider` | string | Off-chain storage provider | `"MongoDBOASIS"` | `"MongoDBOASIS"` |
| `NFTStandardType` | string | NFT standard | `"SPL"` | `"SPL"` |
| `StoreNFTMetaDataOnChain` | boolean | Store metadata on-chain | `true`, `false` | `false` |

---

## Field Priority in Interactive Flow

When you say "Mint a Solana NFT", you'll be prompted in this order:

1. **Required Fields** (must provide)
   - Symbol
   - JSONMetaDataURL

2. **Recommended Fields** (highly suggested)
   - NumberToMint
   - Title
   - ImageUrl

3. **Optional Fields** (nice to have)
   - Description
   - Attributes/Traits
   - External URL
   - Category
   - Price

---

## Examples

### Minimal NFT
```json
{
  "Symbol": "TEST",
  "JSONMetaDataURL": "https://jsonplaceholder.typicode.com/posts/1"
}
```

### Complete NFT
```json
{
  "Symbol": "MYNFT",
  "JSONMetaDataURL": "https://example.com/metadata.json",
  "Title": "Digital Masterpiece",
  "Description": "A beautiful digital artwork",
  "NumberToMint": 1,
  "ImageUrl": "https://example.com/image.png",
  "Price": 0.5,
  "MetaData": {
    "attributes": [
      {"trait_type": "Color", "value": "Blue"},
      {"trait_type": "Rarity", "value": "Legendary"}
    ],
    "external_url": "https://myproject.com",
    "category": "image"
  }
}
```

### Batch Minting
```json
{
  "Symbol": "COLLECTION",
  "JSONMetaDataURL": "https://example.com/metadata.json",
  "Title": "Collection Item",
  "NumberToMint": 10,
  "ImageUrl": "https://example.com/image.png"
}
```

---

## Standards & Best Practices

### Symbol Format
- Keep it short (3-10 characters)
- Alphanumeric only
- Uppercase recommended
- Examples: `"MYNFT"`, `"ART123"`, `"COLLECTION"`

### Attributes/Traits Format
Follow OpenSea/Metaplex standard:
```json
{
  "attributes": [
    {
      "trait_type": "Background",
      "value": "Blue"
    },
    {
      "trait_type": "Rarity",
      "value": "Legendary"
    },
    {
      "trait_type": "Artist",
      "value": "Your Name"
    }
  ]
}
```

### Categories
Standard categories:
- `"image"` - Static images (PNG, JPG)
- `"video"` - Video files (MP4, WebM)
- `"audio"` - Audio files (MP3, WAV)
- `"3d"` - 3D models (GLB, GLTF)
- `"interactive"` - Interactive HTML/WebGL

### Image URLs
- **Local**: `http://localhost:8000/your-image.png` (requires server running)
- **Public**: Any publicly accessible image URL
- **IPFS**: `https://ipfs.io/ipfs/Qm...`
- **Placeholder**: `https://via.placeholder.com/512` (for testing)

---

## Research Sources

Based on:
- **Metaplex JSON Schema** - Solana NFT metadata standard
- **OpenSea Metadata Standards** - Widely adopted NFT metadata format
- **Solana SPL Token Metadata** - On-chain metadata requirements
- **Community Best Practices** - Real-world NFT creation patterns
