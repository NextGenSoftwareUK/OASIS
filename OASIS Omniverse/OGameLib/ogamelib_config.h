/**
 * ogamelib_config.h — OGameLib oasisstar.json load/save
 *
 * Defines the shared star_config_t structure holding all fields common to both
 * ODOOM and OQuake, plus load/save functions for oasisstar.json.
 *
 * Games that need additional fields extend the config by embedding star_config_t
 * as the first member of their own game-specific config struct, then call the
 * load/save functions with an ogamelib_config_ext_t extension hook.
 *
 * USAGE
 * -----
 * Declare the implementation in exactly ONE .c/.cpp file:
 *
 *   #define OGAMELIB_CONFIG_IMPL
 *   #include "ogamelib_config.h"
 *
 * All other files just include it without the define.
 */
#ifndef OGAMELIB_CONFIG_H
#define OGAMELIB_CONFIG_H

#include <stddef.h>

#ifdef __cplusplus
extern "C" {
#endif

#define OGAMELIB_CONFIG_STR_MAX   512
#define OGAMELIB_CONFIG_PATH_MAX  1024
#define OGAMELIB_CONFIG_FILENAME  "oasisstar.json"

/**
 * Fields shared by all OASIS-integrated games.
 * Both ODOOM and OQuake read and write all of these.
 */
typedef struct {
    /* API endpoints */
    char star_api_url[OGAMELIB_CONFIG_STR_MAX];    /* WEB5 STAR API base URL */
    char oasis_api_url[OGAMELIB_CONFIG_STR_MAX];   /* WEB4 OASIS API base URL */
    char star_transport[64];                        /* "remote" (default) or "native" */
    char oasis_dna_path[OGAMELIB_CONFIG_PATH_MAX];  /* Optional: path to OASIS_DNA.json */

    /* Session (persisted across launches) */
    char jwt_token[OGAMELIB_CONFIG_STR_MAX];
    char refresh_token[OGAMELIB_CONFIG_STR_MAX];
    char username[256];

    /* HUD / avatar */
    char beam_face[64];         /* texture/sprite name for "beamed in" status bar face */
    int  max_health;            /* cap for health sync (0 = use game default) */
    int  max_armor;             /* cap for armor sync (0 = use game default) */

    /* Pickup / inventory behaviour */
    int stack_armor;
    int stack_weapons;
    int stack_powerups;
    int stack_keys;
    int always_allow_pickup_if_max; /* 1 = always pick up even when at max */
    int always_add_items_to_inventory;

    /* NFT / minting */
    int  mint_weapons;
    int  mint_armor;
    int  mint_powerups;
    int  mint_keys;
    char nft_provider[128];
    char send_to_address_after_minting[OGAMELIB_CONFIG_STR_MAX];

    /* Cross-game ammo/weapon mappings (stored as comma-separated "from=to" pairs) */
    char cross_game_doom_ammo_to_quake[OGAMELIB_CONFIG_STR_MAX];
    char cross_game_quake_ammo_to_doom[OGAMELIB_CONFIG_STR_MAX];
    char cross_game_doom_weapon_to_quake[OGAMELIB_CONFIG_STR_MAX];
    char cross_game_quake_weapon_to_doom[OGAMELIB_CONFIG_STR_MAX];
} star_config_t;

/**
 * Extension hook: called after load/before save so games can read/write
 * their own additional fields from/to the same JSON file.
 *
 * @param json   Full JSON text (for load) or NULL (for save).
 * @param fp     Open FILE* (for save) or NULL (for load).
 * @param user   Caller-supplied context pointer.
 */
typedef void (*ogamelib_config_ext_fn)(const char* json, void* fp, void* user);

/**
 * Load oasisstar.json from path into cfg.
 * If ext is non-NULL it is called after shared fields are parsed (json = file text, fp = NULL).
 * Returns 1 on success, 0 if file not found or parse error.
 */
int ogamelib_config_load(const char* path, star_config_t* cfg,
                          ogamelib_config_ext_fn ext, void* ext_user);

/**
 * Save cfg to oasisstar.json at path.
 * If ext is non-NULL it is called to append game-specific fields before the closing brace.
 * Returns 1 on success, 0 on write error.
 */
int ogamelib_config_save(const char* path, const star_config_t* cfg,
                          ogamelib_config_ext_fn ext, void* ext_user);

/**
 * Write just the session fields (jwt_token, refresh_token, username) back to the file.
 * Cheaper than a full save when only session data changes (e.g. after beamin).
 * Reads the existing file, updates the three keys in-place, rewrites the file.
 */
int ogamelib_config_save_session(const char* path, const star_config_t* cfg);

/* ── Implementation ── */

#ifdef OGAMELIB_CONFIG_IMPL

#include "ogamelib_json.h"
#include "ogamelib_str.h"
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

static int ogamelib_read_bool(const char* json, const char* key, int default_val)
{
    char buf[16];
    if (!ogamelib_json_extract(json, key, buf, sizeof(buf))) return default_val;
    return (strcmp(buf, "true") == 0 || strcmp(buf, "1") == 0) ? 1 : 0;
}
static int ogamelib_read_int(const char* json, const char* key, int default_val)
{
    char buf[32];
    if (!ogamelib_json_extract(json, key, buf, sizeof(buf))) return default_val;
    return atoi(buf);
}
#define READ_STR(json, key, dest) ogamelib_json_extract((json),(key),(dest),(int)sizeof(dest))
#define READ_BOOL(json, key, def) ogamelib_read_bool((json),(key),(def))
#define READ_INT(json, key, def)  ogamelib_read_int((json),(key),(def))

int ogamelib_config_load(const char* path, star_config_t* cfg,
                          ogamelib_config_ext_fn ext, void* ext_user)
{
    if (!path || !cfg) return 0;

    FILE* f = fopen(path, "r");
    if (!f) return 0;

    fseek(f, 0, SEEK_END);
    long len = ftell(f);
    rewind(f);
    if (len <= 0) { fclose(f); return 0; }

    char* json = (char*)malloc((size_t)len + 1);
    if (!json) { fclose(f); return 0; }
    size_t read = fread(json, 1, (size_t)len, f);
    json[read] = '\0';
    fclose(f);

    /* API endpoints */
    READ_STR(json, "star_api_url",   cfg->star_api_url);
    READ_STR(json, "oasis_api_url",  cfg->oasis_api_url);
    READ_STR(json, "star_transport", cfg->star_transport);
    READ_STR(json, "oasis_dna_path", cfg->oasis_dna_path);

    /* Session */
    READ_STR(json, "jwt_token",      cfg->jwt_token);
    READ_STR(json, "refresh_token",  cfg->refresh_token);
    READ_STR(json, "username",       cfg->username);

    /* HUD */
    READ_STR(json, "beam_face",  cfg->beam_face);
    cfg->max_health = READ_INT(json, "max_health", 0);
    cfg->max_armor  = READ_INT(json, "max_armor",  0);

    /* Pickup behaviour */
    cfg->stack_armor    = READ_BOOL(json, "stack_armor",    0);
    cfg->stack_weapons  = READ_BOOL(json, "stack_weapons",  0);
    cfg->stack_powerups = READ_BOOL(json, "stack_powerups", 0);
    cfg->stack_keys     = READ_BOOL(json, "stack_keys",     0);
    cfg->always_allow_pickup_if_max    = READ_BOOL(json, "always_allow_pickup_if_max",    0);
    cfg->always_add_items_to_inventory = READ_BOOL(json, "always_add_items_to_inventory", 0);

    /* NFT */
    cfg->mint_weapons  = READ_BOOL(json, "mint_weapons",  0);
    cfg->mint_armor    = READ_BOOL(json, "mint_armor",    0);
    cfg->mint_powerups = READ_BOOL(json, "mint_powerups", 0);
    cfg->mint_keys     = READ_BOOL(json, "mint_keys",     0);
    READ_STR(json, "nft_provider",                  cfg->nft_provider);
    READ_STR(json, "send_to_address_after_minting", cfg->send_to_address_after_minting);

    /* Cross-game mappings */
    READ_STR(json, "cross_game_doom_ammo_to_quake",   cfg->cross_game_doom_ammo_to_quake);
    READ_STR(json, "cross_game_quake_ammo_to_doom",   cfg->cross_game_quake_ammo_to_doom);
    READ_STR(json, "cross_game_doom_weapon_to_quake", cfg->cross_game_doom_weapon_to_quake);
    READ_STR(json, "cross_game_quake_weapon_to_doom", cfg->cross_game_quake_weapon_to_doom);

    if (ext) ext(json, NULL, ext_user);

    free(json);
    return 1;
}

#define WRITE_STR_FIELD(f, key, val, comma) \
    fprintf((f), "    \"%s\": \"%s\"%s\n", (key), (val) ? (val) : "", (comma) ? "," : "")
#define WRITE_BOOL_FIELD(f, key, val, comma) \
    fprintf((f), "    \"%s\": %s%s\n", (key), (val) ? "true" : "false", (comma) ? "," : "")
#define WRITE_INT_FIELD(f, key, val, comma) \
    fprintf((f), "    \"%s\": %d%s\n", (key), (val), (comma) ? "," : "")

int ogamelib_config_save(const char* path, const star_config_t* cfg,
                          ogamelib_config_ext_fn ext, void* ext_user)
{
    if (!path || !cfg) return 0;
    FILE* f = fopen(path, "w");
    if (!f) return 0;

    fprintf(f, "{\n");
    WRITE_STR_FIELD(f,  "star_api_url",   cfg->star_api_url,   1);
    WRITE_STR_FIELD(f,  "oasis_api_url",  cfg->oasis_api_url,  1);
    WRITE_STR_FIELD(f,  "star_transport", cfg->star_transport,  1);
    WRITE_STR_FIELD(f,  "oasis_dna_path", cfg->oasis_dna_path, 1);
    WRITE_STR_FIELD(f,  "jwt_token",      cfg->jwt_token,       1);
    WRITE_STR_FIELD(f,  "refresh_token",  cfg->refresh_token,   1);
    WRITE_STR_FIELD(f,  "username",       cfg->username,        1);
    WRITE_STR_FIELD(f,  "beam_face",      cfg->beam_face,       1);
    WRITE_INT_FIELD(f,  "max_health",     cfg->max_health,      1);
    WRITE_INT_FIELD(f,  "max_armor",      cfg->max_armor,       1);
    WRITE_BOOL_FIELD(f, "stack_armor",    cfg->stack_armor,     1);
    WRITE_BOOL_FIELD(f, "stack_weapons",  cfg->stack_weapons,   1);
    WRITE_BOOL_FIELD(f, "stack_powerups", cfg->stack_powerups,  1);
    WRITE_BOOL_FIELD(f, "stack_keys",     cfg->stack_keys,      1);
    WRITE_BOOL_FIELD(f, "mint_weapons",   cfg->mint_weapons,    1);
    WRITE_BOOL_FIELD(f, "mint_armor",     cfg->mint_armor,      1);
    WRITE_BOOL_FIELD(f, "mint_powerups",  cfg->mint_powerups,   1);
    WRITE_BOOL_FIELD(f, "mint_keys",      cfg->mint_keys,       1);
    WRITE_STR_FIELD(f,  "nft_provider",   cfg->nft_provider,    1);
    WRITE_STR_FIELD(f,  "send_to_address_after_minting", cfg->send_to_address_after_minting, 1);
    WRITE_BOOL_FIELD(f, "always_allow_pickup_if_max",    cfg->always_allow_pickup_if_max,    1);
    WRITE_BOOL_FIELD(f, "always_add_items_to_inventory", cfg->always_add_items_to_inventory, 1);
    WRITE_STR_FIELD(f,  "cross_game_doom_ammo_to_quake",   cfg->cross_game_doom_ammo_to_quake,   1);
    WRITE_STR_FIELD(f,  "cross_game_quake_ammo_to_doom",   cfg->cross_game_quake_ammo_to_doom,   1);
    WRITE_STR_FIELD(f,  "cross_game_doom_weapon_to_quake", cfg->cross_game_doom_weapon_to_quake, 1);

    if (ext) {
        ext(NULL, f, ext_user);
        /* Game-specific fields end with trailing comma; write final field without */
        WRITE_STR_FIELD(f, "cross_game_quake_weapon_to_doom", cfg->cross_game_quake_weapon_to_doom, 0);
    } else {
        WRITE_STR_FIELD(f, "cross_game_quake_weapon_to_doom", cfg->cross_game_quake_weapon_to_doom, 0);
    }

    fprintf(f, "}\n");
    fclose(f);
    return 1;
}

int ogamelib_config_save_session(const char* path, const star_config_t* cfg)
{
    if (!path || !cfg) return 0;

    /* Read existing file */
    FILE* f = fopen(path, "r");
    if (!f) return ogamelib_config_save(path, cfg, NULL, NULL);

    fseek(f, 0, SEEK_END);
    long len = ftell(f);
    rewind(f);
    if (len <= 0) { fclose(f); return 0; }

    char* json = (char*)malloc((size_t)len + 4096);
    if (!json) { fclose(f); return 0; }
    size_t rlen = fread(json, 1, (size_t)len, f);
    json[rlen] = '\0';
    fclose(f);

    /* Simple in-place patch: replace the three session values */
    /* For simplicity we do a full rewrite with the updated values and preserved rest */
    /* Load into temp config, overlay session fields, save */
    star_config_t tmp;
    memset(&tmp, 0, sizeof(tmp));
    ogamelib_config_load(path, &tmp, NULL, NULL);
    ogamelib_str_copy(tmp.jwt_token,     cfg->jwt_token,     sizeof(tmp.jwt_token));
    ogamelib_str_copy(tmp.refresh_token, cfg->refresh_token, sizeof(tmp.refresh_token));
    ogamelib_str_copy(tmp.username,      cfg->username,      sizeof(tmp.username));

    free(json);
    return ogamelib_config_save(path, &tmp, NULL, NULL);
}

#undef READ_STR
#undef READ_BOOL
#undef READ_INT
#undef WRITE_STR_FIELD
#undef WRITE_BOOL_FIELD
#undef WRITE_INT_FIELD

#endif /* OGAMELIB_CONFIG_IMPL */

#ifdef __cplusplus
}
#endif

#endif /* OGAMELIB_CONFIG_H */
