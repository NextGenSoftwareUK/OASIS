# OQuake3 — OASIS STAR API Integration

Quake3e (open-source Quake III Arena engine), integrated with the OASIS STAR API (cross-game inventory, XP, quests, NFT minting).

Quake III Arena is an arena/deathmatch game — it has no traditional key/door locks. Runes (from Q3:TA Harvester/Overload modes), powerups, and bot kills fill the collectible and XP roles in OASIS. Cross-game portal (type 5900) connects OQuake3 to all OASIS Omniverse games.

- Base engine: [Quake3e (ec-)](https://github.com/ec-/Quake3e) (GPL-2.0)
- OASIS thing type range: **7000–7899**
- Portal thing type: **5900** (shared cross-game)
- Integration prefix: `OQuake3_STAR_`

---

## Quick Start

### Windows

```
1. Open OQuake3\ in File Explorer
2. Double-click BUILD_OQUAKE3.bat
3. On first run, choose [I] for incremental build
4. The script builds OGEngineClient and Quake3e, then copies output to build\
5. Place baseq3\pak0.pk3 next to OQUAKE3.exe
6. Run OQUAKE3.exe
```

Requirements: Visual Studio 2022, CMake.

### Linux / macOS

```bash
export Q3E_SRC=$HOME/src/Quake3e    # clone https://github.com/ec-/Quake3e
chmod +x BUILD_OQUAKE3.sh
./BUILD_OQUAKE3.sh
# place baseq3/pak0.pk3 next to build/OQUAKE3
./build/OQUAKE3
```

Requirements: GCC/Clang, CMake, SDL2 (for Quake3e).

---

## Architecture

```
Quake3e Engine (Q3 Arena)
    |
    |-- OQuake3_STAR_Init()          — startup
    |-- OQuake3_STAR_PollItems()     — per-frame pump (ogengine_sync_pump)
    |-- OQuake3_STAR_OnRunePickup / HasRune
    |-- OQuake3_STAR_OnItemPickup    — weapons, armor, health, ammo, powerups
    |-- OQuake3_STAR_OnBotKilled / OnPlayerFragged
    |-- OQuake3_STAR_Cleanup()       — shutdown
         |
         v
    ogengine.dll / libstar_api.so    (OGEngineClient NativeAOT)
         |
         v
    OASIS STAR API (cross-game cloud inventory, XP, quests, NFT minting)
```

**Note:** Q3 is an arena game — no doors or traditional keys. Runes from Team Arena modes substitute. Cross-game portal (type 5900) connects OQuake3 to OQuake, OQuake2, ODOOM, and all other OASIS Omniverse games.

---

## OASIS Thing Type Range

| Range      | Category           |
|------------|--------------------|
| 5900       | Portal (cross-game)|
| 7001–7004  | Runes              |
| 7100–7108  | Weapons            |
| 7200–7202  | Armor              |
| 7300–7302  | Health             |
| 7400–7407  | Ammo               |
| 7450–7454  | Powerups           |
| 7500–7503  | Bots               |
| 7504–7899  | Reserved           |

---

## Files

| File                                  | Purpose                                               |
|---------------------------------------|-------------------------------------------------------|
| `oquake3_ogengine_integration.h`      | Header — thing types, function declarations           |
| `oquake3_ogengine_integration.c`      | Implementation — all integration logic                |
| `ogengine.h`                          | OASIS STAR API C ABI (stub; build script overwrites)  |
| `ogengine_sync.h`                     | Async sync layer (stub; build script overwrites)      |
| `oasisstar.json`                      | Config — STAR API URLs, session, bot mint flags       |
| `BUILD_OQUAKE3.bat`                   | Windows build script                                  |
| `BUILD_OQUAKE3.sh`                    | Linux/macOS build script                              |
| `Docs\INTEGRATION_INSTRUCTIONS.md`  | Full integration guide                                |

---

## Configuration (oasisstar.json)

Key fields:

```json
{
  "ogengine_url": "http://localhost:8888",
  "oasis_api_url": "http://localhost:7777",
  "saved_jwt": "",
  "offline_mode": 0,
  "mint_monsters": 1,
  "use_health_on_pickup": 0,
  "mint_monster_oquake3_xaero": 1
}
```

See `oasisstar.json` and `Docs\INTEGRATION_INSTRUCTIONS.md` for all fields.

---

## Bot NFT Minting

Each bot has a per-entry flag in `oasisstar.json`:

```
mint_monster_oquake3_grunt, mint_monster_oquake3_klesk,
mint_monster_oquake3_xaero, mint_monster_oquake3_orbb
```

Set to `1` to mint an NFT on kill, `0` to skip. The global `mint_monsters` flag can disable all minting. Xaero is the Q3 SP final boss — flagged `is_boss=1` and awards 200 XP by default.

---

## Integration Hook Points

```c
// At engine startup:
OQuake3_STAR_Init();

// Every game frame:
OQuake3_STAR_PollItems();

// On rune pickup (Q3:TA modes):
OQuake3_STAR_OnRunePickup("rune_strength");

// Check if player has rune in OASIS (cross-game):
if (OQuake3_STAR_HasRune("rune_haste")) { /* apply haste */ }

// On item pickup:
OQuake3_STAR_OnItemPickup("Railgun", "Weapon", 1, NULL);
OQuake3_STAR_OnItemPickup("Quad Damage", "Powerup", 1, NULL);

// On bot kill (PvE / single-player):
OQuake3_STAR_OnBotKilled("bot_xaero");

// On player frag (PvP):
OQuake3_STAR_OnPlayerFragged("EnemyPlayer", 0);

// At engine shutdown:
OQuake3_STAR_Cleanup();
```

See `Docs\INTEGRATION_INSTRUCTIONS.md` for complete step-by-step integration instructions.

---

## Credits

- **Quake3e engine**: ec- and Quake3e contributors (GPL-2.0)
- **Quake III Arena**: id Software / Bethesda Softworks
- **OASIS STAR API**: Next Generation Software Ltd / NextGen Soft Ltd
- **Integration**: OASIS Omniverse project
