# WEB5 STAR API - Complete Documentation

## 📋 **Overview**

The WEB5 STAR API is the gamification and business layer that runs on top of the WEB4 OASIS API. It provides the STAR ODK (Omniverse Interoperable Metaverse Low Code Generator), mission systems, NFT management, inventory systems, and comprehensive metaverse development tools.

## 🏗️ **Architecture**

### **Core Components**
- **STAR ODK**: Low-code metaverse development platform
- **Missions System**: Quest and mission management
- **NFT Management**: Non-fungible token creation and trading
- **Inventory System**: Item and asset management
- **Celestial Bodies**: Virtual world objects and environments
- **Development Tools**: Templates, libraries, runtimes, and plugins

### **Key Features**
- **Low-Code Development**: Drag-and-drop interface builder
- **Cross-Platform Deployment**: Write once, deploy everywhere
- **Metaverse Integration**: 3D world creation and management
- **Gamification**: Mission systems and reward mechanisms
- **Asset Management**: NFT creation and trading
- **Template System**: Pre-built components and templates

## 🔗 **Base URL**
```
https://star-api.oasisplatform.world
```

## 🔐 **Authentication**

### **API Key Authentication**
```http
Authorization: Bearer YOUR_STAR_API_KEY
```

### **Avatar Authentication**
```http
Authorization: Avatar YOUR_AVATAR_ID
```

## 📚 **API Endpoints**

### **Avatar API**

#### **Avatar Operations**
```http
POST /api/avatar/authenticate
GET /api/avatar/current
```

### **Missions API**

#### **Mission CRUD Operations**
```http
GET /api/missions
GET /api/missions/{id}
POST /api/missions
PUT /api/missions/{id}
DELETE /api/missions/{id}
```

#### **Mission Search & Filtering**
```http
GET /api/missions/by-type/{type}
GET /api/missions/by-status/{status}
GET /api/missions/search
POST /api/missions/create
```

#### **Mission Loading**
```http
GET /api/missions/{id}/load
GET /api/missions/load-from-path
GET /api/missions/load-from-published
GET /api/missions/load-all-for-avatar
```

#### **Mission Publishing**
```http
POST /api/missions/{id}/publish
POST /api/missions/{id}/download
GET /api/missions/{id}/versions
GET /api/missions/{id}/version/{version}
```

#### **Mission Management**
```http
POST /api/missions/{id}/edit
POST /api/missions/{id}/unpublish
POST /api/missions/{id}/republish
POST /api/missions/{id}/activate
POST /api/missions/{id}/deactivate
POST /api/missions/{id}/clone
```

#### **Mission Operations**
```http
POST /api/missions/{id}/complete
GET /api/missions/{id}/leaderboard
GET /api/missions/{id}/rewards
GET /api/missions/stats
```

### **CelestialBodies API**

#### **CelestialBodies CRUD Operations**
```http
GET /api/celestialbodies
GET /api/celestialbodies/{id}
POST /api/celestialbodies
PUT /api/celestialbodies/{id}
DELETE /api/celestialbodies/{id}
```

#### **CelestialBodies Search & Filtering**
```http
GET /api/celestialbodies/by-type/{type}
GET /api/celestialbodies/in-space/{spaceId}
GET /api/celestialbodies/search
POST /api/celestialbodies/create
```

#### **CelestialBodies Loading**
```http
GET /api/celestialbodies/{id}/load
GET /api/celestialbodies/load-from-path
GET /api/celestialbodies/load-from-published
GET /api/celestialbodies/load-all-for-avatar
```

#### **CelestialBodies Publishing**
```http
POST /api/celestialbodies/{id}/publish
POST /api/celestialbodies/{id}/download
GET /api/celestialbodies/{id}/versions
GET /api/celestialbodies/{id}/version/{version}
```

#### **CelestialBodies Management**
```http
POST /api/celestialbodies/{id}/edit
POST /api/celestialbodies/{id}/unpublish
POST /api/celestialbodies/{id}/republish
POST /api/celestialbodies/{id}/activate
POST /api/celestialbodies/{id}/deactivate
```

### **CelestialBodiesMetaData API**

#### **CelestialBodiesMetaData CRUD Operations**
```http
GET /api/celestialbodiesmetadata
GET /api/celestialbodiesmetadata/{id}
POST /api/celestialbodiesmetadata
PUT /api/celestialbodiesmetadata/{id}
DELETE /api/celestialbodiesmetadata/{id}
```

#### **CelestialBodiesMetaData Search & Filtering**
```http
GET /api/celestialbodiesmetadata/search
POST /api/celestialbodiesmetadata/create
```

#### **CelestialBodiesMetaData Loading**
```http
GET /api/celestialbodiesmetadata/load-from-path
GET /api/celestialbodiesmetadata/load-from-published
GET /api/celestialbodiesmetadata/load-all-for-avatar
```

#### **CelestialBodiesMetaData Publishing**
```http
POST /api/celestialbodiesmetadata/{id}/publish
POST /api/celestialbodiesmetadata/{id}/download
GET /api/celestialbodiesmetadata/{id}/versions
GET /api/celestialbodiesmetadata/{id}/versions/{version}
```

#### **CelestialBodiesMetaData Management**
```http
POST /api/celestialbodiesmetadata/{id}/clone
POST /api/celestialbodiesmetadata/search
PUT /api/celestialbodiesmetadata/{id}/edit
POST /api/celestialbodiesmetadata/{id}/unpublish
POST /api/celestialbodiesmetadata/{id}/republish
POST /api/celestialbodiesmetadata/{id}/activate
POST /api/celestialbodiesmetadata/{id}/deactivate
```

### **CelestialSpaces API**

#### **CelestialSpaces CRUD Operations**
```http
GET /api/celestialspaces
GET /api/celestialspaces/{id}
POST /api/celestialspaces
PUT /api/celestialspaces/{id}
DELETE /api/celestialspaces/{id}
```

#### **CelestialSpaces Search & Filtering**
```http
GET /api/celestialspaces/by-type/{type}
GET /api/celestialspaces/in-space/{parentSpaceId}
GET /api/celestialspaces/search
POST /api/celestialspaces/create
```

#### **CelestialSpaces Loading**
```http
GET /api/celestialspaces/{id}/load
GET /api/celestialspaces/load-from-path
GET /api/celestialspaces/load-from-published
GET /api/celestialspaces/load-all-for-avatar
```

#### **CelestialSpaces Publishing**
```http
POST /api/celestialspaces/{id}/publish
POST /api/celestialspaces/{id}/download
GET /api/celestialspaces/{id}/versions
GET /api/celestialspaces/{id}/version/{version}
```

#### **CelestialSpaces Management**
```http
POST /api/celestialspaces/{id}/edit
POST /api/celestialspaces/{id}/unpublish
POST /api/celestialspaces/{id}/republish
POST /api/celestialspaces/{id}/activate
POST /api/celestialspaces/{id}/deactivate
```

### **Chapters API**

#### **Chapters CRUD Operations**
```http
GET /api/chapters
GET /api/chapters/{id}
POST /api/chapters
PUT /api/chapters/{id}
DELETE /api/chapters/{id}
```

#### **Chapters Search & Filtering**
```http
GET /api/chapters/search
POST /api/chapters/create
```

#### **Chapters Loading**
```http
GET /api/chapters/{id}/load
GET /api/chapters/load-from-path
GET /api/chapters/load-from-published
GET /api/chapters/load-all-for-avatar
```

#### **Chapters Publishing**
```http
POST /api/chapters/{id}/publish
POST /api/chapters/{id}/download
GET /api/chapters/{id}/versions
GET /api/chapters/{id}/version/{version}
```

#### **Chapters Management**
```http
POST /api/chapters/{id}/edit
POST /api/chapters/{id}/unpublish
POST /api/chapters/{id}/republish
POST /api/chapters/{id}/activate
POST /api/chapters/{id}/deactivate
```

### **Chat API**

#### **Chat Operations**
```http
POST /api/chat/start-new-chat-session
POST /api/chat/send-message/{sessionId}
GET /api/chat/history/{sessionId}
```

### **Competition API**

#### **Competition Management**
```http
GET /api/competition/leaderboard/{competitionType}/{seasonType}
GET /api/competition/my-rank/{competitionType}/{seasonType}
GET /api/competition/rank/{avatarId}/{competitionType}/{seasonType}
GET /api/competition/leagues/{competitionType}/{seasonType}
GET /api/competition/my-league/{competitionType}/{seasonType}
GET /api/competition/league/{avatarId}/{competitionType}/{seasonType}
GET /api/competition/tournaments
POST /api/competition/tournaments/{tournamentId}/join
GET /api/competition/stats/{competitionType}/{seasonType}
```

### **Eggs API**

#### **Egg Operations**
```http
GET /api/eggs/all
GET /api/eggs/discovered
POST /api/eggs/discover
POST /api/eggs/hatch/{eggId}
GET /api/eggs/quests
POST /api/eggs/quests/{questId}/complete
GET /api/eggs/quests/leaderboard
GET /api/eggs/stats
GET /api/eggs/types
```

### **GeoHotSpots API**

#### **GeoHotSpots CRUD Operations**
```http
GET /api/geohotspots
GET /api/geohotspots/{id}
POST /api/geohotspots
PUT /api/geohotspots/{id}
DELETE /api/geohotspots/{id}
```

#### **GeoHotSpots Search & Filtering**
```http
GET /api/geohotspots/nearby
POST /api/geohotspots/create
```

#### **GeoHotSpots Loading**
```http
GET /api/geohotspots/{id}/load
GET /api/geohotspots/load-from-path
GET /api/geohotspots/load-from-published
GET /api/geohotspots/load-all-for-avatar
```

#### **GeoHotSpots Publishing**
```http
POST /api/geohotspots/{id}/publish
POST /api/geohotspots/{id}/download
GET /api/geohotspots/{id}/versions
GET /api/geohotspots/{id}/version/{version}
```

#### **GeoHotSpots Management**
```http
POST /api/geohotspots/{id}/edit
POST /api/geohotspots/{id}/unpublish
POST /api/geohotspots/{id}/republish
POST /api/geohotspots/{id}/activate
POST /api/geohotspots/{id}/deactivate
```

### **GeoNFTs API**

#### **GeoNFTs CRUD Operations**
```http
GET /api/geonfts
GET /api/geonfts/{id}
POST /api/geonfts
PUT /api/geonfts/{id}
DELETE /api/geonfts/{id}
```

#### **GeoNFTs Search & Filtering**
```http
GET /api/geonfts/nearby
GET /api/geonfts/by-avatar/{avatarId}
GET /api/geonfts/search
POST /api/geonfts/create
```

#### **GeoNFTs Loading**
```http
GET /api/geonfts/{id}/load
GET /api/geonfts/load-from-path
GET /api/geonfts/load-from-published
GET /api/geonfts/load-all-for-avatar
```

#### **GeoNFTs Publishing**
```http
POST /api/geonfts/{id}/publish
POST /api/geonfts/{id}/download
GET /api/geonfts/{id}/versions
GET /api/geonfts/{id}/version/{version}
```

#### **GeoNFTs Management**
```http
POST /api/geonfts/{id}/edit
POST /api/geonfts/{id}/unpublish
POST /api/geonfts/{id}/republish
POST /api/geonfts/{id}/activate
POST /api/geonfts/{id}/deactivate
```

### **Holons API**

#### **Holons CRUD Operations**
```http
GET /api/holons
GET /api/holons/{id}
POST /api/holons
PUT /api/holons/{id}
DELETE /api/holons/{id}
```

#### **Holons Search & Filtering**
```http
GET /api/holons/by-type/{type}
GET /api/holons/by-parent/{parentId}
GET /api/holons/by-metadata
GET /api/holons/search
GET /api/holons/by-status/{status}
POST /api/holons/create
```

#### **Holons Loading**
```http
GET /api/holons/{id}/load
GET /api/holons/load-from-path
GET /api/holons/load-from-published
GET /api/holons/load-all-for-avatar
```

#### **Holons Publishing**
```http
POST /api/holons/{id}/publish
POST /api/holons/{id}/download
GET /api/holons/{id}/versions
GET /api/holons/{id}/version/{version}
```

#### **Holons Management**
```http
POST /api/holons/{id}/edit
POST /api/holons/{id}/unpublish
POST /api/holons/{id}/republish
POST /api/holons/{id}/activate
POST /api/holons/{id}/deactivate
```

### **HolonsMetaData API**

#### **HolonsMetaData CRUD Operations**
```http
GET /api/holonsmetadata
GET /api/holonsmetadata/{id}
POST /api/holonsmetadata
PUT /api/holonsmetadata/{id}
DELETE /api/holonsmetadata/{id}
```

#### **HolonsMetaData Search & Filtering**
```http
GET /api/holonsmetadata/search
POST /api/holonsmetadata/create
```

#### **HolonsMetaData Loading**
```http
GET /api/holonsmetadata/load-from-path
GET /api/holonsmetadata/load-from-published
GET /api/holonsmetadata/load-all-for-avatar
```

#### **HolonsMetaData Publishing**
```http
POST /api/holonsmetadata/{id}/publish
POST /api/holonsmetadata/{id}/download
GET /api/holonsmetadata/{id}/versions
GET /api/holonsmetadata/{id}/versions/{version}
```

#### **HolonsMetaData Management**
```http
POST /api/holonsmetadata/{id}/clone
POST /api/holonsmetadata/search
PUT /api/holonsmetadata/{id}/edit
POST /api/holonsmetadata/{id}/unpublish
POST /api/holonsmetadata/{id}/republish
POST /api/holonsmetadata/{id}/activate
POST /api/holonsmetadata/{id}/deactivate
```

### **InventoryItems API**

#### **InventoryItems CRUD Operations**
```http
GET /api/inventoryitems
GET /api/inventoryitems/{id}
POST /api/inventoryitems
PUT /api/inventoryitems/{id}
DELETE /api/inventoryitems/{id}
```

#### **InventoryItems Search & Filtering**
```http
GET /api/inventoryitems/by-avatar/{avatarId}
POST /api/inventoryitems/create
```

#### **InventoryItems Loading**
```http
GET /api/inventoryitems/{id}/load
GET /api/inventoryitems/load-from-path
GET /api/inventoryitems/load-from-published
GET /api/inventoryitems/load-all-for-avatar
```

#### **InventoryItems Publishing**
```http
POST /api/inventoryitems/{id}/publish
POST /api/inventoryitems/{id}/download
GET /api/inventoryitems/{id}/versions
GET /api/inventoryitems/{id}/version/{version}
```

#### **InventoryItems Management**
```http
POST /api/inventoryitems/{id}/edit
POST /api/inventoryitems/{id}/unpublish
POST /api/inventoryitems/{id}/republish
POST /api/inventoryitems/{id}/activate
POST /api/inventoryitems/{id}/deactivate
POST /api/inventoryitems/search
```

### **Libraries API**

#### **Libraries CRUD Operations**
```http
GET /api/libraries
GET /api/libraries/{id}
POST /api/libraries
PUT /api/libraries/{id}
DELETE /api/libraries/{id}
```

#### **Libraries Search & Filtering**
```http
GET /api/libraries/search
GET /api/libraries/by-category/{category}
POST /api/libraries/create
```

#### **Libraries Loading**
```http
GET /api/libraries/{id}/load
GET /api/libraries/load-from-path
GET /api/libraries/load-from-published
GET /api/libraries/load-all-for-avatar
```

#### **Libraries Publishing**
```http
POST /api/libraries/{id}/publish
POST /api/libraries/{id}/download
GET /api/libraries/{id}/versions
GET /api/libraries/{id}/version/{version}
```

#### **Libraries Management**
```http
POST /api/libraries/{id}/clone
POST /api/libraries/{id}/edit
POST /api/libraries/{id}/unpublish
POST /api/libraries/{id}/republish
POST /api/libraries/{id}/activate
POST /api/libraries/{id}/deactivate
```

### **Messaging API**

#### **Messaging Operations**
```http
POST /api/messaging/send-message-to-avatar/{toAvatarId}
GET /api/messaging/messages
GET /api/messaging/conversation/{otherAvatarId}
POST /api/messaging/mark-messages-read
GET /api/messaging/notifications
POST /api/messaging/mark-notifications-read
```

### **NFTs API**

#### **NFTs CRUD Operations**
```http
GET /api/nfts
GET /api/nfts/{id}
POST /api/nfts
PUT /api/nfts/{id}
DELETE /api/nfts/{id}
```

#### **NFTs Search & Filtering**
```http
GET /api/nfts/search
POST /api/nfts/create
```

#### **NFTs Loading**
```http
GET /api/nfts/{id}/load
GET /api/nfts/load-from-path
GET /api/nfts/load-from-published
GET /api/nfts/load-all-for-avatar
```

#### **NFTs Publishing**
```http
POST /api/nfts/{id}/publish
POST /api/nfts/{id}/download
GET /api/nfts/{id}/versions
GET /api/nfts/{id}/version/{version}
```

#### **NFTs Management**
```http
POST /api/nfts/{id}/clone
POST /api/nfts/{id}/edit
POST /api/nfts/{id}/unpublish
POST /api/nfts/{id}/republish
POST /api/nfts/{id}/activate
POST /api/nfts/{id}/deactivate
```

### **OAPPs API**

#### **OAPPs CRUD Operations**
```http
GET /api/oapps
GET /api/oapps/{id}
POST /api/oapps
PUT /api/oapps/{id}
DELETE /api/oapps/{id}
```

#### **OAPPs Search & Filtering**
```http
GET /api/oapps/search
POST /api/oapps/create
```

#### **OAPPs Loading**
```http
GET /api/oapps/load-from-path
GET /api/oapps/load-from-published
GET /api/oapps/load-all-for-avatar
```

#### **OAPPs Publishing**
```http
POST /api/oapps/{id}/publish
POST /api/oapps/{id}/download
GET /api/oapps/{id}/versions
GET /api/oapps/{id}/versions/{version}
```

#### **OAPPs Management**
```http
POST /api/oapps/{id}/clone
POST /api/oapps/search
PUT /api/oapps/{id}/edit
POST /api/oapps/{id}/unpublish
POST /api/oapps/{id}/republish
POST /api/oapps/{id}/activate
POST /api/oapps/{id}/deactivate
```

### **Parks API**

#### **Parks CRUD Operations**
```http
GET /api/parks
GET /api/parks/{id}
POST /api/parks
PUT /api/parks/{id}
DELETE /api/parks/{id}
```

#### **Parks Search & Filtering**
```http
GET /api/parks/nearby
GET /api/parks/by-type/{type}
POST /api/parks/create
```

#### **Parks Loading**
```http
GET /api/parks/{id}/load}
GET /api/parks/load-from-path
GET /api/parks/load-from-published
GET /api/parks/load-all-for-avatar
```

#### **Parks Publishing**
```http
POST /api/parks/{id}/publish
POST /api/parks/{id}/download
GET /api/parks/{id}/versions
GET /api/parks/{id}/version/{version}
```

#### **Parks Management**
```http
POST /api/parks/{id}/edit
POST /api/parks/{id}/unpublish
POST /api/parks/{id}/republish
POST /api/parks/{id}/activate
POST /api/parks/{id}/deactivate
POST /api/parks/search
```

### **Plugins API**

#### **Plugins CRUD Operations**
```http
GET /api/plugins
GET /api/plugins/{id}
POST /api/plugins
PUT /api/plugins/{id}
DELETE /api/plugins/{id}
```

#### **Plugins Search & Filtering**
```http
GET /api/plugins/search
GET /api/plugins/by-type/{type}
POST /api/plugins/create
```

#### **Plugins Loading**
```http
GET /api/plugins/{id}/load
GET /api/plugins/load-from-path
GET /api/plugins/load-from-published
GET /api/plugins/load-all-for-avatar
```

#### **Plugins Publishing**
```http
POST /api/plugins/{id}/publish
POST /api/plugins/{id}/download
GET /api/plugins/{id}/versions
GET /api/plugins/{id}/version/{version}
```

#### **Plugins Management**
```http
POST /api/plugins/{id}/install
POST /api/plugins/{id}/uninstall
POST /api/plugins/{id}/clone
POST /api/plugins/{id}/edit
POST /api/plugins/{id}/unpublish
POST /api/plugins/{id}/republish
POST /api/plugins/{id}/activate
POST /api/plugins/{id}/deactivate
```

### **Quests API**

#### **Quests CRUD Operations**
```http
GET /api/quests
GET /api/quests/{id}
POST /api/quests
PUT /api/quests/{id}
DELETE /api/quests/{id}
```

#### **Quests Search & Filtering**
```http
GET /api/quests/by-avatar/{avatarId}
GET /api/quests/by-type/{type}
GET /api/quests/by-status/{status}
GET /api/quests/search
POST /api/quests/create
```

#### **Quests Loading**
```http
GET /api/quests/{id}/load
GET /api/quests/load-from-path
GET /api/quests/load-from-published
GET /api/quests/load-all-for-avatar
```

#### **Quests Publishing**
```http
POST /api/quests/{id}/publish
POST /api/quests/{id}/download
GET /api/quests/{id}/versions
GET /api/quests/{id}/version/{version}
```

#### **Quests Management**
```http
POST /api/quests/{id}/clone
POST /api/quests/{id}/edit
POST /api/quests/{id}/unpublish
POST /api/quests/{id}/republish
POST /api/quests/{id}/activate
POST /api/quests/{id}/deactivate
POST /api/quests/{id}/complete
GET /api/quests/{id}/leaderboard
GET /api/quests/{id}/rewards
GET /api/quests/stats
```

### **Runtimes API**

#### **Runtimes CRUD Operations**
```http
GET /api/runtimes
GET /api/runtimes/{id}
POST /api/runtimes
PUT /api/runtimes/{id}
DELETE /api/runtimes/{id}
```

#### **Runtimes Search & Filtering**
```http
GET /api/runtimes/search
GET /api/runtimes/by-type/{type}
POST /api/runtimes/create
```

#### **Runtimes Loading**
```http
GET /api/runtimes/load-from-path
GET /api/runtimes/load-from-published
GET /api/runtimes/load-all-for-avatar
```

#### **Runtimes Publishing**
```http
POST /api/runtimes/{id}/publish
POST /api/runtimes/{id}/download
GET /api/runtimes/{id}/versions
GET /api/runtimes/{id}/versions/{version}
```

#### **Runtimes Management**
```http
POST /api/runtimes/{id}/start
POST /api/runtimes/{id}/stop
GET /api/runtimes/{id}/status
POST /api/runtimes/{id}/clone
POST /api/runtimes/search
PUT /api/runtimes/{id}/edit
POST /api/runtimes/{id}/unpublish
POST /api/runtimes/{id}/republish
POST /api/runtimes/{id}/activate
POST /api/runtimes/{id}/deactivate
```

### **STAR API**

#### **STAR Operations**
```http
GET /api/star/status
POST /api/star/ignite
POST /api/star/extinguish
POST /api/star/beam-in
```

### **Templates API**

#### **Templates CRUD Operations**
```http
GET /api/templates
GET /api/templates/{id}
POST /api/templates
PUT /api/templates/{id}
DELETE /api/templates/{id}
```

#### **Templates Search & Filtering**
```http
GET /api/templates/search
GET /api/templates/by-type/{type}
POST /api/templates/create
```

#### **Templates Loading**
```http
GET /api/templates/load-from-path
GET /api/templates/load-from-published
GET /api/templates/load-all-for-avatar
```

#### **Templates Publishing**
```http
POST /api/templates/{id}/publish
POST /api/templates/{id}/download
GET /api/templates/{id}/versions
GET /api/templates/{id}/versions/{version}
```

#### **Templates Management**
```http
POST /api/templates/{id}/clone
POST /api/templates/search
PUT /api/templates/{id}/edit
POST /api/templates/{id}/unpublish
POST /api/templates/{id}/republish
POST /api/templates/{id}/activate
POST /api/templates/{id}/deactivate
```

### **Zomes API**

#### **Zomes CRUD Operations**
```http
GET /api/zomes
GET /api/zomes/{id}
POST /api/zomes
PUT /api/zomes/{id}
DELETE /api/zomes/{id}
```

#### **Zomes Search & Filtering**
```http
GET /api/zomes/by-type/{type}
GET /api/zomes/in-space/{spaceId}
POST /api/zomes/create
```

#### **Zomes Loading**
```http
GET /api/zomes/{id}/load
GET /api/zomes/load-from-path
GET /api/zomes/load-from-published
GET /api/zomes/load-all-for-avatar
```

#### **Zomes Publishing**
```http
POST /api/zomes/{id}/publish
POST /api/zomes/{id}/download
GET /api/zomes/{id}/versions
GET /api/zomes/{id}/version/{version}
```

#### **Zomes Management**
```http
GET /api/zomes/search
POST /api/zomes/{id}/edit
POST /api/zomes/{id}/unpublish
POST /api/zomes/{id}/republish
POST /api/zomes/{id}/activate
POST /api/zomes/{id}/deactivate
```

### **ZomesMetaData API**

#### **ZomesMetaData CRUD Operations**
```http
GET /api/zomesmetadata
GET /api/zomesmetadata/{id}
POST /api/zomesmetadata
PUT /api/zomesmetadata/{id}
DELETE /api/zomesmetadata/{id}
```

#### **ZomesMetaData Search & Filtering**
```http
GET /api/zomesmetadata/search
POST /api/zomesmetadata/create
```

#### **ZomesMetaData Loading**
```http
GET /api/zomesmetadata/load-from-path
GET /api/zomesmetadata/load-from-published
GET /api/zomesmetadata/load-all-for-avatar
```

#### **ZomesMetaData Publishing**
```http
POST /api/zomesmetadata/{id}/publish
POST /api/zomesmetadata/{id}/download
GET /api/zomesmetadata/{id}/versions
GET /api/zomesmetadata/{id}/versions/{version}
```

#### **ZomesMetaData Management**
```http
POST /api/zomesmetadata/{id}/clone
POST /api/zomesmetadata/search
PUT /api/zomesmetadata/{id}/edit
POST /api/zomesmetadata/{id}/unpublish
POST /api/zomesmetadata/{id}/republish
POST /api/zomesmetadata/{id}/activate
POST /api/zomesmetadata/{id}/deactivate
```

## 📊 **Response Format**

### **Success Response**
```json
{
  "result": {
    "success": true,
    "data": {},
    "message": "Operation completed successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### **Error Response**
```json
{
  "result": null,
  "isError": true,
  "message": "Error description",
  "exception": "Exception details"
}
```

## 🔧 **Common Parameters**

### **Pagination**
- `limit` (int, optional): Number of items per page (default: 50)
- `offset` (int, optional): Number of items to skip (default: 0)

### **Filtering**
- `type` (string, optional): Filter by type
- `status` (string, optional): Filter by status
- `avatarId` (string, optional): Filter by avatar ID

### **Sorting**
- `sortBy` (string, optional): Field to sort by
- `sortOrder` (string, optional): Sort order (asc/desc)

## 🚀 **Getting Started**

### **1. Authentication**
```http
POST /api/avatar/authenticate
Content-Type: application/json

{
  "username": "your_username",
  "password": "your_password"
}
```

### **2. Create Your First Mission**
```http
POST /api/missions
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN

{
  "name": "My First Mission",
  "description": "Complete this mission to get started",
  "type": "tutorial",
  "rewards": {
    "experience": 100,
    "items": ["starter_sword"]
  }
}
```

### **3. Load Mission**
```http
GET /api/missions/{missionId}
Authorization: Bearer YOUR_TOKEN
```

## 📈 **Rate Limits**

- **Standard**: 1000 requests per hour
- **Premium**: 10000 requests per hour
- **Enterprise**: Unlimited

## 🛡️ **Security**

### **HTTPS Only**
All API endpoints require HTTPS encryption.

### **Authentication Required**
Most endpoints require valid authentication tokens.

### **Rate Limiting**
API calls are rate-limited to prevent abuse.

## 📞 **Support**

For technical support and questions:
- **Documentation**: [STAR API Docs](https://docs.oasisplatform.world/star-api)
- **Support**: [support@oasisplatform.world](mailto:support@oasisplatform.world)
- **Community**: [OASIS Discord](https://discord.gg/oasis)

## 🔄 **Versioning**

The API uses semantic versioning:
- **Current Version**: v1.0.0
- **Version Header**: `API-Version: v1.0.0`
- **Deprecation Policy**: 6 months notice for breaking changes

## 📝 **Changelog**

### **v1.0.0** (Current)
- Initial release of WEB5 STAR API
- Complete mission system
- NFT management
- Inventory system
- Celestial bodies and spaces
- Development tools (templates, libraries, runtimes, plugins)
- Comprehensive metaverse development platform