# OASIS Omniverse

OASIS Omniverse brings **ODOOM** (Doom + OASIS STAR API), **OQuake** (Quake + OASIS STAR API), and the shared STAR API client and tooling into one place. It enables cross-game inventory, quests, and avatar/SSO auth across classic FPS games.

---

## New here? Start with onboarding

**→ [Developer Onboarding (ODOOM, OQuake & OASIS)](DEVELOPER_ONBOARDING.md)** – The **canonical setup guide** for OASIS Omniverse. Clone repos, install tools, build ODOOM/OQuake, run local or live APIs, and configure `oasisstar.json`. Use this as your main entry point for first-time setup.

---

## Overview

- **ODOOM** – UZDoom-based Doom with STAR API integration (keycards, inventory, quests, SSO).
- **OQuake** – vkQuake-based Quake with STAR API integration (keys, ammo, weapons, inventory, quests, SSO).
- **STARAPIClient** – C# client that implements the C ABI (`star_api_*`) used by the games; builds `star_api.dll` and `star_api.lib`.
- **NativeWrapper** – Legacy C++ wrapper; **not currently used**. Obsoleted by the C# **STARAPIClient** (which produces `star_api.dll` / `star_api.lib`). Kept for reference only.
- **star_sync** – C layer (in ODOOM/OQuake folders) for async auth and inventory sync; sits between game code and STARAPIClient.
- **OASIS Omniverse (Unity)** – Optional Unity host shell with hub, ODOOM/OQuake portals, and Control Center (inventory, quests, settings). See `OASIS Omniverse/README.md` inside the Unity project folder.

## Directory structure

```
OASIS Omniverse/
├── README.md                    # This file
├── DEVELOPER_ONBOARDING.md      # Onboarding – main setup guide; start here
├── STARAPIClient/               # C# client → star_api.dll / star_api.lib
│   ├── README.md
│   └── ...
├── NativeWrapper/               # Legacy (obsolete); STARAPIClient is used instead
│   ├── BUILD_INSTRUCTIONS.md
│   └── ...
├── ODOOM/                       # ODOOM (UZDoom + STAR integration)
│   ├── README.md
│   ├── WINDOWS_INTEGRATION.md
│   ├── BUILD ODOOM.bat
│   └── build/                  # ODOOM.exe, oasisstar.json
├── OQuake/                      # OQuake (vkQuake + STAR integration)
│   ├── README.md
│   ├── WINDOWS_INTEGRATION.md
│   ├── BUILD_OQUAKE.bat
│   └── build/                  # OQUAKE.exe, star_api.dll, oasisstar.json
├── Doom/                        # Doom integration notes/examples
├── Quake/                       # Quake integration notes/examples
├── INTEGRATION_GUIDE.md         # Detailed integration concepts
├── INTEGRATION_STATUS.md        # Current status and next steps
├── QUICKSTART.md                # Short quick-start + checklist
├── PHASE2_QUEST_SYSTEM.md       # Quest system design
└── OASIS Omniverse/             # Unity hub project (optional)
    └── README.md
```

## Quick reference

| Task | Action |
|------|--------|
| **First-time setup** | Follow **[DEVELOPER_ONBOARDING.md](DEVELOPER_ONBOARDING.md)** |
| Build ODOOM | From `OASIS Omniverse\ODOOM\`: run `BUILD ODOOM.bat` |
| Build OQuake | From repo root: run `"OASIS Omniverse\OQuake\BUILD_OQUAKE.bat"` (use Developer Command Prompt for VS) |
| Run local APIs | From OASIS repo root: `Scripts\start_web4_and_web5_apis.bat` |
| Game config | Edit `ODOOM\build\oasisstar.json` and `OQuake\build\oasisstar.json` (see onboarding doc) |

## Features

- **Cross-game item sharing** – Collect keycards/keys in one game, use in another; persistent inventory via STAR API.
- **Avatar/SSO** – Log in with STAR username/password or API key + avatar ID.
- **Multi-game quests** – Quests and objectives spanning ODOOM, OQuake, and more.
- **Stacked/ammo quantities** – Ammo pickups (e.g. shells, nails) sync with correct quantities to the API so totals persist correctly after reload.

## Documentation

This is the full set of current docs (redundant setup/status docs have been removed).

| Doc | Purpose |
|-----|--------|
| **[DEVELOPER_ONBOARDING.md](DEVELOPER_ONBOARDING.md)** | **Start here (onboarding)** – canonical setup: repos, tools, build, run, config |
| [QUICKSTART.md](QUICKSTART.md) | Minimal steps to build and run + checklist |
| [INTEGRATION_STATUS.md](INTEGRATION_STATUS.md) | Current status, next steps, doc links |
| [INTEGRATION_GUIDE.md](INTEGRATION_GUIDE.md) | Architecture, phases, API usage, troubleshooting |
| [PHASE2_QUEST_SYSTEM.md](PHASE2_QUEST_SYSTEM.md) | Quest system design and usage |
| [STARAPIClient/README.md](STARAPIClient/README.md) | STAR API client, star_sync, cache, build, tests |
| [ODOOM/README.md](ODOOM/README.md) | ODOOM-specific build and features |
| [OQuake/README.md](OQuake/README.md) | OQuake-specific build and features |
| [ODOOM/WINDOWS_INTEGRATION.md](ODOOM/WINDOWS_INTEGRATION.md) | ODOOM Windows build details |
| [OQuake/WINDOWS_INTEGRATION.md](OQuake/WINDOWS_INTEGRATION.md) | OQuake Windows build details |

## License

This integration follows the same license as the OASIS project.
