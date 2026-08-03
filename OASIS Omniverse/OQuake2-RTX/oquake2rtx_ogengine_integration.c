/**
 * OQuake2-RTX - OASIS STAR API Integration Implementation
 *
 * Integrates NVIDIA's Q2 RTX (ray-traced Quake 2 remaster) with the OASIS STAR API
 * so keys, weapons, and inventory collected here sync with OQuake, OQuake2, ODOOM,
 * and all other OASIS Omniverse games via the STAR cross-game inventory API.
 *
 * Q2 RTX is built on the Yamagi Quake II codebase with NVIDIA's Vulkan RTX renderer
 * (VK_NV_ray_tracing / VK_KHR_ray_tracing_pipeline). The game content and file formats
 * are identical to Quake II, so this integration shares the same OASIS thing type range
 * (6000-6899) as OQuake2 — a silver key is the same item regardless of renderer.
 *
 * Integration Points:
 * 1. Key pickup -> add to STAR inventory (blue_key, red_key)
 * 2. Door touch -> check local key first, then cross-game inventory
 * 3. Weapon/armor/health/ammo pickup -> STAR inventory tracking
 * 4. Monster kill -> award XP, optional NFT mint
 * 5. In-game console: "star" command (star version, star inventory, star beamin, etc.)
 * 6. HUD overlays: inventory, quest tracker, XP, toast (renderer-neutral hooks)
 *
 * OASIS thing type range: 6000-6899 (same as OQuake2)
 * Portal thing type: 5900 (shared across OASIS Omniverse games)
 *
 * Base engine: Q2 RTX (NVIDIA, Vulkan RTX, based on Yamagi Q2 / GPL-2.0)
 * See https://github.com/NVIDIA/Q2RTX for the base engine.
 */

#include "oquake2rtx_ogengine_integration.h"
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

/* Boolean compatibility */
#ifndef qboolean
typedef int qboolean;
#endif
#ifndef true
#define true  1
#define false 0
#endif

/* Q2 RTX / Yamagi Q2 console print */
#ifndef Q2RTX_Con_Printf
#  ifdef __cplusplus
extern "C" void Com_Printf(const char* fmt, ...);
#  else
extern void Com_Printf(const char* fmt, ...);
#  endif
#  define Q2RTX_Con_Printf Com_Printf
#endif

/* String helpers */
#ifndef Q2RTX_Q_strlcpy
#  define Q2RTX_Q_strlcpy(dst, src, sz) do { strncpy(dst, src, (sz)-1); (dst)[(sz)-1] = '\0'; } while(0)
#endif
#ifndef Q2RTX_Q_snprintf
#  define Q2RTX_Q_snprintf snprintf
#endif
#ifndef Q2RTX_Q_strcasecmp
#  ifdef _WIN32
#    define Q2RTX_Q_strcasecmp _stricmp
#  else
#    define Q2RTX_Q_strcasecmp strcasecmp
#  endif
#endif

/* Async beamin timeout (wall-clock seconds) */
#define OQ2RTX_BEAMIN_ASYNC_TIMEOUT_SEC 30.0

/* Toast frame count (~5s at 35fps) */
#define OQ2RTX_TOAST_FRAMES_DEFAULT 175

/* -------------------------------------------------------------------------
 * Forward declarations
 * ------------------------------------------------------------------------- */
static void OQ2RTX_OnSendItemDone(void* user_data);
static void OQ2RTX_SaveStarConfigToFile(void);
static void OQ2RTX_StarLog(const char* fmt, ...);
static void OQ2RTX_StarDebugLog(const char* fmt, ...);
static int  OQ2RTX_LoadJsonConfig(const char* json_path);
static int  OQ2RTX_FindConfigFile(const char* filename, char* out_path, int maxlen);
static void OQ2RTX_RefreshInventoryCache(void);
static void OQ2RTX_SetToastMessage(const char* msg);

/* -------------------------------------------------------------------------
 * String helpers
 * ------------------------------------------------------------------------- */

static int OQ2RTX_ContainsNoCase(const char* haystack, const char* needle) {
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

static int OQ2RTX_ExtractJsonValue(const char* json, const char* key, char* val, int maxlen) {
    char search[128];
    const char* p;
    int n;
    if (!json || !key || !val || maxlen < 2) return 0;
    val[0] = '\0';
    Q2RTX_Q_snprintf(search, sizeof(search), "\"%s\"", key);
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
static int  g_star_initialized           = 0;
static int  g_star_beamed_in             = 0;
static int  g_star_async_auth_pending    = 0;
static double g_star_async_auth_start    = 0.0;
static int  g_star_auth_timed_out        = 0;
static int  g_star_console_registered    = 0;
static char g_star_username[64]          = {0};
static char g_json_config_path[512]      = {0};
static char g_oq2rtx_saved_username[128] = {0};
static char g_oq2rtx_saved_jwt[2048]     = {0};
static char g_oq2rtx_saved_refresh[2048] = {0};
static volatile int g_star_profile_loaded_pending = 0;
static volatile int g_inventory_refresh_pending   = 0;
static volatile int g_inventory_requested         = 0;
static qboolean g_star_debug_logging = false;

/* Toast */
static char g_oq2rtx_toast_message[256] = "";
static int  g_oq2rtx_toast_frames = 0;

/* Popup state */
static qboolean g_quest_popup_open = false;
static qboolean g_inventory_open   = false;

/* Inventory cache */
#define OQ2RTX_MAX_INVENTORY_ITEMS 256
#define OQ2RTX_MAX_OVERLAY_ROWS    8
#define OQ2RTX_SEND_TARGET_MAX     63
#define OQ2RTX_GROUP_LABEL_MAX     96

typedef struct oquake2rtx_inventory_entry_s {
    char name[256];
    char description[512];
    char item_type[64];
    char id[64];
    char game_source[64];
    char nft_id[128];
    int  quantity;
} oquake2rtx_inventory_entry_t;

static oquake2rtx_inventory_entry_t g_inventory_entries[OQ2RTX_MAX_INVENTORY_ITEMS];
static int g_inventory_count        = 0;
static int g_inventory_selected_row = 0;
static int g_inventory_scroll_row   = 0;
static int g_inventory_active_tab   = 0;
static char g_inventory_status[128] = "STAR inventory unavailable.";

enum {
    OQ2RTX_TAB_KEYS     = 0,
    OQ2RTX_TAB_WEAPONS  = 1,
    OQ2RTX_TAB_ARMOR    = 2,
    OQ2RTX_TAB_HEALTH   = 3,
    OQ2RTX_TAB_AMMO     = 4,
    OQ2RTX_TAB_MONSTERS = 5,
    OQ2RTX_TAB_COUNT    = 6
};

/* Send-to popup */
static char g_inventory_send_target[OQ2RTX_SEND_TARGET_MAX + 1] = {0};
static int  g_inventory_send_popup    = 0;
static int  g_inventory_send_quantity = 1;
static int  g_inventory_send_button   = 0;

/* Use-item pending */
static char g_oq2rtx_use_pending_name[256]        = {0};
static char g_oq2rtx_use_pending_type[64]         = {0};
static char g_oq2rtx_use_pending_description[512] = {0};

/* -------------------------------------------------------------------------
 * Monster table (same monsters as OQuake2 — same game content)
 * ------------------------------------------------------------------------- */

typedef struct oquake2rtx_monster_entry_s {
    const char* engine_classname;
    const char* config_key;
    const char* display_name;
    int xp;
    int is_boss;
} oquake2rtx_monster_entry_t;

static const oquake2rtx_monster_entry_t OQUAKE2RTX_MONSTERS[] = {
    { "monster_gunner",         "oquake2rtx_gunner",    "Gunner",    50,  0 },
    { "monster_gladiator",      "oquake2rtx_gladiator", "Gladiator", 120, 0 },
    { "monster_tank",           "oquake2rtx_tank",      "Tank",      150, 1 },
    { "monster_tank_commander", "oquake2rtx_tank",      "Tank",      150, 1 },
    { "monster_makron",         "oquake2rtx_makron",    "Makron",    500, 1 },
    { "monster_jorg",           "oquake2rtx_jorg",      "Jorg",      400, 1 },
    { "monster_brain",          "oquake2rtx_brain",     "Brain",     80,  0 },
    { "monster_floater",        "oquake2rtx_floater",   "Floater",   45,  0 },
    { "monster_mutant",         "oquake2rtx_mutant",    "Mutant",    60,  0 },
    { "monster_medic",          "oquake2rtx_medic",     "Medic",     55,  0 },
    { "monster_soldier",        "oquake2rtx_soldier",   "Soldier",   30,  0 },
    { "monster_infantry",       "oquake2rtx_soldier",   "Soldier",   30,  0 },
    { NULL, NULL, NULL, 0, 0 }
};

#define OQ2RTX_MONSTER_COUNT ((int)(sizeof(OQUAKE2RTX_MONSTERS) / sizeof(OQUAKE2RTX_MONSTERS[0])) - 1)
#define OQ2RTX_MONSTER_FLAGS_MAX 32

static int g_oq2rtx_mint_monster_flags[OQ2RTX_MONSTER_FLAGS_MAX];

/* -------------------------------------------------------------------------
 * Config settings
 * ------------------------------------------------------------------------- */

static char g_ogengine_url[512]  = "https://oasisweb4.com/api/star";
static char g_oasis_api_url[512] = "https://oasisweb4.com";
static char g_star_transport[32] = "remote";
static char g_oasis_dna_path[512]= "";
static char g_star_avatar_id[128]= "";
static char g_star_key[256]      = "";
static int  g_stack_armor        = 1;
static int  g_stack_weapons      = 1;
static int  g_stack_powerups     = 1;
static int  g_stack_keys         = 1;
static int  g_mint_weapons       = 0;
static int  g_mint_armor         = 0;
static int  g_mint_powerups      = 0;
static int  g_mint_keys          = 0;
static int  g_max_health         = 100;
static int  g_max_armor          = 200;
static int  g_always_allow_pickup_if_max    = 1;
static int  g_always_add_items_to_inventory = 0;
static int  g_use_health_on_pickup          = 0;
static int  g_use_armor_on_pickup           = 0;
static int  g_use_powerup_on_pickup         = 0;
static int  g_beam_face          = 1;
static char g_nft_provider[64]   = "SolanaOASIS";
static char g_send_to_address[256]= "";

/* -------------------------------------------------------------------------
 * Internal helpers
 * ------------------------------------------------------------------------- */

static void OQ2RTX_StarLog(const char* fmt, ...) {
    char buf[512];
    va_list ap;
    va_start(ap, fmt);
    vsnprintf(buf, sizeof(buf), fmt, ap);
    va_end(ap);
    buf[sizeof(buf) - 1] = '\0';
    Q2RTX_Con_Printf("[OQuake2-RTX] %s\n", buf);
}

static void OQ2RTX_StarDebugLog(const char* fmt, ...) {
    char buf[512];
    va_list ap;
    if (!g_star_debug_logging) return;
    va_start(ap, fmt);
    vsnprintf(buf, sizeof(buf), fmt, ap);
    va_end(ap);
    buf[sizeof(buf) - 1] = '\0';
    Q2RTX_Con_Printf("[OQuake2-RTX DEBUG] %s\n", buf);
}

static void OQ2RTX_SetToastMessage(const char* msg) {
    if (!msg || !msg[0]) return;
    Q2RTX_Q_strlcpy(g_oq2rtx_toast_message, msg, sizeof(g_oq2rtx_toast_message));
    g_oq2rtx_toast_frames = OQ2RTX_TOAST_FRAMES_DEFAULT;
}

static int OQ2RTX_DoMintForItemType(const char* item_type) {
    if (!item_type || !item_type[0]) return 0;
    if (OQ2RTX_ContainsNoCase(item_type, "Key")) return g_mint_keys;
    if (OQ2RTX_ContainsNoCase(item_type, "Weapon")) return g_mint_weapons;
    if (OQ2RTX_ContainsNoCase(item_type, "Armor")) return g_mint_armor;
    if (OQ2RTX_ContainsNoCase(item_type, "Powerup") || OQ2RTX_ContainsNoCase(item_type, "Artifact")) return g_mint_powerups;
    return 0;
}

static void OQ2RTX_QueuePickup(const char* item_name, const char* description,
                                const char* item_type, int quantity) {
    int do_mint;
    if (!item_name || !item_name[0]) return;
    do_mint = OQ2RTX_DoMintForItemType(item_type);
    if (do_mint) {
        ogengine_queue_pickup_with_mint(
            item_name, description ? description : "",
            "Quake2RTX", item_type ? item_type : "Item",
            1, g_nft_provider[0] ? g_nft_provider : "SolanaOASIS",
            g_send_to_address[0] ? g_send_to_address : NULL,
            quantity > 0 ? quantity : 1);
    } else {
        ogengine_queue_add_item(
            item_name, description ? description : "",
            "Quake2RTX", item_type ? item_type : "Item",
            NULL, quantity > 0 ? quantity : 1, 1);
    }
    ogengine_queue_quest_progress_from_pickup("Quake2RTX", item_type ? item_type : "Item", item_name);
}

/* -------------------------------------------------------------------------
 * Inventory helpers
 * ------------------------------------------------------------------------- */

static void OQ2RTX_RefreshInventoryCache(void) {
    ogengine_item_list_t* list = NULL;
    size_t i;
    if (!g_star_initialized) return;
    if (ogengine_get_inventory(&list) != OGENGINE_SUCCESS || !list) return;
    g_inventory_count = 0;
    for (i = 0; i < list->count && g_inventory_count < OQ2RTX_MAX_INVENTORY_ITEMS; i++) {
        oquake2rtx_inventory_entry_t* e = &g_inventory_entries[g_inventory_count];
        Q2RTX_Q_strlcpy(e->name,        list->items[i].name,        sizeof(e->name));
        Q2RTX_Q_strlcpy(e->description, list->items[i].description, sizeof(e->description));
        Q2RTX_Q_strlcpy(e->item_type,   list->items[i].item_type,   sizeof(e->item_type));
        Q2RTX_Q_strlcpy(e->id,          list->items[i].id,          sizeof(e->id));
        Q2RTX_Q_strlcpy(e->game_source, list->items[i].game_source, sizeof(e->game_source));
        Q2RTX_Q_strlcpy(e->nft_id,      list->items[i].nft_id,      sizeof(e->nft_id));
        e->quantity = list->items[i].quantity;
        g_inventory_count++;
    }
    ogengine_free_item_list(list);
    OQ2RTX_StarDebugLog("Inventory refreshed: %d items", g_inventory_count);
}

/* -------------------------------------------------------------------------
 * Config file I/O
 * ------------------------------------------------------------------------- */

static int OQ2RTX_FindConfigFile(const char* filename, char* out_path, int maxlen) {
    FILE* f;
    const char* locations[] = {
        "", "build/", "../build/",
        "../../OASIS Omniverse/OQuake2-RTX/build/",
        "../OASIS Omniverse/OQuake2-RTX/build/",
        NULL
    };
    int i;
    char test_path[512];
    for (i = 0; locations[i]; i++) {
        Q2RTX_Q_snprintf(test_path, sizeof(test_path), "%s%s", locations[i], filename);
        f = fopen(test_path, "r");
        if (f) { fclose(f); Q2RTX_Q_strlcpy(out_path, test_path, maxlen); return 1; }
    }
#ifdef _WIN32
    {
        char exe_path[MAX_PATH] = {0}, exe_dir[MAX_PATH] = {0};
        if (GetModuleFileNameA(NULL, exe_path, sizeof(exe_path))) {
            char* ls = strrchr(exe_path, '\\');
            if (ls) {
                int dl = (int)(ls - exe_path);
                if (dl > 0 && dl < (int)sizeof(exe_dir)) {
                    memcpy(exe_dir, exe_path, (size_t)dl); exe_dir[dl] = '\0';
                    Q2RTX_Q_snprintf(test_path, sizeof(test_path), "%s\\%s", exe_dir, filename);
                    f = fopen(test_path, "r");
                    if (f) { fclose(f); Q2RTX_Q_strlcpy(out_path, test_path, maxlen); return 1; }
                }
            }
        }
    }
#elif defined(__linux__)
    {
        char self[512]; ssize_t n = readlink("/proc/self/exe", self, sizeof(self) - 1);
        if (n > 0) { self[n] = '\0'; char* sl = strrchr(self, '/');
            if (sl) { *sl = '\0'; Q2RTX_Q_snprintf(test_path, sizeof(test_path), "%s/%s", self, filename);
                f = fopen(test_path, "r"); if (f) { fclose(f); Q2RTX_Q_strlcpy(out_path, test_path, maxlen); return 1; } } }
    }
#endif
    return 0;
}

static int OQ2RTX_LoadJsonConfig(const char* json_path) {
    FILE* f; char* json; long fsz; size_t len; char value[512]; int loaded = 0;
    if (!json_path || !json_path[0]) return 0;
    f = fopen(json_path, "r");
    if (!f) return 0;
    if (fseek(f, 0, SEEK_END) != 0) { fclose(f); return 0; }
    fsz = ftell(f);
    if (fsz <= 0 || fsz > 512 * 1024) { fclose(f); return 0; }
    if (fseek(f, 0, SEEK_SET) != 0) { fclose(f); return 0; }
    json = (char*)malloc((size_t)fsz + 1);
    if (!json) { fclose(f); return 0; }
    len = fread(json, 1, (size_t)fsz, f); fclose(f);
    if (len == 0) { free(json); return 0; }
    json[len] = '\0';

    if (OQ2RTX_ExtractJsonValue(json, "ogengine_url", value, sizeof(value)) && value[0])
        { Q2RTX_Q_strlcpy(g_ogengine_url, value, sizeof(g_ogengine_url)); loaded = 1; }
    else if (OQ2RTX_ExtractJsonValue(json, "star_api_url", value, sizeof(value)) && value[0])
        { Q2RTX_Q_strlcpy(g_ogengine_url, value, sizeof(g_ogengine_url)); loaded = 1; }
    if (OQ2RTX_ExtractJsonValue(json, "oasis_api_url", value, sizeof(value)) && value[0])
        Q2RTX_Q_strlcpy(g_oasis_api_url, value, sizeof(g_oasis_api_url));
    if (OQ2RTX_ExtractJsonValue(json, "star_transport", value, sizeof(value)) && value[0])
        Q2RTX_Q_strlcpy(g_star_transport, value, sizeof(g_star_transport));
    if (OQ2RTX_ExtractJsonValue(json, "oasis_dna_path", value, sizeof(value)) && value[0])
        Q2RTX_Q_strlcpy(g_oasis_dna_path, value, sizeof(g_oasis_dna_path));
    if (OQ2RTX_ExtractJsonValue(json, "avatar_id", value, sizeof(value)) && value[0])
        Q2RTX_Q_strlcpy(g_star_avatar_id, value, sizeof(g_star_avatar_id));
    if (OQ2RTX_ExtractJsonValue(json, "beam_face", value, sizeof(value))) g_beam_face = atoi(value);
    if (OQ2RTX_ExtractJsonValue(json, "stack_armor", value, sizeof(value))) g_stack_armor = atoi(value);
    if (OQ2RTX_ExtractJsonValue(json, "stack_weapons", value, sizeof(value))) g_stack_weapons = atoi(value);
    if (OQ2RTX_ExtractJsonValue(json, "stack_powerups", value, sizeof(value))) g_stack_powerups = atoi(value);
    if (OQ2RTX_ExtractJsonValue(json, "stack_keys", value, sizeof(value))) g_stack_keys = atoi(value);
    if (OQ2RTX_ExtractJsonValue(json, "mint_weapons", value, sizeof(value))) g_mint_weapons = atoi(value);
    if (OQ2RTX_ExtractJsonValue(json, "mint_armor", value, sizeof(value))) g_mint_armor = atoi(value);
    if (OQ2RTX_ExtractJsonValue(json, "mint_powerups", value, sizeof(value))) g_mint_powerups = atoi(value);
    if (OQ2RTX_ExtractJsonValue(json, "mint_keys", value, sizeof(value))) g_mint_keys = atoi(value);
    if (OQ2RTX_ExtractJsonValue(json, "max_health", value, sizeof(value))) g_max_health = atoi(value);
    if (OQ2RTX_ExtractJsonValue(json, "max_armor", value, sizeof(value))) g_max_armor = atoi(value);
    if (OQ2RTX_ExtractJsonValue(json, "always_allow_pickup_if_max", value, sizeof(value))) g_always_allow_pickup_if_max = atoi(value);
    if (OQ2RTX_ExtractJsonValue(json, "always_add_items_to_inventory", value, sizeof(value))) g_always_add_items_to_inventory = atoi(value);
    if (OQ2RTX_ExtractJsonValue(json, "use_health_on_pickup", value, sizeof(value))) g_use_health_on_pickup = atoi(value);
    if (OQ2RTX_ExtractJsonValue(json, "use_armor_on_pickup", value, sizeof(value))) g_use_armor_on_pickup = atoi(value);
    if (OQ2RTX_ExtractJsonValue(json, "use_powerup_on_pickup", value, sizeof(value))) g_use_powerup_on_pickup = atoi(value);
    if (OQ2RTX_ExtractJsonValue(json, "nft_provider", value, sizeof(value)) && value[0])
        Q2RTX_Q_strlcpy(g_nft_provider, value, sizeof(g_nft_provider));
    if (OQ2RTX_ExtractJsonValue(json, "send_to_address_after_minting", value, sizeof(value)) && value[0])
        Q2RTX_Q_strlcpy(g_send_to_address, value, sizeof(g_send_to_address));
    if (OQ2RTX_ExtractJsonValue(json, "beamedin_avatar", value, sizeof(value)) && value[0])
        Q2RTX_Q_strlcpy(g_oq2rtx_saved_username, value, sizeof(g_oq2rtx_saved_username));
    if (OQ2RTX_ExtractJsonValue(json, "saved_jwt", value, sizeof(value)) && value[0])
        Q2RTX_Q_strlcpy(g_oq2rtx_saved_jwt, value, sizeof(g_oq2rtx_saved_jwt));
    if (OQ2RTX_ExtractJsonValue(json, "jwt_token", value, sizeof(value)) && value[0])
        Q2RTX_Q_strlcpy(g_oq2rtx_saved_jwt, value, sizeof(g_oq2rtx_saved_jwt));
    if (OQ2RTX_ExtractJsonValue(json, "refresh_token", value, sizeof(value)) && value[0])
        Q2RTX_Q_strlcpy(g_oq2rtx_saved_refresh, value, sizeof(g_oq2rtx_saved_refresh));
    {
        int i;
        for (i = 0; i < OQ2RTX_MONSTER_COUNT && i < OQ2RTX_MONSTER_FLAGS_MAX; i++) {
            char key[64];
            Q2RTX_Q_snprintf(key, sizeof(key), "mint_monster_%s", OQUAKE2RTX_MONSTERS[i].config_key);
            if (OQ2RTX_ExtractJsonValue(json, key, value, sizeof(value)))
                g_oq2rtx_mint_monster_flags[i] = atoi(value);
        }
    }
    free(json);
    return loaded;
}

static void OQ2RTX_SaveStarConfigToFile(void) {
    char path[512]; FILE* f; int i, j;
    if (g_json_config_path[0]) Q2RTX_Q_strlcpy(path, g_json_config_path, sizeof(path));
    else if (!OQ2RTX_FindConfigFile("oasisstar.json", path, sizeof(path)))
        Q2RTX_Q_strlcpy(path, "oasisstar.json", sizeof(path));
    f = fopen(path, "w"); if (!f) return;
    fprintf(f, "{\n");
    fprintf(f, "  \"ogengine_url\": \"%s\"", g_ogengine_url);
    fprintf(f, ",\n  \"oasis_api_url\": \"%s\"", g_oasis_api_url);
    fprintf(f, ",\n  \"star_transport\": \"%s\"", g_star_transport);
    fprintf(f, ",\n  \"beam_face\": %d", g_beam_face);
    fprintf(f, ",\n  \"stack_armor\": %d,\n  \"stack_weapons\": %d", g_stack_armor, g_stack_weapons);
    fprintf(f, ",\n  \"stack_powerups\": %d,\n  \"stack_keys\": %d", g_stack_powerups, g_stack_keys);
    fprintf(f, ",\n  \"mint_weapons\": %d,\n  \"mint_armor\": %d", g_mint_weapons, g_mint_armor);
    fprintf(f, ",\n  \"mint_powerups\": %d,\n  \"mint_keys\": %d", g_mint_powerups, g_mint_keys);
    fprintf(f, ",\n  \"max_health\": %d,\n  \"max_armor\": %d", g_max_health, g_max_armor);
    fprintf(f, ",\n  \"always_allow_pickup_if_max\": %d", g_always_allow_pickup_if_max);
    fprintf(f, ",\n  \"always_add_items_to_inventory\": %d", g_always_add_items_to_inventory);
    fprintf(f, ",\n  \"use_health_on_pickup\": %d", g_use_health_on_pickup);
    fprintf(f, ",\n  \"nft_provider\": \"%s\"", g_nft_provider);
    {
        char uname[128] = {0}; char jwt[2048] = {0};
        int gu = (ogengine_get_current_username(uname, sizeof(uname)) > 0 && uname[0]);
        if (!gu && g_star_username[0]) { Q2RTX_Q_strlcpy(uname, g_star_username, sizeof(uname)); gu = 1; }
        if (gu) {
            const char* p;
            Q2RTX_Q_strlcpy(g_oq2rtx_saved_username, uname, sizeof(g_oq2rtx_saved_username));
            fprintf(f, ",\n  \"beamedin_avatar\": \"");
            for (p = uname; *p; p++) { if (*p == '"' || *p == '\\') fputc('\\', f); fputc((unsigned char)*p, f); }
            fprintf(f, "\"");
        }
        if (ogengine_get_current_jwt(jwt, sizeof(jwt)) > 0 && jwt[0]) {
            const char* p;
            Q2RTX_Q_strlcpy(g_oq2rtx_saved_jwt, jwt, sizeof(g_oq2rtx_saved_jwt));
            fprintf(f, ",\n  \"saved_jwt\": \"");
            for (p = jwt; *p; p++) { if (*p == '"' || *p == '\\') fputc('\\', f); fputc((unsigned char)*p, f); }
            fprintf(f, "\"");
        }
        {
            char rb[2048] = {0};
            if (ogengine_get_current_refresh_token(rb, sizeof(rb)) > 0 && rb[0]) {
                const char* p;
                fprintf(f, ",\n  \"refresh_token\": \"");
                for (p = rb; *p; p++) { if (*p == '"' || *p == '\\') fputc('\\', f); fputc((unsigned char)*p, f); }
                fprintf(f, "\"");
            }
        }
    }
    for (i = 0; i < OQ2RTX_MONSTER_COUNT && i < OQ2RTX_MONSTER_FLAGS_MAX; i++) {
        int already = 0;
        for (j = 0; j < i; j++)
            if (!strcmp(OQUAKE2RTX_MONSTERS[j].config_key, OQUAKE2RTX_MONSTERS[i].config_key)) { already = 1; break; }
        if (already) continue;
        fprintf(f, ",\n  \"mint_monster_%s\": %d", OQUAKE2RTX_MONSTERS[i].config_key, g_oq2rtx_mint_monster_flags[i] ? 1 : 0);
    }
    fprintf(f, "\n}\n");
    fclose(f);
}

/* -------------------------------------------------------------------------
 * Auth / operation callbacks
 * ------------------------------------------------------------------------- */

static void OQ2RTX_OnAuthDone(ogengine_result_t result, void* user_data) {
    (void)user_data;
    if (g_star_auth_timed_out) { g_star_auth_timed_out = 0; return; }
    g_star_async_auth_pending = 0;
    if (result == OGENGINE_SUCCESS) {
        char uname[64] = {0};
        g_star_beamed_in = 1;
        ogengine_get_current_username(uname, sizeof(uname));
        if (uname[0]) Q2RTX_Q_strlcpy(g_star_username, uname, sizeof(g_star_username));
        Q2RTX_Con_Printf("[OQuake2-RTX] Beamed in as: %s\n", g_star_username[0] ? g_star_username : "(unknown)");
        ogengine_refresh_avatar_profile();
        ogengine_request_inventory_in_background();
        OQ2RTX_SaveStarConfigToFile();
    } else {
        g_star_beamed_in = 0;
        Q2RTX_Con_Printf("[OQuake2-RTX] Beam-in failed: %s\n", ogengine_get_last_error());
    }
}

static void OQ2RTX_OnOperationDone(ogengine_result_t result, int operation_type, void* user_data) {
    (void)user_data; (void)result;
    if (operation_type == OGENGINE_OP_PROFILE_LOADED)
        g_star_profile_loaded_pending = 1;
    else if (operation_type == OGENGINE_OP_GET_INVENTORY) {
        g_inventory_refresh_pending = 1;
        g_inventory_requested = 0;
    }
}

static void OQ2RTX_OnSendItemDone(void* user_data) {
    int success = 0; char err_buf[384] = {0}; (void)user_data;
    if (!ogengine_sync_send_item_get_result(&success, err_buf, sizeof(err_buf))) return;
    if (success) Q2RTX_Q_strlcpy(g_inventory_status, "Item sent.", sizeof(g_inventory_status));
    else Q2RTX_Q_snprintf(g_inventory_status, sizeof(g_inventory_status), "Send failed: %s", err_buf[0] ? err_buf : "unknown error");
    OQ2RTX_RefreshInventoryCache();
}

/* -------------------------------------------------------------------------
 * Console command
 * ------------------------------------------------------------------------- */

void OQuake2RTX_STAR_Console_f(void) {
    Q2RTX_Con_Printf("[OQuake2-RTX] STAR console — use: star beamin <user> <pass>  |  star inventory  |  star version\n");
}

/* -------------------------------------------------------------------------
 * Public API: Init / Cleanup
 * ------------------------------------------------------------------------- */

void OQuake2RTX_STAR_Init(void) {
    char found_json_path[512] = {0};
    int i;

    ogengine_sync_init();

    for (i = 0; i < OQ2RTX_MONSTER_FLAGS_MAX; i++)
        g_oq2rtx_mint_monster_flags[i] = 1;

    if (OQ2RTX_FindConfigFile("oasisstar.json", found_json_path, sizeof(found_json_path))) {
        if (OQ2RTX_LoadJsonConfig(found_json_path)) {
            Q2RTX_Q_strlcpy(g_json_config_path, found_json_path, sizeof(g_json_config_path));
            Q2RTX_Con_Printf("[OQuake2-RTX] Loaded config: %s\n", found_json_path);
        }
    } else {
        Q2RTX_Con_Printf("[OQuake2-RTX] oasisstar.json not found — using defaults. "
                         "Copy oasisstar.json from OQuake2-RTX folder next to the game exe.\n");
    }

    memset(&g_star_config, 0, sizeof(g_star_config));
    g_star_config.base_url          = g_ogengine_url;
    g_star_config.api_key           = g_star_key[0] ? g_star_key : NULL;
    g_star_config.avatar_id         = g_star_avatar_id[0] ? g_star_avatar_id : NULL;
    g_star_config.timeout_seconds   = 30;
    g_star_config.client_game_source= "OQUAKE2RTX";
    g_star_config.transport         = (!strcmp(g_star_transport, "native")) ? 1 : 0;
    g_star_config.oasis_dna_path    = g_oasis_dna_path[0] ? g_oasis_dna_path : NULL;

    if (ogengine_init(&g_star_config) != OGENGINE_SUCCESS) {
        Q2RTX_Con_Printf("[OQuake2-RTX] STAR API init failed: %s\n", ogengine_get_last_error());
        return;
    }

    if (g_oasis_api_url[0]) ogengine_set_oasis_base_url(g_oasis_api_url);

    ogengine_set_operation_callback(OQ2RTX_OnOperationDone, NULL);
    ogengine_set_callback(OQ2RTX_OnAuthDone, NULL);

    if (g_oq2rtx_saved_jwt[0]) {
        ogengine_set_saved_session(g_oq2rtx_saved_jwt);
        if (g_oq2rtx_saved_refresh[0]) ogengine_set_refresh_token(g_oq2rtx_saved_refresh);
        ogengine_restore_session();
        Q2RTX_Con_Printf("[OQuake2-RTX] Restoring session for: %s\n",
                         g_oq2rtx_saved_username[0] ? g_oq2rtx_saved_username : "(unknown)");
    }

    g_star_initialized = 1;
    Q2RTX_Con_Printf("[OQuake2-RTX] OASIS STAR API initialized. Q2 RTX renderer active.\n");
    Q2RTX_Con_Printf("[OQuake2-RTX] OASIS thing type range: 6000-6899 (shared with OQuake2). Portal: 5900.\n");
    Q2RTX_Con_Printf("[OQuake2-RTX] Use 'star beamin <user> <pass>' to sign in.\n");
}

void OQuake2RTX_STAR_Cleanup(void) {
    if (!g_star_initialized) return;
    OQ2RTX_SaveStarConfigToFile();
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

void OQuake2RTX_STAR_OnKeyPickup(const char* key_name) {
    const char* desc;
    if (!g_star_initialized || !g_star_beamed_in || !key_name || !key_name[0]) return;
    desc = (!strcmp(key_name, OQUAKE2RTX_ITEM_BLUE_KEY))
        ? "Blue Key - Opens blue-marked doors (Q2 RTX)"
        : (!strcmp(key_name, OQUAKE2RTX_ITEM_RED_KEY))
        ? "Red Key - Opens red-marked doors (Q2 RTX)"
        : "Key from Q2 RTX";
    Q2RTX_Con_Printf("[OQuake2-RTX] Key picked up: %s -> STAR inventory\n", key_name);
    ogengine_queue_add_item(key_name, desc, "Quake2RTX", "KeyItem", NULL, 1, 1);
    ogengine_queue_quest_progress_from_pickup("Quake2RTX", "KeyItem", key_name);
}

int OQuake2RTX_STAR_CheckDoorAccess(const char* door_targetname, const char* required_key_name) {
    if (!g_star_initialized || !g_star_beamed_in || !required_key_name || !required_key_name[0]) return 0;
    if (ogengine_has_item(required_key_name)) {
        Q2RTX_Con_Printf("[OQuake2-RTX] Door '%s' opened via STAR key: %s\n",
                         door_targetname ? door_targetname : "(unnamed)", required_key_name);
        return 1;
    }
    return 0;
}

/* -------------------------------------------------------------------------
 * Public API: Item pickup hooks
 * ------------------------------------------------------------------------- */

void OQuake2RTX_STAR_OnItemPickup(const char* item_name, const char* item_type,
                                   int quantity, const char* description) {
    if (!g_star_initialized || !g_star_beamed_in || !item_name || !item_name[0]) return;
    OQ2RTX_QueuePickup(item_name, description ? description : "", item_type, quantity);
    OQ2RTX_StarDebugLog("OnItemPickup: '%s' type='%s' qty=%d", item_name, item_type ? item_type : "Item", quantity);
}

void OQuake2RTX_STAR_OnPickupLeftOnFloor(const char* item_name, const char* item_type,
                                          int quantity, const char* description) {
    if (!g_star_initialized || !g_star_beamed_in || !item_name || !item_name[0]) return;
    OQ2RTX_QueuePickup(item_name, description ? description : "", item_type, quantity);
    OQ2RTX_StarDebugLog("OnPickupLeftOnFloor: '%s' qty=%d -> STAR", item_name, quantity);
}

int OQuake2RTX_STAR_InterceptTouchPickupAtMax(void* item_ent, void* player_ent) {
    (void)item_ent; (void)player_ent;
    if (!g_star_initialized || !g_star_beamed_in || !g_always_allow_pickup_if_max) return 0;
    return 0;
}

/* -------------------------------------------------------------------------
 * Public API: Monster kill hooks
 * ------------------------------------------------------------------------- */

void OQuake2RTX_STAR_OnMonsterKilled(const char* monster_classname) {
    int i;
    if (!g_star_initialized || !g_star_beamed_in || !monster_classname || !monster_classname[0]) return;
    for (i = 0; i < OQ2RTX_MONSTER_COUNT && i < OQ2RTX_MONSTER_FLAGS_MAX; i++) {
        if (!Q2RTX_Q_strcasecmp(OQUAKE2RTX_MONSTERS[i].engine_classname, monster_classname)) {
            ogengine_queue_monster_kill(
                monster_classname, OQUAKE2RTX_MONSTERS[i].display_name,
                OQUAKE2RTX_MONSTERS[i].xp, OQUAKE2RTX_MONSTERS[i].is_boss,
                g_oq2rtx_mint_monster_flags[i],
                g_nft_provider[0] ? g_nft_provider : "SolanaOASIS",
                "Quake2RTX");
            return;
        }
    }
    /* Unknown monster: default XP, no mint */
    ogengine_queue_monster_kill(monster_classname, monster_classname, 25, 0, 0, NULL, "Quake2RTX");
}

void OQuake2RTX_STAR_OnBossKilled(const char* boss_name) {
    OQuake2RTX_STAR_OnMonsterKilled(boss_name);
}

/* -------------------------------------------------------------------------
 * Public API: Frame pump
 * ------------------------------------------------------------------------- */

void OQuake2RTX_STAR_PollItems(void) {
    char mint_item[256], nft_id[128], hash[128], err_buf[384];
    if (!g_star_initialized) return;
    ogengine_sync_pump();

    /* --- cross-game spawn poll --- */
    {
        char entity_id[128];
        float sx, sy, sz;
        if (ogengine_poll_spawn_event(entity_id, sizeof(entity_id), &sx, &sy, &sz))
        {
            OQ2RTX_StarLog("OASIS SpawnEvent: %s at %.0f/%.0f/%.0f (Q2RTX G_Spawn setup deferred)", entity_id, sx, sy, sz);
            /* TODO: call G_Spawn() + set classname/origin fields + gi.linkentity() */
            ogengine_confirm_spawn(entity_id);
        }
    }

    /* --- cross-game event poll --- */
    {
        char evt_json[4096];
        while (ogengine_poll_cross_game_event(evt_json, sizeof(evt_json)))
        {
            char evt_type[64] = "";
            OQ2RTX_ExtractJsonValue(evt_json, "EventType", evt_type, sizeof(evt_type));
            if (strcmp(evt_type, "ShowNarration") == 0) {
                char narration[256] = "";
                OQ2RTX_ExtractJsonValue(evt_json, "NarrationText", narration, sizeof(narration));
                if (narration[0]) OQ2RTX_SetToastMessage(narration);
            } else if (strcmp(evt_type, "PlayAudio") == 0) {
                char audio_title[128] = "", audio_url[256] = "";
                OQ2RTX_ExtractJsonValue(evt_json, "AudioTitle", audio_title, sizeof(audio_title));
                OQ2RTX_ExtractJsonValue(evt_json, "AudioUrl",   audio_url,   sizeof(audio_url));
                OQ2RTX_StarLog("OASIS PlayAudio: %s (%s) — streaming not yet implemented", audio_title, audio_url);
                /* TODO: play audio via Q2RTX sound system */
            } else if (strcmp(evt_type, "PlayVideo") == 0) {
                char video_title[128] = "", video_url[256] = "";
                OQ2RTX_ExtractJsonValue(evt_json, "VideoTitle", video_title, sizeof(video_title));
                OQ2RTX_ExtractJsonValue(evt_json, "VideoUrl",   video_url,   sizeof(video_url));
                OQ2RTX_StarLog("OASIS PlayVideo: %s (%s) — video overlay not yet implemented", video_title, video_url);
            } else if (strcmp(evt_type, "OpenWebsite") == 0) {
                char website_url[256] = "";
                OQ2RTX_ExtractJsonValue(evt_json, "WebsiteUrl", website_url, sizeof(website_url));
                OQ2RTX_StarLog("OASIS OpenWebsite: %s — browser overlay not yet implemented", website_url);
            } else if (strcmp(evt_type, "UnlockPortal") == 0) {
                char portal_id[64] = "";
                OQ2RTX_ExtractJsonValue(evt_json, "PortalId", portal_id, sizeof(portal_id));
                OQ2RTX_StarLog("OASIS UnlockPortal: %s — portal unlock not yet implemented", portal_id);
            }
        }
    }

    /* --- inventory grant poll --- */
    {
        char item_guid[64];
        while (ogengine_poll_inventory_grant(item_guid, sizeof(item_guid)))
        {
            OQ2RTX_StarLog("OASIS InventoryGrant: %s — inventory refresh triggered", item_guid);
            ogengine_get_inventory(NULL);
        }
    }

    /* Auth timeout check */
    if (g_star_async_auth_pending) {
#ifdef _WIN32
        LARGE_INTEGER freq, now; double elapsed = 0.0;
        if (QueryPerformanceFrequency(&freq) && QueryPerformanceCounter(&now))
            elapsed = (double)now.QuadPart / (double)freq.QuadPart - g_star_async_auth_start;
#else
        struct timespec ts; double elapsed = 0.0;
        if (clock_gettime(CLOCK_MONOTONIC, &ts) == 0)
            elapsed = (double)ts.tv_sec + (double)ts.tv_nsec * 1e-9 - g_star_async_auth_start;
#endif
        if (elapsed > OQ2RTX_BEAMIN_ASYNC_TIMEOUT_SEC) {
            g_star_async_auth_pending = 0; g_star_auth_timed_out = 1;
            Q2RTX_Con_Printf("[OQuake2-RTX] Beam-in timed out. Try 'star beamin' again.\n");
        }
    }
    if (g_star_profile_loaded_pending) {
        g_star_profile_loaded_pending = 0;
        ogengine_request_inventory_in_background();
    }
    if (g_inventory_refresh_pending) {
        g_inventory_refresh_pending = 0;
        OQ2RTX_RefreshInventoryCache();
    }
    while (ogengine_consume_last_mint_result(mint_item, sizeof(mint_item), nft_id, sizeof(nft_id), hash, sizeof(hash)))
        if (mint_item[0]) Q2RTX_Con_Printf("[OQuake2-RTX] Minted NFT: %s (id=%s)\n", mint_item, nft_id[0] ? nft_id : "?");
    while (ogengine_consume_last_background_error(err_buf, sizeof(err_buf)))
        if (err_buf[0]) Q2RTX_Con_Printf("[OQuake2-RTX] STAR error: %s\n", err_buf);
    { char log_buf[512]; while (ogengine_consume_console_log(log_buf, sizeof(log_buf)))
        if (log_buf[0]) Q2RTX_Con_Printf("[OQuake2-RTX STAR] %s\n", log_buf); }
    if (g_oq2rtx_toast_frames > 0) g_oq2rtx_toast_frames--;
}

/* -------------------------------------------------------------------------
 * Public API: HUD / overlay draw hooks (renderer-neutral stubs)
 * Q2 RTX uses Vulkan; engine integrates these into its own HUD/UI layer.
 * ------------------------------------------------------------------------- */

void OQuake2RTX_STAR_DrawInventoryOverlay(void* ctx)  { (void)ctx; }
void OQuake2RTX_STAR_DrawBeamedInStatus(void* ctx)    { (void)ctx; }
void OQuake2RTX_STAR_DrawQuestTracker(void* ctx)      { (void)ctx; }
void OQuake2RTX_STAR_DrawXpStatus(void* ctx)          { (void)ctx; }
void OQuake2RTX_STAR_DrawToast(void* ctx)             { (void)ctx; }
void OQuake2RTX_STAR_DrawVersionStatus(void* ctx)     { (void)ctx; }

/* -------------------------------------------------------------------------
 * Public API: Popup state queries
 * ------------------------------------------------------------------------- */

int OQuake2RTX_STAR_IsQuestPopupOpen(void)    { return g_quest_popup_open ? 1 : 0; }
int OQuake2RTX_STAR_IsInventoryPopupOpen(void){ return g_inventory_open   ? 1 : 0; }

/* -------------------------------------------------------------------------
 * Public API: Misc queries
 * ------------------------------------------------------------------------- */

int OQuake2RTX_STAR_ShouldUseAnorakFace(void) {
    if (!g_beam_face || !g_star_initialized) return 0;
    return !Q2RTX_Q_strcasecmp(g_star_username, "anorak") ||
           !Q2RTX_Q_strcasecmp(g_star_username, "avatar") ||
           !Q2RTX_Q_strcasecmp(g_star_username, "dellams");
}

const char* OQuake2RTX_STAR_GetUsername(void) {
    return g_star_username;
}

/* -------------------------------------------------------------------------
 * Cross-game teleportation
 * ------------------------------------------------------------------------- */

void OQuake2RTX_STAR_CheckIncomingTeleport(void)
{
    char map[256];
    float x = 0, y = 0, z = 64;
    if (!ogengine_poll_teleport_request(map, sizeof(map), &x, &y, &z))
        return;
    OQ2RTX_StarLog("OASIS Teleport arrive: map=%s pos=%.0f/%.0f/%.0f", map, x, y, z);
    {
        edict_t *player_ent = &g_edicts[1];
        VectorSet(player_ent->s.origin, x, y, z);
        VectorClear(player_ent->velocity);
        gi.linkentity(player_ent);
    }
    ogengine_confirm_teleport_arrival();
}
