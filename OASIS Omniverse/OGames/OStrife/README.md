# OStrife — Strife + OASIS STAR API

**OStrife** is a fork of **UZDoom** (a GZDoom variant) targeting Strife with the **OASIS STAR API** integrated, bringing this unique RPG-infused Doom-engine game into the OASIS Omniverse. Keys, inventory, XP, and quests are shared across all 20 OASIS Omniverse OGames.

Engine: UZDoom (GZDoom fork — GPL-3.0, supports Doom, Heretic, Hexen, Strife)

---

## Quick start

### Windows

1. **Prerequisites:** Visual Studio 2019+, CMake 3.15+, zlib, SDL2. Strife game data (`STRIFE1.WAD` — from the Veteran Edition on Steam/GOG, or retail).
2. **Build:**
   ```bat
   BUILD_OSTRIFE.bat
   ```
3. **Run:** Place `STRIFE1.WAD` alongside the executable, or pass `-iwad STRIFE1.WAD`.
4. **STAR API:** Open console (`~`): `star_beamin <username> <password>`

### Linux / macOS

```bash
./BUILD_OSTRIFE.sh
```

---

## OASIS features

Strife already has its own quest and dialogue system — OASIS quests sit alongside the native Strife quest log and share the same XP and karma pool.

| Key / console | Action |
|---------------|--------|
| **I** | OASIS Inventory popup |
| **Q** | OASIS Quest popup (OASIS quests only) |
| **↑ / ↓** | Navigate popup list |
| **Esc** | Close popup |
| `` star_beamin `` | Log in to OASIS |

HUD overlays: username label (top-left), XP counter (top-right), toast notifications (centre).

---

## Cross-game keys

| Strife item | Cross-game key | Other games |
|-------------|---------------|-------------|
| Gold key | `gold_key` | OWolf3D Gold Key, OQuake Gold Key |
| Brass key | `yellow_keycard` | ODOOM Yellow Key |
| Blue ID card | `blue_keycard` | ODOOM Blue Key, ODuke3D Blue Card |
| Red ID card | `red_keycard` | ODOOM Red Key, ODuke3D Red Card |
| Power crystal | `oasis_power_crystal` | OASIS-exclusive cross-game quest item |
| Sigil (pieces 1–5) | `oasis_sigil_*` | Unique cross-game OASIS legendary quest chain |

---

## Architecture

```
ostrife_ogengine_integration.cpp   (UZDoom engine hooks)
         ↓
    OGLib  (shared C library)
         ↓
  OGEngineClient  (ogengine.dll C ABI — C# NativeAOT)
         ↓
  OASIS STAR API  (WEB4 / WEB5)
```

Hook sites: `D_DoomMain`, game ticker, inventory pickup, key check, actor death, quest state.

---

## Map editor

**UltimateDoomBuilder** — see `EditorIntegrations/UltimateDoomBuilder/` for OASIS entity definitions. Strife uses the Strife/Doom map format; UDB supports it natively.

---

## Documentation

| Document | Description |
|----------|-------------|
| [../ARCHITECTURE.md](../ARCHITECTURE.md) | Full OASIS Omniverse architecture |
| [../OGLib/README.md](../OGLib/README.md) | Shared C game integration library |

---

OStrife is based on **GZDoom / UZDoom** (GPL-3.0) by Randy Heit, Graf Zahl, and contributors.  
Strife is copyright Rogue Entertainment / Velocity Inc. You must own a copy to use OStrife.
