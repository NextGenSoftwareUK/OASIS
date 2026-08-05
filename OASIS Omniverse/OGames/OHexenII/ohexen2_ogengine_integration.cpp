/**
 * OHexenII - OASIS STAR API Integration
 * Base engine: uhexen2 (Hammer of Thyrion)  https://github.com/sezero/uhexen2
 *
 * Hexen II uses the Quake engine (id Tech 2 derivative) in C.  Hook sites are
 * in the engine C layer; OGEngineClient is called through the C wrapper below.
 */

#include "ohexen2_ogengine_integration.h"
#include "OGEngineClient.h"

#include <stdio.h>
#include <string.h>

#define OH2_GAME_SOURCE "OHEXEN2"

static OGEngineClient* g_client    = nullptr;
static bool            g_ready     = false;
static char            g_class[32] = "";

/* ── Hexen II monster roster ── */
struct H2Monster { const char* cls; int xp; };
static constexpr H2Monster H2_MONSTER_XP[] = {
    { "imp",              10 }, { "imp_crusherA",    15 },
    { "heresiarch",       80 }, { "tree_of_evil",    60 },
    { "dragonlord",       80 }, { "eidolon",         80 },
    { "praedator",        40 }, { "succubus",        30 },
    { "seraph_guardian",  40 }, { "centaur",         25 },
    { "medusa",           50 }, { "death_knight",    60 },
    { "evil_eye",         15 }, { "morcalavin",     500 },
    { nullptr,             0 }
};

/* ── Hexen II item/pickup map ── */
struct H2Item { const char* cls; const char* category; int value; };
static constexpr H2Item H2_ITEM_MAP[] = {
    { "item_health_vial",     "consumable", 10  },
    { "item_health_urn",      "consumable", 25  },
    { "item_health_phelm",    "consumable", 50  },
    { "item_armor_mesh",      "armor",      50  },
    { "item_armor_platemail", "armor",     100  },
    { "item_armor_enchanted", "armor",     150  },
    { "item_artifact_ring",   "powerup",   20   },
    { "item_artifact_torch",  "powerup",   10   },
    { "item_artifact_quartzflask","consumable",30},
    { "item_hammer",          "weapon",    40   },
    { "item_vorpal_blade",    "weapon",    50   },
    { "item_gauntlets",       "weapon",    60   },
    { "item_demonicclaws",    "weapon",    70   },
    { "rune_piece",           "key_item",   0   },
    { nullptr,                nullptr,      0   }
};

extern "C" void OHexenII_STAR_Init(const char* star_api_base_url,
                                    const char* oasis_json_path) {
    g_client = new OGEngineClient(OH2_GAME_SOURCE, star_api_base_url,
                                   oasis_json_path);
    g_ready  = g_client->Initialize();
    if (g_ready)
        fprintf(stdout, "[OHexenII] STAR API ready — Thyrion calls.\n");
}

extern "C" void OHexenII_STAR_Cleanup(void) {
    if (g_client) { g_client->Shutdown(); delete g_client; g_client = nullptr; }
    g_ready = false;
}

extern "C" void OHexenII_STAR_Tick(void) {
    if (g_ready) g_client->Tick();
}

extern "C" void OHexenII_STAR_OnClassSelected(const char* class_name,
                                               const char* player_name) {
    if (!g_ready) return;
    snprintf(g_class, sizeof(g_class), "%s", class_name ? class_name : "");
    g_client->SetAvatarAttribute("player_class", g_class);
    fprintf(stdout, "[OHexenII] Class: %s\n", g_class);
}

extern "C" void OHexenII_STAR_OnItemPickup(const char* item_classname,
                                            const char* item_name) {
    if (!g_ready) return;
    for (int i = 0; H2_ITEM_MAP[i].cls; ++i) {
        if (!strcasecmp(H2_ITEM_MAP[i].cls, item_classname)) {
            g_client->AddInventoryItem(item_name, H2_ITEM_MAP[i].category,
                                        H2_ITEM_MAP[i].value);
            return;
        }
    }
    g_client->AddInventoryItem(item_name, "misc", 5);
}

extern "C" void OHexenII_STAR_OnMonsterKilled(const char* monster_class,
                                               const char* killer) {
    if (!g_ready) return;
    for (int i = 0; H2_MONSTER_XP[i].cls; ++i) {
        if (!strcasecmp(H2_MONSTER_XP[i].cls, monster_class)) {
            g_client->AwardXP(H2_MONSTER_XP[i].xp, monster_class);
            return;
        }
    }
    g_client->AwardXP(10, monster_class ? monster_class : "Unknown");
}

extern "C" void OHexenII_STAR_DrawHUDStatus(int screen_w, int screen_h) {
    if (!g_ready) return;
    /* Draw via uhexen2's Draw_String / Sbar_Draw overrides. */
}

extern "C" int OHexenII_STAR_HandleKey(int key_code) {
    if (!g_ready) return 0;
    return g_client->HandleKey(key_code);
}

extern "C" int OHexenII_STAR_IsReady(void) {
    return g_ready ? 1 : 0;
}
