# OASIS API Overview - Complete Catalog

**Last Updated:** December 2025  
**Purpose:** Comprehensive catalog of all OASIS APIs for AI agents and developers

---

## Overview

OASIS provides **500+ endpoints** across two major API layers:

- **WEB4 OASIS API** - Core data aggregation and identity layer (300+ endpoints)
- **WEB5 STAR API** - Gamification and metaverse layer (200+ endpoints)

**Base URL:** `https://api.oasisweb4.com`

---

## 🌐 WEB4 OASIS API

Core infrastructure layer bridging Web2 and Web3. Provides universal data management, identity, and blockchain integration.

**Total: 300+ endpoints across 40+ controllers**

---

### 🔐 Authentication & Identity

#### **AVATAR API** (80+ endpoints)
**Purpose:** Complete user management and identity system

**Key Features:**
- User registration and authentication
- Avatar CRUD operations (create, read, update, delete)
- Profile management (details, portraits, preferences)
- Password management (forgot, reset, validate)
- Session management (login, logout, token refresh)
- Provider-specific authentication flows
- Avatar search and discovery

**Main Endpoints:**
- `POST /api/avatar/register` - Register new avatar
- `POST /api/avatar/authenticate` - Login
- `GET /api/avatar/{id}` - Get avatar by ID
- `GET /api/avatar/username/{username}` - Get avatar by username
- `PUT /api/avatar/{id}` - Update avatar
- `POST /api/avatar/upload-portrait` - Upload avatar image
- Provider-specific versions of all endpoints

**Documentation:** [`../developers/API Documentation/WEB4 OASIS API/Avatar-API.md`](../developers/API%20Documentation/WEB4%20OASIS%20API/Avatar-API.md)

---

#### **KEYS API** (40+ endpoints)
**Purpose:** Cryptographic key management across all providers

**Key Features:**
- Key generation and linking
- Provider-specific key operations
- Public/private key management
- Key retrieval and lookup
- Signature operations
- WiFi key management

**Main Endpoints:**
- `POST /api/keys/generate_keypair` - Generate new keypair
- `POST /api/keys/link_provider_public_key` - Link provider key
- `GET /api/keys/get_provider_public_keys/{avatarId}` - Get avatar keys
- `GET /api/keys/get_avatar_for_provider_key/{providerKey}` - Lookup by key

**Documentation:** [`../developers/API Documentation/WEB4 OASIS API/Keys-API.md`](../developers/API%20Documentation/WEB4%20OASIS%20API/Keys-API.md)

---

### 💎 Reputation & Social

#### **KARMA API** (20+ endpoints)
**Purpose:** Digital reputation and reward system

**Key Features:**
- Karma weighting (positive/negative)
- Karma voting system
- Akashic records (historical karma)
- Karma statistics and analytics
- Leaderboards and rankings
- Historical tracking

**Main Endpoints:**
- `POST /api/karma/add-karma` - Add karma to avatar
- `GET /api/karma/get-karma-for-avatar/{id}` - Get avatar karma
- `GET /api/karma/get-karma-leaderboard` - Get leaderboard
- `POST /api/karma/vote-for-positive-karma-weighting` - Vote on weighting

**Documentation:** [`../developers/API Documentation/WEB4 OASIS API/Karma-API.md`](../developers/API%20Documentation/WEB4%20OASIS%20API/Karma-API.md)

---

#### **SOCIAL API** (5+ endpoints)
**Purpose:** Social networking features

**Key Features:**
- Social feed management
- Avatar connections
- Social interactions

**Documentation:** [`../developers/API Documentation/WEB4 OASIS API/Social-API.md`](../developers/API%20Documentation/WEB4%20OASIS%20API/Social-API.md)

---

#### **MESSAGING API** (6+ endpoints)
**Purpose:** Advanced avatar-to-avatar messaging

**Key Features:**
- Direct messaging between avatars
- Conversation management
- Message history
- Notification system

**Main Endpoints:**
- `POST /api/messaging/send-message` - Send message
- `GET /api/messaging/get-conversations/{avatarId}` - Get conversations
- `GET /api/messaging/get-messages/{conversationId}` - Get message history

**Documentation:** [`../developers/API Documentation/WEB4 OASIS API/Messaging-API.md`](../developers/API%20Documentation/WEB4%20OASIS%20API/Messaging-API.md)

---

#### **CHAT API** (3+ endpoints)
**Purpose:** Real-time chat system

**Key Features:**
- Chat session management
- Real-time messaging
- Message persistence

**Documentation:** [`../developers/API Documentation/WEB4 OASIS API/Chat-API.md`](../developers/API%20Documentation/WEB4%20OASIS%20API/Chat-API.md)

---

#### **SHARE API** (2+ endpoints)
**Purpose:** Content sharing system

**Documentation:** [`../developers/API Documentation/WEB4 OASIS API/Share-API.md`](../developers/API%20Documentation/WEB4%20OASIS%20API/Share-API.md)

---

### 💾 Data Management

#### **DATA API** (30+ endpoints)
**Purpose:** Comprehensive data operations (Holon CRUD)

**Key Features:**
- Holon save/load/delete operations
- File upload/download
- Data storage with auto-replication
- Cross-provider data synchronization
- Advanced search capabilities
- Provider-specific operations

**Main Endpoints:**
- `POST /api/data/save-holon` - Save holon data
- `GET /api/data/load-holon/{id}` - Load holon by ID
- `DELETE /api/data/delete-holon/{id}` - Delete holon
- `POST /api/data/save-file` - Upload file
- `GET /api/data/load-file/{id}` - Download file
- Provider-specific versions with failover/replication parameters

**Documentation:** [`../developers/API Documentation/WEB4 OASIS API/Data-API.md`](../developers/API%20Documentation/WEB4%20OASIS%20API/Data-API.md)

---

#### **FILES API** (6+ endpoints)
**Purpose:** File management system

**Key Features:**
- File upload and download
- File metadata management
- File sharing and organization
- Provider support

**Documentation:** [`../developers/API Documentation/WEB4 OASIS API/Files-API.md`](../developers/API%20Documentation/WEB4%20OASIS%20API/Files-API.md)

---

#### **SEARCH API** (2+ endpoints)
**Purpose:** Universal search across all providers

**Key Features:**
- Cross-provider search
- Holon search
- Avatar search

**Main Endpoints:**
- `GET /api/search/search/{searchTerm}` - Universal search

**Documentation:** [`../developers/API Documentation/WEB4 OASIS API/Search-API.md`](../developers/API%20Documentation/WEB4%20OASIS%20API/Search-API.md)

---

### 💰 Financial Services

#### **WALLET API** (25+ endpoints)
**Purpose:** Multi-chain wallet management

**Key Features:**
- Multi-chain wallet support
- Portfolio management and analytics
- Transfer operations
- Transaction history
- Wallet import/export
- Default wallet selection
- Chain-specific operations

**Main Endpoints:**
- `GET /api/wallet/avatar/{id}/wallets` - Get avatar wallets
- `GET /api/wallet/default-wallet/{avatarId}` - Get default wallet
- `POST /api/wallet/transfer` - Transfer funds
- `GET /api/wallet/portfolio/value/{avatarId}` - Get portfolio value
- `GET /api/wallet/wallet/{walletId}/analytics` - Get wallet analytics
- `GET /api/wallet/supported-chains` - Get supported chains

**Documentation:** [`../developers/API Documentation/WEB4 OASIS API/Wallet-API.md`](../developers/API%20Documentation/WEB4%20OASIS%20API/Wallet-API.md)

---

#### **NFT API** (20+ endpoints)
**Purpose:** Cross-chain NFT operations

**Key Features:**
- NFT creation and minting
- NFT transfers
- Cross-chain NFT management
- NFT metadata
- NFT analytics and trading
- Geo-cached NFTs (for AR/gaming)

**Main Endpoints:**
- `POST /api/nft/mint` - Mint NFT
- `POST /api/nft/transfer` - Transfer NFT
- `GET /api/nft/get-nfts-for-avatar/{avatarId}` - Get avatar NFTs
- `GET /api/nft/get-nft/{id}` - Get NFT details

**Documentation:** [`../developers/API Documentation/WEB4 OASIS API/NFT-API.md`](../developers/API%20Documentation/WEB4%20OASIS%20API/NFT-API.md)

---

#### **SUBSCRIPTION API** (8+ endpoints)
**Purpose:** Billing and subscription management

**Key Features:**
- Subscription plans (Bronze, Silver, Gold, Enterprise)
- Quota management
- Usage tracking
- Billing operations

**Documentation:** [`../developers/API Documentation/WEB4 OASIS API/Subscription-API.md`](../developers/API%20Documentation/WEB4%20OASIS%20API/Subscription-API.md)

---

### 🌐 Network & Discovery

#### **ONET API** (10+ endpoints)
**Purpose:** Decentralized P2P networking

**Key Features:**
- Network status and topology
- Node connection and management
- Broadcasting and discovery
- Network statistics
- Peer management

**Main Endpoints:**
- `GET /api/onet/network-status` - Get network status
- `GET /api/onet/topology` - Get network topology
- `POST /api/onet/connect-node` - Connect to node
- `GET /api/onet/statistics` - Get network statistics

**Documentation:** [`../developers/API Documentation/WEB4 OASIS API/ONET-API.md`](../developers/API%20Documentation/WEB4%20OASIS%20API/ONET-API.md)

---

#### **ONODE API** (12+ endpoints)
**Purpose:** Node management and configuration

**Key Features:**
- Node configuration
- Monitoring and metrics
- Peer management
- System control and logging

**Documentation:** [`../developers/API Documentation/WEB4 OASIS API/ONODE-API.md`](../developers/API%20Documentation/WEB4%20OASIS%20API/ONODE-API.md)

---

### 🎮 Gamification

#### **COMPETITION API** (9+ endpoints)
**Purpose:** Gaming and competition system

**Key Features:**
- Leaderboards and rankings
- Tournament management
- League systems
- Competition statistics and analytics

**Main Endpoints:**
- `GET /api/competition/leaderboard/{competitionId}` - Get leaderboard
- `POST /api/competition/create-tournament` - Create tournament
- `GET /api/competition/statistics/{competitionId}` - Get statistics

**Documentation:** [`../developers/API Documentation/WEB4 OASIS API/Competition-API.md`](../developers/API%20Documentation/WEB4%20OASIS%20API/Competition-API.md)

---

#### **GIFTS API** (6+ endpoints)
**Purpose:** Gift system

**Key Features:**
- Gift sending and receiving
- Gift history
- Statistics and analytics

**Documentation:** [`../developers/API Documentation/WEB4 OASIS API/Gifts-API.md`](../developers/API%20Documentation/WEB4%20OASIS%20API/Gifts-API.md)

---

#### **EGGS API** (3+ endpoints)
**Purpose:** Egg discovery and hatching system

**Key Features:**
- Egg discovery
- Egg hatching
- Statistics

**Documentation:** [`../developers/API Documentation/WEB4 OASIS API/Eggs-API.md`](../developers/API%20Documentation/WEB4%20OASIS%20API/Eggs-API.md)

---

### 🗺️ Location & Mapping

#### **MAP API** (15+ endpoints)
**Purpose:** Advanced mapping and location services

**Key Features:**
- Route creation and navigation
- 3D object placement
- Location services
- Mapping analytics
- Geo-location features

**Main Endpoints:**
- `POST /api/map/create-route` - Create navigation route
- `POST /api/map/place-object` - Place 3D object
- `GET /api/map/location/{locationId}` - Get location details

**Documentation:** [`../developers/API Documentation/WEB4 OASIS API/Map-API.md`](../developers/API%20Documentation/WEB4%20OASIS%20API/Map-API.md)

---

### 🔗 Blockchain Integration

#### **EOSIO API** (9+ endpoints)
**Purpose:** EOSIO blockchain integration

**Key Features:**
- EOSIO account operations
- Transaction management
- Smart contract interactions

**Documentation:** [`../developers/API Documentation/WEB4 OASIS API/EOSIO-API.md`](../developers/API%20Documentation/WEB4%20OASIS%20API/EOSIO-API.md)

---

#### **HOLOCHAIN API** (7+ endpoints)
**Purpose:** Holochain integration

**Key Features:**
- Holochain agent operations
- Entry management
- Zome interactions

**Documentation:** [`../developers/API Documentation/WEB4 OASIS API/Holochain-API.md`](../developers/API%20Documentation/WEB4%20OASIS%20API/Holochain-API.md)

---

#### **TELOS API** (9+ endpoints)
**Purpose:** Telos blockchain integration

**Documentation:** [`../developers/API Documentation/WEB4 OASIS API/Telos-API.md`](../developers/API%20Documentation/WEB4%20OASIS%20API/Telos-API.md)

---

#### **SEEDS API** (15+ endpoints)
**Purpose:** Seeds/Hypha blockchain integration

**Documentation:** [`../developers/API Documentation/WEB4 OASIS API/Seeds-API.md`](../developers/API%20Documentation/WEB4%20OASIS%20API/Seeds-API.md)

---

#### **SOLANA API** (2+ endpoints)
**Purpose:** Solana blockchain integration

**Main Endpoints:**
- `POST /api/solana/transaction` - Submit Solana transaction
- `GET /api/solana/balance/{walletAddress}` - Get Solana balance

**Documentation:** [`../developers/API Documentation/WEB4 OASIS API/Solana-API.md`](../developers/API%20Documentation/WEB4%20OASIS%20API/Solana-API.md)

---

#### **CARGO API**
**Purpose:** NFT marketplace protocol integration

**Documentation:** [`../developers/API Documentation/WEB4 OASIS API/Cargo-API.md`](../developers/API%20Documentation/WEB4%20OASIS%20API/Cargo-API.md)

---

### ⚙️ Infrastructure & System Management

#### **PROVIDER API** (25+ endpoints)
**Purpose:** Provider management and configuration

**Key Features:**
- Provider registration and activation
- Provider configuration
- Auto-replication and failover settings
- Provider monitoring and management
- Provider status and health checks

**Main Endpoints:**
- `POST /api/provider/register-provider-type/{providerType}` - Register provider
- `POST /api/provider/activate-provider/{providerType}` - Activate provider
- `GET /api/provider/get-all-providers` - List all providers
- `GET /api/provider/get-provider/{providerType}` - Get provider details

**Documentation:** [`../developers/API Documentation/WEB4 OASIS API/Provider-API.md`](../developers/API%20Documentation/WEB4%20OASIS%20API/Provider-API.md)

---

#### **HYPERDRIVE API** (50+ endpoints)
**Purpose:** HyperDrive system management and analytics

**Key Features:**
- Configuration management
- Metrics and monitoring
- Analytics and reporting
- Failover management
- Replication rules
- Cost management
- Subscription integration
- Quota management
- AI recommendations

**Main Endpoints:**
- `GET /api/hyperdrive/config` - Get configuration
- `GET /api/hyperdrive/metrics` - Get performance metrics
- `GET /api/hyperdrive/analytics/dashboard` - Get analytics dashboard
- `GET /api/hyperdrive/failover/predictions` - Get failover predictions
- `GET /api/hyperdrive/replication/rules` - Get replication rules
- `GET /api/hyperdrive/costs/current` - Get current costs

**Documentation:** [`../developers/API Documentation/WEB4 OASIS API/HyperDrive-API.md`](../developers/API%20Documentation/WEB4%20OASIS%20API/HyperDrive-API.md)

---

#### **STATS API** (8+ endpoints)
**Purpose:** Analytics and statistics

**Key Features:**
- Comprehensive statistics
- Performance metrics
- Usage analytics
- Reporting

**Documentation:** [`../developers/API Documentation/WEB4 OASIS API/Stats-API.md`](../developers/API%20Documentation/WEB4%20OASIS%20API/Stats-API.md)

---

#### **SETTINGS API** (12+ endpoints)
**Purpose:** Configuration management

**Key Features:**
- System settings
- Avatar preferences
- Provider settings

**Documentation:** [`../developers/API Documentation/WEB4 OASIS API/Settings-API.md`](../developers/API%20Documentation/WEB4%20OASIS%20API/Settings-API.md)

---

### 📹 Media & Communication

#### **VIDEO API** (3+ endpoints)
**Purpose:** Video calling functionality

**Documentation:** [`../developers/API Documentation/WEB4 OASIS API/Video-API.md`](../developers/API%20Documentation/WEB4%20OASIS%20API/Video-API.md)

---

### 🌍 Additional Services

#### **OLAND API** (8+ endpoints)
**Purpose:** OLand system management

**Documentation:** [`../developers/API Documentation/WEB4 OASIS API/OLand-API.md`](../developers/API%20Documentation/WEB4%20OASIS%20API/OLand-API.md)

---

#### **OAPP API** (5+ endpoints)
**Purpose:** OASIS Application management

**Documentation:** [`../developers/API Documentation/WEB4 OASIS API/OAPP-API.md`](../developers/API%20Documentation/WEB4%20OASIS%20API/OAPP-API.md)

---

#### **TELEGRAM API**
**Purpose:** Telegram bot integration

---

#### **PINATA API**
**Purpose:** IPFS pinning service integration

**Documentation:** [`../developers/API Documentation/WEB4 OASIS API/Pinata-API.md`](../developers/API%20Documentation/WEB4%20OASIS%20API/Pinata-API.md)

---

#### **BRIDGE API**
**Purpose:** Cross-chain bridging operations

---

#### **STABLECOIN API**
**Purpose:** Stablecoin operations

---

#### **CONTRACT GENERATOR API**
**Purpose:** Smart contract generation and deployment

---

## ⭐ WEB5 STAR API

Gamification and metaverse layer for building immersive experiences, games, and virtual worlds.

**Total: 200+ endpoints across 20+ controllers**

---

### 🎮 Missions & Quests

#### **MISSIONS API** (27+ endpoints)
**Purpose:** Complete mission system

**Key Features:**
- Mission CRUD operations
- Mission lifecycle (activate, deactivate, complete)
- Publishing and versioning
- Cloning and templating
- Leaderboards and rewards
- Analytics and statistics
- Search and filtering

**Main Endpoints:**
- `GET /api/missions` - List missions
- `POST /api/missions` - Create mission
- `GET /api/missions/{id}` - Get mission
- `PUT /api/missions/{id}` - Update mission
- `POST /api/missions/{id}/publish` - Publish mission
- `POST /api/missions/{id}/clone` - Clone mission
- `GET /api/missions/{id}/leaderboard` - Get leaderboard

**Documentation:** [`../developers/API Documentation/WEB5 STAR API/Missions-API.md`](../developers/API%20Documentation/WEB5%20STAR%20API/Missions-API.md)

---

#### **QUESTS API** (25+ endpoints)
**Purpose:** Quest and challenge system

**Key Features:**
- Quest creation and management
- Quest completion tracking
- Rewards and achievements
- Quest chains and dependencies
- Statistics and leaderboards

**Documentation:** [`../developers/API Documentation/WEB5 STAR API/Quests-API.md`](../developers/API%20Documentation/WEB5%20STAR%20API/Quests-API.md)

---

### 🌌 Virtual Worlds

#### **CELESTIALBODIES API** (25+ endpoints)
**Purpose:** 3D world objects (planets, stars, moons)

**Key Features:**
- Celestial body creation and management
- Type-based filtering (planet, star, moon, etc.)
- Space positioning and relationships
- Publishing and version control
- Search and discovery

**Main Endpoints:**
- `GET /api/celestialbodies` - List celestial bodies
- `POST /api/celestialbodies` - Create celestial body
- `GET /api/celestialbodies/in-space/{spaceId}` - Bodies in space
- `GET /api/celestialbodies/by-type/{type}` - Filter by type

**Documentation:** [`../developers/API Documentation/WEB5 STAR API/CelestialBodies-API.md`](../developers/API%20Documentation/WEB5%20STAR%20API/CelestialBodies-API.md)

---

#### **CELESTIALSPACES API** (25+ endpoints)
**Purpose:** Virtual environments and space regions

**Key Features:**
- Space creation and management
- Space hierarchy (parent-child relationships)
- Space operations and navigation
- Publishing and distribution

**Documentation:** [`../developers/API Documentation/WEB5 STAR API/CelestialSpaces-API.md`](../developers/API%20Documentation/WEB5%20STAR%20API/CelestialSpaces-API.md)

---

#### **CELESTIALBODIES METADATA API** (25+ endpoints)
**Purpose:** Metadata management for celestial bodies

**Documentation:** [`../developers/API Documentation/WEB5 STAR API/CelestialBodiesMetaData-API.md`](../developers/API%20Documentation/WEB5%20STAR%20API/CelestialBodiesMetaData-API.md)

---

### 📚 Content Management

#### **HOLONS API** (25+ endpoints)
**Purpose:** Universal data containers for STAR system

**Key Features:**
- Hierarchical content management
- Metadata and status tracking
- Advanced search and filtering
- Parent-child relationships
- Publishing and versioning

**Documentation:** [`../developers/API Documentation/WEB5 STAR API/Holons-API.md`](../developers/API%20Documentation/WEB5%20STAR%20API/Holons-API.md)

---

#### **HOLONS METADATA API** (25+ endpoints)
**Purpose:** Metadata management for holons

**Documentation:** [`../developers/API Documentation/WEB5 STAR API/HolonsMetaData-API.md`](../developers/API%20Documentation/WEB5%20STAR%20API/HolonsMetaData-API.md)

---

#### **CHAPTERS API** (20+ endpoints)
**Purpose:** Content organization and narrative progression

**Key Features:**
- Chapter creation and management
- Story structure and navigation
- Publishing and distribution
- Content relationships

**Documentation:** [`../developers/API Documentation/WEB5 STAR API/Chapters-API.md`](../developers/API%20Documentation/WEB5%20STAR%20API/Chapters-API.md)

---

### 🎁 Asset Management

#### **NFTs API** (25+ endpoints)
**Purpose:** Cross-chain NFT operations for STAR system

**Key Features:**
- NFT creation and minting
- NFT trading and transfers
- Publishing and distribution
- Version control
- Cross-chain NFT management

**Documentation:** [`../developers/API Documentation/WEB5 STAR API/NFTs-API.md`](../developers/API%20Documentation/WEB5%20STAR%20API/NFTs-API.md)

---

#### **INVENTORYITEMS API** (25+ endpoints)
**Purpose:** Avatar inventory and item management

**Key Features:**
- Item tracking and management
- Inventory organization
- Item analytics
- Avatar-specific inventories

**Documentation:** [`../developers/API Documentation/WEB5 STAR API/InventoryItems-API.md`](../developers/API%20Documentation/WEB5%20STAR%20API/InventoryItems-API.md)

---

### 🛠️ Development Tools

#### **TEMPLATES API** (25+ endpoints)
**Purpose:** Reusable component templates

**Key Features:**
- Template library
- Template sharing and distribution
- Category-based organization
- Version control
- Template publishing

**Documentation:** [`../developers/API Documentation/WEB5 STAR API/Templates-API.md`](../developers/API%20Documentation/WEB5%20STAR%20API/Templates-API.md)

---

#### **LIBRARIES API** (25+ endpoints)
**Purpose:** Shared code libraries

**Key Features:**
- Code library management
- Category organization
- Publishing and collaboration
- Version control

**Documentation:** [`../developers/API Documentation/WEB5 STAR API/Libraries-API.md`](../developers/API%20Documentation/WEB5%20STAR%20API/Libraries-API.md)

---

#### **RUNTIMES API** (25+ endpoints)
**Purpose:** Execution environment management

**Key Features:**
- Runtime lifecycle control
- Status monitoring
- Start/stop operations
- Runtime configuration

**Documentation:** [`../developers/API Documentation/WEB5 STAR API/Runtimes-API.md`](../developers/API%20Documentation/WEB5%20STAR%20API/Runtimes-API.md)

---

#### **PLUGINS API** (25+ endpoints)
**Purpose:** Plugin system management

**Key Features:**
- Plugin installation
- Type-based organization
- Lifecycle control
- Plugin management

**Documentation:** [`../developers/API Documentation/WEB5 STAR API/Plugins-API.md`](../developers/API%20Documentation/WEB5%20STAR%20API/Plugins-API.md)

---

#### **OAPPs API** (25+ endpoints)
**Purpose:** OASIS Application management

**Key Features:**
- OAPP creation and deployment
- OAPP lifecycle management
- Publishing and distribution

**Documentation:** [`../developers/API Documentation/WEB5 STAR API/OAPPs-API.md`](../developers/API%20Documentation/WEB5%20STAR%20API/OAPPs-API.md)

---

#### **ZOMES API** (25+ endpoints)
**Purpose:** Application modules (Holochain integration)

**Key Features:**
- Zome management
- Zome deployment
- Module organization

**Documentation:** [`../developers/API Documentation/WEB5 STAR API/Zomes-API.md`](../developers/API%20Documentation/WEB5%20STAR%20API/Zomes-API.md)

---

#### **ZOMES METADATA API** (25+ endpoints)
**Purpose:** Metadata management for zomes

**Documentation:** [`../developers/API Documentation/WEB5 STAR API/ZomesMetaData-API.md`](../developers/API%20Documentation/WEB5%20STAR%20API/ZomesMetaData-API.md)

---

### 📍 Location Services

#### **GEOHOTSPOTS API** (25+ endpoints)
**Purpose:** Location-based hotspots and features

**Key Features:**
- Hotspot creation and management
- Nearby location services
- Publishing and distribution
- Geo-location features

**Documentation:** [`../developers/API Documentation/WEB5 STAR API/GeoHotSpots-API.md`](../developers/API%20Documentation/WEB5%20STAR%20API/GeoHotSpots-API.md)

---

#### **GEONFTS API** (25+ endpoints)
**Purpose:** Location-based NFTs (for AR/gaming)

**Key Features:**
- Geo-tagged NFT creation
- Location-based trading
- Avatar-specific collections
- AR integration

**Documentation:** [`../developers/API Documentation/WEB5 STAR API/GeoNFTs-API.md`](../developers/API%20Documentation/WEB5%20STAR%20API/GeoNFTs-API.md)

---

### 🎪 Social & Environment

#### **PARKS API** (25+ endpoints)
**Purpose:** Virtual park and social space management

**Key Features:**
- Virtual park creation
- Type-based organization
- Social interaction features
- Park management

**Documentation:** [`../developers/API Documentation/WEB5 STAR API/Parks-API.md`](../developers/API%20Documentation/WEB5%20STAR%20API/Parks-API.md)

---

#### **EGGS API** (8+ endpoints)
**Purpose:** Egg discovery and hatching system

**Key Features:**
- Egg discovery
- Egg hatching mechanics
- Quest integration
- Statistics and leaderboards

**Documentation:** [`../developers/API Documentation/WEB5 STAR API/Eggs-API.md`](../developers/API%20Documentation/WEB5%20STAR%20API/Eggs-API.md)

---

#### **COMPETITION API** (9+ endpoints)
**Purpose:** Competition and leaderboard system

**Documentation:** [`../developers/API Documentation/WEB5 STAR API/Competition-API.md`](../developers/API%20Documentation/WEB5%20STAR%20API/Competition-API.md)

---

### ⚡ System

#### **STAR API** (4+ endpoints)
**Purpose:** STAR ODK core system operations

**Key Features:**
- System status and control
- Ignition and shutdown
- Beam-in operations

**Main Endpoints:**
- `GET /api/star/status` - Get system status
- `POST /api/star/ignite` - Start system
- `POST /api/star/shutdown` - Stop system

**Documentation:** [`../developers/API Documentation/WEB5 STAR API/STAR-API.md`](../developers/API%20Documentation/WEB5%20STAR%20API/STAR-API.md)

---

#### **AVATAR API** (2+ endpoints)
**Purpose:** Avatar operations for STAR system

**Documentation:** [`../developers/API Documentation/WEB5 STAR API/Avatar-API.md`](../developers/API%20Documentation/WEB5%20STAR%20API/Avatar-API.md)

---

## 📊 API Summary Statistics

### WEB4 OASIS API
- **Total Controllers:** 40+
- **Total Endpoints:** 300+
- **Major Categories:** 15+

### WEB5 STAR API
- **Total Controllers:** 20+
- **Total Endpoints:** 200+
- **Major Categories:** 10+

### Combined
- **Total APIs:** 60+ controllers
- **Total Endpoints:** 500+
- **Coverage:** Comprehensive Web2/Web3 infrastructure + Metaverse/Gaming layer

---

## 🔗 Related Documentation

- **Complete API Reference:** [`../developers/API Documentation/`](../developers/API%20Documentation/)
- **API Endpoints Summary:** [`../../OASIS_API_COMPLETE_ENDPOINTS_SUMMARY.md`](../../OASIS_API_COMPLETE_ENDPOINTS_SUMMARY.md)
- **Quick Start:** [`QUICK_START.md`](QUICK_START.md)
- **Architecture Overview:** [`ARCHITECTURE_OVERVIEW.md`](ARCHITECTURE_OVERVIEW.md)

---

## 📝 Notes for AI Agents

1. **API Structure:** All WEB4 APIs use `/api/{controller}/{action}` pattern
2. **Authentication:** Most endpoints require Bearer token authentication
3. **Provider-Specific:** Many endpoints have provider-specific versions
4. **HyperDrive Integration:** All data operations automatically use HyperDrive for failover/replication
5. **Response Format:** All APIs return standard `OASISResult<T>` format
6. **Documentation Coverage:** Individual API docs exist but may not cover all endpoints - check controller code for complete reference

---

**This overview provides a comprehensive catalog. For detailed endpoint documentation, see the individual API documentation files referenced above.**

