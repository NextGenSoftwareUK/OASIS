# WEB5 STAR API Documentation

## Overview

Welcome to the WEB5 STAR API documentation. This API provides advanced functionality for the STAR ODK ecosystem, enabling creation of immersive metaverse experiences.

## API Documentation

### Core APIs
- [Avatar API](Avatar-API.md) - Avatar management and customization
- [STAR API](STAR-API.md) - STAR ODK core functionality
- [Missions API](Missions-API.md) - Mission management system
- [Quests API](Quests-API.md) - Quest and challenge system
- [Competition API](Competition-API.md) - Competition and leaderboards

### Communication APIs
- [Chat API](Chat-API.md) - Real-time chat system
- [Messaging API](Messaging-API.md) - Direct messaging

### Game APIs
- [Eggs API](Eggs-API.md) - Egg collection and hatching

### Celestial APIs
- [CelestialBodies API](CelestialBodies-API.md) - Planets, stars, and moons
- [CelestialSpaces API](CelestialSpaces-API.md) - Space regions and sectors

### NFT & Location APIs
- [NFTs API](NFTs-API.md) - NFT creation and trading
- [GeoNFTs API](GeoNFTs-API.md) - Location-based NFTs
- [GeoHotSpots API](GeoHotSpots-API.md) - Location hotspots

### Data & Structure APIs
- [Holons API](Holons-API.md) - Universal data containers
- [Zomes API](Zomes-API.md) - Application modules
- [Chapters API](Chapters-API.md) - Story and narrative progression

### Development APIs
- [OAPPs API](OAPPs-API.md) - OASIS Application management
- [Templates API](Templates-API.md) - Reusable templates
- [Runtimes API](Runtimes-API.md) - Execution environments
- [Plugins API](Plugins-API.md) - Plugin system
- [Libraries API](Libraries-API.md) - Code libraries

### Environment APIs
- [Parks API](Parks-API.md) - Virtual park management
- [InventoryItems API](InventoryItems-API.md) - Item management

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