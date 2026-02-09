/**
 * OQuake - OASIS STAR API Integration Implementation
 *
 * Integrates Quake with the OASIS STAR API so keys collected in ODOOM
 * can open doors in OQuake and vice versa.
 *
 * Integration Points:
 * 1. Key pickup -> add to STAR inventory (silver_key, gold_key)
 * 2. Door touch -> check local key first, then cross-game (Doom keycards)
 * 3. In-game console: "star" command (star version, star inventory, star beamin, etc.)
 */

#include "quakedef.h"
#include "oquake_star_integration.h"
#include "oquake_version.h"
#include <stdio.h>
#include <string.h>
#include <stdlib.h>

static star_api_config_t g_star_config;
static int g_star_initialized = 0;

static int star_initialized(void) {
    return g_star_initialized;
}

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
    Cmd_AddCommand("star", OQuake_STAR_Console_f);
    /* OASIS / OQuake loading splash - same professional style as ODOOM */
    Con_Printf("\n");
    Con_Printf("  ================================================\n");
    Con_Printf("            O A S I S   O Q U A K E\n");
    Con_Printf("  ================================================\n");
    Con_Printf("\n");
    Con_Printf("  " OQUAKE_VERSION_STR "\n");
    Con_Printf("  STAR API - Enabling full interoperable games across the OASIS Omniverse!\n");
    Con_Printf("  Type 'star' in console for STAR commands.\n");
    Con_Printf("\n");
    Con_Printf("  Welcome to OQuake!\n");
    Con_Printf("\n");
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

/*-----------------------------------------------------------------------------
 * STAR console command (star <subcmd> [args...]) - same style as ODOOM
 *-----------------------------------------------------------------------------*/
void OQuake_STAR_Console_f(void) {
    int argc = Cmd_Argc();
    if (argc < 2) {
        Con_Printf("\n");
        Con_Printf("STAR API console commands (OQuake):\n");
        Con_Printf("\n");
        Con_Printf("  star version        - Show integration and API status\n");
        Con_Printf("  star status         - Show init state and last error\n");
        Con_Printf("  star inventory      - List items in STAR inventory\n");
        Con_Printf("  star has <item>     - Check if you have an item (e.g. silver_key)\n");
        Con_Printf("  star add <item> [desc] [type] - Add item\n");
        Con_Printf("  star use <item> [context]     - Use item\n");
        Con_Printf("  star pickup keycard <silver|gold> - Add OQuake key (convenience)\n");
        Con_Printf("  star beamin   - Log in (STAR_USERNAME/PASSWORD or API key)\n");
        Con_Printf("  star beamout  - Log out / disconnect from STAR\n");
        Con_Printf("\n");
        return;
    }
    const char* sub = Cmd_Argv(1);
    if (strcmp(sub, "pickup") == 0) {
        if (argc < 4 || strcmp(Cmd_Argv(2), "keycard") != 0) {
            Con_Printf("Usage: star pickup keycard <silver|gold>\n");
            return;
        }
        const char* color = Cmd_Argv(3);
        const char* name = NULL;
        const char* desc = NULL;
        if (strcmp(color, "silver") == 0) { name = OQUAKE_ITEM_SILVER_KEY; desc = get_key_description(name); }
        else if (strcmp(color, "gold") == 0) { name = OQUAKE_ITEM_GOLD_KEY; desc = get_key_description(name); }
        else { Con_Printf("Unknown keycard: %s. Use silver|gold.\n", color); return; }
        star_api_result_t r = star_api_add_item(name, desc, "Quake", "KeyItem");
        if (r == STAR_API_SUCCESS) Con_Printf("Added %s to STAR inventory.\n", name);
        else Con_Printf("Failed: %s\n", star_api_get_last_error());
        return;
    }
    if (strcmp(sub, "version") == 0) {
        Con_Printf("STAR API integration 1.0 (OQuake)\n");
        Con_Printf("  Initialized: %s\n", star_initialized() ? "yes" : "no");
        if (!star_initialized()) Con_Printf("  Last error: %s\n", star_api_get_last_error());
        return;
    }
    if (strcmp(sub, "status") == 0) {
        Con_Printf("STAR API initialized: %s\n", star_initialized() ? "yes" : "no");
        Con_Printf("Last error: %s\n", star_api_get_last_error());
        return;
    }
    if (strcmp(sub, "inventory") == 0) {
        if (!star_initialized()) { Con_Printf("STAR API not initialized. %s\n", star_api_get_last_error()); return; }
        star_item_list_t* list = NULL;
        star_api_result_t r = star_api_get_inventory(&list);
        if (r != STAR_API_SUCCESS) {
            Con_Printf("Failed to get inventory: %s\n", star_api_get_last_error());
            return;
        }
        if (!list || list->count == 0) { Con_Printf("Inventory is empty.\n"); if (list) star_api_free_item_list(list); return; }
        Con_Printf("STAR inventory (%u items):\n", (unsigned)list->count);
        for (size_t i = 0; i < list->count; i++) {
            Con_Printf("  %s - %s (%s, %s)\n", list->items[i].name, list->items[i].description, list->items[i].game_source, list->items[i].item_type);
        }
        star_api_free_item_list(list);
        return;
    }
    if (strcmp(sub, "has") == 0) {
        if (argc < 3) { Con_Printf("Usage: star has <item_name>\n"); return; }
        int has = star_api_has_item(Cmd_Argv(2));
        Con_Printf("Has '%s': %s\n", Cmd_Argv(2), has ? "yes" : "no");
        return;
    }
    if (strcmp(sub, "add") == 0) {
        if (argc < 3) { Con_Printf("Usage: star add <item_name> [description] [item_type]\n"); return; }
        const char* name = Cmd_Argv(2);
        const char* desc = argc > 3 ? Cmd_Argv(3) : "Added from console";
        const char* type = argc > 4 ? Cmd_Argv(4) : "Miscellaneous";
        star_api_result_t r = star_api_add_item(name, desc, "Quake", type);
        if (r == STAR_API_SUCCESS) Con_Printf("Added '%s' to STAR inventory.\n", name);
        else Con_Printf("Failed to add '%s': %s\n", name, star_api_get_last_error());
        return;
    }
    if (strcmp(sub, "use") == 0) {
        if (argc < 3) { Con_Printf("Usage: star use <item_name> [context]\n"); return; }
        const char* ctx = argc > 3 ? Cmd_Argv(3) : "console";
        int ok = star_api_use_item(Cmd_Argv(2), ctx);
        Con_Printf("Use '%s' (context %s): %s\n", Cmd_Argv(2), ctx, ok ? "ok" : "failed");
        if (!ok) Con_Printf("  %s\n", star_api_get_last_error());
        return;
    }
    if (strcmp(sub, "beamin") == 0) {
        if (star_initialized()) { Con_Printf("Already logged in. Use 'star beamout' first.\n"); return; }
        g_star_config.base_url = "https://star-api.oasisplatform.world/api";
        g_star_config.api_key = getenv("STAR_API_KEY");
        g_star_config.avatar_id = getenv("STAR_AVATAR_ID");
        g_star_config.timeout_seconds = 10;
        star_api_result_t r = star_api_init(&g_star_config);
        if (r != STAR_API_SUCCESS) {
            Con_Printf("Beamin failed - init: %s\n", star_api_get_last_error());
            return;
        }
        const char* username = getenv("STAR_USERNAME");
        const char* password = getenv("STAR_PASSWORD");
        if (username && password) {
            r = star_api_authenticate(username, password);
            if (r == STAR_API_SUCCESS) { g_star_initialized = 1; Con_Printf("Logged in (beamin). Cross-game keys enabled.\n"); return; }
            Con_Printf("Beamin (SSO) failed: %s\n", star_api_get_last_error());
            return;
        }
        if (g_star_config.api_key && g_star_config.avatar_id) {
            g_star_initialized = 1;
            Con_Printf("Logged in with API key. Cross-game keys enabled.\n");
            return;
        }
        Con_Printf("Set STAR_USERNAME/STAR_PASSWORD or STAR_API_KEY/STAR_AVATAR_ID and try again.\n");
        return;
    }
    if (strcmp(sub, "beamout") == 0) {
        if (!star_initialized()) { Con_Printf("Not logged in. Use 'star beamin' to log in.\n"); return; }
        star_api_cleanup();
        g_star_initialized = 0;
        Con_Printf("Logged out (beamout). Use 'star beamin' to log in again.\n");
        return;
    }
    Con_Printf("Unknown STAR subcommand: %s. Type 'star' for list.\n", sub);
}
