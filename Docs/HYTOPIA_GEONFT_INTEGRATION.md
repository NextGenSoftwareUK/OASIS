# HYTOPIA + OASIS GeoNFT Integration Guide

**Date:** January 22, 2026  
**Status:** 💡 Integration Proposal  
**Goal:** Enable GeoNFT placement in HYTOPIA worlds using real-world coordinates

---

## Overview

OASIS GeoNFTs use **real-world coordinates** (latitude/longitude) to place NFTs at specific locations. These can be integrated into HYTOPIA worlds by:

1. **Mapping real-world coordinates to HYTOPIA world coordinates**
2. **Querying nearby GeoNFTs** based on player location
3. **Rendering GeoNFTs** as in-world objects/entities
4. **Enabling collection/interaction** with placed GeoNFTs

---

## How GeoNFTs Work

### Coordinate System

- **Input Format**: Standard degrees (e.g., `51.5074, -0.1278` for London)
- **Storage Format**: Micro-degrees (multiply by 1,000,000)
- **Precision**: ~0.1 meter at equator
- **API Endpoint**: `POST /api/nft/place-geo-nft`

### GeoNFT Properties

```typescript
interface GeoNFT {
  id: string;
  title: string;
  description: string;
  imageUrl: string;
  lat: number;        // Micro-degrees (e.g., 51507400)
  long: number;       // Micro-degrees (e.g., -127800)
  allowOtherPlayersToAlsoCollect: boolean;
  permSpawn: boolean;
  globalSpawnQuantity: number;
  playerSpawnQuantity: number;
  respawnDurationInSeconds: number;
}
```

---

## Integration Architecture

```
┌─────────────────────────────────────────────────────────┐
│              HYTOPIA World (TypeScript)                 │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  ┌──────────────────────────────────────────────────┐  │
│  │      OASIS GeoNFT Plugin                        │  │
│  │  - getNearbyGeoNFTs(lat, lng, radius)          │  │
│  │  - placeGeoNFT(nftId, lat, lng)                │  │
│  │  - convertLatLngToWorldCoords(lat, lng)        │  │
│  │  - spawnGeoNFTInWorld(geoNFT, position)        │  │
│  └──────────────────────────────────────────────────┘  │
│                          │                              │
│                          ▼                              │
│  ┌──────────────────────────────────────────────────┐  │
│  │      OASIS REST API                              │  │
│  │  GET  /api/nft/get-nearby-geo-nfts              │  │
│  │  POST /api/nft/place-geo-nft                    │  │
│  │  GET  /api/nft/load-all-geo-nfts-for-avatar/{id}│  │
│  └──────────────────────────────────────────────────┘  │
│                          │                              │
└──────────────────────────┼──────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────┐
│              OASIS API (ONODE Backend)                  │
├─────────────────────────────────────────────────────────┤
│  - GeoNFT Storage (lat/long in micro-degrees)          │
│  - Nearby GeoNFT Queries (radius-based)                │
│  - Collection/Interaction Tracking                     │
└─────────────────────────────────────────────────────────┘
```

---

## HYTOPIA Plugin Implementation

### 1. Install OASIS GeoNFT Plugin

```typescript
// In your HYTOPIA world's package.json
{
  "dependencies": {
    "@hytopia/oasis-geonft": "^1.0.0"
  }
}
```

### 2. Initialize GeoNFT Client

```typescript
import { OASISGeoNFT } from "@hytopia/oasis-geonft";

const geoNFT = new OASISGeoNFT({
  apiUrl: "https://api.oasisweb4.com",
  avatarId: player.avatarId,
  authToken: player.authToken
});
```

---

## Core Integration Patterns

### Pattern 1: Map Real-World Coordinates to HYTOPIA World

**Challenge**: HYTOPIA uses voxel coordinates (x, y, z), while GeoNFTs use lat/long.

**Solution**: Create a coordinate mapping system.

```typescript
/**
 * Convert real-world coordinates to HYTOPIA world coordinates
 * 
 * Options:
 * 1. Use world's origin as a reference point
 * 2. Map entire world to a geographic region
 * 3. Use relative positioning from player spawn
 */
class GeoCoordinateMapper {
  // Option 1: World origin as reference point
  private worldOriginLat: number = 51.5074; // London (or your world's center)
  private worldOriginLng: number = -0.1278;
  private worldOriginX: number = 0; // HYTOPIA world origin
  private worldOriginZ: number = 0;
  
  // Scale factor: 1 degree ≈ 111 km
  // For HYTOPIA: decide how many blocks = 1 km
  private blocksPerKm: number = 100; // Adjust based on your world scale
  
  /**
   * Convert lat/long to HYTOPIA world coordinates
   */
  convertLatLngToWorldCoords(lat: number, lng: number): { x: number, z: number } {
    // Convert from micro-degrees to degrees
    const latDegrees = lat / 1000000;
    const lngDegrees = lng / 1000000;
    
    // Calculate distance from world origin
    const latDiff = latDegrees - this.worldOriginLat;
    const lngDiff = lngDegrees - this.worldOriginLng;
    
    // Convert to kilometers
    const latKm = latDiff * 111; // 1 degree latitude ≈ 111 km
    const lngKm = lngDiff * 111 * Math.cos(this.worldOriginLat * Math.PI / 180);
    
    // Convert to HYTOPIA blocks
    const x = this.worldOriginX + (lngKm * this.blocksPerKm);
    const z = this.worldOriginZ + (latKm * this.blocksPerKm);
    
    return { x: Math.round(x), z: Math.round(z) };
  }
  
  /**
   * Convert HYTOPIA world coordinates to lat/long
   */
  convertWorldCoordsToLatLng(x: number, z: number): { lat: number, lng: number } {
    // Reverse the conversion
    const lngKm = (x - this.worldOriginX) / this.blocksPerKm;
    const latKm = (z - this.worldOriginZ) / this.blocksPerKm;
    
    const lngDegrees = this.worldOriginLng + (lngKm / (111 * Math.cos(this.worldOriginLat * Math.PI / 180)));
    const latDegrees = this.worldOriginLat + (latKm / 111);
    
    return {
      lat: Math.round(latDegrees * 1000000), // Convert to micro-degrees
      lng: Math.round(lngDegrees * 1000000)
    };
  }
}

const geoMapper = new GeoCoordinateMapper();
```

### Pattern 2: Load and Display Nearby GeoNFTs

**Use Case**: Show GeoNFTs near the player's location in the HYTOPIA world.

```typescript
import { OASISGeoNFT } from "@hytopia/oasis-geonft";

class GeoNFTManager {
  private geoNFT: OASISGeoNFT;
  private geoMapper: GeoCoordinateMapper;
  private activeGeoNFTs: Map<string, Entity> = new Map();
  
  constructor() {
    this.geoNFT = new OASISGeoNFT({
      apiUrl: "https://api.oasisweb4.com",
      avatarId: player.avatarId,
      authToken: player.authToken
    });
    this.geoMapper = new GeoCoordinateMapper();
  }
  
  /**
   * Load nearby GeoNFTs based on player's world position
   */
  async loadNearbyGeoNFTs(playerPosition: { x: number, z: number }, radiusKm: number = 1) {
    // Convert player's world position to lat/long
    const playerCoords = this.geoMapper.convertWorldCoordsToLatLng(
      playerPosition.x,
      playerPosition.z
    );
    
    // Query nearby GeoNFTs
    const nearbyNFTs = await this.geoNFT.getNearbyGeoNFTs({
      latitude: playerCoords.lat / 1000000, // Convert to degrees
      longitude: playerCoords.lng / 1000000,
      radiusKm: radiusKm
    });
    
    // Spawn GeoNFTs in world
    for (const nft of nearbyNFTs) {
      await this.spawnGeoNFTInWorld(nft);
    }
  }
  
  /**
   * Spawn a GeoNFT as an entity in the HYTOPIA world
   */
  async spawnGeoNFTInWorld(geoNFT: GeoNFT) {
    // Convert lat/long to world coordinates
    const worldCoords = this.geoMapper.convertLatLngToWorldCoords(
      geoNFT.lat,
      geoNFT.long
    );
    
    // Create entity at that position
    const entity = world.createEntity({
      position: { x: worldCoords.x, y: 0, z: worldCoords.z },
      model: geoNFT.imageUrl, // Use NFT image as texture/model
      name: geoNFT.title,
      metadata: {
        geoNFTId: geoNFT.id,
        description: geoNFT.description,
        allowCollect: geoNFT.allowOtherPlayersToAlsoCollect
      }
    });
    
    // Add interaction handler
    entity.onInteract = async (player) => {
      await this.handleGeoNFTInteraction(entity, player);
    };
    
    this.activeGeoNFTs.set(geoNFT.id, entity);
  }
  
  /**
   * Handle player interaction with GeoNFT
   */
  async handleGeoNFTInteraction(entity: Entity, player: Player) {
    const geoNFT = entity.metadata;
    
    // Show NFT details
    world.ui.showModal({
      title: geoNFT.name,
      description: geoNFT.description,
      image: geoNFT.imageUrl,
      actions: [
        {
          label: "Collect",
          onClick: async () => {
            if (geoNFT.allowCollect) {
              await this.collectGeoNFT(geoNFT.geoNFTId, player);
            } else {
              world.ui.showNotification("This GeoNFT is not collectible");
            }
          }
        },
        {
          label: "View on Map",
          onClick: () => {
            // Open OASIS portal map view
            window.open(`https://portal.oasisweb4.com/star?geonft=${geoNFT.geoNFTId}`);
          }
        }
      ]
    });
  }
  
  /**
   * Collect a GeoNFT (transfer ownership to player)
   */
  async collectGeoNFT(geoNFTId: string, player: Player) {
    const result = await this.geoNFT.collectGeoNFT(geoNFTId);
    
    if (result.success) {
      world.ui.showNotification(`✅ Collected ${result.title}!`);
      
      // Remove from world (or mark as collected)
      const entity = this.activeGeoNFTs.get(geoNFTId);
      if (entity) {
        entity.destroy();
        this.activeGeoNFTs.delete(geoNFTId);
      }
    }
  }
}
```

### Pattern 3: Place GeoNFT from HYTOPIA World

**Use Case**: Creator places an NFT at their current world position.

```typescript
/**
 * Place an NFT as GeoNFT at current world position
 */
async function placeGeoNFTAtCurrentPosition(nftId: string) {
  // Get player's current world position
  const playerPos = player.position;
  
  // Convert to lat/long
  const geoCoords = geoMapper.convertWorldCoordsToLatLng(
    playerPos.x,
    playerPos.z
  );
  
  // Place GeoNFT via OASIS API
  const result = await geoNFT.placeGeoNFT({
    originalOASISNFTId: nftId,
    latitude: geoCoords.lat / 1000000, // Convert to degrees
    longitude: geoCoords.lng / 1000000,
    allowOtherPlayersToAlsoCollect: true,
    permSpawn: true
  });
  
  world.ui.showNotification(`✅ GeoNFT placed at ${playerPos.x}, ${playerPos.z}!`);
  
  // Spawn it in world immediately
  await geoNFTManager.spawnGeoNFTInWorld(result);
}
```

### Pattern 4: Real-World Location Mapping

**Use Case**: Map entire HYTOPIA world to a real-world region.

```typescript
/**
 * Map HYTOPIA world to a real-world geographic region
 * 
 * Example: World represents Central Park, NYC
 */
class RealWorldMapping {
  // Define world boundaries in real-world coordinates
  private worldBounds = {
    north: 40.7829,  // Central Park north
    south: 40.7648,  // Central Park south
    east: -73.9493,  // Central Park east
    west: -73.9814   // Central Park west
  };
  
  // HYTOPIA world size in blocks
  private worldSize = {
    width: 1000,   // blocks
    height: 1000   // blocks
  };
  
  /**
   * Convert lat/long to world coordinates within bounds
   */
  convertLatLngToWorldCoords(lat: number, lng: number): { x: number, z: number } {
    const latDegrees = lat / 1000000;
    const lngDegrees = lng / 1000000;
    
    // Normalize to 0-1 range
    const latNormalized = (latDegrees - this.worldBounds.south) / 
                          (this.worldBounds.north - this.worldBounds.south);
    const lngNormalized = (lngDegrees - this.worldBounds.west) / 
                          (this.worldBounds.east - this.worldBounds.west);
    
    // Map to world coordinates
    const x = lngNormalized * this.worldSize.width;
    const z = latNormalized * this.worldSize.height;
    
    return { x: Math.round(x), z: Math.round(z) };
  }
  
  /**
   * Convert world coordinates to lat/long
   */
  convertWorldCoordsToLatLng(x: number, z: number): { lat: number, lng: number } {
    // Normalize to 0-1 range
    const lngNormalized = x / this.worldSize.width;
    const latNormalized = z / this.worldSize.height;
    
    // Map to real-world coordinates
    const lngDegrees = this.worldBounds.west + 
                       (lngNormalized * (this.worldBounds.east - this.worldBounds.west));
    const latDegrees = this.worldBounds.south + 
                       (latNormalized * (this.worldBounds.north - this.worldBounds.south));
    
    return {
      lat: Math.round(latDegrees * 1000000),
      lng: Math.round(lngDegrees * 1000000)
    };
  }
}
```

---

## Complete HYTOPIA Plugin

```typescript
// @hytopia/oasis-geonft/src/index.ts

export class OASISGeoNFT {
  private apiUrl: string;
  private avatarId: string;
  private authToken: string;
  
  constructor(config: OASISGeoNFTConfig) {
    this.apiUrl = config.apiUrl;
    this.avatarId = config.avatarId;
    this.authToken = config.authToken;
  }
  
  /**
   * Get nearby GeoNFTs
   */
  async getNearbyGeoNFTs(options: {
    latitude: number;
    longitude: number;
    radiusKm?: number;
  }): Promise<GeoNFT[]> {
    const response = await fetch(
      `${this.apiUrl}/api/nft/get-nearby-geo-nfts?` +
      `latitude=${options.latitude}&` +
      `longitude=${options.longitude}&` +
      `radiusKm=${options.radiusKm || 1}`,
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
   * Place GeoNFT at coordinates
   */
  async placeGeoNFT(options: {
    originalOASISNFTId: string;
    latitude: number;
    longitude: number;
    allowOtherPlayersToAlsoCollect?: boolean;
    permSpawn?: boolean;
  }): Promise<GeoNFT> {
    // Convert degrees to micro-degrees
    const latMicroDegrees = Math.round(options.latitude * 1000000);
    const lngMicroDegrees = Math.round(options.longitude * 1000000);
    
    const response = await fetch(`${this.apiUrl}/api/nft/place-geo-nft`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        "Authorization": `Bearer ${this.authToken}`
      },
      body: JSON.stringify({
        originalOASISNFTId: options.originalOASISNFTId,
        lat: latMicroDegrees,
        long: lngMicroDegrees,
        allowOtherPlayersToAlsoCollect: options.allowOtherPlayersToAlsoCollect ?? true,
        permSpawn: options.permSpawn ?? false,
        globalSpawnQuantity: 1,
        playerSpawnQuantity: 1,
        respawnDurationInSeconds: 0,
        geoNFTMetaDataProvider: "MongoDBOASIS",
        originalOASISNFTOffChainProvider: "MongoDBOASIS"
      })
    });
    
    const data = await response.json();
    if (data.isError) {
      throw new Error(data.message);
    }
    
    return data.result;
  }
  
  /**
   * Load all GeoNFTs for avatar
   */
  async loadPlayerGeoNFTs(): Promise<GeoNFT[]> {
    const response = await fetch(
      `${this.apiUrl}/api/nft/load-all-geo-nfts-for-avatar/${this.avatarId}`,
      {
        headers: {
          "Authorization": `Bearer ${this.authToken}`
        }
      }
    );
    
    const data = await response.json();
    return data.result || [];
  }
}
```

---

## Integration Examples

### Example 1: Auto-Load Nearby GeoNFTs

```typescript
// In your HYTOPIA world script
import { OASISGeoNFT } from "@hytopia/oasis-geonft";
import { GeoCoordinateMapper } from "./geo-mapper";

const geoNFT = new OASISGeoNFT({
  apiUrl: "https://api.oasisweb4.com",
  avatarId: player.avatarId,
  authToken: player.authToken
});

const geoMapper = new GeoCoordinateMapper();

// Auto-load nearby GeoNFTs when player moves
world.onPlayerMove((player) => {
  const playerPos = player.position;
  
  // Check if player moved significantly (e.g., 100 blocks)
  if (shouldReloadGeoNFTs(playerPos)) {
    loadNearbyGeoNFTs(playerPos);
  }
});

async function loadNearbyGeoNFTs(playerPos: { x: number, z: number }) {
  // Convert to lat/long
  const geoCoords = geoMapper.convertWorldCoordsToLatLng(playerPos.x, playerPos.z);
  
  // Get nearby GeoNFTs (within 500m)
  const nearbyNFTs = await geoNFT.getNearbyGeoNFTs({
    latitude: geoCoords.lat / 1000000,
    longitude: geoCoords.lng / 1000000,
    radiusKm: 0.5
  });
  
  // Spawn in world
  for (const nft of nearbyNFTs) {
    spawnGeoNFT(nft);
  }
}
```

### Example 2: Place NFT at Current Position

```typescript
// Creator places NFT at their current world position
world.onCommand("place-geonft", async (player, args) => {
  if (args.length < 1) {
    player.sendMessage("Usage: /place-geonft <nft-id>");
    return;
  }
  
  const nftId = args[0];
  const playerPos = player.position;
  
  // Convert to lat/long
  const geoCoords = geoMapper.convertWorldCoordsToLatLng(
    playerPos.x,
    playerPos.z
  );
  
  try {
    const geoNFT = await oasisGeoNFT.placeGeoNFT({
      originalOASISNFTId: nftId,
      latitude: geoCoords.lat / 1000000,
      longitude: geoCoords.lng / 1000000,
      allowOtherPlayersToAlsoCollect: true
    });
    
    player.sendMessage(`✅ GeoNFT placed! Other players can now discover it.`);
  } catch (error) {
    player.sendMessage(`❌ Failed to place GeoNFT: ${error.message}`);
  }
});
```

### Example 3: GeoNFT Treasure Hunt

```typescript
// Create a treasure hunt using GeoNFTs
class GeoNFTTreasureHunt {
  async createTreasureHunt(waypoints: Array<{ lat: number, lng: number, nftId: string }>) {
    for (const waypoint of waypoints) {
      // Place GeoNFT at each waypoint
      await geoNFT.placeGeoNFT({
        originalOASISNFTId: waypoint.nftId,
        latitude: waypoint.lat,
        longitude: waypoint.lng,
        allowOtherPlayersToAlsoCollect: true,
        permSpawn: true
      });
    }
    
    // Players must collect all waypoints to complete the hunt
    world.broadcast("🗺️ Treasure hunt created! Find all GeoNFTs to win!");
  }
}
```

---

## API Endpoints

### Get Nearby GeoNFTs
```
GET /api/nft/get-nearby-geo-nfts?latitude={lat}&longitude={lng}&radiusKm={radius}
```

### Place GeoNFT
```
POST /api/nft/place-geo-nft
Body: {
  originalOASISNFTId: string,
  lat: number,  // micro-degrees
  long: number, // micro-degrees
  allowOtherPlayersToAlsoCollect: boolean,
  permSpawn: boolean,
  ...
}
```

### Load Player's GeoNFTs
```
GET /api/nft/load-all-geo-nfts-for-avatar/{avatarId}
```

---

## Benefits for HYTOPIA Creators

### 1. **Real-World Integration**
- Place NFTs at actual locations
- Players discover NFTs by visiting real places
- Bridge digital and physical worlds

### 2. **Location-Based Gameplay**
- Treasure hunts across real locations
- Location-specific rewards
- Geographic storytelling

### 3. **Cross-Platform Discovery**
- GeoNFTs placed in HYTOPIA appear in AR World
- Same NFTs discoverable across platforms
- Unified location-based experience

### 4. **Monetization**
- Sell location-based NFT experiences
- Charge for GeoNFT placement in popular areas
- Revenue sharing with location owners

---

## Implementation Checklist

### Phase 1: Basic Integration
- [ ] Create `@hytopia/oasis-geonft` plugin
- [ ] Implement coordinate mapping system
- [ ] Implement `getNearbyGeoNFTs()` function
- [ ] Implement `placeGeoNFT()` function
- [ ] Create GeoNFT entity spawner

### Phase 2: World Integration
- [ ] Auto-load nearby GeoNFTs on player movement
- [ ] Display GeoNFTs as in-world objects
- [ ] Add interaction handlers
- [ ] Implement collection system

### Phase 3: Advanced Features
- [ ] Real-world location mapping
- [ ] GeoNFT treasure hunts
- [ ] Location-based quests
- [ ] Cross-platform sync

---

## Coordinate Mapping Strategies

### Strategy 1: Origin-Based Mapping
- Define world origin as a real-world location
- All coordinates relative to origin
- **Best for**: Custom worlds, fictional locations

### Strategy 2: Bounded Region Mapping
- Map entire world to a real-world region
- World boundaries = geographic boundaries
- **Best for**: Real-world recreations (cities, parks)

### Strategy 3: Relative Positioning
- Use player spawn as reference
- Coordinates relative to spawn point
- **Best for**: Procedural worlds, exploration games

---

## References

- [GeoNFT Placement Guide](../MCP/GEONFT_PLACEMENT.md)
- [OASIS NFT API](./NFT-API.md)
- [HYTOPIA Developer Docs](https://dev.hytopia.com/)
