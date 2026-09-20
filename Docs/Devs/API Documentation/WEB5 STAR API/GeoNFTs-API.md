# GeoNFTs API

> **Shared geospatial policy ownership:** GeoNFTs remain a distinct public API and the streamlined NFT placement/collection workflow. Their quantity, sharing, cooldown and respawn decisions delegate to `GeoSpatialSpawnPolicy`, the same invariant used by GeoHotSpots. GeoHotSpots may grant existing WEB4 GeoNFTs through `GeoNFTRewardIds`; that relationship does not convert the authored GeoNFT into a GeoHotSpot or replace any GeoNFT route.

## 📋 **Table of Contents**

- [Overview](#overview)
- [GeoNFT Management](#geonft-management)
- [GeoNFT Operations](#geonft-operations)
- [GeoNFT Location](#geonft-location)
- [Error Responses](#error-responses)

## Overview

The GeoNFTs API provides location-based NFT management for the STAR ecosystem. It handles geo-located NFTs with AR integration.

The WEB5 `STARGeoNFT` is a STARNET Smartbrick wrapper around a WEB4 GeoNFT; the WEB4 GeoNFT in turn extends a WEB4 NFT and carries its Web3 NFT representations. WEB5 adds publishing, discovery, versioning, installation, and composition without replacing the wrapped WEB4 identity. See [NFT and GeoNFT architecture across Web3, WEB4, and WEB5](../../NFT_GEONFT_WEB4_WEB5_ARCHITECTURE.md) for the complete type and collection model and the Our World merge rules.

## GeoNFT Management

### Get All GeoNFTs
```http
GET /api/geonfts
Authorization: Bearer YOUR_TOKEN
```

### Get the authenticated avatar's GeoNFT wrappers
```http
GET /api/geonfts/load-all-for-avatar?showAllVersions=false&version=0
Authorization: Bearer YOUR_TOKEN
```

This returns WEB5 `STARGeoNFT` records. `geoNFTId` is the canonical link to the wrapped WEB4 GeoNFT. Clients that display playable placements must resolve that ID through WEB4 and deduplicate by the WEB4 ID.

### Get GeoNFT by ID
```http
GET /api/geonfts/{web5GeoNftId}
Authorization: Bearer YOUR_TOKEN
```

### Create GeoNFT
```http
POST /api/geonfts
Authorization: Bearer YOUR_TOKEN
```

### Update GeoNFT
```http
PUT /api/geonfts/{web5GeoNftId}
Authorization: Bearer YOUR_TOKEN
```

### Delete GeoNFT
```http
DELETE /api/geonfts/{web5GeoNftId}
Authorization: Bearer YOUR_TOKEN
```

## GeoNFT Operations

Minting and collecting the geographic asset use the WEB4 NFT/GeoNFT API and the wrapped WEB4 `geoNFTId`. The WEB5 controller manages the STARNET wrapper and its package lifecycle:

```http
POST /api/geonfts/{web5GeoNftId}/publish
POST /api/geonfts/{web5GeoNftId}/download
GET  /api/geonfts/{web5GeoNftId}/versions
GET  /api/geonfts/{web5GeoNftId}/version/{version}
POST /api/geonfts/{web5GeoNftId}/edit
POST /api/geonfts/{web5GeoNftId}/unpublish
POST /api/geonfts/{web5GeoNftId}/republish
POST /api/geonfts/{web5GeoNftId}/activate
POST /api/geonfts/{web5GeoNftId}/deactivate
Authorization: Bearer YOUR_TOKEN
```

## GeoNFT Location

### Get Nearby GeoNFTs
```http
GET /api/geonfts/nearby?latitude=51.5074&longitude=-0.1278&radiusKm=10
Authorization: Bearer YOUR_TOKEN
```

### Get GeoNFTs by avatar
```http
GET /api/geonfts/by-avatar/{avatarId}
Authorization: Bearer YOUR_TOKEN
```

### Search GeoNFT wrappers
```http
GET /api/geonfts/search?query=tree
Authorization: Bearer YOUR_TOKEN
```

## Error Responses

### GeoNFT Not Found
```json
{
  "result": null,
  "isError": true,
  "message": "GeoNFT not found"
}
```

### Location Invalid
```json
{
  "result": null,
  "isError": true,
  "message": "Invalid location"
}
```

---

## Navigation

**← Previous:** [NFTs API](NFTs-API.md) | **Next:** [GeoHotSpots API](GeoHotSpots-API.md) →
