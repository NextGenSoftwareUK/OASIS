# OShadowWarrior — Shadow Warrior + OASIS STAR API

**OShadowWarrior** is a fork of [Raze](https://github.com/ZDoom/Raze) targeting the original **Shadow Warrior** (1997) with the **OASIS STAR API** integrated, bringing this BUILD-engine classic into the OASIS Omniverse. For the ray-traced modern variant see **[OShadowWarriorRT](../OShadowWarriorRT/README.md)**.

Engine: Raze (BUILD engine reimplementation — GPL-2.0, supports Blood, Duke Nukem 3D, Exhumed, Shadow Warrior)

---

## Quick start

### Windows

1. **Prerequisites:** Visual Studio 2019+, CMake 3.15+. Shadow Warrior game data (`SW.GRP`).
2. **Build:**
   ```bat
   BUILD_OSHADOWWARRIOR.bat
   ```
3. **Run:** Set `SW_DATA` to your game data directory and launch `raze.exe -game sw`.
4. **STAR API:** In-game console: `star beamin <username> <password>`

### Linux / macOS

```bash
./BUILD_OSHADOWWARRIOR.sh
```

---

## OASIS features

| Key | Action |
|-----|--------|
| **I** | OASIS Inventory popup |
| **Q** | OASIS Quest popup |
| **↑ / ↓** | Navigate popup list |
| **Esc** | Close popup |

HUD overlays: username label (top-left), XP counter (top-right), toast notifications (centre).

---

## Cross-game keys

| Shadow Warrior item | Cross-game key | Other games |
|--------------------|---------------|-------------|
| Gold key | `gold_key` | OWolf3D Gold Key, OQuake Gold Key |
| Silver key | `silver_key` | OWolf3D Silver Key, OQuake Silver Key |
| Bronze key | `oasis_bronze_key` | OASIS-exclusive cross-game quest item |
| Red pass | `red_keycard` | ODOOM Red Key, ODuke3D Red Card |
| Blue pass | `blue_keycard` | ODOOM Blue Key, ODuke3D Blue Card |
| Yellow pass | `yellow_keycard` | ODOOM Yellow Key |

---

## Architecture

```
oshadowwarrior_ogengine_integration.cpp   (Raze/Shadow Warrior hooks)
         ↓
    OGLib  (shared C library)
         ↓
  OGEngineClient  (ogengine.dll C ABI — C# NativeAOT)
         ↓
  OASIS STAR API  (WEB4 / WEB5)
```

---

## Map editor

Shadow Warrior maps use the **BUILD** engine format. Use **Mapster32** (via `EditorIntegrations/Mapster32/`) to place OASIS portal and trigger entities.

---

## Documentation

| Document | Description |
|----------|-------------|
| [../ARCHITECTURE.md](../ARCHITECTURE.md) | Full OASIS Omniverse architecture |
| [../OGLib/README.md](../OGLib/README.md) | Shared C game integration library |

---

OShadowWarrior is based on **Raze** (GPL-2.0) by Randy Heit and contributors.  
Shadow Warrior is copyright 3D Realms / Devolver Digital. You must own a copy to use OShadowWarrior.
