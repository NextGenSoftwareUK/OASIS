/**
 * OStrife - OASIS STAR API Integration
 * Base engine: UZDoom (GZDoom fork)  https://github.com/UZDoom/UZDoom
 */

#include "ostrife_ogengine_integration.h"
#include "OGEngineClient.h"

#include "c_dispatch.h"
#include "c_console.h"
#include "c_cvars.h"
#include "printf.h"
#include "i_time.h"
#include "v_text.h"
#include "v_draw.h"

#define OS_GAME_SOURCE "OSTRIFE"

static OGEngineClient* g_client = nullptr;
static bool            g_ready  = false;

/* ── Enemy XP table (Strife roster) ── */
struct StrifeActor { const char* cls; int xp; };
static constexpr StrifeActor STRIFE_ENEMY_XP[] = {
    { "Peasant",          5   }, { "Acolyte",          15  },
    { "StrifeSoldier",    20  }, { "Templar",           30  },
    { "Stalker",          20  }, { "Inquisitor",        60  },
    { "Spectre",          40  }, { "RevenantTrader",    10  },
    { "MaceFist",         25  }, { "Entity",           500  },
    { nullptr,             0  }
};

/* ── Item map ── */
struct StrifeItem { const char* cls; const char* category; int value; };
static constexpr StrifeItem STRIFE_ITEM_MAP[] = {
    { "GoldCoin",             "currency",  1   },
    { "GoldCoin10",           "currency",  10  },
    { "GoldCoin25",           "currency",  25  },
    { "MedicalKit",           "consumable",50  },
    { "MedPatch",             "consumable",25  },
    { "RingOfRegeneration",   "powerup",   30  },
    { "ShadowArmor",          "armor",     80  },
    { "LeatherArmor",         "armor",     40  },
    { "MetalArmor",           "armor",     80  },
    { "Sigil",                "key_item",   0  },
    { "Crossbow",             "weapon",    40  },
    { "AssaultGun",           "weapon",    60  },
    { "FlameThrower",         "weapon",    80  },
    { "MissileLauncher",      "weapon",   100  },
    { "TelephoneScrambler",   "powerup",   20  },
    { nullptr,                 nullptr,     0  }
};

CCMD(star_ostrife) {
    if (argv.argc() < 2) {
        Printf("star_ostrife: version | inv | xp | login\n");
        return;
    }
    if (!g_client || !g_ready) { Printf("OStrife STAR not ready.\n"); return; }

    if (!stricmp(argv[1], "version")) {
        Printf("OStrife STAR v1.0 | %s\n", OS_GAME_SOURCE);
    } else if (!stricmp(argv[1], "inv")) {
        g_client->PrintInventory();
    } else if (!stricmp(argv[1], "xp")) {
        Printf("XP: %d\n", g_client->GetXP());
    } else if (!stricmp(argv[1], "login")) {
        if (argv.argc() >= 4)
            g_client->Login(argv[2], argv[3]);
        else
            Printf("Usage: star_ostrife login <username> <password>\n");
    }
}

extern "C" void OStrife_STAR_Init(const char* star_api_base_url,
                                   const char* oasis_json_path) {
    g_client = new OGEngineClient(OS_GAME_SOURCE, star_api_base_url,
                                   oasis_json_path);
    g_ready  = g_client->Initialize();
    if (g_ready)
        Printf("[OStrife] STAR API ready — join the Revolution.\n");
}

extern "C" void OStrife_STAR_Cleanup(void) {
    if (g_client) { g_client->Shutdown(); delete g_client; g_client = nullptr; }
    g_ready = false;
}

extern "C" void OStrife_STAR_Tick(void) {
    if (g_ready) g_client->Tick();
}

extern "C" void OStrife_STAR_OnItemPickup(const char* class_name,
                                           const char* item_name) {
    if (!g_ready) return;
    for (int i = 0; STRIFE_ITEM_MAP[i].cls; ++i) {
        if (!stricmp(STRIFE_ITEM_MAP[i].cls, class_name)) {
            g_client->AddInventoryItem(item_name, STRIFE_ITEM_MAP[i].category,
                                        STRIFE_ITEM_MAP[i].value);
            return;
        }
    }
    g_client->AddInventoryItem(item_name, "misc", 5);
}

extern "C" void OStrife_STAR_OnEnemyKilled(const char* actor_class,
                                            const char* killer_name) {
    if (!g_ready) return;
    for (int i = 0; STRIFE_ENEMY_XP[i].cls; ++i) {
        if (!stricmp(STRIFE_ENEMY_XP[i].cls, actor_class)) {
            g_client->AwardXP(STRIFE_ENEMY_XP[i].xp, actor_class);
            return;
        }
    }
    g_client->AwardXP(10, actor_class);
}

extern "C" void OStrife_STAR_DrawHUDStatus(int screen_w, int screen_h) {
    if (!g_ready) return;
    char buf[128];
    snprintf(buf, sizeof(buf), "OASIS | XP: %d", g_client->GetXP());
    screen.DrawText(NewSmallFont, CR_GOLD, screen_w - 200, screen_h - 20,
                    buf, TAG_DONE);
}

extern "C" int OStrife_STAR_HandleKey(int key_code) {
    if (!g_ready) return 0;
    return g_client->HandleKey(key_code);
}

extern "C" int OStrife_STAR_IsReady(void) {
    return g_ready ? 1 : 0;
}
