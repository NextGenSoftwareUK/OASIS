/**
 * ODOOM3-BFG - OASIS STAR API Integration Implementation
 *
 * Integrates RBDOOM-3-BFG (id Tech 4) with the OASIS STAR API.
 * Keycards collected in any OASIS game can open Doom 3 doors and vice versa.
 * Monster kills award XP and optional NFT mints. Cross-game quests supported.
 *
 * Integration points in neo/d3xp/:
 *   idGameLocal::Init()            → D3Doom_STAR_Init()
 *   idGameLocal::Shutdown()        → D3Doom_STAR_Cleanup()
 *   idGameLocal::RunFrame()        → D3Doom_STAR_Tick()
 *   idPlayer::GiveInventoryItem()  → D3Doom_STAR_OnItemPickup()
 *   idGameLocal::RequirementMet()  → D3Doom_STAR_CheckDoorAccess()
 *   idAI::Killed()                 → D3Doom_STAR_OnMonsterKilled()
 *
 * BUILD: COPY_TO_RBDOOM3_AND_BUILD.ps1 copies this file plus headers and OGLib/
 * to C:\Source\ODOOM3-BFG\neo\d3xp\ before CMake runs.
 */

#include "precompiled.h"
#pragma hdrstop

/* d3xp integration API */
#include "d3doom_star_integration.h"

/* STAR API (copies arrive via COPY_TO_RBDOOM3_AND_BUILD.ps1) */
#include "star_api.h"
#include "star_sync.h"

#ifdef _MSC_VER
#  ifndef NOMINMAX
#    define NOMINMAX
#  endif
#  define strcasecmp  _stricmp
#  define strncasecmp _strnicmp
#endif

/* OGLib: session forwarders + JSON-driven monster table (single-TU impl) */
#define OGLIB_SESSION_IMPL
#define OGLIB_MONSTER_IMPL
#include "OGLib/oglib.h"

/*=============================================================================
 * Global state
 *===========================================================================*/

#define D3DOOM_GAME_SOURCE    "ODOOM3BFG"
#define D3DOOM_LOG_TAG        "[STAR] "
#define D3DOOM_MAX_PATH       512
#define D3DOOM_JSON_BUF_SIZE  8192

static bool g_d3doom_initialized = false;
static bool g_d3doom_client_ready = false;
static bool g_d3doom_debug = true;
static char g_d3doom_json_path[D3DOOM_MAX_PATH] = {};
static char g_d3doom_saved_username[128] = {};
static char g_d3doom_saved_jwt[2048] = {};
static char g_d3doom_saved_refresh_token[2048] = {};

/* Monster table — loaded from oasisstar.json "odoom3bfg".monsters; defaults below. */
static oglib_monster_table_t g_d3doom_monster_table = {};

/* Default hardcoded entries; JSON in oasisstar.json takes priority per-entry. */
static const struct { const char* engine_name; const char* display_name; int xp; int is_boss; int do_mint; }
k_D3DoomDefaultMonsters[] = {
    { "monster_zombie",               "Zombie",            20,    0, 0 },
    { "monster_demon_imp",            "Imp",               25,    0, 0 },
    { "monster_demon_imp_crawler",    "Imp Crawler",       20,    0, 0 },
    { "monster_demon_trite",          "Trite",             10,    0, 0 },
    { "monster_demon_tick",           "Tick",              10,    0, 0 },
    { "monster_demon_cherub",         "Cherub",            15,    0, 0 },
    { "monster_demon_pinky",          "Pinky",             40,    0, 0 },
    { "monster_demon_pinky_pipes",    "Pinky (Pipes)",     40,    0, 0 },
    { "monster_demon_maggot",         "Maggot",            20,    0, 0 },
    { "monster_demon_wraith",         "Wraith",            50,    0, 0 },
    { "monster_demon_vulgar",         "Vulgar",            45,    0, 0 },
    { "monster_demon_sentry",         "Sentry",            30,    0, 0 },
    { "monster_demon_hellknight",     "Hell Knight",      100,    0, 0 },
    { "monster_demon_revenant",       "Revenant",          60,    0, 0 },
    { "monster_demon_mancubus",       "Mancubus",         100,    0, 0 },
    { "monster_demon_archvile",       "Archvile",         150,    0, 0 },
    { "monster_boss_vagary",          "Vagary",           400,    1, 1 },
    { "monster_boss_guardian",        "Guardian",         600,    1, 1 },
    { "monster_boss_guardian2",       "Guardian (Final)", 800,    1, 1 },
    { "monster_boss_sabaoth",         "Sabaoth",          500,    1, 1 },
    { "monster_boss_cyberdemon",      "Cyberdemon",      1000,    1, 1 },
    { "monster_boss_d3xp_maledict",   "Maledict",        1200,    1, 1 },
    { nullptr, nullptr, 0, 0, 0 }
};

/* Config CVars — read at Init, used throughout session. */
static idCVar d3doom_star_api_url(
    "d3doom_star_api_url", "https://star-api.oasisplatform.world/api",
    CVAR_GAME|CVAR_ARCHIVE, "OASIS STAR API base URL");
static idCVar d3doom_star_oasis_url(
    "d3doom_star_oasis_url", "https://api.oasisplatform.world",
    CVAR_GAME|CVAR_ARCHIVE, "OASIS WEB4 API base URL");
static idCVar d3doom_star_nft_provider(
    "d3doom_star_nft_provider", "SolanaOASIS",
    CVAR_GAME|CVAR_ARCHIVE, "NFT provider for minting (SolanaOASIS, etc.)");
static idCVar d3doom_star_mint_keys(
    "d3doom_star_mint_keys", "0",
    CVAR_GAME|CVAR_ARCHIVE, "1 = mint NFT on key pickup");
static idCVar d3doom_star_mint_monsters(
    "d3doom_star_mint_monsters", "1",
    CVAR_GAME|CVAR_ARCHIVE, "1 = mint NFT on boss kill (respects do_mint in monster table)");
static idCVar d3doom_star_debug(
    "d3doom_star_debug", "1",
    CVAR_GAME|CVAR_ARCHIVE, "1 = verbose STAR logging to console and star_api.log");
static idCVar d3doom_star_consume_key_on_door(
    "d3doom_star_consume_key_on_door", "1",
    CVAR_GAME|CVAR_ARCHIVE, "1 = remove STAR key when used to open a door");

/*=============================================================================
 * Logging helpers
 *===========================================================================*/

static void StarLog(const char* fmt, ...) {
    char buf[1024];
    va_list ap;
    va_start(ap, fmt);
    vsnprintf(buf, sizeof(buf), fmt, ap);
    va_end(ap);
    buf[sizeof(buf) - 1] = '\0';
    common->Printf(D3DOOM_LOG_TAG "%s\n", buf);
    star_api_log_to_file(buf);
}

static void StarLogDebug(const char* fmt, ...) {
    if (!g_d3doom_debug) return;
    char buf[1024];
    va_list ap;
    va_start(ap, fmt);
    vsnprintf(buf, sizeof(buf), fmt, ap);
    va_end(ap);
    buf[sizeof(buf) - 1] = '\0';
    common->Printf(D3DOOM_LOG_TAG "%s\n", buf);
    star_api_log_to_file(buf);
}

/*=============================================================================
 * oasisstar.json config
 *===========================================================================*/

static bool D3Doom_FindJsonPath(char* out, int out_max) {
    /* Try cwd and exe directory */
    static const char* kCandidates[] = {
        "oasisstar.json",
        "build/oasisstar.json",
        nullptr
    };
    for (int i = 0; kCandidates[i]; ++i) {
        FILE* f = fopen(kCandidates[i], "r");
        if (f) { fclose(f); snprintf(out, out_max, "%s", kCandidates[i]); return true; }
    }
#ifdef _WIN32
    char exe[MAX_PATH] = {};
    if (GetModuleFileNameA(nullptr, exe, sizeof(exe))) {
        char* slash = strrchr(exe, '\\');
        if (slash) {
            *slash = '\0';
            snprintf(out, out_max, "%s\\oasisstar.json", exe);
            FILE* f = fopen(out, "r");
            if (f) { fclose(f); return true; }
        }
    }
#endif
    return false;
}

static void D3Doom_LoadJson(const char* path) {
    FILE* f = fopen(path, "r");
    if (!f) return;
    char buf[D3DOOM_JSON_BUF_SIZE] = {};
    size_t n = fread(buf, 1, sizeof(buf) - 1, f);
    fclose(f);
    if (!n) return;
    buf[n] = '\0';

    char val[256];
    if (oglib_json_extract(buf, "star_api_url", val, sizeof(val)) && val[0])
        d3doom_star_api_url.SetString(val);
    if (oglib_json_extract(buf, "oasis_api_url", val, sizeof(val)) && val[0])
        d3doom_star_oasis_url.SetString(val);
    if (oglib_json_extract(buf, "nft_provider", val, sizeof(val)) && val[0])
        d3doom_star_nft_provider.SetString(val);
    if (oglib_json_extract(buf, "mint_keys", val, sizeof(val)) && val[0])
        d3doom_star_mint_keys.SetInteger(atoi(val));
    if (oglib_json_extract(buf, "mint_monsters", val, sizeof(val)) && val[0])
        d3doom_star_mint_monsters.SetInteger(atoi(val));
    if (oglib_json_extract(buf, "star_debug", val, sizeof(val)) && val[0])
        d3doom_star_debug.SetInteger(atoi(val));
    if (oglib_json_extract(buf, "consume_key_on_door", val, sizeof(val)) && val[0])
        d3doom_star_consume_key_on_door.SetInteger(atoi(val));

    /* Session restore */
    char jwt_buf[2048] = {};
    if (oglib_json_extract(buf, "beamedin_avatar", val, sizeof(val)) && val[0])
        snprintf(g_d3doom_saved_username, sizeof(g_d3doom_saved_username), "%s", val);
    if ((oglib_json_extract(buf, "jwt_token", jwt_buf, sizeof(jwt_buf))) && jwt_buf[0])
        snprintf(g_d3doom_saved_jwt, sizeof(g_d3doom_saved_jwt), "%s", jwt_buf);
    if (oglib_json_extract(buf, "refresh_token", jwt_buf, sizeof(jwt_buf)) && jwt_buf[0])
        snprintf(g_d3doom_saved_refresh_token, sizeof(g_d3doom_saved_refresh_token), "%s", jwt_buf);

    /* Monster table — JSON entries take priority over hardcoded defaults */
    oglib_monster_table_load_from_oasisstar(&g_d3doom_monster_table, buf, "odoom3bfg");

    StarLogDebug("Loaded oasisstar.json from %s", path);
}

static void D3Doom_SaveJson(const char* path) {
    if (!path || !path[0]) return;
    FILE* f = fopen(path, "w");
    if (!f) return;

    char uname[128] = {};
    char jwt[2048] = {};
    char refresh[2048] = {};
    if (g_d3doom_initialized) {
        star_api_get_current_username(uname, sizeof(uname));
        if (!star_api_is_session_expired()) {
            star_api_get_current_jwt(jwt, sizeof(jwt));
            star_api_get_current_refresh_token(refresh, sizeof(refresh));
        }
    }
    if (!uname[0] && g_d3doom_saved_username[0])
        snprintf(uname, sizeof(uname), "%s", g_d3doom_saved_username);

    fprintf(f, "{\n");
    fprintf(f, "  \"star_api_url\": \"%s\",\n",   d3doom_star_api_url.GetString());
    fprintf(f, "  \"oasis_api_url\": \"%s\",\n",  d3doom_star_oasis_url.GetString());
    fprintf(f, "  \"nft_provider\": \"%s\",\n",   d3doom_star_nft_provider.GetString());
    fprintf(f, "  \"mint_keys\": %d,\n",           d3doom_star_mint_keys.GetInteger());
    fprintf(f, "  \"mint_monsters\": %d,\n",       d3doom_star_mint_monsters.GetInteger());
    fprintf(f, "  \"star_debug\": %d,\n",          d3doom_star_debug.GetInteger());
    fprintf(f, "  \"consume_key_on_door\": %d",    d3doom_star_consume_key_on_door.GetInteger());
    if (uname[0] || jwt[0]) {
        fprintf(f, ",\n");
        if (uname[0]) {
            fprintf(f, "  \"beamedin_avatar\": \"");
            for (const char* p = uname; *p; p++) { if (*p == '"' || *p == '\\') fputc('\\', f); fputc(*p, f); }
            fprintf(f, "\"");
        }
        if (jwt[0]) {
            fprintf(f, ",\n  \"jwt_token\": \"");
            for (const char* p = jwt; *p; p++) { if (*p == '"' || *p == '\\') fputc('\\', f); fputc(*p, f); }
            fprintf(f, "\"");
        }
        if (refresh[0]) {
            fprintf(f, ",\n  \"refresh_token\": \"");
            for (const char* p = refresh; *p; p++) { if (*p == '"' || *p == '\\') fputc('\\', f); fputc(*p, f); }
            fprintf(f, "\"");
        }
    }
    fprintf(f, "\n}\n");
    fclose(f);
}

/*=============================================================================
 * Auth callback
 *===========================================================================*/

static void D3Doom_OnAuthDone(void* user_data) {
    int success = 0;
    char username[128] = {}, avatar_id[128] = {}, err[256] = {};
    star_sync_auth_get_result(&success, username, sizeof(username), avatar_id, sizeof(avatar_id), err, sizeof(err));
    if (success) {
        char jwt_buf[2048] = {};
        star_sync_auth_get_result_jwt(jwt_buf, sizeof(jwt_buf));
        snprintf(g_d3doom_saved_username, sizeof(g_d3doom_saved_username), "%s", username);
        if (jwt_buf[0]) snprintf(g_d3doom_saved_jwt, sizeof(g_d3doom_saved_jwt), "%s", jwt_buf);
        g_d3doom_client_ready = true;
        star_api_refresh_avatar_profile();
        StarLog("Beamed in as %s", username);
        if (g_d3doom_json_path[0]) D3Doom_SaveJson(g_d3doom_json_path);
    } else {
        StarLog("Beam-in failed: %s", err);
    }
}

static void D3Doom_OnOperationCallback(star_api_result_t result, int op_type, void* user_data) {
    if (op_type == STAR_API_OP_PROFILE_LOADED && result == STAR_API_SUCCESS) {
        g_d3doom_client_ready = true;
        int xp = 0;
        star_api_get_avatar_xp(&xp);
        StarLog("Profile loaded. Avatar XP: %d", xp);
    }
}

/*=============================================================================
 * Monster table initialization
 *===========================================================================*/

static void D3Doom_InitMonsterTable(void) {
    g_d3doom_monster_table.count = 0;
    /* Populate hardcoded defaults first */
    for (int i = 0; k_D3DoomDefaultMonsters[i].engine_name; ++i) {
        if (g_d3doom_monster_table.count >= OGLIB_MONSTER_TABLE_MAX) break;
        oglib_monster_entry_t* e = &g_d3doom_monster_table.entries[g_d3doom_monster_table.count++];
        snprintf(e->engine_name,  sizeof(e->engine_name),  "%s", k_D3DoomDefaultMonsters[i].engine_name);
        snprintf(e->display_name, sizeof(e->display_name), "%s", k_D3DoomDefaultMonsters[i].display_name);
        e->xp      = k_D3DoomDefaultMonsters[i].xp;
        e->is_boss = k_D3DoomDefaultMonsters[i].is_boss;
        e->do_mint = k_D3DoomDefaultMonsters[i].do_mint;
    }
    /* JSON from oasisstar.json overrides entries with matching engine_name (loaded in D3Doom_LoadJson) */
}

/*=============================================================================
 * Console command: "star ..."
 *===========================================================================*/

static void D3Doom_STAR_CmdHandler(const idCmdArgs& args) {
    if (args.Argc() < 2) {
        common->Printf("Usage: star <command> [args]\n"
                       "  version             Show STAR API version\n"
                       "  status              Show init/auth status\n"
                       "  beamin <user> <pw>  Authenticate with OASIS STAR\n"
                       "  beamout             Log out of OASIS STAR\n"
                       "  inventory           List STAR inventory items\n"
                       "  add <name>          Add item to STAR inventory (testing)\n"
                       "  debug <on|off>      Toggle debug logging\n");
        return;
    }
    const char* subcmd = args.Argv(1);

    if (!strcasecmp(subcmd, "version")) {
        common->Printf(D3DOOM_LOG_TAG "ODOOM3-BFG STAR Integration v1.0 (game_source=" D3DOOM_GAME_SOURCE ")\n");
        return;
    }

    if (!strcasecmp(subcmd, "status")) {
        common->Printf(D3DOOM_LOG_TAG "initialized=%d client_ready=%d debug=%d\n",
            g_d3doom_initialized ? 1 : 0, g_d3doom_client_ready ? 1 : 0, g_d3doom_debug ? 1 : 0);
        if (g_d3doom_saved_username[0])
            common->Printf(D3DOOM_LOG_TAG "saved avatar: %s\n", g_d3doom_saved_username);
        return;
    }

    if (!strcasecmp(subcmd, "debug")) {
        if (args.Argc() >= 3) {
            int on = !strcasecmp(args.Argv(2), "on") || atoi(args.Argv(2));
            g_d3doom_debug = (on != 0);
            d3doom_star_debug.SetInteger(on);
            star_api_set_debug(on);
        }
        common->Printf(D3DOOM_LOG_TAG "debug=%s\n", g_d3doom_debug ? "on" : "off");
        return;
    }

    if (!strcasecmp(subcmd, "beamin")) {
        if (args.Argc() < 4) {
            common->Printf(D3DOOM_LOG_TAG "Usage: star beamin <username> <password>\n");
            return;
        }
        if (!g_d3doom_initialized) {
            common->Printf(D3DOOM_LOG_TAG "STAR not initialized; check star_api.dll is present.\n");
            return;
        }
        common->Printf(D3DOOM_LOG_TAG "Authenticating...\n");
        star_sync_auth_start(args.Argv(2), args.Argv(3), D3Doom_OnAuthDone, nullptr);
        return;
    }

    if (!strcasecmp(subcmd, "beamout")) {
        g_d3doom_client_ready = false;
        g_d3doom_saved_username[0] = '\0';
        g_d3doom_saved_jwt[0] = '\0';
        g_d3doom_saved_refresh_token[0] = '\0';
        if (g_d3doom_json_path[0]) D3Doom_SaveJson(g_d3doom_json_path);
        star_api_cleanup();
        g_d3doom_initialized = false;
        common->Printf(D3DOOM_LOG_TAG "Beamed out.\n");
        return;
    }

    if (!strcasecmp(subcmd, "inventory")) {
        if (!g_d3doom_client_ready) { common->Printf(D3DOOM_LOG_TAG "Not beamed in.\n"); return; }
        star_item_list_t* list = nullptr;
        if (star_api_get_inventory(&list) != STAR_API_SUCCESS || !list) {
            common->Printf(D3DOOM_LOG_TAG "Could not fetch inventory.\n");
            return;
        }
        common->Printf(D3DOOM_LOG_TAG "Inventory (%d items):\n", (int)list->count);
        for (size_t i = 0; i < list->count; ++i)
            common->Printf("  [%d] %s  (type: %s  src: %s  qty: %d)\n",
                (int)i + 1, list->items[i].name, list->items[i].item_type,
                list->items[i].game_source, list->items[i].quantity);
        star_api_free_item_list(list);
        return;
    }

    if (!strcasecmp(subcmd, "add")) {
        if (args.Argc() < 3) { common->Printf(D3DOOM_LOG_TAG "Usage: star add <item_name>\n"); return; }
        if (!g_d3doom_client_ready) { common->Printf(D3DOOM_LOG_TAG "Not beamed in.\n"); return; }
        star_api_queue_add_item(args.Argv(2), "Added via console", D3DOOM_GAME_SOURCE, "Item", nullptr, 1, 1);
        common->Printf(D3DOOM_LOG_TAG "Queued add: %s\n", args.Argv(2));
        return;
    }

    common->Printf(D3DOOM_LOG_TAG "Unknown command '%s'. Type 'star' for help.\n", subcmd);
}

/*=============================================================================
 * Public API implementation
 *===========================================================================*/

void D3Doom_STAR_Init(void) {
    if (g_d3doom_initialized) return;

    g_d3doom_debug = (d3doom_star_debug.GetInteger() != 0);
    StarLog("ODOOM3-BFG: Initialising STAR integration...");

    /* Populate hardcoded monster table defaults */
    D3Doom_InitMonsterTable();

    /* Load oasisstar.json (may override monster table + config + saved session) */
    if (D3Doom_FindJsonPath(g_d3doom_json_path, sizeof(g_d3doom_json_path))) {
        D3Doom_LoadJson(g_d3doom_json_path);
    } else {
        /* Create a default one next to the exe */
#ifdef _WIN32
        char exe[MAX_PATH] = {};
        if (GetModuleFileNameA(nullptr, exe, sizeof(exe))) {
            char* slash = strrchr(exe, '\\');
            if (slash) { *slash = '\0'; snprintf(g_d3doom_json_path, sizeof(g_d3doom_json_path), "%s\\oasisstar.json", exe); }
        }
        if (!g_d3doom_json_path[0])
#endif
            snprintf(g_d3doom_json_path, sizeof(g_d3doom_json_path), "oasisstar.json");
        D3Doom_SaveJson(g_d3doom_json_path);
        StarLog("Created default oasisstar.json: %s", g_d3doom_json_path);
    }

    g_d3doom_debug = (d3doom_star_debug.GetInteger() != 0);
    star_api_set_debug(g_d3doom_debug ? 1 : 0);

    /* Initialise STAR API client */
    star_api_config_t cfg = {};
    cfg.base_url          = d3doom_star_api_url.GetString();
    cfg.client_game_source = D3DOOM_GAME_SOURCE;
    cfg.transport         = 0;  /* remote HTTP */
    cfg.timeout_seconds   = 15;

    star_sync_init();

    star_api_result_t r = star_api_init(&cfg);
    if (r != STAR_API_SUCCESS) {
        StarLog("star_api_init failed (%d) — STAR features disabled.", (int)r);
        return;
    }

    star_api_set_operation_callback(D3Doom_OnOperationCallback, nullptr);
    star_api_set_oasis_base_url(d3doom_star_oasis_url.GetString());
    g_d3doom_initialized = true;

    /* Restore saved session if we have a JWT */
    if (g_d3doom_saved_jwt[0]) {
        star_api_set_saved_session(g_d3doom_saved_jwt);
        if (g_d3doom_saved_refresh_token[0])
            star_api_set_refresh_token(g_d3doom_saved_refresh_token);
        star_api_restore_session();
        StarLog("Restoring session for %s...", g_d3doom_saved_username[0] ? g_d3doom_saved_username : "(unknown)");
    }

    /* Register in-game console command */
    cmdSystem->AddCommand("star", D3Doom_STAR_CmdHandler, CMD_FL_GAME, "OASIS STAR API commands");

    StarLog("STAR integration ready. Type 'star beamin <user> <pass>' to authenticate.");
}

void D3Doom_STAR_Cleanup(void) {
    if (!g_d3doom_initialized) return;

    /* Flush any pending queued jobs before shutdown */
    star_api_flush_add_item_jobs(  );
    star_api_flush_use_item_jobs();

    /* Save config and session tokens */
    if (g_d3doom_json_path[0])
        D3Doom_SaveJson(g_d3doom_json_path);

    star_sync_cleanup();
    star_api_cleanup();

    g_d3doom_initialized  = false;
    g_d3doom_client_ready = false;
    StarLog("STAR integration shut down.");
}

void D3Doom_STAR_Tick(void) {
    if (!g_d3doom_initialized) return;

    /* Drive async auth/inventory/use-item completions on main thread */
    star_sync_pump();

    /* Drain C# console messages into engine console */
    char logbuf[512];
    while (star_api_consume_console_log(logbuf, sizeof(logbuf)))
        common->Printf(D3DOOM_LOG_TAG "%s\n", logbuf);

    /* Report any background mint results */
    char mint_item[256], nft_id[128], hash[128];
    if (star_api_consume_last_mint_result(mint_item, sizeof(mint_item), nft_id, sizeof(nft_id), hash, sizeof(hash)))
        StarLog("NFT minted: %s  id=%s  hash=%s", mint_item, nft_id, hash);

    /* Report any background errors */
    char errbuf[256];
    if (star_api_consume_last_background_error(errbuf, sizeof(errbuf)))
        StarLog("Background error: %s", errbuf);
}

void D3Doom_STAR_OnItemPickup(const char* inv_name, const char* inv_classname, int is_carry_item) {
    if (!g_d3doom_initialized || !g_d3doom_client_ready) return;
    if (!inv_name || !inv_name[0]) return;

    /* Classify item type for STAR */
    const char* item_type = is_carry_item ? "Key" : "Item";

    /* Build description */
    char desc[256];
    if (inv_classname && inv_classname[0])
        snprintf(desc, sizeof(desc), "Doom 3 BFG %s (%s)", item_type, inv_classname);
    else
        snprintf(desc, sizeof(desc), "Doom 3 BFG %s", item_type);

    int do_mint = is_carry_item ? d3doom_star_mint_keys.GetInteger() : 0;

    if (do_mint) {
        star_api_queue_pickup_with_mint(
            inv_name, desc, D3DOOM_GAME_SOURCE, item_type,
            do_mint, d3doom_star_nft_provider.GetString(), nullptr, 1);
    } else {
        star_api_queue_add_item(
            inv_name, desc, D3DOOM_GAME_SOURCE, item_type, nullptr, 1, 1);
    }

    /* Advance quest objectives for carrying items (keys, PDAs) */
    if (is_carry_item)
        star_api_queue_quest_progress_from_pickup(D3DOOM_GAME_SOURCE, item_type, inv_name);

    StarLogDebug("Item pickup: name='%s' type=%s mint=%d", inv_name, item_type, do_mint);
}

int D3Doom_STAR_CheckDoorAccess(const char* required_inv_name) {
    if (!g_d3doom_initialized || !g_d3doom_client_ready) return 0;
    if (!required_inv_name || !required_inv_name[0]) return 0;

    if (!star_api_has_item(required_inv_name)) return 0;

    StarLog("Cross-game door: using STAR '%s'", required_inv_name);

    if (d3doom_star_consume_key_on_door.GetInteger())
        star_api_use_item(required_inv_name, "door");

    return 1;
}

void D3Doom_STAR_OnMonsterKilled(const char* entity_def_name, int engine_is_boss) {
    if (!g_d3doom_initialized || !g_d3doom_client_ready) return;
    if (!entity_def_name || !entity_def_name[0]) return;

    const oglib_monster_entry_t* entry = oglib_monster_find(&g_d3doom_monster_table, entity_def_name);
    if (!entry) {
        /* Unknown monster — skip to avoid spamming STAR with unnamed entities */
        return;
    }

    int is_boss  = entry->is_boss | engine_is_boss;
    int do_mint  = entry->do_mint & d3doom_star_mint_monsters.GetInteger();

    star_api_queue_monster_kill(
        entity_def_name,
        entry->display_name,
        entry->xp,
        is_boss,
        do_mint,
        do_mint ? d3doom_star_nft_provider.GetString() : nullptr,
        D3DOOM_GAME_SOURCE);

    StarLogDebug("Monster kill: %s (%s) xp=%d boss=%d mint=%d",
        entity_def_name, entry->display_name, entry->xp, is_boss, do_mint);
}

/*=============================================================================
 * HUD / GUI — state and popup controls
 *
 * New state appended here; existing global block is in the top section.
 *===========================================================================*/

#define D3DOOM_INV_MAX     32
#define D3DOOM_QUEST_MAX   16
#define D3DOOM_TOAST_FRAMES 180   /* ~3 s at 60 fps */

static bool g_d3doom_inv_popup_open   = false;
static bool g_d3doom_quest_popup_open = false;
static int  g_d3doom_inv_selected     = 0;
static int  g_d3doom_quest_selected   = 0;
static int  g_d3doom_inv_count        = 0;
static int  g_d3doom_quest_count      = 0;
static int  g_d3doom_xp               = 0;
static int  g_d3doom_toast_frames     = 0;
static char g_d3doom_toast_msg[128];
static char g_d3doom_inv_names[D3DOOM_INV_MAX][64];
static char g_d3doom_quest_names[D3DOOM_QUEST_MAX][64];
static char g_d3doom_quest_descs[D3DOOM_QUEST_MAX][128];

int         D3Doom_STAR_IsBeamedIn(void)          { return g_d3doom_client_ready ? 1 : 0; }
const char* D3Doom_STAR_GetUsername(void)          { return g_d3doom_saved_username; }
int         D3Doom_STAR_GetXP(void)               { return g_d3doom_xp; }
const char* D3Doom_STAR_GetVersionString(void)     { return "ODOOM3-BFG 1.0.0"; }
int         D3Doom_STAR_IsInventoryPopupOpen(void) { return g_d3doom_inv_popup_open ? 1 : 0; }
int         D3Doom_STAR_IsQuestPopupOpen(void)     { return g_d3doom_quest_popup_open ? 1 : 0; }
int         D3Doom_STAR_ShouldBlockInput(void)     { return (g_d3doom_inv_popup_open || g_d3doom_quest_popup_open) ? 1 : 0; }
int         D3Doom_STAR_ShouldUseAvatarFace(void)  { return g_d3doom_client_ready ? 1 : 0; }

void D3Doom_STAR_ToggleInventoryPopup(void) {
    g_d3doom_inv_popup_open = !g_d3doom_inv_popup_open;
    if (g_d3doom_inv_popup_open) {
        g_d3doom_quest_popup_open = false;
        g_d3doom_inv_selected = 0;
        g_d3doom_inv_count = 0;   /* TODO: call star_api_get_inventory */
    }
}

void D3Doom_STAR_ToggleQuestPopup(void) {
    g_d3doom_quest_popup_open = !g_d3doom_quest_popup_open;
    if (g_d3doom_quest_popup_open) {
        g_d3doom_inv_popup_open = false;
        g_d3doom_quest_selected = 0;
        g_d3doom_quest_count = 0; /* TODO: call star_api_get_quests */
    }
}

/*=============================================================================
 * HUD / GUI — drawing
 *
 * Doom 3 BFG (RBDOOM-3-BFG) uses idRenderSystem for 2-D drawing.
 * Virtual screen: 640 × 480 logical pixels.
 *
 * Key idRenderSystem draw calls:
 *   rs->DrawSmallString(x, y, str, color, forceColor)
 *   rs->DrawBigString(x, y, str, color, forceColor)
 *   rs->DrawFill(x, y, w, h, color)
 *   rs->GetWidth() / rs->GetHeight()  — physical resolution
 *
 * Call D3Doom_STAR_DrawHUDStatus from idGameLocal::Draw() after the HUD GUI
 * has been rendered.  Pass ::renderSystem or NULL.
 *===========================================================================*/

/* Doom 3 virtual screen constants */
#define D3D_VW 640
#define D3D_VH 480
#define D3D_SH 8    /* small char height */
#define D3D_SW 8    /* small char width  */

void D3Doom_STAR_DrawHUDStatus(void* render_system) {
    idRenderSystem* rs = render_system ? static_cast<idRenderSystem*>(render_system) : ::renderSystem;
    if (!rs || !g_d3doom_initialized) return;

    /* Tick toast timer */
    if (g_d3doom_toast_frames > 0) g_d3doom_toast_frames--;

    /* Version — bottom-right */
    const char* ver = "ODOOM3-BFG 1.0.0";
    int vlen = (int)idStr::Length(ver);
    rs->DrawSmallString(D3D_VW - vlen * D3D_SW - 4,
                        D3D_VH - D3D_SH - 4, ver, colorDkGrey, false);

    if (!g_d3doom_client_ready) return;

    /* Beamed-in label — top-left */
    char buf[96];
    idStr::snPrintf(buf, sizeof(buf), "OASIS: %s", g_d3doom_saved_username);
    rs->DrawSmallString(4, 4, buf, colorWhite, false);

    /* XP — top-right */
    char xpbuf[32];
    idStr::snPrintf(xpbuf, sizeof(xpbuf), "XP: %d", g_d3doom_xp);
    int xplen = (int)idStr::Length(xpbuf);
    rs->DrawSmallString(D3D_VW - xplen * D3D_SW - 4, 4, xpbuf, colorYellow, false);

    /* Toast — centred, fades */
    if (g_d3doom_toast_frames > 0) {
        int tlen = (int)idStr::Length(g_d3doom_toast_msg);
        int tx   = (D3D_VW - tlen * D3D_SW) / 2;
        rs->DrawSmallString(tx, 120, g_d3doom_toast_msg, colorWhite, false);
    }
}

void D3Doom_STAR_DrawPopupOverlay(void* render_system) {
    if (!g_d3doom_inv_popup_open && !g_d3doom_quest_popup_open) return;
    idRenderSystem* rs = render_system ? static_cast<idRenderSystem*>(render_system) : ::renderSystem;
    if (!rs) return;

    /* Semi-transparent dark background panel */
    rs->DrawFill(60, 40, 520, 400, idVec4(0.0f, 0.0f, 0.0f, 0.75f));

    if (g_d3doom_inv_popup_open) {
        rs->DrawBigString(200, 50, "OASIS INVENTORY", colorWhite, false);
        rs->DrawSmallString(64, 78, "-----------------------------------------------", colorDkGrey, false);

        if (g_d3doom_inv_count == 0) {
            rs->DrawSmallString(80, 120, "No items in your OASIS inventory.", colorDkGrey, false);
        } else {
            int visible = idMath::Imin(g_d3doom_inv_count, 16);
            for (int i = 0; i < visible; i++) {
                char line[80];
                idStr::snPrintf(line, sizeof(line), "%s%s",
                                i == g_d3doom_inv_selected ? "> " : "  ",
                                g_d3doom_inv_names[i]);
                idVec4 col = (i == g_d3doom_inv_selected) ? colorWhite : colorMdGrey;
                rs->DrawSmallString(80, 100 + i * D3D_SH + 2, line, col, false);
            }
        }

        rs->DrawSmallString(64, 390, "[I] Close   [U] Use   [A] Send to Avatar   [C] Send to Clan", colorYellow, false);
    }

    if (g_d3doom_quest_popup_open) {
        rs->DrawBigString(220, 50, "OASIS QUESTS", colorWhite, false);
        rs->DrawSmallString(64, 78, "-----------------------------------------------", colorDkGrey, false);

        if (g_d3doom_quest_count == 0) {
            rs->DrawSmallString(80, 120, "No active OASIS quests.", colorDkGrey, false);
        } else {
            int visible = idMath::Imin(g_d3doom_quest_count, 14);
            for (int i = 0; i < visible; i++) {
                char line[80];
                idStr::snPrintf(line, sizeof(line), "%s%s",
                                i == g_d3doom_quest_selected ? "> " : "  ",
                                g_d3doom_quest_names[i]);
                idVec4 col = (i == g_d3doom_quest_selected) ? colorWhite : colorMdGrey;
                rs->DrawSmallString(80, 100 + i * D3D_SH + 2, line, col, false);
            }
            if (g_d3doom_quest_selected < g_d3doom_quest_count)
                rs->DrawSmallString(80, 370, g_d3doom_quest_descs[g_d3doom_quest_selected], colorCyan, false);
        }

        rs->DrawSmallString(64, 390, "[Q] Close   [Up/Down] Navigate", colorYellow, false);
    }
}

/*=============================================================================
 * Input handling
 *
 * Doom 3 BFG key constants (from sys/win32/win_input.cpp / KeyInput.h):
 *   K_I = 'i' (lower-case), K_Q = 'q', K_ESCAPE = K_ESCAPE, etc.
 * Pass the lower-case character or keyNum_t to this function.
 *===========================================================================*/

void D3Doom_STAR_HandleKey(int key, int down) {
    if (!g_d3doom_initialized || !down) return;

    const int K_i       = 'i';
    const int K_q       = 'q';
    const int K_ESC     = 27;    /* K_ESCAPE */
    const int K_UP      = 200;   /* K_UPARROW */
    const int K_DN      = 208;   /* K_DOWNARROW */
    const int K_u       = 'u';
    const int K_a       = 'a';
    const int K_c       = 'c';

    if (key == K_i) { D3Doom_STAR_ToggleInventoryPopup(); return; }
    if (key == K_q) { D3Doom_STAR_ToggleQuestPopup();     return; }
    if (key == K_ESC) {
        g_d3doom_inv_popup_open = g_d3doom_quest_popup_open = false;
        return;
    }
    if (key == K_UP) {
        if (g_d3doom_inv_popup_open   && g_d3doom_inv_selected   > 0) g_d3doom_inv_selected--;
        if (g_d3doom_quest_popup_open && g_d3doom_quest_selected > 0) g_d3doom_quest_selected--;
        return;
    }
    if (key == K_DN) {
        if (g_d3doom_inv_popup_open   && g_d3doom_inv_selected   < g_d3doom_inv_count   - 1) g_d3doom_inv_selected++;
        if (g_d3doom_quest_popup_open && g_d3doom_quest_selected < g_d3doom_quest_count - 1) g_d3doom_quest_selected++;
        return;
    }
    if (key == K_u && g_d3doom_inv_popup_open && g_d3doom_inv_selected < g_d3doom_inv_count) {
        star_api_use_item(g_d3doom_inv_names[g_d3doom_inv_selected]);
        snprintf(g_d3doom_toast_msg, sizeof(g_d3doom_toast_msg), "Used: %s", g_d3doom_inv_names[g_d3doom_inv_selected]);
        g_d3doom_toast_frames = D3DOOM_TOAST_FRAMES;
        g_d3doom_inv_count = 0;
        return;
    }
    if (key == K_a && g_d3doom_inv_popup_open && g_d3doom_inv_selected < g_d3doom_inv_count) {
        star_api_send_item_to_avatar(g_d3doom_inv_names[g_d3doom_inv_selected]);
        snprintf(g_d3doom_toast_msg, sizeof(g_d3doom_toast_msg), "Sent to Avatar: %s", g_d3doom_inv_names[g_d3doom_inv_selected]);
        g_d3doom_toast_frames = D3DOOM_TOAST_FRAMES;
        g_d3doom_inv_count = 0;
        return;
    }
    if (key == K_c && g_d3doom_inv_popup_open && g_d3doom_inv_selected < g_d3doom_inv_count) {
        star_api_send_item_to_clan(g_d3doom_inv_names[g_d3doom_inv_selected]);
        snprintf(g_d3doom_toast_msg, sizeof(g_d3doom_toast_msg), "Sent to Clan: %s", g_d3doom_inv_names[g_d3doom_inv_selected]);
        g_d3doom_toast_frames = D3DOOM_TOAST_FRAMES;
        g_d3doom_inv_count = 0;
        return;
    }
}
