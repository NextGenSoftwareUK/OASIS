# GeoNFT Placement via MCP

## Overview

GeoNFTs allow you to place (geocache) NFTs at real-world coordinates. Once placed, they can be discovered and collected by other players (if configured to allow it).

## How GeoNFTs Work

### Portal Interface
- **Location**: STAR tab → "Mint GeoNFT" button
- **Map**: Interactive Leaflet.js map (defaults to London)
- **Selection**: Click on map, search address, or enter coordinates
- **Coordinates**: Uses standard degrees (e.g., `51.5074, -0.1278`)
- **API**: Converts to micro-degrees (multiply by 1,000,000) automatically

### Backend API
- **Endpoint**: `POST /api/nft/place-geo-nft`
- **Coordinates**: Stored as micro-degrees (long integers)
- **Authentication**: Requires JWT token
- **Ownership**: You must own the NFT to place it

### Request Format
```json
{
  "originalOASISNFTId": "uuid",
  "lat": 51507400,  // 51.5074 * 1,000,000 (micro-degrees)
  "long": -127800,  // -0.1278 * 1,000,000 (micro-degrees)
  "allowOtherPlayersToAlsoCollect": true,
  "permSpawn": false,
  "globalSpawnQuantity": 1,
  "playerSpawnQuantity": 1,
  "respawnDurationInSeconds": 0,
  "geoNFTMetaDataProvider": "MongoDBOASIS",
  "originalOASISNFTOffChainProvider": "MongoDBOASIS"
}
```

## MCP Tool: `oasis_place_geo_nft`

### Usage

**Tool Name:** `oasis_place_geo_nft`

**Natural Language Examples:**
- "Place NFT abc-123 at coordinates 51.5074, -0.1278"
- "Geocache my NFT at London coordinates"
- "Place NFT at latitude 40.7128, longitude -74.0060" (New York)

### Parameters

**Required:**
- `originalOASISNFTId` (string): The OASIS NFT ID to place (must be owned by you)
- `latitude` (number): Latitude in degrees (e.g., `51.5074`)
- `longitude` (number): Longitude in degrees (e.g., `-0.1278`)

**Optional:**
- `allowOtherPlayersToAlsoCollect` (boolean): Allow others to collect (default: `true`)
- `permSpawn` (boolean): Permanent spawn (default: `false`)
- `globalSpawnQuantity` (number): Global spawn quantity (default: `1`)
- `playerSpawnQuantity` (number): Player spawn quantity (default: `1`)
- `respawnDurationInSeconds` (number): Respawn duration (default: `0`)
- `geoNFTMetaDataProvider` (string): Metadata provider (default: `MongoDBOASIS`)
- `originalOASISNFTOffChainProvider` (string): Original NFT provider (default: `MongoDBOASIS`)

### Coordinate Conversion

The MCP tool automatically converts degrees to micro-degrees:
- Input: `51.5074` (degrees)
- Output: `51507400` (micro-degrees)
- Formula: `degrees * 1,000,000`

### Example MCP Call

```typescript
oasis_place_geo_nft({
  originalOASISNFTId: "abc-123-def-456",
  latitude: 51.5074,      // London
  longitude: -0.1278,
  allowOtherPlayersToAlsoCollect: true
})
```

## Complete Workflow

### 1. Mint an NFT
```
Mint a Solana NFT with name "My Geocache NFT"
```

### 2. Get the NFT ID
The mint response will include the NFT ID.

### 3. Place at Coordinates
```
Place NFT {nftId} at coordinates 51.5074, -0.1278
```

### 4. View on Map
- Go to STAR tab in portal
- View GeoNFT Map
- Your placed NFT will appear at the coordinates

## Common Coordinates

| Location | Latitude | Longitude |
|----------|----------|-----------|
| London | 51.5074 | -0.1278 |
| New York | 40.7128 | -74.0060 |
| Tokyo | 35.6762 | 139.6503 |
| Paris | 48.8566 | 2.3522 |
| San Francisco | 37.7749 | -122.4194 |

## Technical Details

### Coordinate System
- **Portal/User Input**: Standard degrees (decimal)
- **API Storage**: Micro-degrees (multiply by 1,000,000)
- **Precision**: ~0.1 meter at equator (micro-degree precision)

### GeoNFT Properties
- **Permanent**: Once placed, GeoNFTs persist (unless `permSpawn: false` and respawn configured)
- **Discoverable**: Can be found by other players (if `allowOtherPlayersToAlsoCollect: true`)
- **Collectible**: Players can collect GeoNFTs they discover
- **Spawn System**: Supports respawn mechanics for game-like experiences

### API Endpoints
- `POST /api/nft/place-geo-nft` - Place existing NFT
- `POST /api/nft/mint-and-place-geo-nft` - Mint and place in one step
- `GET /api/nft/load-all-geo-nfts-for-avatar/{avatarId}` - Get all GeoNFTs for avatar
- `GET /api/nft/load-all-geo-nfts` - Get all GeoNFTs (admin only)

## Next Steps

After placing a GeoNFT:
1. It appears on the GeoNFT Map in the portal
2. Other players can discover it (if allowed)
3. Players can collect it (if allowed)
4. You can view all your placed GeoNFTs via `oasis_get_geo_nfts`

## Notes

- You must own the NFT to place it
- Authentication is required (JWT token)
- Coordinates are validated by the backend
- Multiple GeoNFTs can be placed at the same location
- GeoNFTs are stored in the OASIS database and can be queried by location
