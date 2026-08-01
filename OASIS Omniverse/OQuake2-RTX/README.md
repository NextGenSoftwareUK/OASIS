# OQuake2-RTX — OASIS STAR API Integration

NVIDIA's Q2 RTX ray-traced Quake 2 remaster, integrated with the OASIS STAR API (cross-game inventory, XP, quests, NFT minting).

Q2 RTX is built on the Yamagi Quake II codebase with NVIDIA's Vulkan RTX renderer. It shares the same OASIS thing type range (6000–6899) as OQuake2 — the same game content, different renderer. Both games contribute to the same cross-game OASIS inventory.

- Base engine: [Q2 RTX (NVIDIA)](https://github.com/NVIDIA/Q2RTX) (GPL-2.0)
- OASIS thing type range: **6000–6899** (shared with OQuake2)
- Portal thing type: **5900** (shared cross-game)
- Integration prefix: `OQuake2RTX_STAR_`

---

## Quick Start

### Windows

```
1. Open OQuake2-RTX\ in File Explorer
2. Double-click BUILD_OQUAKE2RTX.bat
3. On first run, choose [I] for incremental build
4. The script builds OGEngineClient and the Q2 RTX engine, then copies output to build\
5. Place baseq2\pak0.pak next to OQUAKE2RTX.exe
6. Run OQUAKE2RTX.exe
```

Requirements: Visual Studio 2022, CMake, Vulkan SDK, NVIDIA RTX GPU (or software RTX fallback).

### Linux / macOS

```bash
export Q2RTX_SRC=$HOME/src/Q2RTX   # clone https://github.com/NVIDIA/Q2RTX
chmod +x BUILD_OQUAKE2RTX.sh
./BUILD_OQUAKE2RTX.sh
# place baseq2/pak0.pak next to build/OQUAKE2RTX
./build/OQUAKE2RTX
```

Requirements: GCC/Clang, CMake, Vulkan SDK (linux-only; macOS via MoltenVK), libvulkan-dev.

---

## Architecture

```
Q2 RTX Engine (Vulkan RTX)
    |
    |-- OQuake2RTX_STAR_Init()       — startup
    |-- OQuake2RTX_STAR_PollItems()  — per-frame pump (ogengine_sync_pump)
    |-- OQuake2RTX_STAR_OnKeyPickup / CheckDoorAccess / OnItemPickup
    |-- OQuake2RTX_STAR_OnMonsterKilled / OnBossKilled
    |-- OQuake2RTX_STAR_Cleanup()    — shutdown
         |
         v
    ogengine.dll / libstar_api.so    (OGEngineClient NativeAOT)
         |
         v
    OASIS STAR API (cross-game cloud inventory, XP, quests, NFT minting)
```

**Important:** Q2 RTX shares the 6xxx thing type range with OQuake2. Items picked up in OQuake2-RTX appear in OQuake2's inventory and vice versa — they are the same game content. Cross-game portal (type 5900) connects to all OASIS Omniverse games.

---

## OASIS Thing Type Range

| Range      | Category           |
|------------|--------------------|
| 5900       | Portal (cross-game)|
| 6001–6002  | Keys               |
| 6100–6109  | Weapons            |
| 6200–6202  | Armor              |
| 6300–6302  | Health             |
| 6400–6405  | Ammo               |
| 6500–6509  | Monsters           |
| 6510–6899  | Reserved           |

---

## Files

| File                                    | Purpose                                                 |
|-----------------------------------------|---------------------------------------------------------|
| `oquake2rtx_ogengine_integration.h`     | Header — thing types, function declarations             |
| `oquake2rtx_ogengine_integration.c`     | Implementation — all integration logic                  |
| `ogengine.h`                            | OASIS STAR API C ABI (stub; build script overwrites)    |
| `ogengine_sync.h`                       | Async sync layer (stub; build script overwrites)        |
| `oasisstar.json`                        | Config — STAR API URLs, session, monster mint flags     |
| `BUILD_OQUAKE2RTX.bat`                  | Windows build script                                    |
| `BUILD_OQUAKE2RTX.sh`                   | Linux/macOS build script                                |
| `Docs\INTEGRATION_INSTRUCTIONS.md`     | Full integration guide                                  |

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
  "mint_monster_oquake2rtx_makron": 1
}
```

See `oasisstar.json` and `Docs\INTEGRATION_INSTRUCTIONS.md` for all fields.

---

## Monster NFT Minting

Each monster has a per-entry flag in `oasisstar.json`:

```
mint_monster_oquake2rtx_gunner, mint_monster_oquake2rtx_gladiator,
mint_monster_oquake2rtx_tank, mint_monster_oquake2rtx_makron,
mint_monster_oquake2rtx_jorg, mint_monster_oquake2rtx_brain,
mint_monster_oquake2rtx_floater, mint_monster_oquake2rtx_mutant,
mint_monster_oquake2rtx_medic, mint_monster_oquake2rtx_soldier
```

Set to `1` to mint an NFT on kill, `0` to skip. The global `mint_monsters` flag can disable all minting.

---

## Integration Hook Points

```c
// At engine startup:
OQuake2RTX_STAR_Init();

// Every game frame:
OQuake2RTX_STAR_PollItems();

// On key pickup:
OQuake2RTX_STAR_OnKeyPickup("silver_key");

// On locked door touch (player lacks key locally):
if (OQuake2RTX_STAR_CheckDoorAccess("door_silver", "silver_key")) { /* open door */ }

// On item pickup:
OQuake2RTX_STAR_OnItemPickup("Railgun", "Weapon", 1, NULL);

// On monster kill:
OQuake2RTX_STAR_OnMonsterKilled("monster_makron");

// At engine shutdown:
OQuake2RTX_STAR_Cleanup();
```

See `Docs\INTEGRATION_INSTRUCTIONS.md` for complete step-by-step integration instructions.

---

## Credits

- **Q2 RTX engine**: NVIDIA Corporation and Q2 RTX contributors (GPL-2.0)
- **Yamagi Quake II base**: Yamagi Burmeister and contributors
- **OASIS STAR API**: Next Generation Software Ltd / NextGen Soft Ltd
- **Integration**: OASIS Omniverse project
