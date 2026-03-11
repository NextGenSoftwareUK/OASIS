# OASIS Omniverse

OASIS Omniverse brings **ODOOM** (Doom + OASIS STAR API), **OQuake** (Quake + OASIS STAR API), and the shared STAR API client and tooling into one place. It enables cross-game inventory, quests, and avatar/SSO auth across classic FPS games.

**ODOOM and OQuake are built to be 100% compatible with Windows, macOS, and Linux.** Use the platform-specific Getting Started guide below for your OS.

---

## New here? Start with a Getting Started guide

Choose your platform for a clear, step-by-step setup:

| Platform | Guide |
|----------|--------|
| **Windows** | **[Docs/GettingStarted_Windows.md](Docs/GettingStarted_Windows.md)** |
| **Linux** | **[Docs/GettingStarted_Linux.md](Docs/GettingStarted_Linux.md)** |
| **macOS** | **[Docs/GettingStarted_Mac.md](Docs/GettingStarted_Mac.md)** |

**→ [Developer Onboarding (ODOOM, OQuake & OASIS)](DEVELOPER_ONBOARDING.md)** – Deeper canonical setup (repos, tools, build, run, `oasisstar.json`). Use with the platform guide above.

---

## Repositories to clone

To build ODOOM and OQuake you need the OASIS repo plus the game engines and Quake data. Clone into a common parent (e.g. `C:\Source\`):

| Repository | Purpose |
|------------|---------|
| **OASIS** (this repo) | Backend, STARAPIClient, ODOOM/OQuake integration |
| **UZDoom** | Doom engine used by ODOOM |
| **vkQuake** | Quake engine used by OQuake (Vulkan) |
| **quake-rerelease-qc** | QuakeC source used by OQuake |

Example clone commands (see [DEVELOPER_ONBOARDING.md](DEVELOPER_ONBOARDING.md) for full setup):

```bash
git clone <OASIS-repo-url> C:\Source\OASIS-master
git clone https://github.com/UZDoom/UZDoom.git C:\Source\UZDoom
git clone https://github.com/Novum/vkQuake.git C:\Source\vkQuake
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
- **OASIS Omniverse (Unity)** – Optional Unity host shell with hub, ODOOM/OQuake portals, and Control Center (inventory, quests, settings). See `OASIS Omniverse/README.md` inside the Unity project folder.

## Directory structure

```
OASIS Omniverse/
├── README.md                    # This file
├── DEVELOPER_ONBOARDING.md      # Onboarding – main setup guide; start here
├── BUILD EVERYTHING.bat         # Build STARAPIClient + ODOOM + OQuake (no prompts, no launch)
├── BUILD_AND_DEPLOY_STAR_CLIENT.bat
├── STARAPIClient/               # STAR API client (used by ODOOM & OQuake) → star_api.dll / star_api.lib
│   ├── README.md
│   └── ...
├── NativeWrapper/               # Deprecated; do not use. Use STARAPIClient.
│   ├── BUILD_INSTRUCTIONS.md
│   └── ...
├── ODOOM/                       # ODOOM (UZDoom + STAR integration)
│   ├── README.md
│   ├── WINDOWS_INTEGRATION.md
│   ├── BUILD ODOOM.bat
│   ├── RUN ODOOM.bat
│   └── build/                  # ODOOM.exe, oasisstar.json
├── OQuake/                      # OQuake (vkQuake + STAR integration)
│   ├── README.md
│   ├── WINDOWS_INTEGRATION.md
│   ├── BUILD_OQUAKE.bat
│   ├── RUN OQUAKE.bat
│   └── build/                  # OQUAKE.exe, star_api.dll, oasisstar.json
├── Doom/                        # Doom integration notes/examples
├── Quake/                       # Quake integration notes/examples
├── INTEGRATION_GUIDE.md         # Detailed integration concepts
├── QUICKSTART.md                # Short quick-start + checklist
├── PHASE2_QUEST_SYSTEM.md       # Quest system design
└── OASIS Omniverse/             # Unity hub project (optional)
    └── README.md
```

## Quick reference

- **Build one thing at a time** – Do not run more than one build (or heavy test run) at a time; it can cause issues. Run each build or test suite separately and wait for it to finish before starting the next.
- **First-time setup** – Follow **[DEVELOPER_ONBOARDING.md](DEVELOPER_ONBOARDING.md)**.
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

## Documentation

- **Getting Started (by platform):** [Windows](Docs/GettingStarted_Windows.md) · [Linux](Docs/GettingStarted_Linux.md) · [macOS](Docs/GettingStarted_Mac.md)
- **[DEVELOPER_ONBOARDING.md](DEVELOPER_ONBOARDING.md)** – Onboarding: repos, tools, build, run, config.
- [QUICKSTART.md](QUICKSTART.md) – Minimal steps to build and run + checklist.
- [LINUX_BUILD.md](LINUX_BUILD.md) – Linux/macOS build reference (env vars, scripts).
- [INTEGRATION_GUIDE.md](INTEGRATION_GUIDE.md) – Architecture, phases, API usage, troubleshooting.
- [PHASE2_QUEST_SYSTEM.md](PHASE2_QUEST_SYSTEM.md) – Quest system design and usage.
- [STARAPIClient/README.md](STARAPIClient/README.md) – STAR API client, star_sync, cache, build, tests.
- [ODOOM/README.md](ODOOM/README.md) – ODOOM-specific build and features.
- [OQuake/README.md](OQuake/README.md) – OQuake-specific build and features.
- [ODOOM/WINDOWS_INTEGRATION.md](ODOOM/WINDOWS_INTEGRATION.md) – ODOOM Windows build details.
- [OQuake/WINDOWS_INTEGRATION.md](OQuake/WINDOWS_INTEGRATION.md) – OQuake Windows build details.

## License

This integration follows the same license as the OASIS project.
