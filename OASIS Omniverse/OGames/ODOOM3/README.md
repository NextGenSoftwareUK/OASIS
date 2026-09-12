# ODOOM3 — Doom 3 + OASIS STAR API

**ODOOM3** is a fork of [dhewm3](https://github.com/dhewm/dhewm3) (the community Doom 3 source port) with the **OASIS STAR API** integrated. Classic Doom 3 with cross-game inventory, XP, quests, and portals shared across all 20 OASIS Omniverse OGames.

For the BFG Edition see **[ODOOM3-BFG](../ODOOM3-BFG/README.md)**.

Engine: dhewm3 (id Tech 4 — GPL-2.0)

---

## Quick start

### Windows

1. **Prerequisites:** Visual Studio 2019+, CMake 3.15+. Doom 3 game data (`base/` directory from retail).
2. **Build:**
   ```bat
   BUILD_ODOOM3.bat
   ```
3. **Run:** `RUN_ODOOM3.bat`, or run `dhewm3.exe +set fs_basepath C:\Doom3\`.
4. **STAR API:** Open console `` ` ``: `star_beamin <username> <password>`

### Linux / macOS

```bash
./BUILD_ODOOM3.sh
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

---

## Cross-game keys

| ODOOM3 item | Cross-game key | Other games |
|-------------|---------------|-------------|
| blue_key | `blue_keycard` | ODOOM Blue Key, ODuke3D Blue Card, OWolf3D Gold Key |
| red_key | `red_keycard` | ODOOM Red Key, ODuke3D Red Card |
| yellow_key | `yellow_keycard` | ODOOM Yellow Key, ODuke3D Yellow Card, OWolf3D Silver Key |

---

## Architecture

```
d3doom3_ogengine_integration.cpp   (neo/game/ — compiled into base.dll)
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

**DarkRadiant** / **ODOOM3-Editor** — see `EditorIntegrations/DarkRadiant/` for OASIS entity definitions (`oasis_portal_enter`, `oasis_portal_exit`, `oasis_trigger`).

---

## Documentation

| Document | Description |
|----------|-------------|
| [Docs/](Docs/) | Integration instructions and Windows setup |
| [../ARCHITECTURE.md](../ARCHITECTURE.md) | Full OASIS Omniverse architecture |
| [../OGLib/README.md](../OGLib/README.md) | Shared C game integration library |

---

ODOOM3 is based on **dhewm3** (GPL-2.0) by dhewm3 contributors.  
Doom 3 is copyright id Software. You must own a copy to use ODOOM3.
