/**
 * OBlood - OASIS STAR API Integration
 * Base engine: Raze (Blood backend)  https://github.com/ZDoom/Raze
 */

#include "oblood_ogengine_integration.h"
#include "OGEngineClient.h"

// Raze / GZDoom common headers
#include "c_dispatch.h"
#include "c_console.h"
#include "c_cvars.h"
#include "printf.h"
#include "i_time.h"
#include "v_draw.h"

#define OB_GAME_SOURCE "OBLOOD"

static OGEngineClient* g_client = nullptr;
static bool            g_ready  = false;

/* ── XP table ── */
struct BloodEnemy { int type; const char* name; int xp; };
static constexpr BloodEnemy BLOOD_ENEMY_XP[] = {
    { 1,  "CultistTommy",  20 }, { 2,  "CultistShotgun", 20 },
    { 3,  "CultistFlare",  20 }, { 4,  "ZombieAxe",      15 },
    { 5,  "ZombieButcher", 20 }, { 6,  "Gargoyle",       25 },
    { 7,  "GargoyleGreen", 30 }, { 8,  "Bat",             5 },
    { 9,  "GillBeast",     40 }, { 10, "Cerberus",        80 },
    { 11, "ShrimpHead",    30 }, { 12, "Tchernobog",     500 },
    { 0,  nullptr,          0 }
};

/* ── Item map ── */
struct BloodItem { const char* name; const char* category; int value; };
static constexpr BloodItem BLOOD_ITEM_MAP[] = {
    { "DoctorBag",          "consumable", 50  },
    { "MedKit",             "consumable", 25  },
    { "LifeSeed",           "consumable", 10  },
    { "ArmorBasic",         "armor",      50  },
    { "ArmorFlak",          "armor",      100 },
    { "FlareAmmo",          "ammo",        5  },
    { "ShellAmmo",          "ammo",        5  },
    { "BulletAmmo",         "ammo",        5  },
    { "HEAmmo",             "ammo",       10  },
    { "FlarePistol",        "weapon",     30  },
    { "SawnOffShotgun",     "weapon",     50  },
    { "TommyGun",           "weapon",     80  },
    { "SprayCan",           "weapon",     40  },
    { "NapalmLauncher",     "weapon",    100  },
    { "VoodooDoll",         "powerup",    20  },
    { "BeastVision",        "powerup",    15  },
    { "GraveBand",          "powerup",    20  },
    { "JumpBoots",          "powerup",    15  },
    { nullptr,              nullptr,       0  }
};

/* ── CCMD ── */
CCMD(star_oblood) {
    if (argv.argc() < 2) {
        Printf("star_oblood: version | inv | xp | login\n");
        return;
    }
    if (!g_client || !g_ready) { Printf("OBlood STAR not ready.\n"); return; }

    if (!stricmp(argv[1], "version")) {
        Printf("OBlood STAR v1.0 | %s\n", OB_GAME_SOURCE);
    } else if (!stricmp(argv[1], "inv")) {
        g_client->PrintInventory();
    } else if (!stricmp(argv[1], "xp")) {
        Printf("XP: %d\n", g_client->GetXP());
    } else if (!stricmp(argv[1], "login")) {
        if (argv.argc() >= 4)
            g_client->Login(argv[2], argv[3]);
        else
            Printf("Usage: star_oblood login <username> <password>\n");
    }
}

/* ── Exported C API ── */

extern "C" void OBlood_STAR_Init(const char* star_api_base_url,
                                  const char* oasis_json_path) {
    g_client = new OGEngineClient(OB_GAME_SOURCE, star_api_base_url,
                                   oasis_json_path);
    g_ready  = g_client->Initialize();
    if (g_ready)
        Printf("[OBlood] STAR API ready — Cabal beware.\n");
}

extern "C" void OBlood_STAR_Cleanup(void) {
    if (g_client) { g_client->Shutdown(); delete g_client; g_client = nullptr; }
    g_ready = false;
}

extern "C" void OBlood_STAR_Tick(void) {
    if (g_ready) g_client->Tick();
}

extern "C" void OBlood_STAR_OnItemPickup(int item_type, const char* item_name) {
    if (!g_ready) return;
    for (int i = 0; BLOOD_ITEM_MAP[i].name; ++i) {
        if (!stricmp(BLOOD_ITEM_MAP[i].name, item_name)) {
            g_client->AddInventoryItem(item_name, BLOOD_ITEM_MAP[i].category,
                                        BLOOD_ITEM_MAP[i].value);
            return;
        }
    }
    g_client->AddInventoryItem(item_name, "misc", 5);
}

extern "C" void OBlood_STAR_OnEnemyKilled(int enemy_type, const char* enemy_name,
                                           const char* killer) {
    if (!g_ready) return;
    for (int i = 0; BLOOD_ENEMY_XP[i].name; ++i) {
        if (BLOOD_ENEMY_XP[i].type == enemy_type) {
            g_client->AwardXP(BLOOD_ENEMY_XP[i].xp, BLOOD_ENEMY_XP[i].name);
            return;
        }
    }
    g_client->AwardXP(10, enemy_name);
}

extern "C" void OBlood_STAR_DrawHUDStatus(int screen_w, int screen_h) {
    if (!g_ready) return;
    char buf[128];
    snprintf(buf, sizeof(buf), "OASIS | XP: %d", g_client->GetXP());
    DrawText(twod, NewSmallFont, CR_GOLD, screen_w - 200, screen_h - 20,
             buf, DTA_FullscreenScale, FSMode_ScaleToScreen, TAG_DONE);
}

extern "C" int OBlood_STAR_HandleKey(int key_code) {
    if (!g_ready) return 0;
    return g_client->HandleKey(key_code);
}

extern "C" int OBlood_STAR_IsReady(void) {
    return g_ready ? 1 : 0;
}

static void on_auth_done(bool success, const char* msg) {
    if (success)
        Printf("[OBlood] Welcome to OBlood, Caleb.\n");
    else
        Printf("[OBlood] Auth failed: %s\n", msg);
}
