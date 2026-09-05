/**
 * OShadowWarriorRT - OASIS STAR API Integration Implementation
 *
 * Integrates Duke-RT (Shadow Warrior Classic backend) with the OASIS STAR API.
 * Duke-RT (https://github.com/postmemetic/Duke-RT) is a Raze fork that adds
 * Vulkan path-traced ray-tracing.  All hook sites, CCMD usage, and screen
 * API calls are identical to OShadowWarrior — only the game-source identifier
 * differs ("OSHADOWWARRIOR_RT" vs "OSHADOWWARRIOR").
 *
 * Base engine: Duke-RT (Raze fork)  https://github.com/postmemetic/Duke-RT  (GPL-3.0)
 */

#include "oshadowwarriorrt_ogengine_integration.h"
#include "ogengine.h"
#include "ogengine_sync.h"

#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <cstdarg>
#include <string>
#include <map>
#include <mutex>
#include <algorithm>
#include <cctype>

/* GZDoom / Raze / Duke-RT shared headers */
#include "c_dispatch.h"
#include "c_console.h"
#include "c_cvars.h"
#include "printf.h"
#include "i_time.h"
#include "v_draw.h"

/*===========================================================================
 * Constants and module state
 *=========================================================================*/

#define OSWRT_GAME_SOURCE  "OSHADOWWARRIOR_RT"
#define OSWRT_LOG_TAG      "[SW-RT] "
#define OSWRT_VERSION_STR  "OShadowWarriorRT 1.0.0"
#define OSWRT_MAX_PATH     512
#define OSWRT_TOAST_TICKS  180
#define OSWRT_INV_MAX      32

static int  g_initialized  = 0;
static int  g_client_ready = 0;
static int  g_debug        = 1;
static char g_json_path[OSWRT_MAX_PATH];
static char g_username[128];
static int  g_xp           = 0;

static char g_toast_msg[256];
static int  g_toast_ticks  = 0;

static char g_inv_names[OSWRT_INV_MAX][64];
static int  g_inv_qty[OSWRT_INV_MAX];
static int  g_inv_count    = 0;
static int  g_show_inv     = 0;

static std::mutex g_mutex;

/*===========================================================================
 * Shadow Warrior enemy XP table  (same values as OShadowWarrior)
 *=========================================================================*/

static const std::map<int,std::pair<const char*,int>> SW_ENEMY_XP = {
    {  0, { "Ninja",           20 } },
    {  1, { "Coolie",          15 } },
    {  2, { "CoolieGhost",     15 } },
    {  3, { "Ripper",          25 } },
    {  4, { "RipperLittle",    10 } },
    {  5, { "Guardian",        35 } },
    {  6, { "GuardianStatue",  35 } },
    {  7, { "Serpent",         30 } },
    {  8, { "Bunny",            5 } },
    {  9, { "SkeletonStatue",  40 } },
    { 10, { "Sumo",           120 } },
    { 11, { "DemonLord",      300 } },
    { 12, { "Hornet",          10 } },
    { 13, { "ZillaBoss",      350 } },
};

/*===========================================================================
 * Shadow Warrior item table
 *=========================================================================*/

struct SwItem { const char* key; const char* type; };
static const std::map<int,SwItem> SW_ITEM_MAP = {
    {  0, { "sw_medkit",           "health"  } },
    {  1, { "sw_medkit_large",     "health"  } },
    {  2, { "sw_heart",            "health"  } },
    {  3, { "sw_fortune_cookie",   "health"  } },
    {  4, { "sw_armor_vest",       "armor"   } },
    {  5, { "sw_armor_flash_bomb", "armor"   } },
    {  6, { "sw_shuriken",         "ammo"    } },
    {  7, { "sw_grenade",          "ammo"    } },
    {  8, { "sw_rocket",           "ammo"    } },
    {  9, { "sw_sticky_bomb",      "ammo"    } },
    { 10, { "sw_nuke_warhead",     "ammo"    } },
    { 11, { "sw_uzi",              "weapon"  } },
    { 12, { "sw_shotgun",          "weapon"  } },
    { 13, { "sw_rail_gun",         "weapon"  } },
    { 14, { "sw_rocket_launcher",  "weapon"  } },
    { 15, { "sw_grenade_launcher", "weapon"  } },
    { 16, { "sw_heart_of_weasel",  "powerup" } },
    { 17, { "sw_caltrops",         "ammo"    } },
    { 18, { "sw_gas_bomb",         "weapon"  } },
};

/*===========================================================================
 * Logging and toast
 *=========================================================================*/

static void oswrt_log(const char* fmt, ...) {
    if (!g_debug) return;
    char buf[512];
    va_list ap; va_start(ap, fmt);
    vsnprintf(buf, sizeof(buf), fmt, ap);
    va_end(ap);
    Printf("%s%s\n", OSWRT_LOG_TAG, buf);
}

static void show_toast(const char* msg) {
    std::lock_guard<std::mutex> lk(g_mutex);
    snprintf(g_toast_msg, sizeof(g_toast_msg), "%s", msg);
    g_toast_ticks = OSWRT_TOAST_TICKS;
}

/*===========================================================================
 * Callbacks
 *=========================================================================*/

static void on_inventory_done(ogengine_item_list_t* list,
                               ogengine_result_t result,
                               const char* /*error_msg*/,
                               void* /*user*/) {
    std::lock_guard<std::mutex> lk(g_mutex);
    g_inv_count = 0;
    if (result != OGENGINE_SUCCESS || !list) return;
    int n = list->count < OSWRT_INV_MAX ? list->count : OSWRT_INV_MAX;
    for (int i = 0; i < n; ++i) {
        snprintf(g_inv_names[i], 64, "%s", list->items[i].name);
        g_inv_qty[i] = list->items[i].quantity;
    }
    g_inv_count = n;
}

static void on_auth_done(int success, const char* username,
                          const char* /*avatar_id*/, const char* error_msg,
                          void* /*user*/) {
    if (success) {
        snprintf(g_username, sizeof(g_username), "%s", username ? username : "");
        g_client_ready = 1;
        oswrt_log("Logged in as %s", g_username);
        show_toast("OASIS: Welcome to OShadowWarriorRT, Lo Wang.");
        ogengine_sync_inventory_start(OSWRT_GAME_SOURCE, on_inventory_done, nullptr);
    } else {
        oswrt_log("Auth failed: %s", error_msg ? error_msg : "unknown");
        g_client_ready = 0;
    }
}

/*===========================================================================
 * Public API
 *=========================================================================*/

extern "C" void OShadowWarriorRT_STAR_Init(const char* star_api_base_url,
                                            const char* oasis_json_path) {
    if (g_initialized) return;

    ogengine_sync_init();
    snprintf(g_json_path, sizeof(g_json_path), "%s",
             oasis_json_path ? oasis_json_path : "oasisstar.json");

    ogengine_result_t rc = ogengine_init(star_api_base_url);
    if (rc != OGENGINE_SUCCESS) {
        oswrt_log("ogengine_init failed (%d): %s", (int)rc,
                  ogengine_get_last_error());
        return;
    }

    char saved_user[128] = {0};
    if (ogengine_load_session(g_json_path, saved_user, sizeof(saved_user))
            == OGENGINE_SUCCESS && saved_user[0]) {
        snprintf(g_username, sizeof(g_username), "%s", saved_user);
        g_client_ready = 1;
        ogengine_sync_inventory_start(OSWRT_GAME_SOURCE, on_inventory_done, nullptr);
        oswrt_log("Session restored for %s", g_username);
    }

    g_initialized = 1;
    oswrt_log("OASIS OShadowWarriorRT v" OSWRT_VERSION_STR " ready (Duke-RT/Raze backend)");
}

extern "C" void OShadowWarriorRT_STAR_Cleanup(void) {
    if (!g_initialized) return;
    ogengine_sync_cleanup();
    ogengine_shutdown();
    g_initialized = 0;
    g_client_ready = 0;
}

extern "C" void OShadowWarriorRT_STAR_Tick(void) {
    if (!g_initialized) return;
    ogengine_sync_pump();
    std::lock_guard<std::mutex> lk(g_mutex);
    if (g_toast_ticks > 0) --g_toast_ticks;
}

extern "C" void OShadowWarriorRT_STAR_OnItemPickup(int sprite_type,
                                                    const char* item_name) {
    if (!g_client_ready) return;
    auto it = SW_ITEM_MAP.find(sprite_type);
    if (it != SW_ITEM_MAP.end()) {
        ogengine_queue_add_item(it->second.key, it->second.type,
                                OSWRT_GAME_SOURCE, g_username, 1);
        if (g_debug)
            oswrt_log("Pickup: %s -> %s",
                      item_name ? item_name : "?", it->second.key);
    } else if (item_name && item_name[0]) {
        ogengine_queue_add_item(item_name, "sw_item",
                                OSWRT_GAME_SOURCE, g_username, 1);
    }
}

extern "C" void OShadowWarriorRT_STAR_OnEnemyKilled(int enemy_type,
                                                     const char* enemy_name,
                                                     const char* killer) {
    if (!g_client_ready) return;
    auto it = SW_ENEMY_XP.find(enemy_type);
    int xp = (it != SW_ENEMY_XP.end()) ? it->second.second : 5;
    ogengine_queue_add_xp(xp, OSWRT_GAME_SOURCE,
                          (killer && killer[0]) ? killer : g_username);
    { std::lock_guard<std::mutex> lk(g_mutex); g_xp += xp; }
    if (g_debug)
        oswrt_log("Killed %s (type=%d) -> +%d XP (total=%d)",
                  enemy_name ? enemy_name : "?", enemy_type, xp, g_xp);
    if (xp >= 120) {
        char toast[128];
        snprintf(toast, sizeof(toast), "OASIS: Boss down! +%d XP", xp);
        show_toast(toast);
    }
}

extern "C" void OShadowWarriorRT_STAR_DrawHUDStatus(int screen_w, int screen_h) {
    if (!g_initialized) return;
    std::lock_guard<std::mutex> lk(g_mutex);

    if (g_toast_ticks > 0 && g_toast_msg[0]) {
        /* screen::DrawText(NewSmallFont, CR_GOLD, 4, screen_h - 24, g_toast_msg); */
        (void)screen_h;
    }
    if (g_show_inv && g_inv_count > 0) {
        for (int i = 0; i < g_inv_count && i < 8; ++i) {
            char line[128];
            snprintf(line, sizeof(line), "  %s x%d", g_inv_names[i], g_inv_qty[i]);
            /* screen::DrawText(NewSmallFont, CR_WHITE, 4, 30 + i*12, line); */
            (void)line;
        }
    }
    if (g_client_ready && g_xp > 0) {
        char xp_str[32];
        snprintf(xp_str, sizeof(xp_str), "XP: %d", g_xp);
        /* screen::DrawText(NewSmallFont, CR_GOLD, screen_w - 80, 4, xp_str); */
        (void)xp_str; (void)screen_w;
    }
}

extern "C" int OShadowWarriorRT_STAR_HandleKey(int key_code) {
    if (key_code == 'I' || key_code == 'i') {
        std::lock_guard<std::mutex> lk(g_mutex);
        g_show_inv = !g_show_inv;
        return 1;
    }
    if (key_code == 'Q' || key_code == 'q') {
        if (g_client_ready)
            oswrt_log("OASIS: XP=%d inv=%d user=%s", g_xp, g_inv_count, g_username);
        return 1;
    }
    return 0;
}

extern "C" int OShadowWarriorRT_STAR_IsReady(void) { return g_client_ready; }

/*===========================================================================
 * CCMD: "star_oswrt" console command
 *=========================================================================*/

CCMD(star_oswrt) {
    if (argv.argc() < 2) {
        Printf("star_oswrt version | inv | xp | login <user> <pass>\n");
        return;
    }
    if (!stricmp(argv[1], "version")) {
        char ver[64]; ogengine_get_version(ver, sizeof(ver));
        Printf(OSWRT_LOG_TAG "STAR API: %s\n", ver);
    } else if (!stricmp(argv[1], "inv")) {
        std::lock_guard<std::mutex> lk(g_mutex);
        Printf(OSWRT_LOG_TAG "Inventory (%d items):\n", g_inv_count);
        for (int i = 0; i < g_inv_count; ++i)
            Printf("  %-40s x%d\n", g_inv_names[i], g_inv_qty[i]);
    } else if (!stricmp(argv[1], "xp")) {
        Printf(OSWRT_LOG_TAG "Session XP: %d\n", g_xp);
    } else if (!stricmp(argv[1], "login") && argv.argc() >= 4) {
        ogengine_sync_auth_start(argv[2], argv[3], on_auth_done, nullptr);
        Printf(OSWRT_LOG_TAG "Authenticating as %s ...\n", argv[2]);
    } else {
        Printf(OSWRT_LOG_TAG "Unknown subcommand '%s'\n", argv[1]);
    }
}
