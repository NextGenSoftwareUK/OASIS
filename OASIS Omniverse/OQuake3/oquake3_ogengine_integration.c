/**
 * OQuake3 - OASIS STAR API Integration Implementation
 *
 * Integrates Quake3e with the OASIS STAR API. Quake III Arena is an arena/deathmatch
 * game — it has no traditional key/door locks. Runes (from Q3:TA modes) and powerups
 * fill the collectible role. Bot/player kills in PvE/PvP modes earn XP and can trigger
 * NFT minting per bot type.
 *
 * OASIS thing type range: 7000-7899
 * Portal thing type: 5900 (shared cross-game)
 *
 * Base engine: Quake3e (https://github.com/ec-/Quake3e, GPL-2.0)
 *
 * Build: link with ogengine.lib (Windows) or -lstar_api (Linux/macOS).
 * See BUILD_OQUAKE3.bat / BUILD_OQUAKE3.sh for automated build steps.
 */

#include "oquake3_ogengine_integration.h"
#include "ogengine_sync.h"

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <time.h>

#ifdef _WIN32
#   include <windows.h>
#   define OQ3_PATH_SEP '\\'
#else
#   include <unistd.h>
#   include <limits.h>
#   define OQ3_PATH_SEP '/'
#endif

/* ---------------------------------------------------------------------------
 * Quake3e compatibility macros
 * Q3 uses Com_Printf for console output, Q_strlcpy/Q_snprintf for strings.
 * When compiled outside the Q3 tree (standalone), provide fallbacks.
 * --------------------------------------------------------------------------- */

#ifndef Q3_INTEGRATED
/* Standalone build — use standard C I/O */
#   define Q3_Com_Printf    printf
#   define Q3_Q_strlcpy(d, s, n)  strncpy(d, s, (n)-1), (d)[(n)-1] = '\0'
#   define Q3_Q_snprintf    snprintf
#   define Q3_Q_strcasecmp  strcasecmp
#else
/* Integrated Q3 tree — alias to engine functions */
#   define Q3_Com_Printf    Com_Printf
#   define Q3_Q_strlcpy     Q_strlcpy
#   define Q3_Q_snprintf    Q_snprintf
#   define Q3_Q_strcasecmp  Q_stricmp
#endif

/* ---------------------------------------------------------------------------
 * Version
 * --------------------------------------------------------------------------- */
#define OQUAKE3_STAR_VERSION     "1.0.0"
#define OQUAKE3_THING_TYPE_MIN   7000
#define OQUAKE3_THING_TYPE_MAX   7899
#define OQUAKE3_GAME_SOURCE      "Quake3"
#define OQUAKE3_GAME_SOURCE_TAG  "OQUAKE3"

/* ---------------------------------------------------------------------------
 * Config path search order
 * --------------------------------------------------------------------------- */
static const char* const OQ3_CONFIG_SEARCH_PATHS[] = {
    "oasisstar.json",
    "baseq3/oasisstar.json",
    "../oasisstar.json",
    "../../oasisstar.json",
    NULL
};

/* ---------------------------------------------------------------------------
 * Global state
 * --------------------------------------------------------------------------- */
static int   g_star_initialized           = 0;
static int   g_star_beamed_in             = 0;
static int   g_star_async_auth_pending    = 0;
static char  g_star_username[256]         = {0};
static char  g_json_config_path[1024]     = {0};

/* Persisted session values (loaded from / saved to oasisstar.json) */
static char  g_oq3_saved_username[256]    = {0};
static char  g_oq3_saved_jwt[2048]        = {0};
static char  g_oq3_saved_refresh[512]     = {0};

/* Async operation flags */
static int   g_star_profile_loaded_pending  = 0;
static int   g_inventory_refresh_pending    = 0;

/* Auth timeout */
static time_t g_auth_start_time            = 0;
#define OQ3_AUTH_TIMEOUT_SEC 30

/* Toast state */
static char  g_oq3_toast_message[512]      = {0};
static int   g_oq3_toast_frames            = 0;
#define OQ3_TOAST_DURATION_FRAMES 180

/* Popup state */
static int   g_quest_popup_open            = 0;
static int   g_inventory_open              = 0;

/* Config variables (loaded from oasisstar.json) */
static char  g_ogengine_url[512]           = "http://localhost:8888";
static char  g_oasis_api_url[512]          = "http://localhost:7777";
static int   g_offline_mode                = 0;
static int   g_stack_weapons               = 1;
static int   g_stack_armor                 = 1;
static int   g_stack_powerups              = 1;
static int   g_stack_runes                 = 1;
static int   g_stack_ammo                  = 0;
static int   g_mint_weapons                = 0;
static int   g_mint_armor                  = 0;
static int   g_mint_powerups               = 0;
static int   g_mint_runes                  = 0;
static int   g_max_health                  = 100;
static int   g_max_armor                   = 200;
static int   g_always_allow_pickup_if_max  = 1;
static int   g_always_add_items            = 0;
static int   g_use_health_on_pickup        = 0;
static int   g_use_armor_on_pickup         = 0;
static int   g_use_powerup_on_pickup       = 0;
static int   g_mint_monsters               = 1;
static char  g_nft_provider[128]           = "SolanaOASIS";
static char  g_send_to_address[256]        = {0};
static char  g_beamedin_avatar[256]        = {0};
static char  g_avatar_id[256]              = {0};

/* ---------------------------------------------------------------------------
 * Inventory entry (local cache of items picked up this session)
 * --------------------------------------------------------------------------- */
typedef struct {
    char name[256];
    char item_type[64];
    int  quantity;
    int  thing_type;
} oquake3_inventory_entry_t;

static oquake3_inventory_entry_t g_inventory_entries[256];
static int                       g_inventory_count = 0;

typedef enum {
    OQ3_TAB_RUNES = 0,
    OQ3_TAB_WEAPONS,
    OQ3_TAB_ARMOR,
    OQ3_TAB_HEALTH,
    OQ3_TAB_AMMO,
    OQ3_TAB_POWERUPS,
    OQ3_TAB_BOTS,
    OQ3_TAB_COUNT
} oquake3_inv_tab_t;

/* ---------------------------------------------------------------------------
 * Bot table
 * --------------------------------------------------------------------------- */
typedef struct {
    const char* engine_classname; /* Q3 classname or bot name */
    const char* config_key;       /* key in oasisstar.json for per-bot mint flag */
    const char* display_name;
    int         xp;
    int         is_boss;          /* Xaero is the final boss in SP mode */
    int         thing_type;
} oquake3_bot_entry_t;

static const oquake3_bot_entry_t OQUAKE3_BOTS[] = {
    { "bot_grunt",  "oquake3_grunt",  "Grunt Bot",  50,  0, OQUAKE3_THING_BOT_GRUNT },
    { "bot_klesk",  "oquake3_klesk",  "Klesk Bot",  75,  0, OQUAKE3_THING_BOT_KLESK },
    { "bot_xaero",  "oquake3_xaero",  "Xaero Bot",  200, 1, OQUAKE3_THING_BOT_XAERO },
    { "bot_orbb",   "oquake3_orbb",   "Orbb Bot",   60,  0, OQUAKE3_THING_BOT_ORBB  },
};
#define OQUAKE3_BOT_COUNT (int)(sizeof(OQUAKE3_BOTS) / sizeof(OQUAKE3_BOTS[0]))

/* Per-bot mint flags (loaded from oasisstar.json) */
static int g_oq3_mint_bot_flags[OQUAKE3_BOT_COUNT];

/* ---------------------------------------------------------------------------
 * Rune table (thing type -> canonical name)
 * --------------------------------------------------------------------------- */
typedef struct {
    const char* classname;     /* Q3 item classname */
    const char* canonical_name;
    int         thing_type;
} oquake3_rune_entry_t;

static const oquake3_rune_entry_t OQUAKE3_RUNES[] = {
    { "item_rune1", OQUAKE3_ITEM_RUNE_STRENGTH,     OQUAKE3_THING_RUNE_STRENGTH     },
    { "item_rune2", OQUAKE3_ITEM_RUNE_HASTE,        OQUAKE3_THING_RUNE_HASTE        },
    { "item_rune3", OQUAKE3_ITEM_RUNE_REGENERATION, OQUAKE3_THING_RUNE_REGENERATION },
    { "item_rune4", OQUAKE3_ITEM_RUNE_RESISTANCE,   OQUAKE3_THING_RUNE_RESISTANCE   },
};
#define OQUAKE3_RUNE_COUNT (int)(sizeof(OQUAKE3_RUNES) / sizeof(OQUAKE3_RUNES[0]))

/* ---------------------------------------------------------------------------
 * Weapon/item table (item_name -> thing_type)
 * --------------------------------------------------------------------------- */
typedef struct {
    const char* item_name;
    int         thing_type;
    const char* item_type;
} oquake3_item_map_t;

static const oquake3_item_map_t OQUAKE3_ITEM_MAP[] = {
    /* Weapons */
    { "Gauntlet",          OQUAKE3_THING_GAUNTLET,         "Weapon"  },
    { "Machinegun",        OQUAKE3_THING_MACHINEGUN,        "Weapon"  },
    { "Shotgun",           OQUAKE3_THING_SHOTGUN,           "Weapon"  },
    { "Grenade Launcher",  OQUAKE3_THING_GRENADE_LAUNCHER,  "Weapon"  },
    { "Rocket Launcher",   OQUAKE3_THING_ROCKET_LAUNCHER,   "Weapon"  },
    { "Lightning Gun",     OQUAKE3_THING_LIGHTNING_GUN,     "Weapon"  },
    { "Railgun",           OQUAKE3_THING_RAILGUN,           "Weapon"  },
    { "Plasma Gun",        OQUAKE3_THING_PLASMA_GUN,        "Weapon"  },
    { "BFG",               OQUAKE3_THING_BFG,               "Weapon"  },
    /* Armor */
    { "Armor Shard",       OQUAKE3_THING_ARMOR_SHARD,       "Armor"   },
    { "Yellow Armor",      OQUAKE3_THING_YELLOW_ARMOR,      "Armor"   },
    { "Red Armor",         OQUAKE3_THING_RED_ARMOR,         "Armor"   },
    /* Health */
    { "Small Health",      OQUAKE3_THING_SMALL_HEALTH,      "Health"  },
    { "Large Health",      OQUAKE3_THING_LARGE_HEALTH,      "Health"  },
    { "Mega Health",       OQUAKE3_THING_MEGA_HEALTH,       "Health"  },
    /* Ammo */
    { "Bullets",           OQUAKE3_THING_AMMO_BULLETS,      "Ammo"    },
    { "Shells",            OQUAKE3_THING_AMMO_SHELLS,        "Ammo"    },
    { "Grenades",          OQUAKE3_THING_AMMO_GRENADES,     "Ammo"    },
    { "Rockets",           OQUAKE3_THING_AMMO_ROCKETS,      "Ammo"    },
    { "Lightning",         OQUAKE3_THING_AMMO_LIGHTNING,    "Ammo"    },
    { "Slugs",             OQUAKE3_THING_AMMO_SLUGS,        "Ammo"    },
    { "Plasma",            OQUAKE3_THING_AMMO_PLASMA,       "Ammo"    },
    { "BFG Ammo",          OQUAKE3_THING_AMMO_BFG,          "Ammo"    },
    /* Powerups */
    { "Quad Damage",       OQUAKE3_THING_QUAD_DAMAGE,       "Powerup" },
    { "Battle Suit",       OQUAKE3_THING_BATTLE_SUIT,       "Powerup" },
    { "Haste",             OQUAKE3_THING_HASTE,             "Powerup" },
    { "Invisibility",      OQUAKE3_THING_INVISIBILITY,      "Powerup" },
    { "Regeneration",      OQUAKE3_THING_REGENERATION,      "Powerup" },
};
#define OQUAKE3_ITEM_MAP_COUNT (int)(sizeof(OQUAKE3_ITEM_MAP) / sizeof(OQUAKE3_ITEM_MAP[0]))

/* ---------------------------------------------------------------------------
 * Helper: find item in map
 * --------------------------------------------------------------------------- */
static const oquake3_item_map_t* OQ3_FindItemMap(const char* item_name)
{
    int i;
    if (!item_name) return NULL;
    for (i = 0; i < OQUAKE3_ITEM_MAP_COUNT; i++) {
        if (strcmp(OQUAKE3_ITEM_MAP[i].item_name, item_name) == 0)
            return &OQUAKE3_ITEM_MAP[i];
    }
    return NULL;
}

/* ---------------------------------------------------------------------------
 * Helper: find rune by classname
 * --------------------------------------------------------------------------- */
static const oquake3_rune_entry_t* OQ3_FindRune(const char* classname)
{
    int i;
    if (!classname) return NULL;
    for (i = 0; i < OQUAKE3_RUNE_COUNT; i++) {
        if (strcmp(OQUAKE3_RUNES[i].classname, classname) == 0)
            return &OQUAKE3_RUNES[i];
        if (strcmp(OQUAKE3_RUNES[i].canonical_name, classname) == 0)
            return &OQUAKE3_RUNES[i];
    }
    return NULL;
}

/* ---------------------------------------------------------------------------
 * Helper: find bot in table
 * --------------------------------------------------------------------------- */
static const oquake3_bot_entry_t* OQ3_FindBot(const char* classname)
{
    int i;
    if (!classname) return NULL;
    for (i = 0; i < OQUAKE3_BOT_COUNT; i++) {
        if (strcmp(OQUAKE3_BOTS[i].engine_classname, classname) == 0)
            return &OQUAKE3_BOTS[i];
        if (strcmp(OQUAKE3_BOTS[i].display_name, classname) == 0)
            return &OQUAKE3_BOTS[i];
    }
    return NULL;
}

/* ---------------------------------------------------------------------------
 * Helper: track item in local cache
 * --------------------------------------------------------------------------- */
static void OQ3_TrackInventoryEntry(const char* name, const char* item_type, int quantity, int thing_type)
{
    int i;
    for (i = 0; i < g_inventory_count; i++) {
        if (strcmp(g_inventory_entries[i].name, name) == 0) {
            g_inventory_entries[i].quantity += quantity;
            return;
        }
    }
    if (g_inventory_count < 256) {
        Q3_Q_strlcpy(g_inventory_entries[g_inventory_count].name, name, 256);
        Q3_Q_strlcpy(g_inventory_entries[g_inventory_count].item_type, item_type, 64);
        g_inventory_entries[g_inventory_count].quantity   = quantity;
        g_inventory_entries[g_inventory_count].thing_type = thing_type;
        g_inventory_count++;
    }
}

/* ---------------------------------------------------------------------------
 * Helper: set toast
 * --------------------------------------------------------------------------- */
static void OQ3_SetToast(const char* msg)
{
    Q3_Q_strlcpy(g_oq3_toast_message, msg, (int)sizeof(g_oq3_toast_message));
    g_oq3_toast_frames = OQ3_TOAST_DURATION_FRAMES;
}

static void oasis_open_url(const char *url)
{
    if (!url || !url[0]) return;
#ifdef _WIN32
    { char _cmd[512]; snprintf(_cmd, sizeof(_cmd), "start \"\" \"%s\"", url); (void)system(_cmd); }
#elif defined(__APPLE__)
    { char _cmd[512]; snprintf(_cmd, sizeof(_cmd), "open \"%s\"", url); (void)system(_cmd); }
#else
    { char _cmd[512]; snprintf(_cmd, sizeof(_cmd), "xdg-open \"%s\" &", url); (void)system(_cmd); }
#endif
}

/* ---------------------------------------------------------------------------
 * Simple JSON value extractor (same pattern as OQuake2)
 * --------------------------------------------------------------------------- */
static int OQ3_ExtractJsonValue(const char* json, const char* key, char* out, int out_size)
{
    char search[256];
    const char* p;
    const char* start;
    const char* end;
    int len;

    Q3_Q_snprintf(search, sizeof(search), "\"%s\"", key);
    p = strstr(json, search);
    if (!p) return 0;

    p += strlen(search);
    while (*p == ' ' || *p == '\t' || *p == ':' || *p == ' ') p++;
    if (!*p) return 0;

    if (*p == '"') {
        p++;
        start = p;
        end = strchr(p, '"');
        if (!end) return 0;
        len = (int)(end - start);
        if (len >= out_size) len = out_size - 1;
        memcpy(out, start, len);
        out[len] = '\0';
        return 1;
    } else {
        start = p;
        end = p;
        while (*end && *end != ',' && *end != '}' && *end != '\n') end++;
        len = (int)(end - start);
        while (len > 0 && (start[len-1] == ' ' || start[len-1] == '\t' || start[len-1] == '\r')) len--;
        if (len >= out_size) len = out_size - 1;
        memcpy(out, start, len);
        out[len] = '\0';
        return 1;
    }
}

/* ---------------------------------------------------------------------------
 * Config file search
 * --------------------------------------------------------------------------- */
static int OQ3_FindConfigFile(char* out_path, int out_size)
{
    int i;
    char try_path[1024];

    /* Executable-relative search (Windows) */
#ifdef _WIN32
    {
        char exe_dir[MAX_PATH];
        DWORD len = GetModuleFileNameA(NULL, exe_dir, MAX_PATH);
        if (len > 0) {
            char* last_sep = strrchr(exe_dir, '\\');
            if (last_sep) *last_sep = '\0';

            for (i = 0; OQ3_CONFIG_SEARCH_PATHS[i]; i++) {
                Q3_Q_snprintf(try_path, sizeof(try_path), "%s\\%s", exe_dir, OQ3_CONFIG_SEARCH_PATHS[i]);
                {
                    FILE* f = fopen(try_path, "r");
                    if (f) { fclose(f); Q3_Q_strlcpy(out_path, try_path, out_size); return 1; }
                }
            }
        }
    }
#else
    /* /proc/self/exe search (Linux) */
    {
        char exe_dir[PATH_MAX];
        ssize_t len = readlink("/proc/self/exe", exe_dir, PATH_MAX - 1);
        if (len > 0) {
            char* last_sep;
            exe_dir[len] = '\0';
            last_sep = strrchr(exe_dir, '/');
            if (last_sep) *last_sep = '\0';

            for (i = 0; OQ3_CONFIG_SEARCH_PATHS[i]; i++) {
                Q3_Q_snprintf(try_path, sizeof(try_path), "%s/%s", exe_dir, OQ3_CONFIG_SEARCH_PATHS[i]);
                {
                    FILE* f = fopen(try_path, "r");
                    if (f) { fclose(f); Q3_Q_strlcpy(out_path, try_path, out_size); return 1; }
                }
            }
        }
    }
#endif

    /* Relative path search */
    for (i = 0; OQ3_CONFIG_SEARCH_PATHS[i]; i++) {
        FILE* f = fopen(OQ3_CONFIG_SEARCH_PATHS[i], "r");
        if (f) {
            fclose(f);
            Q3_Q_strlcpy(out_path, OQ3_CONFIG_SEARCH_PATHS[i], out_size);
            return 1;
        }
    }

    /* Default path */
    Q3_Q_strlcpy(out_path, "oasisstar.json", out_size);
    return 0;
}

/* ---------------------------------------------------------------------------
 * Load oasisstar.json
 * --------------------------------------------------------------------------- */
static void OQ3_LoadJsonConfig(const char* path)
{
    FILE* f;
    long  file_size;
    char* buf;
    char  val[512];
    int   i;

    f = fopen(path, "r");
    if (!f) {
        Q3_Com_Printf("[OQuake3-STAR] oasisstar.json not found: %s\n", path);
        return;
    }

    fseek(f, 0, SEEK_END);
    file_size = ftell(f);
    fseek(f, 0, SEEK_SET);
    if (file_size <= 0 || file_size > 1024 * 1024) {
        Q3_Com_Printf("[OQuake3-STAR] oasisstar.json too large or empty: %s\n", path);
        fclose(f);
        return;
    }

    buf = (char*)malloc((size_t)file_size + 1);
    if (!buf) { fclose(f); return; }
    buf[fread(buf, 1, (size_t)file_size, f)] = '\0';
    fclose(f);

    if (OQ3_ExtractJsonValue(buf, "ogengine_url",    g_ogengine_url,  sizeof(g_ogengine_url)))  { /* loaded */ }
    if (OQ3_ExtractJsonValue(buf, "oasis_api_url",   g_oasis_api_url, sizeof(g_oasis_api_url))) { /* loaded */ }
    if (OQ3_ExtractJsonValue(buf, "nft_provider",    g_nft_provider,  sizeof(g_nft_provider)))  { /* loaded */ }
    if (OQ3_ExtractJsonValue(buf, "send_to_address_after_minting", g_send_to_address, sizeof(g_send_to_address))) { /* loaded */ }
    if (OQ3_ExtractJsonValue(buf, "beamedin_avatar", g_beamedin_avatar, sizeof(g_beamedin_avatar))) { /* loaded */ }
    if (OQ3_ExtractJsonValue(buf, "avatar_id",       g_avatar_id, sizeof(g_avatar_id)))         { /* loaded */ }

    if (OQ3_ExtractJsonValue(buf, "saved_jwt",       g_oq3_saved_jwt,     sizeof(g_oq3_saved_jwt)))     { /* loaded */ }
    if (OQ3_ExtractJsonValue(buf, "refresh_token",   g_oq3_saved_refresh, sizeof(g_oq3_saved_refresh))) { /* loaded */ }

    if (OQ3_ExtractJsonValue(buf, "offline_mode",               val, sizeof(val))) g_offline_mode              = atoi(val);
    if (OQ3_ExtractJsonValue(buf, "stack_weapons",              val, sizeof(val))) g_stack_weapons             = atoi(val);
    if (OQ3_ExtractJsonValue(buf, "stack_armor",                val, sizeof(val))) g_stack_armor               = atoi(val);
    if (OQ3_ExtractJsonValue(buf, "stack_powerups",             val, sizeof(val))) g_stack_powerups            = atoi(val);
    if (OQ3_ExtractJsonValue(buf, "stack_keys",                 val, sizeof(val))) g_stack_runes               = atoi(val);
    if (OQ3_ExtractJsonValue(buf, "stack_ammo",                 val, sizeof(val))) g_stack_ammo                = atoi(val);
    if (OQ3_ExtractJsonValue(buf, "mint_weapons",               val, sizeof(val))) g_mint_weapons              = atoi(val);
    if (OQ3_ExtractJsonValue(buf, "mint_armor",                 val, sizeof(val))) g_mint_armor                = atoi(val);
    if (OQ3_ExtractJsonValue(buf, "mint_powerups",              val, sizeof(val))) g_mint_powerups             = atoi(val);
    if (OQ3_ExtractJsonValue(buf, "mint_keys",                  val, sizeof(val))) g_mint_runes                = atoi(val);
    if (OQ3_ExtractJsonValue(buf, "max_health",                 val, sizeof(val))) g_max_health                = atoi(val);
    if (OQ3_ExtractJsonValue(buf, "max_armor",                  val, sizeof(val))) g_max_armor                 = atoi(val);
    if (OQ3_ExtractJsonValue(buf, "always_allow_pickup_if_max", val, sizeof(val))) g_always_allow_pickup_if_max = atoi(val);
    if (OQ3_ExtractJsonValue(buf, "always_add_items_to_inventory", val, sizeof(val))) g_always_add_items       = atoi(val);
    if (OQ3_ExtractJsonValue(buf, "use_health_on_pickup",       val, sizeof(val))) g_use_health_on_pickup      = atoi(val);
    if (OQ3_ExtractJsonValue(buf, "use_armor_on_pickup",        val, sizeof(val))) g_use_armor_on_pickup       = atoi(val);
    if (OQ3_ExtractJsonValue(buf, "use_powerup_on_pickup",      val, sizeof(val))) g_use_powerup_on_pickup     = atoi(val);
    if (OQ3_ExtractJsonValue(buf, "mint_monsters",              val, sizeof(val))) g_mint_monsters             = atoi(val);

    /* Per-bot mint flags */
    for (i = 0; i < OQUAKE3_BOT_COUNT; i++) {
        char key[256];
        Q3_Q_snprintf(key, sizeof(key), "mint_monster_%s", OQUAKE3_BOTS[i].config_key);
        if (OQ3_ExtractJsonValue(buf, key, val, sizeof(val)))
            g_oq3_mint_bot_flags[i] = atoi(val);
        else
            g_oq3_mint_bot_flags[i] = g_mint_monsters;
    }

    free(buf);
    Q3_Com_Printf("[OQuake3-STAR] Loaded config: %s\n", path);
}

/* ---------------------------------------------------------------------------
 * Save oasisstar.json (persist session data + per-bot flags)
 * --------------------------------------------------------------------------- */
static void OQ3_SaveStarConfigToFile(const char* path)
{
    FILE* f;
    char  cur_jwt[2048]   = {0};
    char  cur_refresh[512] = {0};
    char  cur_user[256]   = {0};
    int   i;

    ogengine_get_current_jwt(cur_jwt, sizeof(cur_jwt));
    ogengine_get_current_refresh_token(cur_refresh, sizeof(cur_refresh));
    ogengine_get_current_username(cur_user, sizeof(cur_user));

    if (!cur_jwt[0] && g_oq3_saved_jwt[0])
        Q3_Q_strlcpy(cur_jwt, g_oq3_saved_jwt, sizeof(cur_jwt));
    if (!cur_refresh[0] && g_oq3_saved_refresh[0])
        Q3_Q_strlcpy(cur_refresh, g_oq3_saved_refresh, sizeof(cur_refresh));
    if (!cur_user[0] && g_oq3_saved_username[0])
        Q3_Q_strlcpy(cur_user, g_oq3_saved_username, sizeof(cur_user));

    f = fopen(path, "w");
    if (!f) {
        Q3_Com_Printf("[OQuake3-STAR] Failed to save config: %s\n", path);
        return;
    }

    fprintf(f, "{\n");
    fprintf(f, "  \"config_file\": \"json\",\n");
    fprintf(f, "  \"star_transport\": \"remote\",\n");
    fprintf(f, "  \"ogengine_url\": \"%s\",\n",  g_ogengine_url);
    fprintf(f, "  \"oasis_api_url\": \"%s\",\n", g_oasis_api_url);
    fprintf(f, "  \"oasis_dna_path\": \"\",\n");
    fprintf(f, "  \"beam_face\": 1,\n");
    fprintf(f, "  \"stack_armor\": %d,\n",           g_stack_armor);
    fprintf(f, "  \"stack_weapons\": %d,\n",         g_stack_weapons);
    fprintf(f, "  \"stack_powerups\": %d,\n",        g_stack_powerups);
    fprintf(f, "  \"stack_keys\": %d,\n",            g_stack_runes);
    fprintf(f, "  \"mint_weapons\": %d,\n",          g_mint_weapons);
    fprintf(f, "  \"mint_armor\": %d,\n",            g_mint_armor);
    fprintf(f, "  \"mint_powerups\": %d,\n",         g_mint_powerups);
    fprintf(f, "  \"mint_keys\": %d,\n",             g_mint_runes);
    fprintf(f, "  \"max_health\": %d,\n",            g_max_health);
    fprintf(f, "  \"max_armor\": %d,\n",             g_max_armor);
    fprintf(f, "  \"always_allow_pickup_if_max\": %d,\n", g_always_allow_pickup_if_max);
    fprintf(f, "  \"always_add_items_to_inventory\": %d,\n", g_always_add_items);
    fprintf(f, "  \"use_health_on_pickup\": %d,\n",  g_use_health_on_pickup);
    fprintf(f, "  \"use_armor_on_pickup\": %d,\n",   g_use_armor_on_pickup);
    fprintf(f, "  \"use_powerup_on_pickup\": %d,\n", g_use_powerup_on_pickup);
    fprintf(f, "  \"offline_mode\": %d,\n",          g_offline_mode);
    fprintf(f, "  \"nft_provider\": \"%s\",\n",      g_nft_provider);
    fprintf(f, "  \"send_to_address_after_minting\": \"%s\",\n", g_send_to_address);
    fprintf(f, "  \"beamedin_avatar\": \"%s\",\n",   g_beamedin_avatar);
    fprintf(f, "  \"avatar_id\": \"%s\",\n",          g_avatar_id);
    fprintf(f, "  \"saved_jwt\": \"%s\",\n",          cur_jwt);
    fprintf(f, "  \"refresh_token\": \"%s\",\n",     cur_refresh);
    fprintf(f, "  \"mint_monsters\": %d",            g_mint_monsters);

    /* Per-bot mint flags */
    for (i = 0; i < OQUAKE3_BOT_COUNT; i++) {
        fprintf(f, ",\n  \"mint_monster_%s\": %d", OQUAKE3_BOTS[i].config_key, g_oq3_mint_bot_flags[i]);
    }

    fprintf(f, "\n}\n");
    fclose(f);
    Q3_Com_Printf("[OQuake3-STAR] Config saved: %s\n", path);
}

/* ---------------------------------------------------------------------------
 * Auth callback
 * --------------------------------------------------------------------------- */
static void OQ3_OnAuthDone(ogengine_result_t result, void* user_data)
{
    (void)user_data;
    g_star_async_auth_pending = 0;

    if (result == OGENGINE_SUCCESS) {
        char uname[256] = {0};
        ogengine_get_current_username(uname, sizeof(uname));
        Q3_Q_strlcpy(g_star_username, uname, sizeof(g_star_username));
        Q3_Q_strlcpy(g_oq3_saved_username, uname, sizeof(g_oq3_saved_username));
        g_star_beamed_in = 1;
        ogengine_refresh_avatar_profile();
        ogengine_request_inventory_in_background();
        g_star_profile_loaded_pending  = 1;
        g_inventory_refresh_pending    = 1;
        OQ3_SetToast("OASIS: Beamed in!");
        Q3_Com_Printf("[OQuake3-STAR] Auth OK — beamed in as: %s\n", uname);
    } else {
        char err[512] = {0};
        ogengine_drain_error(err, sizeof(err));
        Q3_Com_Printf("[OQuake3-STAR] Auth FAILED: %s\n", err[0] ? err : "(unknown error)");
        OQ3_SetToast("OASIS: Beam-in failed.");
    }
}

/* ---------------------------------------------------------------------------
 * Operation callback
 * --------------------------------------------------------------------------- */
static void OQ3_OnOperationDone(ogengine_result_t result, int operation_type, void* user_data)
{
    (void)user_data;

    if (operation_type == OGENGINE_OP_PROFILE_LOADED) {
        g_star_profile_loaded_pending = 0;
        if (result == OGENGINE_SUCCESS)
            Q3_Com_Printf("[OQuake3-STAR] Avatar profile loaded.\n");
    } else if (operation_type == OGENGINE_OP_GET_INVENTORY) {
        g_inventory_refresh_pending = 0;
        if (result == OGENGINE_SUCCESS)
            Q3_Com_Printf("[OQuake3-STAR] Inventory refreshed.\n");
    }
}

/* ---------------------------------------------------------------------------
 * Init
 * --------------------------------------------------------------------------- */
void OQuake3_STAR_Init(void)
{
    ogengine_config_t cfg;
    ogengine_result_t r;
    int i;

    if (g_star_initialized) {
        Q3_Com_Printf("[OQuake3-STAR] Already initialized.\n");
        return;
    }

    ogengine_sync_init();

    memset(&cfg, 0, sizeof(cfg));
    memset(g_inventory_entries, 0, sizeof(g_inventory_entries));
    g_inventory_count = 0;
    for (i = 0; i < OQUAKE3_BOT_COUNT; i++) g_oq3_mint_bot_flags[i] = 1;

    /* Find and load config */
    OQ3_FindConfigFile(g_json_config_path, sizeof(g_json_config_path));
    OQ3_LoadJsonConfig(g_json_config_path);

    if (g_offline_mode) {
        Q3_Com_Printf("[OQuake3-STAR] Offline mode — STAR API disabled.\n");
        g_star_initialized = 1;
        return;
    }

    cfg.base_url           = g_ogengine_url;
    cfg.api_key            = "";
    cfg.avatar_id          = g_avatar_id[0] ? g_avatar_id : "";
    cfg.timeout_seconds    = 30;
    cfg.client_game_source = OQUAKE3_GAME_SOURCE_TAG;
    cfg.transport          = 0;
    cfg.oasis_dna_path     = "";

    r = ogengine_init(&cfg);
    if (r != OGENGINE_SUCCESS) {
        Q3_Com_Printf("[OQuake3-STAR] ogengine_init failed (%d). STAR disabled.\n", (int)r);
        ogengine_sync_cleanup();
        return;
    }

    ogengine_set_oasis_base_url(g_oasis_api_url);
    ogengine_set_callback((ogengine_callback_t)OQ3_OnAuthDone);
    ogengine_set_operation_callback((ogengine_operation_callback_t)OQ3_OnOperationDone);

    /* Restore saved session */
    if (g_oq3_saved_jwt[0]) {
        ogengine_set_saved_session(g_oq3_saved_jwt);
        if (g_oq3_saved_refresh[0])
            ogengine_set_refresh_token(g_oq3_saved_refresh);
        r = ogengine_restore_session();
        if (r == OGENGINE_SUCCESS) {
            char uname[256] = {0};
            ogengine_get_current_username(uname, sizeof(uname));
            Q3_Q_strlcpy(g_star_username, uname, sizeof(g_star_username));
            g_star_beamed_in = 1;
            ogengine_refresh_avatar_profile();
            ogengine_request_inventory_in_background();
            g_star_profile_loaded_pending  = 1;
            g_inventory_refresh_pending    = 1;
            Q3_Com_Printf("[OQuake3-STAR] Session restored — %s\n", uname);
        } else {
            Q3_Com_Printf("[OQuake3-STAR] Session restore failed — re-auth required.\n");
        }
    }

    g_star_initialized = 1;
    Q3_Com_Printf("[OQuake3-STAR] Initialized. Version %s\n", OQUAKE3_STAR_VERSION);
    Q3_Com_Printf("[OQuake3-STAR] OASIS thing type range: 7000-7899 (Quake III Arena)\n");
    Q3_Com_Printf("[OQuake3-STAR] Type 'star' in console to authenticate.\n");
}

/* ---------------------------------------------------------------------------
 * Cleanup
 * --------------------------------------------------------------------------- */
void OQuake3_STAR_Cleanup(void)
{
    if (!g_star_initialized) return;
    OQ3_SaveStarConfigToFile(g_json_config_path);
    ogengine_flush_pending();
    ogengine_cleanup();
    ogengine_sync_cleanup();
    g_star_initialized = 0;
    g_star_beamed_in   = 0;
    Q3_Com_Printf("[OQuake3-STAR] Shutdown complete.\n");
}

/* ---------------------------------------------------------------------------
 * Rune pickup
 * --------------------------------------------------------------------------- */
void OQuake3_STAR_OnRunePickup(const char* rune_name)
{
    const oquake3_rune_entry_t* rune;
    char desc[512];
    int  do_mint;

    if (!g_star_initialized || g_offline_mode) return;
    if (!rune_name) return;

    rune = OQ3_FindRune(rune_name);

    Q3_Q_snprintf(desc, sizeof(desc), "Rune: %s (Quake III)", rune ? rune->canonical_name : rune_name);
    do_mint = g_mint_runes;

    ogengine_queue_pickup_with_mint(
        rune ? rune->canonical_name : rune_name,
        desc,
        OQUAKE3_GAME_SOURCE,
        "Rune",
        do_mint,
        g_nft_provider,
        g_send_to_address,
        1
    );

    ogengine_queue_quest_progress_from_pickup(OQUAKE3_GAME_SOURCE, "Rune", rune ? rune->canonical_name : rune_name);

    OQ3_TrackInventoryEntry(rune ? rune->canonical_name : rune_name, "Rune", 1, rune ? rune->thing_type : OQUAKE3_THING_RUNE_STRENGTH);

    {
        char toast[256];
        Q3_Q_snprintf(toast, sizeof(toast), "OASIS: Rune added — %s", rune ? rune->canonical_name : rune_name);
        OQ3_SetToast(toast);
    }
    Q3_Com_Printf("[OQuake3-STAR] Rune pickup: %s\n", rune ? rune->canonical_name : rune_name);
}

/* ---------------------------------------------------------------------------
 * HasRune — cross-game inventory check
 * --------------------------------------------------------------------------- */
int OQuake3_STAR_HasRune(const char* rune_name)
{
    if (!g_star_initialized || g_offline_mode) return 0;
    return ogengine_has_item(rune_name) ? 1 : 0;
}

/* ---------------------------------------------------------------------------
 * Item pickup queue helper
 * --------------------------------------------------------------------------- */
static void OQ3_QueuePickup(const char* item_name, const char* item_type,
                             int quantity, const char* description, int do_mint)
{
    char desc_buf[512];
    if (!description || !description[0]) {
        Q3_Q_snprintf(desc_buf, sizeof(desc_buf), "%s (Quake III)", item_name);
        description = desc_buf;
    }

    ogengine_queue_pickup_with_mint(
        item_name,
        description,
        OQUAKE3_GAME_SOURCE,
        item_type,
        do_mint,
        g_nft_provider,
        g_send_to_address,
        quantity
    );
    ogengine_queue_quest_progress_from_pickup(OQUAKE3_GAME_SOURCE, item_type, item_name);
}

/* ---------------------------------------------------------------------------
 * OnItemPickup
 * --------------------------------------------------------------------------- */
void OQuake3_STAR_OnItemPickup(const char* item_name, const char* item_type,
                                int quantity, const char* description)
{
    const oquake3_item_map_t* entry;
    int do_mint = 0;

    if (!g_star_initialized || g_offline_mode) return;
    if (!item_name || !item_type) return;

    entry = OQ3_FindItemMap(item_name);

    if (strcmp(item_type, "Weapon")  == 0) do_mint = g_mint_weapons;
    else if (strcmp(item_type, "Armor")   == 0) do_mint = g_mint_armor;
    else if (strcmp(item_type, "Powerup") == 0) do_mint = g_mint_powerups;
    else if (strcmp(item_type, "Rune")    == 0) do_mint = g_mint_runes;

    OQ3_QueuePickup(item_name, item_type, quantity, description, do_mint);
    OQ3_TrackInventoryEntry(item_name, item_type, quantity, entry ? entry->thing_type : 0);
}

/* ---------------------------------------------------------------------------
 * OnPickupLeftOnFloor
 * --------------------------------------------------------------------------- */
void OQuake3_STAR_OnPickupLeftOnFloor(const char* item_name, const char* item_type,
                                       int quantity, const char* description)
{
    if (!g_star_initialized || g_offline_mode) return;
    if (!item_name) return;
    if (g_always_add_items) {
        OQ3_QueuePickup(item_name, item_type ? item_type : "Item", quantity, description, 0);
        Q3_Com_Printf("[OQuake3-STAR] Left-on-floor item added to OASIS: %s\n", item_name);
    }
}

/* ---------------------------------------------------------------------------
 * InterceptTouchPickupAtMax
 * --------------------------------------------------------------------------- */
int OQuake3_STAR_InterceptTouchPickupAtMax(void* item_ent, void* player_ent)
{
    (void)item_ent;
    (void)player_ent;
    /* Return 0 to let normal Q3 pickup logic proceed */
    return 0;
}

/* ---------------------------------------------------------------------------
 * OnBotKilled
 * --------------------------------------------------------------------------- */
void OQuake3_STAR_OnBotKilled(const char* bot_classname)
{
    const oquake3_bot_entry_t* bot;
    int   do_mint;
    int   bot_idx;

    if (!g_star_initialized || g_offline_mode) return;
    if (!bot_classname) return;

    bot = OQ3_FindBot(bot_classname);
    if (!bot) {
        /* Unknown bot — award generic XP */
        ogengine_queue_monster_kill(bot_classname, OQUAKE3_GAME_SOURCE, 25, 0, g_nft_provider, g_send_to_address, 0);
        Q3_Com_Printf("[OQuake3-STAR] Unknown bot killed: %s (25 XP)\n", bot_classname);
        return;
    }

    bot_idx = (int)(bot - OQUAKE3_BOTS);
    do_mint = (g_mint_monsters && bot_idx >= 0 && bot_idx < OQUAKE3_BOT_COUNT)
                ? g_oq3_mint_bot_flags[bot_idx] : 0;

    ogengine_queue_monster_kill(
        bot->display_name,
        OQUAKE3_GAME_SOURCE,
        bot->xp,
        do_mint,
        g_nft_provider,
        g_send_to_address,
        bot->is_boss
    );

    OQ3_TrackInventoryEntry(bot->display_name, "Bot", 1, bot->thing_type);

    {
        char toast[256];
        Q3_Q_snprintf(toast, sizeof(toast), "OASIS: +%d XP — %s%s",
                       bot->xp, bot->display_name, bot->is_boss ? " (BOSS!)" : "");
        OQ3_SetToast(toast);
    }
    Q3_Com_Printf("[OQuake3-STAR] Bot killed: %s | XP: %d | mint: %d\n",
                   bot->display_name, bot->xp, do_mint);
}

/* ---------------------------------------------------------------------------
 * OnPlayerFragged
 * --------------------------------------------------------------------------- */
void OQuake3_STAR_OnPlayerFragged(const char* victim_name, int is_bot)
{
    int xp;
    if (!g_star_initialized || g_offline_mode) return;
    if (!victim_name) return;

    xp = is_bot ? 30 : 50;  /* PvP frags worth more */
    ogengine_queue_monster_kill(victim_name, OQUAKE3_GAME_SOURCE, xp, 0, g_nft_provider, g_send_to_address, 0);
    Q3_Com_Printf("[OQuake3-STAR] Frag: %s | XP: %d\n", victim_name, xp);
}

/* ---------------------------------------------------------------------------
 * PollItems (frame pump)
 * --------------------------------------------------------------------------- */
void OQuake3_STAR_PollItems(void)
{
    char msg_buf[512];

    if (!g_star_initialized) return;
    if (g_offline_mode) return;

    ogengine_sync_pump();

    /* --- cross-game spawn poll --- */
    {
        char entity_id[128];
        float sx, sy, sz;
        if (ogengine_poll_spawn_event(entity_id, sizeof(entity_id), &sx, &sy, &sz))
        {
            Q3_Com_Printf("[OQuake3-STAR] OASIS SpawnEvent: %s at %.0f/%.0f/%.0f\n", entity_id, sx, sy, sz);
            {
                char spawn_cmd[192];
                Q3_Q_snprintf(spawn_cmd, sizeof(spawn_cmd), "spawn %s\n", entity_id);
                trap_SendConsoleCommand(EXEC_APPEND, spawn_cmd);
            }
            ogengine_confirm_spawn(entity_id);
        }
    }

    /* --- cross-game event poll --- */
    {
        char evt_json[4096];
        while (ogengine_poll_cross_game_event(evt_json, sizeof(evt_json)))
        {
            char evt_type[64] = "";
            OQ3_ExtractJsonValue(evt_json, "EventType", evt_type, sizeof(evt_type));
            if (strcmp(evt_type, "ShowNarration") == 0) {
                char narration[256] = "";
                OQ3_ExtractJsonValue(evt_json, "NarrationText", narration, sizeof(narration));
                if (narration[0]) OQ3_SetToast(narration);
            } else if (strcmp(evt_type, "PlayAudio") == 0) {
                char audio_title[128] = "", audio_url[256] = "";
                OQ3_ExtractJsonValue(evt_json, "AudioTitle", audio_title, sizeof(audio_title));
                OQ3_ExtractJsonValue(evt_json, "AudioUrl",   audio_url,   sizeof(audio_url));
                oasis_open_url(audio_url);
                if (audio_title[0]) OQ3_SetToast(audio_title);
                Q3_Com_Printf("[OQuake3-STAR] OASIS PlayAudio: %s → %s\n", audio_title, audio_url);
            } else if (strcmp(evt_type, "PlayVideo") == 0) {
                char video_title[128] = "", video_url[256] = "";
                OQ3_ExtractJsonValue(evt_json, "VideoTitle", video_title, sizeof(video_title));
                OQ3_ExtractJsonValue(evt_json, "VideoUrl",   video_url,   sizeof(video_url));
                oasis_open_url(video_url);
                if (video_title[0]) OQ3_SetToast(video_title);
                Q3_Com_Printf("[OQuake3-STAR] OASIS PlayVideo: %s → %s\n", video_title, video_url);
            } else if (strcmp(evt_type, "OpenWebsite") == 0) {
                char website_url[256] = "";
                OQ3_ExtractJsonValue(evt_json, "WebsiteUrl", website_url, sizeof(website_url));
                oasis_open_url(website_url);
                Q3_Com_Printf("[OQuake3-STAR] OASIS OpenWebsite: %s\n", website_url);
            } else if (strcmp(evt_type, "UnlockPortal") == 0) {
                char portal_id[64] = "";
                OQ3_ExtractJsonValue(evt_json, "PortalId", portal_id, sizeof(portal_id));
                Q3_Com_Printf("[OQuake3-STAR] OASIS UnlockPortal: %s — portal unlock not yet implemented\n", portal_id);
            }
        }
    }

    /* --- inventory grant poll --- */
    {
        char item_guid[64];
        while (ogengine_poll_inventory_grant(item_guid, sizeof(item_guid)))
        {
            Q3_Com_Printf("[OQuake3-STAR] OASIS InventoryGrant: %s — inventory refresh triggered\n", item_guid);
            ogengine_get_inventory(NULL);
        }
    }

    /* Auth timeout check */
    if (g_star_async_auth_pending) {
        time_t now = time(NULL);
        if (difftime(now, g_auth_start_time) > OQ3_AUTH_TIMEOUT_SEC) {
            g_star_async_auth_pending = 0;
            Q3_Com_Printf("[OQuake3-STAR] Auth timed out after %d seconds.\n", OQ3_AUTH_TIMEOUT_SEC);
            OQ3_SetToast("OASIS: Beam-in timed out.");
        }
    }

    /* Drain mint results */
    while (ogengine_drain_mint_result(msg_buf, sizeof(msg_buf)) > 0) {
        Q3_Com_Printf("[OQuake3-STAR] Mint: %s\n", msg_buf);
    }

    /* Drain errors */
    while (ogengine_drain_error(msg_buf, sizeof(msg_buf)) > 0) {
        Q3_Com_Printf("[OQuake3-STAR] Error: %s\n", msg_buf);
    }

    /* Drain logs */
    while (ogengine_drain_log(msg_buf, sizeof(msg_buf)) > 0) {
        Q3_Com_Printf("[OQuake3-STAR] Log: %s\n", msg_buf);
    }

    /* Toast decay */
    if (g_oq3_toast_frames > 0) g_oq3_toast_frames--;
}

/* ---------------------------------------------------------------------------
 * HUD draw stubs (Q3 has its own 2D HUD system via CG_DrawPic/CG_DrawString)
 * These are no-ops here — the real implementations belong in cgame/ code.
 * --------------------------------------------------------------------------- */
void OQuake3_STAR_DrawInventoryOverlay(oq3_cb_context_t* ctx) { (void)ctx; }
void OQuake3_STAR_DrawBeamedInStatus(oq3_cb_context_t* ctx)   { (void)ctx; }
void OQuake3_STAR_DrawQuestTracker(oq3_cb_context_t* ctx)     { (void)ctx; }
void OQuake3_STAR_DrawXpStatus(oq3_cb_context_t* ctx)         { (void)ctx; }
void OQuake3_STAR_DrawVersionStatus(oq3_cb_context_t* ctx)    { (void)ctx; }

void OQuake3_STAR_DrawToast(oq3_cb_context_t* ctx)
{
    (void)ctx;
    /* Toast message available in g_oq3_toast_message when g_oq3_toast_frames > 0 */
}

/* ---------------------------------------------------------------------------
 * Popup state queries
 * --------------------------------------------------------------------------- */
int OQuake3_STAR_IsQuestPopupOpen(void)     { return g_quest_popup_open; }
int OQuake3_STAR_IsInventoryPopupOpen(void) { return g_inventory_open; }

/* ---------------------------------------------------------------------------
 * Misc queries
 * --------------------------------------------------------------------------- */
int OQuake3_STAR_ShouldUseAnorakFace(void)
{
    return g_star_beamed_in ? 1 : 0;
}

const char* OQuake3_STAR_GetUsername(void)
{
    return g_star_username;
}

/* ---------------------------------------------------------------------------
 * Console command
 * --------------------------------------------------------------------------- */
void OQuake3_STAR_Console_f(void)
{
    if (!g_star_initialized) {
        Q3_Com_Printf("[OQuake3-STAR] Not initialized.\n");
        return;
    }

    if (g_star_beamed_in) {
        Q3_Com_Printf("[OQuake3-STAR] Beamed in as: %s\n", g_star_username);
        Q3_Com_Printf("  Inventory entries tracked: %d\n", g_inventory_count);
        Q3_Com_Printf("  OASIS thing type range: 7000-7899 (Quake III Arena)\n");
        Q3_Com_Printf("  Version: %s\n", OQUAKE3_STAR_VERSION);
    } else {
        Q3_Com_Printf("[OQuake3-STAR] Not beamed in. Usage: star <username> <password>\n");
        Q3_Com_Printf("  OASIS thing type range: 7000-7899 (Quake III Arena)\n");
    }
}

/* -------------------------------------------------------------------------
 * Cross-game teleportation
 * ------------------------------------------------------------------------- */

void OQuake3_STAR_CheckIncomingTeleport(void)
{
    char map[256];
    float x = 0, y = 0, z = 64;
    if (!ogengine_poll_teleport_request(map, sizeof(map), &x, &y, &z))
        return;
    Q3_Com_Printf("[OQuake3-STAR] OASIS Teleport arrive: map=%s pos=%.0f/%.0f/%.0f\n", map, x, y, z);
    {
        gentity_t *player_ent = &g_entities[0];
        player_ent->client->ps.origin[0] = x;
        player_ent->client->ps.origin[1] = y;
        player_ent->client->ps.origin[2] = z;
        trap_LinkEntity(player_ent);
    }
    ogengine_confirm_teleport_arrival();
}
