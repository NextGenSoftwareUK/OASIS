/**
 * ODOOM3 - OASIS STAR API Integration Implementation
 *
 * Integrates dhewm3 (classic Doom 3) with the OASIS STAR API.
 * Keycards collected in any OASIS game can open Doom 3 doors and vice versa.
 * Monster kills award XP and optional NFT mints. Cross-game quests supported.
 *
 * Integration points in neo/game/:
 *   idGameLocal::Init()            → D3Doom3_STAR_Init()
 *   idGameLocal::Shutdown()        → D3Doom3_STAR_Cleanup()
 *   idGameLocal::RunFrame()        → D3Doom3_STAR_Tick()
 *   idPlayer::GiveInventoryItem()  → D3Doom3_STAR_OnItemPickup()
 *   idGameLocal::RequirementMet()  → D3Doom3_STAR_CheckDoorAccess()
 *   idAI::Killed()                 → D3Doom3_STAR_OnMonsterKilled()
 *
 * BUILD: COPY_TO_DHEWM3_AND_BUILD.ps1 copies this file plus headers and OGLib/
 * to C:\Source\ODOOM3\neo\game\ before CMake runs.
 *
 * dhewm3 notes:
 *   - GAME_DLL is defined; this file is compiled into base.dll (not the exe).
 *   - Engine globals (common, cmdSystem, etc.) are set via the import struct
 *     during idGameLocal::Init() and are available via extern declarations.
 *   - No precompiled headers; include explicitly.
 */

#include "sys/platform.h"
#include "gamesys/SysCvar.h"      /* idCVar via framework/CVarSystem.h */
#include "framework/CmdSystem.h"  /* idCmdArgs, idCmdSystem */
#include "framework/Common.h"     /* idCommon */

#include "d3doom3_ogengine_integration.h"

/* STAR API (copies arrive via COPY_TO_DHEWM3_AND_BUILD.ps1) */
#include "ogengine.h"
#include "ogengine_sync.h"

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

/*
 * Engine global pointers — defined in game/Game_local.cpp (#ifdef GAME_DLL)
 * and set from the import struct during idGameLocal::Init().
 */
extern idCommon     *common;
extern idCmdSystem  *cmdSystem;
extern idCVarSystem *cvarSystem;

/*=============================================================================
 * Global state
 *===========================================================================*/

#define D3DOOM3_GAME_SOURCE    "ODOOM3"
#define D3DOOM3_LOG_TAG        "[STAR3] "
#define D3DOOM3_MAX_PATH       512
#define D3DOOM3_JSON_BUF_SIZE  8192

static bool g_d3doom3_initialized = false;
static bool g_d3doom3_client_ready = false;
static bool g_d3doom3_debug = true;
static char g_d3doom3_json_path[D3DOOM3_MAX_PATH] = {};
static char g_d3doom3_saved_username[128] = {};
static char g_d3doom3_saved_jwt[2048] = {};
static char g_d3doom3_saved_refresh_token[2048] = {};

/* Monster table — loaded from oasisstar.json "odoom3".monsters; defaults below. */
static oglib_monster_table_t g_d3doom3_monster_table = {};

/*
 * Default hardcoded entries; JSON in oasisstar.json takes priority per-entry.
 * Classic Doom 3 base-game monsters only (no d3xp/Resurrection of Evil content).
 */
static const struct { const char* engine_name; const char* display_name; int xp; int is_boss; int do_mint; }
k_D3Doom3DefaultMonsters[] = {
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
    { nullptr, nullptr, 0, 0, 0 }
};

/* Config CVars — read at Init, used throughout session. */
static idCVar d3doom3_ogengine_url(
    "d3doom3_ogengine_url", "https://star-api.oasisplatform.world/api",
    CVAR_GAME|CVAR_ARCHIVE, "OASIS STAR API base URL");
static idCVar d3doom3_star_oasis_url(
    "d3doom3_star_oasis_url", "https://api.oasisplatform.world",
    CVAR_GAME|CVAR_ARCHIVE, "OASIS WEB4 API base URL");
static idCVar d3doom3_star_nft_provider(
    "d3doom3_star_nft_provider", "SolanaOASIS",
    CVAR_GAME|CVAR_ARCHIVE, "NFT provider for minting (SolanaOASIS, etc.)");
static idCVar d3doom3_star_mint_keys(
    "d3doom3_star_mint_keys", "0",
    CVAR_GAME|CVAR_ARCHIVE, "1 = mint NFT on key pickup");
static idCVar d3doom3_star_mint_monsters(
    "d3doom3_star_mint_monsters", "1",
    CVAR_GAME|CVAR_ARCHIVE, "1 = mint NFT on boss kill (respects do_mint in monster table)");
static idCVar d3doom3_star_debug(
    "d3doom3_star_debug", "1",
    CVAR_GAME|CVAR_ARCHIVE, "1 = verbose STAR logging to console and star_api.log");
static idCVar d3doom3_star_consume_key_on_door(
    "d3doom3_star_consume_key_on_door", "1",
    CVAR_GAME|CVAR_ARCHIVE, "1 = remove STAR key when used to open a door");

/*=============================================================================
 * Logging helpers
 *===========================================================================*/

static int D3Doom3_ExtractJsonValue(const char* json, const char* key, char* out, int maxlen)
{
    char search[128];
    snprintf(search, sizeof(search), "\"%s\"", key);
    const char* p = strstr(json, search);
    if (!p) return 0;
    p += strlen(search);
    while (*p == ' ' || *p == ':') ++p;
    if (*p != '"') return 0;
    ++p;
    int i = 0;
    while (*p && *p != '"' && i < maxlen - 1) out[i++] = *p++;
    out[i] = '\0';
    return i > 0;
}

static void StarLog(const char* fmt, ...) {
    char buf[1024];
    va_list ap;
    va_start(ap, fmt);
    vsnprintf(buf, sizeof(buf), fmt, ap);
    va_end(ap);
    buf[sizeof(buf) - 1] = '\0';
    common->Printf(D3DOOM3_LOG_TAG "%s\n", buf);
    ogengine_log_to_file(buf);
}

static void StarLogDebug(const char* fmt, ...) {
    if (!g_d3doom3_debug) return;
    char buf[1024];
    va_list ap;
    va_start(ap, fmt);
    vsnprintf(buf, sizeof(buf), fmt, ap);
    va_end(ap);
    buf[sizeof(buf) - 1] = '\0';
    common->Printf(D3DOOM3_LOG_TAG "%s\n", buf);
    ogengine_log_to_file(buf);
}

/*=============================================================================
 * oasisstar.json config
 *===========================================================================*/

static bool D3Doom3_FindJsonPath(char* out, int out_max) {
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

static void D3Doom3_LoadJson(const char* path) {
    FILE* f = fopen(path, "r");
    if (!f) return;
    char buf[D3DOOM3_JSON_BUF_SIZE] = {};
    size_t n = fread(buf, 1, sizeof(buf) - 1, f);
    fclose(f);
    if (!n) return;
    buf[n] = '\0';

    char val[256];
    if (oglib_json_extract(buf, "ogengine_url", val, sizeof(val)) && val[0])
        d3doom3_ogengine_url.SetString(val);
    if (oglib_json_extract(buf, "oasis_api_url", val, sizeof(val)) && val[0])
        d3doom3_star_oasis_url.SetString(val);
    if (oglib_json_extract(buf, "nft_provider", val, sizeof(val)) && val[0])
        d3doom3_star_nft_provider.SetString(val);
    if (oglib_json_extract(buf, "mint_keys", val, sizeof(val)) && val[0])
        d3doom3_star_mint_keys.SetInteger(atoi(val));
    if (oglib_json_extract(buf, "mint_monsters", val, sizeof(val)) && val[0])
        d3doom3_star_mint_monsters.SetInteger(atoi(val));
    if (oglib_json_extract(buf, "star_debug", val, sizeof(val)) && val[0])
        d3doom3_star_debug.SetInteger(atoi(val));
    if (oglib_json_extract(buf, "consume_key_on_door", val, sizeof(val)) && val[0])
        d3doom3_star_consume_key_on_door.SetInteger(atoi(val));

    /* Session restore */
    char jwt_buf[2048] = {};
    if (oglib_json_extract(buf, "beamedin_avatar", val, sizeof(val)) && val[0])
        snprintf(g_d3doom3_saved_username, sizeof(g_d3doom3_saved_username), "%s", val);
    if ((oglib_json_extract(buf, "jwt_token", jwt_buf, sizeof(jwt_buf))) && jwt_buf[0])
        snprintf(g_d3doom3_saved_jwt, sizeof(g_d3doom3_saved_jwt), "%s", jwt_buf);
    if (oglib_json_extract(buf, "refresh_token", jwt_buf, sizeof(jwt_buf)) && jwt_buf[0])
        snprintf(g_d3doom3_saved_refresh_token, sizeof(g_d3doom3_saved_refresh_token), "%s", jwt_buf);

    /* Monster table — JSON entries take priority over hardcoded defaults */
    oglib_monster_table_load_from_oasisstar(&g_d3doom3_monster_table, buf, "odoom3");

    StarLogDebug("Loaded oasisstar.json from %s", path);
}

static void D3Doom3_SaveJson(const char* path) {
    if (!path || !path[0]) return;
    FILE* f = fopen(path, "w");
    if (!f) return;

    char uname[128] = {};
    char jwt[2048] = {};
    char refresh[2048] = {};
    if (g_d3doom3_initialized) {
        ogengine_get_current_username(uname, sizeof(uname));
        if (!ogengine_is_session_expired()) {
            ogengine_get_current_jwt(jwt, sizeof(jwt));
            ogengine_get_current_refresh_token(refresh, sizeof(refresh));
        }
    }
    if (!uname[0] && g_d3doom3_saved_username[0])
        snprintf(uname, sizeof(uname), "%s", g_d3doom3_saved_username);

    fprintf(f, "{\n");
    fprintf(f, "  \"ogengine_url\": \"%s\",\n",   d3doom3_ogengine_url.GetString());
    fprintf(f, "  \"oasis_api_url\": \"%s\",\n",  d3doom3_star_oasis_url.GetString());
    fprintf(f, "  \"nft_provider\": \"%s\",\n",   d3doom3_star_nft_provider.GetString());
    fprintf(f, "  \"mint_keys\": %d,\n",           d3doom3_star_mint_keys.GetInteger());
    fprintf(f, "  \"mint_monsters\": %d,\n",       d3doom3_star_mint_monsters.GetInteger());
    fprintf(f, "  \"star_debug\": %d,\n",          d3doom3_star_debug.GetInteger());
    fprintf(f, "  \"consume_key_on_door\": %d",    d3doom3_star_consume_key_on_door.GetInteger());
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

static void D3Doom3_OnAuthDone(void* user_data) {
    int success = 0;
    char username[128] = {}, avatar_id[128] = {}, err[256] = {};
    ogengine_sync_auth_get_result(&success, username, sizeof(username), avatar_id, sizeof(avatar_id), err, sizeof(err));
    if (success) {
        char jwt_buf[2048] = {};
        ogengine_sync_auth_get_result_jwt(jwt_buf, sizeof(jwt_buf));
        snprintf(g_d3doom3_saved_username, sizeof(g_d3doom3_saved_username), "%s", username);
        if (jwt_buf[0]) snprintf(g_d3doom3_saved_jwt, sizeof(g_d3doom3_saved_jwt), "%s", jwt_buf);
        g_d3doom3_client_ready = true;
        ogengine_refresh_avatar_profile();
        StarLog("Beamed in as %s", username);
        if (g_d3doom3_json_path[0]) D3Doom3_SaveJson(g_d3doom3_json_path);
    } else {
        StarLog("Beam-in failed: %s", err);
    }
}

static void D3Doom3_OnOperationCallback(ogengine_result_t result, int op_type, void* user_data) {
    if (op_type == OGENGINE_OP_PROFILE_LOADED && result == OGENGINE_SUCCESS) {
        g_d3doom3_client_ready = true;
        int xp = 0;
        ogengine_get_avatar_xp(&xp);
        StarLog("Profile loaded. Avatar XP: %d", xp);
    }
}

/*=============================================================================
 * Monster table initialization
 *===========================================================================*/

static void D3Doom3_InitMonsterTable(void) {
    g_d3doom3_monster_table.count = 0;
    for (int i = 0; k_D3Doom3DefaultMonsters[i].engine_name; ++i) {
        if (g_d3doom3_monster_table.count >= OGLIB_MONSTER_TABLE_MAX) break;
        oglib_monster_entry_t* e = &g_d3doom3_monster_table.entries[g_d3doom3_monster_table.count++];
        snprintf(e->engine_name,  sizeof(e->engine_name),  "%s", k_D3Doom3DefaultMonsters[i].engine_name);
        snprintf(e->display_name, sizeof(e->display_name), "%s", k_D3Doom3DefaultMonsters[i].display_name);
        e->xp      = k_D3Doom3DefaultMonsters[i].xp;
        e->is_boss = k_D3Doom3DefaultMonsters[i].is_boss;
        e->do_mint = k_D3Doom3DefaultMonsters[i].do_mint;
    }
    /* JSON from oasisstar.json overrides entries with matching engine_name (loaded in D3Doom3_LoadJson) */
}

/*=============================================================================
 * Console command: "star ..."
 *===========================================================================*/

static void D3Doom3_STAR_CmdHandler(const idCmdArgs& args) {
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
        common->Printf(D3DOOM3_LOG_TAG "ODOOM3 STAR Integration v1.0 (game_source=" D3DOOM3_GAME_SOURCE ")\n");
        return;
    }

    if (!strcasecmp(subcmd, "status")) {
        common->Printf(D3DOOM3_LOG_TAG "initialized=%d client_ready=%d debug=%d\n",
            g_d3doom3_initialized ? 1 : 0, g_d3doom3_client_ready ? 1 : 0, g_d3doom3_debug ? 1 : 0);
        if (g_d3doom3_saved_username[0])
            common->Printf(D3DOOM3_LOG_TAG "saved avatar: %s\n", g_d3doom3_saved_username);
        return;
    }

    if (!strcasecmp(subcmd, "debug")) {
        if (args.Argc() >= 3) {
            int on = !strcasecmp(args.Argv(2), "on") || atoi(args.Argv(2));
            g_d3doom3_debug = (on != 0);
            d3doom3_star_debug.SetInteger(on);
            ogengine_set_debug(on);
        }
        common->Printf(D3DOOM3_LOG_TAG "debug=%s\n", g_d3doom3_debug ? "on" : "off");
        return;
    }

    if (!strcasecmp(subcmd, "beamin")) {
        if (args.Argc() < 4) {
            common->Printf(D3DOOM3_LOG_TAG "Usage: star beamin <username> <password>\n");
            return;
        }
        if (!g_d3doom3_initialized) {
            common->Printf(D3DOOM3_LOG_TAG "STAR not initialized; check star_api.dll is present.\n");
            return;
        }
        common->Printf(D3DOOM3_LOG_TAG "Authenticating...\n");
        ogengine_sync_auth_start(args.Argv(2), args.Argv(3), D3Doom3_OnAuthDone, nullptr);
        return;
    }

    if (!strcasecmp(subcmd, "beamout")) {
        g_d3doom3_client_ready = false;
        g_d3doom3_saved_username[0] = '\0';
        g_d3doom3_saved_jwt[0] = '\0';
        g_d3doom3_saved_refresh_token[0] = '\0';
        if (g_d3doom3_json_path[0]) D3Doom3_SaveJson(g_d3doom3_json_path);
        ogengine_cleanup();
        g_d3doom3_initialized = false;
        common->Printf(D3DOOM3_LOG_TAG "Beamed out.\n");
        return;
    }

    if (!strcasecmp(subcmd, "inventory")) {
        if (!g_d3doom3_client_ready) { common->Printf(D3DOOM3_LOG_TAG "Not beamed in.\n"); return; }
        ogengine_item_list_t* list = nullptr;
        if (ogengine_get_inventory(&list) != OGENGINE_SUCCESS || !list) {
            common->Printf(D3DOOM3_LOG_TAG "Could not fetch inventory.\n");
            return;
        }
        common->Printf(D3DOOM3_LOG_TAG "Inventory (%d items):\n", (int)list->count);
        for (size_t i = 0; i < list->count; ++i)
            common->Printf("  [%d] %s  (type: %s  src: %s  qty: %d)\n",
                (int)i + 1, list->items[i].name, list->items[i].item_type,
                list->items[i].game_source, list->items[i].quantity);
        ogengine_free_item_list(list);
        return;
    }

    if (!strcasecmp(subcmd, "add")) {
        if (args.Argc() < 3) { common->Printf(D3DOOM3_LOG_TAG "Usage: star add <item_name>\n"); return; }
        if (!g_d3doom3_client_ready) { common->Printf(D3DOOM3_LOG_TAG "Not beamed in.\n"); return; }
        ogengine_queue_add_item(args.Argv(2), "Added via console", D3DOOM3_GAME_SOURCE, "Item", nullptr, 1, 1);
        common->Printf(D3DOOM3_LOG_TAG "Queued add: %s\n", args.Argv(2));
        return;
    }

    common->Printf(D3DOOM3_LOG_TAG "Unknown command '%s'. Type 'star' for help.\n", subcmd);
}

/*=============================================================================
 * Public API implementation
 *===========================================================================*/

void D3Doom3_STAR_Init(void) {
    if (g_d3doom3_initialized) return;

    g_d3doom3_debug = (d3doom3_star_debug.GetInteger() != 0);
    StarLog("ODOOM3: Initialising STAR integration...");

    /* Populate hardcoded monster table defaults */
    D3Doom3_InitMonsterTable();

    /* Load oasisstar.json (may override monster table + config + saved session) */
    if (D3Doom3_FindJsonPath(g_d3doom3_json_path, sizeof(g_d3doom3_json_path))) {
        D3Doom3_LoadJson(g_d3doom3_json_path);
    } else {
#ifdef _WIN32
        char exe[MAX_PATH] = {};
        if (GetModuleFileNameA(nullptr, exe, sizeof(exe))) {
            char* slash = strrchr(exe, '\\');
            if (slash) { *slash = '\0'; snprintf(g_d3doom3_json_path, sizeof(g_d3doom3_json_path), "%s\\oasisstar.json", exe); }
        }
        if (!g_d3doom3_json_path[0])
#endif
            snprintf(g_d3doom3_json_path, sizeof(g_d3doom3_json_path), "oasisstar.json");
        D3Doom3_SaveJson(g_d3doom3_json_path);
        StarLog("Created default oasisstar.json: %s", g_d3doom3_json_path);
    }

    g_d3doom3_debug = (d3doom3_star_debug.GetInteger() != 0);
    ogengine_set_debug(g_d3doom3_debug ? 1 : 0);

    /* Initialise STAR API client */
    ogengine_config_t cfg = {};
    cfg.base_url           = d3doom3_ogengine_url.GetString();
    cfg.client_game_source = D3DOOM3_GAME_SOURCE;
    cfg.transport          = 0;  /* remote HTTP */
    cfg.timeout_seconds    = 15;

    ogengine_sync_init();

    ogengine_result_t r = ogengine_init(&cfg);
    if (r != OGENGINE_SUCCESS) {
        StarLog("ogengine_init failed (%d) — STAR features disabled.", (int)r);
        return;
    }

    ogengine_set_operation_callback(D3Doom3_OnOperationCallback, nullptr);
    ogengine_set_oasis_base_url(d3doom3_star_oasis_url.GetString());
    g_d3doom3_initialized = true;

    /* Restore saved session if we have a JWT */
    if (g_d3doom3_saved_jwt[0]) {
        ogengine_set_saved_session(g_d3doom3_saved_jwt);
        if (g_d3doom3_saved_refresh_token[0])
            ogengine_set_refresh_token(g_d3doom3_saved_refresh_token);
        ogengine_restore_session();
        StarLog("Restoring session for %s...", g_d3doom3_saved_username[0] ? g_d3doom3_saved_username : "(unknown)");
    }

    /* Register in-game console command */
    cmdSystem->AddCommand("star", D3Doom3_STAR_CmdHandler, CMD_FL_GAME, "OASIS STAR API commands");

    StarLog("STAR integration ready. Type 'star beamin <user> <pass>' to authenticate.");
}

void D3Doom3_STAR_Cleanup(void) {
    if (!g_d3doom3_initialized) return;

    ogengine_flush_add_item_jobs();
    ogengine_flush_use_item_jobs();

    if (g_d3doom3_json_path[0])
        D3Doom3_SaveJson(g_d3doom3_json_path);

    ogengine_sync_cleanup();
    ogengine_cleanup();

    g_d3doom3_initialized  = false;
    g_d3doom3_client_ready = false;
    StarLog("STAR integration shut down.");
}

void D3Doom3_STAR_Tick(void) {
    if (!g_d3doom3_initialized) return;

    ogengine_sync_pump();

    char logbuf[512];
    while (ogengine_consume_console_log(logbuf, sizeof(logbuf)))
        common->Printf(D3DOOM3_LOG_TAG "%s\n", logbuf);

    char mint_item[256], nft_id[128], hash[128];
    if (ogengine_consume_last_mint_result(mint_item, sizeof(mint_item), nft_id, sizeof(nft_id), hash, sizeof(hash)))
        StarLog("NFT minted: %s  id=%s  hash=%s", mint_item, nft_id, hash);

    char errbuf[256];
    if (ogengine_consume_last_background_error(errbuf, sizeof(errbuf)))
        StarLog("Background error: %s", errbuf);

    /* --- cross-game spawn poll --- */
    {
        char entity_id[128];
        float sx, sy, sz;
        if (ogengine_poll_spawn_event(entity_id, sizeof(entity_id), &sx, &sy, &sz))
        {
            StarLog("OASIS SpawnEvent: %s at %.0f/%.0f/%.0f", entity_id, sx, sy, sz);
            char spawn_cmd[256];
            snprintf(spawn_cmd, sizeof(spawn_cmd), "spawn %s\n", entity_id);
            cmdSystem->BufferCommandText(CMD_EXEC_APPEND, spawn_cmd);
            ogengine_confirm_spawn(entity_id);
        }
    }

    /* --- cross-game event poll --- */
    {
        char evt_json[4096];
        while (ogengine_poll_cross_game_event(evt_json, sizeof(evt_json)))
        {
            char evt_type[64] = "";
            D3Doom3_ExtractJsonValue(evt_json, "EventType", evt_type, sizeof(evt_type));
            if (strcmp(evt_type, "ShowNarration") == 0) {
                char narration[128] = "";
                D3Doom3_ExtractJsonValue(evt_json, "NarrationText", narration, sizeof(narration));
                if (narration[0]) {
                    snprintf(g_d3doom3_toast_msg, sizeof(g_d3doom3_toast_msg), "%s", narration);
                    g_d3doom3_toast_frames = D3DOOM3_TOAST_FRAMES;
                }
            } else if (strcmp(evt_type, "PlayAudio") == 0) {
                char audio_title[128] = "", audio_url[256] = "";
                D3Doom3_ExtractJsonValue(evt_json, "AudioTitle", audio_title, sizeof(audio_title));
                D3Doom3_ExtractJsonValue(evt_json, "AudioUrl",   audio_url,   sizeof(audio_url));
                StarLog("OASIS PlayAudio: %s (%s) — streaming not yet implemented", audio_title, audio_url);
                /* TODO: play audio via idSoundSystem */
            } else if (strcmp(evt_type, "PlayVideo") == 0) {
                char video_title[128] = "", video_url[256] = "";
                D3Doom3_ExtractJsonValue(evt_json, "VideoTitle", video_title, sizeof(video_title));
                D3Doom3_ExtractJsonValue(evt_json, "VideoUrl",   video_url,   sizeof(video_url));
                StarLog("OASIS PlayVideo: %s (%s) — video overlay not yet implemented", video_title, video_url);
                /* TODO: show video via idCinematic */
            } else if (strcmp(evt_type, "OpenWebsite") == 0) {
                char website_url[256] = "";
                D3Doom3_ExtractJsonValue(evt_json, "WebsiteUrl", website_url, sizeof(website_url));
                StarLog("OASIS OpenWebsite: %s — browser overlay not yet implemented", website_url);
            } else if (strcmp(evt_type, "UnlockPortal") == 0) {
                char portal_id[64] = "";
                D3Doom3_ExtractJsonValue(evt_json, "PortalId", portal_id, sizeof(portal_id));
                StarLog("OASIS UnlockPortal: %s — portal unlock not yet implemented", portal_id);
                /* TODO: notify OGEditor portal system */
            }
        }
    }

    /* --- inventory grant poll --- */
    {
        char item_guid[64];
        while (ogengine_poll_inventory_grant(item_guid, sizeof(item_guid)))
        {
            StarLog("OASIS InventoryGrant: %s — inventory refresh triggered", item_guid);
            ogengine_get_inventory(NULL);
        }
    }
}

void D3Doom3_STAR_OnItemPickup(const char* inv_name, const char* inv_classname, int is_carry_item) {
    if (!g_d3doom3_initialized || !g_d3doom3_client_ready) return;
    if (!inv_name || !inv_name[0]) return;

    const char* item_type = is_carry_item ? "Key" : "Item";

    char desc[256];
    if (inv_classname && inv_classname[0])
        snprintf(desc, sizeof(desc), "Doom 3 (Classic) %s (%s)", item_type, inv_classname);
    else
        snprintf(desc, sizeof(desc), "Doom 3 (Classic) %s", item_type);

    int do_mint = is_carry_item ? d3doom3_star_mint_keys.GetInteger() : 0;

    if (do_mint) {
        ogengine_queue_pickup_with_mint(
            inv_name, desc, D3DOOM3_GAME_SOURCE, item_type,
            do_mint, d3doom3_star_nft_provider.GetString(), nullptr, 1);
    } else {
        ogengine_queue_add_item(
            inv_name, desc, D3DOOM3_GAME_SOURCE, item_type, nullptr, 1, 1);
    }

    if (is_carry_item)
        ogengine_queue_quest_progress_from_pickup(D3DOOM3_GAME_SOURCE, item_type, inv_name);

    StarLogDebug("Item pickup: name='%s' type=%s mint=%d", inv_name, item_type, do_mint);
}

int D3Doom3_STAR_CheckDoorAccess(const char* required_inv_name) {
    if (!g_d3doom3_initialized || !g_d3doom3_client_ready) return 0;
    if (!required_inv_name || !required_inv_name[0]) return 0;

    if (!ogengine_has_item(required_inv_name)) return 0;

    StarLog("Cross-game door: using STAR '%s'", required_inv_name);

    if (d3doom3_star_consume_key_on_door.GetInteger())
        ogengine_use_item(required_inv_name, "door");

    return 1;
}

void D3Doom3_STAR_OnMonsterKilled(const char* entity_def_name, int engine_is_boss) {
    if (!g_d3doom3_initialized || !g_d3doom3_client_ready) return;
    if (!entity_def_name || !entity_def_name[0]) return;

    const oglib_monster_entry_t* entry = oglib_monster_find(&g_d3doom3_monster_table, entity_def_name);
    if (!entry) return;

    int is_boss = entry->is_boss | engine_is_boss;
    int do_mint = entry->do_mint & d3doom3_star_mint_monsters.GetInteger();

    ogengine_queue_monster_kill(
        entity_def_name,
        entry->display_name,
        entry->xp,
        is_boss,
        do_mint,
        do_mint ? d3doom3_star_nft_provider.GetString() : nullptr,
        D3DOOM3_GAME_SOURCE);

    StarLogDebug("Monster kill: %s (%s) xp=%d boss=%d mint=%d",
        entity_def_name, entry->display_name, entry->xp, is_boss, do_mint);
}

/*=============================================================================
 * HUD / GUI — state and popup controls
 *
 * New state for inventory/quest popups, XP display, beamed-in face, toasts.
 *===========================================================================*/

#define D3DOOM3_INV_MAX      32
#define D3DOOM3_QUEST_MAX    16
#define D3DOOM3_TOAST_FRAMES 180   /* ~3 s at 60 fps */

static bool g_d3doom3_inv_popup_open   = false;
static bool g_d3doom3_quest_popup_open = false;
static int  g_d3doom3_inv_selected     = 0;
static int  g_d3doom3_quest_selected   = 0;
static int  g_d3doom3_inv_count        = 0;
static int  g_d3doom3_quest_count      = 0;
static int  g_d3doom3_xp               = 0;
static int  g_d3doom3_toast_frames     = 0;
static char g_d3doom3_toast_msg[128];
static char g_d3doom3_inv_names[D3DOOM3_INV_MAX][64];
static char g_d3doom3_quest_names[D3DOOM3_QUEST_MAX][64];
static char g_d3doom3_quest_descs[D3DOOM3_QUEST_MAX][128];

int         D3Doom3_STAR_IsBeamedIn(void)          { return g_d3doom3_client_ready ? 1 : 0; }
const char* D3Doom3_STAR_GetUsername(void)          { return g_d3doom3_saved_username; }
int         D3Doom3_STAR_GetXP(void)               { return g_d3doom3_xp; }
const char* D3Doom3_STAR_GetVersionString(void)     { return "ODOOM3 1.0.0"; }
int         D3Doom3_STAR_IsInventoryPopupOpen(void) { return g_d3doom3_inv_popup_open ? 1 : 0; }
int         D3Doom3_STAR_IsQuestPopupOpen(void)     { return g_d3doom3_quest_popup_open ? 1 : 0; }
int         D3Doom3_STAR_ShouldBlockInput(void)     { return (g_d3doom3_inv_popup_open || g_d3doom3_quest_popup_open) ? 1 : 0; }
int         D3Doom3_STAR_ShouldUseAvatarFace(void)  { return g_d3doom3_client_ready ? 1 : 0; }

void D3Doom3_STAR_ToggleInventoryPopup(void) {
    g_d3doom3_inv_popup_open = !g_d3doom3_inv_popup_open;
    if (g_d3doom3_inv_popup_open) {
        g_d3doom3_quest_popup_open = false;
        g_d3doom3_inv_selected = 0;
        g_d3doom3_inv_count = 0;   /* TODO: call ogengine_get_inventory */
    }
}

void D3Doom3_STAR_ToggleQuestPopup(void) {
    g_d3doom3_quest_popup_open = !g_d3doom3_quest_popup_open;
    if (g_d3doom3_quest_popup_open) {
        g_d3doom3_inv_popup_open = false;
        g_d3doom3_quest_selected = 0;
        g_d3doom3_quest_count = 0; /* TODO: call ogengine_get_quests */
    }
}

/*=============================================================================
 * HUD / GUI — drawing  (dhewm3 / classic Doom 3)
 *
 * dhewm3 idRenderSystem 2-D drawing (virtual 640×480 screen):
 *   rs->DrawStretchPic(x, y, w, h, s1, t1, s2, t2, material)
 *   rs->DrawFill(x, y, w, h, r, g, b, a)
 *   rs->DrawSmallChar(x, y, ch)
 *   rs->DrawSmallString(x, y, str, color, forceColor)
 *   rs->DrawBigChar(x, y, ch)
 *   rs->DrawBigString(x, y, str, color, forceColor)
 *
 * Call D3Doom3_STAR_DrawHUDStatus from idGameLocal::Draw() after the HUD
 * GUI has been rendered.  Pass ::renderSystem or NULL.
 *===========================================================================*/

#define D3D3_VW 640
#define D3D3_VH 480
#define D3D3_SW 8
#define D3D3_SH 8

void D3Doom3_STAR_DrawHUDStatus(void* render_system) {
    idRenderSystem* rs = render_system ? static_cast<idRenderSystem*>(render_system) : ::renderSystem;
    if (!rs || !g_d3doom3_initialized) return;

    if (g_d3doom3_toast_frames > 0) g_d3doom3_toast_frames--;

    /* Version — bottom-right */
    const char* ver = "ODOOM3 1.0.0";
    int vlen = (int)idStr::Length(ver);
    rs->DrawSmallString(D3D3_VW - vlen * D3D3_SW - 4, D3D3_VH - D3D3_SH - 4, ver, colorDkGrey, false);

    if (!g_d3doom3_client_ready) return;

    /* Beamed-in label — top-left */
    char buf[96];
    idStr::snPrintf(buf, sizeof(buf), "OASIS: %s", g_d3doom3_saved_username);
    rs->DrawSmallString(4, 4, buf, colorWhite, false);

    /* XP — top-right */
    char xpbuf[32];
    idStr::snPrintf(xpbuf, sizeof(xpbuf), "XP: %d", g_d3doom3_xp);
    int xplen = (int)idStr::Length(xpbuf);
    rs->DrawSmallString(D3D3_VW - xplen * D3D3_SW - 4, 4, xpbuf, colorYellow, false);

    /* Toast — centred */
    if (g_d3doom3_toast_frames > 0) {
        int tlen = (int)idStr::Length(g_d3doom3_toast_msg);
        int tx   = (D3D3_VW - tlen * D3D3_SW) / 2;
        rs->DrawSmallString(tx, 120, g_d3doom3_toast_msg, colorWhite, false);
    }
}

void D3Doom3_STAR_DrawPopupOverlay(void* render_system) {
    if (!g_d3doom3_inv_popup_open && !g_d3doom3_quest_popup_open) return;
    idRenderSystem* rs = render_system ? static_cast<idRenderSystem*>(render_system) : ::renderSystem;
    if (!rs) return;

    rs->DrawFill(60, 40, 520, 400, 0.0f, 0.0f, 0.0f, 0.75f);

    if (g_d3doom3_inv_popup_open) {
        rs->DrawBigString(200, 50, "OASIS INVENTORY", colorWhite, false);
        rs->DrawSmallString(64, 78, "-----------------------------------------------", colorDkGrey, false);

        if (g_d3doom3_inv_count == 0) {
            rs->DrawSmallString(80, 120, "No items in your OASIS inventory.", colorDkGrey, false);
        } else {
            int visible = (g_d3doom3_inv_count < 16) ? g_d3doom3_inv_count : 16;
            for (int i = 0; i < visible; i++) {
                char line[80];
                idStr::snPrintf(line, sizeof(line), "%s%s",
                    i == g_d3doom3_inv_selected ? "> " : "  ", g_d3doom3_inv_names[i]);
                rs->DrawSmallString(80, 100 + i * D3D3_SH + 2, line,
                    (i == g_d3doom3_inv_selected) ? colorWhite : colorMdGrey, false);
            }
        }

        rs->DrawSmallString(64, 390, "[I] Close  [U] Use  [A] Send to Avatar  [C] Send to Clan", colorYellow, false);
    }

    if (g_d3doom3_quest_popup_open) {
        rs->DrawBigString(220, 50, "OASIS QUESTS", colorWhite, false);
        rs->DrawSmallString(64, 78, "-----------------------------------------------", colorDkGrey, false);

        if (g_d3doom3_quest_count == 0) {
            rs->DrawSmallString(80, 120, "No active OASIS quests.", colorDkGrey, false);
        } else {
            int visible = (g_d3doom3_quest_count < 14) ? g_d3doom3_quest_count : 14;
            for (int i = 0; i < visible; i++) {
                char line[80];
                idStr::snPrintf(line, sizeof(line), "%s%s",
                    i == g_d3doom3_quest_selected ? "> " : "  ", g_d3doom3_quest_names[i]);
                rs->DrawSmallString(80, 100 + i * D3D3_SH + 2, line,
                    (i == g_d3doom3_quest_selected) ? colorWhite : colorMdGrey, false);
            }
            if (g_d3doom3_quest_selected < g_d3doom3_quest_count)
                rs->DrawSmallString(80, 370, g_d3doom3_quest_descs[g_d3doom3_quest_selected], colorCyan, false);
        }

        rs->DrawSmallString(64, 390, "[Q] Close   [Up/Down] Navigate", colorYellow, false);
    }
}

/*=============================================================================
 * Input handling
 *
 * dhewm3 key constants from framework/KeyInput.h:
 *   K_ESCAPE = 27, K_UPARROW, K_DOWNARROW — or use lower-case char for letters.
 *===========================================================================*/

void D3Doom3_STAR_HandleKey(int key, int down) {
    if (!g_d3doom3_initialized || !down) return;

    const int K_ESC = 27;
    const int K_UP  = 200;
    const int K_DN  = 208;

    if (key == 'i') { D3Doom3_STAR_ToggleInventoryPopup(); return; }
    if (key == 'q') { D3Doom3_STAR_ToggleQuestPopup();     return; }
    if (key == K_ESC) {
        g_d3doom3_inv_popup_open = g_d3doom3_quest_popup_open = false;
        return;
    }
    if (key == K_UP) {
        if (g_d3doom3_inv_popup_open   && g_d3doom3_inv_selected   > 0) g_d3doom3_inv_selected--;
        if (g_d3doom3_quest_popup_open && g_d3doom3_quest_selected > 0) g_d3doom3_quest_selected--;
        return;
    }
    if (key == K_DN) {
        if (g_d3doom3_inv_popup_open   && g_d3doom3_inv_selected   < g_d3doom3_inv_count   - 1) g_d3doom3_inv_selected++;
        if (g_d3doom3_quest_popup_open && g_d3doom3_quest_selected < g_d3doom3_quest_count - 1) g_d3doom3_quest_selected++;
        return;
    }
    if (key == 'u' && g_d3doom3_inv_popup_open && g_d3doom3_inv_selected < g_d3doom3_inv_count) {
        ogengine_use_item(g_d3doom3_inv_names[g_d3doom3_inv_selected]);
        snprintf(g_d3doom3_toast_msg, sizeof(g_d3doom3_toast_msg), "Used: %s", g_d3doom3_inv_names[g_d3doom3_inv_selected]);
        g_d3doom3_toast_frames = D3DOOM3_TOAST_FRAMES;
        g_d3doom3_inv_count = 0;
        return;
    }
    if (key == 'a' && g_d3doom3_inv_popup_open && g_d3doom3_inv_selected < g_d3doom3_inv_count) {
        ogengine_send_item_to_avatar(g_d3doom3_inv_names[g_d3doom3_inv_selected]);
        snprintf(g_d3doom3_toast_msg, sizeof(g_d3doom3_toast_msg), "Sent to Avatar: %s", g_d3doom3_inv_names[g_d3doom3_inv_selected]);
        g_d3doom3_toast_frames = D3DOOM3_TOAST_FRAMES;
        g_d3doom3_inv_count = 0;
        return;
    }
    if (key == 'c' && g_d3doom3_inv_popup_open && g_d3doom3_inv_selected < g_d3doom3_inv_count) {
        ogengine_send_item_to_clan(g_d3doom3_inv_names[g_d3doom3_inv_selected]);
        snprintf(g_d3doom3_toast_msg, sizeof(g_d3doom3_toast_msg), "Sent to Clan: %s", g_d3doom3_inv_names[g_d3doom3_inv_selected]);
        g_d3doom3_toast_frames = D3DOOM3_TOAST_FRAMES;
        g_d3doom3_inv_count = 0;
        return;
    }
}

/*=============================================================================
 * OASIS Portal / Teleport — incoming warp from another OGame
 *===========================================================================*/

void D3Doom3_STAR_CheckIncomingTeleport(void)
{
    char map[256];
    float x = 0, y = 0, z = 64;
    if (!ogengine_poll_teleport_request(map, sizeof(map), &x, &y, &z))
        return;
    StarLog("OASIS Teleport arrive: map=%s pos=%.0f/%.0f/%.0f", map, x, y, z);
    {
        idPlayer *localPlayer = gameLocal.GetLocalPlayer();
        if (localPlayer)
        {
            idAngles ang(0.0f, 0.0f, 0.0f);
            localPlayer->Teleport(idVec3(x, y, z), ang, NULL);
        }
    }
    ogengine_confirm_teleport_arrival();
}
