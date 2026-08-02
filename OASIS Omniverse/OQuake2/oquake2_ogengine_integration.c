/**
 * OQuake2 - OASIS STAR API Integration Implementation
 *
 * Integrates Yamagi Quake II with the OASIS STAR API so keys collected in
 * OQuake, ODOOM, or other OASIS Omniverse games can open doors in OQuake2
 * and vice versa — and vice versa (cross-game keys, quests, inventory, XP,
 * SSO, and more across all OASIS Omniverse games).
 *
 * Integration Points:
 * 1. Key pickup -> add to STAR inventory (blue_key, red_key)
 * 2. Door touch -> check local key first, then cross-game inventory
 * 3. Weapon/armor/health/ammo pickup -> add to STAR inventory
 * 4. Monster kill -> award XP, optional NFT mint
 * 5. In-game console: "star" command (star version, star inventory, star beamin, etc.)
 * 6. HUD overlays: inventory, quest tracker, XP, toast messages
 *
 * OASIS thing type range: 6000-6899
 * Portal thing type: 5900 (shared across OASIS Omniverse games)
 *
 * Base engine: Yamagi Quake II (C, GPL-2.0)
 * See https://github.com/yquake2/yquake2 for the base engine.
 */

#include "oquake2_ogengine_integration.h"
#include "ogengine_sync.h"
#include <stdio.h>
#include <string.h>
#include <stdlib.h>
#include <ctype.h>
#include <time.h>
#include <stdarg.h>
#ifdef _WIN32
#include <windows.h>
#else
#include <sys/stat.h>
#include <dlfcn.h>
#include <unistd.h>
#endif

/* MSVC does not support GCC __attribute__ syntax; suppress it. */
#ifdef _MSC_VER
#  define __attribute__(x)
#endif

/* Yamagi Quake II uses qboolean / true / false from q_shared.h */
#ifndef qboolean
typedef int qboolean;
#endif
#ifndef true
#define true  1
#define false 0
#endif

/* Yamagi Q2 console print — substitute the real engine function at build time. */
#ifndef Q2_Con_Printf
#  ifdef __cplusplus
extern "C" void Com_Printf(const char* fmt, ...);
#  else
extern void Com_Printf(const char* fmt, ...);
#  endif
#  define Q2_Con_Printf Com_Printf
#endif

/* Yamagi Q2 string helpers — substitute the real engine function at build time. */
#ifndef Q2_Q_strlcpy
#  define Q2_Q_strlcpy(dst, src, sz) do { strncpy(dst, src, (sz)-1); (dst)[(sz)-1] = '\0'; } while(0)
#endif
#ifndef Q2_Q_snprintf
#  define Q2_Q_snprintf snprintf
#endif
#ifndef Q2_Q_strcasecmp
#  ifdef _WIN32
#    define Q2_Q_strcasecmp _stricmp
#  else
#    define Q2_Q_strcasecmp strcasecmp
#  endif
#endif

/* OQuake2 text scale on HUD (Yamagi Q2 draws at its own resolution). */
#define OQ2_UI_TEXT_SCALE 1.0f

/* Async `star beamin` guard: wall-clock seconds. */
#define OQ2_BEAMIN_ASYNC_TIMEOUT_SEC 30.0

/* -------------------------------------------------------------------------
 * Forward declarations
 * ------------------------------------------------------------------------- */
static void OQ2_OnSendItemDone(void* user_data);
static void OQ2_SaveStarConfigToFile(void);
static void OQ2_StarLog(const char* fmt, ...);
static void OQ2_StarDebugLog(const char* fmt, ...);
static int  OQ2_LoadJsonConfig(const char* json_path);
static int  OQ2_FindConfigFile(const char* filename, char* out_path, int maxlen);
static void OQ2_RefreshInventoryCache(void);
static void OQ2_RefreshOverlayFromClient(void);
static void OQ2_SetToastMessage(const char* msg);

/* -------------------------------------------------------------------------
 * String helpers
 * ------------------------------------------------------------------------- */

static int OQ2_ContainsNoCase(const char* haystack, const char* needle) {
    const char *h, *n, *hp;
    if (!haystack || !needle || !needle[0]) return 0;
    for (hp = haystack; *hp; hp++) {
        h = hp; n = needle;
        while (*h && *n && (tolower((unsigned char)*h) == tolower((unsigned char)*n))) { h++; n++; }
        if (!*n) return 1;
    }
    return 0;
}

/* -------------------------------------------------------------------------
 * JSON config helpers
 * ------------------------------------------------------------------------- */

/* Simple JSON value extractor: finds "key": "value" or "key": number */
static int OQ2_ExtractJsonValue(const char* json, const char* key, char* val, int maxlen) {
    char search[128];
    const char* p;
    int n;
    if (!json || !key || !val || maxlen < 2) return 0;
    val[0] = '\0';
    Q2_Q_snprintf(search, sizeof(search), "\"%s\"", key);
    p = strstr(json, search);
    if (!p) return 0;
    p += strlen(search);
    while (*p == ' ' || *p == '\t' || *p == '\r' || *p == '\n') p++;
    if (*p != ':') return 0;
    p++;
    while (*p == ' ' || *p == '\t' || *p == '\r' || *p == '\n') p++;
    n = 0;
    if (*p == '"') {
        p++;
        while (*p && *p != '"' && n < maxlen - 1) {
            if (*p == '\\' && p[1]) { p++; }
            val[n++] = *p++;
        }
    } else {
        while (*p && *p != ',' && *p != '}' && *p != '\n' && *p != '\r' && n < maxlen - 1)
            val[n++] = *p++;
        while (n > 0 && (val[n-1] == ' ' || val[n-1] == '\t')) n--;
    }
    val[n] = '\0';
    return n > 0;
}

/* -------------------------------------------------------------------------
 * Global state
 * ------------------------------------------------------------------------- */

static ogengine_config_t g_star_config;
static int  g_star_initialized          = 0;
static int  g_star_beamed_in            = 0;
static int  g_star_async_auth_pending   = 0;
static double g_star_async_auth_start   = 0.0;
static int  g_star_auth_timed_out       = 0;
static int  g_star_console_registered   = 0;
static char g_star_username[64]         = {0};
static char g_json_config_path[512]     = {0};
static char g_oq2_saved_username[128]   = {0};
static char g_oq2_saved_jwt[2048]       = {0};
static char g_oq2_saved_refresh_token[2048] = {0};
static char g_star_last_pickup_name[256]    = {0};
static char g_star_last_pickup_type[64]     = {0};
static int  g_star_has_last_pickup          = 0;
static volatile int g_star_profile_loaded_pending  = 0;
static volatile int g_inventory_refresh_pending    = 0;
static volatile int g_inventory_requested          = 0;
static qboolean g_star_debug_logging = false;

/* Toast message (top-center HUD) */
#define OQ2_TOAST_FRAMES_DEFAULT 175
static char g_oq2_toast_message[256] = "";
static int  g_oq2_toast_frames = 0;

/* Quest popup state */
static qboolean g_quest_popup_open  = false;
static qboolean g_inventory_open    = false;

/* Inventory cache */
#define OQ2_MAX_INVENTORY_ITEMS 256
#define OQ2_MAX_OVERLAY_ROWS    8
#define OQ2_SEND_TARGET_MAX     63
#define OQ2_GROUP_LABEL_MAX     96

typedef struct oquake2_inventory_entry_s {
    char name[256];
    char description[512];
    char item_type[64];
    char id[64];
    char game_source[64];
    char nft_id[128];
    int  quantity;
} oquake2_inventory_entry_t;

static oquake2_inventory_entry_t g_inventory_entries[OQ2_MAX_INVENTORY_ITEMS];
static int g_inventory_count        = 0;
static int g_inventory_selected_row = 0;
static int g_inventory_scroll_row   = 0;
static int g_inventory_active_tab   = 0;    /* 0=Keys, 1=Weapons, 2=Armor, 3=Health, 4=Ammo, 5=Monsters */
static char g_inventory_status[128] = "STAR inventory unavailable.";

enum {
    OQ2_TAB_KEYS     = 0,
    OQ2_TAB_WEAPONS  = 1,
    OQ2_TAB_ARMOR    = 2,
    OQ2_TAB_HEALTH   = 3,
    OQ2_TAB_AMMO     = 4,
    OQ2_TAB_MONSTERS = 5,
    OQ2_TAB_COUNT    = 6
};

/* Send-to popup state */
static char g_inventory_send_target[OQ2_SEND_TARGET_MAX + 1] = {0};
static int  g_inventory_send_popup    = 0;
static int  g_inventory_send_quantity = 1;
static int  g_inventory_send_button   = 0;

/* Use-item pending state */
static char g_oq2_use_pending_name[256]        = {0};
static char g_oq2_use_pending_type[64]         = {0};
static char g_oq2_use_pending_description[512] = {0};

/* -------------------------------------------------------------------------
 * Monster table
 * ------------------------------------------------------------------------- */

typedef struct oquake2_monster_entry_s {
    const char* engine_classname;   /* e.g. "monster_gunner" */
    const char* config_key;         /* e.g. "oquake2_gunner" for oasisstar.json */
    const char* display_name;       /* Human-readable name */
    int xp;
    int is_boss;
} oquake2_monster_entry_t;

static const oquake2_monster_entry_t OQUAKE2_MONSTERS[] = {
    { "monster_gunner",    "oquake2_gunner",    "Gunner",    50,  0 },
    { "monster_gladiator", "oquake2_gladiator", "Gladiator", 120, 0 },
    { "monster_tank",      "oquake2_tank",      "Tank",      150, 1 },
    { "monster_tank_commander", "oquake2_tank", "Tank",      150, 1 },
    { "monster_makron",    "oquake2_makron",    "Makron",    500, 1 },
    { "monster_jorg",      "oquake2_jorg",      "Jorg",      400, 1 },
    { "monster_brain",     "oquake2_brain",     "Brain",     80,  0 },
    { "monster_floater",   "oquake2_floater",   "Floater",   45,  0 },
    { "monster_mutant",    "oquake2_mutant",    "Mutant",    60,  0 },
    { "monster_medic",     "oquake2_medic",     "Medic",     55,  0 },
    { "monster_soldier",   "oquake2_soldier",   "Soldier",   30,  0 },
    { "monster_infantry",  "oquake2_soldier",   "Soldier",   30,  0 },  /* alternate infantry classname */
    { NULL, NULL, NULL, 0, 0 }
};

#define OQ2_MONSTER_COUNT ((int)(sizeof(OQUAKE2_MONSTERS) / sizeof(OQUAKE2_MONSTERS[0])) - 1)
#define OQ2_MONSTER_FLAGS_MAX 32

static int g_oq2_mint_monster_flags[OQ2_MONSTER_FLAGS_MAX];

/* -------------------------------------------------------------------------
 * CVARs (Yamagi Q2 uses cvar_t from q_shared.h; adapt to engine API)
 * -------------------------------------------------------------------------
 * NOTE: In Yamagi Q2, register cvars with Cvar_Get (returns cvar_t*).
 * We store the pointers returned by Cvar_Get at Init time.
 * The types below are declared extern so engines can include this file without
 * pulling in the full Yamagi cvar headers; bridge the types at the call site.
 */

/* Config/transport */
static char g_ogengine_url[512]    = "https://oasisweb4.com/api/star";
static char g_oasis_api_url[512]   = "https://oasisweb4.com";
static char g_star_transport[32]   = "remote";
static char g_oasis_dna_path[512]  = "";
static char g_star_avatar_id[128]  = "";
static char g_star_key[256]        = "";

/* Pickup/stack config */
static int g_stack_armor     = 1;
static int g_stack_weapons   = 1;
static int g_stack_powerups  = 1;
static int g_stack_keys      = 1;
static int g_mint_weapons    = 0;
static int g_mint_armor      = 0;
static int g_mint_powerups   = 0;
static int g_mint_keys       = 0;
static int g_max_health      = 100;
static int g_max_armor       = 200;   /* Q2 body armor max is 200 */
static int g_always_allow_pickup_if_max   = 1;
static int g_always_add_items_to_inventory= 0;
static int g_use_health_on_pickup         = 0;
static int g_use_armor_on_pickup          = 0;
static int g_use_powerup_on_pickup        = 0;
static int g_beam_face        = 1;
static char g_nft_provider[64]            = "SolanaOASIS";
static char g_send_to_address[256]        = "";

/* -------------------------------------------------------------------------
 * Internal helpers
 * ------------------------------------------------------------------------- */

static int star_initialized(void) {
    return g_star_initialized;
}

static void OQ2_StarLog(const char* fmt, ...) {
    char buf[512];
    va_list ap;
    va_start(ap, fmt);
    vsnprintf(buf, sizeof(buf), fmt, ap);
    va_end(ap);
    buf[sizeof(buf) - 1] = '\0';
    Q2_Con_Printf("[OQuake2] %s\n", buf);
}

static void OQ2_StarDebugLog(const char* fmt, ...) {
    char buf[512];
    va_list ap;
    if (!g_star_debug_logging) return;
    va_start(ap, fmt);
    vsnprintf(buf, sizeof(buf), fmt, ap);
    va_end(ap);
    buf[sizeof(buf) - 1] = '\0';
    Q2_Con_Printf("[OQuake2 DEBUG] %s\n", buf);
}

static void OQ2_SetToastMessage(const char* msg) {
    if (!msg || !msg[0]) return;
    Q2_Q_strlcpy(g_oq2_toast_message, msg, sizeof(g_oq2_toast_message));
    g_oq2_toast_frames = OQ2_TOAST_FRAMES_DEFAULT;
}

/** Returns 1 if mint is on for this item_type, 0 otherwise. */
static int OQ2_DoMintForItemType(const char* item_type) {
    if (!item_type || !item_type[0]) return 0;
    if (OQ2_ContainsNoCase(item_type, "Key") || !strcmp(item_type, "KeyItem"))
        return g_mint_keys;
    if (OQ2_ContainsNoCase(item_type, "Weapon"))
        return g_mint_weapons;
    if (OQ2_ContainsNoCase(item_type, "Armor"))
        return g_mint_armor;
    if (OQ2_ContainsNoCase(item_type, "Powerup") || OQ2_ContainsNoCase(item_type, "Artifact"))
        return g_mint_powerups;
    return 0;
}

/** Queue one add-item or pickup-with-mint job. */
static void OQ2_QueuePickup(const char* item_name, const char* description,
                             const char* item_type, int quantity) {
    int do_mint;
    if (!item_name || !item_name[0]) return;
    do_mint = OQ2_DoMintForItemType(item_type);
    if (do_mint) {
        ogengine_queue_pickup_with_mint(
            item_name,
            description ? description : "",
            "Quake2",
            item_type ? item_type : "Item",
            1,
            g_nft_provider[0] ? g_nft_provider : "SolanaOASIS",
            g_send_to_address[0] ? g_send_to_address : NULL,
            quantity > 0 ? quantity : 1);
    } else {
        ogengine_queue_add_item(
            item_name,
            description ? description : "",
            "Quake2",
            item_type ? item_type : "Item",
            NULL,
            quantity > 0 ? quantity : 1,
            1);
    }
    /* Also queue quest progress */
    ogengine_queue_quest_progress_from_pickup("Quake2", item_type ? item_type : "Item", item_name);
}

/** Add item to STAR inventory unconditionally (for keys and unlock-type items). */
static int OQ2_AddInventoryUnlockIfMissing(const char* item_name, const char* description,
                                            const char* item_type) {
    if (!item_name || !item_name[0]) return 0;
    OQ2_QueuePickup(item_name, description, item_type, 1);
    return 1;
}

/* -------------------------------------------------------------------------
 * Inventory overlay helpers
 * ------------------------------------------------------------------------- */

static int OQ2_ItemMatchesTab(const oquake2_inventory_entry_t* item, int tab) {
    const char* t;
    if (!item) return 0;
    t = item->item_type;
    switch (tab) {
    case OQ2_TAB_KEYS:     return OQ2_ContainsNoCase(t, "Key");
    case OQ2_TAB_WEAPONS:  return OQ2_ContainsNoCase(t, "Weapon");
    case OQ2_TAB_ARMOR:    return OQ2_ContainsNoCase(t, "Armor");
    case OQ2_TAB_HEALTH:   return OQ2_ContainsNoCase(t, "Health");
    case OQ2_TAB_AMMO:     return OQ2_ContainsNoCase(t, "Ammo");
    case OQ2_TAB_MONSTERS: return OQ2_ContainsNoCase(t, "Monster");
    default: return 0;
    }
}

static void OQ2_RefreshInventoryCache(void) {
    ogengine_item_list_t* list = NULL;
    size_t i;
    if (!g_star_initialized) return;
    if (ogengine_get_inventory(&list) != OGENGINE_SUCCESS || !list) return;
    g_inventory_count = 0;
    for (i = 0; i < list->count && g_inventory_count < OQ2_MAX_INVENTORY_ITEMS; i++) {
        oquake2_inventory_entry_t* e = &g_inventory_entries[g_inventory_count];
        Q2_Q_strlcpy(e->name,        list->items[i].name,        sizeof(e->name));
        Q2_Q_strlcpy(e->description, list->items[i].description, sizeof(e->description));
        Q2_Q_strlcpy(e->item_type,   list->items[i].item_type,   sizeof(e->item_type));
        Q2_Q_strlcpy(e->id,          list->items[i].id,          sizeof(e->id));
        Q2_Q_strlcpy(e->game_source, list->items[i].game_source, sizeof(e->game_source));
        Q2_Q_strlcpy(e->nft_id,      list->items[i].nft_id,      sizeof(e->nft_id));
        e->quantity = list->items[i].quantity;
        g_inventory_count++;
    }
    ogengine_free_item_list(list);
    OQ2_StarDebugLog("Inventory refreshed: %d items", g_inventory_count);
}

static void OQ2_RefreshOverlayFromClient(void) {
    OQ2_RefreshInventoryCache();
    g_inventory_refresh_pending = 0;
}

/* -------------------------------------------------------------------------
 * Item description helpers
 * ------------------------------------------------------------------------- */

static const char* OQ2_GetKeyDescription(const char* key_name) {
    if (!key_name) return "Key from OQuake2";
    if (!strcmp(key_name, OQUAKE2_ITEM_BLUE_KEY)) return "Blue Key - Opens blue-marked doors (OQuake2)";
    if (!strcmp(key_name, OQUAKE2_ITEM_RED_KEY))  return "Red Key - Opens red-marked doors (OQuake2)";
    return "Key from OQuake2";
}

static const char* OQ2_GetWeaponDescription(const char* item_name) {
    if (!item_name) return "Weapon from Quake II";
    if (OQ2_ContainsNoCase(item_name, "Blaster"))          return "Blaster - Basic energy weapon (Quake II)";
    if (OQ2_ContainsNoCase(item_name, "Super Shotgun"))    return "Super Shotgun - Spread shot (Quake II)";
    if (OQ2_ContainsNoCase(item_name, "Shotgun"))          return "Shotgun - Standard shotgun (Quake II)";
    if (OQ2_ContainsNoCase(item_name, "Chaingun"))         return "Chaingun - Rapid fire bullets (Quake II)";
    if (OQ2_ContainsNoCase(item_name, "Machinegun"))       return "Machinegun - Automatic fire (Quake II)";
    if (OQ2_ContainsNoCase(item_name, "Grenade Launcher")) return "Grenade Launcher - Explosive projectile (Quake II)";
    if (OQ2_ContainsNoCase(item_name, "Rocket Launcher"))  return "Rocket Launcher - Explosive rockets (Quake II)";
    if (OQ2_ContainsNoCase(item_name, "Hyperblaster"))     return "Hyperblaster - Rapid energy pulses (Quake II)";
    if (OQ2_ContainsNoCase(item_name, "Railgun"))          return "Railgun - Instant-hit slug (Quake II)";
    if (OQ2_ContainsNoCase(item_name, "BFG10K"))           return "BFG10K - Big Fucking Gun (Quake II)";
    return "Weapon from Quake II";
}

static const char* OQ2_GetArmorDescription(const char* item_name) {
    if (!item_name) return "Armor from Quake II";
    if (OQ2_ContainsNoCase(item_name, "Jacket"))  return "Jacket Armor - Light protection (+25 armor)";
    if (OQ2_ContainsNoCase(item_name, "Combat"))  return "Combat Armor - Medium protection (+50 armor)";
    if (OQ2_ContainsNoCase(item_name, "Body"))    return "Body Armor - Heavy protection (+100 armor)";
    return "Armor from Quake II";
}

/* -------------------------------------------------------------------------
 * Config file I/O
 * ------------------------------------------------------------------------- */

static int OQ2_FindConfigFile(const char* filename, char* out_path, int maxlen) {
    FILE* f;
    const char* locations[] = {
        "",
        "build/",
        "../build/",
        "../../OASIS Omniverse/OQuake2/build/",
        "../OASIS Omniverse/OQuake2/build/",
        "OASIS Omniverse/OQuake2/build/",
        NULL
    };
    int i;
    char test_path[512];
    for (i = 0; locations[i]; i++) {
        Q2_Q_snprintf(test_path, sizeof(test_path), "%s%s", locations[i], filename);
        f = fopen(test_path, "r");
        if (f) {
            fclose(f);
            Q2_Q_strlcpy(out_path, test_path, maxlen);
            return 1;
        }
    }
#ifdef _WIN32
    {
        char exe_path[MAX_PATH] = {0};
        char exe_dir[MAX_PATH]  = {0};
        if (GetModuleFileNameA(NULL, exe_path, sizeof(exe_path))) {
            char* last_slash = strrchr(exe_path, '\\');
            if (last_slash) {
                int dir_len = (int)(last_slash - exe_path);
                if (dir_len > 0 && dir_len < (int)sizeof(exe_dir)) {
                    memcpy(exe_dir, exe_path, (size_t)dir_len);
                    exe_dir[dir_len] = '\0';
                    Q2_Q_snprintf(test_path, sizeof(test_path), "%s\\%s", exe_dir, filename);
                    f = fopen(test_path, "r");
                    if (f) { fclose(f); Q2_Q_strlcpy(out_path, test_path, maxlen); return 1; }
                    Q2_Q_snprintf(test_path, sizeof(test_path), "%s\\build\\%s", exe_dir, filename);
                    f = fopen(test_path, "r");
                    if (f) { fclose(f); Q2_Q_strlcpy(out_path, test_path, maxlen); return 1; }
                }
            }
        }
    }
#elif defined(__linux__)
    {
        char self[512];
        ssize_t n = readlink("/proc/self/exe", self, sizeof(self) - 1);
        if (n > 0) {
            char* slash;
            self[n] = '\0';
            slash = strrchr(self, '/');
            if (slash) {
                *slash = '\0';
                Q2_Q_snprintf(test_path, sizeof(test_path), "%s/%s", self, filename);
                f = fopen(test_path, "r");
                if (f) { fclose(f); Q2_Q_strlcpy(out_path, test_path, maxlen); return 1; }
            }
        }
    }
#endif
    return 0;
}

static int OQ2_LoadJsonConfig(const char* json_path) {
    FILE* f;
    char* json;
    long fsz;
    size_t len;
    char value[512];
    int loaded = 0;
    if (!json_path || !json_path[0]) return 0;
    f = fopen(json_path, "r");
    if (!f) return 0;
    if (fseek(f, 0, SEEK_END) != 0) { fclose(f); return 0; }
    fsz = ftell(f);
    if (fsz <= 0 || fsz > 512 * 1024) { fclose(f); return 0; }
    if (fseek(f, 0, SEEK_SET) != 0) { fclose(f); return 0; }
    json = (char*)malloc((size_t)fsz + 1);
    if (!json) { fclose(f); return 0; }
    len = fread(json, 1, (size_t)fsz, f);
    fclose(f);
    if (len == 0) { free(json); return 0; }
    json[len] = '\0';

    if (OQ2_ExtractJsonValue(json, "ogengine_url", value, sizeof(value)) && value[0]) {
        Q2_Q_strlcpy(g_ogengine_url, value, sizeof(g_ogengine_url)); loaded = 1;
    } else if (OQ2_ExtractJsonValue(json, "star_api_url", value, sizeof(value)) && value[0]) {
        Q2_Q_strlcpy(g_ogengine_url, value, sizeof(g_ogengine_url)); loaded = 1;
    }
    if (OQ2_ExtractJsonValue(json, "oasis_api_url", value, sizeof(value)) && value[0])
        Q2_Q_strlcpy(g_oasis_api_url, value, sizeof(g_oasis_api_url));
    if (OQ2_ExtractJsonValue(json, "star_transport", value, sizeof(value)) && value[0])
        Q2_Q_strlcpy(g_star_transport, value, sizeof(g_star_transport));
    if (OQ2_ExtractJsonValue(json, "oasis_dna_path", value, sizeof(value)) && value[0])
        Q2_Q_strlcpy(g_oasis_dna_path, value, sizeof(g_oasis_dna_path));
    if (OQ2_ExtractJsonValue(json, "avatar_id", value, sizeof(value)) && value[0])
        Q2_Q_strlcpy(g_star_avatar_id, value, sizeof(g_star_avatar_id));
    if (OQ2_ExtractJsonValue(json, "beam_face", value, sizeof(value)))
        g_beam_face = atoi(value);
    if (OQ2_ExtractJsonValue(json, "stack_armor", value, sizeof(value)))
        g_stack_armor = atoi(value);
    if (OQ2_ExtractJsonValue(json, "stack_weapons", value, sizeof(value)))
        g_stack_weapons = atoi(value);
    if (OQ2_ExtractJsonValue(json, "stack_powerups", value, sizeof(value)))
        g_stack_powerups = atoi(value);
    if (OQ2_ExtractJsonValue(json, "stack_keys", value, sizeof(value)))
        g_stack_keys = atoi(value);
    if (OQ2_ExtractJsonValue(json, "mint_weapons", value, sizeof(value)))
        g_mint_weapons = atoi(value);
    if (OQ2_ExtractJsonValue(json, "mint_armor", value, sizeof(value)))
        g_mint_armor = atoi(value);
    if (OQ2_ExtractJsonValue(json, "mint_powerups", value, sizeof(value)))
        g_mint_powerups = atoi(value);
    if (OQ2_ExtractJsonValue(json, "mint_keys", value, sizeof(value)))
        g_mint_keys = atoi(value);
    if (OQ2_ExtractJsonValue(json, "max_health", value, sizeof(value)))
        g_max_health = atoi(value);
    if (OQ2_ExtractJsonValue(json, "max_armor", value, sizeof(value)))
        g_max_armor = atoi(value);
    if (OQ2_ExtractJsonValue(json, "always_allow_pickup_if_max", value, sizeof(value)))
        g_always_allow_pickup_if_max = atoi(value);
    if (OQ2_ExtractJsonValue(json, "always_add_items_to_inventory", value, sizeof(value)))
        g_always_add_items_to_inventory = atoi(value);
    if (OQ2_ExtractJsonValue(json, "use_health_on_pickup", value, sizeof(value)))
        g_use_health_on_pickup = atoi(value);
    if (OQ2_ExtractJsonValue(json, "use_armor_on_pickup", value, sizeof(value)))
        g_use_armor_on_pickup = atoi(value);
    if (OQ2_ExtractJsonValue(json, "use_powerup_on_pickup", value, sizeof(value)))
        g_use_powerup_on_pickup = atoi(value);
    if (OQ2_ExtractJsonValue(json, "nft_provider", value, sizeof(value)) && value[0])
        Q2_Q_strlcpy(g_nft_provider, value, sizeof(g_nft_provider));
    if (OQ2_ExtractJsonValue(json, "send_to_address_after_minting", value, sizeof(value)) && value[0])
        Q2_Q_strlcpy(g_send_to_address, value, sizeof(g_send_to_address));

    /* Saved session */
    if (OQ2_ExtractJsonValue(json, "beamedin_avatar", value, sizeof(value)) && value[0])
        Q2_Q_strlcpy(g_oq2_saved_username, value, sizeof(g_oq2_saved_username));
    if (OQ2_ExtractJsonValue(json, "saved_jwt", value, sizeof(value)) && value[0])
        Q2_Q_strlcpy(g_oq2_saved_jwt, value, sizeof(g_oq2_saved_jwt));
    if (OQ2_ExtractJsonValue(json, "jwt_token", value, sizeof(value)) && value[0])
        Q2_Q_strlcpy(g_oq2_saved_jwt, value, sizeof(g_oq2_saved_jwt));
    if (OQ2_ExtractJsonValue(json, "refresh_token", value, sizeof(value)) && value[0])
        Q2_Q_strlcpy(g_oq2_saved_refresh_token, value, sizeof(g_oq2_saved_refresh_token));

    /* Per-monster mint flags */
    {
        int i;
        for (i = 0; i < OQ2_MONSTER_COUNT && i < OQ2_MONSTER_FLAGS_MAX; i++) {
            char key[64];
            Q2_Q_snprintf(key, sizeof(key), "mint_monster_%s", OQUAKE2_MONSTERS[i].config_key);
            if (OQ2_ExtractJsonValue(json, key, value, sizeof(value)))
                g_oq2_mint_monster_flags[i] = atoi(value);
        }
    }

    /* offline_mode */
    if (OQ2_ExtractJsonValue(json, "offline_mode", value, sizeof(value)))
        OQ2_StarDebugLog("offline_mode=%s (not yet implemented; ignored)", value);

    free(json);
    return loaded;
}

static void OQ2_SaveStarConfigToFile(void) {
    char path[512];
    FILE* f;
    int i, j;
    if (g_json_config_path[0])
        Q2_Q_strlcpy(path, g_json_config_path, sizeof(path));
    else if (!OQ2_FindConfigFile("oasisstar.json", path, sizeof(path)))
        Q2_Q_strlcpy(path, "oasisstar.json", sizeof(path));

    f = fopen(path, "w");
    if (!f) return;
    fprintf(f, "{\n");
    fprintf(f, "  \"ogengine_url\": \"%s\"", g_ogengine_url);
    fprintf(f, ",\n  \"oasis_api_url\": \"%s\"", g_oasis_api_url);
    fprintf(f, ",\n  \"star_transport\": \"%s\"", g_star_transport);
    fprintf(f, ",\n  \"oasis_dna_path\": \"%s\"", g_oasis_dna_path);
    fprintf(f, ",\n  \"beam_face\": %d", g_beam_face);
    fprintf(f, ",\n  \"stack_armor\": %d", g_stack_armor);
    fprintf(f, ",\n  \"stack_weapons\": %d", g_stack_weapons);
    fprintf(f, ",\n  \"stack_powerups\": %d", g_stack_powerups);
    fprintf(f, ",\n  \"stack_keys\": %d", g_stack_keys);
    fprintf(f, ",\n  \"mint_weapons\": %d", g_mint_weapons);
    fprintf(f, ",\n  \"mint_armor\": %d", g_mint_armor);
    fprintf(f, ",\n  \"mint_powerups\": %d", g_mint_powerups);
    fprintf(f, ",\n  \"mint_keys\": %d", g_mint_keys);
    fprintf(f, ",\n  \"max_health\": %d", g_max_health);
    fprintf(f, ",\n  \"max_armor\": %d", g_max_armor);
    fprintf(f, ",\n  \"always_allow_pickup_if_max\": %d", g_always_allow_pickup_if_max);
    fprintf(f, ",\n  \"always_add_items_to_inventory\": %d", g_always_add_items_to_inventory);
    fprintf(f, ",\n  \"use_health_on_pickup\": %d", g_use_health_on_pickup);
    fprintf(f, ",\n  \"use_armor_on_pickup\": %d", g_use_armor_on_pickup);
    fprintf(f, ",\n  \"use_powerup_on_pickup\": %d", g_use_powerup_on_pickup);
    fprintf(f, ",\n  \"nft_provider\": \"%s\"", g_nft_provider);
    fprintf(f, ",\n  \"send_to_address_after_minting\": \"%s\"", g_send_to_address);
    /* Session */
    {
        char uname[128] = {0};
        char jwt[2048]  = {0};
        int got_uname = (ogengine_get_current_username(uname, sizeof(uname)) > 0 && uname[0]);
        if (!got_uname && g_star_username[0]) {
            Q2_Q_strlcpy(uname, g_star_username, sizeof(uname));
            got_uname = 1;
        }
        if (got_uname) {
            const char* p;
            Q2_Q_strlcpy(g_oq2_saved_username, uname, sizeof(g_oq2_saved_username));
            fprintf(f, ",\n  \"beamedin_avatar\": \"");
            for (p = uname; *p; p++) { if (*p == '"' || *p == '\\') fputc('\\', f); fputc((unsigned char)*p, f); }
            fprintf(f, "\"");
        }
        if (ogengine_get_current_jwt(jwt, sizeof(jwt)) > 0 && jwt[0]) {
            const char* p;
            Q2_Q_strlcpy(g_oq2_saved_jwt, jwt, sizeof(g_oq2_saved_jwt));
            fprintf(f, ",\n  \"saved_jwt\": \"");
            for (p = jwt; *p; p++) { if (*p == '"' || *p == '\\') fputc('\\', f); fputc((unsigned char)*p, f); }
            fprintf(f, "\"");
        }
        {
            char refresh_buf[2048] = {0};
            if (ogengine_get_current_refresh_token(refresh_buf, sizeof(refresh_buf)) > 0 && refresh_buf[0]) {
                const char* p;
                Q2_Q_strlcpy(g_oq2_saved_refresh_token, refresh_buf, sizeof(g_oq2_saved_refresh_token));
                fprintf(f, ",\n  \"refresh_token\": \"");
                for (p = refresh_buf; *p; p++) { if (*p == '"' || *p == '\\') fputc('\\', f); fputc((unsigned char)*p, f); }
                fprintf(f, "\"");
            }
        }
    }
    /* Per-monster mint flags (unique config_keys only) */
    for (i = 0; i < OQ2_MONSTER_COUNT && i < OQ2_MONSTER_FLAGS_MAX; i++) {
        int already = 0;
        for (j = 0; j < i; j++)
            if (!strcmp(OQUAKE2_MONSTERS[j].config_key, OQUAKE2_MONSTERS[i].config_key)) { already = 1; break; }
        if (already) continue;
        fprintf(f, ",\n  \"mint_monster_%s\": %d", OQUAKE2_MONSTERS[i].config_key, g_oq2_mint_monster_flags[i] ? 1 : 0);
    }
    fprintf(f, "\n}\n");
    fclose(f);
}

/* -------------------------------------------------------------------------
 * Auth callbacks
 * ------------------------------------------------------------------------- */

static void OQ2_OnAuthDone(ogengine_result_t result, void* user_data) {
    (void)user_data;
    if (g_star_auth_timed_out) {
        g_star_auth_timed_out = 0;
        return;
    }
    g_star_async_auth_pending = 0;
    if (result == OGENGINE_SUCCESS) {
        char uname[64] = {0};
        g_star_beamed_in = 1;
        ogengine_get_current_username(uname, sizeof(uname));
        if (uname[0]) Q2_Q_strlcpy(g_star_username, uname, sizeof(g_star_username));
        Q2_Con_Printf("[OQuake2] Beamed in as: %s\n", g_star_username[0] ? g_star_username : "(unknown)");
        ogengine_refresh_avatar_profile();
        ogengine_request_inventory_in_background();
        OQ2_SaveStarConfigToFile();
    } else {
        g_star_beamed_in = 0;
        Q2_Con_Printf("[OQuake2] Beam-in failed: %s\n", ogengine_get_last_error());
    }
}

static void OQ2_OnOperationDone(ogengine_result_t result, int operation_type, void* user_data) {
    (void)user_data;
    if (operation_type == OGENGINE_OP_PROFILE_LOADED) {
        g_star_profile_loaded_pending = 1;
    } else if (operation_type == OGENGINE_OP_GET_INVENTORY) {
        g_inventory_refresh_pending = 1;
        g_inventory_requested = 0;
    }
    (void)result;
}

static void OQ2_OnSendItemDone(void* user_data) {
    int success = 0;
    char err_buf[384] = {0};
    (void)user_data;
    if (!ogengine_sync_send_item_get_result(&success, err_buf, sizeof(err_buf))) return;
    if (success)
        Q2_Q_strlcpy(g_inventory_status, "Item sent.", sizeof(g_inventory_status));
    else
        Q2_Q_snprintf(g_inventory_status, sizeof(g_inventory_status), "Send failed: %s", err_buf[0] ? err_buf : "unknown error");
    OQ2_RefreshOverlayFromClient();
}

/* -------------------------------------------------------------------------
 * Console command
 * ------------------------------------------------------------------------- */

void OQuake2_STAR_Console_f(void) {
    /* Yamagi Q2: read args with Cmd_Argc() / Cmd_Argv().
     * Since this file avoids a hard dependency on the exact cmd API, the engine
     * wrapper should call OQ2_StarConsoleExec(argc, argv) instead. */
    Q2_Con_Printf("[OQuake2] STAR console — use: star beamin <user> <pass>  |  star inventory  |  star version\n");
}

/* -------------------------------------------------------------------------
 * Public API: Init / Cleanup
 * ------------------------------------------------------------------------- */

void OQuake2_STAR_Init(void) {
    char found_json_path[512] = {0};
    int i;

    ogengine_sync_init();

    /* Default all monster mint flags to 1 */
    for (i = 0; i < OQ2_MONSTER_FLAGS_MAX; i++)
        g_oq2_mint_monster_flags[i] = 1;

    /* Load oasisstar.json if present */
    if (OQ2_FindConfigFile("oasisstar.json", found_json_path, sizeof(found_json_path))) {
        if (OQ2_LoadJsonConfig(found_json_path)) {
            Q2_Q_strlcpy(g_json_config_path, found_json_path, sizeof(g_json_config_path));
            Q2_Con_Printf("[OQuake2] Loaded config: %s\n", found_json_path);
        }
    } else {
        Q2_Con_Printf("[OQuake2] oasisstar.json not found — using defaults. "
                      "Copy oasisstar.json from OQuake2 folder next to the game exe.\n");
    }

    /* Build ogengine config */
    memset(&g_star_config, 0, sizeof(g_star_config));
    g_star_config.base_url         = g_ogengine_url;
    g_star_config.api_key          = g_star_key[0]  ? g_star_key  : NULL;
    g_star_config.avatar_id        = g_star_avatar_id[0] ? g_star_avatar_id : NULL;
    g_star_config.timeout_seconds  = 30;
    g_star_config.client_game_source = "OQUAKE2";
    g_star_config.transport        = (!strcmp(g_star_transport, "native")) ? 1 : 0;
    g_star_config.oasis_dna_path   = g_oasis_dna_path[0] ? g_oasis_dna_path : NULL;

    if (ogengine_init(&g_star_config) != OGENGINE_SUCCESS) {
        Q2_Con_Printf("[OQuake2] STAR API init failed: %s\n", ogengine_get_last_error());
        return;
    }

    if (g_oasis_api_url[0])
        ogengine_set_oasis_base_url(g_oasis_api_url);

    ogengine_set_operation_callback(OQ2_OnOperationDone, NULL);
    ogengine_set_callback(OQ2_OnAuthDone, NULL);

    /* Try to restore saved session */
    if (g_oq2_saved_jwt[0]) {
        ogengine_set_saved_session(g_oq2_saved_jwt);
        if (g_oq2_saved_refresh_token[0])
            ogengine_set_refresh_token(g_oq2_saved_refresh_token);
        ogengine_restore_session();
        Q2_Con_Printf("[OQuake2] Restoring session for: %s\n",
                      g_oq2_saved_username[0] ? g_oq2_saved_username : "(unknown)");
    }

    g_star_initialized = 1;

    Q2_Con_Printf("[OQuake2] OASIS STAR API initialized. Use 'star beamin <user> <pass>' to sign in.\n");
    Q2_Con_Printf("[OQuake2] OASIS thing type range: 6000-6899. Portal: 5900.\n");
}

void OQuake2_STAR_Cleanup(void) {
    if (!g_star_initialized) return;
    OQ2_SaveStarConfigToFile();
    ogengine_flush_add_item_jobs();
    ogengine_cleanup();
    ogengine_sync_cleanup();
    g_star_initialized = 0;
    g_star_beamed_in   = 0;
    g_inventory_count  = 0;
}

/* -------------------------------------------------------------------------
 * Public API: Key / door hooks
 * ------------------------------------------------------------------------- */

void OQuake2_STAR_OnKeyPickup(const char* key_name) {
    const char* desc;
    if (!g_star_initialized || !g_star_beamed_in) return;
    if (!key_name || !key_name[0]) return;
    desc = OQ2_GetKeyDescription(key_name);
    Q2_Con_Printf("[OQuake2] Key picked up: %s -> STAR inventory\n", key_name);
    OQ2_AddInventoryUnlockIfMissing(key_name, desc, "KeyItem");
    /* Complete any key-collection objectives */
    ogengine_queue_quest_progress_from_pickup("Quake2", "KeyItem", key_name);
    Q2_Q_strlcpy(g_star_last_pickup_name, key_name, sizeof(g_star_last_pickup_name));
    Q2_Q_strlcpy(g_star_last_pickup_type, "KeyItem", sizeof(g_star_last_pickup_type));
    g_star_has_last_pickup = 1;
}

int OQuake2_STAR_CheckDoorAccess(const char* door_targetname, const char* required_key_name) {
    if (!g_star_initialized || !g_star_beamed_in) return 0;
    if (!required_key_name || !required_key_name[0]) return 0;
    if (ogengine_has_item(required_key_name)) {
        Q2_Con_Printf("[OQuake2] Door '%s' opened via STAR key: %s\n",
                      door_targetname ? door_targetname : "(unnamed)", required_key_name);
        return 1;
    }
    OQ2_StarDebugLog("Door '%s' denied: STAR does not have '%s'",
                     door_targetname ? door_targetname : "(unnamed)", required_key_name);
    return 0;
}

/* -------------------------------------------------------------------------
 * Public API: Item pickup hooks
 * ------------------------------------------------------------------------- */

void OQuake2_STAR_OnItemPickup(const char* item_name, const char* item_type,
                                int quantity, const char* description) {
    if (!g_star_initialized || !g_star_beamed_in) return;
    if (!item_name || !item_name[0]) return;
    OQ2_QueuePickup(item_name, description ? description : "", item_type, quantity);
    Q2_Q_strlcpy(g_star_last_pickup_name, item_name, sizeof(g_star_last_pickup_name));
    Q2_Q_strlcpy(g_star_last_pickup_type, item_type ? item_type : "Item", sizeof(g_star_last_pickup_type));
    g_star_has_last_pickup = 1;
    OQ2_StarDebugLog("OnItemPickup: '%s' type='%s' qty=%d", item_name, item_type ? item_type : "Item", quantity);
}

void OQuake2_STAR_OnPickupLeftOnFloor(const char* item_name, const char* item_type,
                                       int quantity, const char* description) {
    if (!g_star_initialized || !g_star_beamed_in) return;
    if (!item_name || !item_name[0]) return;
    /* Player was at max — send to STAR instead of leaving on floor */
    OQ2_QueuePickup(item_name, description ? description : "", item_type, quantity);
    OQ2_StarDebugLog("OnPickupLeftOnFloor: '%s' qty=%d -> STAR", item_name, quantity);
}

int OQuake2_STAR_InterceptTouchPickupAtMax(void* item_ent, void* player_ent) {
    /* Engines implement this by calling with (item, player) so we check player health/armor.
     * Returns 1 to intercept (call OnPickupLeftOnFloor and free item), 0 to proceed normally. */
    (void)item_ent;
    (void)player_ent;
    if (!g_star_initialized || !g_star_beamed_in) return 0;
    if (!g_always_allow_pickup_if_max) return 0;
    /* Engine-specific: the caller should check item_ent's classname to determine type and whether
     * the player is at max for that type, then call OQuake2_STAR_OnPickupLeftOnFloor before freeing. */
    return 0;
}

/* -------------------------------------------------------------------------
 * Public API: Monster kill hooks
 * ------------------------------------------------------------------------- */

void OQuake2_STAR_OnMonsterKilled(const char* monster_classname) {
    int i;
    if (!g_star_initialized || !g_star_beamed_in) return;
    if (!monster_classname || !monster_classname[0]) return;
    for (i = 0; i < OQ2_MONSTER_COUNT && i < OQ2_MONSTER_FLAGS_MAX; i++) {
        if (!Q2_Q_strcasecmp(OQUAKE2_MONSTERS[i].engine_classname, monster_classname)) {
            int do_mint = g_oq2_mint_monster_flags[i];
            ogengine_queue_monster_kill(
                monster_classname,
                OQUAKE2_MONSTERS[i].display_name,
                OQUAKE2_MONSTERS[i].xp,
                OQUAKE2_MONSTERS[i].is_boss,
                do_mint,
                g_nft_provider[0] ? g_nft_provider : "SolanaOASIS",
                "Quake2");
            OQ2_StarDebugLog("Monster killed: %s (%s) xp=%d boss=%d mint=%d",
                             monster_classname, OQUAKE2_MONSTERS[i].display_name,
                             OQUAKE2_MONSTERS[i].xp, OQUAKE2_MONSTERS[i].is_boss, do_mint);
            return;
        }
    }
    /* Unknown monster: award default XP, no mint */
    ogengine_queue_monster_kill(monster_classname, monster_classname, 25, 0, 0, NULL, "Quake2");
    OQ2_StarDebugLog("Monster killed (unknown): %s xp=25", monster_classname);
}

void OQuake2_STAR_OnBossKilled(const char* boss_name) {
    OQuake2_STAR_OnMonsterKilled(boss_name);
}

/* -------------------------------------------------------------------------
 * Public API: Frame pump
 * ------------------------------------------------------------------------- */

void OQuake2_STAR_PollItems(void) {
    char mint_item[256], nft_id[128], hash[128];
    char err_buf[384];
    if (!g_star_initialized) return;

    ogengine_sync_pump();

    /* --- cross-game spawn poll --- */
    {
        char entity_id[128];
        float sx, sy, sz;
        if (ogengine_poll_spawn_event(entity_id, sizeof(entity_id), &sx, &sy, &sz))
        {
            /* TODO: spawn entity by entity_id at sx/sy/sz via game's native spawn API.
             * Map entity_id to native classname using OGAsset catalog lookup.
             * For now, log the request. */
            oglib_log(OGLIB_LOG_INFO, "OASIS SpawnEvent: %s at %.0f/%.0f/%.0f", entity_id, sx, sy, sz);
            ogengine_confirm_spawn(entity_id);
        }
    }

    /* Handle async auth timeout */
    if (g_star_async_auth_pending) {
#ifdef _WIN32
        LARGE_INTEGER freq, now;
        double elapsed = 0.0;
        if (QueryPerformanceFrequency(&freq) && QueryPerformanceCounter(&now)) {
            elapsed = (double)now.QuadPart / (double)freq.QuadPart - g_star_async_auth_start;
        }
        if (elapsed > OQ2_BEAMIN_ASYNC_TIMEOUT_SEC) {
#else
        struct timespec ts;
        double elapsed = 0.0;
        if (clock_gettime(CLOCK_MONOTONIC, &ts) == 0) {
            elapsed = (double)ts.tv_sec + (double)ts.tv_nsec * 1e-9 - g_star_async_auth_start;
        }
        if (elapsed > OQ2_BEAMIN_ASYNC_TIMEOUT_SEC) {
#endif
            g_star_async_auth_pending = 0;
            g_star_auth_timed_out = 1;
            Q2_Con_Printf("[OQuake2] Beam-in timed out. Try 'star beamin' again.\n");
        }
    }

    /* Profile loaded callback */
    if (g_star_profile_loaded_pending) {
        g_star_profile_loaded_pending = 0;
        ogengine_request_inventory_in_background();
    }

    /* Inventory refresh callback */
    if (g_inventory_refresh_pending) {
        g_inventory_refresh_pending = 0;
        OQ2_RefreshInventoryCache();
    }

    /* Consume mint results */
    while (ogengine_consume_last_mint_result(mint_item, sizeof(mint_item), nft_id, sizeof(nft_id), hash, sizeof(hash))) {
        if (mint_item[0])
            Q2_Con_Printf("[OQuake2] Minted NFT: %s (id=%s)\n", mint_item, nft_id[0] ? nft_id : "?");
    }

    /* Consume background errors */
    while (ogengine_consume_last_background_error(err_buf, sizeof(err_buf))) {
        if (err_buf[0])
            Q2_Con_Printf("[OQuake2] STAR error: %s\n", err_buf);
    }

    /* Consume STAR console log messages */
    {
        char log_buf[512];
        while (ogengine_consume_console_log(log_buf, sizeof(log_buf))) {
            if (log_buf[0]) Q2_Con_Printf("[OQuake2 STAR] %s\n", log_buf);
        }
    }

    /* Decay toast */
    if (g_oq2_toast_frames > 0)
        g_oq2_toast_frames--;
}

/* -------------------------------------------------------------------------
 * Public API: HUD / overlay draw hooks
 * (Implementations are stubs — engine provides its own draw calls)
 * ------------------------------------------------------------------------- */

void OQuake2_STAR_DrawInventoryOverlay(void* ctx) {
    (void)ctx;
    /* Engine-specific: call OQ2_RefreshInventoryCache if g_inventory_open is true,
     * then iterate g_inventory_entries[0..g_inventory_count-1] to draw rows. */
}

void OQuake2_STAR_DrawBeamedInStatus(void* ctx) {
    (void)ctx;
    /* Engine-specific: draw g_star_username in top-left if g_star_beamed_in. */
}

void OQuake2_STAR_DrawQuestTracker(void* ctx) {
    (void)ctx;
    /* Engine-specific: draw active quest name from ogengine_get_tracker_quest_name(). */
}

void OQuake2_STAR_DrawXpStatus(void* ctx) {
    (void)ctx;
    /* Engine-specific: draw XP from ogengine_get_avatar_xp() in top-right. */
}

void OQuake2_STAR_DrawToast(void* ctx) {
    (void)ctx;
    /* Engine-specific: draw g_oq2_toast_message at top-center if g_oq2_toast_frames > 0. */
}

void OQuake2_STAR_DrawVersionStatus(void* ctx) {
    (void)ctx;
    /* Engine-specific: draw "OASIS STAR" version label in corner. */
}

/* -------------------------------------------------------------------------
 * Public API: Popup state queries
 * ------------------------------------------------------------------------- */

int OQuake2_STAR_IsQuestPopupOpen(void) {
    return g_quest_popup_open ? 1 : 0;
}

int OQuake2_STAR_IsInventoryPopupOpen(void) {
    return g_inventory_open ? 1 : 0;
}

/* -------------------------------------------------------------------------
 * Public API: Misc queries
 * ------------------------------------------------------------------------- */

int OQuake2_STAR_ShouldUseAnorakFace(void) {
    if (!g_beam_face || !g_star_initialized) return 0;
    return !Q2_Q_strcasecmp(g_star_username, "anorak") ||
           !Q2_Q_strcasecmp(g_star_username, "avatar") ||
           !Q2_Q_strcasecmp(g_star_username, "dellams");
}

const char* OQuake2_STAR_GetUsername(void) {
    return g_star_username;
}

/* -------------------------------------------------------------------------
 * Cross-game teleportation
 * ------------------------------------------------------------------------- */

void OQuake2_STAR_CheckIncomingTeleport(void)
{
    char map[256];
    float x = 0, y = 0, z = 64;
    if (!ogengine_poll_teleport_request(map, sizeof(map), &x, &y, &z))
        return;
    oglib_log(OGLIB_LOG_INFO, "OASIS Teleport arrive: map=%s pos=%.0f/%.0f/%.0f", map, x, y, z);
    /* TODO: warp player — e.g. gi.WriteByte(svc_stufftext); gi.WriteString("map <mapname>\n"); gi.unicast(player, true);
     * For in-map position warp: set player->s.origin and call gi.linkentity(player) */
    ogengine_confirm_teleport_arrival();
}
