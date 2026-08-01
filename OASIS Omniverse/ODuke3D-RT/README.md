# ODuke3D-RT – Duke Nukem 3D + Vulkan Ray Tracing + OASIS STAR API

**ODuke3D-RT** is a fork of [Duke-RT](https://github.com/fgsfdsfgs/duke-rt) — a Vulkan ray-tracing modification of EDuke32 — with the **OASIS STAR API** integrated. It offers the same cross-game OASIS features as **ODuke3D** but with modern ray-traced lighting and reflections for Duke Nukem 3D.

Cross-game keys work across all six OASIS Omniverse games: **ODOOM**, **OQuake**, **ODOOM3**, **ODOOM3-BFG**, **ODuke3D**, and ODuke3D-RT.

**For classic (non-RT) Duke Nukem 3D see [ODuke3D](../ODuke3D/README.md).**

ODuke3D-RT is a fork of Duke-RT (Vulkan ray tracing EDuke32 mod, GPL-2.0) which is itself based on EDuke32 (Jonathon Fowler, Richard Gobeille, contributors, GPL-2.0). Duke Nukem 3D game data is property of Gearbox Software / 3D Realms. By NextGen World Ltd.

## Quick start

### Windows

1. **Prerequisites:** Visual Studio 2019+ with C++ workload, CMake 3.15+, **Vulkan SDK**, ODuke3D-RT/Duke-RT clone at `C:\Source\ODuke3D-RT`. See [Docs/WINDOWS_INTEGRATION.md](Docs/WINDOWS_INTEGRATION.md).

2. **Build:**
   ```batch
   BUILD_ODUKE3DRT.bat
   ```
   Output: `build-vs2019-win64\Release\eduke32.exe`.

3. **Run:**
   ```batch
   RUN_ODUKE3DRT.bat [C:\Duke3D]
   ```

4. **STAR API:** Set `STAR_USERNAME`/`STAR_PASSWORD` or `STAR_API_KEY`/`STAR_AVATAR_ID`.

### Linux / macOS

1. **Prerequisites:** gcc, cmake, ninja or make, Vulkan SDK, Duke3D GRP.

2. **Build:**
   ```bash
   ./BUILD_ODUKE3DRT.sh
   ```

3. **Run:**
   ```bash
   ./RUN_ODUKE3DRT.sh ~/Duke3D
   ```

## Architecture

```
oduke3drt_star_integration.c   (ODuke3D-RT hooks + HUD/GUI — same API as ODuke3D)
         ↓
    OGLib  (shared C library — config, beamin, session shims)
         ↓
  STARAPIClient  (C# NativeAOT → star_api.dll)
         ↓
  WEB4 / WEB5 OASIS APIs
```

Duke-RT renders the 3-D scene via Vulkan ray tracing, but the HUD overlay layer is the standard EDuke32 2-D draw path. OASIS overlays use `printext256()` in that layer and are unaffected by the RT rendering path.

## OASIS GUI features

Identical to ODuke3D and all other OASIS Omniverse ports:

| Feature | Key | Description |
|---------|-----|-------------|
| Inventory popup | **I** | Browse and use OASIS inventory items |
| Quest popup | **Q** | View active OASIS quests |
| Send to Avatar | **A** (in inventory) | Send selected item to your avatar |
| Send to Clan | **C** (in inventory) | Send selected item to your clan |
| OASIS avatar face | HUD | Status bar face replaced by OASIS avatar when beamed in |
| XP display | HUD top-right | Running OASIS XP total |
| Beamed-in label | HUD top-left | `OASIS: username` when connected |
| Version display | HUD bottom-right | ODuke3D-RT version string |
| Toast notifications | HUD centre | Key pickups, XP awards, cross-game door events |

## Documentation

| Document | Description |
|----------|-------------|
| [Docs/WINDOWS_INTEGRATION.md](Docs/WINDOWS_INTEGRATION.md) | Full Windows setup and Vulkan requirements |
| [Docs/INTEGRATION_INSTRUCTIONS.md](Docs/INTEGRATION_INSTRUCTIONS.md) | How to integrate OASIS STAR into a Duke-RT fork |
| [../ODuke3D/README.md](../ODuke3D/README.md) | Classic (non-RT) Duke3D integration |
| [../ARCHITECTURE.md](../ARCHITECTURE.md) | Full OASIS Omniverse architecture |

## Credits

**ODuke3D-RT is based on [Duke-RT](https://github.com/fgsfdsfgs/duke-rt)** (Vulkan ray-tracing EDuke32 modification, GPL-2.0). EDuke32 by Jonathon Fowler, Richard Gobeille, and contributors (GPL-2.0). Duke Nukem 3D © Gearbox Software / 3D Realms.
