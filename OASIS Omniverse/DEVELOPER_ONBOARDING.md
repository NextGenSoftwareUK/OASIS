# Developer Onboarding: ODOOM, OQuake & OASIS Omniverse

This guide helps new developers set up a full local environment to work on **ODOOM** (Doom + OASIS STAR API), **OQuake** (Quake + OASIS STAR API), and the **OASIS** backend and APIs. It covers cloning all required repos, installing tools, building, running, and choosing between local APIs or the live OASIS APIs.

---

## Table of contents

1. [Repositories to clone](#1-repositories-to-clone)
2. [Tools to install](#2-tools-to-install)
3. [Build and run scripts](#3-build-and-run-scripts)
4. [Running the APIs: local vs live](#4-running-the-apis-local-vs-live)
5. [Config: oasisstar.json (ODOOM & OQuake)](#5-config-oasisstarjson-odoom--oquake)
6. [Quick reference](#6-quick-reference)

---

## 1. Repositories to clone

Clone the following into a common parent (e.g. `C:\Source\`). Paths below assume `C:\Source\`; adjust if you use another root.

| Repository | Purpose | Suggested path |
|------------|---------|----------------|
| **OASIS** (this repo) | Backend, ONODE/WEB4, STAR/WEB5, STARAPIClient, ODOOM/OQuake integration code | `C:\Source\OASIS-master` |
| **UZDoom** | Doom engine used by ODOOM | `C:\Source\UZDoom` |
| **vkQuake** | Quake engine used by OQuake (Vulkan) | `C:\Source\vkQuake` |
| **quake-rerelease-qc** | QuakeC source (maps, progs) used by OQuake | `C:\Source\quake-rerelease-qc` |

### Clone commands (examples)

```bash
# OASIS (main repo – contains ODOOM/OQuake integration, APIs, scripts)
git clone <OASIS-repo-url> C:\Source\OASIS-master

# UZDoom – engine for ODOOM
git clone https://github.com/UZDoom/UZDoom.git C:\Source\UZDoom

# vkQuake – engine for OQuake
git clone https://github.com/Novum/vkQuake.git C:\Source\vkQuake

# QuakeC (quake-rerelease-qc) – game data / progs for OQuake
git clone <quake-rerelease-qc-repo-url> C:\Source\quake-rerelease-qc
```

Replace `<OASIS-repo-url>` and `<quake-rerelease-qc-repo-url>` with your actual repo URLs. The build scripts reference these paths; you can change them in the batch files if your layout differs (see below).

### Optional (level editing)

- **Ultimate Doom Builder** – for editing Doom maps (e.g. for ODOOM). Install from the project’s official site; no clone required.

---

## 2. Tools to install

Install these before building ODOOM, OQuake, or the OASIS APIs.

| Tool | Used by | Notes |
|------|---------|--------|
| **Visual Studio 2019 or 2022** | ODOOM, OQuake, STARAPIClient, OASIS APIs | Install **Desktop development with C++** (MSVC, Windows SDK). Use **Developer Command Prompt for VS** when running build scripts so `msbuild`/`cmake` are in PATH. |
| **CMake** (3.10+) | UZDoom, vkQuake | Add to PATH or ensure it’s available from the Developer Command Prompt. |
| **Python 3.6+** | UZDoom (ODOOM) | Required by UZDoom’s build; add to PATH. |
| **Vulkan SDK** | vkQuake (OQuake) | Install from [vulkan.lunarg.com](https://vulkan.lunarg.com/sdk/home). Required for building vkQuake. |
| **.NET SDK** (e.g. 6 or 8) | OASIS WEB4/WEB5 APIs, STARAPIClient | Needed to run and build the ONODE and STAR Web APIs and the C# STAR API client. |

### Checklist

- [ ] Visual Studio with C++ tools
- [ ] CMake
- [ ] Python 3
- [ ] Vulkan SDK (for OQuake)
- [ ] .NET SDK (for APIs and STARAPIClient)

---

## 3. Build and run scripts

### OASIS repo layout (relevant folders)

- `Scripts\` – start/stop WEB4 and WEB5 APIs, run tests.
- `OASIS Omniverse\` – root for **BUILD EVERYTHING.bat** (build client + ODOOM + OQuake with no prompts), **BUILD_AND_DEPLOY_STAR_CLIENT.bat**, and game folders.
- `OASIS Omniverse\ODOOM\` – ODOOM build and run scripts.
- `OASIS Omniverse\OQuake\` – OQuake build and run scripts.
- `OASIS Omniverse\STARAPIClient\` – C# client and C exports used by the games.

### ODOOM

- **Build:** From `OASIS Omniverse\ODOOM\` run:
  - `BUILD ODOOM.bat`
- **Build and run:** Run:
  - `RUN ODOOM.bat`
- **Build everything (no prompts):** From `OASIS Omniverse\` run **BUILD EVERYTHING.bat** to build STARAPIClient, ODOOM, and OQuake with no prompts; then use RUN ODOOM.bat / RUN OQUAKE.bat to launch.
- Output: `OASIS Omniverse\ODOOM\build\ODOOM.exe` (and DLLs). Place your WAD (e.g. `doom2.wad`) in `build\` or configure the game to find it.
- If UZDoom is not at `C:\Source\UZDoom`, set **UZDOOM_SRC** at the top of `BUILD ODOOM.bat`.

### OQuake

- **Build:** From the OASIS repo root run:
  - `"OASIS Omniverse\OQuake\BUILD_OQUAKE.bat"`
- **Build and run:** Run:
  - `"OASIS Omniverse\OQuake\RUN OQUAKE.bat"`
- Set **VKQUAKE_SRC** (e.g. `C:\Source\vkQuake`) and **OQUAKE_BASEDIR** / **QUAKE_ENGINE_EXE** in the script if needed. Run from **Developer Command Prompt for VS** so MSBuild is in PATH.
- Output: `OASIS Omniverse\OQuake\build\` (OQUAKE.exe and star_api.dll). Quake game data (id1, pak0.pak, pak1.pak, gfx.wad) must be available (e.g. via `-basedir` or next to the exe). See `OQuake\WINDOWS_INTEGRATION.md`.

### STAR API client (C layer)

- The ODOOM and OQuake build scripts use or build the STAR API client (e.g. `star_api.dll` / `star_api.lib`) from the OASIS Omniverse (e.g. STARAPIClient / Doom folder). You typically do **not** need to build it separately when using `BUILD ODOOM.bat` and `BUILD_OQUAKE.bat`.

---

## 4. Running the APIs: local vs live

The games talk to two HTTP APIs:

- **WEB4 (OASIS ONODE)** – e.g. avatar, auth, inventory persistence.
- **WEB5 (STAR)** – STAR API used by the games (inventory, quests, etc.).

You can either run these **locally** or point the games at the **live** OASIS APIs.

### Option A: Run WEB4 and WEB5 locally

1. From the **OASIS repo root** (e.g. `C:\Source\OASIS-master`), run:
   ```batch
   Scripts\start_web4_and_web5_apis.bat
   ```
   This calls `Scripts\start_web4_and_web5_apis.ps1`, which starts WEB4 (ONODE) then WEB5 (STAR) in serial. Default URLs:
   - WEB4: `http://localhost:5555`
   - WEB5: `http://localhost:5556`

2. Keep the window open (or run with `-NoWait` if the script supports it) so the APIs stay up.

3. Use **local** API base URLs in the game config (see [Section 5](#5-config-oasisstarjson-odoom--oquake)).

4. To stop them: run `Scripts\stop_web4_and_web5_apis.bat` (or the matching `.ps1`).

### Option B: Use the live OASIS APIs

If you don’t start the local APIs, you can point the games at the live servers by editing **oasisstar.json** in each game’s **build** folder:

- **WEB4 (OASIS):** `https://oasisweb4.one/api`
- **WEB5 (STAR):** `https://oasisweb4.one/star/api`

See [Section 5](#5-config-oasisstarjson-odoom--oquake) for the exact keys and paths.

---

## 5. Config: oasisstar.json (ODOOM & OQuake)

Both games read STAR/WEB4 URLs and options from a config file named **oasisstar.json** in their **build** folder. Ensure this file exists after your first build (it may be created by the game or the build).

### File locations

- **ODOOM:** `OASIS Omniverse\ODOOM\build\oasisstar.json`
- **OQuake:** `OASIS Omniverse\OQuake\build\oasisstar.json`

### Relevant keys

- **star_api_url** – WEB5 (STAR) API base URL (e.g. `http://localhost:5556` or `https://oasisweb4.one/star/api`).
- **oasis_api_url** – WEB4 (OASIS/ONODE) API base URL (e.g. `http://localhost:5555` or `https://oasisweb4.one/api`).
- **mint_weapons**, **mint_armor**, **mint_powerups**, **mint_keys** – Set to `1` to mint an NFT (WEB4 NFTHolon) when collecting that category; `0` to disable. Optional; default off for keys/weapons/armor/powerups if omitted.
- **nft_provider** – NFT mint provider (e.g. `SolanaOASIS`). Optional.

Other keys (e.g. `beam_face`, `stack_armor`, `stack_keys`) control behavior; you can leave them as-is for onboarding.

### Local APIs (default after build)

```json
{
  "star_api_url": "http://localhost:5556",
  "oasis_api_url": "http://localhost:5555",
  "beam_face": 1,
  "stack_armor": 1,
  "stack_weapons": 1,
  "stack_powerups": 1,
  "stack_keys": 1,
  "mint_weapons": 0,
  "mint_armor": 0,
  "mint_powerups": 0,
  "mint_keys": 0,
  "nft_provider": "SolanaOASIS"
}
```

### Live APIs

Edit **oasisstar.json** in **ODOOM** and **OQuake** build folders and set:

- **WEB4:** `"oasis_api_url": "https://oasisweb4.one/api"`
- **WEB5:** `"star_api_url": "https://oasisweb4.one/star/api"`

Example (excerpt):

```json
{
  "star_api_url": "https://oasisweb4.one/star/api",
  "oasis_api_url": "https://oasisweb4.one/api",
  "mint_weapons": 0,
  "mint_armor": 0,
  "mint_powerups": 0,
  "mint_keys": 0,
  "nft_provider": "SolanaOASIS",
  "send_to_address_after_minting": ""
}
```

Optional: **`send_to_address_after_minting`** – wallet address to send the minted NFT to after minting (used by ODOOM and OQuake when minting inventory item NFTs via the STAR API).

Summary:

- **Local:** Run `Scripts\start_web4_and_web5_apis.bat` and use `localhost:5555` / `localhost:5556` in oasisstar.json.
- **Live:** Do not start the bat; set `oasis_api_url` and `star_api_url` in both games’ `build\oasisstar.json` to the URLs above.

---

## 6. Quick reference

| Task | Action |
|------|--------|
| Clone everything | OASIS-master, UZDoom, vkQuake, quake-rerelease-qc under e.g. `C:\Source\`. |
| Install tools | VS (C++), CMake, Python 3, Vulkan SDK, .NET SDK. |
| Build everything (no prompts) | From `OASIS Omniverse\`: **BUILD EVERYTHING.bat** (client + ODOOM + OQuake; then use RUN ODOOM.bat / RUN OQUAKE.bat). |
| Start local APIs | From OASIS repo root: `Scripts\start_web4_and_web5_apis.bat`. |
| Stop local APIs | `Scripts\stop_web4_and_web5_apis.bat`. |
| Build ODOOM | `OASIS Omniverse\ODOOM\BUILD ODOOM.bat` (set UZDOOM_SRC if needed). |
| Run ODOOM | `OASIS Omniverse\ODOOM\RUN ODOOM.bat` or run `build\ODOOM.exe`. |
| Build OQuake | From repo root: `"OASIS Omniverse\OQuake\BUILD_OQUAKE.bat"` (set VKQUAKE_SRC; use Developer Command Prompt). |
| Run OQuake | `"OASIS Omniverse\OQuake\RUN OQUAKE.bat"` or run `build\OQUAKE.exe` with game data. |
| Use live APIs | Edit `ODOOM\build\oasisstar.json` and `OQuake\build\oasisstar.json`: set `star_api_url` and `oasis_api_url` to `https://oasisweb4.one/star/api` and `https://oasisweb4.one/api`. |
| Auth (games) | Set env vars: `STAR_USERNAME` / `STAR_PASSWORD` or `STAR_API_KEY` / `STAR_AVATAR_ID`. |

### More detail

- **Architecture & porting:** `OASIS Omniverse\ARCHITECTURE.md` – client-centric design, minimal game hooks, how to port new games.
- **Integration (items, quests, APIs):** `OASIS Omniverse\INTEGRATION_GUIDE.md`
- **ODOOM:** `OASIS Omniverse\ODOOM\WINDOWS_INTEGRATION.md`, `ODOOM\README.md`
- **OQuake:** `OASIS Omniverse\OQuake\WINDOWS_INTEGRATION.md`, `OQuake\README.md`
- **APIs and scripts:** `Scripts\README.md`
- **STARAPIClient:** `OASIS Omniverse\STARAPIClient\README.md`

---

*Last updated for OASIS Omniverse (ODOOM, OQuake) and Scripts in the OASIS repo.*
