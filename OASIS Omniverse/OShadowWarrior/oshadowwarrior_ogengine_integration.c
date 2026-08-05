/**
 * OShadowWarrior - OASIS STAR API Integration Implementation
 *
 * Integrates VoidSW (Shadow Warrior Classic / Build engine) with the OASIS
 * STAR API.  VoidSW is structurally identical to EDuke32, so this file closely
 * mirrors oduke3d_ogengine_integration.c — same Build engine compat headers,
 * same OSD / printext256 draw API.
 *
 * Integration points (see oshadowwarrior_ogengine_integration.h for details):
 *   OShadowWarrior_STAR_Init       → GameMain() / SW_GameMain()
 *   OShadowWarrior_STAR_Cleanup    → G_GameExit() / engine teardown
 *   OShadowWarrior_STAR_Tick       → DoGamePlay() once per tic
 *   OShadowWarrior_STAR_OnItemPickup → DoPickupItem() / CheckPickupSprite()
 *   OShadowWarrior_STAR_OnEnemyKilled → KillEnemy() path
 *   OShadowWarrior_STAR_DrawHUDStatus → DrawStatus() / HUD draw
 *   OShadowWarrior_STAR_HandleKey  → gameInput() key handler
 *
 * Editor: Mapster32 — the shared OASIS Mapster32 companion tool
 *         (oasis_m32_tool) covers OShadowWarrior portals, quests and assets.
 *
 * Base engine: VoidSW  https://github.com/BSzili/VoidSW  (GPL-2.0)
 */

#include <stdarg.h>
#include <stdlib.h>
#include <string.h>
#include <stdio.h>

/* VoidSW / JFShadowWarrior Build engine compat layer */
#include "compat.h"

/* Build engine console output (same declaration as EDuke32's OSD_Printf) */
extern int OSD_Printf(const char* fmt, ...);

/* Build engine text draw (same as EDuke32's printext256) */
extern void printext256(int32_t xpos, int32_t ypos, int16_t col,
                        int16_t backcol, const char* name, uint8_t fontsize);

#include "oshadowwarrior_ogengine_integration.h"

/* STAR API */
#include "ogengine.h"
#include "ogengine_sync.h"

/* OGLib single-TU implementation */
#define OGLIB_SESSION_IMPL
#define OGLIB_MONSTER_IMPL
#include "OGLib/oglib.h"

/*===========================================================================
 * VoidSW scan-code fallbacks (Build engine / SDL)
 *=========================================================================*/

#ifndef sc_I
# define sc_I       23
#endif
#ifndef sc_Q
# define sc_Q       16
#endif
#ifndef sc_Escape
# define sc_Escape   1
#endif

/*===========================================================================
 * Constants and module state
 *=========================================================================*/

#define OSW_GAME_SOURCE   "OSHADOWWARRIOR"
#define OSW_LOG_TAG       "[SW] "
#define OSW_VERSION_STR   "OShadowWarrior 1.0.0"
#define OSW_MAX_PATH      512
#define OSW_TOAST_TICKS   180   /* ~3 s at 60 tics/s */
#define OSW_INV_MAX       32
#define OSW_QUEST_MAX     16

static int  g_initialized  = 0;
static int  g_client_ready = 0;
static int  g_debug        = 1;
static char g_json_path[OSW_MAX_PATH];
static char g_username[128];
static int  g_xp           = 0;

/* Toast */
static char g_toast_msg[256];
static int  g_toast_ticks  = 0;

/* Inventory overlay */
static char g_inv_names[OSW_INV_MAX][64];
static int  g_inv_qty[OSW_INV_MAX];
static int  g_inv_count    = 0;
static int  g_show_inv     = 0;

/*===========================================================================
 * Shadow Warrior enemy XP table  (VoidSW sprite type → XP)
 * Sprite constants from src/names.h / sw.h
 *=========================================================================*/

typedef struct { int id; const char* name; int xp; } sw_enemy_t;

static const sw_enemy_t SW_ENEMIES[] = {
    /*  id    name               xp  */
    {  0,  "Ninja",              20 },
    {  1,  "Coolie",             15 },
    {  2,  "CoolieGhost",        15 },
    {  3,  "Ripper",             25 },
    {  4,  "RipperLittle",       10 },
    {  5,  "Guardian",           35 },
    {  6,  "GuardianStatue",     35 },
    {  7,  "Serpent",            30 },
    {  8,  "Bunny",               5 },
    {  9,  "SkeletonStatue",     40 },
    { 10,  "Sumo",              120 },
    { 11,  "DemonLord",         300 },  /* Boss */
    { 12,  "Hornet",             10 },
    { 13,  "ZillaBoss",         350 },  /* Final boss */
    { -1,  NULL,                  0 },
};

static int sw_enemy_xp(int enemy_id, const char* enemy_name) {
    for (int i = 0; SW_ENEMIES[i].id >= 0; ++i) {
        if (SW_ENEMIES[i].id == enemy_id)
            return SW_ENEMIES[i].xp;
    }
    /* Fallback: use OGLib if ID unknown */
    (void)enemy_name;
    return 5;
}

/*===========================================================================
 * Shadow Warrior item table (sprite type → STAR item key + type)
 *=========================================================================*/

typedef struct { int id; const char* item_key; const char* item_type; } sw_item_t;

static const sw_item_t SW_ITEMS[] = {
    {  0, "sw_medkit",            "health"    },
    {  1, "sw_medkit_large",      "health"    },
    {  2, "sw_heart",             "health"    },
    {  3, "sw_fortune_cookie",    "health"    },
    {  4, "sw_armor_vest",        "armor"     },
    {  5, "sw_armor_flash_bomb",  "armor"     },
    {  6, "sw_shuriken",          "ammo"      },
    {  7, "sw_grenade",           "ammo"      },
    {  8, "sw_rocket",            "ammo"      },
    {  9, "sw_sticky_bomb",       "ammo"      },
    { 10, "sw_nuke_warhead",      "ammo"      },
    { 11, "sw_uzi",               "weapon"    },
    { 12, "sw_shotgun",           "weapon"    },
    { 13, "sw_rail_gun",          "weapon"    },
    { 14, "sw_rocket_launcher",   "weapon"    },
    { 15, "sw_grenade_launcher",  "weapon"    },
    { 16, "sw_heart_of_weasel",   "powerup"   },
    { 17, "sw_caltrops",          "ammo"      },
    { 18, "sw_gas_bomb",          "weapon"    },
    { -1, NULL,                   NULL        },
};

static const sw_item_t* sw_item_lookup(int item_id) {
    for (int i = 0; SW_ITEMS[i].id >= 0; ++i)
        if (SW_ITEMS[i].id == item_id)
            return &SW_ITEMS[i];
    return NULL;
}

/*===========================================================================
 * Logging
 *=========================================================================*/

static void osw_log(const char* fmt, ...) {
    if (!g_debug) return;
    char buf[512];
    va_list ap; va_start(ap, fmt);
    vsnprintf(buf, sizeof(buf), fmt, ap);
    va_end(ap);
    OSD_Printf("%s%s\n", OSW_LOG_TAG, buf);
}

static void show_toast(const char* msg) {
    Bstrncpy(g_toast_msg, msg, sizeof(g_toast_msg) - 1);
    g_toast_msg[sizeof(g_toast_msg) - 1] = '\0';
    g_toast_ticks = OSW_TOAST_TICKS;
}

/*===========================================================================
 * Callbacks
 *=========================================================================*/

static void on_inventory_done(ogengine_item_list_t* list,
                               ogengine_result_t result,
                               const char* error_msg,
                               void* user) {
    (void)error_msg; (void)user;
    g_inv_count = 0;
    if (result != OGENGINE_SUCCESS || !list) return;
    int n = list->count < OSW_INV_MAX ? list->count : OSW_INV_MAX;
    for (int i = 0; i < n; ++i) {
        Bstrncpy(g_inv_names[i], list->items[i].name, 63);
        g_inv_qty[i] = list->items[i].quantity;
    }
    g_inv_count = n;
}

static void on_auth_done(int success, const char* username,
                          const char* avatar_id, const char* error_msg,
                          void* user) {
    (void)avatar_id; (void)user;
    if (success) {
        Bstrncpy(g_username, username ? username : "", sizeof(g_username) - 1);
        g_client_ready = 1;
        osw_log("Logged in as %s", g_username);
        show_toast("OASIS: Welcome to OShadowWarrior, Lo Wang.");
        ogengine_sync_inventory_start(OSW_GAME_SOURCE, on_inventory_done, NULL);
    } else {
        osw_log("Auth failed: %s", error_msg ? error_msg : "unknown");
        g_client_ready = 0;
    }
}

/*===========================================================================
 * Public API
 *=========================================================================*/

void OShadowWarrior_STAR_Init(const char* star_api_base_url,
                               const char* oasis_json_path) {
    if (g_initialized) return;

    ogengine_sync_init();
    Bstrncpy(g_json_path, oasis_json_path ? oasis_json_path : "oasisstar.json",
             OSW_MAX_PATH - 1);

    ogengine_result_t rc = ogengine_init(star_api_base_url);
    if (rc != OGENGINE_SUCCESS) {
        osw_log("ogengine_init failed (%d): %s", (int)rc,
                ogengine_get_last_error());
        return;
    }

    char saved_user[128] = {0};
    if (ogengine_load_session(g_json_path, saved_user, sizeof(saved_user))
            == OGENGINE_SUCCESS && saved_user[0]) {
        Bstrncpy(g_username, saved_user, sizeof(g_username) - 1);
        g_client_ready = 1;
        ogengine_sync_inventory_start(OSW_GAME_SOURCE, on_inventory_done, NULL);
        osw_log("Session restored for %s", g_username);
    }

    g_initialized = 1;
    osw_log("OASIS OShadowWarrior v" OSW_VERSION_STR " ready");
}

void OShadowWarrior_STAR_Cleanup(void) {
    if (!g_initialized) return;
    ogengine_sync_cleanup();
    ogengine_shutdown();
    g_initialized = 0;
    g_client_ready = 0;
}

void OShadowWarrior_STAR_Tick(void) {
    if (!g_initialized) return;
    ogengine_sync_pump();
    if (g_toast_ticks > 0) --g_toast_ticks;
}

void OShadowWarrior_STAR_OnItemPickup(int item_id, const char* item_name) {
    if (!g_client_ready) return;
    const sw_item_t* item = sw_item_lookup(item_id);
    if (!item) {
        if (item_name && item_name[0]) {
            ogengine_queue_add_item(item_name, "sw_item",
                                    OSW_GAME_SOURCE, g_username, 1);
        }
        return;
    }
    ogengine_queue_add_item(item->item_key, item->item_type,
                            OSW_GAME_SOURCE, g_username, 1);
    osw_log("Pickup: %s → %s", item_name ? item_name : "?", item->item_key);
}

void OShadowWarrior_STAR_OnEnemyKilled(int enemy_id, const char* enemy_name,
                                        const char* killer) {
    if (!g_client_ready) return;
    int xp = sw_enemy_xp(enemy_id, enemy_name);
    ogengine_queue_add_xp(xp, OSW_GAME_SOURCE,
                          killer && killer[0] ? killer : g_username);
    g_xp += xp;
    if (g_debug)
        osw_log("Killed %s (id=%d) → +%d XP (total=%d)",
                enemy_name ? enemy_name : "?", enemy_id, xp, g_xp);
    /* Boss kill toast */
    if (xp >= 120) {
        char toast[128];
        Bsnprintf(toast, sizeof(toast), "OASIS: Boss down! +%d XP", xp);
        show_toast(toast);
    }
}

void OShadowWarrior_STAR_DrawHUDStatus(int screen_w, int screen_h) {
    if (!g_initialized) return;

    /* Toast message — Build engine 8×8 character draw, y near bottom */
    if (g_toast_ticks > 0 && g_toast_msg[0]) {
        printext256(4, screen_h - 20, 14, -1, g_toast_msg, 0);
    }

    /* Inventory overlay */
    if (g_show_inv && g_inv_count > 0) {
        printext256(4, 20, 10, -1, "OASIS Inventory:", 0);
        for (int i = 0; i < g_inv_count && i < 8; ++i) {
            char line[128];
            Bsnprintf(line, sizeof(line), "  %-36s x%d",
                      g_inv_names[i], g_inv_qty[i]);
            printext256(4, 30 + i * 10, 15, -1, line, 0);
        }
    }

    /* XP badge — top-right */
    if (g_client_ready && g_xp > 0) {
        char xp_str[32];
        Bsnprintf(xp_str, sizeof(xp_str), "XP:%d", g_xp);
        printext256(screen_w - 60, 4, 14, -1, xp_str, 0);
    }

    (void)screen_w;
}

int OShadowWarrior_STAR_HandleKey(int scan_code) {
    if (scan_code == sc_I) {
        g_show_inv = !g_show_inv;
        return 1;
    }
    if (scan_code == sc_Q) {
        if (g_client_ready)
            osw_log("OASIS: XP=%d inv=%d user=%s", g_xp, g_inv_count, g_username);
        return 1;
    }
    return 0;
}

int OShadowWarrior_STAR_IsReady(void) { return g_client_ready; }
