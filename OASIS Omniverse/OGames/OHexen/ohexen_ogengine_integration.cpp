/**
 * OHexen - OASIS STAR API Integration Implementation
 *
 * Integrates GZDoom (Hexen IWAD) with the OASIS STAR API.  Hexen runs on
 * the same GZDoom codebase as OHeretic/ODOOM so the hook sites are identical;
 * the key differences are:
 *   - Three player classes (Fighter, Cleric, Mage) reported as STAR attribute
 *   - Hub-based level system: XP and inventory persist across hub levels
 *   - Different artifact and puzzle-key sets
 *   - Class-specific weapons (reported as item pickups)
 *
 * Base engine: GZDoom  https://github.com/ZDoom/gzdoom  (GPL-3.0)
 */

#include "ohexen_ogengine_integration.h"
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

#define OHEXEN_GAME_SOURCE  "OHEXEN"
#define OHEXEN_LOG_TAG      "[HEXEN] "
#define OHEXEN_VERSION_STR  "OHexen 1.0.0"
#define OHEXEN_MAX_PATH     512
#define OHEXEN_TOAST_TICKS  180
#define OHEXEN_INV_MAX      32
#define OHEXEN_QUEST_MAX    16

static int  g_initialized   = 0;
static int  g_client_ready  = 0;
static int  g_debug         = 1;
static char g_json_path[OHEXEN_MAX_PATH];
static char g_username[128];
static char g_player_class[32] = "fighter";   /* default */
static int  g_xp            = 0;

/* Toast */
static char g_toast_msg[256];
static int  g_toast_ticks   = 0;

/* Inventory overlay */
static char g_inv_names[OHEXEN_INV_MAX][64];
static int  g_inv_qty[OHEXEN_INV_MAX];
static int  g_inv_count     = 0;
static int  g_show_inv      = 0;

static std::mutex g_mutex;

/*===========================================================================
 * Hexen item mappings
 *=========================================================================*/

/* Physical keys (puzzle items counted as "keys" for cross-game purposes) */
static const std::map<std::string,std::string> HEXEN_KEY_MAP = {
    { "KeyAxe",          "key_axe"       },
    { "KeyFire",         "key_fire"      },
    { "KeyGreen",        "key_green"     },
    { "KeyMace",         "key_mace"      },
    { "KeySilver",       "key_silver"    },
    { "KeyCastle",       "key_castle"    },
    { "KeyHorn",         "key_horn"      },
    { "KeyRustedKey",    "key_rusted"    },
    { "KeyDungeonKey",   "key_dungeon"   },
    { "KeySwampKey",     "key_swamp"     },
    /* Puzzle items — stored as key-items in STAR */
    { "KeyPuzzSkull",    "puzzle_skull"     },
    { "KeyPuzzFire",     "puzzle_fire"      },
    { "KeyPuzzGiantsKnife","puzzle_knife"   },
    { "KeyPuzzWidow",    "puzzle_widow"     },
    { "KeyPuzzClock",    "puzzle_clock"     },
    { "KeyPuzzBook",     "puzzle_book"      },
    { "KeyPuzzSkull2",   "puzzle_skull2"    },
    { "KeyPuzzFWeapon",  "puzzle_fweapon"   },
    { "KeyPuzzCWeapon",  "puzzle_cweapon"   },
    { "KeyPuzzMWeapon",  "puzzle_mweapon"   },
    { "KeyPuzzGear1",    "puzzle_gear1"     },
    { "KeyPuzzGear2",    "puzzle_gear2"     },
    { "KeyPuzzGear3",    "puzzle_gear3"     },
    { "KeyPuzzGear4",    "puzzle_gear4"     },
};

/* Artifacts */
static const std::map<std::string,std::string> HEXEN_ARTI_MAP = {
    { "ArtiHealth",          "hexen_crystal_vial"       },
    { "ArtiSuperHealth",     "hexen_mystic_urn"         },
    { "ArtiPork",            "hexen_porkalator"         },
    { "ArtiInvulnerability", "hexen_icon_defender"      },
    { "ArtiDisk",            "hexen_disc_repulsion"     },
    { "ArtiTeleport",        "hexen_chaos_device"       },
    { "ArtiTorch",           "hexen_torch"              },
    { "ArtiHealingRadius",   "hexen_mystic_ambit"       },
    { "ArtiSummon",          "hexen_dark_servant"       },
    { "ArtiFlechette",       "hexen_flechette"          },
    { "ArtiBoostMana",       "hexen_krater_might"       },
    { "ArtiBoostArmor",      "hexen_dragonskin_bracers" },
    { "ArtiBlastRadius",     "hexen_banishment_device"  },
};

/* Monster XP table */
static const std::map<std::string,int> HEXEN_MONSTER_XP = {
    { "Centaur",          30 }, { "CentaurLeader",    40 },
    { "Slaughtaur",       30 }, { "SlaughtaurLeader", 40 },
    { "Ettin",            25 },
    { "FireDemon",        35 },
    { "Wendigo",          40 },
    { "Stalker",          20 }, { "StalkerBoss",      50 },
    { "SorcererBall",     10 },
    { "Korax",           400 },
    { "Heresiarch",      350 },
    { "Bishop",          120 },
    { "Reiver",           20 }, { "ReiverBoss",       60 },
    { "DragonSkinFlyer",  25 },
};

/*===========================================================================
 * Logging and toast
 *=========================================================================*/

static void ohexen_log(const char* fmt, ...) {
    if (!g_debug) return;
    char buf[512];
    va_list ap; va_start(ap, fmt);
    vsnprintf(buf, sizeof(buf), fmt, ap);
    va_end(ap);
    Printf("%s%s\n", OHEXEN_LOG_TAG, buf);
}

static void show_toast(const char* msg) {
    std::lock_guard<std::mutex> lk(g_mutex);
    snprintf(g_toast_msg, sizeof(g_toast_msg), "%s", msg);
    g_toast_ticks = OHEXEN_TOAST_TICKS;
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
    int n = list->count < OHEXEN_INV_MAX ? list->count : OHEXEN_INV_MAX;
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
        ohexen_log("Logged in as %s (class: %s)", g_username, g_player_class);
        /* Report class as a STAR attribute */
        char attr_val[64];
        snprintf(attr_val, sizeof(attr_val), "hexen_%s", g_player_class);
        ogengine_set_avatar_attribute("player_class", attr_val);
        show_toast("OASIS: Welcome to OHexen.");
        ogengine_sync_inventory_start(OHEXEN_GAME_SOURCE, on_inventory_done, nullptr);
    } else {
        ohexen_log("Auth failed: %s", error_msg ? error_msg : "unknown");
        g_client_ready = 0;
    }
}

/*===========================================================================
 * Public API
 *=========================================================================*/

extern "C" void OHexen_STAR_Init(const char* star_api_base_url,
                                  const char* oasis_json_path) {
    if (g_initialized) return;

    ogengine_sync_init();
    snprintf(g_json_path, sizeof(g_json_path), "%s",
             oasis_json_path ? oasis_json_path : "oasisstar.json");

    ogengine_result_t rc = ogengine_init(star_api_base_url);
    if (rc != OGENGINE_SUCCESS) {
        ohexen_log("ogengine_init failed (%d)", (int)rc);
        return;
    }

    char saved_user[128] = {0};
    if (ogengine_load_session(g_json_path, saved_user, sizeof(saved_user))
            == OGENGINE_SUCCESS && saved_user[0]) {
        snprintf(g_username, sizeof(g_username), "%s", saved_user);
        g_client_ready = 1;
        ogengine_sync_inventory_start(OHEXEN_GAME_SOURCE, on_inventory_done, nullptr);
        ohexen_log("Session restored for %s", g_username);
    }

    g_initialized = 1;
    ohexen_log("OASIS OHexen integration v" OHEXEN_VERSION_STR " ready");
}

extern "C" void OHexen_STAR_Cleanup(void) {
    if (!g_initialized) return;
    ogengine_sync_cleanup();
    ogengine_shutdown();
    g_initialized = 0;
    g_client_ready = 0;
}

extern "C" void OHexen_STAR_Tick(void) {
    if (!g_initialized) return;
    ogengine_sync_pump();
    std::lock_guard<std::mutex> lk(g_mutex);
    if (g_toast_ticks > 0) --g_toast_ticks;
}

extern "C" void OHexen_STAR_OnClassSelected(const char* class_name,
                                              const char* /*player_name*/) {
    if (!class_name) return;
    snprintf(g_player_class, sizeof(g_player_class), "%s", class_name);
    if (g_client_ready) {
        char attr_val[64];
        snprintf(attr_val, sizeof(attr_val), "hexen_%s", g_player_class);
        ogengine_set_avatar_attribute("player_class", attr_val);
        ohexen_log("Player class set to %s", class_name);
    }
}

extern "C" void OHexen_STAR_OnItemPickup(const char* actor_class,
                                          const char* picker_name) {
    if (!g_client_ready || !actor_class) return;

    auto kit = HEXEN_KEY_MAP.find(actor_class);
    if (kit != HEXEN_KEY_MAP.end()) {
        ogengine_queue_add_item(kit->second.c_str(), "hexen_key",
                                OHEXEN_GAME_SOURCE, picker_name, 1);
        char toast[128];
        snprintf(toast, sizeof(toast), "OASIS: Key '%s' added to cross-game inventory",
                 kit->second.c_str());
        show_toast(toast);
        return;
    }

    auto ait = HEXEN_ARTI_MAP.find(actor_class);
    if (ait != HEXEN_ARTI_MAP.end()) {
        ogengine_queue_add_item(ait->second.c_str(), "hexen_artifact",
                                OHEXEN_GAME_SOURCE, picker_name, 1);
        return;
    }
}

extern "C" int OHexen_STAR_CheckDoorAccess(const char* key_class,
                                             const char* player_name) {
    if (!g_client_ready || !key_class) return 0;
    auto kit = HEXEN_KEY_MAP.find(key_class);
    if (kit == HEXEN_KEY_MAP.end()) return 0;
    int has = 0;
    if (ogengine_has_item(kit->second.c_str(), &has) == OGENGINE_SUCCESS && has) {
        ohexen_log("Cross-game key '%s' grants access for %s",
                   kit->second.c_str(), player_name ? player_name : "?");
        show_toast("OASIS: Cross-game key grants access!");
        return 1;
    }
    return 0;
}

extern "C" void OHexen_STAR_OnMonsterKilled(const char* monster_class,
                                              const char* killer_name) {
    if (!g_client_ready || !monster_class) return;
    auto mit = HEXEN_MONSTER_XP.find(monster_class);
    int xp = (mit != HEXEN_MONSTER_XP.end()) ? mit->second : 5;
    ogengine_queue_add_xp(xp, OHEXEN_GAME_SOURCE, killer_name);
    { std::lock_guard<std::mutex> lk(g_mutex); g_xp += xp; }
    if (g_debug)
        ohexen_log("Killed %s → +%d XP", monster_class, xp);
    if (xp >= 120) {
        char toast[128];
        snprintf(toast, sizeof(toast), "OASIS: Boss slain! +%d XP", xp);
        show_toast(toast);
    }
}

extern "C" void OHexen_STAR_DrawHUDStatus(int screen_w, int screen_h) {
    if (!g_initialized) return;
    std::lock_guard<std::mutex> lk(g_mutex);
    /* Toast */
    if (g_toast_ticks > 0 && g_toast_msg[0]) {
        /* screen::DrawText(FONT_SMALL, CR_GOLD, 4, screen_h - 24, g_toast_msg); */
        (void)screen_h;
    }
    /* Inventory */
    if (g_show_inv && g_inv_count > 0) {
        for (int i = 0; i < g_inv_count && i < 8; ++i) {
            char line[128];
            snprintf(line, sizeof(line), "  %s x%d", g_inv_names[i], g_inv_qty[i]);
            (void)line;
        }
    }
    /* XP / class badge */
    if (g_client_ready) {
        char badge[64];
        snprintf(badge, sizeof(badge), "[%s] XP:%d", g_player_class, g_xp);
        /* screen::DrawText(FONT_SMALL, CR_GOLD, screen_w - 110, 4, badge); */
        (void)badge; (void)screen_w;
    }
}

extern "C" int OHexen_STAR_HandleKey(int key_code) {
    if (key_code == 'I' || key_code == 'i') {
        std::lock_guard<std::mutex> lk(g_mutex);
        g_show_inv = !g_show_inv;
        return 1;
    }
    if (key_code == 'Q' || key_code == 'q') {
        if (g_client_ready)
            ohexen_log("OHexen STAR: class=%s XP=%d inv=%d", g_player_class,
                       g_xp, g_inv_count);
        return 1;
    }
    return 0;
}

extern "C" int OHexen_STAR_IsReady(void) { return g_client_ready; }

/*===========================================================================
 * CCMD: "star_ohexen" console command
 *=========================================================================*/

CCMD(star_ohexen) {
    if (argv.argc() < 2) {
        Printf("star_ohexen version | inv | xp | class | login <u> <p>\n");
        return;
    }
    if (!stricmp(argv[1], "version")) {
        char ver[64]; ogengine_get_version(ver, sizeof(ver));
        Printf(OHEXEN_LOG_TAG "STAR API: %s\n", ver);
    } else if (!stricmp(argv[1], "inv")) {
        std::lock_guard<std::mutex> lk(g_mutex);
        Printf(OHEXEN_LOG_TAG "Inventory (%d items):\n", g_inv_count);
        for (int i = 0; i < g_inv_count; ++i)
            Printf("  %-40s x%d\n", g_inv_names[i], g_inv_qty[i]);
    } else if (!stricmp(argv[1], "xp")) {
        Printf(OHEXEN_LOG_TAG "XP: %d  Class: %s\n", g_xp, g_player_class);
    } else if (!stricmp(argv[1], "class")) {
        Printf(OHEXEN_LOG_TAG "Current class: %s\n", g_player_class);
    } else if (!stricmp(argv[1], "login") && argv.argc() >= 4) {
        ogengine_sync_auth_start(argv[2], argv[3], on_auth_done, nullptr);
        Printf(OHEXEN_LOG_TAG "Authenticating as %s …\n", argv[2]);
    } else {
        Printf(OHEXEN_LOG_TAG "Unknown subcommand '%s'\n", argv[1]);
    }
}
