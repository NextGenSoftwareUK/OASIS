/**
 * ORtCW - OASIS STAR API Integration
 * Base engine: iortcw  https://github.com/iortcw/iortcw  (GPL-3.0)
 *
 * iortcw is a Q3-engine derivative.  Game DLL hook patterns closely mirror
 * OQuake3 — trap_* syscalls for engine calls, G_RunFrame for the main loop.
 */

#include "ortcw_ogengine_integration.h"
#include "OGEngineClient.h"

#include <stdio.h>
#include <string.h>

#define ORTCW_GAME_SOURCE "ORTCW"

static OGEngineClient* g_client = nullptr;
static bool            g_ready  = false;

/* ── Enemy XP table (RtCW Wehrmacht / supernatural roster) ── */
struct RtCWEnemy { const char* cls; const char* name; int xp; };
static constexpr RtCWEnemy RTCW_ENEMY_XP[] = {
    { "ai_soldier",      "Wehrmacht Soldier",  20  },
    { "ai_officer",      "SS Officer",          25  },
    { "ai_ss",           "SS Elite Guard",      30  },
    { "ai_zombie",       "Undead Soldier",      20  },
    { "ai_bat",          "Bat",                  5  },
    { "ai_ghost",        "Looper Ghost",        25  },
    { "ai_uber",         "Uber-Soldat",         80  },
    { "ai_blackguard",   "Black Guard",         30  },
    { "ai_loper",        "Loper",               40  },
    { "ai_boss_helga",   "Helga Von Bulow",    200  },
    { "ai_boss_heinrich","Heinrich I",          500  },
    { nullptr,            nullptr,               0  }
};

/* ── Item map (RtCW weapon / ammo / health classnames) ── */
struct RtCWItem { const char* cls; const char* category; int value; };
static constexpr RtCWItem RTCW_ITEM_MAP[] = {
    { "item_health_small",   "consumable", 10  },
    { "item_health",         "consumable", 25  },
    { "item_health_large",   "consumable", 50  },
    { "item_armor_shard",    "armor",       5  },
    { "item_armor",          "armor",      50  },
    { "ammo_9mm",            "ammo",        5  },
    { "ammo_40mm",           "ammo",        5  },
    { "ammo_mauser",         "ammo",        5  },
    { "ammo_panzerfaust",    "ammo",       15  },
    { "ammo_flamethrower",   "ammo",       10  },
    { "ammo_tesla",          "ammo",       10  },
    { "weapon_luger",        "weapon",     30  },
    { "weapon_mp40",         "weapon",     50  },
    { "weapon_thompson",     "weapon",     50  },
    { "weapon_sten",         "weapon",     50  },
    { "weapon_mauser",       "weapon",     60  },
    { "weapon_fg42",         "weapon",     70  },
    { "weapon_panzerfaust",  "weapon",     80  },
    { "weapon_flamethrower", "weapon",     80  },
    { "weapon_venom",        "weapon",    100  },
    { "weapon_tesla",        "weapon",    100  },
    { nullptr,                nullptr,      0  }
};

extern "C" void ORtCW_STAR_Init(const char* star_api_base_url,
                                  const char* oasis_json_path) {
    g_client = new OGEngineClient(ORTCW_GAME_SOURCE, star_api_base_url,
                                   oasis_json_path);
    g_ready  = g_client->Initialize();
    if (g_ready)
        fprintf(stdout,
                "[ORtCW] STAR API ready — Blazkowicz reports for duty.\n");
}

extern "C" void ORtCW_STAR_Cleanup(void) {
    if (g_client) { g_client->Shutdown(); delete g_client; g_client = nullptr; }
    g_ready = false;
}

extern "C" void ORtCW_STAR_Tick(void) {
    if (g_ready) g_client->Tick();
}

extern "C" void ORtCW_STAR_OnItemPickup(const char* classname,
                                          const char* item_name) {
    if (!g_ready) return;
    for (int i = 0; RTCW_ITEM_MAP[i].cls; ++i) {
        if (!strcasecmp(RTCW_ITEM_MAP[i].cls, classname)) {
            g_client->AddInventoryItem(item_name, RTCW_ITEM_MAP[i].category,
                                        RTCW_ITEM_MAP[i].value);
            return;
        }
    }
    g_client->AddInventoryItem(item_name, "misc", 5);
}

extern "C" void ORtCW_STAR_OnEnemyKilled(const char* enemy_class,
                                           const char* enemy_name,
                                           const char* killer) {
    if (!g_ready) return;
    for (int i = 0; RTCW_ENEMY_XP[i].cls; ++i) {
        if (!strcasecmp(RTCW_ENEMY_XP[i].cls, enemy_class)) {
            g_client->AwardXP(RTCW_ENEMY_XP[i].xp, RTCW_ENEMY_XP[i].name);
            return;
        }
    }
    g_client->AwardXP(10, enemy_name ? enemy_name : enemy_class);
}

extern "C" void ORtCW_STAR_DrawHUDStatus(int screen_w, int screen_h) {
    if (!g_ready) return;
    /* Draw via iortcw CG_DrawStringExt / cgame HUD overlay. */
}

extern "C" int ORtCW_STAR_HandleKey(int key_code) {
    if (!g_ready) return 0;
    return g_client->HandleKey(key_code);
}

extern "C" int ORtCW_STAR_IsReady(void) {
    return g_ready ? 1 : 0;
}
