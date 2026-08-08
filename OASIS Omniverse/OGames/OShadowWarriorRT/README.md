# OShadowWarriorRT — Shadow Warrior (Ray-Traced) + OASIS STAR API

**OShadowWarriorRT** is a ray-traced variant of [OShadowWarrior](../OShadowWarrior/README.md), targeting the original Shadow Warrior (1997) with community Vulkan path-tracing rendering and the **OASIS STAR API** integrated. For the standard BUILD-engine version see **[OShadowWarrior](../OShadowWarrior/README.md)**.

Engine: Raze with Vulkan RT rendering path (BUILD engine — GPL-2.0)

---

## Quick start

### Windows

1. **Prerequisites:** Visual Studio 2019+, CMake 3.15+, Vulkan SDK, RTX-capable GPU. Shadow Warrior game data (`SW.GRP`).
2. **Build:**
   ```bat
   BUILD_OSHADOWWARRIORRT.bat
   ```
3. **Run:** Set `SW_DATA` to your game data directory; the RT build auto-selects the Vulkan renderer.
4. **STAR API:** In-game console: `star beamin <username> <password>`

### Linux / macOS

```bash
./BUILD_OSHADOWWARRIORRT.sh
```

---

## OASIS features

Identical to [OShadowWarrior](../OShadowWarrior/README.md) — same keys, inventory, XP, and HUD overlays. The RT build adds path-traced global illumination and reflections.

| Key | Action |
|-----|--------|
| **I** | OASIS Inventory popup |
| **Q** | OASIS Quest popup |
| **Esc** | Close popup |

---

## Cross-game keys

Same pool as OShadowWarrior — keys are shared between the standard and RT builds.

| Shadow Warrior item | Cross-game key |
|--------------------|---------------|
| Gold key | `gold_key` |
| Silver key | `silver_key` |
| Bronze key | `oasis_bronze_key` |
| Red / Blue / Yellow pass | `red_keycard` / `blue_keycard` / `yellow_keycard` |

---

## Architecture

```
oshadowwarriorrt_ogengine_integration.cpp   (Raze/SW RT engine hooks)
         ↓
    OGLib  (shared C library)
         ↓
  OGEngineClient  (ogengine.dll C ABI — C# NativeAOT)
         ↓
  OASIS STAR API  (WEB4 / WEB5)
```

---

## Map editor

Same as OShadowWarrior — **Mapster32** via `EditorIntegrations/Mapster32/`. Maps are portable between the standard and RT builds.

---

OShadowWarriorRT is based on **Raze** (GPL-2.0) with community Vulkan RT rendering contributions.  
Shadow Warrior is copyright 3D Realms / Devolver Digital. You must own a copy to use OShadowWarriorRT.
