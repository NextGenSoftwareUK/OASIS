/**
 * oglib_monster.h — OGLib JSON-driven monster table
 *
 * Replaces (or augments) hardcoded monster arrays in ODOOM, OQuake, and
 * future ports such as ODOOM3-BFG. Games load their monster config from the
 * "monsters" array inside their game section of oasisstar.json.
 *
 * JSON format per entry (all fields optional except engine_name):
 *   {
 *     "engine_name": "monster_zombie",   <- entity def / actor class name
 *     "display_name": "Zombie",          <- shown in inventory/NFT name
 *     "xp": 20,                          <- XP awarded on kill
 *     "is_boss": 0,                      <- 1 = boss (triggers boss NFT path)
 *     "do_mint": 0                       <- 1 = mint NFT on kill
 *   }
 *
 * Example oasisstar.json section:
 *   "odoom3bfg": {
 *     "monsters": [
 *       { "engine_name": "monster_zombie", "display_name": "Zombie", "xp": 20 },
 *       { "engine_name": "monster_boss_cyberdemon", "display_name": "Cyberdemon",
 *         "xp": 1000, "is_boss": 1, "do_mint": 1 }
 *     ]
 *   }
 *
 * USAGE
 * -----
 * In exactly ONE .c/.cpp file per game (before including oglib.h):
 *   #define OGLIB_MONSTER_IMPL
 *   #include "OGLib/oglib.h"
 *
 * All other files include without the define.
 *
 * EXISTING GAME MIGRATION
 * -----------------------
 * ODOOM and OQuake carry hardcoded monster tables in their integration .cpp
 * files. To migrate: call oglib_monster_table_load_json() at Init time; the
 * JSON table takes priority while the hardcoded table remains as fallback.
 * See oglib_monster_table_find() — it is NULL-safe and returns NULL when the
 * monster is not listed, so "not found = skip" works for both approaches.
 */

#ifndef OGLIB_MONSTER_H
#define OGLIB_MONSTER_H

#include <string.h>
#include <stdio.h>
#include <stdlib.h>

#ifdef __cplusplus
extern "C" {
#endif

#define OGLIB_MONSTER_TABLE_MAX 128
#define OGLIB_MONSTER_NAME_MAX  128

typedef struct {
    char engine_name[OGLIB_MONSTER_NAME_MAX]; /* entity def / actor class name */
    char display_name[OGLIB_MONSTER_NAME_MAX]; /* shown in STAR inventory / NFT name */
    int  xp;
    int  is_boss;
    int  do_mint;
} oglib_monster_entry_t;

typedef struct {
    oglib_monster_entry_t entries[OGLIB_MONSTER_TABLE_MAX];
    int count;
} oglib_monster_table_t;

/**
 * Look up a monster by engine name (case-sensitive).
 * Returns pointer into table->entries, or NULL if not found.
 */
static inline const oglib_monster_entry_t*
oglib_monster_find(const oglib_monster_table_t* table, const char* engine_name)
{
    if (!table || !engine_name) return NULL;
    for (int i = 0; i < table->count; i++) {
        if (strcmp(table->entries[i].engine_name, engine_name) == 0)
            return &table->entries[i];
    }
    return NULL;
}

/**
 * Look up a monster by engine name, case-insensitive.
 * Returns pointer into table->entries, or NULL if not found.
 */
static inline const oglib_monster_entry_t*
oglib_monster_find_nocase(const oglib_monster_table_t* table, const char* engine_name)
{
    if (!table || !engine_name) return NULL;
    for (int i = 0; i < table->count; i++) {
#ifdef _WIN32
        if (_stricmp(table->entries[i].engine_name, engine_name) == 0)
#else
        if (strcasecmp(table->entries[i].engine_name, engine_name) == 0)
#endif
            return &table->entries[i];
    }
    return NULL;
}

#ifdef OGLIB_MONSTER_IMPL

/**
 * Extract a string value from a flat JSON object fragment by key.
 * Returns 1 on success, 0 on failure. Internal helper.
 */
static int oglib_monster_json_field(const char* obj, const char* key,
                                    char* out, int out_max)
{
    char pat[128];
    if (!obj || !key || !out || out_max <= 0) return 0;
    snprintf(pat, sizeof(pat), "\"%s\"", key);
    const char* p = strstr(obj, pat);
    if (!p) return 0;
    p += strlen(pat);
    while (*p == ' ' || *p == '\t' || *p == '\r' || *p == '\n') p++;
    if (*p != ':') return 0;
    p++;
    while (*p == ' ' || *p == '\t' || *p == '\r' || *p == '\n') p++;
    int n = 0;
    if (*p == '"') {
        p++;
        while (*p && *p != '"' && n < out_max - 1) out[n++] = *p++;
    } else {
        while (*p && *p != ',' && *p != '}' && *p != '\n' &&
               *p != ' ' && *p != '\t' && n < out_max - 1)
            out[n++] = *p++;
    }
    out[n] = '\0';
    return n > 0 ? 1 : 0;
}

/**
 * Load a monster table from a JSON "monsters" array fragment.
 *
 * json_array must be the JSON text starting at '[' of the monsters array,
 * e.g. the value of oasisstar.json "monsters" key inside a game section.
 *
 * Appends to table (does not clear it first) up to OGLIB_MONSTER_TABLE_MAX
 * total entries. Returns number of entries successfully parsed.
 *
 * Entries already in the table with the same engine_name are overwritten
 * (JSON takes priority over hardcoded defaults).
 */
static int oglib_monster_table_load_json_array(oglib_monster_table_t* table,
                                                const char* json_array)
{
    if (!table || !json_array) return 0;
    const char* p = json_array;
    /* Advance to opening '[' */
    while (*p && *p != '[') p++;
    if (*p != '[') return 0;
    p++;

    int added = 0;
    while (*p) {
        /* Find next object '{' */
        while (*p && *p != '{' && *p != ']') p++;
        if (!*p || *p == ']') break;
        const char* obj_start = p;
        /* Find matching '}' (no nesting) */
        while (*p && *p != '}') p++;
        if (!*p) break;
        const char* obj_end = p + 1;

        /* Copy object fragment to a buffer for parsing */
        int obj_len = (int)(obj_end - obj_start);
        if (obj_len <= 0 || obj_len > 4096) { p++; continue; }
        char obj[4096];
        int copy_len = obj_len < 4095 ? obj_len : 4095;
        memcpy(obj, obj_start, copy_len);
        obj[copy_len] = '\0';

        /* Parse fields */
        char engine_name[OGLIB_MONSTER_NAME_MAX] = {0};
        char display_name[OGLIB_MONSTER_NAME_MAX] = {0};
        char xp_str[32] = "0";
        char is_boss_str[8] = "0";
        char do_mint_str[8] = "0";

        if (!oglib_monster_json_field(obj, "engine_name", engine_name, sizeof(engine_name)))
        { p++; continue; } /* engine_name is required */

        oglib_monster_json_field(obj, "display_name", display_name, sizeof(display_name));
        oglib_monster_json_field(obj, "xp",         xp_str,      sizeof(xp_str));
        oglib_monster_json_field(obj, "is_boss",    is_boss_str, sizeof(is_boss_str));
        oglib_monster_json_field(obj, "do_mint",    do_mint_str, sizeof(do_mint_str));

        if (display_name[0] == '\0')
            strncpy(display_name, engine_name, sizeof(display_name) - 1);

        /* Check if engine_name already exists — overwrite if so */
        int idx = -1;
        for (int i = 0; i < table->count; i++) {
            if (strcmp(table->entries[i].engine_name, engine_name) == 0) {
                idx = i; break;
            }
        }
        if (idx < 0) {
            if (table->count >= OGLIB_MONSTER_TABLE_MAX) { p++; continue; }
            idx = table->count++;
        }

        strncpy(table->entries[idx].engine_name,  engine_name,  OGLIB_MONSTER_NAME_MAX - 1);
        strncpy(table->entries[idx].display_name, display_name, OGLIB_MONSTER_NAME_MAX - 1);
        table->entries[idx].xp       = atoi(xp_str);
        table->entries[idx].is_boss  = atoi(is_boss_str);
        table->entries[idx].do_mint  = atoi(do_mint_str);
        added++;
        p++;
    }
    return added;
}

/**
 * Convenience: find the "game_section.monsters" array inside a full
 * oasisstar.json string and load into table.
 *
 * game_section: e.g. "odoom3bfg", "odoom", "oquake".
 * Returns number of entries loaded, or 0 on failure.
 */
static int oglib_monster_table_load_from_oasisstar(oglib_monster_table_t* table,
                                                    const char* oasisstar_json,
                                                    const char* game_section)
{
    if (!table || !oasisstar_json || !game_section) return 0;

    /* Find game section key, e.g. "odoom3bfg" : { */
    char sec_pattern[128];
    snprintf(sec_pattern, sizeof(sec_pattern), "\"%s\"", game_section);
    const char* sec = strstr(oasisstar_json, sec_pattern);
    if (!sec) return 0;

    /* Find "monsters" array inside this section.
     * Simple approach: search forward for "monsters" before the next top-level section. */
    sec += strlen(sec_pattern);
    /* Find the '{' opening the section */
    while (*sec && *sec != '{') sec++;
    if (!*sec) return 0;

    /* Search for "monsters" key within a reasonable range (64 KB) */
    const char* monsters_key = strstr(sec, "\"monsters\"");
    if (!monsters_key || (monsters_key - sec) > 65536) return 0;

    monsters_key += strlen("\"monsters\"");
    while (*monsters_key && *monsters_key != '[') monsters_key++;
    if (!*monsters_key) return 0;

    return oglib_monster_table_load_json_array(table, monsters_key);
}

#endif /* OGLIB_MONSTER_IMPL */

#ifdef __cplusplus
}
#endif

#endif /* OGLIB_MONSTER_H */
