/**
 * Doom - OASIS STAR API Integration Implementation
 * 
 * This file implements the integration between Doom and the OASIS STAR API.
 * 
 * Integration Points:
 * 1. When player picks up a keycard, it's added to STAR API inventory
 * 2. When player tries to open a door, check both local and cross-game inventory
 * 3. Track all item pickups for cross-game quests
 */

#include "doom_star_integration.h"
#include <stdio.h>
#include <string.h>

static star_api_config_t g_star_config;
static bool g_star_initialized = false;

// Map Doom keycard types to item names
static const char* GetKeycardName(int keycard_type) {
    switch (keycard_type) {
        case 1: return DOOM_ITEM_RED_KEYCARD;
        case 2: return DOOM_ITEM_BLUE_KEYCARD;
        case 3: return DOOM_ITEM_YELLOW_KEYCARD;
        case 4: return DOOM_ITEM_SKULL_KEY;
        default: return NULL;
    }
}

// Map Doom keycard types to descriptions
static const char* GetKeycardDescription(int keycard_type) {
    switch (keycard_type) {
        case 1: return "Red Keycard - Opens red doors";
        case 2: return "Blue Keycard - Opens blue doors";
        case 3: return "Yellow Keycard - Opens yellow doors";
        case 4: return "Skull Key - Opens skull-marked doors";
        default: return "Unknown Keycard";
    }
}

void Doom_STAR_Init(void) {
    // Load configuration from file or environment
    g_star_config.base_url = "https://star-api.oasisplatform.world/api";
    g_star_config.api_key = getenv("STAR_API_KEY");  // Optional
    g_star_config.avatar_id = getenv("STAR_AVATAR_ID");  // Optional
    g_star_config.timeout_seconds = 10;
    
    star_api_result_t result = star_api_init(&g_star_config);
    if (result != STAR_API_SUCCESS) {
        printf("STAR API: Failed to initialize: %s\n", star_api_get_last_error());
        return;
    }
    
    // Try SSO authentication first (recommended)
    const char* username = getenv("STAR_USERNAME");
    const char* password = getenv("STAR_PASSWORD");
    
    if (username && password) {
        result = star_api_authenticate(username, password);
        if (result == STAR_API_SUCCESS) {
            g_star_initialized = true;
            printf("STAR API: Authenticated via SSO. Cross-game features enabled.\n");
            return;
        } else {
            printf("STAR API: SSO authentication failed: %s\n", star_api_get_last_error());
        }
    }
    
    // Fall back to API key authentication
    if (g_star_config.api_key && g_star_config.avatar_id) {
        g_star_initialized = true;
        printf("STAR API: Initialized with API key. Cross-game features enabled.\n");
    } else {
        printf("STAR API: Warning - No authentication method configured. Set STAR_USERNAME/STAR_PASSWORD or STAR_API_KEY/STAR_AVATAR_ID.\n");
    }
}

void Doom_STAR_Cleanup(void) {
    if (g_star_initialized) {
        star_api_cleanup();
        g_star_initialized = false;
        printf("STAR API: Cleaned up.\n");
    }
}

void Doom_STAR_OnKeycardPickup(int keycard_type) {
    if (!g_star_initialized) {
        return;
    }
    
    const char* keycard_name = GetKeycardName(keycard_type);
    const char* keycard_desc = GetKeycardDescription(keycard_type);
    
    if (!keycard_name) {
        return;
    }
    
    // Add keycard to STAR API inventory
    star_api_result_t result = star_api_add_item(
        keycard_name,
        keycard_desc,
        "Doom",
        "KeyItem"
    );
    
    if (result == STAR_API_SUCCESS) {
        printf("STAR API: Added %s to cross-game inventory.\n", keycard_name);
    } else {
        printf("STAR API: Failed to add %s: %s\n", keycard_name, star_api_get_last_error());
    }
}

bool Doom_STAR_CheckDoorAccess(int door_line, int required_key) {
    if (!g_star_initialized) {
        // If STAR API not initialized, fall back to local check only
        return false;  // Let Doom's normal door logic handle it
    }
    
    // First check local Doom inventory
    // (This would be done by Doom's normal keycard checking)
    
    // Then check cross-game inventory
    const char* required_keycard = GetKeycardName(required_key);
    if (required_keycard) {
        // Check if player has this keycard from any game
        if (star_api_has_item(required_keycard)) {
            printf("STAR API: Door opened using cross-game keycard: %s\n", required_keycard);
            star_api_use_item(required_keycard, "doom_door");
            return true;
        }
    }
    
    return false;  // Let Doom's normal logic handle it
}

void Doom_STAR_OnItemPickup(const char* item_name, const char* item_description) {
    if (!g_star_initialized || !item_name) {
        return;
    }
    
    // Add item to cross-game inventory
    star_api_result_t result = star_api_add_item(
        item_name,
        item_description ? item_description : "Item from Doom",
        "Doom",
        "Miscellaneous"
    );
    
    if (result == STAR_API_SUCCESS) {
        printf("STAR API: Added %s to cross-game inventory.\n", item_name);
    }
}

bool Doom_STAR_HasCrossGameKeycard(const char* keycard_name) {
    if (!g_star_initialized || !keycard_name) {
        return false;
    }
    
    return star_api_has_item(keycard_name);
}

/**
 * Integration Hook Example for Doom Source Code:
 * 
 * In p_inter.c, modify P_TouchSpecialThing():
 * 
 * void P_TouchSpecialThing(mobj_t* special, mobj_t* toucher) {
 *     // ... existing code ...
 *     
 *     // Add STAR API integration
 *     if (special->sprite == SPR_KEYR || special->sprite == SPR_KEYB || 
 *         special->sprite == SPR_KEYY || special->sprite == SPR_BSKU) {
 *         int keycard_type = DetermineKeycardType(special->sprite);
 *         Doom_STAR_OnKeycardPickup(keycard_type);
 *     }
 *     
 *     // ... rest of existing code ...
 * }
 * 
 * In p_doors.c, modify P_UseSpecialLine():
 * 
 * bool P_UseSpecialLine(line_t* line, mobj_t* thing) {
 *     // ... existing code ...
 *     
 *     // Check cross-game inventory if local check fails
 *     if (line->special == Door_Open && !HasLocalKeycard(required_key)) {
 *         if (Doom_STAR_CheckDoorAccess(line - lines, required_key)) {
 *             return true;  // Door opened with cross-game keycard
 *         }
 *     }
 *     
 *     // ... rest of existing code ...
 * }
 */

