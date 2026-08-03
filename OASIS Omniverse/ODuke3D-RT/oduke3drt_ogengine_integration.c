/**
 * ODuke3D-RT - OASIS STAR API Integration Implementation
 *
 * Integrates Duke-RT (a Vulkan ray-tracing EDuke32 fork) with the OASIS STAR API.
 * Identical feature set to ODuke3D — blue/red/yellow key cards shared cross-game
 * with ODOOM, OQuake, ODOOM3, ODOOM3-BFG, and ODuke3D.
 *
 * Duke-RT adds Vulkan ray-tracing on top of EDuke32; the OASIS integration
 * layer is the same C code as ODuke3D — only the prefix and game_source string
 * differ.  The RT rendering path draws HUD overlays via the same EDuke32
 * 2-D draw layer that sits above the RT 3-D scene.
 *
 * Integration hook sites in source/duke3d/src/:
 *   app_main() / G_GameExit()          → ODuke3DRT_STAR_Init / Cleanup
 *   G_Tics()                           → ODuke3DRT_STAR_Tick
 *   P_CheckInventory() key card path   → ODuke3DRT_STAR_OnKeyPickup
 *   G_OperateSectors() locked-door     → ODuke3DRT_STAR_CheckDoorAccess
 *   A_DamageObject() when extra <= 0   → ODuke3DRT_STAR_OnActorKilled
 *   G_DrawRooms() / display-rest       → ODuke3DRT_STAR_DrawHUDStatus
 *                                         ODuke3DRT_STAR_DrawPopupOverlay
 *   G_ProcessInput() / KB path         → ODuke3DRT_STAR_HandleKey
 *   G_DrawStatusBar() face tile select → ODuke3DRT_STAR_ShouldUseAvatarFace
 *   P_ProcessInput() movement guard    → ODuke3DRT_STAR_ShouldBlockInput
 */

#include <stdarg.h>
#include <stdlib.h>
#include <string.h>

#include "compat.h"
extern void printext256(int32_t xpos, int32_t ypos, int16_t col,
                        int16_t backcol, const char *name, uint8_t fontsize);
extern int OSD_Printf(const char *fmt, ...);
#include "names.h"

#include "oduke3drt_ogengine_integration.h"
#include "ogengine.h"
#include "ogengine_sync.h"

#define OGLIB_SESSION_IMPL
#define OGLIB_MONSTER_IMPL
#include "OGLib/oglib.h"

#ifndef sc_I
# define sc_I        23
#endif
#ifndef sc_Q
# define sc_Q        16
#endif
#ifndef sc_Escape
# define sc_Escape    1
#endif
#ifndef sc_UpArrow
# define sc_UpArrow  72
#endif
#ifndef sc_DownArrow
# define sc_DownArrow 80
#endif
#ifndef sc_U
# define sc_U        22
#endif
#ifndef sc_A
# define sc_A         4
#endif
#ifndef sc_C
# define sc_C        46
#endif

/*===========================================================================
 * Constants and module state
 *=========================================================================*/

#define ODUKE3DRT_GAME_SOURCE  "ODUKE3DRT"
#define ODUKE3DRT_LOG_TAG      "[DUKE3D-RT] "
#define ODUKE3DRT_VERSION_STR  "ODuke3D-RT 1.0.0"
#define ODUKE3DRT_MAX_PATH     512
#define ODUKE3DRT_TOAST_TICKS  180
#define ODUKE3DRT_INV_MAX      32
#define ODUKE3DRT_QUEST_MAX    16

static int  g_initialized    = 0;
static int  g_client_ready   = 0;
static int  g_debug          = 1;
static char g_json_path[ODUKE3DRT_MAX_PATH];
static char g_username[128];
static int  g_xp             = 0;
static int  g_beamed_in      = 0;

static int  g_inv_popup_open   = 0;
static int  g_quest_popup_open = 0;
static int  g_inv_selected     = 0;
static int  g_quest_selected   = 0;
static int  g_inv_count        = 0;
static int  g_quest_count      = 0;
static char g_inv_names[ODUKE3DRT_INV_MAX][64];
static char g_quest_names[ODUKE3DRT_QUEST_MAX][64];
static char g_quest_descs[ODUKE3DRT_QUEST_MAX][128];

static char g_toast_msg[128];
static int  g_toast_ticks = 0;

static oglib_monster_table_t g_monster_table;

/*===========================================================================
 * Picnum → name mapping (same as ODuke3D — same engine, same tiles)
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

static int name_to_picnum(const char *entity_id) {
    const char *id = entity_id;
    if (strncmp(id, "oduke3drt_", 10) == 0) id += 10;
    else if (strncmp(id, "oduke3d_", 8) == 0) id += 8;
    for (int i = 0; s_picnum_map[i].name; i++)
        if (strcmp(id, s_picnum_map[i].name) == 0) return s_picnum_map[i].picnum;
    return -1;
}

extern int32_t A_InsertSprite(int16_t whatsect, int32_t s_x, int32_t s_y, int32_t s_z,
                               int16_t s_pn, int8_t s_shade, uint8_t s_xr, uint8_t s_yr,
                               int16_t s_ang, int32_t s_xv, int32_t s_yv,
                               int16_t s_owner, int32_t s_statnum);
extern playerdata_t g_player[];
extern int myconnectindex;

/*===========================================================================
 * Internal helpers
 *=========================================================================*/

static void rt_log(const char *fmt, ...) {
    if (!g_debug) return;
    char buf[512];
    va_list ap; va_start(ap, fmt);
    vsnprintf(buf, sizeof(buf), fmt, ap);
    va_end(ap);
    OSD_Printf("%s%s\n", ODUKE3DRT_LOG_TAG, buf);
}

static void set_toast(const char *msg) {
    Bstrncpy(g_toast_msg, msg, sizeof(g_toast_msg) - 1);
    g_toast_ticks = ODUKE3DRT_TOAST_TICKS;
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

static int ODRT_ExtractJsonValue(const char *json, const char *key, char *out, int maxlen)
{
    char search[128];
    const char *p;
    int i;
    snprintf(search, sizeof(search), "\"%s\"", key);
    p = strstr(json, search);
    if (!p) return 0;
    p += strlen(search);
    while (*p == ' ' || *p == ':') ++p;
    if (*p != '"') return 0;
    ++p;
    for (i = 0; *p && *p != '"' && i < maxlen - 1; ++i) out[i] = *p++;
    out[i] = '\0';
    return i > 0;
}

static void close_all_popups(void) {
    g_inv_popup_open = 0;
    g_quest_popup_open = 0;
}

static void refresh_inventory(void) {
    ogengine_item_list_t *list = NULL;
    int i;
    g_inv_count = 0;
    if (ogengine_get_inventory(&list) != OGENGINE_SUCCESS || !list) return;
    for (i = 0; i < (int)list->count && i < ODUKE3DRT_INV_MAX; i++)
        Bstrncpy(g_inv_names[i], list->items[i].name, sizeof(g_inv_names[i]) - 1);
    g_inv_count = (int)list->count < ODUKE3DRT_INV_MAX ? (int)list->count : ODUKE3DRT_INV_MAX;
    ogengine_free_item_list(list);
}

static void refresh_quests(void) {
    static char qbuf[16384];
    const char *p, *eol, *end;
    int n;
    g_quest_count = 0;
    n = ogengine_get_quests_string(qbuf, sizeof(qbuf));
    if (n <= 0 || qbuf[0] == '\0') return;
    qbuf[n < (int)sizeof(qbuf) ? n : (int)sizeof(qbuf) - 1] = '\0';
    p = qbuf; end = qbuf + n;
    while (p < end && g_quest_count < ODUKE3DRT_QUEST_MAX) {
        eol = (const char *)memchr(p, '\n', (size_t)(end - p));
        if (!eol) eol = end;
        if (eol - p >= 2 && p[0] == 'Q' && p[1] == '\t') {
            const char *f  = p + 2;
            const char *t1 = (const char *)memchr(f, '\t', (size_t)(eol - f));
            if (t1) {
                const char *f2 = t1 + 1;
                const char *t2 = (const char *)memchr(f2, '\t', (size_t)(eol - f2));
                int nlen = t2 ? (int)(t2 - f2) : (int)(eol - f2);
                if (nlen > 63) nlen = 63;
                Bstrncpy(g_quest_names[g_quest_count], f2, nlen);
                g_quest_names[g_quest_count][nlen] = '\0';
                if (t2 && t2 + 1 < eol) {
                    const char *f3 = t2 + 1;
                    const char *t3 = (const char *)memchr(f3, '\t', (size_t)(eol - f3));
                    int dlen = t3 ? (int)(t3 - f3) : (int)(eol - f3);
                    if (dlen > 127) dlen = 127;
                    Bstrncpy(g_quest_descs[g_quest_count], f3, dlen);
                    g_quest_descs[g_quest_count][dlen] = '\0';
                }
                g_quest_count++;
            }
        }
        p = eol + 1;
    }
}

/*===========================================================================
 * Lifecycle
 *=========================================================================*/

void ODuke3DRT_STAR_Init(void) {
    if (g_initialized) return;

    Bsnprintf(g_json_path, sizeof(g_json_path), "oasisstar.json");

    const char *env_user   = getenv("STAR_USERNAME");
    const char *env_pass   = getenv("STAR_PASSWORD");
    const char *env_key    = getenv("OGENGINE_KEY");
    const char *env_avatar = getenv("STAR_AVATAR_ID");

    oglib_monster_table_init(&g_monster_table);
    oglib_monster_table_load_from_oasisstar(g_json_path, "oduke3drt", &g_monster_table);

    StarApiConfig cfg;
    memset(&cfg, 0, sizeof(cfg));
    cfg.api_url     = "https://star-api.oasisplatform.world/api";
    cfg.oasis_url   = "https://api.oasisplatform.world";
    cfg.game_source = ODUKE3DRT_GAME_SOURCE;
    cfg.username    = env_user   ? env_user   : "";
    cfg.password    = env_pass   ? env_pass   : "";
    cfg.api_key     = env_key    ? env_key    : "";
    cfg.avatar_id   = env_avatar ? env_avatar : "";

    if (StarApi_Init(&cfg) == 0) {
        g_client_ready = 1;
        Bstrncpy(g_username, env_user ? env_user : "", sizeof(g_username) - 1);
        g_beamed_in = 1;
        OSD_Printf(ODUKE3DRT_LOG_TAG "OASIS STAR API: Authenticated. Cross-game keys enabled.\n");
    } else {
        OSD_Printf(ODUKE3DRT_LOG_TAG "OASIS STAR API: Not authenticated. "
                   "Set STAR_USERNAME/STAR_PASSWORD or OGENGINE_KEY/STAR_AVATAR_ID.\n");
    }

    ogengine_sync_init();
    g_initialized = 1;
    OSD_Printf(ODUKE3DRT_LOG_TAG "%s initialised.\n", ODUKE3DRT_VERSION_STR);
}

void ODuke3DRT_STAR_Cleanup(void) {
    if (!g_initialized) return;
    ogengine_sync_shutdown();
    StarApi_Shutdown();
    g_initialized = 0;
    g_client_ready = 0;
}

void ODuke3DRT_STAR_Tick(void) {
    if (!g_initialized) return;
    ogengine_sync_tick(NULL);

    /* --- cross-game spawn poll --- */
    {
        char entity_id[128];
        float sx, sy, sz;
        if (ogengine_poll_spawn_event(entity_id, sizeof(entity_id), &sx, &sy, &sz))
        {
            {
                int pn = name_to_picnum(entity_id);
                if (pn >= 0) {
                    int16_t sect = (int16_t)g_player[myconnectindex].ps->cursectnum;
                    A_InsertSprite(sect, (int32_t)sx, (int32_t)sy, (int32_t)sz,
                                   (int16_t)pn, 0, 32, 32, 0, 0, 0, -1, 1);
                    oglib_log(OGLIB_LOG_INFO, "OASIS SpawnEvent: %s (picnum %d) at %.0f/%.0f/%.0f", entity_id, pn, sx, sy, sz);
                } else {
                    oglib_log(OGLIB_LOG_WARN, "OASIS SpawnEvent: unknown entity '%s' — no Duke-RT picnum", entity_id);
                }
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
            ODRT_ExtractJsonValue(evt_json, "EventType", evt_type, sizeof(evt_type));
            if (strcmp(evt_type, "ShowNarration") == 0) {
                char narration[256] = "";
                ODRT_ExtractJsonValue(evt_json, "NarrationText", narration, sizeof(narration));
                if (narration[0]) set_toast(narration);
            } else if (strcmp(evt_type, "PlayAudio") == 0) {
                char audio_title[128] = "", audio_url[256] = "";
                ODRT_ExtractJsonValue(evt_json, "AudioTitle", audio_title, sizeof(audio_title));
                ODRT_ExtractJsonValue(evt_json, "AudioUrl",   audio_url,   sizeof(audio_url));
                oasis_open_url(audio_url);
                if (audio_title[0]) set_toast(audio_title);
                oglib_log(OGLIB_LOG_INFO, "OASIS PlayAudio: %s → %s", audio_title, audio_url);
            } else if (strcmp(evt_type, "PlayVideo") == 0) {
                char video_title[128] = "", video_url[256] = "";
                ODRT_ExtractJsonValue(evt_json, "VideoTitle", video_title, sizeof(video_title));
                ODRT_ExtractJsonValue(evt_json, "VideoUrl",   video_url,   sizeof(video_url));
                oasis_open_url(video_url);
                if (video_title[0]) set_toast(video_title);
                oglib_log(OGLIB_LOG_INFO, "OASIS PlayVideo: %s → %s", video_title, video_url);
            } else if (strcmp(evt_type, "OpenWebsite") == 0) {
                char website_url[256] = "";
                ODRT_ExtractJsonValue(evt_json, "WebsiteUrl", website_url, sizeof(website_url));
                oasis_open_url(website_url);
                oglib_log(OGLIB_LOG_INFO, "OASIS OpenWebsite: %s", website_url);
            } else if (strcmp(evt_type, "UnlockPortal") == 0) {
                char portal_id[64] = "";
                ODRT_ExtractJsonValue(evt_json, "PortalId", portal_id, sizeof(portal_id));
                oglib_log(OGLIB_LOG_INFO, "OASIS UnlockPortal: %s — portal unlock not yet implemented", portal_id);
            }
        }
    }

    /* --- inventory grant poll --- */
    {
        char item_guid[64];
        while (ogengine_poll_inventory_grant(item_guid, sizeof(item_guid)))
        {
            oglib_log(OGLIB_LOG_INFO, "OASIS InventoryGrant: %s — inventory refresh triggered", item_guid);
            ogengine_get_inventory(NULL);
        }
    }

    if (g_toast_ticks > 0) g_toast_ticks--;
}

/*===========================================================================
 * Cross-game events
 *=========================================================================*/

void ODuke3DRT_STAR_OnKeyPickup(const char *key_type) {
    if (!g_client_ready || !key_type) return;
    ogengine_sync_on_item_pickup(key_type, "Key", 1);
    char toast[128];
    Bsnprintf(toast, sizeof(toast), "Key added to OASIS: %s", key_type);
    set_toast(toast);
    rt_log("Key pickup: %s", key_type);
}

int ODuke3DRT_STAR_CheckDoorAccess(const char *key_type) {
    if (!g_client_ready || !key_type) return 0;
    int result = ogengine_sync_check_door_access(key_type);
    if (result) {
        char toast[128];
        Bsnprintf(toast, sizeof(toast), "Cross-game key used: %s", key_type);
        set_toast(toast);
        rt_log("Cross-game door access granted: %s", key_type);
    }
    return result;
}

void ODuke3DRT_STAR_OnActorKilled(int picnum, int engine_is_boss) {
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

    if (is_boss) rt_log("BOSS killed: %s (+%d XP)", entry->display_name, xp_award);
    else         rt_log("Kill: %s (+%d XP)", entry->display_name, xp_award);
}

/*===========================================================================
 * State queries
 *=========================================================================*/

int         ODuke3DRT_STAR_IsBeamedIn(void)          { return g_beamed_in; }
const char *ODuke3DRT_STAR_GetUsername(void)          { return g_username; }
int         ODuke3DRT_STAR_GetXP(void)               { return g_xp; }
const char *ODuke3DRT_STAR_GetVersionString(void)     { return ODUKE3DRT_VERSION_STR; }
int         ODuke3DRT_STAR_IsInventoryPopupOpen(void) { return g_inv_popup_open; }
int         ODuke3DRT_STAR_IsQuestPopupOpen(void)     { return g_quest_popup_open; }
int         ODuke3DRT_STAR_ShouldBlockInput(void)     { return g_inv_popup_open || g_quest_popup_open; }
int         ODuke3DRT_STAR_ShouldUseAvatarFace(void)  { return g_beamed_in; }

void ODuke3DRT_STAR_ToggleInventoryPopup(void) {
    if (g_inv_popup_open) { g_inv_popup_open = 0; return; }
    close_all_popups();
    g_inv_popup_open = 1; g_inv_selected = 0; refresh_inventory();
}

void ODuke3DRT_STAR_ToggleQuestPopup(void) {
    if (g_quest_popup_open) { g_quest_popup_open = 0; return; }
    close_all_popups();
    g_quest_popup_open = 1; g_quest_selected = 0; refresh_quests();
}

/*===========================================================================
 * HUD drawing (EDuke32 2-D layer — unchanged by RT rendering path)
 *=========================================================================*/

void ODuke3DRT_STAR_DrawHUDStatus(void) {
    if (!g_initialized) return;

    /* Version — bottom-right */
    {
        const char *ver = ODUKE3DRT_VERSION_STR;
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

    /* XP — top-right */
    {
        char buf[32];
        Bsnprintf(buf, sizeof(buf), "XP: %d", g_xp);
        int len = (int)strlen(buf);
        printext256(316 - len * 4, 2, 30, -1, buf, 1);
    }

    /* Toast — centred, fades */
    if (g_toast_ticks > 0) {
        int16_t col = (g_toast_ticks > 30) ? 31 : (int16_t)(g_toast_ticks * 31 / 30);
        int len = (int)strlen(g_toast_msg);
        int x   = (320 - len * 4) / 2;
        if (x < 0) x = 0;
        printext256(x, 50, col, -1, g_toast_msg, 1);
    }
}

void ODuke3DRT_STAR_DrawPopupOverlay(void) {
    if (!g_inv_popup_open && !g_quest_popup_open) return;

    if (g_inv_popup_open) {
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
                          i == g_inv_selected ? "> " : "  ", g_inv_names[i]);
                printext256(48, 50 + i * 8, col, -1, line, 1);
            }
        }

        printext256(48, 162, 30, -1, "[U] Use   [A] Send to Avatar   [C] Send to Clan", 1);
        printext256(48, 172, 12, -1, "[I] Close   [Up/Down] Navigate", 1);
    }

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
                          i == g_quest_selected ? "> " : "  ", g_quest_names[i]);
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

void ODuke3DRT_STAR_HandleKey(int sc, int down) {
    if (!g_initialized || !down) return;

    switch (sc) {
        case sc_I: ODuke3DRT_STAR_ToggleInventoryPopup(); break;
        case sc_Q: ODuke3DRT_STAR_ToggleQuestPopup();     break;
        case sc_Escape: close_all_popups(); break;
        case sc_UpArrow:
            if (g_inv_popup_open   && g_inv_selected   > 0) g_inv_selected--;
            if (g_quest_popup_open && g_quest_selected > 0) g_quest_selected--;
            break;
        case sc_DownArrow:
            if (g_inv_popup_open   && g_inv_selected   < g_inv_count   - 1) g_inv_selected++;
            if (g_quest_popup_open && g_quest_selected < g_quest_count - 1) g_quest_selected++;
            break;
        case sc_U:
            if (g_inv_popup_open && g_inv_selected < g_inv_count) {
                ogengine_sync_use_item(g_inv_names[g_inv_selected]);
                char toast[128];
                Bsnprintf(toast, sizeof(toast), "Used: %s", g_inv_names[g_inv_selected]);
                set_toast(toast); refresh_inventory();
            }
            break;
        case sc_A:
            if (g_inv_popup_open && g_inv_selected < g_inv_count) {
                ogengine_sync_send_to_avatar(g_inv_names[g_inv_selected]);
                char toast[128];
                Bsnprintf(toast, sizeof(toast), "Sent to Avatar: %s", g_inv_names[g_inv_selected]);
                set_toast(toast); refresh_inventory();
            }
            break;
        case sc_C:
            if (g_inv_popup_open && g_inv_selected < g_inv_count) {
                ogengine_sync_send_to_clan(g_inv_names[g_inv_selected]);
                char toast[128];
                Bsnprintf(toast, sizeof(toast), "Sent to Clan: %s", g_inv_names[g_inv_selected]);
                set_toast(toast); refresh_inventory();
            }
            break;
        default: break;
    }
}

/*===========================================================================
 * OASIS Portal / Teleport — incoming warp from another OGame
 * Call from app_main or level-load path (e.g. G_NewGame / G_LoadGame).
 *=========================================================================*/

void ODuke3DRT_STAR_CheckIncomingTeleport(void)
{
    char map[256];
    float x = 0, y = 0, z = 64;
    if (!ogengine_poll_teleport_request(map, sizeof(map), &x, &y, &z))
        return;
    oglib_log(OGLIB_LOG_INFO, "OASIS Teleport arrive: map=%s pos=%.0f/%.0f/%.0f", map, x, y, z);
    {
        DukePlayer_t *psp = g_player[myconnectindex].ps;
        psp->pos.x = (int32_t)x;
        psp->pos.y = (int32_t)y;
        psp->pos.z = (int32_t)z;
        psp->opos  = psp->pos;
        psp->vel.x = psp->vel.y = psp->vel.z = 0;
    }
    ogengine_confirm_teleport_arrival();
}
