# ODoom64 — Doom 64 + OASIS STAR API

**ODoom64** is a fork of [Doom 64 EX+](https://github.com/atsb/Doom64EX-Plus) with the **OASIS STAR API** integrated, bringing the N64-exclusive Doom 64 into the OASIS Omniverse. Keys, inventory, XP, and quests are shared across all 20 OASIS Omniverse OGames.

Engine: Doom 64 EX+ (custom Doom 64 engine — GPL-2.0)

---

## Quick start

### Windows

1. **Prerequisites:** Visual Studio 2019+, CMake 3.15+, SDL2. Doom 64 game data (`DOOM64.WAD` — included with the Steam/GOG remaster or extracted from ROM).
2. **Build:**
   ```bat
   BUILD_ODOOM64.bat
   ```
3. **Run:** Place `DOOM64.WAD` alongside the executable.
4. **STAR API:** Console command: `star_beamin <username> <password>`

### Linux / macOS

```bash
./BUILD_ODOOM64.sh
```

---

## OASIS features

| Key | Action |
|-----|--------|
| **I** | OASIS Inventory popup |
| **Q** | OASIS Quest popup |
| **↑ / ↓** | Navigate popup list |
| **Esc** | Close popup |

HUD overlays: username (top-left), XP counter (top-right), toast notifications (centre).

---

## Cross-game keys

| Doom 64 item | Cross-game key | Other games |
|--------------|---------------|-------------|
| Blue skull key | `blue_keycard` | ODOOM Blue Key, ODuke3D Blue Card, ODOOM3 blue_key |
| Red skull key | `red_keycard` | ODOOM Red Key, ODuke3D Red Card |
| Yellow skull key | `yellow_keycard` | ODOOM Yellow Key, OWolf3D Silver Key |

---

## Architecture

```
odoom64_ogengine_integration.cpp   (engine-level hooks)
         ↓
    OGLib  (shared C library)
         ↓
  OGEngineClient  (ogengine.dll C ABI — C# NativeAOT)
         ↓
  OASIS STAR API  (WEB4 / WEB5)
```

---

## Map editor

**UltimateDoomBuilder** — see `EditorIntegrations/UltimateDoomBuilder/` for OASIS entity definitions. Doom 64 maps use the Doom 64 map format; UDB supports it natively.

---

## Documentation

| Document | Description |
|----------|-------------|
| [../ARCHITECTURE.md](../ARCHITECTURE.md) | Full OASIS Omniverse architecture |
| [../OGLib/README.md](../OGLib/README.md) | Shared C game integration library |

---

ODoom64 is based on **Doom 64 EX+** (GPL-2.0) by atsb and contributors.  
Doom 64 is copyright id Software / Midway Games. You must own a copy to use ODoom64.
