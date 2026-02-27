# OASIS STAR API – Game Integration Guide

## Overview

This guide describes how **ODOOM** and **OQuake** (and other games) integrate with the OASIS STAR API for cross-game item sharing, quests, and avatar/SSO. For setup (repos, tools, build, config), use **[DEVELOPER_ONBOARDING.md](DEVELOPER_ONBOARDING.md)**.

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Phase 1: Cross-Game Item Sharing](#phase-1-cross-game-item-sharing)
3. [Phase 2: Multi-Game Quests](#phase-2-multi-game-quests)
4. [Inventory NFT minting](#inventory-nft-minting)
5. [Future: NFT Boss Collection](#future-nft-boss-collection)
6. [Setup Instructions](#setup-instructions)
7. [API Reference](#api-reference)
8. [Troubleshooting](#troubleshooting)

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                    Game Engines (C/C++)                     │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐     │
│  │  ODOOM   │  │  OQuake  │  │  Doom II │  │  Others  │     │
│  │ (UZDoom) │  │(vkQuake) │  │          │  │          │     │
│  └────┬─────┘  └────┬─────┘  └────┬─────┘  └────┬─────┘     │
└───────┼─────────────┼─────────────┼─────────────┼───────────┘
        │             │             │             │
        └─────────────┴─────────────┴─────────────┘
                      │
        ┌─────────────▼─────────────┐
        │   STARAPIClient           │
        │   (star_api.dll / C ABI)  │
        │   C# client → star_api_*  │
        └─────────────┬─────────────┘
                      │
         ┌────────────┴────────────┐
         │                         │
┌────────▼────────┐    ┌───────────▼─────────┐
│  OASIS API      │    │  OASIS STAR API     │
│  (WEB4)         │    │  (WEB5)             │
│  Avatar / SSO   │    │  Inventory • Quests │
│  NFT mint       │    │  REST/HTTP          │
└─────────────────┘    └─────────────────────┘
```

## Phase 1: Cross-Game Item Sharing

### Goal

Enable players to collect items (keycards, keys) in one game and use them in another game.

### Implementation Steps

#### 1. Item Collection Tracking

When a player picks up an item in-game, it's automatically added to their STAR API inventory:

```c
// In Doom: Player picks up red keycard
Doom_STAR_OnKeycardPickup(1);  // Adds to STAR API

// In Quake: Player picks up silver key
Quake_STAR_OnKeyPickup("silver_key");  // Adds to STAR API
```

#### 2. Cross-Game Item Usage

When a player tries to use an item (e.g., open a door), the game checks both local and cross-game inventory. The **local inventory cache** lives in the **STAR API client** (STARAPIClient): `star_api_get_inventory`, `star_api_has_item`, and `star_api_use_item` use the client’s cache first and only hit the API when the cache is null or the item is not found. Games (ODOOM, OQuake, etc.) can call these APIs directly; no need to keep a separate game-side inventory list for door/has/use. See STARAPIClient README for “STARAPIClient vs star_sync” and “Local inventory cache”.

```c
// In Quake: Check if player can open door
bool can_open = Quake_STAR_CheckDoorAccess("door_123", "silver_key");
// This checks:
// 1. Local Quake inventory
// 2. STAR API inventory (from Doom, etc.)
// 3. Compatible items (e.g., Doom red keycard = Quake silver key)
```

#### 3. Item Mapping

Items from different games can be mapped to each other for compatibility:

```json
{
  "itemMappings": {
    "doom": {
      "red_keycard": {
        "compatibleGames": ["quake"],
        "quakeEquivalent": "silver_key"
      }
    }
  }
}
```

### Example Flow

1. **Player in Doom:**
   - Picks up red keycard
   - `Doom_STAR_OnKeycardPickup(1)` called
   - Red keycard added to STAR API inventory

2. **Player in Quake:**
   - Approaches door requiring silver key
   - Quake checks local inventory (not found)
   - Quake checks STAR API: `star_api_has_item("red_keycard")` → true
   - Door opens using Doom's red keycard!

## Phase 2: Multi-Game Quests

### Goal

Create quests that span multiple games, requiring players to complete objectives in different games.

### Implementation

#### 1. Quest Definition

```json
{
  "name": "Cross-Dimensional Keycard Hunt",
  "description": "Collect keycards from multiple dimensions",
  "objectives": [
    {
      "game": "doom",
      "item": "red_keycard",
      "description": "Collect red keycard in Doom"
    },
    {
      "game": "quake",
      "item": "silver_key",
      "description": "Collect silver key in Quake"
    }
  ],
  "reward": {
    "type": "special_item",
    "name": "master_keycard",
    "description": "Opens all doors in all games"
  }
}
```

#### 2. Quest Tracking

```c
// Check quest progress
bool objective1_complete = star_api_is_quest_objective_complete(
    "cross_dimensional_keycard_hunt",
    "doom_red_keycard"
);

// When objective completed
if (objective1_complete && objective2_complete) {
    // Quest complete! Give reward
    star_api_add_item("master_keycard", "Master Keycard", "Quest Reward", "SpecialItem");
}
```

### Example Quest Flow

1. **Quest Started:**
   - Player accepts "Cross-Dimensional Keycard Hunt" quest
   - Quest stored in STAR API

2. **Objective 1 (Doom):**
   - Player collects red keycard in Doom
   - Objective marked complete in STAR API

3. **Objective 2 (Quake):**
   - Player collects silver key in Quake
   - Objective marked complete in STAR API

4. **Quest Complete:**
   - All objectives complete
   - Master keycard reward given
   - Available in all games!

## Inventory NFT minting

When enabled in **oasisstar.json** (ODOOM and OQuake), collecting items can **mint an NFT** (WEB4 NFTHolon) and attach it to the inventory item. Config keys:

- **mint_weapons**, **mint_armor**, **mint_powerups**, **mint_keys** – Set to `1` to mint when collecting that category; `0` to disable.
- **nft_provider** – Provider name (e.g. `SolanaOASIS`).

The games call `star_api_mint_inventory_nft` (STARAPIClient C ABI) when mint is on for the item type; the mint is synchronous; the resulting NFT ID is stored with the item in STAR inventory. In the in-game inventory popup, minted items show **[NFT]** and can be grouped separately.

## Future: NFT Boss Collection

### Goal

Enable players to collect bosses as NFTs in one game and deploy them as allies in another game.

### Concept

1. **Boss Collection:**
   ```c
   // Player defeats boss in Doom
   star_api_create_boss_nft(
       "cyberdemon",
       "Cyberdemon from Doom",
       boss_stats,
       boss_model_data
   );
   ```

2. **Boss Deployment:**
   ```c
   // Player deploys boss in Quake
   bool deployed = star_api_deploy_boss_nft(
       "cyberdemon_nft_id",
       "quake_level_1"
   );
   
   if (deployed) {
       // Spawn boss as ally in Quake
       SpawnAllyBoss("cyberdemon", boss_stats);
   }
   ```

### NFT Structure

```json
{
  "nftId": "cyberdemon_12345",
  "name": "Cyberdemon",
  "description": "Defeated in Doom E1M8",
  "gameSource": "doom",
  "stats": {
    "health": 4000,
    "damage": 100,
    "speed": 50
  },
  "modelData": "...",
  "collectedAt": "2024-01-15T10:30:00Z"
}
```

## Setup Instructions

### Prerequisites

1. **STAR API Access:**
   - Get API key from OASIS platform
   - Get Avatar ID for player identification
   - Ensure network access to STAR API

2. **Build Tools:**
   - C/C++ compiler (GCC, Clang, MSVC)
   - CMake 3.10+
   - On Linux/Mac: libcurl development libraries
   - On Windows: Windows SDK (for WinHTTP)

### Step 1: Build STAR API client (STARAPIClient)

ODOOM and OQuake use **STARAPIClient** only. Build from OASIS repo root:

```powershell
dotnet publish "OASIS Omniverse/STARAPIClient/STARAPIClient.csproj" -c Release -r win-x64 -p:PublishAot=true -p:SelfContained=true -p:NoWarn=NU1605
```

Or use the game build scripts (`BUILD ODOOM.bat` / `BUILD_OQUAKE.bat`), which use or build STARAPIClient. Do not use NativeWrapper.

### Step 2: Configure API Credentials

Set environment variables:

```bash
export STAR_API_KEY="your_api_key_here"
export STAR_AVATAR_ID="your_avatar_id_here"
```

Or edit `Config/star_api_config.json`:

```json
{
  "starApiBaseUrl": "https://star-api.oasisplatform.world/api",
  "apiKey": "your_api_key_here",
  "avatarId": "your_avatar_id_here"
}
```

### Step 3: Integrate into Game

Follow game-specific integration guides:
- [Doom Integration Guide](Doom/README.md)
- [Quake Integration Guide](Quake/README.md)

### Step 4: Test Integration

1. Start game with STAR API integration
2. Collect an item (e.g., keycard)
3. Verify item appears in STAR API inventory
4. Start another game
5. Verify item is accessible in second game

## API Reference

### C/C++ API

#### Initialization

```c
star_api_config_t config = {
    .base_url = "https://star-api.oasisplatform.world/api",
    .api_key = "your_api_key",
    .avatar_id = "your_avatar_id",
    .timeout_seconds = 10
};

star_api_result_t result = star_api_init(&config);
```

#### Check for Item

```c
bool has_item = star_api_has_item("red_keycard");
```

#### Add Item

```c
star_api_result_t result = star_api_add_item(
    "red_keycard",
    "Red Keycard - Opens red doors",
    "Doom",
    "KeyItem"
);
```

#### Use Item

```c
bool used = star_api_use_item("red_keycard", "door_123");
```

#### Get Inventory

```c
star_item_list_t* inventory = NULL;
star_api_result_t result = star_api_get_inventory(&inventory);

if (result == STAR_API_SUCCESS) {
    for (size_t i = 0; i < inventory->count; i++) {
        printf("Item: %s\n", inventory->items[i].name);
    }
    star_api_free_item_list(inventory);
}
```

### C# API

See `STARAPIClient/GameIntegrationClient.cs` for full C# API documentation.

## Troubleshooting

### Common Issues

#### 1. STAR API Not Initializing

**Symptoms:** `star_api_init()` returns error

**Solutions:**
- Verify `STAR_API_KEY` and `STAR_AVATAR_ID` are set
- Check network connectivity to STAR API
- Verify API key is valid
- Check firewall settings

#### 2. Items Not Appearing in Cross-Game Inventory

**Symptoms:** Item collected but not visible in other game

**Solutions:**
- Check console output for API errors
- Verify item name matches between games
- Check STAR API logs
- Ensure API call succeeded (check return value)

#### 3. Doors Not Opening with Cross-Game Keys

**Symptoms:** Has keycard but door won't open

**Solutions:**
- Verify `star_api_has_item()` returns true
- Check door access logic is properly integrated
- Verify keycard name matches
- Check item mapping configuration

#### 4. Network Timeouts

**Symptoms:** API calls timeout

**Solutions:**
- Increase timeout in config
- Check network connectivity
- Verify STAR API is accessible
- Check for proxy/firewall issues

### Debug Mode

Enable debug logging:

```c
// Set log level
star_api_set_log_level(STAR_API_LOG_DEBUG);

// Check last error
const char* error = star_api_get_last_error();
printf("Last error: %s\n", error);
```

## Best Practices

1. **Error Handling:**
   - Always check return values
   - Handle network failures gracefully
   - Fall back to local-only mode if API unavailable

2. **Performance:**
   - Cache inventory locally
   - Batch API calls when possible
   - Use async callbacks for non-blocking operations

3. **Security:**
   - Never hardcode API keys
   - Use environment variables or secure config files
   - Validate all API responses

4. **Item Naming:**
   - Use consistent naming conventions
   - Document item mappings
   - Use descriptive names

## Next steps

1. **Setup:** Use [DEVELOPER_ONBOARDING.md](DEVELOPER_ONBOARDING.md) to clone repos, install tools, and build ODOOM/OQuake.
2. **Phase 1:** Build and run ODOOM and OQuake; test cross-game item sharing (keycards, keys, ammo).
3. **Phase 2:** Use the quest system; see [PHASE2_QUEST_SYSTEM.md](PHASE2_QUEST_SYSTEM.md).
4. **Phase 3:** NFT boss collection (foundation in place).

## Support

- **Setup and build:** [DEVELOPER_ONBOARDING.md](DEVELOPER_ONBOARDING.md), [ODOOM/README.md](ODOOM/README.md), [OQuake/README.md](OQuake/README.md), [STARAPIClient/README.md](STARAPIClient/README.md)
- **Troubleshooting:** Game-specific WINDOWS_INTEGRATION.md files; STAR API logs (e.g. `star_api.log` in game exe folder)

## License

This integration follows the same license as the OASIS project.



