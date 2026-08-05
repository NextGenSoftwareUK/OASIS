# Best Source Ports & Map Editors — Reference Guide

*OASIS Omniverse research notes. Covers the classic id Software / Apogee lineage — the games in or adjacent to the OASIS Omniverse stack.*

---

## Wolfenstein 3D

### Best ports

| Port | Recommendation | Notes |
|------|---------------|-------|
| **ECWolf** | ✅ Best overall | Modern DECORATE-based modding, SDL2, cross-platform, actively maintained. Full support for Wolf3D, Spear of Destiny, and Blake Stone. |
| **LZWolf** | Good alternative | Fork of ECWolf with additional features (slopes, extra actor types). Less mainstream. |
| **Wolf4SDL** | Legacy | Simple SDL1 port of the original source, minimal modernisation. |

**OASIS Omniverse choice: ECWolf → [OWolf3D](../OWolf3D/README.md)**

---

## Doom / Doom II

### Best ports

| Port | Recommendation | Notes |
|------|---------------|-------|
| **GZDoom** | ✅ Best for modding | OpenGL/Vulkan, full ZScript/DECORATE/ACS, massive mod ecosystem. The definitive modern Doom engine. |
| **LZDoom** | Low-spec systems | GZDoom fork targeting older/integrated GPUs. Feature-equivalent otherwise. |
| **Crispy Doom** | Best vanilla-faithful | Stays extremely close to original DOS behaviour while adding QoL fixes. |
| **PrBoom+** | Demo-compatible | The port of choice for speedrunners and demo playback. |
| **DSDA-Doom** | Advanced demo/speedrun | Fork of PrBoom+ with strict accuracy, built-in level stats, the current speedrun standard. |
| **Doom Retro** | QoL + faithful | Good middle ground — looks like vanilla, plays modern. |

**Doom RT:** No official Doom 1/2 ray-traced port exists in the same vein as Quake II RTX. The closest is GZDoom with Vulkan path-tracing mods (community shaders), but these are not production-ready standalone ports.

**OASIS Omniverse choice: UZDoom (GZDoom fork) → [ODOOM](../ODOOM/README.md)**

---

## Quake (Quake I)

### Best ports

| Port | Recommendation | Notes |
|------|---------------|-------|
| **vkQuake** | ✅ Best overall | Vulkan renderer, actively maintained, full mod/mission-pack compatibility, smooth framerate, HDR support. |
| **QuakeSpasm** | Best OpenGL classic | Rock-solid, highly compatible, the longtime community standard. vkQuake is its Vulkan successor. |
| **QuakeSpasm-Spiked** | Feature-rich variant | Adds many extensions (csqc, protocol extensions) while keeping QuakeSpasm compatibility. |
| **Ironwail** | Best performance | Heavily optimised QuakeSpasm fork; fastest software-path renderer. |
| **Mark V** | Retro-accurate | Aims for original look-and-feel with modern engine fixes. |

**Quake RT:** No dedicated Quake 1 ray-tracing port equivalent to Quake II RTX. Unofficial RTX shader work exists but no standalone production release.

**OASIS Omniverse choice: vkQuake → [OQuake](../OQuake/README.md)**

---

## Quake II

### Best ports

| Port | Recommendation | Notes |
|------|---------------|-------|
| **Yamagi Quake II** | ✅ Best overall | Clean, stable, cross-platform, faithful to the original with QoL improvements. The go-to general-purpose Q2 port. Active development. |
| **Quake II RTX** | ✅ Best visuals | NVIDIA's official Vulkan path-tracing remaster. Stunning ray-traced lighting, reflections, and shadows on original Q2 levels. Requires RTX GPU. Open-source (GPL). Based on Yamagi Q2. |
| **Q2PRO** | Best for multiplayer | Lightweight, low-latency, used by the competitive Quake 2 community. |
| **KMQuake2** | Classic modding | Older but widely used port with extensive mod support, popular in the modding era. |

**OASIS Omniverse planned: Yamagi Quake 2 → OQuake2 / Q2 RTX → OQuake2-RTX**

### Map editors

| Editor | Recommendation | Notes |
|--------|---------------|-------|
| **TrenchBroom** | ✅ Best modern editor | Cross-platform, real-time 3D editing, excellent UX. Supports Q1, Q2, Hexen II. Actively maintained. Best choice for new Q2 mappers. |
| **J.A.C.K.** (Jackhammer) | Good alternative | Traditional Quake editor feel, solid Q2 support. Good if you prefer GTKRadiant-style workflow. |
| **NetRadiant-custom** | Multi-game option | Active fork of GtkRadiant; handles Q1/Q2/Q3 all from one tool. |
| **GtkRadiant** | Classic/legacy | The original id-blessed editor. Still functional but dated. |

**Compiler:** [q2tools-220](https://github.com/qbism/q2tools-220) — modern Q2 BSP compiler with phong shading, v220 map format, and various fixes. Use alongside TrenchBroom.

### TrenchBroom vs NetRadiant-custom for Q1 / Q2

Both editors support Q1 and Q2, but TrenchBroom is the better choice for those games:

| | TrenchBroom | NetRadiant-custom |
|-|------------|-------------------|
| **Q1 / Q2** | ✅ Purpose-built — modern UX, real-time 3D editing, face manipulation, texture locking | ⚠️ Works, but Q1/Q2 are secondary targets; feels like a Q3 tool adapted backwards |
| **Q3** | ⚠️ Limited curve/patch support | ✅ Native curve/patch editor — essential for Q3 geometry |
| **Origin** | Designed from scratch for Quake-engine brush mapping | Derived from GtkRadiant, which was built by id for Q3 |
| **UX** | Modern, intuitive | Older, traditional |
| **Cross-format copy-paste** | ❌ No | ✅ Yes (geometry only — see Q3 section) |

**Rule of thumb:** TrenchBroom for Q1/Q2, NetRadiant-custom for Q3.

---

## Quake III Arena

### Best ports

| Port | Recommendation | Notes |
|------|---------------|-------|
| **Quake3e** | ✅ Best overall | Vulkan renderer, 10–200%+ FPS over vanilla, HDR + bloom, reworked QVM security, **fully compatible with all existing Q3A mods and demos**. The most popular port among active competitive players. |
| **ioquake3** | ✅ Best ecosystem / baseline | The longstanding community-maintained baseline port. Large ecosystem, widely used by mod authors as their target. OpenGL. |
| **ioquake3 (OpenGL2 branch)** | Best visuals | Full PBR pipeline: cascaded shadow maps, SSAO, normal/parallax/specular maps, HDR, tone mapping. Looks great with custom PBR assets but PBR shaders appear wrong on original Q3 assets. Slower than Quake3e. |
| **Spearmint** | Standalone game engine only | Technically advanced ioquake3 fork with engine improvements, but **deliberately breaks QVM/DLL mod compatibility**. Its README states: *"Spearmint is not compatible with existing mods (the QVM/DLL files) or demos for any game."* Not suitable for playing Q3A mods. Best for building new standalone games. |

**Key distinction:** Quake3e is mod-compatible and fastest; Spearmint is architecturally superior but mod-incompatible.

**OASIS Omniverse planned: Quake3e → OQuake3**

### Map editors

| Editor | Recommendation | Notes |
|--------|---------------|-------|
| **NetRadiant-custom** | ✅ Best desktop editor | Active fork of GtkRadiant; full Q3 curve/patch support (critical for Q3 map geometry), copy-paste between Q1/Q2/Q3 formats. |
| **GtkRadiant** | Classic / legacy | The original id-endorsed Q3 editor. Still works; most old tutorials reference it. |
| **Q3Edit** | Modern browser option | Open-source, browser-based editor using TypeScript + WebGL2. Runs q3map2 compiler and ioquake3 as WASM — edit, compile, and playtest in one tab without any local install. Good for quick experiments. |

### NetRadiant cross-format conversion (Q1 ↔ Q2 ↔ Q3)

NetRadiant-custom lets you **copy and paste brush geometry between Q1, Q2, and Q3 map formats** — useful for porting level layouts — but it is not a clean automatic converter. What actually transfers:

| Element | Transfers? | Notes |
|---------|-----------|-------|
| Brush geometry | ✅ Yes | Solid shapes copy across formats cleanly |
| Textures / shaders | ❌ No | Q1 uses WAD-packed textures; Q2 uses PCX/TGA with folder paths; Q3 uses shader names. All differ — must be remapped manually |
| Entities | ❌ No | Entity definitions differ per game (`monster_ogre` is meaningless in Q2/Q3; Q3 entities have no Q1 equivalent). Must be replaced manually |
| Q3 patches / curves | ❌ No | Bezier surfaces don't exist in Q1/Q2 `.map` format. Lost entirely or rebuilt as chunky brushes |

**Practical use:** Porting a Q1 level to Q2 is feasible — copy the brushwork, re-texture, replace entities. Q1↔Q2 is the easiest pair since the formats are closely related. Anything involving Q3 patches going to Q1/Q2 breaks down. It's a **geometry migration tool**, not a converter — significant manual work is always required regardless of direction.

---

## Duke Nukem 3D

### Best ports

| Port | Recommendation | Notes |
|------|---------------|-------|
| **EDuke32** | ✅ Best overall | The definitive Duke3D port. Polymer renderer, HRP support, scripting extensions (CON, EDuke scripting), massive mod ecosystem. Cross-platform. |
| **Duke-RT** | ✅ Best visuals | Vulkan ray-tracing modification of EDuke32 by fgsfdsfgs. Same mod compatibility as EDuke32 but with RTX lighting. Requires modern GPU. |
| **NBlood** | For Blood fans | EDuke32 engine running Blood. Not strictly Duke3D but same engine lineage. |
| **BuildGDX** | Java-based Build engine | Runs Duke3D, Blood, SW, Exhumed. Cross-platform including systems where EDuke32 is unavailable. |

**OASIS Omniverse choices:**
- EDuke32 → [ODuke3D](../ODuke3D/README.md)
- Duke-RT → [ODuke3D-RT](../ODuke3D-RT/README.md)

---

## Heretic

### Best ports

| Port | Recommendation | Notes |
|------|---------------|-------|
| **UZDoom** | ✅ Best for OASIS | GZDoom fork used by ODOOM. Inherits full GZDoom Heretic support — ZScript/DECORATE, OpenGL/Vulkan, cross-platform. Using the same binary as ODOOM keeps the OASIS ecosystem consistent and integration hooks identical. |
| **GZDoom** | ✅ Best standalone | The definitive modern port for Heretic. Full ZScript/DECORATE, OpenGL/Vulkan, massive mod ecosystem. UZDoom is a fork of this. |
| **Crispy Heretic** | Best vanilla-faithful | Stays close to the original DOS Heretic behaviour with modern QoL fixes (uncapped framerate, widescreen). |
| **Chocolate Heretic** | Most accurate | Bit-perfect original DOS accuracy. Minimal extras; best for demo playback. |

**OASIS Omniverse choice: UZDoom (GZDoom fork) → [OHeretic](../OHeretic/README.md)**

---

## Hexen: Beyond Heretic

### Best ports

| Port | Recommendation | Notes |
|------|---------------|-------|
| **UZDoom** | ✅ Best for OASIS | GZDoom fork used by ODOOM. Inherits full GZDoom Hexen support including all three character classes (Fighter, Cleric, Mage). Same binary and hook patterns as ODOOM and OHeretic. |
| **GZDoom** | ✅ Best standalone | Full Hexen support, ZScript/ACS modding, OpenGL/Vulkan. UZDoom is a fork of this. |
| **Crispy Hexen** | Best vanilla-faithful | Vanilla-faithful Hexen port derived from Chocolate Hexen. |
| **Chocolate Hexen** | Most accurate | Bit-perfect original DOS accuracy. |

**OASIS Omniverse choice: UZDoom (GZDoom fork) → [OHexen](../OHexen/README.md)**

---

## Shadow Warrior Classic

### Best ports

| Port | Recommendation | Notes |
|------|---------------|-------|
| **Raze** | ✅ Best overall | ZDoom team's Build engine reimplementation. Shadow Warrior Classic backend is first-class. Runs inside the same GZDoom C++ infrastructure as OHeretic/OHexen — identical CCMD system, hook patterns, and screen API. Actively maintained. |
| **Duke-RT** | ✅ Best visuals | Raze fork (https://github.com/postmemetic/Duke-RT) that adds Vulkan path-traced ray-tracing. Inherits Raze's full Shadow Warrior backend, so hook sites are identical to the non-RT version. |
| **BuildGDX** | Java-based alternative | Runs SW, Duke3D, Blood, Exhumed. Cross-platform. Good fallback where Raze is unavailable. |
| **VoidSW** | Abandoned | Historical Build-engine SW port. Development effectively halted; superseded by Raze. |

**OASIS Omniverse choices:**
- Raze → [OShadowWarrior](../OShadowWarrior/README.md)
- Duke-RT (Raze fork) → [OShadowWarriorRT](../OShadowWarriorRT/README.md)

---

## Blood

### Best ports

| Port | Recommendation | Notes |
|------|---------------|-------|
| **Raze** | ✅ Best overall | ZDoom team's Build engine reimplementation with a first-class Blood backend. Same GZDoom C++ infrastructure as OShadowWarrior and OExhumed — hook patterns, CCMD system, and screen API are identical. Actively maintained. |
| **BuildGDX** | Java-based alternative | Cross-platform Build engine port covering Blood, Duke3D, SW, and Exhumed. Good fallback. |
| **NBlood** | Legacy option | Standalone Build engine Blood port. Now superseded by Raze's Blood backend. |
| **BloodGDX** | Abandoned | Original Java Blood port by M210; development halted. Superseded by BuildGDX and Raze. |

**OASIS Omniverse choice:**
- Raze → [OBlood](../OBlood/README.md)

---

## Exhumed / PowerSlave

### Best ports

| Port | Recommendation | Notes |
|------|---------------|-------|
| **Raze** | ✅ Best overall | Raze's Exhumed backend is the most complete and actively maintained port. Shares the same hook infrastructure as OBlood and OShadowWarrior. |
| **PCExhumed** | Historical | The original Exhumed PC source; early community port. Superseded by Raze. |
| **BuildGDX** | Java-based alternative | Cross-platform Build port with Exhumed support. Good fallback. |

**OASIS Omniverse choice:**
- Raze → [OExhumed](../OExhumed/README.md)

---

## Strife: Quest for the Sigil

### Best ports

| Port | Recommendation | Notes |
|------|---------------|-------|
| **UZDoom** | ✅ Best overall (OASIS) | UZDoom (GZDoom fork) supports Strife natively with full GZDoom rendering, mod support, and the same hook API as OHeretic and OHexen. Keeps OASIS integration consistent. |
| **GZDoom** | ✅ Excellent | GZDoom's Strife support is first-class and actively maintained. UZDoom is preferred for OASIS consistency. |
| **Chocolate Strife** | ✅ Most authentic | Emulates the original game accurately (vanilla behavior, 320×200 output). No OASIS hook layer — compatibility mode only. |
| **Crispy Strife** | Authentic + extras | Chocolate fork with a few quality-of-life improvements. |

**OASIS Omniverse choice:**
- UZDoom → [OStrife](../OStrife/README.md)

---

## Doom 64

### Best ports

| Port | Recommendation | Notes |
|------|---------------|-------|
| **Doom64 EX+** | ✅ Best overall | Community continuation of the original Doom64 EX by Kaiser. GPL, actively maintained, SDL2, faithful to the N64 original. The community standard for open-source Doom 64 play. |
| **Doom64 CE** | Community Edition | Community Edition fork with additional features. Less widely used than EX+. |
| **GZDoom** | Partial support | Can run Doom 64 WADs via conversion tools but lacks native N64-authentic rendering and the Unmaker behavior. Not suitable for ODoom64. |

**OASIS Omniverse choice:**
- Doom64 EX+ → [ODoom64](../ODoom64/README.md)

---

## Hexen II

### Best ports

| Port | Recommendation | Notes |
|------|---------------|-------|
| **uhexen2** | ✅ Best overall | Hammer of Thyrion — the long-running community-maintained Hexen II port (since 2005). SDL2, cross-platform, actively updated. The only serious option for open-source Hexen II. |
| **HexenWorld** | Multiplayer | Built into uhexen2 for HexenWorld multiplayer mode. |

**OASIS Omniverse choice:**
- uhexen2 → [OHexenII](../OHexenII/README.md)

---

## Return to Castle Wolfenstein

### Best ports

| Port | Recommendation | Notes |
|------|---------------|-------|
| **iortcw** | ✅ Best overall | Community-maintained GPL Q3-engine port of RtCW. SDL2, cross-platform, actively maintained — the definitive open-source RtCW port. Same lineage as ioquake3. |
| **Spearmint** | Generalist Q3 engine | Supports RtCW but is more of a general Q3 engine reimplementation. Less RtCW-focused than iortcw. |
| **OpenWolf** | Abandoned | Early GPL RtCW fork. Development effectively halted. |

**OASIS Omniverse choice:**
- iortcw → [ORtCW](../ORtCW/README.md)

---

## Doom 3

### Best ports

| Port | Recommendation | Notes |
|------|---------------|-------|
| **dhewm3** | ✅ Best for classic Doom 3 | Clean GPL source port of Doom 3 (not BFG). Stable, cross-platform, mod-compatible, SDL2. The community standard for classic D3 modding. |
| **RBDOOM-3-BFG** | ✅ Best for BFG / visuals | Vulkan renderer, PBR materials, ray-traced global illumination (experimental), HDR, SSAO. Based on the BFG Edition source. Actively developed. |
| **dhewm3 + Sikkmod** | Visual upgrade | dhewm3 with the classic Sikkmod post-processing for a simpler visual upgrade without BFG dependencies. |

**OASIS Omniverse choices:**
- dhewm3 → [ODOOM3](../ODOOM3/README.md)
- RBDOOM-3-BFG → [ODOOM3-BFG](../ODOOM3-BFG/README.md)

---

## Summary: OASIS Omniverse Port Selections

| Game | Source Port | OASIS Port | Status |
|------|------------|------------|--------|
| Wolfenstein 3D | ECWolf | OWolf3D | ✅ Complete |
| Doom / Doom II | UZDoom (GZDoom fork) | ODOOM | ✅ Complete |
| Heretic | UZDoom (GZDoom fork) | OHeretic | 🔧 Integration files complete |
| Hexen: Beyond Heretic | UZDoom (GZDoom fork) | OHexen | 🔧 Integration files complete |
| Shadow Warrior Classic | Raze | OShadowWarrior | 🔧 Integration files complete |
| Shadow Warrior Classic (RT) | Duke-RT (Raze fork) | OShadowWarriorRT | 🔧 Integration files complete |
| Blood | Raze | OBlood | 🔧 Integration files complete |
| Exhumed / PowerSlave | Raze | OExhumed | 🔧 Integration files complete |
| Strife: Quest for the Sigil | UZDoom (GZDoom fork) | OStrife | 🔧 Integration files complete |
| Doom 64 | Doom64 EX+ | ODoom64 | 🔧 Integration files complete |
| Hexen II | uhexen2 (Hammer of Thyrion) | OHexenII | 🔧 Integration files complete |
| Return to Castle Wolfenstein | iortcw | ORtCW | 🔧 Integration files complete |
| Quake | vkQuake | OQuake | ✅ Complete |
| Quake II | Yamagi Quake 2 | OQuake2 | 🔜 Planned |
| Quake II (RT) | Q2 RTX | OQuake2-RTX | 🔜 Planned |
| Quake III Arena | Quake3e | OQuake3 | 🔜 Planned |
| Doom 3 (classic) | dhewm3 | ODOOM3 | ✅ Complete |
| Doom 3 BFG | RBDOOM-3-BFG | ODOOM3-BFG | ✅ Complete |
| Duke Nukem 3D | EDuke32 | ODuke3D | ✅ Complete |
| Duke Nukem 3D (RT) | Duke-RT | ODuke3D-RT | ✅ Complete |

---

## Quick-reference: Map Editors by Game

| Game(s) | Best Editor | Runner-up |
|---------|------------|-----------|
| Quake I | TrenchBroom | Ironwail (in-engine) |
| Quake II | TrenchBroom + q2tools-220 | J.A.C.K. |
| Quake III | NetRadiant-custom | GtkRadiant / Q3Edit (browser) |
| Doom / Doom II | [Ultimate Doom Builder](https://forum.zdoom.org/viewtopic.php?t=69920) | SLADE |
| Duke Nukem 3D | Mapster32 (bundled with EDuke32) | — |
| Wolfenstein 3D | ECWolf + DECORATE (no dedicated visual editor; Tiled works for grid maps) | — |

---

*Last updated: 2026-07-31*
