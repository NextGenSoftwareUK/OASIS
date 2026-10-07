/**
 * OHeretic - OASIS STAR API Integration Implementation
 *
 * Integrates GZDoom (Heretic IWAD) with the OASIS STAR API so keys,
 * artifacts and XP earned in OHeretic flow across the OASIS Omniverse.
 *
 * Build as part of your GZDoom fork with OGEngineClient.dll in the path.
 * Copy ogengine.h / ogengine_sync.h alongside this file before compiling.
 *
 * Integration points (see oheretic_ogengine_integration.h for details):
 *   OHeretic_STAR_Init           → G_InitNew / startup
 *   OHeretic_STAR_Cleanup        → G_ExitLevel / shutdown
 *   OHeretic_STAR_Tick           → G_Ticker (once per tic)
 *   OHeretic_STAR_OnItemPickup   → AInventory::CallTryPickup path
 *   OHeretic_STAR_CheckDoorAccess→ P_ActivateLine locked-door check
 *   OHeretic_STAR_OnMonsterKilled→ AActor::Die / P_KillMobj
 *   OHeretic_STAR_DrawHUDStatus  → D_Display
 *   OHeretic_STAR_HandleKey      → key event handler
 *
 * Base engine: GZDoom  https://github.com/ZDoom/gzdoom  (GPL-3.0)
 */

#include "oheretic_ogengine_integration.h"
#include "ogengine.h"
#include "ogengine_sync.h"

#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <cstdarg>
#include <string>
#include <map>
#include <algorithm>
#include <cctype>
#include <atomic>
#include <mutex>

/* GZDoom headers */
#include "gamedata/a_keys.h"
#include "playsim/actor.h"
#include "playsim/a_pickups.h"
#include "playsim/p_local.h"
#include "gamedata/info.h"
#include "vm.h"
#include "c_dispatch.h"
#include "c_console.h"
#include "c_cvars.h"
#include "printf.h"
#include "i_time.h"
#include "g_levellocals.h"
#include "playsim/d_player.h"

/*===========================================================================
 * Constants and module state
 *=========================================================================*/

#define OHERETIC_GAME_SOURCE  "OHERETIC"
#define OHERETIC_LOG_TAG      "[HERETIC] "
#define OHERETIC_VERSION_STR  "OHeretic 1.0.0"
#define OHERETIC_MAX_PATH     512
#define OHERETIC_TOAST_TICKS  180   /* ~3 s at 60 tics/s */
#define OHERETIC_INV_MAX      32
#define OHERETIC_QUEST_MAX    16

static int  g_initialized  = 0;
static int  g_client_ready = 0;
static int  g_debug        = 1;
static char g_json_path[OHERETIC_MAX_PATH];
static char g_username[128];
static int  g_xp           = 0;
static int  g_beamed_in    = 0;

/* Toast/popup */
static char g_toast_msg[256];
static int  g_toast_ticks  = 0;

/* Inventory overlay */
static char g_inv_names[OHERETIC_INV_MAX][64];
static int  g_inv_qty[OHERETIC_INV_MAX];
static int  g_inv_count    = 0;
static int  g_show_inv     = 0;

/* Mutex for cross-thread state */
static std::mutex g_mutex;

/*===========================================================================
 * Heretic item mappings  (GZDoom class name → OASIS item key)
 *=========================================================================*/

/* Keys */
static const std::map<std::string,std::string> HERETIC_KEY_MAP = {
    { "KeyGreen",  "key_green"  },
    { "KeyYellow", "key_yellow" },
    { "KeyBlue",   "key_blue"   },
};

/* Artifacts */
static const std::map<std::string,std::string> HERETIC_ARTI_MAP = {
    { "ArtiHealth",          "heretic_crystal_vial"      },
    { "ArtiSuperHealth",     "heretic_mystic_urn"        },
    { "ArtiTome",            "heretic_tome_of_power"     },
    { "ArtiTimeBomb",        "heretic_timebomb"          },
    { "ArtiTeleport",        "heretic_chaos_device"      },
    { "ArtiInvulnerability", "heretic_ring_invulnerable" },
    { "ArtiFly",             "heretic_wings_wrath"       },
    { "ArtiTorch",           "heretic_torch"             },
    { "ArtiMorphOvum",       "heretic_morph_ovum"        },
    { "ArtiShadowSphere",    "heretic_shadow_sphere"     },
    { "ArtiBlastRadius",     "heretic_blastradius"       },
};

/* Monster XP table (class name → XP) */
static const std::map<std::string,int> HERETIC_MONSTER_XP = {
    { "Gargoyle",        10 }, { "GargoyleLeader",   20 },
    { "Golem",           15 }, { "GolemGhost",        15 },
    { "Nitrogolem",      20 }, { "NitrogolimGhost",   20 },
    { "Undead",          25 }, { "UndeadWarrior",     25 },
    { "Disciple",        30 },
    { "Weredragon",      40 },
    { "Sabreclaw",       25 },
    { "Ophidian",        50 },
    { "IronLich",       120 },
    { "Maulotaur",      200 },
    { "DSparil",        400 },
};

/*===========================================================================
 * Logging
 *=========================================================================*/

static void oheretic_log(const char* fmt, ...) {
    if (!g_debug) return;
    char buf[512];
    va_list ap; va_start(ap, fmt);
    vsnprintf(buf, sizeof(buf), fmt, ap);
    va_end(ap);
    Printf("%s%s\n", OHERETIC_LOG_TAG, buf);
}

static void show_toast(const char* msg) {
    std::lock_guard<std::mutex> lk(g_mutex);
    snprintf(g_toast_msg, sizeof(g_toast_msg), "%s", msg);
    g_toast_ticks = OHERETIC_TOAST_TICKS;
}

/*===========================================================================
 * Inventory refresh callback
 *=========================================================================*/

static void on_inventory_done(ogengine_item_list_t* list,
                               ogengine_result_t result,
                               const char* error_msg,
                               void* /*user*/) {
    std::lock_guard<std::mutex> lk(g_mutex);
    g_inv_count = 0;
    if (result != OGENGINE_SUCCESS || !list) return;
    int n = list->count < OHERETIC_INV_MAX ? list->count : OHERETIC_INV_MAX;
    for (int i = 0; i < n; ++i) {
        snprintf(g_inv_names[i], 64, "%s", list->items[i].name);
        g_inv_qty[i] = list->items[i].quantity;
    }
    g_inv_count = n;
    (void)error_msg;
}

/*===========================================================================
 * Auth callback
 *=========================================================================*/

static void on_auth_done(int success,
                          const char* username,
                          const char* /*avatar_id*/,
                          const char* error_msg,
                          void* /*user*/) {
    if (success) {
        snprintf(g_username, sizeof(g_username), "%s", username ? username : "");
        g_client_ready = 1;
        oheretic_log("Logged in as %s", g_username);
        show_toast("OASIS: Welcome to OHeretic, Corvus.");
        ogengine_sync_inventory_start(OHERETIC_GAME_SOURCE, on_inventory_done, nullptr);
    } else {
        oheretic_log("Auth failed: %s", error_msg ? error_msg : "unknown");
        g_client_ready = 0;
    }
}

/*===========================================================================
 * Public API
 *=========================================================================*/

extern "C" void OHeretic_STAR_Init(const char* star_api_base_url,
                                    const char* oasis_json_path) {
    if (g_initialized) return;

    ogengine_sync_init();
    snprintf(g_json_path, sizeof(g_json_path), "%s",
             oasis_json_path ? oasis_json_path : "oasisstar.json");

    ogengine_result_t rc = ogengine_init(star_api_base_url);
    if (rc != OGENGINE_SUCCESS) {
        oheretic_log("ogengine_init failed (%d): %s", (int)rc,
                     ogengine_get_last_error());
        return;
    }

    /* Try persisted session from JSON first */
    char saved_user[128] = {0};
    ogengine_result_t load_rc = ogengine_load_session(g_json_path,
                                                       saved_user, sizeof(saved_user));
    if (load_rc == OGENGINE_SUCCESS && saved_user[0]) {
        snprintf(g_username, sizeof(g_username), "%s", saved_user);
        g_client_ready = 1;
        ogengine_sync_inventory_start(OHERETIC_GAME_SOURCE, on_inventory_done, nullptr);
        oheretic_log("Session restored for %s", g_username);
    }

    g_initialized = 1;
    oheretic_log("OASIS OHeretic integration v" OHERETIC_VERSION_STR " ready");
}

extern "C" void OHeretic_STAR_Cleanup(void) {
    if (!g_initialized) return;
    ogengine_sync_cleanup();
    ogengine_shutdown();
    g_initialized = 0;
    g_client_ready = 0;
}

extern "C" void OHeretic_STAR_Tick(void) {
    if (!g_initialized) return;
    ogengine_sync_pump();

    std::lock_guard<std::mutex> lk(g_mutex);
    if (g_toast_ticks > 0) --g_toast_ticks;
}

extern "C" void OHeretic_STAR_OnItemPickup(const char* actor_class,
                                            const char* picker_name) {
    if (!g_client_ready || !actor_class) return;

    /* Check keys */
    auto kit = HERETIC_KEY_MAP.find(actor_class);
    if (kit != HERETIC_KEY_MAP.end()) {
        ogengine_queue_add_item(kit->second.c_str(), "heretic_key",
                                OHERETIC_GAME_SOURCE, picker_name, 1);
        oheretic_log("Key pickup: %s → %s", actor_class, kit->second.c_str());
        char toast[128];
        snprintf(toast, sizeof(toast), "OASIS: Key '%s' added to cross-game inventory",
                 kit->second.c_str());
        show_toast(toast);
        return;
    }

    /* Check artifacts */
    auto ait = HERETIC_ARTI_MAP.find(actor_class);
    if (ait != HERETIC_ARTI_MAP.end()) {
        ogengine_queue_add_item(ait->second.c_str(), "heretic_artifact",
                                OHERETIC_GAME_SOURCE, picker_name, 1);
        oheretic_log("Artifact pickup: %s → %s", actor_class, ait->second.c_str());
        return;
    }
}

extern "C" int OHeretic_STAR_CheckDoorAccess(const char* key_class,
                                              const char* player_name) {
    if (!g_client_ready || !key_class) return 0;

    auto kit = HERETIC_KEY_MAP.find(key_class);
    if (kit == HERETIC_KEY_MAP.end()) return 0;

    /* Check cross-game inventory for this key */
    int has = 0;
    ogengine_result_t rc = ogengine_has_item(kit->second.c_str(), &has);
    if (rc == OGENGINE_SUCCESS && has) {
        oheretic_log("Cross-game key '%s' grants door access for %s",
                     kit->second.c_str(), player_name ? player_name : "?");
        show_toast("OASIS: Cross-game key grants access!");
        return 1;
    }
    return 0;
}

extern "C" void OHeretic_STAR_OnMonsterKilled(const char* monster_class,
                                               const char* killer_name) {
    if (!g_client_ready || !monster_class) return;

    auto mit = HERETIC_MONSTER_XP.find(monster_class);
    int xp = (mit != HERETIC_MONSTER_XP.end()) ? mit->second : 5;

    ogengine_queue_add_xp(xp, OHERETIC_GAME_SOURCE, killer_name);

    std::lock_guard<std::mutex> lk(g_mutex);
    g_xp += xp;

    if (g_debug)
        oheretic_log("Killed %s → +%d XP (total: %d)", monster_class, xp, g_xp);

    /* Boss kills always show a toast */
    if (xp >= 120) {
        char toast[128];
        snprintf(toast, sizeof(toast), "OASIS: Boss slain! +%d XP", xp);
        show_toast(toast);
    }
}

extern "C" void OHeretic_STAR_DrawHUDStatus(int screen_w, int screen_h) {
    if (!g_initialized) return;

    std::lock_guard<std::mutex> lk(g_mutex);

    /* Toast message */
    if (g_toast_ticks > 0 && g_toast_msg[0]) {
        /* GZDoom screen text draw — substitute the real DrawText API */
        /* screen::DrawText(FONT_SMALL, CR_GOLD, 4, screen_h - 24, g_toast_msg); */
        (void)screen_w;
    }

    /* Inventory overlay (when toggled) */
    if (g_show_inv && g_inv_count > 0) {
        int y = 30;
        /* screen::DrawText(FONT_SMALL, CR_GREEN, 4, y, "OASIS Inventory:"); */
        for (int i = 0; i < g_inv_count && i < 8; ++i) {
            char line[128];
            snprintf(line, sizeof(line), "  %s  x%d", g_inv_names[i], g_inv_qty[i]);
            /* screen::DrawText(FONT_SMALL, CR_WHITE, 4, y + 12*(i+1), line); */
            (void)line;
        }
        (void)y;
    }

    /* XP badge in top-right */
    if (g_client_ready && g_xp > 0) {
        char xp_str[32];
        snprintf(xp_str, sizeof(xp_str), "XP: %d", g_xp);
        /* screen::DrawText(FONT_SMALL, CR_GOLD, screen_w - 80, 4, xp_str); */
        (void)xp_str;
    }
}

extern "C" int OHeretic_STAR_HandleKey(int key_code) {
    /* I key — toggle inventory overlay */
    if (key_code == 'I' || key_code == 'i') {
        std::lock_guard<std::mutex> lk(g_mutex);
        g_show_inv = !g_show_inv;
        return 1;
    }
    /* Q key — show quest status in console */
    if (key_code == 'Q' || key_code == 'q') {
        if (g_client_ready)
            oheretic_log("OHeretic STAR: XP=%d inv_items=%d", g_xp, g_inv_count);
        return 1;
    }
    return 0;
}

extern "C" int OHeretic_STAR_IsReady(void) {
    return g_client_ready;
}

/*===========================================================================
 * CCMD: "star" console command  (oheretic: star version, star inv, ...)
 *=========================================================================*/

CCMD(star_oheretic) {
    if (argv.argc() < 2) {
        Printf("star version   — show STAR API version\n"
               "star inv       — dump OASIS inventory to console\n"
               "star xp        — show current session XP\n"
               "star login <user> <pass>  — authenticate\n");
        return;
    }
    if (!stricmp(argv[1], "version")) {
        char ver[64];
        ogengine_get_version(ver, sizeof(ver));
        Printf(OHERETIC_LOG_TAG "STAR API: %s\n", ver);
    } else if (!stricmp(argv[1], "inv")) {
        std::lock_guard<std::mutex> lk(g_mutex);
        Printf(OHERETIC_LOG_TAG "Inventory (%d items):\n", g_inv_count);
        for (int i = 0; i < g_inv_count; ++i)
            Printf("  %-40s x%d\n", g_inv_names[i], g_inv_qty[i]);
    } else if (!stricmp(argv[1], "xp")) {
        Printf(OHERETIC_LOG_TAG "Session XP: %d\n", g_xp);
    } else if (!stricmp(argv[1], "login") && argv.argc() >= 4) {
        ogengine_sync_auth_start(argv[2], argv[3], on_auth_done, nullptr);
        Printf(OHERETIC_LOG_TAG "Authenticating as %s …\n", argv[2]);
    } else {
        Printf(OHERETIC_LOG_TAG "Unknown subcommand '%s'\n", argv[1]);
    }
}
