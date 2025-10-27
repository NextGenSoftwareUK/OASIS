# WEB5 STAR API Documentation

## Overview

Welcome to the WEB5 STAR API documentation. This API provides advanced functionality for the STAR ODK ecosystem, enabling creation of immersive metaverse experiences.

## API Documentation

### Core APIs
- [Avatar API](Avatar-API.md) - Avatar management and customization (`/api/avatar`)
- [STAR API](STAR-API.md) - STAR ODK core functionality (`/api/star`)
- [Missions API](Missions-API.md) - Mission management system (`/api/missions`)
- [Quests API](Quests-API.md) - Quest and challenge system (`/api/quests`)
- [Competition API](Competition-API.md) - Competition and leaderboards (`/api/competition`)


### Game APIs
- [Eggs API](Eggs-API.md) - Egg collection and hatching (`/api/eggs`)

### Celestial APIs
- [CelestialBodies API](CelestialBodies-API.md) - Planets, stars, and moons (`/api/celestialbodies`)
- [CelestialSpaces API](CelestialSpaces-API.md) - Space regions and sectors (`/api/celestialspaces`)

### NFT & Location APIs
- [NFTs API](NFTs-API.md) - NFT creation and trading (`/api/nfts`)
- [GeoNFTs API](GeoNFTs-API.md) - Location-based NFTs (`/api/geonfts`)
- [GeoHotSpots API](GeoHotSpots-API.md) - Location hotspots (`/api/geohotspots`)

### Data & Structure APIs
- [Holons API](Holons-API.md) - Universal data containers (`/api/holons`)
- [Zomes API](Zomes-API.md) - Application modules (`/api/zomes`)
- [Chapters API](Chapters-API.md) - Story and narrative progression (`/api/chapters`)

### Development APIs
- [OAPPs API](OAPPs-API.md) - OASIS Application management (`/api/oapps`)
- [Templates API](Templates-API.md) - Reusable templates (`/api/templates`)
- [Runtimes API](Runtimes-API.md) - Execution environments (`/api/runtimes`)
- [Plugins API](Plugins-API.md) - Plugin system (`/api/plugins`)
- [Libraries API](Libraries-API.md) - Code libraries (`/api/libraries`)

### Environment APIs
- [Parks API](Parks-API.md) - Virtual park management (`/api/parks`)
- [InventoryItems API](InventoryItems-API.md) - Item management (`/api/inventoryitems`)

### Metadata APIs
- [CelestialBodiesMetaData API](CelestialBodiesMetaData-API.md) - Celestial body metadata (`/api/celestialbodiesmetadata`)
- [HolonsMetaData API](HolonsMetaData-API.md) - Holon metadata (`/api/holonsmetadata`)
- [ZomesMetaData API](ZomesMetaData-API.md) - Zome metadata (`/api/zomesmetadata`)

## Getting Started

All APIs require authentication using Bearer tokens. Include your token in the Authorization header:

```http
Authorization: Bearer YOUR_TOKEN
```

## Response Format

All API responses follow the standard OASIS response format:

```json
{
  "result": {
    "success": true,
    "data": { ... },
    "message": "Operation successful"
  },
  "isError": false,
  "message": "Success"
}
```

## Support

For support and questions, please visit the [OASIS Documentation](../../DEVELOPER_DOCUMENTATION_INDEX.md).

---

**[Back to Main Documentation](../README.md)**