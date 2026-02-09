/**
 * OQuake - OASIS STAR API Integration Implementation
 *
 * Integrates Quake with the OASIS STAR API so keys collected in ODOOM
 * can open doors in OQuake and vice versa.
 *
 * Integration Points:
 * 1. Key pickup -> add to STAR inventory (silver_key, gold_key)
 * 2. Door touch -> check local key first, then cross-game (Doom keycards)
 */

#include "quakedef.h"
#include "oquake_star_integration.h"
#include <stdio.h>
#include <string.h>
#include <stdlib.h>

static star_api_config_t g_star_config;
static int g_star_initialized = 0;

static const char* get_key_description(const char* key_name) {
    if (strcmp(key_name, OQUAKE_ITEM_SILVER_KEY) == 0)
        return "Silver Key - Opens silver-marked doors";
    if (strcmp(key_name, OQUAKE_ITEM_GOLD_KEY) == 0)
        return "Gold Key - Opens gold-marked doors";
    return "Key from OQuake";
}

void OQuake_STAR_Init(void) {
    g_star_config.base_url = "https://star-api.oasisplatform.world/api";
    g_star_config.api_key = getenv("STAR_API_KEY");
    g_star_config.avatar_id = getenv("STAR_AVATAR_ID");
    g_star_config.timeout_seconds = 10;

    star_api_result_t result = star_api_init(&g_star_config);
    if (result != STAR_API_SUCCESS) {
        printf("OQuake STAR API: Failed to initialize: %s\n", star_api_get_last_error());
        return;
    }

    const char* username = getenv("STAR_USERNAME");
    const char* password = getenv("STAR_PASSWORD");
    if (username && password) {
        result = star_api_authenticate(username, password);
        if (result == STAR_API_SUCCESS) {
            g_star_initialized = 1;
            printf("OQuake STAR API: Authenticated. Cross-game keys enabled.\n");
            return;
        }
        printf("OQuake STAR API: SSO failed: %s\n", star_api_get_last_error());
    }
    if (g_star_config.api_key && g_star_config.avatar_id) {
        g_star_initialized = 1;
        printf("OQuake STAR API: Using API key. Cross-game keys enabled.\n");
    } else {
        printf("OQuake STAR API: Set STAR_USERNAME/STAR_PASSWORD or STAR_API_KEY/STAR_AVATAR_ID for cross-game keys.\n");
    }
}

void OQuake_STAR_Cleanup(void) {
    if (g_star_initialized) {
        star_api_cleanup();
        g_star_initialized = 0;
        printf("OQuake STAR API: Cleaned up.\n");
    }
}

void OQuake_STAR_OnKeyPickup(const char* key_name) {
    if (!g_star_initialized || !key_name)
        return;
    const char* desc = get_key_description(key_name);
    star_api_result_t result = star_api_add_item(key_name, desc, "Quake", "KeyItem");
    if (result == STAR_API_SUCCESS)
        printf("OQuake STAR API: Added %s to cross-game inventory.\n", key_name);
    else
        printf("OQuake STAR API: Failed to add %s: %s\n", key_name, star_api_get_last_error());
}

int OQuake_STAR_CheckDoorAccess(const char* door_targetname, const char* required_key_name) {
    if (!g_star_initialized || !required_key_name)
        return 0;
    if (star_api_has_item(required_key_name)) {
        printf("OQuake STAR API: Door opened with cross-game key: %s\n", required_key_name);
        star_api_use_item(required_key_name, door_targetname ? door_targetname : "quake_door");
        return 1;
    }
    return 0;
}
