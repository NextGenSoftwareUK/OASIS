/**
 * OMorrowind - OASIS STAR API integration for OpenMW.
 *
 * Follows the ODOOM/OQuake integration pattern:
 *   - oasisstar.json holds URLs, the saved session (JWT/refresh) and the
 *     offline sync (edge) settings; it is re-saved after beam-in and edge changes.
 *   - Offline sync is configured before ogengine_init, polled every frame, and
 *     driven through OMorrowind_STAR_OfflineSyncCommand.
 *   - Async work (auth, inventory) runs through ogengine_sync_* and completes on
 *     the main thread inside OMorrowind_STAR_Tick.
 *
 * Engine hooks (already wired in OpenMW):
 *   engine.cpp                   Init / Tick / Cleanup
 *   mwworld/inventorystore.cpp   OnItemPickup
 *   mwmechanics/actors.cpp       OnActorKilled
 */

#include "omorrowind_ogengine_integration.h"
#include "ogengine.h"
#include "ogengine_sync.h"
#include "oglib_json.h"
#include "oglib_edge.h"

#include <algorithm>
#include <cctype>
#include <cstdarg>
#include <cstdio>
#include <cstring>
#include <map>
#include <mutex>
#include <string>

#include <components/debug/debuglog.hpp>

/*===========================================================================
 * Constants and module state
 *=========================================================================*/

#define OMW_GAME_SOURCE "OMORROWIND"
#define OMW_LOG_TAG "[OMW] "
#define OMW_VERSION_STR "OMorrowind 1.1.0"
#define OMW_MAX_PATH 512
#define OMW_TOAST_FRAMES 180
#define OMW_INV_MAX 64
#define OMW_DEFAULT_OASIS_URL "https://api.oasisweb4.com/api"
#define OMW_DEFAULT_PROVIDER "SolanaOASIS"

static bool g_initialized = false;
static bool g_client_ready = false;
static bool g_debug = true;
static char g_json_path[OMW_MAX_PATH] = "oasisstar.json";
static char g_ogengine_url[512] = "";
static char g_oasis_api_url[512] = OMW_DEFAULT_OASIS_URL;
static char g_nft_provider[64] = OMW_DEFAULT_PROVIDER;
static char g_username[128] = "";
static char g_saved_jwt[2048] = "";
static char g_saved_refresh[2048] = "";
static int g_xp = 0;

/* Offline sync (edge) settings, persisted in oasisstar.json - same as ODOOM/OQuake. */
static oglib_edge_settings_t g_edge_settings = OGLIB_EDGE_SETTINGS_DEFAULT;

/* Toast */
static char g_toast_msg[256] = "";
static int g_toast_frames = 0;

/* Inventory overlay */
struct InvEntry {
    char name[64];
    int qty;
};
static InvEntry g_inventory[OMW_INV_MAX];
static int g_inv_count = 0;
static bool g_show_inv = false;

static std::mutex g_mutex;

/*===========================================================================
 * Morrowind creature XP table (OpenMW ref-ID -> XP). 150+ XP counts as a boss.
 *=========================================================================*/

static const std::map<std::string, int> MW_CREATURE_XP = {
    { "mudcrab", 5 }, { "kwamaqueen", 25 }, { "kwama_warrior", 15 }, { "kwamaworker", 10 },
    { "scrib", 5 }, { "guar", 10 }, { "nix-hound", 15 }, { "shalk", 25 },
    { "scamp", 20 }, { "clannfear", 50 }, { "daedroth", 80 }, { "dremora", 60 },
    { "dremoralord", 100 }, { "ogrim", 80 }, { "ogrimbeast", 120 }, { "wingedtwilight", 70 },
    { "storm_atronach", 90 }, { "fire_atronach", 70 }, { "frost_atronach", 70 }, { "golden_saint", 150 },
    { "hungerblood", 80 }, { "ancestor_ghost", 30 }, { "bonewalker", 40 }, { "bonelordlord", 80 },
    { "centurionspiderdwemer", 80 }, { "centurion_steam", 100 }, { "ash_zombie", 30 }, { "ash_ghoul", 60 },
    { "ash_vampire", 200 }, { "corprus_stalker", 35 }, { "dagoth_ur_1", 500 },
};

/* Unique key items worth syncing to the cross-game inventory. */
static const std::map<std::string, std::string> MW_KEY_ITEMS = {
    { "misc_skeleton_key", "Skeleton Key" },
    { "key_hlaalu_council", "Hlaalu Council Key" },
    { "key_sunder", "Sunder" },
    { "key_keening", "Keening" },
    { "key_wraithguard", "Wraithguard" },
};

/* Notable unique artifacts: ref-ID -> (display name, item type). */
static const std::map<std::string, std::pair<std::string, std::string>> MW_NOTABLE_ITEMS = {
    { "the_ring_of_phynaster", { "Ring of Phynaster", "Ring" } },
    { "amulet_of_judge_vaut", { "Amulet of Judge Vaut", "Amulet" } },
    { "daedric_crescent_unique", { "Daedric Crescent", "Weapon" } },
    { "staff_of_magnus_unique", { "Staff of Magnus", "Weapon" } },
    { "ebony_mail_unique", { "Ebony Mail", "Armor" } },
    { "hircine_ring_unique", { "Hircine's Ring", "Ring" } },
    { "sanguine_rose_unique", { "Sanguine Rose", "Item" } },
};

static std::string to_lower(const char* s)
{
    std::string out = s ? s : "";
    std::transform(out.begin(), out.end(), out.begin(), [](unsigned char c) { return (char)std::tolower(c); });
    return out;
}

/*===========================================================================
 * Logging and toast
 *=========================================================================*/

static void omw_log(const char* fmt, ...)
{
    char buf[1024];
    va_list ap;
    va_start(ap, fmt);
    vsnprintf(buf, sizeof(buf), fmt, ap);
    va_end(ap);
    Log(Debug::Info) << OMW_LOG_TAG << buf;
}

static void omw_debug(const char* fmt, ...)
{
    if (!g_debug)
        return;
    char buf[1024];
    va_list ap;
    va_start(ap, fmt);
    vsnprintf(buf, sizeof(buf), fmt, ap);
    va_end(ap);
    Log(Debug::Info) << OMW_LOG_TAG << buf;
}

static void show_toast(const char* msg)
{
    std::lock_guard<std::mutex> lk(g_mutex);
    snprintf(g_toast_msg, sizeof(g_toast_msg), "%s", msg ? msg : "");
    g_toast_frames = OMW_TOAST_FRAMES;
}

/*===========================================================================
 * oasisstar.json
 *=========================================================================*/

static void json_write_escaped(FILE* f, const char* key, const char* value, bool last)
{
    fprintf(f, "  \"%s\": \"", key);
    for (const char* p = value ? value : ""; *p; ++p) {
        if (*p == '"' || *p == '\\')
            fputc('\\', f);
        fputc(*p, f);
    }
    fprintf(f, last ? "\"\n" : "\",\n");
}

static void LoadJsonConfig(const char* path)
{
    FILE* f = fopen(path, "rb");
    if (!f)
        return;
    fseek(f, 0, SEEK_END);
    long size = ftell(f);
    fseek(f, 0, SEEK_SET);
    if (size <= 0 || size > 1024 * 1024) {
        fclose(f);
        return;
    }
    std::string json((size_t)size, '\0');
    size_t n = fread(&json[0], 1, (size_t)size, f);
    fclose(f);
    json.resize(n);

    oglib_edge_load_json(&g_edge_settings, json.c_str());

    char val[256];
    if (oglib_json_extract(json.c_str(), "ogengine_url", val, sizeof(val)) && val[0])
        snprintf(g_ogengine_url, sizeof(g_ogengine_url), "%s", val);
    if (oglib_json_extract(json.c_str(), "oasis_api_url", val, sizeof(val)) && val[0])
        snprintf(g_oasis_api_url, sizeof(g_oasis_api_url), "%s", val);
    if (oglib_json_extract(json.c_str(), "nft_provider", val, sizeof(val)) && val[0])
        snprintf(g_nft_provider, sizeof(g_nft_provider), "%s", val);
    if (oglib_json_extract(json.c_str(), "star_debug", val, sizeof(val)) && val[0])
        g_debug = atoi(val) != 0;
    if (oglib_json_extract(json.c_str(), "beamedin_avatar", val, sizeof(val)) && val[0])
        snprintf(g_username, sizeof(g_username), "%s", val);
    oglib_json_extract(json.c_str(), "jwt_token", g_saved_jwt, sizeof(g_saved_jwt));
    oglib_json_extract(json.c_str(), "refresh_token", g_saved_refresh, sizeof(g_saved_refresh));
    omw_debug("Loaded %s", path);
}

static void SaveJsonConfig(void)
{
    char jwt[2048] = "";
    char refresh[2048] = "";
    char user[128] = "";
    if (g_initialized && g_client_ready && !ogengine_is_session_expired()) {
        ogengine_get_current_jwt(jwt, sizeof(jwt));
        ogengine_get_current_refresh_token(refresh, sizeof(refresh));
        ogengine_get_current_username(user, sizeof(user));
    }
    if (!user[0])
        snprintf(user, sizeof(user), "%s", g_username);

    FILE* f = fopen(g_json_path, "w");
    if (!f) {
        omw_log("Could not write %s", g_json_path);
        return;
    }
    fprintf(f, "{\n");
    oglib_edge_save_json(f, &g_edge_settings);
    fprintf(f, "  \"star_debug\": %d,\n", g_debug ? 1 : 0);
    json_write_escaped(f, "ogengine_url", g_ogengine_url, false);
    json_write_escaped(f, "oasis_api_url", g_oasis_api_url, false);
    json_write_escaped(f, "nft_provider", g_nft_provider, false);
    json_write_escaped(f, "beamedin_avatar", user, false);
    json_write_escaped(f, "jwt_token", jwt, false);
    json_write_escaped(f, "refresh_token", refresh, true);
    fprintf(f, "}\n");
    fclose(f);
}

/*===========================================================================
 * Async completions (run on the main thread inside ogengine_sync_pump)
 *=========================================================================*/

static void OnInventoryDone(void* /*user*/)
{
    ogengine_item_list_t* list = nullptr;
    ogengine_result_t result = OGENGINE_SUCCESS;
    char err[256] = "";
    if (!ogengine_sync_inventory_get_result(&list, &result, err, sizeof(err)))
        return;
    {
        std::lock_guard<std::mutex> lk(g_mutex);
        g_inv_count = 0;
        if (result == OGENGINE_SUCCESS && list) {
            int n = (int)std::min<size_t>(list->count, OMW_INV_MAX);
            for (int i = 0; i < n; ++i) {
                snprintf(g_inventory[i].name, sizeof(g_inventory[i].name), "%s", list->items[i].name);
                g_inventory[i].qty = list->items[i].quantity;
            }
            g_inv_count = n;
        }
    }
    if (result != OGENGINE_SUCCESS)
        omw_debug("Inventory sync failed: %s", err[0] ? err : "unknown");
    ogengine_sync_inventory_clear_result();
}

static void RefreshInventory(void)
{
    if (g_client_ready && !ogengine_sync_inventory_in_progress())
        ogengine_sync_inventory_start(nullptr, 0, OMW_GAME_SOURCE, OnInventoryDone, nullptr);
}

static void OnAuthDone(void* /*user*/)
{
    int success = 0;
    char username[128] = "";
    char avatar_id[128] = "";
    char err[256] = "";
    ogengine_sync_auth_get_result(&success, username, sizeof(username), avatar_id, sizeof(avatar_id), err, sizeof(err));
    if (success) {
        snprintf(g_username, sizeof(g_username), "%s", username);
        g_client_ready = true;
        omw_log("Beamed in as %s", g_username);
        show_toast("OASIS: Welcome to OMorrowind, traveller.");
        SaveJsonConfig();
        RefreshInventory();
    } else {
        g_client_ready = false;
        omw_log("Beam-in failed: %s", err[0] ? err : ogengine_get_last_error());
        show_toast("OASIS: Beam-in failed.");
    }
}

/*===========================================================================
 * Public API
 *=========================================================================*/

extern "C" void OMorrowind_STAR_Init(const char* star_api_base_url, const char* oasis_json_path)
{
    if (g_initialized)
        return;

    if (oasis_json_path && oasis_json_path[0])
        snprintf(g_json_path, sizeof(g_json_path), "%s", oasis_json_path);
    if (star_api_base_url && star_api_base_url[0])
        snprintf(g_ogengine_url, sizeof(g_ogengine_url), "%s", star_api_base_url);
    LoadJsonConfig(g_json_path);

    ogengine_set_debug(g_debug ? 1 : 0);
    ogengine_sync_init();

    if (oglib_edge_configure(&g_edge_settings) != OGENGINE_SUCCESS)
        omw_log("Offline sync settings rejected: %s", ogengine_get_last_error());

    ogengine_config_t cfg = {};
    cfg.base_url = g_ogengine_url;
    cfg.client_game_source = OMW_GAME_SOURCE;
    cfg.transport = 0;
    cfg.timeout_seconds = 15;
    if (ogengine_init(&cfg) != OGENGINE_SUCCESS) {
        omw_log("ogengine_init failed: %s - STAR features disabled.", ogengine_get_last_error());
        ogengine_sync_cleanup();
        return;
    }
    ogengine_set_oasis_base_url(g_oasis_api_url);
    g_initialized = true;

    /* Restore the saved session (same as ODOOM/OQuake). */
    if (g_saved_jwt[0]) {
        ogengine_set_saved_session(g_saved_jwt);
        if (g_saved_refresh[0])
            ogengine_set_refresh_token(g_saved_refresh);
        if (ogengine_restore_session() == OGENGINE_SUCCESS) {
            ogengine_get_current_username(g_username, sizeof(g_username));
            g_client_ready = true;
            omw_log("Session restored for %s", g_username);
            RefreshInventory();
        } else {
            omw_log("Saved session could not be restored - beam in again.");
        }
    }

    omw_log("OASIS integration v" OMW_VERSION_STR " ready");
}

extern "C" void OMorrowind_STAR_Cleanup(void)
{
    if (!g_initialized)
        return;
    ogengine_flush_add_item_jobs();
    ogengine_flush_use_item_jobs();
    SaveJsonConfig();
    ogengine_sync_cleanup();
    ogengine_cleanup();
    g_initialized = false;
    g_client_ready = false;
}

extern "C" void OMorrowind_STAR_Tick(void)
{
    if (!g_initialized)
        return;

    ogengine_sync_pump();

    {
        char edge_msg[512];
        int changed = oglib_edge_finish_change(&g_edge_settings, edge_msg, sizeof(edge_msg));
        if (changed != 0) {
            if (changed == 1)
                SaveJsonConfig();
            omw_log("%s", edge_msg);
            show_toast(edge_msg);
        }
        if (g_client_ready && !ogengine_sync_auth_in_progress()) {
            char note[256];
            if (ogengine_poll_edge_notification(note, sizeof(note)) == 1) {
                omw_log("%s", note);
                show_toast(note);
            }
        }
    }

    char buf[512];
    while (ogengine_consume_console_log(buf, sizeof(buf)))
        omw_debug("%s", buf);
    if (ogengine_consume_last_background_error(buf, sizeof(buf)))
        omw_log("Background error: %s", buf);
    {
        char item[256], nft_id[128], hash[128];
        if (ogengine_consume_last_mint_result(item, sizeof(item), nft_id, sizeof(nft_id), hash, sizeof(hash)))
            omw_log("NFT minted: %s (id %s)", item, nft_id);
    }

    std::lock_guard<std::mutex> lk(g_mutex);
    if (g_toast_frames > 0)
        --g_toast_frames;
}

extern "C" void OMorrowind_STAR_OnItemPickup(const char* item_id, const char* item_name, int quantity)
{
    if (!g_client_ready || !item_id)
        return;
    const std::string id = to_lower(item_id);
    const int qty = quantity > 0 ? quantity : 1;

    auto kit = MW_KEY_ITEMS.find(id);
    if (kit != MW_KEY_ITEMS.end()) {
        const std::string desc = "Key item from OMorrowind (" + id + ")";
        ogengine_queue_add_item(kit->second.c_str(), desc.c_str(), OMW_GAME_SOURCE, "KeyItem", nullptr, qty, 1);
        char toast[192];
        snprintf(toast, sizeof(toast), "OASIS: %s added to cross-game inventory", kit->second.c_str());
        show_toast(toast);
        omw_debug("Key item: %s -> %s", item_id, kit->second.c_str());
        return;
    }

    auto nit = MW_NOTABLE_ITEMS.find(id);
    if (nit != MW_NOTABLE_ITEMS.end()) {
        const std::string desc = "Unique artifact from OMorrowind (" + id + ")";
        ogengine_queue_add_item(
            nit->second.first.c_str(), desc.c_str(), OMW_GAME_SOURCE, nit->second.second.c_str(), nullptr, qty, 1);
        char toast[192];
        snprintf(toast, sizeof(toast), "OASIS: Unique item '%s' recorded!", item_name ? item_name : nit->second.first.c_str());
        show_toast(toast);
        omw_debug("Notable item: %s", item_id);
    }
}

extern "C" void OMorrowind_STAR_OnActorKilled(const char* actor_id, const char* actor_name, const char* /*killer*/)
{
    if (!g_client_ready || !actor_id)
        return;
    auto it = MW_CREATURE_XP.find(to_lower(actor_id));
    const int xp = it != MW_CREATURE_XP.end() ? it->second : 10;
    const int is_boss = xp >= 150 ? 1 : 0;
    const char* name = (actor_name && actor_name[0]) ? actor_name : actor_id;

    /* XP, quest progress and (for bosses) minting all run on the client's background thread. */
    ogengine_queue_monster_kill(actor_id, name, xp, is_boss, is_boss, g_nft_provider, OMW_GAME_SOURCE);
    {
        std::lock_guard<std::mutex> lk(g_mutex);
        g_xp += xp;
    }
    omw_debug("Killed %s -> +%d XP (session total %d)", name, xp, g_xp);
    if (is_boss) {
        char toast[128];
        snprintf(toast, sizeof(toast), "OASIS: %s slain! +%d XP", name, xp);
        show_toast(toast);
    }
}

extern "C" void OMorrowind_STAR_DrawHUDStatus(int screen_w, int screen_h)
{
    /* OpenMW draws its HUD with MyGUI; the toast/inventory state above is ready for a
       MyGUI overlay widget. Until that widget exists, status goes to the log. */
    (void)screen_w;
    (void)screen_h;
}

extern "C" int OMorrowind_STAR_HandleKey(int sdl_scancode)
{
    if (sdl_scancode == 12) { /* SDL_SCANCODE_I */
        std::lock_guard<std::mutex> lk(g_mutex);
        g_show_inv = !g_show_inv;
        return 1;
    }
    if (sdl_scancode == 20) { /* SDL_SCANCODE_Q */
        omw_log("OASIS: user=%s ready=%d XP(session)=%d inventory=%d", g_username, g_client_ready ? 1 : 0, g_xp,
            g_inv_count);
        RefreshInventory();
        return 1;
    }
    return 0;
}

extern "C" int OMorrowind_STAR_IsReady(void)
{
    return g_client_ready ? 1 : 0;
}

extern "C" void OMorrowind_STAR_BeamIn(const char* username, const char* password)
{
    if (!g_initialized || !username || !password)
        return;
    if (ogengine_sync_auth_in_progress()) {
        omw_log("Beam-in already in progress.");
        return;
    }
    omw_log("Beaming in as %s...", username);
    ogengine_sync_auth_start(username, password, OnAuthDone, nullptr);
}

extern "C" void OMorrowind_STAR_BeamOut(void)
{
    if (!g_initialized)
        return;
    g_client_ready = false;
    g_username[0] = '\0';
    g_saved_jwt[0] = '\0';
    g_saved_refresh[0] = '\0';
    SaveJsonConfig();
    /* Same as ODOOM3: drop the client; the next Init (or game start) begins logged out. */
    ogengine_sync_cleanup();
    ogengine_cleanup();
    g_initialized = false;
    omw_log("Beamed out.");
    show_toast("OASIS: Beamed out.");
}

extern "C" int OMorrowind_STAR_OfflineSyncMode(void)
{
    int capabilities = ogengine_get_edge_capabilities();
    return !(capabilities & 1) ? -1 : (capabilities & 2) ? 1 : 0;
}

extern "C" void OMorrowind_STAR_OfflineSyncCommand(const char* command)
{
    char message[512];
    oglib_edge_command(command, message, sizeof(message));
    omw_log("%s", message);
    show_toast(message);
}
