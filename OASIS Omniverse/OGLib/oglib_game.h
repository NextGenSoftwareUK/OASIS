/**
 * oglib_game.h — shared OASIS game integration core (single-header).
 *
 * Implements the ODOOM/OQuake integration pattern once, so each engine only
 * supplies a thin adapter (print function, game source, monster/item tables)
 * and calls a handful of hooks:
 *
 *   oglib_game_init(&desc)            engine startup
 *   oglib_game_tick()                 once per frame
 *   oglib_game_shutdown()             engine shutdown
 *   oglib_game_on_pickup(cls, qty)    player picked up an item (engine class name)
 *   oglib_game_on_kill(cls)           player killed a monster (engine class name)
 *   oglib_game_command(args)          console "star <args>"
 *
 * What it does (same as ODOOM/OQuake):
 *   - oasisstar.json via oglib_config (URLs, saved session, NFT provider) plus the
 *     offline sync (edge) settings via oglib_edge, saved through the config hook.
 *   - Offline sync configured before ogengine_init, change/notification polling
 *     every frame, "star offline status|on|off|sync-and-off|cancel".
 *   - Saved session restore at startup; "star beamin <user> <pass>" / "star beamout".
 *   - Inventory refresh through ogengine_sync_inventory_*.
 *   - Item pickups -> ogengine_queue_add_item; kills -> ogengine_queue_monster_kill.
 *   - Drains console log, background errors and mint results each frame.
 *
 * In exactly ONE .c/.cpp file:
 *
 *   #define OGLIB_GAME_IMPL
 *   #define OGLIB_CONFIG_IMPL
 *   #include "oglib_game.h"
 */
#ifndef OGLIB_GAME_H
#define OGLIB_GAME_H

#include <stddef.h>

#ifdef __cplusplus
extern "C" {
#endif

/** Monster table row. Rows end with a {NULL} entry. */
typedef struct {
    const char* engine_name;   /* class name the engine reports on death */
    const char* display_name;  /* name shown in OASIS */
    int xp;
    int is_boss;               /* bosses can mint an NFT on kill */
} oglib_game_monster_t;

/** Item table row: engine pickup class -> OASIS inventory item. Rows end with {NULL}. */
typedef struct {
    const char* engine_name;   /* class name the engine reports on pickup */
    const char* item_name;     /* inventory item name */
    const char* item_type;     /* "KeyItem", "Weapon", "Ammo", "Armor", "Health", "Powerup", "Artifact", "Item" */
} oglib_game_item_t;

typedef void (*oglib_game_print_fn)(const char* line, void* user);

/** Everything the core needs from the engine adapter. */
typedef struct {
    const char* game_source;            /* e.g. "OSHADOWWARRIOR" (reported to OASIS) */
    const char* display_name;           /* e.g. "OShadowWarrior" (log prefix) */
    const char* config_path;            /* NULL = "oasisstar.json" */
    const char* default_ogengine_url;   /* used when oasisstar.json has none (may be NULL) */
    oglib_game_print_fn print;          /* console output; NULL = stdout */
    void* print_user;
    const oglib_game_monster_t* monsters;  /* may be NULL */
    const oglib_game_item_t* items;        /* may be NULL */
} oglib_game_desc_t;

int  oglib_game_init(const oglib_game_desc_t* desc);   /* 1 = ready, 0 = STAR disabled */
void oglib_game_tick(void);
void oglib_game_shutdown(void);

/** Report a pickup. Unlisted classes are ignored. Returns 1 if it was sent to OASIS. */
int  oglib_game_on_pickup(const char* engine_class, int quantity);
/** Report a kill. Unlisted classes get 10 XP. Returns 1 if it was sent to OASIS. */
int  oglib_game_on_kill(const char* engine_class);

/** Console command, without the leading "star": e.g. "beamin user pass", "offline on". */
void oglib_game_command(const char* args);

void oglib_game_beamin(const char* username, const char* password);
void oglib_game_beamout(void);

int  oglib_game_is_ready(void);          /* 1 when beamed in */
const char* oglib_game_username(void);   /* "" when not beamed in */
/** Current toast text, or NULL when none is showing. Decays inside oglib_game_tick. */
const char* oglib_game_toast(void);

/** Offline sync, same contract as OQuake: -1 = Remote-Only release, 0 = off, 1 = on. */
int  oglib_game_offline_sync_mode(void);
void oglib_game_offline_sync_command(const char* command);

#ifdef __cplusplus
}
#endif

/* ── Implementation ─────────────────────────────────────────────────────── */

#ifdef OGLIB_GAME_IMPL

#include "ogengine.h"
#include "ogengine_sync.h"
#include "oglib_config.h"
#include "oglib_edge.h"
#include "oglib_str.h"

#include <ctype.h>
#include <stdarg.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#ifdef __cplusplus
extern "C" {
#endif

#define OGLIB_GAME_TOAST_FRAMES 180

static oglib_game_desc_t g_oglib_game;
static star_config_t g_oglib_game_cfg;
static oglib_edge_settings_t g_oglib_game_edge = OGLIB_EDGE_SETTINGS_DEFAULT;
static char g_oglib_game_cfg_path[OGLIB_CONFIG_PATH_MAX] = OGLIB_CONFIG_FILENAME;
static int g_oglib_game_initialized = 0;
static int g_oglib_game_ready = 0;
static int g_oglib_game_debug = 0;
static char g_oglib_game_toast[256] = "";
static int g_oglib_game_toast_frames = 0;

static void oglib_game_printf(const char* fmt, ...)
{
    char line[1024];
    int n = snprintf(line, sizeof(line), "[%s] ", g_oglib_game.display_name ? g_oglib_game.display_name : "OASIS");
    va_list ap;
    va_start(ap, fmt);
    if (n < 0 || (size_t)n >= sizeof(line)) n = 0;
    vsnprintf(line + n, sizeof(line) - (size_t)n, fmt, ap);
    va_end(ap);
    if (g_oglib_game.print) g_oglib_game.print(line, g_oglib_game.print_user);
    else printf("%s\n", line);
}

static void oglib_game_set_toast(const char* msg)
{
    oglib_str_copy(g_oglib_game_toast, msg ? msg : "", sizeof(g_oglib_game_toast));
    g_oglib_game_toast_frames = OGLIB_GAME_TOAST_FRAMES;
}

static int oglib_game_streq_nocase(const char* a, const char* b)
{
    if (!a || !b) return 0;
    for (; *a && *b; ++a, ++b)
        if (tolower((unsigned char)*a) != tolower((unsigned char)*b)) return 0;
    return *a == *b;
}

/* oglib_config hook: load (json != NULL) or save (fp != NULL) the edge + debug fields. */
static void oglib_game_config_ext(const char* json, void* fp, void* user)
{
    (void)user;
    if (json) {
        char val[32];
        oglib_edge_load_json(&g_oglib_game_edge, json);
        if (oglib_json_extract(json, "star_debug", val, sizeof(val)) && val[0])
            g_oglib_game_debug = atoi(val) != 0;
    }
    if (fp) {
        oglib_edge_save_json((FILE*)fp, &g_oglib_game_edge);
        fprintf((FILE*)fp, "  \"star_debug\": %d,\n", g_oglib_game_debug);
    }
}

static void oglib_game_save_config(void)
{
    if (!oglib_config_save(g_oglib_game_cfg_path, &g_oglib_game_cfg, oglib_game_config_ext, NULL))
        oglib_game_printf("Could not write %s", g_oglib_game_cfg_path);
}

static void oglib_game_on_inventory_done(void* user)
{
    ogengine_item_list_t* list = NULL;
    ogengine_result_t result = OGENGINE_SUCCESS;
    char err[256] = "";
    (void)user;
    if (!ogengine_sync_inventory_get_result(&list, &result, err, sizeof(err))) return;
    if (result == OGENGINE_SUCCESS && list)
        oglib_game_printf("Inventory: %d item(s).", (int)list->count);
    else
        oglib_game_printf("Inventory sync failed: %s", err[0] ? err : "unknown error");
    ogengine_sync_inventory_clear_result();
}

static void oglib_game_refresh_inventory(void)
{
    if (g_oglib_game_ready && !ogengine_sync_inventory_in_progress())
        ogengine_sync_inventory_start(NULL, 0, g_oglib_game.game_source, oglib_game_on_inventory_done, NULL);
}

static void oglib_game_on_auth_done(void* user)
{
    int success = 0;
    char username[256] = "";
    char avatar_id[128] = "";
    char err[256] = "";
    (void)user;
    ogengine_sync_auth_get_result(&success, username, sizeof(username), avatar_id, sizeof(avatar_id), err, sizeof(err));
    if (success) {
        g_oglib_game_ready = 1;
        oglib_str_copy(g_oglib_game_cfg.username, username, sizeof(g_oglib_game_cfg.username));
        ogengine_get_current_jwt(g_oglib_game_cfg.jwt_token, sizeof(g_oglib_game_cfg.jwt_token));
        ogengine_get_current_refresh_token(g_oglib_game_cfg.refresh_token, sizeof(g_oglib_game_cfg.refresh_token));
        oglib_game_save_config();
        oglib_game_printf("Beamed in as %s.", username);
        oglib_game_set_toast("OASIS: Beamed in!");
        oglib_game_refresh_inventory();
    } else {
        g_oglib_game_ready = 0;
        oglib_game_printf("Beam-in failed: %s", err[0] ? err : ogengine_get_last_error());
        oglib_game_set_toast("OASIS: Beam-in failed.");
    }
}

int oglib_game_init(const oglib_game_desc_t* desc)
{
    ogengine_config_t cfg;
    if (g_oglib_game_initialized) return 1;
    if (!desc || !desc->game_source) return 0;
    g_oglib_game = *desc;
    if (!g_oglib_game.display_name) g_oglib_game.display_name = desc->game_source;
    oglib_str_copy(g_oglib_game_cfg_path, desc->config_path ? desc->config_path : OGLIB_CONFIG_FILENAME,
        sizeof(g_oglib_game_cfg_path));

    memset(&g_oglib_game_cfg, 0, sizeof(g_oglib_game_cfg));
    if (!oglib_config_load(g_oglib_game_cfg_path, &g_oglib_game_cfg, oglib_game_config_ext, NULL))
        oglib_game_printf("%s not found - using defaults.", g_oglib_game_cfg_path);
    if (!g_oglib_game_cfg.ogengine_url[0] && desc->default_ogengine_url)
        oglib_str_copy(g_oglib_game_cfg.ogengine_url, desc->default_ogengine_url, sizeof(g_oglib_game_cfg.ogengine_url));
    if (!g_oglib_game_cfg.nft_provider[0])
        oglib_str_copy(g_oglib_game_cfg.nft_provider, "SolanaOASIS", sizeof(g_oglib_game_cfg.nft_provider));

    ogengine_set_debug(g_oglib_game_debug);
    ogengine_sync_init();

    if (oglib_edge_configure(&g_oglib_game_edge) != OGENGINE_SUCCESS)
        oglib_game_printf("Offline sync settings rejected: %s", ogengine_get_last_error());

    memset(&cfg, 0, sizeof(cfg));
    cfg.base_url = g_oglib_game_cfg.ogengine_url;
    cfg.client_game_source = desc->game_source;
    cfg.timeout_seconds = 15;
    cfg.transport = oglib_game_streq_nocase(g_oglib_game_cfg.star_transport, "native") ? 1 : 0;
    cfg.oasis_dna_path = g_oglib_game_cfg.oasis_dna_path[0] ? g_oglib_game_cfg.oasis_dna_path : NULL;
    if (ogengine_init(&cfg) != OGENGINE_SUCCESS) {
        oglib_game_printf("STAR API init failed: %s - OASIS features disabled.", ogengine_get_last_error());
        ogengine_sync_cleanup();
        return 0;
    }
    if (g_oglib_game_cfg.oasis_api_url[0])
        ogengine_set_oasis_base_url(g_oglib_game_cfg.oasis_api_url);
    g_oglib_game_initialized = 1;

    if (g_oglib_game_cfg.jwt_token[0]) {
        ogengine_set_saved_session(g_oglib_game_cfg.jwt_token);
        if (g_oglib_game_cfg.refresh_token[0])
            ogengine_set_refresh_token(g_oglib_game_cfg.refresh_token);
        if (ogengine_restore_session() == OGENGINE_SUCCESS) {
            ogengine_get_current_username(g_oglib_game_cfg.username, sizeof(g_oglib_game_cfg.username));
            g_oglib_game_ready = 1;
            oglib_game_printf("Session restored for %s.", g_oglib_game_cfg.username);
            oglib_game_refresh_inventory();
        } else {
            oglib_game_printf("Saved session could not be restored - type 'star beamin <user> <pass>'.");
        }
    } else {
        oglib_game_printf("Ready. Type 'star beamin <user> <pass>' to beam in.");
    }
    return 1;
}

void oglib_game_tick(void)
{
    char buf[512];
    if (g_oglib_game_toast_frames > 0 && --g_oglib_game_toast_frames == 0) g_oglib_game_toast[0] = '\0';
    if (!g_oglib_game_initialized) return;

    ogengine_sync_pump();

    {
        char edge_msg[512];
        int changed = oglib_edge_finish_change(&g_oglib_game_edge, edge_msg, sizeof(edge_msg));
        if (changed != 0) {
            if (changed == 1) oglib_game_save_config();
            oglib_game_printf("%s", edge_msg);
            oglib_game_set_toast(edge_msg);
        }
        if (g_oglib_game_ready && !ogengine_sync_auth_in_progress()) {
            char note[256];
            if (ogengine_poll_edge_notification(note, sizeof(note)) == 1) {
                oglib_game_printf("%s", note);
                oglib_game_set_toast(note);
            }
        }
    }

    while (ogengine_consume_console_log(buf, sizeof(buf)))
        if (g_oglib_game_debug) oglib_game_printf("%s", buf);
    if (ogengine_consume_last_background_error(buf, sizeof(buf)))
        oglib_game_printf("Background error: %s", buf);
    {
        char item[256], nft_id[128], hash[128];
        if (ogengine_consume_last_mint_result(item, sizeof(item), nft_id, sizeof(nft_id), hash, sizeof(hash)))
            oglib_game_printf("NFT minted: %s (id %s)", item, nft_id);
    }
}

void oglib_game_shutdown(void)
{
    if (!g_oglib_game_initialized) return;
    ogengine_flush_add_item_jobs();
    ogengine_flush_use_item_jobs();
    oglib_game_save_config();
    ogengine_sync_cleanup();
    ogengine_cleanup();
    g_oglib_game_initialized = 0;
    g_oglib_game_ready = 0;
}

int oglib_game_on_pickup(const char* engine_class, int quantity)
{
    const oglib_game_item_t* it;
    char desc[320];
    if (!g_oglib_game_ready || !engine_class || !g_oglib_game.items) return 0;
    for (it = g_oglib_game.items; it->engine_name; ++it) {
        if (!oglib_game_streq_nocase(it->engine_name, engine_class)) continue;
        snprintf(desc, sizeof(desc), "%s from %s", it->item_type ? it->item_type : "Item", g_oglib_game.display_name);
        ogengine_queue_add_item(it->item_name, desc, g_oglib_game.game_source,
            it->item_type ? it->item_type : "Item", NULL, quantity > 0 ? quantity : 1, 1);
        if (g_oglib_game_debug) oglib_game_printf("Pickup: %s -> %s", engine_class, it->item_name);
        if (it->item_type && strcmp(it->item_type, "KeyItem") == 0) {
            char toast[320];
            snprintf(toast, sizeof(toast), "OASIS: %s added to cross-game inventory", it->item_name);
            oglib_game_set_toast(toast);
        }
        return 1;
    }
    return 0;
}

int oglib_game_on_kill(const char* engine_class)
{
    const oglib_game_monster_t* m = NULL;
    const oglib_game_monster_t* row;
    if (!g_oglib_game_ready || !engine_class || !engine_class[0]) return 0;
    if (g_oglib_game.monsters)
        for (row = g_oglib_game.monsters; row->engine_name; ++row)
            if (oglib_game_streq_nocase(row->engine_name, engine_class)) { m = row; break; }
    /* XP, quest progress and boss minting all run on the client's background thread. */
    ogengine_queue_monster_kill(engine_class, m ? m->display_name : engine_class, m ? m->xp : 10,
        m ? m->is_boss : 0, m ? m->is_boss : 0, g_oglib_game_cfg.nft_provider, g_oglib_game.game_source);
    if (m && m->is_boss) {
        char toast[320];
        snprintf(toast, sizeof(toast), "OASIS: %s defeated! +%d XP", m->display_name, m->xp);
        oglib_game_set_toast(toast);
    }
    return 1;
}

void oglib_game_beamin(const char* username, const char* password)
{
    if (!g_oglib_game_initialized) { oglib_game_printf("STAR API not initialised."); return; }
    if (!username || !username[0] || !password) { oglib_game_printf("Usage: star beamin <user> <pass>"); return; }
    if (ogengine_sync_auth_in_progress()) { oglib_game_printf("Beam-in already in progress."); return; }
    oglib_game_printf("Beaming in as %s...", username);
    ogengine_sync_auth_start(username, password, oglib_game_on_auth_done, NULL);
}

void oglib_game_beamout(void)
{
    g_oglib_game_ready = 0;
    g_oglib_game_cfg.jwt_token[0] = '\0';
    g_oglib_game_cfg.refresh_token[0] = '\0';
    g_oglib_game_cfg.username[0] = '\0';
    oglib_game_save_config();
    if (g_oglib_game_initialized) {
        /* Same as ODOOM3: drop the client; the next game start begins logged out. */
        ogengine_sync_cleanup();
        ogengine_cleanup();
        g_oglib_game_initialized = 0;
    }
    oglib_game_printf("Beamed out.");
    oglib_game_set_toast("OASIS: Beamed out.");
}

int oglib_game_offline_sync_mode(void)
{
    int capabilities = ogengine_get_edge_capabilities();
    return !(capabilities & 1) ? -1 : (capabilities & 2) ? 1 : 0;
}

void oglib_game_offline_sync_command(const char* command)
{
    char message[512];
    oglib_edge_command(command, message, sizeof(message));
    oglib_game_printf("%s", message);
    oglib_game_set_toast(message);
}

void oglib_game_command(const char* args)
{
    char line[512];
    char* argv[4] = { NULL, NULL, NULL, NULL };
    int argc = 0;
    char* p;
    oglib_str_copy(line, args ? args : "", sizeof(line));
    for (p = line; *p && argc < 4;) {
        while (*p == ' ' || *p == '\t') *p++ = '\0';
        if (!*p) break;
        argv[argc++] = p;
        while (*p && *p != ' ' && *p != '\t') ++p;
    }
    if (argc == 0 || oglib_game_streq_nocase(argv[0], "help")) {
        oglib_game_printf("star beamin <user> <pass> | beamout | status | inventory | offline <status|on|off|sync-and-off> | debug <on|off>");
        return;
    }
    if (oglib_game_streq_nocase(argv[0], "beamin")) { oglib_game_beamin(argv[1], argv[2] ? argv[2] : ""); return; }
    if (oglib_game_streq_nocase(argv[0], "beamout")) { oglib_game_beamout(); return; }
    if (oglib_game_streq_nocase(argv[0], "offline")) { oglib_game_offline_sync_command(argv[1] ? argv[1] : "status"); return; }
    if (oglib_game_streq_nocase(argv[0], "inventory")) {
        if (!g_oglib_game_ready) oglib_game_printf("Not beamed in.");
        else oglib_game_refresh_inventory();
        return;
    }
    if (oglib_game_streq_nocase(argv[0], "status")) {
        oglib_game_printf("initialised=%d beamed_in=%d user=%s debug=%d", g_oglib_game_initialized, g_oglib_game_ready,
            g_oglib_game_cfg.username[0] ? g_oglib_game_cfg.username : "-", g_oglib_game_debug);
        return;
    }
    if (oglib_game_streq_nocase(argv[0], "debug")) {
        if (argv[1]) g_oglib_game_debug = oglib_game_streq_nocase(argv[1], "on") || strcmp(argv[1], "1") == 0;
        ogengine_set_debug(g_oglib_game_debug);
        oglib_game_save_config();
        oglib_game_printf("debug=%d", g_oglib_game_debug);
        return;
    }
    oglib_game_printf("Unknown command '%s' - type 'star help'.", argv[0]);
}

int oglib_game_is_ready(void) { return g_oglib_game_ready; }
const char* oglib_game_username(void) { return g_oglib_game_ready ? g_oglib_game_cfg.username : ""; }
const char* oglib_game_toast(void) { return g_oglib_game_toast[0] ? g_oglib_game_toast : NULL; }

#ifdef __cplusplus
}
#endif

#endif /* OGLIB_GAME_IMPL */

#endif /* OGLIB_GAME_H */
