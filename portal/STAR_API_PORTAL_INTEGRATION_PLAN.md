# STAR API Portal Integration Plan

## Overview

This document outlines the plan to wire up all STAR API endpoints with the portal. The STAR API auto-generated documentation is located at:
`STAR ODK/NextGenSoftware.OASIS.STAR.WebAPI/docs/STAR_API_AUTO_GENERATED.md`

## ⚠️ IMPORTANT: Endpoint Path Discrepancy

**Issue Found:** The portal code uses **lowercase** endpoint paths (e.g., `/api/nft/`, `/api/quests/`) while the STAR API documentation shows **capitalized** paths (e.g., `/api/NFTs/`, `/api/Quests/`).

**Action Required:** Verify actual endpoint paths from STAR API before implementation. The paths listed in this document follow the documentation format (capitalized), but the actual API may use different casing.

See `STAR_API_CURRENT_USAGE_ANALYSIS.md` for detailed analysis of current usage.

## Current State

The portal currently has the following STAR-related JavaScript modules:
- `star-dashboard.js` - Main STAR dashboard
- `quest-builder.js` - Quest creation interface
- `quest-interface.js` - Quest interaction interface
- `mission-interface.js` - Mission management interface
- `geonft-interface.js` - GeoNFT management

### Currently Implemented Endpoints (4 endpoints)

**Status Legend:**
- ✅ Currently implemented in portal
- ❌ Not implemented (needs to be added)
- ⚠️ Implemented but path needs verification

1. **star-dashboard.js:**
   - ⚠️ `/api/nft/load-all-nfts-for_avatar/${avatarId}` (GET) - **Note:** Uses lowercase path, docs show `/api/NFTs/load-all-for-avatar`

2. **quest-builder.js:**
   - ⚠️ `/api/quests` (POST) - Create quest - **Note:** Uses lowercase path, docs show `/api/Quests`

3. **mission-interface.js:**
   - ⚠️ `/api/missions` (POST) - Create mission - **Note:** Uses lowercase path, docs show `/api/Missions`

4. **geonft-interface.js:**
   - ⚠️ `/api/nft/place-geo-nft` (POST) - Place GeoNFT - **Note:** Uses lowercase path, needs verification

5. **quest-interface.js:**
   - ❌ No endpoints implemented (only TODOs)

## STAR API Base Configuration

**Development:** `http://localhost:50564/api` (from auto-generated docs)
**Production:** `https://star-api.oasisplatform.world/api`

**Authentication:**
- Avatar Authentication: `Authorization: Avatar {AVATAR_ID}`
- JWT Token: `Authorization: Bearer {TOKEN}`

## STAR API Endpoints to Integrate

Based on the auto-generated documentation, the following endpoint categories need to be integrated:

### 1. Avatar Endpoints
- `POST /api/Avatar/authenticate` - Authenticate with STAR API
- `GET /api/Avatar/current` - Get current authenticated avatar

### 2. Quest Endpoints
- `GET /api/Quests` - Get all quests
- `GET /api/Quests/{id}` - Get quest by ID
- `POST /api/Quests` - Create quest
- `PUT /api/Quests/{id}` - Update quest
- `DELETE /api/Quests/{id}` - Delete quest
- `POST /api/Quests/{id}/clone` - Clone quest
- `POST /api/Quests/{id}/publish` - Publish quest
- `POST /api/Quests/search` - Search quests
- `GET /api/Quests/{id}/versions` - Get quest versions
- `POST /api/Quests/{id}/download` - Download quest
- `POST /api/Quests/{id}/edit` - Edit quest
- `POST /api/Quests/{id}/unpublish` - Unpublish quest
- `POST /api/Quests/{id}/republish` - Republish quest
- `POST /api/Quests/{id}/activate` - Activate quest
- `POST /api/Quests/{id}/deactivate` - Deactivate quest
- `GET /api/Quests/{id}/load` - Load quest
- `GET /api/Quests/load-from-path` - Load quest from path
- `GET /api/Quests/load-from-published` - Load quest from published
- `GET /api/Quests/load-all-for-avatar` - Load all quests for avatar
- `POST /api/Quests/{id}/start` - Start quest (if exists)
- `POST /api/Quests/{id}/complete` - Complete quest (if exists)

### 3. Mission Endpoints
- `GET /api/Missions` - Get all missions
- `GET /api/Missions/{id}` - Get mission by ID
- `POST /api/Missions` - Create mission
- `PUT /api/Missions/{id}` - Update mission
- `DELETE /api/Missions/{id}` - Delete mission
- `POST /api/Missions/{id}/clone` - Clone mission
- `POST /api/Missions/{id}/publish` - Publish mission
- `POST /api/Missions/search` - Search missions
- `GET /api/Missions/{id}/versions` - Get mission versions
- `POST /api/Missions/{id}/download` - Download mission
- `POST /api/Missions/{id}/edit` - Edit mission
- `POST /api/Missions/{id}/unpublish` - Unpublish mission
- `POST /api/Missions/{id}/republish` - Republish mission
- `POST /api/Missions/{id}/activate` - Activate mission
- `POST /api/Missions/{id}/deactivate` - Deactivate mission
- `GET /api/Missions/{id}/load` - Load mission
- `GET /api/Missions/load-from-path` - Load mission from path
- `GET /api/Missions/load-from-published` - Load mission from published
- `GET /api/Missions/load-all-for-avatar` - Load all missions for avatar

### 4. NFT Endpoints (STAR API)
- `GET /api/NFTs` - Get all NFTs
- `GET /api/NFTs/{id}` - Get NFT by ID
- `POST /api/NFTs` - Create NFT
- `PUT /api/NFTs/{id}` - Update NFT
- `DELETE /api/NFTs/{id}` - Delete NFT
- `POST /api/NFTs/{id}/clone` - Clone NFT
- `POST /api/NFTs/{id}/publish` - Publish NFT
- `POST /api/NFTs/search` - Search NFTs
- `GET /api/NFTs/{id}/versions` - Get NFT versions
- `POST /api/NFTs/{id}/download` - Download NFT
- `POST /api/NFTs/{id}/edit` - Edit NFT
- `POST /api/NFTs/{id}/unpublish` - Unpublish NFT
- `POST /api/NFTs/{id}/republish` - Republish NFT
- `POST /api/NFTs/{id}/activate` - Activate NFT
- `POST /api/NFTs/{id}/deactivate` - Deactivate NFT
- `GET /api/NFTs/load-all-for-avatar/{avatarId}` - Load all NFTs for avatar

### 5. GeoNFT Endpoints
- `GET /api/GeoNFTs` - Get all GeoNFTs
- `GET /api/GeoNFTs/{id}` - Get GeoNFT by ID
- `POST /api/GeoNFTs` - Create GeoNFT
- `PUT /api/GeoNFTs/{id}` - Update GeoNFT
- `DELETE /api/GeoNFTs/{id}` - Delete GeoNFT
- `POST /api/GeoNFTs/{id}/clone` - Clone GeoNFT
- `POST /api/GeoNFTs/{id}/publish` - Publish GeoNFT
- `POST /api/GeoNFTs/search` - Search GeoNFTs
- `GET /api/GeoNFTs/{id}/versions` - Get GeoNFT versions
- `POST /api/GeoNFTs/{id}/download` - Download GeoNFT
- `POST /api/GeoNFTs/{id}/edit` - Edit GeoNFT
- `POST /api/GeoNFTs/{id}/unpublish` - Unpublish GeoNFT
- `POST /api/GeoNFTs/{id}/republish` - Republish GeoNFT
- `POST /api/GeoNFTs/{id}/activate` - Activate GeoNFT
- `POST /api/GeoNFTs/{id}/deactivate` - Deactivate GeoNFT
- `POST /api/GeoNFTs/place-geo-nft` - Place GeoNFT (if exists)
- `GET /api/GeoNFTs/load-all-for-avatar/{avatarId}` - Load all GeoNFTs for avatar

### 6. OAPP Endpoints
- `GET /api/OAPPs` - Get all OAPPs
- `GET /api/OAPPs/{id}` - Get OAPP by ID
- `POST /api/OAPPs` - Create OAPP
- `PUT /api/OAPPs/{id}` - Update OAPP
- `DELETE /api/OAPPs/{id}` - Delete OAPP
- `POST /api/OAPPs/{id}/clone` - Clone OAPP
- `POST /api/OAPPs/{id}/publish` - Publish OAPP
- `POST /api/OAPPs/search` - Search OAPPs
- `GET /api/OAPPs/{id}/versions` - Get OAPP versions
- `POST /api/OAPPs/{id}/download` - Download OAPP
- `POST /api/OAPPs/{id}/edit` - Edit OAPP
- `POST /api/OAPPs/{id}/unpublish` - Unpublish OAPP
- `POST /api/OAPPs/{id}/republish` - Republish OAPP
- `POST /api/OAPPs/{id}/activate` - Activate OAPP
- `POST /api/OAPPs/{id}/deactivate` - Deactivate OAPP
- `GET /api/OAPPs/load-all-for-avatar` - Load all OAPPs for avatar

### 7. Celestial Bodies Endpoints
- `GET /api/CelestialBodies` - Get all celestial bodies
- `GET /api/CelestialBodies/{id}` - Get celestial body by ID
- `POST /api/CelestialBodies` - Create celestial body
- `PUT /api/CelestialBodies/{id}` - Update celestial body
- `DELETE /api/CelestialBodies/{id}` - Delete celestial body
- `GET /api/CelestialBodies/by-type/{type}` - Get by type
- `GET /api/CelestialBodies/in-space/{spaceId}` - Get in space
- `GET /api/CelestialBodies/search` - Search celestial bodies
- `POST /api/CelestialBodies/create` - Create celestial body
- `GET /api/CelestialBodies/{id}/load` - Load celestial body
- `GET /api/CelestialBodies/load-all-for-avatar` - Load all for avatar
- `POST /api/CelestialBodies/{id}/publish` - Publish celestial body
- `POST /api/CelestialBodies/{id}/download` - Download celestial body
- `GET /api/CelestialBodies/{id}/versions` - Get versions
- `POST /api/CelestialBodies/{id}/activate` - Activate
- `POST /api/CelestialBodies/{id}/deactivate` - Deactivate

### 8. Celestial Spaces Endpoints
- Similar CRUD and STARNET operations as Celestial Bodies

### 9. Chapters Endpoints
- Similar CRUD and STARNET operations

### 10. Inventory Items Endpoints
- Similar CRUD and STARNET operations

### 11. Competition Endpoints
- `GET /api/competition/leaderboard/{competitionType}/{seasonType}` - Get leaderboard
- `GET /api/competition/my-rank/{competitionType}/{seasonType}` - Get my rank
- `GET /api/competition/rank/{avatarId}/{competitionType}/{seasonType}` - Get rank
- `GET /api/competition/leagues/{competitionType}/{seasonType}` - Get leagues
- `GET /api/competition/my-league/{competitionType}/{seasonType}` - Get my league

### 12. Eggs Endpoints (if exists)
- Similar CRUD operations

### 13. Templates, Libraries, Runtimes, Plugins Endpoints
- Similar CRUD and STARNET operations for each

### 14. Parks Endpoints
- Similar CRUD and STARNET operations
- `GET /api/Parks/nearby` - Get nearby parks
- `GET /api/Parks/type/{type}` - Get parks by type

### 15. GeoHotSpots Endpoints
- Similar CRUD and STARNET operations

### 16. Metadata Endpoints
- CelestialBodiesMetaData
- ZomesMetaData
- HolonsMetaData

## Integration Tasks

### Task 0: Verify Endpoint Paths (CRITICAL FIRST STEP)

**Before implementing anything, verify:**
1. Check actual STAR API endpoint paths (run locally or check Swagger)
2. Document correct path casing (capitalized vs lowercase)
3. Document correct path structure (hyphens vs underscores, etc.)
4. Update all endpoint references in this plan with verified paths
5. Create endpoint path reference document

**Current Path Discrepancies to Verify:**
- Portal uses: `/api/nft/load-all-nfts-for_avatar/${avatarId}`
- Docs show: `/api/NFTs/load-all-for-avatar` (different structure and casing)
- Portal uses: `/api/quests` 
- Docs show: `/api/Quests`
- Portal uses: `/api/missions`
- Docs show: `/api/Missions`
- Portal uses: `/api/nft/place-geo-nft`
- Docs show: `/api/GeoNFTs` (may have different placement endpoint)

### Task 1: Create STAR API Client Utility

Create `portal/api/starApiClient.js` with:
- Base URL configuration
- Authentication handling
- Generic API request functions
- Error handling
- Response formatting
- **Centralized endpoint path management** (so paths can be updated in one place)

### Task 2: Update star-dashboard.js

**Currently Has:**
- ✅ NFT loading (but path may be wrong)

**Needs to Add:**
- ❌ Avatar endpoints (current avatar)
- ❌ Quest stats loading
- ❌ Mission stats loading
- ❌ GeoNFT stats loading
- ❌ OAPP loading
- ❌ Recent activity tracking
- ⚠️ Fix NFT loading path if incorrect

### Task 3: Update quest-builder.js

**Currently Has:**
- ✅ Quest creation (but path may be wrong)

**Needs to Add:**
- ❌ Quest publishing (`POST /api/Quests/{id}/publish`)
- ❌ Quest cloning (`POST /api/Quests/{id}/clone`)
- ❌ Quest template loading (from Templates API)
- ⚠️ Fix quest creation path if incorrect

### Task 4: Update quest-interface.js

**Currently Has:**
- ❌ Nothing - only TODO comments

**Needs to Add (ALL):**
- ❌ Quest loading (`GET /api/Quests/{id}` or `/api/quests/{id}`)
- ❌ Quest listing (`GET /api/Quests` or `/api/quests`)
- ❌ Quest starting (if endpoint exists: `POST /api/Quests/{id}/start` or `/api/quests/{id}/start`)
- ❌ Quest completion (if endpoint exists: `POST /api/Quests/{id}/complete` or `/api/quests/{id}/complete`)
- ❌ Quest progress tracking
- ❌ Quest search (`POST /api/Quests/search` or `/api/quests/search`)
- ❌ Load all quests for avatar

### Task 5: Update mission-interface.js

**Currently Has:**
- ✅ Mission creation (but path may be wrong)

**Needs to Add:**
- ❌ Mission listing (`GET /api/Missions` or `/api/missions`)
- ❌ Mission loading (`GET /api/Missions/load-all-for-avatar` or `/api/missions/load-all-for-avatar`)
- ❌ Mission update (`PUT /api/Missions/{id}`)
- ❌ Mission deletion (`DELETE /api/Missions/{id}`)
- ❌ Mission publishing
- ❌ Mission search
- ⚠️ Fix mission creation path if incorrect

### Task 6: Update geonft-interface.js

**Currently Has:**
- ✅ GeoNFT placement (but path may be wrong)

**Needs to Add:**
- ❌ GeoNFT creation (`POST /api/GeoNFTs` or `/api/geonfts`)
- ❌ GeoNFT loading (`GET /api/GeoNFTs/load-all-for-avatar/{avatarId}`)
- ❌ GeoNFT listing
- ❌ GeoNFT search
- ⚠️ Fix GeoNFT placement path if incorrect
- ⚠️ Verify placement endpoint exists and correct path

### Task 7: Create/Update OAPP Management Module

Integrate:
- OAPP listing (`GET /api/OAPPs`)
- OAPP creation (`POST /api/OAPPs`)
- OAPP publishing (`POST /api/OAPPs/{id}/publish`)
- OAPP download (`POST /api/OAPPs/{id}/download`)
- OAPP activation/deactivation

### Task 8: Create Celestial Bodies Module (if needed)

Integrate celestial bodies endpoints if portal needs this functionality

### Task 9: Create Competition Module (if needed)

Integrate competition/leaderboard endpoints

### Task 10: Update Configuration

Update portal configuration to use STAR API base URL from centralized config

## Implementation Approach

1. **Create STAR API Client First** - This will be the foundation for all integrations
2. **Update Existing Modules** - Start with modules that already exist
3. **Add New Modules** - Add modules for new functionality (Competition, etc.)
4. **Test Integration** - Test each integration with actual STAR API
5. **Error Handling** - Implement comprehensive error handling
6. **Loading States** - Add loading indicators for all async operations

## Next Steps

### Phase 1: Verification (MUST DO FIRST)
1. ✅ Read STAR_API_AUTO_GENERATED.md (done)
2. ✅ Analyze current portal usage (done - see STAR_API_CURRENT_USAGE_ANALYSIS.md)
3. ❌ **VERIFY ACTUAL ENDPOINT PATHS** - Test STAR API locally or check Swagger UI
4. ❌ Document correct endpoint paths
5. ❌ Update this plan with verified paths

### Phase 2: Foundation
6. Create STAR API client utility with verified paths
7. Create endpoint path configuration file (centralized path management)

### Phase 3: Fix Existing Implementations
8. Fix path discrepancies in currently implemented endpoints
9. Test existing endpoints with verified paths
10. Update error handling in existing code

### Phase 4: Implement Missing Endpoints
11. Update star-dashboard.js with missing endpoints
12. Update quest-interface.js (currently has nothing)
13. Add missing endpoints to quest-builder.js
14. Add missing endpoints to mission-interface.js
15. Add missing endpoints to geonft-interface.js

### Phase 5: Testing
16. Test all integrations with STAR API
17. Update error handling across all modules
18. Add loading states
19. Document final endpoint mappings

## Reference Documents

- **STAR_API_AUTO_GENERATED.md** - Complete STAR API documentation
- **STAR_API_CURRENT_USAGE_ANALYSIS.md** - Analysis of what's currently in portal
- **This document** - Integration plan

