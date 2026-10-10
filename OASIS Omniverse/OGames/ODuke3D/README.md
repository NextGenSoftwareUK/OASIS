# ODuke3D – Duke Nukem 3D + OASIS STAR API

**ODuke3D** is a fork of [EDuke32](https://eduke32.com) (the premier Duke Nukem 3D source port) with the **OASIS STAR API** integrated for cross-game features in the OASIS Omniverse. Keys collected in **ODOOM**, **OQuake**, **ODOOM3** (Doom 3 classic), **ODOOM3-BFG** (Doom 3 BFG), or **ODuke3D-RT** (ray-traced Duke) can open doors in ODuke3D and vice versa.

**ODuke3D** handles classic Duke Nukem 3D (DN3D.GRP).  For modern ray-traced graphics see **[ODuke3D-RT](../ODuke3D-RT/README.md)** (Duke-RT fork).

ODuke3D is a fork of EDuke32 (EDuke32 contributors, Jonathon Fowler, Richard Gobeille, et al.) and is licensed under **GPL-2.0**. By NextGen World Ltd.

## Quick start

### Windows

1. **Prerequisites:** MinGW-w64 with `make` and `gcc` in PATH (or Visual Studio for MSVC). ODuke3D/EDuke32 clone at `C:\Source\ODuke3D`. See [Docs/WINDOWS_INTEGRATION.md](Docs/WINDOWS_INTEGRATION.md).

2. **Build:** From this folder run:
   ```batch
   BUILD_ODUKE3D.bat
   ```
   Output: **eduke32.exe** in the ODuke3D source root. Put `duke3d.grp` (game data) in the same directory or pass `-j` path.

3. **Run:** Use **RUN_ODUKE3D.bat** to build (if needed) and launch, or run `eduke32.exe -j C:\Duke3D` directly.

4. **STAR API:** Set `STAR_USERNAME`/`STAR_PASSWORD` or `OGENGINE_KEY`/`STAR_AVATAR_ID` for cross-game keys.

### Linux / macOS

1. **Prerequisites:** gcc, GNU make, SDL2, `$HOME/Source/ODuke3D` (EDuke32 fork). See BUILD_ODUKE3D.sh header.

2. **Build:**
   ```bash
   ./BUILD_ODUKE3D.sh
   ```

3. **Run:**
   ```bash
   ./RUN_ODUKE3D.sh ~/Duke3D
   ```

## Architecture

```
oduke3d_ogengine_integration.c   (ODuke3D engine hooks + HUD/GUI)
         ↓
    OGLib  (shared C library — config, beamin, session shims)
         ↓
  OGEngineClient  (C# NativeAOT → ogengine.dll)
         ↓
  WEB4 / WEB5 OASIS APIs
```

## OASIS GUI features

All features match the ODOOM and OQuake integration:

| Feature | Key | Description |
|---------|-----|-------------|
| Inventory popup | **I** | Browse and use OASIS inventory items |
| Quest popup | **Q** | View active OASIS quests |
| Send to Avatar | **A** (in inventory) | Send selected item to your avatar |
| Send to Clan | **C** (in inventory) | Send selected item to your clan |
| OASIS avatar face | HUD | Duke status bar face replaced by OASIS avatar when beamed in |
| XP display | HUD top-right | Running OASIS XP total |
| Beamed-in label | HUD top-left | `OASIS: username` when connected |
| Version display | HUD bottom-right | ODuke3D version string |
| Toast notifications | HUD centre | Key pickups, XP awards, cross-game door events |

Input is blocked during open popups (movement, fire, use) — press **I**, **Q**, or **Esc** to close.

## Cross-game keys

| ODuke3D key card | Maps to |
|-----------------|---------|
| Blue access card | ODOOM blue/yellow keycard, OQuake gold_key, ODOOM3/ODOOM3-BFG blue_key, ODuke3D-RT blue_key |
| Red access card  | ODOOM red keycard, OQuake silver_key, ODOOM3/ODOOM3-BFG red_key, ODuke3D-RT red_key |
| Yellow access card | same pool as blue (Duke3D uses 3 colours → maps to ODOOM/Quake equivalents) |

## Documentation

| Document | Description |
|----------|-------------|
| [Docs/WINDOWS_INTEGRATION.md](Docs/WINDOWS_INTEGRATION.md) | Full Windows setup and troubleshooting |
| [Docs/INTEGRATION_INSTRUCTIONS.md](Docs/INTEGRATION_INSTRUCTIONS.md) | How to integrate OASIS STAR into a Duke3D engine fork |
| [../ARCHITECTURE.md](../ARCHITECTURE.md) | Full OASIS Omniverse architecture |
| [../OGLib/README.md](../OGLib/README.md) | Shared C game integration library |

## Credits

**ODuke3D is based on [EDuke32](https://eduke32.com)** by Jonathon Fowler, Richard Gobeille, and contributors. EDuke32 is licensed under the **GNU General Public License v2.0 (GPL-2.0)**. Duke Nukem 3D game data is property of Gearbox Software / 3D Realms; you must own a copy to use ODuke3D.
