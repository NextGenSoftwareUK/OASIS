# WEB5 STAR API Overview

## What is WEB5 STAR API?

The **WEB5 STAR API** is the gamification and metaverse layer that runs on top of the [WEB4 OASIS API](../web4-oasis-api/overview.md). It provides mission systems, quests, location-based GeoNFTs, celestial worlds, OASIS Applications (OAPPs), inventory, competition, eggs, and map services for building immersive metaverse and game experiences.

**Base URL (same host as WEB4):** `http://api.oasisweb4.com/api`  
**Alternative STAR host (when deployed):** `https://star-api.oasisweb4.com` — confirm in your environment.

**Swagger UI:** [http://api.oasisweb4.com/swagger/index.html](http://api.oasisweb4.com/swagger/index.html)

---

## Key Features

### Game mechanics
- **Missions** – Create, assign, complete missions; leaderboards; rewards
- **Quests** – Quest lifecycle, objectives, completion, analytics
- **Competition** – Leaderboards, leagues, tournaments, ranks
- **Eggs** – Discover, hatch, egg quests and leaderboards

### Location & AR
- **GeoNFTs** – Location-based NFTs for AR/VR; place, mint-and-place, discover nearby (see also [WEB4 NFT API](../web4-oasis-api/blockchain-wallets/nft-api.md) GeoNFT endpoints)
- **Map** – Search, routes, nearby locations, visit history, draw on map

### Celestial & worlds
- **Celestial Bodies** – Virtual world objects (planets, stars, moons); create, explore, colonize
- **Celestial Spaces** – Spaces/regions in the metaverse

### Development & apps
- **OAPPs** – OASIS Application management (install, deploy, launch, stop) — when enabled
- **Templates, runtimes, libraries** – STAR ODK development assets

### Data & inventory
- **Inventory** – Virtual items; use, transfer, user inventory
- **Chapters, holons** – Narrative and data structures for STAR experiences

---

## API Categories

### 1. Game Mechanics

| API | Description | Endpoints (intended / live) |
|-----|-------------|----------------------------|
| [Missions API](game-mechanics/missions-api.md) | Mission and quest management | `/api/missions` |
| [Quests API](game-mechanics/quests-api.md) | Quest creation and completion | `/api/quests` |
| **Competition** | Leaderboards, leagues, tournaments | `/api/competition` (live) |
| **Eggs** | Discover, hatch, egg quests | `/api/eggs` (live) |

### 2. Location Services

| API | Description | Endpoints |
|-----|-------------|-----------|
| [GeoNFTs API](location-services/geonfts-api.md) | Location-based NFTs for AR/VR | `/api/nft` (GeoNFT routes, live) + `/api/geonfts` (when available) |
| **Map API** | Search, routes, nearby, visit history | `/api/map` (live) |

### 3. Celestial Systems

| API | Description | Endpoints |
|-----|-------------|-----------|
| [Celestial Bodies API](celestial-systems/celestial-bodies-api.md) | Virtual world objects and spaces | `/api/celestialbodies` |

### 4. Development Tools

| API | Description | Endpoints |
|-----|-------------|-----------|
| [OAPPs API](development-tools/oapps-api.md) | OASIS Application management | `/api/oapps` (when enabled) |

### 5. Data Structures

| API | Description | Endpoints |
|-----|-------------|-----------|
| [Inventory API](data-structures/inventory-api.md) | Virtual item management | `/api/inventoryitems` |

---

## Live WEB4 endpoints used by STAR

Many STAR features are exposed today via the **same API host** as WEB4:

| Feature | WEB4 endpoint | Notes |
|---------|----------------|-------|
| **GeoNFTs** | `GET/POST /api/nft/load-all-geo-nfts-for-avatar/{avatarId}`, `place-geo-nft`, `mint-and-place-geo-nft`, `search-web4-geo-nfts` | See [NFT API – GeoNFT](../web4-oasis-api/blockchain-wallets/nft-api.md) |
| **Competition** | `GET /api/competition/leaderboard/{competitionType}/{seasonType}`, `my-rank`, `rank`, `leagues`, `tournaments`, `stats` | Live |
| **Eggs** | `GET /api/eggs/get-all-eggs`, `get-current-egg-quests`, `my-eggs`; `POST discover`, `hatch/{eggId}` | Live |
| **Map** | `POST /api/map/search`, `GET nearby`, `visit/{locationId}`, `visit-history`, `search-locations`, `stats`; route/draw endpoints | Live |

Use [Swagger](http://api.oasisweb4.com/swagger/index.html) to confirm which STAR endpoints are currently deployed (e.g. `/api/missions`, `/api/quests`, `/api/celestialbodies`, `/api/inventoryitems`, `/api/oapps`).

---

## Authentication

Same as WEB4: **JWT Bearer token** from [Avatar authenticate](../getting-started/authentication.md).

```http
Authorization: Bearer YOUR_JWT_TOKEN
```

Always check **isError** and **message** in responses; many endpoints return **HTTP 200** with a body indicating "Unauthorized" when the token is missing or invalid.

---

## Response format

Standard OASIS wrapper:

```json
{
  "result": { ... },
  "isError": false,
  "message": "Success"
}
```

Error (often HTTP 200):

```json
{
  "result": null,
  "isError": true,
  "message": "Error message",
  "errorCode": "ERROR_CODE"
}
```

---

## Getting started

1. **Authenticate** – Use [WEB4 Avatar API](../web4-oasis-api/authentication-identity/avatar-api.md) to register and authenticate; get a JWT.
2. **Use live STAR endpoints** – Call Competition, Eggs, Map, and NFT GeoNFT endpoints on `http://api.oasisweb4.com/api`.
3. **Check Swagger** – For missions, quests, celestial, inventory, OAPPs, confirm paths and availability on your deployment.

---

## Quick links

- [WEB4 OASIS API](../web4-oasis-api/overview.md) – Data, identity, wallets, NFTs
- [NFT API (GeoNFT)](../web4-oasis-api/blockchain-wallets/nft-api.md) – GeoNFT place, mint-and-place, load, search
- [Getting Started](../getting-started/overview.md)
- [Error codes](../reference/error-codes.md) | [Rate limits](../reference/rate-limits.md)

---

*Last Updated: January 24, 2026*
