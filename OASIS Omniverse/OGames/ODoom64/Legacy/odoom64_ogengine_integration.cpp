/**
 * ODoom64 - OASIS STAR API Integration
 * Base engine: Doom64 EX+  https://github.com/svkaiser/Doom64EX-Plus  (GPL-2.0)
 *
 * Doom64 EX+ is a C codebase (id Tech 1 derivative).  Integration hooks are
 * C functions; C++ is used here only for OGEngineClient convenience.
 */

#include "odoom64_ogengine_integration.h"
#include "OGEngineClient.h"

#include <stdio.h>
#include <string.h>

#define OD64_GAME_SOURCE "ODOOM64"

static OGEngineClient* g_client = nullptr;
static bool            g_ready  = false;

/* ── Doom 64 mobjtype_t subset (enemy types) ── */
struct D64Enemy { int mo_type; const char* name; int xp; };
static constexpr D64Enemy D64_ENEMY_XP[] = {
    { 1,  "ImpMonster",        10  }, { 2,  "Demon",             15 },
    { 3,  "Spectre",           15  }, { 4,  "NightmareDemon",    25 },
    { 5,  "NightmareSpectre",  25  }, { 6,  "LostSoul",           8 },
    { 7,  "Cacodemon",         20  }, { 8,  "CacoSpectre",        25 },
    { 9,  "PainElemental",     30  }, { 10, "HellKnight",         25 },
    { 11, "BaronOfHell",       40  }, { 12, "Arachnotron",        30 },
    { 13, "BabySpider",         8  }, { 14, "Mancubus",           35 },
    { 15, "Revenant",          30  }, { 16, "Cyberdemon",        200 },
    { 17, "SpiderMastermind", 200  }, { 18, "MotherDemon",       500 },
    { 0,  nullptr,              0  }
};

/* ── Doom 64 item / pickup types ── */
struct D64Item { int mo_type; const char* name; const char* category; int value; };
static constexpr D64Item D64_ITEM_MAP[] = {
    { 50, "StimulusPack",    "consumable", 10  },
    { 51, "Medikit",         "consumable", 25  },
    { 52, "SoulSphere",      "consumable", 100 },
    { 53, "Megasphere",      "consumable", 200 },
    { 54, "Berserk",         "powerup",    20  },
    { 55, "BlurSphere",      "powerup",    15  },
    { 56, "RadSuit",         "powerup",    15  },
    { 57, "Invulnerability", "powerup",    30  },
    { 58, "ArmorBonus",      "armor",       5  },
    { 59, "GreenArmor",      "armor",      50  },
    { 60, "BlueArmor",       "armor",      100 },
    { 70, "BlueCard",        "key_item",    0  },
    { 71, "RedCard",         "key_item",    0  },
    { 72, "YellowCard",      "key_item",    0  },
    { 80, "Shotgun",         "weapon",     40  },
    { 81, "SuperShotgun",    "weapon",     60  },
    { 82, "Chaingun",        "weapon",     50  },
    { 83, "RocketLauncher",  "weapon",     80  },
    { 84, "PlasmaRifle",     "weapon",     80  },
    { 85, "BFG9000",         "weapon",    100  },
    { 86, "Unmaker",         "weapon",    120  },
    { 0,  nullptr,            nullptr,      0  }
};

extern "C" void ODoom64_STAR_Init(const char* star_api_base_url,
                                   const char* oasis_json_path) {
    g_client = new OGEngineClient(OD64_GAME_SOURCE, star_api_base_url,
                                   oasis_json_path);
    g_ready  = g_client->Initialize();
    if (g_ready)
        fprintf(stdout, "[ODoom64] STAR API ready — the Unmaker awaits.\n");
}

extern "C" void ODoom64_STAR_Cleanup(void) {
    if (g_client) { g_client->Shutdown(); delete g_client; g_client = nullptr; }
    g_ready = false;
}

extern "C" void ODoom64_STAR_Tick(void) {
    if (g_ready) g_client->Tick();
}

extern "C" void ODoom64_STAR_OnItemPickup(int mo_type, const char* item_name) {
    if (!g_ready) return;
    for (int i = 0; D64_ITEM_MAP[i].name; ++i) {
        if (D64_ITEM_MAP[i].mo_type == mo_type) {
            g_client->AddInventoryItem(D64_ITEM_MAP[i].name,
                                        D64_ITEM_MAP[i].category,
                                        D64_ITEM_MAP[i].value);
            return;
        }
    }
    if (item_name)
        g_client->AddInventoryItem(item_name, "misc", 5);
}

extern "C" void ODoom64_STAR_OnMonsterKilled(int mo_type,
                                              const char* monster_name,
                                              const char* killer) {
    if (!g_ready) return;
    for (int i = 0; D64_ENEMY_XP[i].name; ++i) {
        if (D64_ENEMY_XP[i].mo_type == mo_type) {
            g_client->AwardXP(D64_ENEMY_XP[i].xp, D64_ENEMY_XP[i].name);
            return;
        }
    }
    g_client->AwardXP(10, monster_name ? monster_name : "Unknown");
}

extern "C" void ODoom64_STAR_DrawHUDStatus(int screen_w, int screen_h) {
    if (!g_ready) return;
    /* Draw via Doom 64 EX+'s own M_DrawText or a custom overlay render.
       Exact API depends on the EX+ renderer version. */
}

extern "C" int ODoom64_STAR_HandleKey(int key_code) {
    if (!g_ready) return 0;
    return g_client->HandleKey(key_code);
}

extern "C" int ODoom64_STAR_IsReady(void) {
    return g_ready ? 1 : 0;
}
