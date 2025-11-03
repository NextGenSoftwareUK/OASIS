# GeoNFTs API

## 📋 **Table of Contents**

- [Overview](#overview)
- [GeoNFT Management](#geonft-management)
- [GeoNFT Operations](#geonft-operations)
- [GeoNFT Location](#geonft-location)
- [Error Responses](#error-responses)

## Overview

The GeoNFTs API provides location-based NFT management for the STAR ecosystem. It handles geo-located NFTs with AR integration.

## GeoNFT Management

### Get All GeoNFTs
```http
GET /api/geonft
Authorization: Bearer YOUR_TOKEN
```

### Get GeoNFT by ID
```http
GET /api/geonft/{geoNftId}
Authorization: Bearer YOUR_TOKEN
```

### Create GeoNFT
```http
POST /api/geonft
Authorization: Bearer YOUR_TOKEN
```

### Update GeoNFT
```http
PUT /api/geonft/{geoNftId}
Authorization: Bearer YOUR_TOKEN
```

### Delete GeoNFT
```http
DELETE /api/geonft/{geoNftId}
Authorization: Bearer YOUR_TOKEN
```

## GeoNFT Operations

### Mint GeoNFT
```http
POST /api/geonft/{geoNftId}/mint
Authorization: Bearer YOUR_TOKEN
```

### Collect GeoNFT
```http
POST /api/geonft/{geoNftId}/collect
Authorization: Bearer YOUR_TOKEN
```

### Transfer GeoNFT
```http
POST /api/geonft/{geoNftId}/transfer
Authorization: Bearer YOUR_TOKEN
```

## GeoNFT Location

### Get Nearby GeoNFTs
```http
GET /api/geonft/nearby
Authorization: Bearer YOUR_TOKEN
```

### Get GeoNFT Location
```http
GET /api/geonft/{geoNftId}/location
Authorization: Bearer YOUR_TOKEN
```

### Update GeoNFT Location
```http
PUT /api/geonft/{geoNftId}/location
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
