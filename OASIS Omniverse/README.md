# OASIS Omniverse

OASIS Omniverse brings **ODOOM** (Doom + OASIS STAR API), **OQuake** (Quake + OASIS STAR API), and the shared STAR API client and tooling into one place. It enables cross-game inventory, quests, and avatar/SSO auth across all games.

**All Markdown guides for this area live under [`Docs/`](Docs/).** This README is the **entry point and full index**; use the tables below to jump to what you need.

**ODOOM and OQuake are built to be 100% compatible with Windows, macOS, and Linux.** Use the platform-specific Getting Started guide below for your OS.

---

## New here? Start with a Getting Started guide

Choose your platform for a clear, step-by-step setup:

| Platform | Guide |
|----------|--------|
| **Windows** | **[Docs/GettingStarted_Windows.md](Docs/GettingStarted_Windows.md)** |
| **Linux** | **[Docs/GettingStarted_Linux.md](Docs/GettingStarted_Linux.md)** |
| **macOS** | **[Docs/GettingStarted_Mac.md](Docs/GettingStarted_Mac.md)** |

**→ [Developer Onboarding (ODOOM, OQuake & OASIS)](Docs/DEVELOPER_ONBOARDING.md)** — Deeper canonical setup (repos, tools, build, run, `oasisstar.json`). Use with the platform guide above.

**→ [Docs/README.md](Docs/README.md)** — Short hub for the same platform guides plus pointers back here.

---

## Documentation index (Omniverse `Docs/`)

### Setup and daily workflow

| Document | What it’s for |
|----------|----------------|
| [Docs/DEVELOPER_ONBOARDING.md](Docs/DEVELOPER_ONBOARDING.md) | Repos to clone, tools, build/run scripts, local vs live APIs, `oasisstar.json`, quick reference table |
| [Docs/QUICKSTART.md](Docs/QUICKSTART.md) | Minimal path to build and run + checklist |
| [Docs/LINUX_BUILD.md](Docs/LINUX_BUILD.md) | Linux/macOS script equivalents, env vars, pointers to Getting Started |
| [Docs/GettingStarted_Windows.md](Docs/GettingStarted_Windows.md) | Windows: prerequisites, clone layout, build ODOOM/OQuake/STARAPIClient |
| [Docs/GettingStarted_Linux.md](Docs/GettingStarted_Linux.md) | Linux: same |
| [Docs/GettingStarted_Mac.md](Docs/GettingStarted_Mac.md) | macOS: same |

### Architecture, integration, and audits

| Document | What it’s for |
|----------|----------------|
| [Docs/ARCHITECTURE.md](Docs/ARCHITECTURE.md) | Client-centric design, layers, porting checklist, `star_sync` notes |
| [Docs/INTEGRATION_GUIDE.md](Docs/INTEGRATION_GUIDE.md) | Cross-game items, quests, API usage, phases, troubleshooting |
| [Docs/CROSS_GAME_POWERUP_WEAPON_MAP.md](Docs/CROSS_GAME_POWERUP_WEAPON_MAP.md) | Doom ↔ Quake powerup/weapon canonical IDs and mapping phases |
| [Docs/STAR_INTEGRATION_AUDIT.md](Docs/STAR_INTEGRATION_AUDIT.md) | Integration audit (e.g. in-client `star_sync` vs C implementation) |

### Quests, players, and ODOOM UI behaviour

| Document | What it’s for |
|----------|----------------|
| [Docs/PHASE2_QUEST_SYSTEM.md](Docs/PHASE2_QUEST_SYSTEM.md) | Quest system overview and design |
| [Docs/STAR_Quest_System_Developer_Guide.md](Docs/STAR_Quest_System_Developer_Guide.md) | WEB5 quest API, STARAPIClient, `star_api_*`, game hooks (developers) |
| [Docs/STAR_Games_User_Guide.md](Docs/STAR_Games_User_Guide.md) | Beam-in, inventory, quest keys for OQuake / ODOOM (players & testers) |
| [Docs/ODOOM_Quest_List_STAR.md](Docs/ODOOM_Quest_List_STAR.md) | ODOOM quest list CVars, ZScript, scroll/filter invariants |

### Build sync, native library, and transport

| Document | What it’s for |
|----------|----------------|
| [Docs/ODOOM_UZDoom_Build_Sync.md](Docs/ODOOM_UZDoom_Build_Sync.md) | ODOOM repo vs `UZDOOM_SRC`, copy step, `star_api` / `libstar_api` deploy |
| [Docs/STAR_API_Native_Transport_Architecture.md](Docs/STAR_API_Native_Transport_Architecture.md) | `star_transport` native vs remote, size/AOT considerations |

### Broader OASIS documentation

| Document | What it’s for |
|----------|----------------|
| [Docs/DEVELOPER_DOCUMENTATION_INDEX.md](Docs/DEVELOPER_DOCUMENTATION_INDEX.md) | Large index linking Omniverse topics plus repo-wide `Docs/Devs/` material |

### Per-folder READMEs (code next to docs)

| Location | What it’s for |
|----------|----------------|
| [STARAPIClient/README.md](STARAPIClient/README.md) | STAR API client build, exports, tests, quest hooks |
| [ODOOM/README.md](ODOOM/README.md) | ODOOM build, run, features |
| [OQuake/README.md](OQuake/README.md) | OQuake build, run, game data |
| [ODOOM/WINDOWS_INTEGRATION.md](ODOOM/WINDOWS_INTEGRATION.md) | ODOOM Windows details |
| [OQuake/Docs/WINDOWS_INTEGRATION.md](OQuake/Docs/WINDOWS_INTEGRATION.md) | OQuake Windows setup, game data, troubleshooting |
| [NativeWrapper/README.md](NativeWrapper/README.md) | Deprecated wrapper (reference only) |
| [OASIS Omniverse/README.md](OASIS%20Omniverse/README.md) | Optional Unity hub project (nested folder) |

**Repo root (AI / policy):** [AGENTS.md](../AGENTS.md) — handoff table including Omniverse doc paths.

---

## Repositories to clone

To build ODOOM and OQuake you need the OASIS repo plus the game engines and Quake data. Clone into a common parent (e.g. `C:\Source\`):

| Repository | Purpose |
|------------|---------|
| **OASIS** (this repo) | Backend, STARAPIClient, ODOOM/OQuake integration |
| **Engine for ODOOM** (`UZDOOM_SRC`) | UZDoom-based tree the ODOOM build compiles |
| **Engine for OQuake** (`VKQUAKE_SRC`) | vkQuake-based tree the OQuake build compiles |
| **quake-rerelease-qc** | QuakeC source used by OQuake |

**Recommended:** although upstream is **UZDoom** and **vkQuake**, use the OASIS-maintained forks **[NextGenSoftwareUK/ODOOM](https://github.com/NextGenSoftwareUK/ODOOM)** and **[NextGenSoftwareUK/OQUAKE](https://github.com/NextGenSoftwareUK/OQUAKE)** for `UZDOOM_SRC` and `VKQUAKE_SRC` so the engine already tracks OASIS integration; vanilla upstream requires you to rely entirely on copy/patch steps from this repo.

Example clone commands (see [Docs/DEVELOPER_ONBOARDING.md](Docs/DEVELOPER_ONBOARDING.md) for full setup):

```bash
git clone <OASIS-repo-url> C:\Source\OASIS-master
git clone https://github.com/NextGenSoftwareUK/ODOOM.git C:\Source\UZDoom
git clone https://github.com/NextGenSoftwareUK/OQUAKE.git C:\Source\vkQuake
git clone <quake-rerelease-qc-repo-url> C:\Source\quake-rerelease-qc
```

Build scripts expect these paths by default; you can change them in the build script for your platform (`BUILD ODOOM.bat` / `BUILD_ODOOM.sh`, `BUILD_OQUAKE.bat` / `BUILD_OQUAKE.sh`).

| Platform | ODOOM build | OQuake build |
|----------|-------------|--------------|
| **Windows** | `BUILD ODOOM.bat` | `BUILD_OQUAKE.bat` |
| **Linux / macOS** | `./BUILD_ODOOM.sh` | `./BUILD_OQUAKE.sh` |

---

## Overview

- **ODOOM** – UZDoom-based Doom with STAR API integration (keycards, inventory, quests, SSO).
- **OQuake** – vkQuake-based Quake with STAR API integration (keys, ammo, weapons, inventory, quests, SSO).
- **STARAPIClient** – **The STAR API client used by ODOOM and OQuake.** C# client that implements the C ABI (`star_api_*`); builds `star_api.dll` and `star_api.lib`. Use this for all game integrations.
- **NativeWrapper** – **Deprecated; do not use.** Legacy C++ wrapper kept for reference only. ODOOM and OQuake use **STARAPIClient** only.
- **star_sync** – C layer (in ODOOM/OQuake folders) for async auth and inventory sync; sits between game code and STARAPIClient.
- **OASIS Omniverse (Unity)** – Optional Unity host shell with hub, ODOOM/OQuake portals, and Control Center (inventory, quests, settings). See [`OASIS Omniverse/README.md`](OASIS%20Omniverse/README.md) inside the Unity project folder.

## Directory structure

```
OASIS Omniverse/
├── README.md                    # This file — index + overview
├── Docs/                        # All Markdown guides (see index above)
│   ├── README.md
│   ├── DEVELOPER_ONBOARDING.md
│   ├── GettingStarted_*.md
│   ├── ARCHITECTURE.md
│   ├── INTEGRATION_GUIDE.md
│   ├── QUICKSTART.md
│   ├── LINUX_BUILD.md
│   ├── PHASE2_QUEST_SYSTEM.md
│   └── …
├── BUILD EVERYTHING.bat         # Build STARAPIClient + ODOOM + OQuake (no prompts, no launch)
├── BUILD_AND_DEPLOY_STAR_CLIENT.bat
├── STARAPIClient/
│   ├── README.md
│   └── ...
├── NativeWrapper/               # Deprecated; do not use. Use STARAPIClient.
│   ├── BUILD_INSTRUCTIONS.md
│   └── ...
├── ODOOM/
│   ├── README.md
│   ├── WINDOWS_INTEGRATION.md
│   ├── BUILD ODOOM.bat
│   ├── RUN ODOOM.bat
│   └── build/
├── OQuake/
│   ├── README.md
│   ├── Docs/
│   │   └── WINDOWS_INTEGRATION.md
│   ├── BUILD_OQUAKE.bat
│   ├── RUN OQUAKE.bat
│   └── build/
├── Doom/                        # Doom integration notes/examples
├── Quake/                       # Quake integration notes/examples
└── OASIS Omniverse/             # Unity hub project (optional)
    └── README.md
```

## Quick reference

- **Build one thing at a time** – Do not run more than one build (or heavy test run) at a time; it can cause issues. Run each build or test suite separately and wait for it to finish before starting the next.
- **First-time setup** – Follow **[Docs/DEVELOPER_ONBOARDING.md](Docs/DEVELOPER_ONBOARDING.md)**.
- **Build everything (no prompts)** – From `OASIS Omniverse\`: run **BUILD EVERYTHING.bat** to build and deploy STARAPIClient, then build ODOOM and OQuake with no prompts and without launching. Use **RUN ODOOM.bat** / **RUN OQUAKE.bat** to launch afterward.
- **Build STAR API client** – From `OASIS Omniverse\`: run **BUILD_AND_DEPLOY_STAR_CLIENT.bat** to build and copy `star_api.dll` / `star_api.lib` / `star_api.h` into Doom, Quake, ODOOM, OQuake, and (if present) UZDoom and vkQuake folders. Or at the start of **BUILD ODOOM.bat** or **BUILD_OQUAKE.bat** choose **Y** when asked “Build and deploy STARAPIClient first?”.
- **Build ODOOM** – From `OASIS Omniverse\ODOOM\`: run **BUILD ODOOM.bat**.
- **Build OQuake** – Run **"OASIS Omniverse\OQuake\BUILD_OQUAKE.bat"** (use Developer Command Prompt for VS).
- **Run ODOOM** – **"OASIS Omniverse\ODOOM\RUN ODOOM.bat"** (builds if needed, then launches).
- **Run OQuake** – **"OASIS Omniverse\OQuake\RUN OQUAKE.bat"** (builds if needed, then launches).
- **Run local APIs** – From OASIS repo root: `Scripts\start_web4_and_web5_apis.bat`.
- **Game config** – Edit `ODOOM\build\oasisstar.json` and `OQuake\build\oasisstar.json` (see onboarding doc).

## Features

- **Cross-game item sharing** – Collect keycards/keys in one game, use in another; persistent inventory via STAR API.
- **Inventory NFT minting** – When enabled in `oasisstar.json` (e.g. `mint_weapons`, `mint_keys`), collecting items can mint an NFT (WEB4 NFTHolon) and attach it to the inventory item; optional per category (weapons, armor, powerups, keys).
- **Avatar/SSO** – Log in with STAR username/password or API key + avatar ID.
- **Multi-game quests** – Quests and objectives spanning ODOOM, OQuake, and more.
- **Stacked/ammo quantities** – Ammo pickups (e.g. shells, nails) sync with correct quantities to the API so totals persist correctly after reload.

## License

This integration follows the same license as the OASIS project.
