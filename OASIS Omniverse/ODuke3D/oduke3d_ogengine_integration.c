/**
 * ODuke3D - OASIS STAR API Integration Implementation
 *
 * Integrates EDuke32 (classic Duke Nukem 3D) with the OASIS STAR API.
 * Blue/Red/Yellow key cards share cross-game with ODOOM, OQuake, ODOOM3,
 * ODOOM3-BFG, and ODuke3D-RT.  Monster kills award XP and optional NFT mints.
 *
 * Integration hook sites in source/duke3d/src/:
 *   app_main() and G_GameExit()         → ODuke3D_STAR_Init / Cleanup
 *   G_Tics()                            → ODuke3D_STAR_Tick
 *   P_CheckInventory() key card path    → ODuke3D_STAR_OnKeyPickup
 *   G_OperateSectors() locked-door path → ODuke3D_STAR_CheckDoorAccess
 *   A_DamageObject() when extra <= 0    → ODuke3D_STAR_OnActorKilled
 *   G_DrawRooms() / display-rest        → ODuke3D_STAR_DrawHUDStatus
 *                                          ODuke3D_STAR_DrawPopupOverlay
 *   G_ProcessInput() / KB path          → ODuke3D_STAR_HandleKey
 *   G_DrawStatusBar() face tile select  → ODuke3D_STAR_ShouldUseAvatarFace
 *   P_ProcessInput() movement guard     → ODuke3D_STAR_ShouldBlockInput
 *
 * EDuke32 actor identification: enemies are tiles; this file maps picnum values
 * to canonical names which are then looked up in the JSON monster table.
 *
 * Compiled as part of the EDuke32 game DLL / executable.
 * Depends on: compat.h, OSD, printext256, names.h (for picnum #defines).
 */

#include <stdarg.h>
#include <stdlib.h>
#include <string.h>

/* EDuke32 headers (available inside the source tree) */
#include "compat.h"    /* Bsnprintf, Bstrncpy, Bstrcmp, etc.   */
/* printext256 forward declaration — stable EDuke32 drawing API */
extern void printext256(int32_t xpos, int32_t ypos, int16_t col,
                        int16_t backcol, const char *name, uint8_t fontsize);
/* OSD console output */
extern int OSD_Printf(const char *fmt, ...);

/* EDuke32 names.h picnum constants */
#include "names.h"

#include "oduke3d_ogengine_integration.h"

/* STAR API (copied by build script) */
#include "ogengine.h"
#include "ogengine_sync.h"

/* OGLib: single-TU implementation */
#define OGLIB_SESSION_IMPL
#define OGLIB_MONSTER_IMPL
#include "OGLib/oglib.h"

/* EDuke32 scan code constants (from compat.h / input.h).
   Fallback literals match standard PC scan codes. */
#ifndef sc_I
# define sc_I       23
#endif
#ifndef sc_Q
# define sc_Q       16
#endif
#ifndef sc_Escape
# define sc_Escape   1
#endif
#ifndef sc_UpArrow
# define sc_UpArrow 72
#endif
#ifndef sc_DownArrow
# define sc_DownArrow 80
#endif
#ifndef sc_U
# define sc_U       22
#endif
#ifndef sc_A
# define sc_A        4
#endif
#ifndef sc_C
# define sc_C       46
#endif

/*===========================================================================
 * Constants and module state
 *=========================================================================*/

#define ODUKE3D_GAME_SOURCE   "ODUKE3D"
#define ODUKE3D_LOG_TAG       "[DUKE3D] "
#define ODUKE3D_VERSION_STR   "ODuke3D 1.0.0"
#define ODUKE3D_MAX_PATH      512
#define ODUKE3D_TOAST_TICKS   180   /* ~3 s at 60 tics/s */
#define ODUKE3D_INV_MAX       32
#define ODUKE3D_QUEST_MAX     16

static int  g_initialized    = 0;
static int  g_client_ready   = 0;
static int  g_debug          = 1;
static char g_json_path[ODUKE3D_MAX_PATH];
static char g_username[128];
static int  g_xp             = 0;
static int  g_beamed_in      = 0;

/* Popup state */
static int  g_inv_popup_open   = 0;
static int  g_quest_popup_open = 0;
static int  g_inv_selected     = 0;
static int  g_quest_selected   = 0;
static int  g_inv_count        = 0;
static int  g_quest_count      = 0;
static char g_inv_names[ODUKE3D_INV_MAX][64];
static char g_quest_names[ODUKE3D_QUEST_MAX][64];
static char g_quest_descs[ODUKE3D_QUEST_MAX][128];

/* Toast */
static char g_toast_msg[128];
static int  g_toast_ticks = 0;

/* Monster table loaded from oasisstar.json "oduke3d" section */
static oglib_monster_table_t g_monster_table;

/*===========================================================================
 * Picnum → canonical name mapping
 *=========================================================================*/

static const struct { int picnum; const char *name; } s_picnum_map[] = {
    { LIZTROOP,   "liztroop"   },
    { PIGCOP,     "pigcop"     },
    { OCTABRAIN,  "octabrain"  },
    { COMMANDER,  "commander"  },
    { DRONE,      "drone"      },
    { SENTRY,     "sentry"     },
    { NEWBEAST,   "newbeast"   },
    { GREENSLIME, "greenslime" },
    { RECON,      "recon"      },
    { LIZMAN,     "lizman"     },
    { ROTATEGUN,  "rotategun"  },
    { BOSS1,      "boss1"      },
    { BOSS2,      "boss2"      },
    { BOSS3,      "boss3"      },
    { BOSS4,      "boss4"      },
    { BOSS5,      "boss5"      },
    { 0, NULL }
};

static const char *picnum_to_name(int picnum) {
    for (int i = 0; s_picnum_map[i].name; i++)
        if (s_picnum_map[i].picnum == picnum) return s_picnum_map[i].name;
    return NULL;
}

/*===========================================================================
 * Internal helpers
 *=========================================================================*/

static void oduke3d_log(const char *fmt, ...) {
    if (!g_debug) return;
    char buf[512];
    va_list ap;
    va_start(ap, fmt);
    vsnprintf(buf, sizeof(buf), fmt, ap);
    va_end(ap);
    OSD_Printf("%s%s\n", ODUKE3D_LOG_TAG, buf);
}

static void set_toast(const char *msg) {
    Bstrncpy(g_toast_msg, msg, sizeof(g_toast_msg) - 1);
    g_toast_ticks = ODUKE3D_TOAST_TICKS;
}

static void close_all_popups(void) {
    g_inv_popup_open   = 0;
    g_quest_popup_open = 0;
}

static void refresh_inventory(void) {
    g_inv_count = 0;
    /* TODO: call ogengine_sync_get_inventory(&items, &count) once star_sync exposes it */
}

static void refresh_quests(void) {
    g_quest_count = 0;
    /* TODO: call ogengine_sync_get_quests(&quests, &count) once star_sync exposes it */
}

/*===========================================================================
 * Lifecycle
 *=========================================================================*/

void ODuke3D_STAR_Init(void) {
    if (g_initialized) return;

    Bsnprintf(g_json_path, sizeof(g_json_path), "oasisstar.json");

    const char *env_user   = getenv("STAR_USERNAME");
    const char *env_pass   = getenv("STAR_PASSWORD");
    const char *env_key    = getenv("OGENGINE_KEY");
    const char *env_avatar = getenv("STAR_AVATAR_ID");

    oglib_monster_table_init(&g_monster_table);
    oglib_monster_table_load_from_oasisstar(g_json_path, "oduke3d", &g_monster_table);

    StarApiConfig cfg;
    memset(&cfg, 0, sizeof(cfg));
    cfg.api_url     = "https://star-api.oasisplatform.world/api";
    cfg.oasis_url   = "https://api.oasisplatform.world";
    cfg.game_source = ODUKE3D_GAME_SOURCE;
    cfg.username    = env_user   ? env_user   : "";
    cfg.password    = env_pass   ? env_pass   : "";
    cfg.api_key     = env_key    ? env_key    : "";
    cfg.avatar_id   = env_avatar ? env_avatar : "";

    if (StarApi_Init(&cfg) == 0) {
        g_client_ready = 1;
        Bstrncpy(g_username, env_user ? env_user : "", sizeof(g_username) - 1);
        g_beamed_in = 1;
        OSD_Printf(ODUKE3D_LOG_TAG "OASIS STAR API: Authenticated. Cross-game keys enabled.\n");
    } else {
        OSD_Printf(ODUKE3D_LOG_TAG "OASIS STAR API: Not authenticated. "
                   "Set STAR_USERNAME/STAR_PASSWORD or OGENGINE_KEY/STAR_AVATAR_ID.\n");
    }

    ogengine_sync_init();
    g_initialized = 1;
    OSD_Printf(ODUKE3D_LOG_TAG "%s initialised.\n", ODUKE3D_VERSION_STR);
}

void ODuke3D_STAR_Cleanup(void) {
    if (!g_initialized) return;
    ogengine_sync_shutdown();
    StarApi_Shutdown();
    g_initialized  = 0;
    g_client_ready = 0;
}

void ODuke3D_STAR_Tick(void) {
    if (!g_initialized) return;
    ogengine_sync_tick(NULL);

    /* --- cross-game spawn poll --- */
    {
        char entity_id[128];
        float sx, sy, sz;
        if (ogengine_poll_spawn_event(entity_id, sizeof(entity_id), &sx, &sy, &sz))
        {
            /* TODO: spawn entity by entity_id at sx/sy/sz via game's native spawn API.
             * Map entity_id to native classname using OGAsset catalog lookup.
             * For now, log the request. */
            oglib_log(OGLIB_LOG_INFO, "OASIS SpawnEvent: %s at %.0f/%.0f/%.0f", entity_id, sx, sy, sz);
            ogengine_confirm_spawn(entity_id);
        }
    }

    if (g_toast_ticks > 0) g_toast_ticks--;
}

/*===========================================================================
 * Cross-game events
 *=========================================================================*/

void ODuke3D_STAR_OnKeyPickup(const char *key_type) {
    if (!g_client_ready || !key_type) return;
    ogengine_sync_on_item_pickup(key_type, "Key", 1);
    char toast[128];
    Bsnprintf(toast, sizeof(toast), "Key added to OASIS: %s", key_type);
    set_toast(toast);
    oduke3d_log("Key pickup: %s", key_type);
}

int ODuke3D_STAR_CheckDoorAccess(const char *key_type) {
    if (!g_client_ready || !key_type) return 0;
    int result = ogengine_sync_check_door_access(key_type);
    if (result) {
        char toast[128];
        Bsnprintf(toast, sizeof(toast), "Cross-game key used: %s", key_type);
        set_toast(toast);
        oduke3d_log("Cross-game door access granted: %s", key_type);
    }
    return result;
}

void ODuke3D_STAR_OnActorKilled(int picnum, int engine_is_boss) {
    if (!g_client_ready) return;
    const char *name = picnum_to_name(picnum);
    if (!name) return;

    const oglib_monster_entry_t *entry = oglib_monster_find(&g_monster_table, name);
    if (!entry) return;

    int xp_award = entry->xp;
    int is_boss  = entry->is_boss || engine_is_boss;

    g_xp += xp_award;
    ogengine_sync_on_monster_killed(name, xp_award, is_boss, entry->do_mint);

    char toast[128];
    Bsnprintf(toast, sizeof(toast), "+%d XP  %s", xp_award, entry->display_name);
    set_toast(toast);

    if (is_boss)
        oduke3d_log("BOSS killed: %s (+%d XP)", entry->display_name, xp_award);
    else
        oduke3d_log("Kill: %s (+%d XP)", entry->display_name, xp_award);
}

/*===========================================================================
 * HUD / GUI state queries
 *=========================================================================*/

int         ODuke3D_STAR_IsBeamedIn(void)           { return g_beamed_in; }
const char *ODuke3D_STAR_GetUsername(void)           { return g_username; }
int         ODuke3D_STAR_GetXP(void)                { return g_xp; }
const char *ODuke3D_STAR_GetVersionString(void)      { return ODUKE3D_VERSION_STR; }
int         ODuke3D_STAR_IsInventoryPopupOpen(void)  { return g_inv_popup_open; }
int         ODuke3D_STAR_IsQuestPopupOpen(void)      { return g_quest_popup_open; }
int         ODuke3D_STAR_ShouldBlockInput(void)      { return g_inv_popup_open || g_quest_popup_open; }
int         ODuke3D_STAR_ShouldUseAvatarFace(void)   { return g_beamed_in; }

void ODuke3D_STAR_ToggleInventoryPopup(void) {
    if (g_inv_popup_open) {
        g_inv_popup_open = 0;
    } else {
        close_all_popups();
        g_inv_popup_open = 1;
        g_inv_selected   = 0;
        refresh_inventory();
    }
}

void ODuke3D_STAR_ToggleQuestPopup(void) {
    if (g_quest_popup_open) {
        g_quest_popup_open = 0;
    } else {
        close_all_popups();
        g_quest_popup_open = 1;
        g_quest_selected   = 0;
        refresh_quests();
    }
}

/*===========================================================================
 * HUD drawing
 *
 * EDuke32 logical screen: 320×200 (letterboxed at runtime).
 * printext256(x, y, col, backcol, str, fontsize)
 *   fontsize 0 = large (8×8 chars), fontsize 1 = small (4×6 chars).
 *   col/backcol = palette index 0-255, -1 = transparent background.
 *   Common palette entries: 0=black, 12=dark-grey, 20=medium, 30=yellow, 31=white.
 *=========================================================================*/

void ODuke3D_STAR_DrawHUDStatus(void) {
    if (!g_initialized) return;

    /* Version string — bottom-right */
    {
        const char *ver = ODUKE3D_VERSION_STR;
        int len = (int)strlen(ver);
        printext256(316 - len * 4, 194, 12, -1, ver, 1);
    }

    if (!g_beamed_in) return;

    /* Beamed-in label — top-left */
    {
        char buf[80];
        Bsnprintf(buf, sizeof(buf), "OASIS: %s", g_username);
        printext256(2, 2, 31, -1, buf, 1);
    }

    /* XP counter — top-right */
    {
        char buf[32];
        Bsnprintf(buf, sizeof(buf), "XP: %d", g_xp);
        int len = (int)strlen(buf);
        printext256(316 - len * 4, 2, 30, -1, buf, 1);
    }

    /* Toast notification — centred, ¼ down screen, fades over last 30 tics */
    if (g_toast_ticks > 0) {
        int16_t col = (g_toast_ticks > 30) ? 31 : (int16_t)(g_toast_ticks * 31 / 30);
        int len = (int)strlen(g_toast_msg);
        int x   = (320 - len * 4) / 2;
        if (x < 0) x = 0;
        printext256(x, 50, col, -1, g_toast_msg, 1);
    }
}

void ODuke3D_STAR_DrawPopupOverlay(void) {
    if (!g_inv_popup_open && !g_quest_popup_open) return;

    /* ---- Inventory Popup ---- */
    if (g_inv_popup_open) {
        /* Title bar */
        printext256(88, 22, 31, 0, "OASIS  INVENTORY", 0);
        printext256(48, 38, 20, -1, "-------------------------------", 1);

        if (g_inv_count == 0) {
            printext256(52, 70, 12, -1, "No items in your OASIS inventory.", 1);
        } else {
            int visible = (g_inv_count < 12) ? g_inv_count : 12;
            for (int i = 0; i < visible; i++) {
                int16_t col = (i == g_inv_selected) ? 31 : 20;
                char line[80];
                Bsnprintf(line, sizeof(line), "%s%s",
                          i == g_inv_selected ? "> " : "  ",
                          g_inv_names[i]);
                printext256(48, 50 + i * 8, col, -1, line, 1);
            }
        }

        printext256(48, 162, 30, -1, "[U] Use   [A] Send to Avatar   [C] Send to Clan", 1);
        printext256(48, 172, 12, -1, "[I] Close   [Up/Down] Navigate", 1);
    }

    /* ---- Quest Popup ---- */
    if (g_quest_popup_open) {
        printext256(106, 22, 31, 0, "OASIS  QUESTS", 0);
        printext256(48, 38, 20, -1, "-------------------------------", 1);

        if (g_quest_count == 0) {
            printext256(60, 70, 12, -1, "No active OASIS quests.", 1);
        } else {
            int visible = (g_quest_count < 10) ? g_quest_count : 10;
            for (int i = 0; i < visible; i++) {
                int16_t col = (i == g_quest_selected) ? 31 : 20;
                char line[80];
                Bsnprintf(line, sizeof(line), "%s%s",
                          i == g_quest_selected ? "> " : "  ",
                          g_quest_names[i]);
                printext256(48, 50 + i * 10, col, -1, line, 1);
            }

            if (g_quest_selected < g_quest_count)
                printext256(48, 152, 14, -1, g_quest_descs[g_quest_selected], 1);
        }

        printext256(48, 172, 12, -1, "[Q] Close   [Up/Down] Navigate", 1);
    }
}

/*===========================================================================
 * Input handling
 *=========================================================================*/

void ODuke3D_STAR_HandleKey(int sc, int down) {
    if (!g_initialized || !down) return;

    switch (sc) {
        case sc_I:
            ODuke3D_STAR_ToggleInventoryPopup();
            break;
        case sc_Q:
            ODuke3D_STAR_ToggleQuestPopup();
            break;
        case sc_Escape:
            close_all_popups();
            break;
        case sc_UpArrow:
            if (g_inv_popup_open   && g_inv_selected   > 0) g_inv_selected--;
            if (g_quest_popup_open && g_quest_selected > 0) g_quest_selected--;
            break;
        case sc_DownArrow:
            if (g_inv_popup_open   && g_inv_selected   < g_inv_count   - 1) g_inv_selected++;
            if (g_quest_popup_open && g_quest_selected < g_quest_count - 1) g_quest_selected++;
            break;
        case sc_U:
            /* Use selected inventory item */
            if (g_inv_popup_open && g_inv_selected < g_inv_count) {
                ogengine_sync_use_item(g_inv_names[g_inv_selected]);
                char toast[128];
                Bsnprintf(toast, sizeof(toast), "Used: %s", g_inv_names[g_inv_selected]);
                set_toast(toast);
                refresh_inventory();
            }
            break;
        case sc_A:
            /* Send selected item to avatar */
            if (g_inv_popup_open && g_inv_selected < g_inv_count) {
                ogengine_sync_send_to_avatar(g_inv_names[g_inv_selected]);
                char toast[128];
                Bsnprintf(toast, sizeof(toast), "Sent to Avatar: %s", g_inv_names[g_inv_selected]);
                set_toast(toast);
                refresh_inventory();
            }
            break;
        case sc_C:
            /* Send selected item to clan */
            if (g_inv_popup_open && g_inv_selected < g_inv_count) {
                ogengine_sync_send_to_clan(g_inv_names[g_inv_selected]);
                char toast[128];
                Bsnprintf(toast, sizeof(toast), "Sent to Clan: %s", g_inv_names[g_inv_selected]);
                set_toast(toast);
                refresh_inventory();
            }
            break;
        default:
            break;
    }
}

/*===========================================================================
 * OASIS Portal / Teleport — incoming warp from another OGame
 * Call from app_main or level-load path (e.g. G_NewGame / G_LoadGame).
 *=========================================================================*/

void ODuke3D_STAR_CheckIncomingTeleport(void)
{
    char map[256];
    float x = 0, y = 0, z = 64;
    if (!ogengine_poll_teleport_request(map, sizeof(map), &x, &y, &z))
        return;
    /* TODO: ps[myconnectindex].pos = { x, y, z }; ps[myconnectindex].opos = ps[myconnectindex].pos; */
    ogengine_confirm_teleport_arrival();
}
