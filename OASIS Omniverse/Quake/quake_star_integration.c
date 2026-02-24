/**
 * Quake - OASIS STAR API Integration Implementation
 * 
 * This file implements the integration between Quake and the OASIS STAR API.
 * 
 * Integration Points:
 * 1. When player picks up a key, it's added to STAR API inventory
 * 2. When player tries to open a door, check both local and cross-game inventory
 * 3. Track all item pickups for cross-game quests
 */

#include "quake_star_integration.h"
#include <stdio.h>
#include <string.h>
#include <stdlib.h>

static star_api_config_t g_star_config;
static bool g_star_initialized = false;

void Quake_STAR_Init(void) {
    // Load configuration from file or environment
    g_star_config.base_url = "https://star-api.oasisweb4.com/api";
    g_star_config.api_key = getenv("STAR_API_KEY");
    g_star_config.avatar_id = getenv("STAR_AVATAR_ID");
    g_star_config.timeout_seconds = 30;
    
    if (!g_star_config.api_key || !g_star_config.avatar_id) {
        Con_Printf("STAR API: Warning - API key or Avatar ID not set. Cross-game features disabled.\n");
        return;
    }
    
    star_api_result_t result = star_api_init(&g_star_config);
    if (result == STAR_API_SUCCESS) {
        g_star_initialized = true;
        Con_Printf("STAR API: Initialized successfully. Cross-game features enabled.\n");
    } else {
        Con_Printf("STAR API: Failed to initialize: %s\n", star_api_get_last_error());
    }
}

void Quake_STAR_Cleanup(void) {
    if (g_star_initialized) {
        star_api_cleanup();
        g_star_initialized = false;
        Con_Printf("STAR API: Cleaned up.\n");
    }
}

void Quake_STAR_OnKeyPickup(const char* key_name) {
    if (!g_star_initialized || !key_name) {
        return;
    }
    
    const char* description = NULL;
    if (strcmp(key_name, QUAKE_ITEM_SILVER_KEY) == 0) {
        description = "Silver Key - Opens silver-marked doors";
    } else if (strcmp(key_name, QUAKE_ITEM_GOLD_KEY) == 0) {
        description = "Gold Key - Opens gold-marked doors";
    } else {
        description = "Key from Quake";
    }
    
    // Add key to STAR API inventory
    star_api_result_t result = star_api_add_item(
        key_name,
        description,
        "Quake",
        "KeyItem"
    );
    
    if (result == STAR_API_SUCCESS) {
        Con_Printf("STAR API: Added %s to cross-game inventory.\n", key_name);
    } else {
        Con_Printf("STAR API: Failed to add %s: %s\n", key_name, star_api_get_last_error());
    }
}

bool Quake_STAR_CheckDoorAccess(const char* door_name, const char* required_key) {
    if (!g_star_initialized || !required_key) {
        return false;
    }
    
    // First check local Quake inventory
    // (This would be done by Quake's normal key checking)
    
    // Then check cross-game inventory
    if (star_api_has_item(required_key)) {
        Con_Printf("STAR API: Door '%s' opened using cross-game key: %s\n", door_name, required_key);
        star_api_use_item(required_key, door_name);
        return true;
    }
    
    return false;  // Let Quake's normal logic handle it
}

void Quake_STAR_OnItemPickup(const char* item_name, const char* item_description) {
    if (!g_star_initialized || !item_name) {
        return;
    }
    
    // Add item to cross-game inventory
    star_api_result_t result = star_api_add_item(
        item_name,
        item_description ? item_description : "Item from Quake",
        "Quake",
        "Miscellaneous"
    );
    
    if (result == STAR_API_SUCCESS) {
        Con_Printf("STAR API: Added %s to cross-game inventory.\n", item_name);
    }
}

bool Quake_STAR_HasCrossGameKeycard(const char* keycard_name) {
    if (!g_star_initialized || !keycard_name) {
        return false;
    }
    
    return star_api_has_item(keycard_name);
}

/**
 * Integration Hook Example for Quake Source Code:
 * 
 * In items.c, modify Touch_Item():
 * 
 * void Touch_Item(edict_t* ent, edict_t* other, cplane_t* plane, csurface_t* surf) {
 *     // ... existing code ...
 *     
 *     // Add STAR API integration
 *     if (ent->item && (ent->item->flags & IT_KEY)) {
 *         Quake_STAR_OnKeyPickup(ent->item->pickup_name);
 *     }
 *     
 *     // ... rest of existing code ...
 * }
 * 
 * In doors.c, modify door_use():
 * 
 * void door_use(edict_t* self, edict_t* other, edict_t* activator) {
 *     // ... existing code ...
 *     
 *     // Check cross-game inventory if local check fails
 *     if (self->spawnflags & DOOR_KEY_LOCKED) {
 *         const char* required_key = GetRequiredKey(self);
 *         if (!HasLocalKey(required_key)) {
 *             if (Quake_STAR_CheckDoorAccess(self->targetname, required_key)) {
 *                 // Door opened with cross-game key!
 *                 door_go_up(self, activator);
 *                 return;
 *             }
 *         }
 *     }
 *     
 *     // ... rest of existing code ...
 * }
 */



