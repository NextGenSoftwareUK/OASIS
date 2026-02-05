/**
 * Quake - OASIS STAR API Integration
 * 
 * This header provides integration hooks for Quake to connect with the
 * OASIS STAR API for cross-game item sharing.
 * 
 * Integration Points:
 * - Key collection (Touch_Item)
 * - Door opening (door_use)
 * - Item pickup tracking
 */

#ifndef QUAKE_STAR_INTEGRATION_H
#define QUAKE_STAR_INTEGRATION_H

#include "../NativeWrapper/star_api.h"

#ifdef __cplusplus
extern "C" {
#endif

// Quake-specific item types
#define QUAKE_ITEM_SILVER_KEY    "silver_key"
#define QUAKE_ITEM_GOLD_KEY     "gold_key"
#define QUAKE_ITEM_RUNE1        "rune_1"
#define QUAKE_ITEM_RUNE2        "rune_2"
#define QUAKE_ITEM_QUAD         "quad_damage"
#define QUAKE_ITEM_PENTAGRAM    "pentagram"

// Initialize STAR API integration for Quake
void Quake_STAR_Init(void);

// Cleanup STAR API integration
void Quake_STAR_Cleanup(void);

// Called when player picks up a key
void Quake_STAR_OnKeyPickup(const char* key_name);

// Called when player tries to open a door
bool Quake_STAR_CheckDoorAccess(const char* door_name, const char* required_key);

// Called when player picks up any item
void Quake_STAR_OnItemPickup(const char* item_name, const char* item_description);

// Check if player has a keycard from another game (e.g., Doom)
bool Quake_STAR_HasCrossGameKeycard(const char* keycard_name);

#ifdef __cplusplus
}
#endif

#endif // QUAKE_STAR_INTEGRATION_H



