/**
 * Doom - OASIS STAR API Integration
 * 
 * This header provides integration hooks for Doom to connect with the
 * OASIS STAR API for cross-game item sharing.
 * 
 * Integration Points:
 * - Keycard collection (P_TouchSpecialThing)
 * - Door opening (P_UseSpecialLine)
 * - Item pickup tracking
 */

#ifndef DOOM_STAR_INTEGRATION_H
#define DOOM_STAR_INTEGRATION_H

#include "../NativeWrapper/star_api.h"

#ifdef __cplusplus
extern "C" {
#endif

// Doom-specific item types
#define DOOM_ITEM_RED_KEYCARD    "red_keycard"
#define DOOM_ITEM_BLUE_KEYCARD  "blue_keycard"
#define DOOM_ITEM_YELLOW_KEYCARD "yellow_keycard"
#define DOOM_ITEM_SKULL_KEY     "skull_key"
#define DOOM_ITEM_BERSERK       "berserk_pack"
#define DOOM_ITEM_INVULN        "invulnerability"

// Initialize STAR API integration for Doom
void Doom_STAR_Init(void);

// Cleanup STAR API integration
void Doom_STAR_Cleanup(void);

// Called when player picks up a keycard
void Doom_STAR_OnKeycardPickup(int keycard_type);

// Called when player tries to open a door
bool Doom_STAR_CheckDoorAccess(int door_line, int required_key);

// Called when player picks up any item
void Doom_STAR_OnItemPickup(const char* item_name, const char* item_description);

// Check if player has a keycard from another game (e.g., Quake)
bool Doom_STAR_HasCrossGameKeycard(const char* keycard_name);

#ifdef __cplusplus
}
#endif

#endif // DOOM_STAR_INTEGRATION_H



