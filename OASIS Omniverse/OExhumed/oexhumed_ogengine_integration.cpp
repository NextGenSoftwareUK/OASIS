/**
 * OExhumed - OASIS STAR API Integration
 * Base engine: Raze (Exhumed/PowerSlave backend)  https://github.com/ZDoom/Raze
 */

#include "oexhumed_ogengine_integration.h"
#include "OGEngineClient.h"

#include "c_dispatch.h"
#include "c_console.h"
#include "c_cvars.h"
#include "printf.h"
#include "i_time.h"
#include "v_draw.h"

#define OE_GAME_SOURCE "OEXHUMED"

static OGEngineClient* g_client = nullptr;
static bool            g_ready  = false;

/* ── XP table (Exhumed Egyptian enemy roster) ── */
struct ExhumedEnemy { int type; const char* name; int xp; };
static constexpr ExhumedEnemy EX_ENEMY_XP[] = {
    { 1,  "Mummy",       20  }, { 2, "GreenMummy",   25 },
    { 3,  "RedMummy",    30  }, { 4, "Seth",          30 },
    { 5,  "AnubisBot",   40  }, { 6, "Cobra",         15 },
    { 7,  "Scorpion",    35  }, { 8, "SphinxBot",     50 },
    { 9,  "QueenCobra",  80  }, { 10,"Magmoor",      400 },
    { 0,  nullptr,        0  }
};

/* ── Item map ── */
struct ExhumedItem { const char* name; const char* category; int value; };
static constexpr ExhumedItem EX_ITEM_MAP[] = {
    { "Ankh",           "consumable", 50  },
    { "BlueJewel",      "key_item",    0  },
    { "GreenJewel",     "key_item",    0  },
    { "RedJewel",       "key_item",    0  },
    { "GoldJewel",      "key_item",    0  },
    { "PistolAmmo",     "ammo",        5  },
    { "M60Ammo",        "ammo",        5  },
    { "GrenadeAmmo",    "ammo",       10  },
    { "CobraStaff",     "weapon",     60  },
    { "FlameSnake",     "weapon",     80  },
    { "RingOfRa",       "powerup",    20  },
    { "ScarabAmulet",   "powerup",    15  },
    { nullptr,          nullptr,       0  }
};

CCMD(star_oexhumed) {
    if (argv.argc() < 2) {
        Printf("star_oexhumed: version | inv | xp | login\n");
        return;
    }
    if (!g_client || !g_ready) { Printf("OExhumed STAR not ready.\n"); return; }

    if (!stricmp(argv[1], "version")) {
        Printf("OExhumed STAR v1.0 | %s\n", OE_GAME_SOURCE);
    } else if (!stricmp(argv[1], "inv")) {
        g_client->PrintInventory();
    } else if (!stricmp(argv[1], "xp")) {
        Printf("XP: %d\n", g_client->GetXP());
    } else if (!stricmp(argv[1], "login")) {
        if (argv.argc() >= 4)
            g_client->Login(argv[2], argv[3]);
        else
            Printf("Usage: star_oexhumed login <username> <password>\n");
    }
}

extern "C" void OExhumed_STAR_Init(const char* star_api_base_url,
                                    const char* oasis_json_path) {
    g_client = new OGEngineClient(OE_GAME_SOURCE, star_api_base_url,
                                   oasis_json_path);
    g_ready  = g_client->Initialize();
    if (g_ready)
        Printf("[OExhumed] STAR API ready — Pharaoh's curse begins.\n");
}

extern "C" void OExhumed_STAR_Cleanup(void) {
    if (g_client) { g_client->Shutdown(); delete g_client; g_client = nullptr; }
    g_ready = false;
}

extern "C" void OExhumed_STAR_Tick(void) {
    if (g_ready) g_client->Tick();
}

extern "C" void OExhumed_STAR_OnItemPickup(int item_type, const char* item_name) {
    if (!g_ready) return;
    for (int i = 0; EX_ITEM_MAP[i].name; ++i) {
        if (!stricmp(EX_ITEM_MAP[i].name, item_name)) {
            g_client->AddInventoryItem(item_name, EX_ITEM_MAP[i].category,
                                        EX_ITEM_MAP[i].value);
            return;
        }
    }
    g_client->AddInventoryItem(item_name, "misc", 5);
}

extern "C" void OExhumed_STAR_OnEnemyKilled(int enemy_type, const char* enemy_name,
                                             const char* killer) {
    if (!g_ready) return;
    for (int i = 0; EX_ENEMY_XP[i].name; ++i) {
        if (EX_ENEMY_XP[i].type == enemy_type) {
            g_client->AwardXP(EX_ENEMY_XP[i].xp, EX_ENEMY_XP[i].name);
            return;
        }
    }
    g_client->AwardXP(10, enemy_name);
}

extern "C" void OExhumed_STAR_DrawHUDStatus(int screen_w, int screen_h) {
    if (!g_ready) return;
    char buf[128];
    snprintf(buf, sizeof(buf), "OASIS | XP: %d", g_client->GetXP());
    DrawText(twod, NewSmallFont, CR_GOLD, screen_w - 200, screen_h - 20,
             buf, DTA_FullscreenScale, FSMode_ScaleToScreen, TAG_DONE);
}

extern "C" int OExhumed_STAR_HandleKey(int key_code) {
    if (!g_ready) return 0;
    return g_client->HandleKey(key_code);
}

extern "C" int OExhumed_STAR_IsReady(void) {
    return g_ready ? 1 : 0;
}
