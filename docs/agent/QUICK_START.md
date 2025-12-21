# OASIS Quick Start for AI Agents

**Last Updated:** December 2025  
**Purpose:** Provide AI agents with a clear, structured understanding of OASIS in minimal time

---

## What is OASIS?

**OASIS (Open Advanced Secure Interoperable System)** is a universal data layer that provides a single API to interact with 55+ blockchains, databases, and storage systems. Instead of integrating with each system separately, you make one API call and OASIS handles the multi-chain complexity automatically.

### Core Value Proposition

**"Write Once, Store Everywhere"**

- **Single API** for 50+ providers (blockchains, databases, storage)
- **Automatic Failover** - Your app keeps working even if providers go down
- **Automatic Replication** - Data stored across multiple providers
- **Automatic Load Balancing** - Optimal performance automatically

---

## Key Concepts (5-Minute Overview)

### 1. Providers

**Definition:** A "Provider" is any system OASIS can connect to (blockchain, database, storage service).

**Examples:**
- Blockchains: Ethereum, Solana, Polygon, Arbitrum
- Databases: MongoDB, Neo4j, SQLite
- Storage: IPFS, Pinata, Local File System

**Key Point:** OASIS treats all providers the same way through a unified interface.

**Learn More:** [`../concepts/PROVIDERS.md`](../concepts/PROVIDERS.md)

### 2. HyperDrive

**Definition:** OASIS's intelligent routing system that provides 100% uptime through automatic failover, replication, and load balancing.

**Three Core Features:**
1. **Auto-Failover:** Automatically switches to backup providers when primary fails
2. **Auto-Replication:** Replicates data across multiple providers automatically
3. **Auto-Load Balancing:** Distributes requests across providers for optimal performance

**How It Works:**
```
Your Request → OASIS HyperDrive → Selects Best Provider → Returns Data
                      ↓
              If Provider Fails → Automatically Tries Next Provider
```

**Learn More:** [`../concepts/HYPERDRIVE.md`](../concepts/HYPERDRIVE.md)

### 3. Managers

**Definition:** High-level APIs that handle domain-specific operations (avatars, data, wallets, etc.).

**Examples:**
- **AvatarManager:** User identity and account management
- **HolonManager:** Data storage and retrieval
- **WalletManager:** Multi-chain wallet operations
- **NFTManager:** Cross-chain NFT operations

**Key Point:** Managers abstract away provider complexity. You use managers, not providers directly.

**Learn More:** [`../concepts/MANAGERS.md`](../concepts/MANAGERS.md)

### 4. Holons

**Definition:** OASIS's universal data model - a self-contained unit of data that works across all providers.

**Structure:**
- Every Holon has: ID, Name, Description, Metadata
- Holons are stored across multiple providers
- Holons can reference other Holons

**Key Point:** Holons are provider-agnostic. Same data structure works everywhere.

**Learn More:** [`../concepts/HOLONS.md`](../concepts/HOLONS.md)

### 5. Avatars

**Definition:** OASIS's universal user identity system - works across all providers.

**Characteristics:**
- Each user has a unique Avatar ID (GUID)
- Avatars can have wallets on multiple chains
- Avatars maintain reputation (Karma) across all providers

**Key Point:** One Avatar = One User Identity across 50+ systems.

**Learn More:** [`../concepts/AVATARS.md`](../concepts/AVATARS.md)

---

## How OASIS Works (High-Level Flow)

### Request Flow

```
1. Client Application
      ↓
2. OASIS API (api.oasisweb4.com)
      ↓
3. Manager (e.g., AvatarManager, HolonManager)
      ↓
4. OASIS HyperDrive
      ↓
5. Provider Selection (Best available provider)
      ↓
6. Provider (Ethereum, Solana, MongoDB, etc.)
      ↓
7. Response flows back through the chain
```

### Example: Saving Data

```javascript
// 1. Your application makes a simple API call
POST /api/data/save-holon
{
  "name": "MyData",
  "metadata": { "message": "Hello OASIS" }
}

// 2. OASIS HyperDrive automatically:
//    - Saves to MongoDB (fast primary)
//    - Replicates to Arbitrum (blockchain backup)
//    - Replicates to IPFS (permanent storage)

// 3. Returns success - data is now stored across 3 providers
```

### Example: Loading Data (Auto-Failover)

```javascript
// 1. Your application requests data
GET /api/data/load-holon/{id}

// 2. OASIS HyperDrive tries providers in order:
//    - Tries MongoDB → Success! Returns data
//    OR
//    - Tries MongoDB → Timeout (failed)
//    - Tries Arbitrum → Success! Returns data
//    - Your app never knew MongoDB failed

// 3. Result: 100% uptime
```

---

## Architecture Layers

### Layer 1: WEB4 OASIS API (Data & Identity)
- **Purpose:** Core data aggregation and identity layer
- **Total:** 300+ endpoints across 40+ controllers
- **Key APIs:** Avatar (80+), Keys (40+), Data (30+), Wallet (25+), NFT (20+), Karma (20+), and 30+ more APIs
- **Technology:** .NET Core REST API
- **Base URL:** `https://api.oasisweb4.com`
- **Full Catalog:** See [`API_OVERVIEW.md`](API_OVERVIEW.md) for complete list

### Layer 2: Providers (Storage & Networks)
- **Purpose:** Connect to external systems
- **Categories:** Blockchains, Databases, Storage, Networks
- **Count:** 50+ providers (15+ currently active)

### Layer 3: HyperDrive (Orchestration)
- **Purpose:** Intelligent routing, failover, replication
- **Features:** Auto-failover, auto-replication, load balancing
- **Technology:** .NET Core middleware

---

## Common Use Cases

### 1. Multi-Chain Application
**Problem:** Need to support users on Ethereum, Solana, and Polygon  
**OASIS Solution:** One integration, works with all chains automatically

### 2. High Availability Requirement
**Problem:** Single database = single point of failure  
**OASIS Solution:** Auto-failover across multiple providers = 100% uptime

### 3. Cross-Chain Data Sharing
**Problem:** Data on one chain not accessible from another  
**OASIS Solution:** Universal Holon model works across all providers

### 4. Rapid Provider Integration
**Problem:** Adding new blockchain requires weeks of work  
**OASIS Solution:** Just activate provider in OASIS_DNA, works immediately

---

## Quick Reference

### API Endpoints
- **Base URL:** `https://api.oasisweb4.com`
- **Health Check:** `GET /health`
- **Avatar Register:** `POST /api/avatar/register`
- **Save Data:** `POST /api/data/save-holon`
- **Load Data:** `GET /api/data/load-holon/{id}`

### Key Files
- **Configuration:** `OASIS_DNA.json` - Provider and HyperDrive configuration
- **Providers:** Located in `Providers/` directory
- **Core:** Located in `OASIS Architecture/NextGenSoftware.OASIS.API.Core/`

### Important Classes
- **ProviderManager:** Manages provider registration and selection
- **OASISHyperDrive:** Handles failover, replication, load balancing
- **HolonManager:** Data operations
- **AvatarManager:** User management

---

## Next Steps

### For Understanding Architecture
1. Read [`CORE_CONCEPTS.md`](CORE_CONCEPTS.md) for detailed concept explanations
2. Read [`ARCHITECTURE_OVERVIEW.md`](ARCHITECTURE_OVERVIEW.md) for system architecture
3. Read [`HOW_OASIS_WORKS.md`](HOW_OASIS_WORKS.md) for implementation details

### For Integration Tasks
1. Read [`API_OVERVIEW.md`](API_OVERVIEW.md) for complete API catalog (500+ endpoints)
2. Read [`COMMON_TASKS.md`](COMMON_TASKS.md) for typical integration patterns
3. Review API documentation: [`../developers/API_REFERENCE/`](../developers/API_REFERENCE/)
4. Check provider documentation: [`../concepts/PROVIDERS.md`](../concepts/PROVIDERS.md)

### For Deep Dives
- HyperDrive: [`../concepts/HYPERDRIVE.md`](../concepts/HYPERDRIVE.md)
- Providers: [`../concepts/PROVIDERS.md`](../concepts/PROVIDERS.md)
- Managers: [`../concepts/MANAGERS.md`](../concepts/MANAGERS.md)
- Holons: [`../concepts/HOLONS.md`](../concepts/HOLONS.md)
- Avatars: [`../concepts/AVATARS.md`](../concepts/AVATARS.md)

---

## Important Notes for AI Agents

### When Searching for Information

1. **Provider Status:** Check `docs/reference/PROVIDERS/STATUS.md` for accurate provider information
2. **API Endpoints:** Use `docs/developers/API_REFERENCE/` for complete API documentation
3. **Architecture:** Start with this document, then dive into specific concepts
4. **Configuration:** OASIS_DNA.json is the source of truth for runtime configuration

### Common Misconceptions to Avoid

1. ❌ **"All 50+ providers are active"** → Actually, only ~15 are typically active, others need configuration
2. ❌ **"HyperDrive v2 is fully implemented"** → Verify which features are v1 vs v2
3. ❌ **"Every provider supports every operation"** → Providers have different capabilities
4. ❌ **"Data is automatically replicated everywhere"** → Replication is configurable per provider/data type

### Accuracy Tips

- Provider status changes frequently - verify against code or OASIS_DNA
- API endpoints may vary between mainnet/devnet configurations
- HyperDrive behavior depends on OASIS_DNA configuration
- Always check the actual implementation for definitive answers

---

**This document provides a high-level overview. For detailed information, see the referenced documents.**

