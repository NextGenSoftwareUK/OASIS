/**
 * OMorrowind - OASIS STAR API Integration Implementation
 *
 * Integrates OpenMW (Morrowind open-source reimplementation) with the OASIS
 * STAR API.  OpenMW is C++17 (GPL-3.0); this file links against OpenMW's
 * internal libraries and the OGEngineClient C ABI.
 *
 * Hook sites (see omorrowind_ogengine_integration.h for specifics):
 *   OMorrowind_STAR_Init     → apps/openmw/engine.cpp  Engine::go()
 *   OMorrowind_STAR_Cleanup  → Engine destructor
 *   OMorrowind_STAR_Tick     → Engine::frame()
 *   OMorrowind_STAR_OnItemPickup  → MWWorld::InventoryStore::add()
 *   OMorrowind_STAR_OnActorKilled → MWMechanics combat death path
 *   OMorrowind_STAR_DrawHUDStatus → MWGui::HUD::onFrame()
 *   OMorrowind_STAR_HandleKey     → MWInput::InputManager::keyPressed()
 *
 * Morrowind Lua companion (OASIS.omwaddon) calling STAR C ABI via LuaJIT FFI
 * is a planned follow-up deliverable.
 *
 * Base engine: OpenMW  https://github.com/OpenMW/openmw  (GPL-3.0)
 */

#include "omorrowind_ogengine_integration.h"
#include "ogengine.h"
#include "ogengine_sync.h"

#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <cstdarg>
#include <string>
#include <map>
#include <mutex>
#include <vector>
#include <algorithm>
#include <cctype>

/* OpenMW log/debug output */
#include <components/debug/debuglog.hpp>

/*===========================================================================
 * Constants and module state
 *=========================================================================*/

#define OMW_GAME_SOURCE  "OMORROWIND"
#define OMW_LOG_TAG      "[OMW] "
#define OMW_VERSION_STR  "OMorrowind 1.0.0"
#define OMW_MAX_PATH     512
#define OMW_TOAST_MS     3000   /* 3 s in milliseconds */
#define OMW_INV_MAX      64

static bool g_initialized   = false;
static bool g_client_ready  = false;
static bool g_debug         = true;
static char g_json_path[OMW_MAX_PATH];
static char g_username[128];
static int  g_xp            = 0;

/* Toast */
static char   g_toast_msg[256];
static double g_toast_expiry_ms = 0.0;

/* Inventory overlay */
struct InvEntry { char name[64]; int qty; };
static InvEntry g_inventory[OMW_INV_MAX];
static int      g_inv_count  = 0;
static bool     g_show_inv   = false;

/* Elapsed time from engine (updated each frame via Tick) */
static double g_now_ms = 0.0;

static std::mutex g_mutex;

/*===========================================================================
 * Morrowind creature XP table  (OpenMW ref-ID → XP)
 *=========================================================================*/

static const std::map<std::string,int> MW_CREATURE_XP = {
    { "mudcrab",              5  }, { "kwamaqueen",         25 },
    { "kwama_warrior",       15  }, { "kwamaworker",        10 },
    { "scrib",                5  }, { "guar",               10 },
    { "nix-hound",           15  }, { "shalk",              25 },
    { "scamp",               20  }, { "clannfear",          50 },
    { "daedroth",            80  }, { "dremora",            60 },
    { "dremoralord",        100  }, { "ogrim",              80 },
    { "ogrimbeast",         120  }, { "wingedtwilight",     70 },
    { "storm_atronach",      90  }, { "fire_atronach",      70 },
    { "frost_atronach",      70  }, { "golden_saint",       150 },
    { "hungerblood",         80  }, { "ancestor_ghost",     30 },
    { "bonewalker",          40  }, { "bonelordlord",       80 },
    { "centurionspiderdwemer",80  }, { "centurion_steam",  100 },
    { "ash_zombie",          30  }, { "ash_ghoul",          60 },
    { "ash_vampire",        200  }, { "corprus_stalker",    35 },
    { "dagoth_ur_1",        500  },
};

static int mw_creature_xp(const std::string& ref_id) {
    std::string key = ref_id;
    std::transform(key.begin(), key.end(), key.begin(),
                   [](unsigned char c){ return (char)std::tolower(c); });
    auto it = MW_CREATURE_XP.find(key);
    return (it != MW_CREATURE_XP.end()) ? it->second : 10;
}

/*===========================================================================
 * Morrowind notable item list (items worth reporting to STAR)
 * OpenMW uses string ref-IDs like "misc_skeleton_key", "daedric_crescent_unique"
 *=========================================================================*/

static const std::map<std::string,std::string> MW_KEY_ITEMS = {
    { "misc_skeleton_key",          "mw_skeleton_key"     },
    { "key_hlaalu_council",         "mw_key_council"      },
    { "key_sunder",                 "mw_sunder"           },
    { "key_keening",                "mw_keening"          },
    { "key_wraithguard",            "mw_wraithguard"      },
};

static const std::map<std::string,std::pair<std::string,std::string>> MW_NOTABLE_ITEMS = {
    { "the_ring_of_phynaster",   { "mw_ring_phynaster",   "ring"    } },
    { "amulet_of_judge_vaut",    { "mw_amulet_vaut",      "amulet"  } },
    { "daedric_crescent_unique", { "mw_daedric_crescent", "weapon"  } },
    { "staff_of_magnus_unique",  { "mw_staff_magnus",     "staff"   } },
    { "ebony_mail_unique",       { "mw_ebony_mail",       "armor"   } },
    { "hircine_ring_unique",     { "mw_hircine_ring",     "ring"    } },
    { "sanguine_rose_unique",    { "mw_sanguine_rose",    "wand"    } },
};

/*===========================================================================
 * Logging
 *=========================================================================*/

static void omw_log(const char* fmt, ...) {
    if (!g_debug) return;
    char buf[512];
    va_list ap; va_start(ap, fmt);
    vsnprintf(buf, sizeof(buf), fmt, ap);
    va_end(ap);
    Log(Debug::Info) << OMW_LOG_TAG << buf;
}

static void show_toast(const char* msg) {
    std::lock_guard<std::mutex> lk(g_mutex);
    snprintf(g_toast_msg, sizeof(g_toast_msg), "%s", msg);
    g_toast_expiry_ms = g_now_ms + OMW_TOAST_MS;
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
    int n = std::min(list->count, OMW_INV_MAX);
    for (int i = 0; i < n; ++i) {
        snprintf(g_inventory[i].name, 64, "%s", list->items[i].name);
        g_inventory[i].qty = list->items[i].quantity;
    }
    g_inv_count = n;
}

static void on_auth_done(int success, const char* username,
                          const char* /*avatar_id*/, const char* error_msg,
                          void* /*user*/) {
    if (success) {
        snprintf(g_username, sizeof(g_username), "%s", username ? username : "");
        g_client_ready = true;
        omw_log("Logged in as %s", g_username);
        show_toast("OASIS: Welcome to OMorrowind, traveller.");
        ogengine_sync_inventory_start(OMW_GAME_SOURCE, on_inventory_done, nullptr);
    } else {
        omw_log("Auth failed: %s", error_msg ? error_msg : "unknown");
        g_client_ready = false;
    }
}

/*===========================================================================
 * Public API
 *=========================================================================*/

extern "C" void OMorrowind_STAR_Init(const char* star_api_base_url,
                                      const char* oasis_json_path) {
    if (g_initialized) return;

    ogengine_sync_init();
    snprintf(g_json_path, sizeof(g_json_path), "%s",
             oasis_json_path ? oasis_json_path : "oasisstar.json");

    ogengine_result_t rc = ogengine_init(star_api_base_url);
    if (rc != OGENGINE_SUCCESS) {
        omw_log("ogengine_init failed (%d): %s", (int)rc,
                ogengine_get_last_error());
        return;
    }

    char saved_user[128] = {0};
    if (ogengine_load_session(g_json_path, saved_user, sizeof(saved_user))
            == OGENGINE_SUCCESS && saved_user[0]) {
        snprintf(g_username, sizeof(g_username), "%s", saved_user);
        g_client_ready = true;
        ogengine_sync_inventory_start(OMW_GAME_SOURCE, on_inventory_done, nullptr);
        omw_log("Session restored for %s", g_username);
    }

    g_initialized = true;
    omw_log("OASIS OMorrowind integration v" OMW_VERSION_STR " ready");
}

extern "C" void OMorrowind_STAR_Cleanup(void) {
    if (!g_initialized) return;
    ogengine_sync_cleanup();
    ogengine_shutdown();
    g_initialized = false;
    g_client_ready = false;
}

extern "C" void OMorrowind_STAR_Tick(void) {
    if (!g_initialized) return;
    ogengine_sync_pump();
    /* g_now_ms is updated by the engine caller before this function is called
       so toast expiry checks are correct within the same frame. */
}

extern "C" void OMorrowind_STAR_OnItemPickup(const char* item_id,
                                              const char* item_name,
                                              int quantity) {
    if (!g_client_ready || !item_id) return;

    std::string id_lower = item_id;
    std::transform(id_lower.begin(), id_lower.end(), id_lower.begin(),
                   [](unsigned char c){ return (char)std::tolower(c); });

    /* Unique key items */
    auto kit = MW_KEY_ITEMS.find(id_lower);
    if (kit != MW_KEY_ITEMS.end()) {
        ogengine_queue_add_item(kit->second.c_str(), "mw_key_item",
                                OMW_GAME_SOURCE, g_username, quantity);
        char toast[128];
        snprintf(toast, sizeof(toast), "OASIS: Key item '%s' added to cross-game inventory",
                 item_name ? item_name : item_id);
        show_toast(toast);
        omw_log("Key item: %s → %s", item_id, kit->second.c_str());
        return;
    }

    /* Notable unique items */
    auto nit = MW_NOTABLE_ITEMS.find(id_lower);
    if (nit != MW_NOTABLE_ITEMS.end()) {
        ogengine_queue_add_item(nit->second.first.c_str(),
                                nit->second.second.c_str(),
                                OMW_GAME_SOURCE, g_username, quantity);
        char toast[128];
        snprintf(toast, sizeof(toast), "OASIS: Unique item '%s' recorded!",
                 item_name ? item_name : item_id);
        show_toast(toast);
        omw_log("Notable item: %s", item_id);
        return;
    }
}

extern "C" void OMorrowind_STAR_OnActorKilled(const char* actor_id,
                                               const char* actor_name,
                                               const char* killer) {
    if (!g_client_ready || !actor_id) return;
    int xp = mw_creature_xp(actor_id);
    ogengine_queue_add_xp(xp, OMW_GAME_SOURCE,
                          (killer && killer[0]) ? killer : g_username);
    { std::lock_guard<std::mutex> lk(g_mutex); g_xp += xp; }
    if (g_debug)
        omw_log("Killed %s → +%d XP (total=%d)",
                actor_name ? actor_name : actor_id, xp, g_xp);
    if (xp >= 150) {
        char toast[128];
        snprintf(toast, sizeof(toast), "OASIS: Daedra slain! +%d XP", xp);
        show_toast(toast);
    }
}

extern "C" void OMorrowind_STAR_DrawHUDStatus(int screen_w, int screen_h) {
    if (!g_initialized) return;
    std::lock_guard<std::mutex> lk(g_mutex);

    /* OpenMW uses MyGUI for the HUD.  Draw calls here should be replaced with
       MyGUI::Gui::getInstance().createWidget<...> calls or an overlay widget.
       Stubs ensure the function signature is correct for integration. */

    bool toast_active = (g_now_ms < g_toast_expiry_ms && g_toast_msg[0]);
    (void)toast_active;
    (void)screen_w; (void)screen_h;

    /* TODO: replace with MyGUI overlay text widget */
}

extern "C" int OMorrowind_STAR_HandleKey(int sdl_scancode) {
    /* SDL_SCANCODE_I = 12 */
    if (sdl_scancode == 12) {
        std::lock_guard<std::mutex> lk(g_mutex);
        g_show_inv = !g_show_inv;
        return 1;
    }
    /* SDL_SCANCODE_Q = 20 */
    if (sdl_scancode == 20) {
        if (g_client_ready)
            omw_log("OASIS: XP=%d inv=%d user=%s", g_xp, g_inv_count, g_username);
        return 1;
    }
    return 0;
}

extern "C" int OMorrowind_STAR_IsReady(void) { return g_client_ready ? 1 : 0; }
