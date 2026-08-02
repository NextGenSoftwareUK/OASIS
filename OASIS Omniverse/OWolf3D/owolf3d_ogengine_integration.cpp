/*
** owolf3d_ogengine_integration.cpp
** OWolf3D — OASIS STAR API Integration (ECWolf C++ implementation)
**
** Compile alongside the ECWolf engine sources. The six hook calls are
** documented in Docs/INTEGRATION_INSTRUCTIONS.md.
*/

/* ── ECWolf engine headers ──────────────────────────────────────────────── */
#include "v_video.h"        /* screen, DCanvas */
#include "v_font.h"         /* SmallFont, BigFont, CR_* */
#include "v_text.h"         /* DrawText, TAG_DONE */
#include "id_in.h"          /* Keyboard[], SDLK_*, sc_Escape, sc_UpArrow, sc_DownArrow */
#include "wl_def.h"         /* SCREENWIDTH / SCREENHEIGHT via v_video.h */
#include "zstring.h"        /* FString */

/* ── OASIS STAR API ─────────────────────────────────────────────────────── */
#define OGLIB_JSON_IMPL
#define OGLIB_SESSION_IMPL
#define OGLIB_MONSTER_IMPL
#include "OGLib/oglib.h"
#include "ogengine.h"
#include "ogengine_sync.h"

#include "owolf3d_ogengine_integration.h"

#include <string.h>
#include <stdio.h>
#include <stdlib.h>

/* ── Constants ──────────────────────────────────────────────────────────── */
#define OW_GAME_SOURCE  "OWOLF3D"
#define OW_VERSION_STR  "OWolf3D 1.0.0"
#define OW_LOG(fmt, ...) star_log("[WOLF3D] " fmt, ##__VA_ARGS__)

/* ── State ──────────────────────────────────────────────────────────────── */
static int   g_ow_initialized    = 0;
static int   g_ow_beamedin       = 0;
static int   g_ow_xp             = 0;
static char  g_ow_username[128]  = {0};

/* Toast */
static char  g_toast_msg[256]    = {0};
static int   g_toast_ticks       = 0;
#define TOAST_DURATION 180

/* Popup */
static int   g_inv_popup_open    = 0;
static int   g_quest_popup_open  = 0;

/* Popup inventory list */
#define OW_INV_MAX 32
static ogengine_item_t g_inv_items[OW_INV_MAX];
static int         g_inv_count   = 0;
static int         g_inv_sel     = 0;

/* Quest list */
#define OW_QUEST_MAX 16
static star_quest_t g_quests[OW_QUEST_MAX];
static int          g_quest_count = 0;
static int          g_quest_sel   = 0;

/* Keyboard debounce */
static int g_key_i_prev = 0;
static int g_key_q_prev = 0;

/* ── Key mapping (LOCKDEFS lock number → cross-game STAR key type) ─────── */
static const struct { int keynum; const char *cross_game; } s_key_map[] = {
    { 1, "gold_key"   },   /* GoldKey   (Wolf3D / Spear lock 1) */
    { 2, "silver_key" },   /* SilverKey (Wolf3D / Spear lock 2) */
    { 0, NULL }
};

/* ── Monster table (loaded from oasisstar.json) ─────────────────────────── */
static oglib_monster_entry_t s_monsters[64];
static int                   s_monster_count = 0;

static const oglib_monster_entry_t s_default_monsters[] = {
    /* Regular enemies */
    { "guard",          "Brown Guard",        100,  0, 0 },
    { "greenguard",     "Green Guard",        100,  0, 0 },
    { "dog",            "Dog",                 50,  0, 0 },
    { "doberman",       "Doberman",            50,  0, 0 },
    { "officer",        "Officer",            400,  0, 0 },
    { "lostofficer",    "Lost Officer",       400,  0, 0 },
    { "mutant",         "Mutant",             700,  0, 0 },
    { "gunbat",         "Gun Bat",            700,  0, 0 },
    { "wolfensteinss",  "SS Guard",           500,  0, 0 },
    { "schutzstafell",  "Schutzstafell",      500,  0, 0 },
    { "spectre",        "Spectre",            200,  0, 0 },
    { "ghoulishghost",  "Ghoulish Ghost",     200,  0, 0 },
    /* Wolf3D bosses */
    { "hans",           "Hans Grosse",       5000,  1, 1 },
    { "schabbs",        "Dr. Schabbs",       5000,  1, 1 },
    { "gift",           "Gen. Fettgesicht",  5000,  1, 1 },
    { "fatface",        "Fat Face",          5000,  1, 1 },
    { "fakehitler",     "Fake Hitler",       2000,  1, 0 },
    { "mechahitler",    "Mecha-Hitler",      5000,  1, 1 },
    { "hitler",         "Hitler",            5000,  1, 1 },
    { "gretel",         "Gretel Grosse",     5000,  1, 1 },
    { "trans",          "Trans Grosse",      5000,  1, 1 },
    { "submarinewilly", "Submarine Willy",   5000,  1, 1 },
    /* Spear of Destiny bosses */
    { "ubermutant",        "Uber-Mutant",       5000,  1, 1 },
    { "theaxe",            "The Axe",           5000,  1, 1 },
    { "wilhelm",           "Barnacle Wilhelm",  5000,  1, 1 },
    { "professorquarkblitz","Prof. Quarkblitz", 5000,  1, 1 },
    { "deathknight",       "Death Knight",      5000,  1, 1 },
    { "robot",             "Robot",             5000,  1, 1 },
    { "angeofdeath",       "Angel of Death",    5000,  1, 1 },
    { "devilincarnate",    "Devil Incarnate",   5000,  1, 1 },
};
#define OW_DEFAULT_MONSTER_COUNT (int)(sizeof(s_default_monsters)/sizeof(s_default_monsters[0]))

static const oglib_monster_entry_t *find_monster(const char *classname)
{
    char lower[64];
    int i;
    for (i = 0; classname[i] && i < 63; ++i)
        lower[i] = (char)(classname[i] | 0x20);
    lower[i] = '\0';

    for (i = 0; i < s_monster_count; ++i)
        if (strcmp(s_monsters[i].engine_name, lower) == 0)
            return &s_monsters[i];

    /* fall back to defaults */
    for (i = 0; i < OW_DEFAULT_MONSTER_COUNT; ++i)
        if (strcmp(s_default_monsters[i].engine_name, lower) == 0)
            return &s_default_monsters[i];
    return NULL;
}

static const char *keynum_to_cross_game(int keynum)
{
    for (int i = 0; s_key_map[i].cross_game; ++i)
        if (s_key_map[i].keynum == keynum)
            return s_key_map[i].cross_game;
    return NULL;
}

/* ── Toast ───────────────────────────────────────────────────────────────── */
static void show_toast(const char *msg)
{
    strncpy(g_toast_msg, msg, sizeof(g_toast_msg) - 1);
    g_toast_msg[sizeof(g_toast_msg) - 1] = '\0';
    g_toast_ticks = TOAST_DURATION;
}

/* ── Lifecycle ─────────────────────────────────────────────────────────── */

void OWolf3D_STAR_Init(void)
{
    if (g_ow_initialized) return;

    ogengine_config_t cfg;
    memset(&cfg, 0, sizeof(cfg));
    cfg.game_source = OW_GAME_SOURCE;
    cfg.version     = OW_VERSION_STR;
    cfg.config_path = "oasisstar.json";

    if (ogengine_init(&cfg) != 0) {
        OW_LOG("ogengine_init failed\n");
        return;
    }

    /* Load monster table from JSON */
    s_monster_count = oglib_load_monsters_from_json(
        "oasisstar.json", "owolf3d",
        s_monsters, (int)(sizeof(s_monsters)/sizeof(s_monsters[0])));
    if (s_monster_count <= 0)
        s_monster_count = 0;

    /* Restore saved session */
    ogengine_sync_restore_session();

    g_ow_initialized = 1;
    OW_LOG("Init complete (%s)\n", OW_VERSION_STR);
}

void OWolf3D_STAR_Cleanup(void)
{
    if (!g_ow_initialized) return;
    ogengine_shutdown();
    g_ow_initialized = 0;
}

void OWolf3D_STAR_Tick(void)
{
    if (!g_ow_initialized) return;

    ogengine_sync_tick();

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

    /* Poll beamed-in status */
    g_ow_beamedin = ogengine_is_beamedin();
    if (g_ow_beamedin) {
        const char *u = ogengine_get_username();
        if (u) {
            strncpy(g_ow_username, u, sizeof(g_ow_username) - 1);
            g_ow_username[sizeof(g_ow_username) - 1] = '\0';
        }
        g_ow_xp = ogengine_get_xp();
    }

    if (g_toast_ticks > 0)
        --g_toast_ticks;
}

/* ── Game events ─────────────────────────────────────────────────────────── */

void OWolf3D_STAR_OnActorKilled(const char *classname, int is_boss)
{
    if (!g_ow_initialized || !g_ow_beamedin) return;

    const oglib_monster_entry_t *m = find_monster(classname);
    if (!m) return;

    star_kill_event_t ev;
    memset(&ev, 0, sizeof(ev));
    ev.engine_name  = m->engine_name;
    ev.display_name = m->display_name;
    ev.xp           = m->xp;
    ev.is_boss      = is_boss || m->is_boss;
    ev.do_mint      = m->do_mint;

    ogengine_on_kill(&ev);
    g_ow_xp += m->xp;

    char buf[128];
    snprintf(buf, sizeof(buf), "+%d XP  %s", m->xp, m->display_name);
    show_toast(buf);
}

void OWolf3D_STAR_OnItemPickup(const char *classname, const char *inv_name)
{
    if (!g_ow_initialized || !g_ow_beamedin) return;

    star_item_event_t ev;
    memset(&ev, 0, sizeof(ev));
    ev.engine_name  = classname;
    ev.display_name = (inv_name && inv_name[0]) ? inv_name : classname;
    ev.item_type    = "Item";

    /* Detect keys — map to cross-game canonical names */
    if (strcmp(classname, "GoldKey") == 0 || strcmp(classname, "YellowKey") == 0) {
        ev.item_type    = "Key";
        ev.cross_game   = "gold_key";
        ev.display_name = "Gold Key";
    } else if (strcmp(classname, "SilverKey") == 0 || strcmp(classname, "CyanKey") == 0) {
        ev.item_type    = "Key";
        ev.cross_game   = "silver_key";
        ev.display_name = "Silver Key";
    }

    ogengine_on_item(&ev);

    char buf[128];
    snprintf(buf, sizeof(buf), "Picked up: %s", ev.display_name);
    show_toast(buf);
}

int OWolf3D_STAR_CheckDoorAccess(int keynum)
{
    if (!g_ow_initialized || !g_ow_beamedin) return 0;
    const char *cross = keynum_to_cross_game(keynum);
    if (!cross) return 0;
    return ogengine_has_item(cross);
}

/* ── Query ──────────────────────────────────────────────────────────────── */

int         OWolf3D_STAR_IsBeamedIn(void)      { return g_ow_beamedin; }
const char *OWolf3D_STAR_GetUsername(void)     { return g_ow_username; }
int         OWolf3D_STAR_GetXP(void)           { return g_ow_xp; }
const char *OWolf3D_STAR_GetVersionString(void){ return OW_VERSION_STR; }

/* ── GUI state ──────────────────────────────────────────────────────────── */

int OWolf3D_STAR_IsInventoryPopupOpen(void) { return g_inv_popup_open; }
int OWolf3D_STAR_IsQuestPopupOpen(void)    { return g_quest_popup_open; }

void OWolf3D_STAR_ToggleInventoryPopup(void)
{
    if (!g_ow_beamedin) { show_toast("Beam in first (star beamin)"); return; }
    g_inv_popup_open  = !g_inv_popup_open;
    g_quest_popup_open = 0;
    if (g_inv_popup_open) {
        g_inv_count = ogengine_get_inventory(g_inv_items, OW_INV_MAX);
        g_inv_sel   = 0;
    }
}

void OWolf3D_STAR_ToggleQuestPopup(void)
{
    if (!g_ow_beamedin) { show_toast("Beam in first (star beamin)"); return; }
    g_quest_popup_open = !g_quest_popup_open;
    g_inv_popup_open   = 0;
    if (g_quest_popup_open) {
        g_quest_count = ogengine_get_quests(g_quests, OW_QUEST_MAX);
        g_quest_sel   = 0;
    }
}

int OWolf3D_STAR_ShouldBlockInput(void) { return g_inv_popup_open || g_quest_popup_open; }
int OWolf3D_STAR_ShouldUseAvatarFace(void) { return g_ow_beamedin; }

/* ── Input handling ─────────────────────────────────────────────────────── */

void OWolf3D_STAR_HandleKeys(void)
{
    /* I — inventory popup */
    int key_i = Keyboard[SDLK_I];
    if (key_i && !g_key_i_prev)
        OWolf3D_STAR_ToggleInventoryPopup();
    g_key_i_prev = key_i;

    /* Q — quest popup */
    int key_q = Keyboard[SDLK_Q];
    if (key_q && !g_key_q_prev)
        OWolf3D_STAR_ToggleQuestPopup();
    g_key_q_prev = key_q;

    /* Escape closes popups */
    if (Keyboard[sc_Escape] && (g_inv_popup_open || g_quest_popup_open)) {
        g_inv_popup_open   = 0;
        g_quest_popup_open = 0;
        return;
    }

    if (!g_inv_popup_open && !g_quest_popup_open) return;

    /* Up / Down navigation */
    static int s_up_prev = 0, s_dn_prev = 0;
    int up = Keyboard[sc_UpArrow], dn = Keyboard[sc_DownArrow];

    if (g_inv_popup_open) {
        if (up && !s_up_prev && g_inv_sel > 0)             --g_inv_sel;
        if (dn && !s_dn_prev && g_inv_sel < g_inv_count-1) ++g_inv_sel;

        /* U — use item */
        static int s_u_prev = 0;
        int key_u = Keyboard[SDLK_U];
        if (key_u && !s_u_prev && g_inv_count > 0) {
            ogengine_use_item(g_inv_items[g_inv_sel].id);
            show_toast("Item used");
            g_inv_popup_open = 0;
        }
        s_u_prev = key_u;

        /* A — send to Avatar */
        static int s_a_prev = 0;
        int key_a = Keyboard[SDLK_A];
        if (key_a && !s_a_prev && g_inv_count > 0) {
            ogengine_send_to_avatar(g_inv_items[g_inv_sel].id);
            show_toast("Sent to Avatar");
            g_inv_popup_open = 0;
        }
        s_a_prev = key_a;

        /* C — send to Clan */
        static int s_c_prev = 0;
        int key_c = Keyboard[SDLK_C];
        if (key_c && !s_c_prev && g_inv_count > 0) {
            ogengine_send_to_clan(g_inv_items[g_inv_sel].id);
            show_toast("Sent to Clan");
            g_inv_popup_open = 0;
        }
        s_c_prev = key_c;
    }

    if (g_quest_popup_open) {
        if (up && !s_up_prev && g_quest_sel > 0)              --g_quest_sel;
        if (dn && !s_dn_prev && g_quest_sel < g_quest_count-1)++g_quest_sel;
    }

    s_up_prev = up;
    s_dn_prev = dn;
}

/* ── HUD drawing ─────────────────────────────────────────────────────────── */

/*
 * ECWolf text drawing:
 *   screen->DrawText(font, color, x, y, string, TAG_DONE)
 *   screen->Dim(PalEntry(r,g,b), amount, x, y, w, h)
 *   SmallFont->StringWidth("text") — pixel width measurement
 *   SCREENWIDTH / SCREENHEIGHT — actual viewport size
 */

void OWolf3D_STAR_DrawHUDStatus(void)
{
    if (!g_ow_initialized) return;

    /* Version — bottom-right corner */
    {
        const char *ver = OW_VERSION_STR;
        int vw = SmallFont->StringWidth(ver);
        screen->DrawText(SmallFont, CR_DARKGRAY,
            SCREENWIDTH - vw - 4, SCREENHEIGHT - SmallFont->GetHeight() - 4,
            ver, TAG_DONE);
    }

    if (!g_ow_beamedin) return;

    /* Beamed-in label — top-left */
    {
        char buf[160];
        snprintf(buf, sizeof(buf), "[ %s ]", g_ow_username);
        screen->DrawText(SmallFont, CR_GREEN, 4, 4, buf, TAG_DONE);
    }

    /* XP — top-right */
    {
        char buf[64];
        snprintf(buf, sizeof(buf), "XP: %d", g_ow_xp);
        int bw = SmallFont->StringWidth(buf);
        screen->DrawText(SmallFont, CR_GOLD,
            SCREENWIDTH - bw - 4, 4, buf, TAG_DONE);
    }

    /* Toast — centred, fades out */
    if (g_toast_ticks > 0) {
        int tw = SmallFont->StringWidth(g_toast_msg);
        int tx = (SCREENWIDTH - tw) / 2;
        int ty = SCREENHEIGHT / 3;
        int color = (g_toast_ticks > 60) ? CR_WHITE : CR_DARKGRAY;
        screen->DrawText(SmallFont, color, tx, ty, g_toast_msg, TAG_DONE);
    }
}

void OWolf3D_STAR_DrawPopupOverlay(void)
{
    if (!g_inv_popup_open && !g_quest_popup_open) return;

    /* Semi-transparent background panel */
    int panW = SCREENWIDTH  * 3 / 4;
    int panH = SCREENHEIGHT * 3 / 4;
    int panX = (SCREENWIDTH  - panW) / 2;
    int panY = (SCREENHEIGHT - panH) / 2;

    screen->Dim(PalEntry(0, 0, 0), 0.78f, panX, panY, panW, panH);

    int lh = SmallFont->GetHeight() + 2;
    int th = BigFont  ? BigFont->GetHeight() : 16;

    /* ── Inventory popup ────────────────────────────────────────────────── */
    if (g_inv_popup_open) {
        /* Title */
        const char *title = "OASIS INVENTORY";
        int titW = BigFont ? BigFont->StringWidth(title) : SmallFont->StringWidth(title);
        int titX = panX + (panW - titW) / 2;
        if (BigFont)
            screen->DrawText(BigFont,  CR_GOLD, titX, panY + 6, title, TAG_DONE);
        else
            screen->DrawText(SmallFont, CR_GOLD, titX, panY + 6, title, TAG_DONE);

        int listY = panY + th + 14;
        int maxRows = (panH - th - 40) / lh;

        if (g_inv_count == 0) {
            screen->DrawText(SmallFont, CR_GRAY,
                panX + 8, listY, "(no items)", TAG_DONE);
        } else {
            int start = g_inv_sel > maxRows - 1 ? g_inv_sel - (maxRows - 1) : 0;
            int end   = start + maxRows;
            if (end > g_inv_count) end = g_inv_count;

            for (int i = start; i < end; ++i) {
                int iy   = listY + (i - start) * lh;
                int col  = (i == g_inv_sel) ? CR_WHITE : CR_GRAY;
                char row[128];
                snprintf(row, sizeof(row), "%s  %s",
                    (i == g_inv_sel) ? ">" : " ",
                    g_inv_items[i].display_name);
                screen->DrawText(SmallFont, col, panX + 8, iy, row, TAG_DONE);
            }
        }

        /* Controls */
        const char *ctrl = "[U] Use   [A] Send to Avatar   [C] Send to Clan   [Esc] Close";
        int cw = SmallFont->StringWidth(ctrl);
        screen->DrawText(SmallFont, CR_DARKGRAY,
            panX + (panW - cw) / 2, panY + panH - lh - 4, ctrl, TAG_DONE);
    }

    /* ── Quest popup ─────────────────────────────────────────────────────── */
    if (g_quest_popup_open) {
        const char *title = "OASIS QUESTS";
        int titW = BigFont ? BigFont->StringWidth(title) : SmallFont->StringWidth(title);
        int titX = panX + (panW - titW) / 2;
        if (BigFont)
            screen->DrawText(BigFont,  CR_GOLD, titX, panY + 6, title, TAG_DONE);
        else
            screen->DrawText(SmallFont, CR_GOLD, titX, panY + 6, title, TAG_DONE);

        int listY = panY + th + 14;
        int halfW = panW / 2 - 12;

        if (g_quest_count == 0) {
            screen->DrawText(SmallFont, CR_GRAY,
                panX + 8, listY, "(no active quests)", TAG_DONE);
        } else {
            int maxRows = (panH - th - 40) / lh;
            for (int i = 0; i < g_quest_count && i < maxRows; ++i) {
                int iy  = listY + i * lh;
                int col = (i == g_quest_sel) ? CR_WHITE : CR_GRAY;
                char row[128];
                snprintf(row, sizeof(row), "%s %s",
                    (i == g_quest_sel) ? ">" : " ",
                    g_quests[i].name);
                screen->DrawText(SmallFont, col, panX + 8, iy, row, TAG_DONE);
            }

            /* Description pane (right half) */
            if (g_quest_sel < g_quest_count) {
                const char *desc = g_quests[g_quest_sel].description;
                if (desc && desc[0]) {
                    int dx = panX + halfW + 16;
                    /* simple word-wrap: break at 30 chars */
                    int maxDesc = (panH - th - 40) / lh;
                    int di = 0, dy = listY;
                    while (*desc && di < maxDesc) {
                        char line[64];
                        int n = 0;
                        while (*desc && n < 30) line[n++] = *desc++;
                        while (*desc && *desc != ' ' && n < 63) line[n++] = *desc++;
                        line[n] = '\0';
                        screen->DrawText(SmallFont, CR_LIGHTBLUE, dx, dy, line, TAG_DONE);
                        dy += lh; ++di;
                    }
                }
            }
        }

        const char *ctrl = "[Up/Dn] Navigate   [Esc] Close";
        int cw = SmallFont->StringWidth(ctrl);
        screen->DrawText(SmallFont, CR_DARKGRAY,
            panX + (panW - cw) / 2, panY + panH - lh - 4, ctrl, TAG_DONE);
    }
}

/*-----------------------------------------------------------------------------
 * OASIS Portal / Teleport — incoming warp from another OGame
 *---------------------------------------------------------------------------*/

void OWolf3D_STAR_CheckIncomingTeleport(void)
{
    char map[256];
    float x = 0, y = 0, z = 64;
    if (!ogengine_poll_teleport_request(map, sizeof(map), &x, &y, &z))
        return;
    /* TODO: player.position = { (int)x, (int)y }; player.angle = 0; */
    ogengine_confirm_teleport_arrival();
}
