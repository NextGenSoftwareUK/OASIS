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
#include <ctype.h>

static star_api_config_t g_star_config;
static int g_star_initialized = 0;
static int g_star_console_registered = 0;
static char g_star_username[64] = {0};

/* key binding helpers from keys.c */
extern char *keybindings[MAX_KEYS];
extern qboolean keydown[MAX_KEYS];
extern int Key_StringToKeynum(const char *str);
extern void Key_SetBinding(int keynum, const char *binding);

cvar_t oasis_star_anorak_face = {"oasis_star_anorak_face", "0", CVAR_ARCHIVE};

enum {
    OQ_TAB_KEYS = 0,
    OQ_TAB_POWERUPS = 1,
    OQ_TAB_WEAPONS = 2,
    OQ_TAB_AMMO = 3,
    OQ_TAB_ARMOR = 4,
    OQ_TAB_ITEMS = 5,
    OQ_TAB_COUNT = 6
};

#define OQ_MAX_INVENTORY_ITEMS 256
#define OQ_MAX_OVERLAY_ROWS 8

typedef struct oquake_inventory_entry_s {
    char name[256];
    char description[512];
    char item_type[64];
} oquake_inventory_entry_t;

static oquake_inventory_entry_t g_inventory_entries[OQ_MAX_INVENTORY_ITEMS];
static int g_inventory_count = 0;
static int g_inventory_active_tab = OQ_TAB_KEYS;
static qboolean g_inventory_open = false;
static double g_inventory_last_refresh = 0.0;
static char g_inventory_status[128] = "STAR inventory unavailable.";
static int star_initialized(void);

static const char* OQ_TabShortName(int tab) {
    switch (tab) {
        case OQ_TAB_KEYS: return "Keys";
        case OQ_TAB_POWERUPS: return "Power Ups";
        case OQ_TAB_WEAPONS: return "Weapons";
        case OQ_TAB_AMMO: return "Ammo";
        case OQ_TAB_ARMOR: return "Armor";
        default: return "Items";
    }
}

static int OQ_ContainsNoCase(const char* haystack, const char* needle) {
    size_t i = 0, j = 0;
    size_t hay_len, needle_len;
    if (!haystack || !needle || !needle[0]) return 0;
    hay_len = strlen(haystack);
    needle_len = strlen(needle);
    if (needle_len > hay_len) return 0;
    for (i = 0; i + needle_len <= hay_len; i++) {
        for (j = 0; j < needle_len; j++) {
            unsigned char hc = (unsigned char)haystack[i + j];
            unsigned char nc = (unsigned char)needle[j];
            if (tolower(hc) != tolower(nc))
                break;
        }
        if (j == needle_len)
            return 1;
    }
    return 0;
}

static int OQ_ItemMatchesTab(const oquake_inventory_entry_t* item, int tab) {
    const char* type = item ? item->item_type : NULL;
    const char* name = item ? item->name : NULL;
    int is_key = OQ_ContainsNoCase(type, "key") || OQ_ContainsNoCase(name, "key");
    int is_powerup = OQ_ContainsNoCase(type, "powerup");
    int is_weapon = OQ_ContainsNoCase(type, "weapon");
    int is_ammo = OQ_ContainsNoCase(type, "ammo");
    int is_armor = OQ_ContainsNoCase(type, "armor");

    switch (tab) {
        case OQ_TAB_KEYS: return is_key;
        case OQ_TAB_POWERUPS: return is_powerup;
        case OQ_TAB_WEAPONS: return is_weapon;
        case OQ_TAB_AMMO: return is_ammo;
        case OQ_TAB_ARMOR: return is_armor;
        default: return !is_key && !is_powerup && !is_weapon && !is_ammo && !is_armor;
    }
}

static int OQ_IsMockAnorakCredentials(const char* username, const char* password) {
    if (!username || !password)
        return 0;
    if (strcmp(password, "test!") != 0)
        return 0;
    return q_strcasecmp(username, "anorak") == 0 || q_strcasecmp(username, "avatar") == 0;
}

static void OQ_RefreshInventoryCache(void) {
    star_item_list_t* list = NULL;
    star_api_result_t result;
    size_t i;

    g_inventory_count = 0;
    q_strlcpy(g_inventory_status, "STAR inventory unavailable.", sizeof(g_inventory_status));

    if (!star_initialized()) {
        q_strlcpy(g_inventory_status, "Not beamed in. Use: star beamin", sizeof(g_inventory_status));
        return;
    }

    result = star_api_get_inventory(&list);
    if (result != STAR_API_SUCCESS) {
        q_snprintf(g_inventory_status, sizeof(g_inventory_status), "Inventory error: %s", star_api_get_last_error());
        return;
    }

    if (!list || list->count == 0) {
        q_strlcpy(g_inventory_status, "Inventory is empty.", sizeof(g_inventory_status));
        if (list)
            star_api_free_item_list(list);
        return;
    }

    for (i = 0; i < list->count && g_inventory_count < OQ_MAX_INVENTORY_ITEMS; i++) {
        oquake_inventory_entry_t* dst = &g_inventory_entries[g_inventory_count];
        q_strlcpy(dst->name, list->items[i].name, sizeof(dst->name));
        q_strlcpy(dst->description, list->items[i].description, sizeof(dst->description));
        q_strlcpy(dst->item_type, list->items[i].item_type, sizeof(dst->item_type));
        g_inventory_count++;
    }

    q_snprintf(g_inventory_status, sizeof(g_inventory_status), "STAR inventory synced (%d items)", g_inventory_count);
    star_api_free_item_list(list);
    g_inventory_last_refresh = realtime;
}

static void OQ_InventoryToggle_f(void) {
    g_inventory_open = !g_inventory_open;
    if (g_inventory_open)
        OQ_RefreshInventoryCache();
}

static void OQ_InventoryPrevTab_f(void) {
    if (!g_inventory_open)
        return;
    g_inventory_active_tab--;
    if (g_inventory_active_tab < 0)
        g_inventory_active_tab = OQ_TAB_COUNT - 1;
}

static void OQ_InventoryNextTab_f(void) {
    if (!g_inventory_open)
        return;
    g_inventory_active_tab++;
    if (g_inventory_active_tab >= OQ_TAB_COUNT)
        g_inventory_active_tab = 0;
}

static void OQ_PollInventoryHotkeys(void) {
    /* O/P are handled through bound console commands to avoid double-step input. */
}

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

/* Forward declaration */
// static void OQ_DebugMode_f(void); // Temporarily disabled

void OQuake_STAR_Init(void) {
    star_api_result_t result;
    const char* username;
    const char* password;

    Cvar_RegisterVariable(&oasis_star_anorak_face);
    Cvar_SetValueQuick(&oasis_star_anorak_face, 0);

    if (!g_star_console_registered) {
        Cmd_AddCommand("star", OQuake_STAR_Console_f);
        Cmd_AddCommand("oasis_inventory_toggle", OQ_InventoryToggle_f);
        Cmd_AddCommand("oasis_inventory_prevtab", OQ_InventoryPrevTab_f);
        Cmd_AddCommand("oasis_inventory_nexttab", OQ_InventoryNextTab_f);
        g_star_console_registered = 1;
    }


    g_star_config.base_url = "https://star-api.oasisplatform.world/api";
    g_star_config.api_key = getenv("STAR_API_KEY");
    g_star_config.avatar_id = getenv("STAR_AVATAR_ID");
    g_star_config.timeout_seconds = 10;

    result = star_api_init(&g_star_config);
    if (result != STAR_API_SUCCESS) {
        printf("OQuake STAR API: Failed to initialize: %s\n", star_api_get_last_error());
    } else {
        username = getenv("STAR_USERNAME");
        password = getenv("STAR_PASSWORD");
        if (username && password) {
            result = star_api_authenticate(username, password);
            if (result == STAR_API_SUCCESS) {
                g_star_initialized = 1;
                printf("OQuake STAR API: Authenticated. Cross-game keys enabled.\n");
            } else {
                printf("OQuake STAR API: SSO failed: %s\n", star_api_get_last_error());
            }
        } else if (g_star_config.api_key && g_star_config.avatar_id) {
            g_star_initialized = 1;
            printf("OQuake STAR API: Using API key. Cross-game keys enabled.\n");
        } else {
            printf("OQuake STAR API: Set STAR_USERNAME/STAR_PASSWORD or STAR_API_KEY/STAR_AVATAR_ID for cross-game keys.\n");
        }
    }
    /* OASIS / OQuake loading splash - same professional style as ODOOM */
    Con_Printf("\n");
    Con_Printf("  ================================================\n");
    Con_Printf("            O A S I S   O Q U A K E  " OQUAKE_VERSION " (Build " OQUAKE_BUILD ")\n");
    Con_Printf("               By NextGen World Ltd\n");
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
        Cvar_SetValueQuick(&oasis_star_anorak_face, 0);
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
 * Debug mode toggle command (debugmode on/off)
 *-----------------------------------------------------------------------------*/
/*
static void OQ_DebugMode_f(void) {
    extern cvar_t developer;
    int argc = Cmd_Argc();
    
    if (argc < 2) {
        Con_Printf("Debug mode is currently %s\n", developer.value > 0.5f ? "ON" : "OFF");
        Con_Printf("Usage: debugmode <on|off>\n");
        return;
    }
    
    const char* arg = Cmd_Argv(1);
    if (strcmp(arg, "on") == 0 || strcmp(arg, "1") == 0) {
        Cvar_SetValueQuick(&developer, 1);
        Con_Printf("Debug mode enabled (developer messages will be shown)\n");
    } else if (strcmp(arg, "off") == 0 || strcmp(arg, "0") == 0) {
        Cvar_SetValueQuick(&developer, 0);
        Con_Printf("Debug mode disabled (developer messages hidden)\n");
    } else {
        Con_Printf("Usage: debugmode <on|off>\n");
    }
}
*/

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
        Con_Printf("  star beamin <username> <password> - Log in inside Quake\n");
        Con_Printf("  star beamed in <username> <password> - Alias for beamin\n");
        Con_Printf("  star beamin   - Log in using STAR_USERNAME/STAR_PASSWORD or API key\n");
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
    if (strcmp(sub, "beamin") == 0 || (strcmp(sub, "beamed") == 0 && argc >= 3 && strcmp(Cmd_Argv(2), "in") == 0)) {
        const char* runtime_user = NULL;
        const char* runtime_pass = NULL;
        int arg_shift = (strcmp(sub, "beamed") == 0) ? 1 : 0;
        if (argc >= (4 + arg_shift) && strcmp(Cmd_Argv(2 + arg_shift), "jwt") != 0) {
            runtime_user = Cmd_Argv(2 + arg_shift);
            runtime_pass = Cmd_Argv(3 + arg_shift);
        }

        if (star_initialized() && !runtime_user) { Con_Printf("Already logged in. Use 'star beamout' first.\n"); return; }
        if (star_initialized() && runtime_user) {
            star_api_cleanup();
            g_star_initialized = 0;
        }

        if (runtime_user && runtime_pass && OQ_IsMockAnorakCredentials(runtime_user, runtime_pass)) {
            g_star_initialized = 1;
            q_strlcpy(g_star_username, runtime_user, sizeof(g_star_username));
            Cvar_SetValueQuick(&oasis_star_anorak_face, 1);
            Con_Printf("Beam-in successful (mock). Welcome, %s.\n", runtime_user);
            return;
        }

        Cvar_SetValueQuick(&oasis_star_anorak_face, 0);
        g_star_config.base_url = "https://star-api.oasisplatform.world/api";
        g_star_config.api_key = getenv("STAR_API_KEY");
        g_star_config.avatar_id = getenv("STAR_AVATAR_ID");
        g_star_config.timeout_seconds = 10;
        star_api_result_t r = star_api_init(&g_star_config);
        if (r != STAR_API_SUCCESS) {
            Con_Printf("Beamin failed - init: %s\n", star_api_get_last_error());
            return;
        }
        const char* username = runtime_user ? runtime_user : getenv("STAR_USERNAME");
        const char* password = runtime_pass ? runtime_pass : getenv("STAR_PASSWORD");
        if (username && password) {
            r = star_api_authenticate(username, password);
            if (r == STAR_API_SUCCESS) {
                g_star_initialized = 1;
                q_strlcpy(g_star_username, username, sizeof(g_star_username));
                Con_Printf("Logged in (beamin). Cross-game keys enabled.\n");
                return;
            }
            Con_Printf("Beamin (SSO) failed: %s\n", star_api_get_last_error());
            return;
        }
        if (g_star_config.api_key && g_star_config.avatar_id) {
            g_star_initialized = 1;
            // Try to get username from avatar_id or use a default
            if (g_star_config.avatar_id) {
                q_strlcpy(g_star_username, "API User", sizeof(g_star_username));
            }
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
        g_star_username[0] = 0;
        Cvar_SetValueQuick(&oasis_star_anorak_face, 0);
        Con_Printf("Logged out (beamout). Use 'star beamin' to log in again.\n");
        return;
    }
    Con_Printf("Unknown STAR subcommand: %s. Type 'star' for list.\n", sub);
}

void OQuake_STAR_DrawInventoryOverlay(cb_context_t* cbx) {
    int panel_w;
    int panel_h;
    int panel_x;
    int panel_y;
    int tab_y;
    int tab_slot_w;
    int tab;
    int draw_y;
    int shown;
    int i;
    char line[512];

    OQ_PollInventoryHotkeys();

    if (!g_inventory_open || !cbx)
        return;

    if (realtime - g_inventory_last_refresh > 2.0)
        OQ_RefreshInventoryCache();

    panel_w = q_min(glwidth - 48, 900);
    panel_h = q_min(glheight - 96, 420);
    if (panel_w < 480) panel_w = 480;
    if (panel_h < 160) panel_h = 160;
    panel_x = (glwidth - panel_w) / 2;
    panel_y = (glheight - panel_h) / 2;
    if (panel_x < 0) panel_x = 0;
    if (panel_y < 0) panel_y = 0;

    Draw_Fill(cbx, panel_x, panel_y, panel_w, panel_h, 0, 0.70f);
    {
        const char* header = "OASIS Inventory ";
        int header_len = strlen(header);
        int header_x = panel_x + (panel_w - (header_len * 8)) / 2;
        if (header_x < panel_x + 6) header_x = panel_x + 6;
        Draw_String(cbx, header_x, panel_y + 6, header);
    }
    tab_y = panel_y + 26;
    tab_slot_w = (panel_w - 24) / OQ_TAB_COUNT;
    for (tab = 0; tab < OQ_TAB_COUNT; tab++) {
        int slot_x = panel_x + 12 + tab * tab_slot_w;
        const char* tab_name = OQ_TabShortName(tab);
        int tab_name_w = (int)strlen(tab_name) * 8;
        int tab_name_x = slot_x + (tab_slot_w - tab_name_w) / 2;
        if (tab == g_inventory_active_tab)
            Draw_Fill(cbx, slot_x + 1, tab_y - 1, tab_slot_w - 2, 10, 96, 0.35f);
        Draw_String(cbx, tab_name_x, tab_y, tab_name);
    }
    Draw_String(cbx, panel_x + 6, panel_y + panel_h - 8, "I=Toggle  O/P=Switch Tabs");

    draw_y = panel_y + 46;
    shown = 0;
    for (i = 0; i < g_inventory_count; i++) {
        if (!OQ_ItemMatchesTab(&g_inventory_entries[i], g_inventory_active_tab))
            continue;
        q_snprintf(line, sizeof(line), "%s [%s]", g_inventory_entries[i].name, g_inventory_entries[i].item_type);
        Draw_String(cbx, panel_x + 6, draw_y, line);
        draw_y += 8;
        shown++;
        if (shown >= OQ_MAX_OVERLAY_ROWS)
            break;
    }

    if (shown == 0)
        Draw_String(cbx, panel_x + 6, draw_y, g_inventory_status);
}

void OQuake_STAR_DrawBeamedInStatus(cb_context_t* cbx) {
    extern int glheight;
    if (!cbx)
        return;

    if (glheight <= 0)
        return;

    const char* username = OQuake_STAR_GetUsername();
    char status[128];
    if (username && username[0]) {
        q_snprintf(status, sizeof(status), "Beamed In: %s", username);
    } else {
        q_strlcpy(status, "Beamed In: None", sizeof(status));
    }

    Draw_String(cbx, 8, glheight - 24, status);
}

int OQuake_STAR_ShouldUseAnorakFace(void) {
    return oasis_star_anorak_face.value > 0.5f;
}

const char* OQuake_STAR_GetUsername(void) {
    if (g_star_initialized && g_star_username[0])
        return g_star_username;
    return NULL;
}
