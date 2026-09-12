# ODOOM3-BFG — Doom 3 BFG Edition + OASIS STAR API

**ODOOM3-BFG** is a fork of [RBDOOM-3-BFG](https://github.com/RobertBeckebans/RBDOOM-3-BFG) with the **OASIS STAR API** integrated. Doom 3 BFG Edition (id Tech 4.5, Vulkan renderer) with cross-game inventory, XP, quests, and portals shared across all 20 OASIS Omniverse OGames.

For the classic Doom 3 see **[ODOOM3](../ODOOM3/README.md)**.

Engine: RBDOOM-3-BFG (id Tech 4.5 / id Tech 6 Vulkan — GPL-3.0)

---

## Quick start

### Windows

1. **Prerequisites:** Visual Studio 2019+, CMake 3.15+, Vulkan SDK. Doom 3 BFG Edition game data (`base/`).
2. **Build:**
   ```bat
   BUILD_ODOOM3BFG.bat
   ```
3. **Run:** `RUN_ODOOM3BFG.bat`, or run `RBDOOM3BFG.exe +set fs_basepath C:\Doom3BFG\`.
4. **STAR API:** Open console `` ` ``: `star_beamin <username> <password>`

### Linux / macOS

```bash
./BUILD_ODOOM3BFG.sh
```

---

## OASIS features

| Key / console | Action |
|---------------|--------|
| **I** | OASIS Inventory popup |
| **Q** | OASIS Quest popup |
| **↑ / ↓** | Navigate popup list |
| **Esc** | Close popup |
| `` star_beamin `` | Log in to OASIS |
| `` star_logout `` | Log out |

HUD overlays: username (top-left), XP counter (top-right), toast notifications (centre).  
BFG Edition adds Vulkan path-traced lighting to the OASIS HUD overlay (same positions, higher fidelity renderer).

---

## Cross-game keys

| ODOOM3-BFG item | Cross-game key | Other games |
|-----------------|---------------|-------------|
| blue_key | `blue_keycard` | ODOOM Blue Key, ODuke3D Blue Card, ODOOM3 blue_key |
| red_key | `red_keycard` | ODOOM Red Key, ODuke3D Red Card, ODOOM3 red_key |
| yellow_key | `yellow_keycard` | ODOOM Yellow Key, ODuke3D Yellow Card, ODOOM3 yellow_key |

Keys are identical between ODOOM3 and ODOOM3-BFG — they share the same cross-game pool.

---

## Architecture

```
d3doom_ogengine_integration.cpp   (neo/game/ — compiled into base.dll)
         ↓
  OGEngineClient  (ogengine.dll C ABI — C# NativeAOT)
         ↓
  OASIS STAR API  (WEB4 / WEB5)
```

Hook sites in `neo/game/`:
- `idGameLocal::Init` / `Shutdown` / `RunFrame`
- `idPlayer::GiveInventoryItem`
- `idAI::Killed`
- `idGameLocal::RequirementMet` (door/lock access)

---

## Map editor

**DarkRadiant** / **ODOOM3-Editor** (supports both classic Doom 3 and BFG Edition map format) — see `EditorIntegrations/DarkRadiant/` for OASIS entity definitions.

---

## Documentation

| Document | Description |
|----------|-------------|
| [Docs/](Docs/) | Integration instructions |
| [../ARCHITECTURE.md](../ARCHITECTURE.md) | Full OASIS Omniverse architecture |
| [../OGLib/README.md](../OGLib/README.md) | Shared C game integration library |

---

ODOOM3-BFG is based on **RBDOOM-3-BFG** (GPL-3.0) by Robert Beckebans and contributors.  
Doom 3 BFG Edition is copyright id Software. You must own a copy to use ODOOM3-BFG.
