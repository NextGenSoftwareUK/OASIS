# NFTs API

## 📋 **Table of Contents**

- [Overview](#overview)
- [NFT Management](#nft-management)
- [NFT Operations](#nft-operations)
- [NFT Marketplace](#nft-marketplace)
- [Error Responses](#error-responses)

## Overview

The NFTs API provides NFT management for the STAR ecosystem. It handles NFT creation, minting, trading, and metadata.

## NFT Management

### Get All NFTs
```http
GET /api/nfts
Authorization: Bearer YOUR_TOKEN
```

### Get NFT by ID
```http
GET /api/nfts/{nftId}
Authorization: Bearer YOUR_TOKEN
```

### Create NFT
```http
POST /api/nfts
Authorization: Bearer YOUR_TOKEN
```

### Update NFT
```http
PUT /api/nfts/{nftId}
Authorization: Bearer YOUR_TOKEN
```

### Delete NFT
```http
DELETE /api/nfts/{nftId}
Authorization: Bearer YOUR_TOKEN
```

## NFT Operations

### Mint NFT
```http
POST /api/nfts/{nftId}/mint
Authorization: Bearer YOUR_TOKEN
```

### Transfer NFT
```http
POST /api/nfts/{nftId}/transfer
Authorization: Bearer YOUR_TOKEN
```

### Burn NFT
```http
POST /api/nfts/{nftId}/burn
Authorization: Bearer YOUR_TOKEN
```

## NFT Marketplace

### List NFT for Sale
```http
POST /api/nfts/{nftId}/list
Authorization: Bearer YOUR_TOKEN
```

### Buy NFT
```http
POST /api/nfts/{nftId}/buy
Authorization: Bearer YOUR_TOKEN
```

### Get NFT Listings
```http
GET /api/nfts/marketplace
Authorization: Bearer YOUR_TOKEN
```

## Error Responses

### NFT Not Found
```json
{
  "result": null,
  "isError": true,
  "message": "NFT not found"
}
```

### Mint Failed
```json
{
  "result": null,
  "isError": true,
  "message": "NFT mint failed"
}
```

---

## Navigation

**← Previous:** [CelestialSpaces API](CelestialSpaces-API.md) | **Next:** [GeoNFTs API](GeoNFTs-API.md) →
