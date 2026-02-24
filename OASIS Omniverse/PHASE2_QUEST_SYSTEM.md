# Phase 2: Multi-Game Quest System

## Overview

Phase 2 implements a comprehensive quest system that spans multiple games, allowing players to complete objectives across Doom, Quake, and other integrated games.

## Features

- **Cross-Game Quests**: Quests with objectives in multiple games
- **Automatic Progress Tracking**: Objectives automatically tracked when items are collected
- **Quest Rewards**: Special items and NFTs as rewards
- **Quest Notifications**: In-game notifications for quest progress

## Quest Structure

```json
{
  "id": "cross_dimensional_keycard_hunt",
  "name": "Cross-Dimensional Keycard Hunt",
  "description": "Collect keycards from multiple dimensions to unlock the Master Keycard",
  "type": "CrossGame",
  "difficulty": "Medium",
  "objectives": [
    {
      "id": "doom_red_keycard",
      "description": "Collect red keycard in Doom",
      "game": "doom",
      "item": "red_keycard",
      "status": "Pending"
    },
    {
      "id": "quake_silver_key",
      "description": "Collect silver key in Quake",
      "game": "quake",
      "item": "silver_key",
      "status": "Pending"
    }
  ],
  "rewards": {
    "items": ["master_keycard"],
    "karma": 100,
    "experience": 50
  }
}
```

## Implementation

### 1. Quest Creation

Quests can be created via the STAR API or predefined in the system:

```c
// Create a cross-game quest
star_api_result_t result = star_api_create_quest(
    "cross_dimensional_keycard_hunt",
    "Cross-Dimensional Keycard Hunt",
    "Collect keycards from multiple dimensions",
    objectives_array,
    num_objectives
);
```

### 2. Automatic Objective Tracking

When items are collected, the system automatically checks for active quests:

```c
// In doom_star_integration.c
void Doom_STAR_OnKeycardPickup(int keycard_type) {
    // Add to inventory
    star_api_add_item(...);
    
    // Check active quests and update objectives
    // This happens server-side automatically when item is added
}
```

### 3. Quest Progress Checking

```c
// Check if a quest objective is complete
bool objective_complete = star_api_is_quest_objective_complete(
    "cross_dimensional_keycard_hunt",
    "doom_red_keycard"
);

// Complete a quest objective manually (if needed)
star_api_result_t result = star_api_complete_quest_objective(
    "cross_dimensional_keycard_hunt",
    "doom_red_keycard",
    "Doom"
);
```

### 4. Quest Completion

When all objectives are complete, the quest can be completed:

```c
// Complete quest and claim rewards
star_api_result_t result = star_api_complete_quest(
    "cross_dimensional_keycard_hunt"
);

if (result == STAR_API_SUCCESS) {
    // Quest rewards are automatically added to inventory
    printf("Quest completed! Rewards added to inventory.\n");
}
```

## Example Quest Flow

### Quest: "The Keymaster's Challenge"

**Objective 1: Doom**
- Player collects red keycard in Doom
- System detects item collection
- Quest objective "doom_red_keycard" marked complete

**Objective 2: Quake**
- Player collects silver key in Quake
- System detects item collection
- Quest objective "quake_silver_key" marked complete

**Quest Complete:**
- All objectives complete
- Master Keycard reward added to inventory
- Available in all games!

## Integration Points

### Doom Integration

```c
// In doom_star_integration.c
void Doom_STAR_OnKeycardPickup(int keycard_type) {
    const char* keycard_name = GetKeycardName(keycard_type);
    
    // Add to inventory (triggers quest check server-side)
    star_api_add_item(keycard_name, ...);
    
    // Optional: Check quest progress locally
    // (Server-side checking is automatic)
}
```

### Quake Integration

```c
// In quake_star_integration.c
void Quake_STAR_OnKeyPickup(const char* key_name) {
    // Add to inventory (triggers quest check server-side)
    star_api_add_item(key_name, ...);
}
```

## Quest Types

### 1. Collection Quests
- Collect specific items across games
- Example: "Collect all keycard types"

### 2. Exploration Quests
- Visit specific locations in different games
- Example: "Visit the secret room in Doom E1M1 and Quake E1M1"

### 3. Combat Quests
- Defeat enemies across games
- Example: "Defeat 100 enemies total across Doom and Quake"

### 4. Achievement Quests
- Complete specific achievements
- Example: "Complete Doom on Nightmare and Quake on Hard"

## Quest Rewards

### Item Rewards
- Special items only obtainable through quests
- Example: Master Keycard, Universal Key

### NFT Rewards
- Unique NFTs as quest completion rewards
- Example: Quest Completion Badge NFT

### Experience & Karma
- XP and Karma points for quest completion
- Used for avatar progression

## Server-Side Quest Management

The STAR API handles:
- Quest state management
- Objective progress tracking
- Automatic objective completion detection
- Reward distribution
- Quest notification system

## Client-Side Integration

Games only need to:
1. Add items to inventory (automatic quest checking)
2. Optionally check quest progress
3. Display quest notifications
4. Handle quest completion rewards

## Future Enhancements

- **Dynamic Quests**: Procedurally generated quests
- **Cooperative Quests**: Multi-player quests
- **Quest Chains**: Sequential quests with storylines
- **Seasonal Quests**: Time-limited quests
- **Community Quests**: Server-wide quests



