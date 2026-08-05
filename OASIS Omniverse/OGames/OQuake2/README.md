# OQuake2 – Yamagi Quake II + OASIS STAR API

OQuake2 is Yamagi Quake II integrated with the **OASIS STAR API** so keys collected in **OQuake**, **ODOOM**, or other OASIS Omniverse games can open doors in OQuake2 and vice versa — cross-game keys, quests, inventory, XP, SSO, and more.

**OQuake2 is based on Yamagi Quake II.** Full credit to the [Yamagi Quake II](https://www.yamagi.org/quake2/) team (GPL-2.0). See **[Docs/CREDITS_AND_LICENSE.md](Docs/CREDITS_AND_LICENSE.md)** for credits and license obligations.

**OASIS thing type range: 6000–6899. Portal thing type: 5900 (shared).**

## Quick start

### Windows

1. **Build and copy integration:** From the OASIS repo root run:
   ```bat
   "OASIS Omniverse\OQuake2\BUILD_OQUAKE2.bat"
   ```
   This builds the STAR API if needed, copies OQuake2 files into the Yamagi Q2 source tree, and builds the engine. Edit the script to set `YQUAKE2_SRC` to your Yamagi Q2 clone.

2. **Run the game:** Launch `build\OQUAKE2.exe` from the OQuake2 folder, or use `BUILD_OQUAKE2.bat run`.

3. **Game data:** Yamagi Q2 needs Quake II game data (`baseq2` with `pak0.pak`). Use the `-datadir` flag or place data next to the exe.

### Linux / macOS

1. **Prerequisites:** Install cmake, make, and clone Yamagi Q2:
   ```bash
   sudo apt install -y cmake build-essential   # Linux
   export YQUAKE2_SRC=~/Source/yquake2         # or your Yamagi Q2 clone path
   export YQUAKE2_BASEDIR="$HOME/.steam/steam/steamapps/common/Quake 2"
   ```

2. **Build:**
   ```bash
   cd "OASIS Omniverse/OQuake2"
   ./BUILD_OQUAKE2.sh
   ```

3. **Run:** `./BUILD_OQUAKE2.sh run` (or run `build/OQUAKE2` directly).

4. **Cross-game keys (optional):** set `STAR_USERNAME` / `STAR_PASSWORD` or `OGENGINE_KEY` / `STAR_AVATAR_ID`.

## Architecture

OQuake2 sits at the same level as OQuake in the OASIS Omniverse integration stack:

```
oquake2_ogengine_integration.c  (OQuake2 engine hooks)
         ↓
  OGEngineClient  (C# NativeAOT → ogengine.dll / libstar_api.so)
         ↓
  WEB4 / WEB5 OASIS APIs
```

See **[OASIS Omniverse/ARCHITECTURE.md](../ARCHITECTURE.md)** for the full design.

## OASIS Thing Types

| Range | Category |
|-------|----------|
| 5900  | Portal (shared cross-game) |
| 6001–6002 | Keys (silver, gold) |
| 6100–6109 | Weapons |
| 6200–6202 | Armor |
| 6300–6302 | Health |
| 6400–6405 | Ammo |
| 6500–6509 | Monsters |

Full asset list: see `oasisstar.json`.

## Documentation

| Document | Description |
|----------|-------------|
| [Docs/INTEGRATION_INSTRUCTIONS.md](Docs/INTEGRATION_INSTRUCTIONS.md) | Step-by-step engine integration guide |
| [Docs/CREDITS_AND_LICENSE.md](Docs/CREDITS_AND_LICENSE.md) | Credits to Yamagi Q2 and GPL-2.0 obligations |
| [../ARCHITECTURE.md](../ARCHITECTURE.md) | Full OASIS Omniverse architecture |
| [../OGLib/README.md](../OGLib/README.md) | Shared C game integration library |

## Files in this folder

| File | Purpose |
|------|---------|
| **BUILD_OQUAKE2.bat** | Build STAR API, copy OQuake2 integration into Yamagi Q2 source, build engine (Windows) |
| **BUILD_OQUAKE2.sh** | Same as above for Linux/macOS |
| **oquake2_ogengine_integration.h** | Public integration API header |
| **oquake2_ogengine_integration.c** | Integration implementation (in `Code/` after build) |
| **ogengine.h** | Shared STAR API C ABI header (copied from OGEngineClient by build script) |
| **ogengine_sync.h** | Async sync layer header (copied from OGEngineClient by build script) |
| **oasisstar.json** | OASIS config: API URL, session, mint flags, asset thing type table |
| **Docs/** | Integration instructions and credits |
| **build/** | Build output (OQUAKE2.exe / OQUAKE2, ogengine.dll / libstar_api.so) |
