# OGEditor Asset Catalog

Specification for the shared OASIS asset catalog — how it is defined, generated, distributed, and consumed by all editors.

---

## 1. Canonical Source of Truth

The canonical asset catalog lives in UDB's OGEditorSDK:

```
UltimateDoomBuilder/Source/OGEditorSDK/OGAssetCatalog.cs
```

This C# file defines every OASIS thing type across all 10 OGames (~140 assets). It is the **single source of truth** — all other representations (FGD files, ENT files, DEF files, JSON exports) are derived from it.

The secondary reference (which should be kept in sync but is less authoritative):

```
UltimateDoomBuilder/Source/OASISEditorSDK/OASISAssetCatalog.cs
```

---

## 2. Thing Type Ranges

| OGame | Range | Notes |
|-------|-------|-------|
| ODOOM | Native Doom types | Doom thing types (1–4999) |
| OQuake | 5001–5899 | |
| Portal (all games) | 5900 | `oasis_portal_enter` / `oasis_portal_exit` |
| OQuake2 / OQuake2-RTX | 6001–6899 | Shared range, both contribute |
| OQuake3 | 7001–7899 | |
| ODuke3D / ODuke3D-RT | 8001–8899 | |
| OWolf3D | 9001–9499 | |
| ODOOM3 / ODOOM3-BFG | 10001–10999 | Shared range |

---

## 3. JSON Export Format

The catalog is exported as `oasis_asset_catalog.json` and placed in:

```
OASIS Omniverse/OGEngineClient/oasis_asset_catalog.json
```

This file is read by all satellite editors at startup via `ogeditor_get_assets_for_game()`.

### Full schema

```json
{
  "schema_version": "1.0",
  "generated_at":   "2026-08-02T12:00:00Z",
  "generated_from": "OGAssetCatalog.cs",

  "shared": {
    "portal_enter": {
      "thing_type": 5900,
      "classname":  "oasis_portal_enter",
      "name":       "Portal Enter",
      "category":   "Portals",
      "description": "Teleport source — brush trigger the player walks into"
    },
    "portal_exit": {
      "thing_type": 5900,
      "classname":  "oasis_portal_exit",
      "name":       "Portal Exit",
      "category":   "Portals",
      "description": "Teleport destination — spawn point for arriving players"
    }
  },

  "games": {
    "oquake": {
      "display_name": "OQuake",
      "thing_range":  [5001, 5899],
      "assets": [
        { "thing_type": 5001, "name": "Silver Key",        "classname": "item_key_silver",    "category": "Keys"    },
        { "thing_type": 5002, "name": "Gold Key",          "classname": "item_key_gold",      "category": "Keys"    },
        { "thing_type": 5011, "name": "Shotgun",           "classname": "weapon_supershotgun","category": "Weapons" },
        { "thing_type": 5012, "name": "Nailgun",           "classname": "weapon_nailgun",     "category": "Weapons" },
        { "thing_type": 5013, "name": "Super Nailgun",     "classname": "weapon_supernailgun","category": "Weapons" },
        { "thing_type": 5014, "name": "Grenade Launcher",  "classname": "weapon_grenadelauncher","category":"Weapons"},
        { "thing_type": 5015, "name": "Rocket Launcher",   "classname": "weapon_rocketlauncher","category":"Weapons"},
        { "thing_type": 5016, "name": "Thunderbolt",       "classname": "weapon_lightning",   "category": "Weapons" },
        { "thing_type": 5021, "name": "Shells",            "classname": "item_shells",        "category": "Ammo"    },
        { "thing_type": 5022, "name": "Nails",             "classname": "item_spikes",        "category": "Ammo"    },
        { "thing_type": 5023, "name": "Rockets",           "classname": "item_rockets",       "category": "Ammo"    },
        { "thing_type": 5024, "name": "Cells",             "classname": "item_cells",         "category": "Ammo"    },
        { "thing_type": 5031, "name": "Small Health",      "classname": "item_health",        "category": "Health"  },
        { "thing_type": 5032, "name": "Large Health",      "classname": "item_health",        "category": "Health"  },
        { "thing_type": 5033, "name": "Megahealth",        "classname": "item_megahealth",    "category": "Health"  },
        { "thing_type": 5041, "name": "Green Armour",      "classname": "item_armor1",        "category": "Armour"  },
        { "thing_type": 5042, "name": "Yellow Armour",     "classname": "item_armor2",        "category": "Armour"  },
        { "thing_type": 5043, "name": "Red Armour",        "classname": "item_armorInv",      "category": "Armour"  },
        { "thing_type": 5101, "name": "Grunt",             "classname": "monster_army",       "category": "Monsters"},
        { "thing_type": 5102, "name": "Rottweiler",        "classname": "monster_dog",        "category": "Monsters"},
        { "thing_type": 5103, "name": "Ogre",              "classname": "monster_ogre",       "category": "Monsters"},
        { "thing_type": 5104, "name": "Fiend",             "classname": "monster_demon1",     "category": "Monsters"},
        { "thing_type": 5105, "name": "Zombie",            "classname": "monster_zombie",     "category": "Monsters"},
        { "thing_type": 5106, "name": "Knight",            "classname": "monster_knight",     "category": "Monsters"},
        { "thing_type": 5107, "name": "Death Knight",      "classname": "monster_hell_knight","category": "Monsters"},
        { "thing_type": 5108, "name": "Wizard",            "classname": "monster_wizard",     "category": "Monsters"},
        { "thing_type": 5109, "name": "Scrag",             "classname": "monster_scrag",      "category": "Monsters"},
        { "thing_type": 5110, "name": "Spawn",             "classname": "monster_tarbaby",    "category": "Monsters"},
        { "thing_type": 5111, "name": "Vore",              "classname": "monster_shalrath",   "category": "Monsters"},
        { "thing_type": 5112, "name": "Shambler",          "classname": "monster_shambler",   "category": "Monsters"}
      ]
    },

    "oquake2": {
      "display_name": "OQuake2",
      "thing_range":  [6001, 6899],
      "assets": [
        { "thing_type": 6001, "name": "Blue Key",          "classname": "item_key_blue_key",  "category": "Keys"    },
        { "thing_type": 6002, "name": "Red Key",           "classname": "item_key_red_key",   "category": "Keys"    },
        { "thing_type": 6003, "name": "Commander's Head",  "classname": "item_key_power_cube","category": "Keys"    },
        { "thing_type": 6011, "name": "Blaster",           "classname": "weapon_blaster",     "category": "Weapons" },
        { "thing_type": 6012, "name": "Shotgun",           "classname": "weapon_shotgun",     "category": "Weapons" },
        { "thing_type": 6013, "name": "Super Shotgun",     "classname": "weapon_supershotgun","category": "Weapons" },
        { "thing_type": 6014, "name": "Machinegun",        "classname": "weapon_machinegun",  "category": "Weapons" },
        { "thing_type": 6015, "name": "Chaingun",          "classname": "weapon_chaingun",    "category": "Weapons" },
        { "thing_type": 6016, "name": "Grenade Launcher",  "classname": "weapon_grenadelauncher","category":"Weapons"},
        { "thing_type": 6017, "name": "Rocket Launcher",   "classname": "weapon_rocketlauncher","category":"Weapons"},
        { "thing_type": 6018, "name": "Hyperblaster",      "classname": "weapon_hyperblaster","category": "Weapons" },
        { "thing_type": 6019, "name": "Railgun",           "classname": "weapon_railgun",     "category": "Weapons" },
        { "thing_type": 6020, "name": "BFG10K",            "classname": "weapon_bfg",         "category": "Weapons" },
        { "thing_type": 6021, "name": "Bullets",           "classname": "ammo_bullets",       "category": "Ammo"    },
        { "thing_type": 6022, "name": "Shells",            "classname": "ammo_shells",        "category": "Ammo"    },
        { "thing_type": 6023, "name": "Grenades",          "classname": "ammo_grenades",      "category": "Ammo"    },
        { "thing_type": 6024, "name": "Rockets",           "classname": "ammo_rockets",       "category": "Ammo"    },
        { "thing_type": 6025, "name": "Cells",             "classname": "ammo_cells",         "category": "Ammo"    },
        { "thing_type": 6026, "name": "Slugs",             "classname": "ammo_slugs",         "category": "Ammo"    },
        { "thing_type": 6031, "name": "Small Health",      "classname": "item_health_small",  "category": "Health"  },
        { "thing_type": 6032, "name": "Health",            "classname": "item_health",        "category": "Health"  },
        { "thing_type": 6033, "name": "Mega Health",       "classname": "item_health_mega",   "category": "Health"  },
        { "thing_type": 6041, "name": "Jacket Armor",      "classname": "item_armor_jacket",  "category": "Armour"  },
        { "thing_type": 6042, "name": "Combat Armor",      "classname": "item_armor_combat",  "category": "Armour"  },
        { "thing_type": 6043, "name": "Body Armor",        "classname": "item_armor_body",    "category": "Armour"  },
        { "thing_type": 6101, "name": "Soldier",           "classname": "monster_soldier",    "category": "Monsters"},
        { "thing_type": 6102, "name": "Infantry",          "classname": "monster_infantry",   "category": "Monsters"},
        { "thing_type": 6103, "name": "Gunner",            "classname": "monster_gunner",     "category": "Monsters"},
        { "thing_type": 6104, "name": "Berserker",         "classname": "monster_berserk",    "category": "Monsters"},
        { "thing_type": 6105, "name": "Gladiator",         "classname": "monster_gladiator",  "category": "Monsters"},
        { "thing_type": 6106, "name": "Flyer",             "classname": "monster_flyer",      "category": "Monsters"},
        { "thing_type": 6107, "name": "Medic",             "classname": "monster_medic",      "category": "Monsters"},
        { "thing_type": 6108, "name": "Parasite",          "classname": "monster_parasite",   "category": "Monsters"},
        { "thing_type": 6109, "name": "Brain",             "classname": "monster_brain",      "category": "Monsters"},
        { "thing_type": 6110, "name": "Supertank",         "classname": "monster_supertank",  "category": "Monsters"},
        { "thing_type": 6111, "name": "Tank",              "classname": "monster_tank",       "category": "Monsters"},
        { "thing_type": 6112, "name": "Makron",            "classname": "monster_makron",     "category": "Monsters"}
      ]
    },

    "oquake3": {
      "display_name": "OQuake3",
      "thing_range":  [7001, 7899],
      "assets": [
        { "thing_type": 7001, "name": "Machinegun",        "classname": "weapon_machinegun",  "category": "Weapons" },
        { "thing_type": 7002, "name": "Shotgun",           "classname": "weapon_shotgun",     "category": "Weapons" },
        { "thing_type": 7003, "name": "Grenade Launcher",  "classname": "weapon_grenadelauncher","category":"Weapons"},
        { "thing_type": 7004, "name": "Rocket Launcher",   "classname": "weapon_rocketlauncher","category":"Weapons"},
        { "thing_type": 7005, "name": "Plasma Gun",        "classname": "weapon_plasmagun",   "category": "Weapons" },
        { "thing_type": 7006, "name": "Railgun",           "classname": "weapon_railgun",     "category": "Weapons" },
        { "thing_type": 7007, "name": "Lightning Gun",     "classname": "weapon_lightning",   "category": "Weapons" },
        { "thing_type": 7008, "name": "BFG",               "classname": "weapon_bfg",         "category": "Weapons" },
        { "thing_type": 7021, "name": "Bullets",           "classname": "ammo_bullets",       "category": "Ammo"    },
        { "thing_type": 7022, "name": "Shells",            "classname": "ammo_shells",        "category": "Ammo"    },
        { "thing_type": 7023, "name": "Grenades",          "classname": "ammo_grenades",      "category": "Ammo"    },
        { "thing_type": 7024, "name": "Rockets",           "classname": "ammo_rockets",       "category": "Ammo"    },
        { "thing_type": 7025, "name": "Cells",             "classname": "ammo_cells",         "category": "Ammo"    },
        { "thing_type": 7026, "name": "Lightning",         "classname": "ammo_lightning",     "category": "Ammo"    },
        { "thing_type": 7031, "name": "Small Health",      "classname": "item_health_small",  "category": "Health"  },
        { "thing_type": 7032, "name": "Health",            "classname": "item_health",        "category": "Health"  },
        { "thing_type": 7033, "name": "Mega Health",       "classname": "item_health_mega",   "category": "Health"  },
        { "thing_type": 7041, "name": "Shard",             "classname": "item_armor_shard",   "category": "Armour"  },
        { "thing_type": 7042, "name": "Combat Armor",      "classname": "item_armor_combat",  "category": "Armour"  },
        { "thing_type": 7043, "name": "Body Armor",        "classname": "item_armor_body",    "category": "Armour"  }
      ]
    },

    "oduke3d": {
      "display_name": "ODuke3D",
      "thing_range":  [8001, 8899],
      "assets": [
        { "thing_type": 8001, "name": "Pistol",            "classname": "PISTOL",             "category": "Weapons" },
        { "thing_type": 8002, "name": "Shotgun",           "classname": "SHOTGUN",            "category": "Weapons" },
        { "thing_type": 8003, "name": "Chaingun Cannon",   "classname": "CHAINGUNGUN",        "category": "Weapons" },
        { "thing_type": 8004, "name": "RPG",               "classname": "RPGSPRITE",          "category": "Weapons" },
        { "thing_type": 8005, "name": "Pipe Bomb",         "classname": "PIPEBOMB",           "category": "Weapons" },
        { "thing_type": 8006, "name": "Shrinker",          "classname": "SHRINKERSPRITE",     "category": "Weapons" },
        { "thing_type": 8007, "name": "Devastator",        "classname": "DEVISTATORSPRITE",   "category": "Weapons" },
        { "thing_type": 8008, "name": "Laser Tripbomb",    "classname": "TRIPBOMBSPRITE",     "category": "Weapons" },
        { "thing_type": 8009, "name": "Freeze Ray",        "classname": "FREEZESPRITE",       "category": "Weapons" },
        { "thing_type": 8101, "name": "Blue Key",          "classname": "ACCESSCARD",         "category": "Keys"    },
        { "thing_type": 8102, "name": "Red Key",           "classname": "ACCESSCARD",         "category": "Keys"    },
        { "thing_type": 8103, "name": "Yellow Key",        "classname": "ACCESSCARD",         "category": "Keys"    }
      ]
    },

    "owolf3d": {
      "display_name": "OWolf3D",
      "thing_range":  [9001, 9499],
      "assets": [
        { "thing_type": 9001, "name": "Knife",             "classname": "weapon_knife",       "category": "Weapons" },
        { "thing_type": 9002, "name": "Pistol",            "classname": "weapon_pistol",      "category": "Weapons" },
        { "thing_type": 9003, "name": "Machine Gun",       "classname": "weapon_machinegun",  "category": "Weapons" },
        { "thing_type": 9004, "name": "Chain Gun",         "classname": "weapon_chaingun",    "category": "Weapons" },
        { "thing_type": 9101, "name": "Gold Key",          "classname": "key_gold",           "category": "Keys"    },
        { "thing_type": 9102, "name": "Silver Key",        "classname": "key_silver",         "category": "Keys"    },
        { "thing_type": 9201, "name": "Dog",               "classname": "monster_dog",        "category": "Monsters"},
        { "thing_type": 9202, "name": "Guard",             "classname": "monster_guard",      "category": "Monsters"},
        { "thing_type": 9203, "name": "Officer",           "classname": "monster_officer",    "category": "Monsters"},
        { "thing_type": 9204, "name": "SS",                "classname": "monster_ss",         "category": "Monsters"},
        { "thing_type": 9205, "name": "Hans Grosse",       "classname": "monster_hans",       "category": "Monsters"},
        { "thing_type": 9206, "name": "Dr. Schabbs",       "classname": "monster_schabbs",    "category": "Monsters"},
        { "thing_type": 9207, "name": "Hitler",            "classname": "monster_hitler",     "category": "Monsters"}
      ]
    },

    "odoom3": {
      "display_name": "ODOOM3",
      "thing_range":  [10001, 10999],
      "assets": [
        { "thing_type": 10001, "name": "Blue Key",         "classname": "item_keyblue",       "category": "Keys"    },
        { "thing_type": 10002, "name": "Yellow Key",       "classname": "item_keyyellow",     "category": "Keys"    },
        { "thing_type": 10003, "name": "Red Key",          "classname": "item_keyred",        "category": "Keys"    },
        { "thing_type": 10011, "name": "Pistol",           "classname": "weapon_pistol",      "category": "Weapons" },
        { "thing_type": 10012, "name": "Shotgun",          "classname": "weapon_shotgun",     "category": "Weapons" },
        { "thing_type": 10013, "name": "Machine Gun",      "classname": "weapon_machinegun",  "category": "Weapons" },
        { "thing_type": 10014, "name": "Chainsaw",         "classname": "weapon_chainsaw",    "category": "Weapons" },
        { "thing_type": 10015, "name": "Plasma Gun",       "classname": "weapon_plasmagun",   "category": "Weapons" },
        { "thing_type": 10016, "name": "Rocket Launcher",  "classname": "weapon_rocketlauncher","category":"Weapons"},
        { "thing_type": 10017, "name": "BFG 9000",         "classname": "weapon_bfg",         "category": "Weapons" },
        { "thing_type": 10018, "name": "Soul Cube",        "classname": "weapon_soulcube",    "category": "Weapons" },
        { "thing_type": 10101, "name": "Zombie",           "classname": "monster_zombie",     "category": "Monsters"},
        { "thing_type": 10102, "name": "Imp",              "classname": "monster_demon_imp",  "category": "Monsters"},
        { "thing_type": 10103, "name": "Pinky",            "classname": "monster_demon_pinky","category": "Monsters"},
        { "thing_type": 10104, "name": "Hell Knight",      "classname": "monster_demon_hellknight","category":"Monsters"},
        { "thing_type": 10105, "name": "Revenant",         "classname": "monster_demon_revenant","category":"Monsters"},
        { "thing_type": 10106, "name": "Mancubus",         "classname": "monster_demon_mancubus","category":"Monsters"},
        { "thing_type": 10107, "name": "Archvile",         "classname": "monster_demon_archvile","category":"Monsters"},
        { "thing_type": 10108, "name": "Vagary (Boss)",    "classname": "monster_boss_vagary","category": "Monsters"},
        { "thing_type": 10109, "name": "Sabaoth (Boss)",   "classname": "monster_boss_sabaoth","category":"Monsters"},
        { "thing_type": 10110, "name": "Cyberdemon (Boss)","classname": "monster_boss_cyberdemon","category":"Monsters"}
      ]
    }
  }
}
```

---

## 4. Generating the JSON from C#

A small .NET CLI tool (to be built at `UltimateDoomBuilder/Tools/ExportAssetCatalog/`) reads `OGAssetCatalog.cs` via reflection and writes the JSON:

```csharp
// ExportAssetCatalog/Program.cs
var catalog = new OGAssetCatalog();
var allAssets = catalog.GetAll();

var doc = new {
    schema_version = "1.0",
    generated_at   = DateTime.UtcNow.ToString("o"),
    generated_from = "OGAssetCatalog.cs",
    games          = BuildGamesDict(allAssets)
};

File.WriteAllText(outPath, JsonSerializer.Serialize(doc,
    new JsonSerializerOptions { WriteIndented = true }));
```

This tool should be run as part of the UDB build pipeline whenever `OGAssetCatalog.cs` changes, and the resulting `oasis_asset_catalog.json` committed to the OASIS2 repo.

---

## 5. How Editors Consume the Catalog

### Via OGEditorClient.dll (satellite editors)

```c
// On startup
OGAsset assets[512];
int count = ogeditor_get_all_assets(assets, 512);
// Populate entity browser / thing type list from `assets`
```

The DLL caches the catalog in memory and re-reads the JSON if it detects a newer file timestamp.

### Via JSON directly (fallback if DLL not found)

Each editor can fall back to parsing `oasis_asset_catalog.json` directly using its own JSON library:

- TrenchBroom: `nlohmann/json` (already a dependency)
- NetRadiant: `cJSON` (available in the codebase)
- DarkRadiant: `jsoncpp` or `nlohmann/json`

### In FGD / ENT / DEF files (static fallback)

The FGD, ENT, and DEF files committed to each editor repo are a last-resort static fallback for users who have no DLL and no JSON. They are hand-maintained and should be regenerated from the JSON periodically.

---

## 6. Versioning and Updates

The catalog JSON carries a `schema_version`. When the DLL loads it:

- If `schema_version` matches: use it
- If `schema_version` is older: use it but warn in the OASIS panel ("Asset catalog is outdated — update OGEditorSDK")
- If `schema_version` is newer: parse what it can, warn about unknown fields

The catalog JSON should be distributed:
1. Bundled with the OGEditorSDK NuGet package (for UDB)
2. Bundled with `OGEditorClient.dll` (for satellite editors)
3. Available at `GET /api/assets/catalog` on the STAR Web API (for live refresh)
