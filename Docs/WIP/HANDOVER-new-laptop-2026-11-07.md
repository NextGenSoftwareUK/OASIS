# OASIS Omniverse — New Laptop Handover (updated 2026-11-07)

This is the single source of truth for getting a new machine set up and continuing work. Read this + `MEMORY.md` at the start of every session.

---

## HARD RULES — never break these

| Rule | Detail |
|------|--------|
| **PowerShell: TOTAL BAN** | Never use PowerShell tool or `powershell`/`pwsh` in Bash. Use Bash, Python, Edit/Write/Grep/Glob. UTF-8 BOM corruption risk. |
| **ALL work → Development first** | Never commit to `master`/`main` directly. Only merge after David confirms. |
| **Always `dotnet build` before pushing C#** | Interface changes cascade across 33+ provider projects. |
| **Always `git push` after committing** | David tests live site, not localhost. Unpushed = looks broken. |
| **No Redis** | Persistence via OASIS Data API/provider layer only. |
| **No Co-Authored-By lines** | Commits show David Ellams only. |
| **No preview tool after edits** | Just commit/push, let David check in his browser. |
| **Body/footer text = `#a8bfd8`** | Never use `--text-dim` (`#6a80a8`) — too dark on navy. |
| **OPORTAL-JS and OPORTAL-React are private repos** | Never list as public. |
| **Never push PIPER therapy project** | Without David's explicit review first. |
| **No fancy HTML docs** | Plain `.md` files only for docs/plans/analysis. |
| **Never leave partial grid rows** | Fill every row in card grids on all sites and docs. |
| **One type per C# file** | No inline namespace-scope types; jobs → `Jobs/`; interop → `Interop/`; aim <500 lines/file. |

---

## New Laptop Setup — Clone These Repos

### Primary (OASIS2 — has submodules)

```bash
git clone --recurse-submodules https://github.com/NextGenSoftwareUK/OASIS.git "C:\Source\OASIS2"
cd "C:\Source\OASIS2"
git checkout Development
git submodule update --init --recursive
```

Then check out Development in each submodule:
```bash
cd "OASIS Omniverse/OASIS Hub"
git checkout Development
cd "../OASIS Architecture/NextGenSoftware.OASIS.API.Core"
git checkout Development
cd "../../STAR ODK"
git checkout Development
```

### All Other Repos

```bash
git clone https://github.com/NextGenSoftwareUK/OGEngineSite.git         "C:\Source\OGEngineSite"
git clone https://github.com/NextGenSoftwareUK/OASISOmniverseSite.git   "C:\Source\OASISOmniverseSite"
git clone https://github.com/NextGenSoftwareUK/ODOOMSite.git            "C:\Source\ODOOMSite"
git clone https://github.com/NextGenSoftwareUK/OQuakeSite.git           "C:\Source\OQuakeSite"
git clone https://github.com/NextGenSoftwareUK/HoloNETSite.git         "C:\Source\HoloNETSite"
git clone https://github.com/NextGenSoftwareUK/STARNETSite.git         "C:\Source\STARNETSite"
git clone https://github.com/NextGenSoftwareUK/OurWorldSite.git        "C:\Source\OurWorldSite"
git clone https://github.com/NextGenSoftwareUK/NFTFoundersLandingPage.git "C:\Source\NFTFoundersLandingPage"
# OPORTAL (main site, Vercel): NOT found at C:\Source\OPORTAL on the old laptop when checked 2026-11-07 — confirm its repo URL/location before cloning
git clone https://github.com/NextGenSoftwareUK/OIDE.git                "C:\Source\IDE"
```

**Private repos (need GitHub auth):**
```bash
git clone https://github.com/NextGenSoftwareUK/OPORTAL-JS.git          "C:\Source\OPORTAL-JS"
git clone https://github.com/NextGenSoftwareUK/OPORTAL-React.git       "C:\Source\OPORTAL-React"
```

### Game Source Forks (for OGames BUILD scripts)

```bash
git clone https://github.com/NextGenSoftwareUK/ODOOM.git               "C:\Source\ODOOM"
git clone https://github.com/NextGenSoftwareUK/OQuake.git              "C:\Source\OQuake"
# other OGame source forks follow same pattern: C:\Source\{GameName}
git clone https://github.com/NextGenSoftwareUK/OHalflife.git           "C:\Source\OHalflife"
# (David's fork of FWGS/hlsdk-portable — the OASIS hooks are wired in HERE; do NOT clone upstream FWGS/hlsdk-portable)
git clone https://github.com/FWGS/xash3d-fwgs.git                     "C:\Source\xash3d-fwgs"
```

---

## Repository State (as of 2026-09-13)

| Repo | Path | Branch | Last notable commit |
|------|------|--------|---------------------|
| OASIS2 | `C:\Source\OASIS2` | `Development` | 11 new providers + MapProviderType namespace fix + unit test csproj fix |
| OASIS Hub (submodule) | inside OASIS2 | `Development` | Hub config path fix — all 23 portals use `../../OGames/{Game}/build/` |
| IDE | `C:\Source\IDE` | `main` | Build gates + CI + Web7-10 panel + bridge tests (`b64eb1c`) |
| OPORTAL-JS | `C:\Source\OPORTAL-JS` | `Development` | Notification panel z-index fix (`6931bd6`) |
| OGEngineSite | `C:\Source\OGEngineSite` | `master` | 23 OGames connected stat |

---

## OASIS2 — C# Current State

**Branch:** `Development`

### 11 New Providers (shipped 2026-09-13, commit `00a148334`)

All are skeleton implementations (IOASISStorageProviderBase / IOASISMapProvider pattern, .NET 10) ready for real API wiring:

| Provider | Category | Purpose |
|----------|----------|---------|
| GoogleMapsOASIS | Maps | Google Maps Platform — mapping, routing, Places API |
| HEREMapsOASIS | Maps | HERE Maps Platform — enterprise routing and fleet |
| MapLibreOASIS | Maps | MapLibre — open-source vector tile rendering |
| NianticLightshipOASIS | Maps | Niantic Lightship AR — AR anchors, VPS |
| AxelarOASIS | Blockchain | Axelar GMP — general-purpose cross-chain messaging |
| WormholeOASIS | Blockchain | Wormhole — native cross-chain token and message bridge |
| CeramicOASIS | Blockchain (moved from Network) | Ceramic Network — decentralised mutable data streams |
| CivicOASIS | Network | Civic — on-chain identity verification |
| ReclaimProtocolOASIS | Network | Reclaim Protocol — ZK attestations from web2 APIs |
| DenoDeployOASIS | Storage | Deno Deploy — edge-first serverless storage |
| FastlyOASIS | Storage | Fastly Compute — edge compute with KV store |

All wired into all three solution files (The OASIS.sln, The OASIS - NoTests.sln, The OASIS - Public.sln).

**Status check 2026-11-07:** my original skeletons did not compile cleanly; commit `2d0a601adf` ("fix: resolve all build errors in 11 new OASIS providers") on `Development` fixed them and moved Ceramic to `Providers/Blockchain/`. After that, GoogleMaps, HEREMaps, MapLibre, NianticLightship, Axelar, Wormhole, Civic, ReclaimProtocol, DenoDeploy, Fastly and the ONODE unit-test project all built; Ceramic was not rebuilt at its new path.

### Other C# fixes in same commit
- `MapProviderType.cs` in Contracts project: namespace was `Contracts.Interfaces` → fixed to `Contracts.Enums`
- Unit test project (`NextGenSoftware.OASIS.API.ONODE.Core.UnitTests`): added `<GenerateAssemblyInfo>false</GenerateAssemblyInfo>` to fix CS0579 duplicate assembly attribute build error

### OASIS2 Backlog
- **ONET architecture fixes (#1–5) are on `master` but NOT on `Development`** — need cherry-pick (see HANDOVER-2026-09-12.md at repo root for full list)
- Unit tests exist in `ONODE/TestProjects/` but should be re-run after the csproj fix: `dotnet test` in the test project
- 7 files >1000 lines on master pending split: `OAPPManagerBase.cs`, `SOLIDOASIS.cs`, `TelegramBotService.cs` clearest candidates
- 96 annotated split-audit verdicts awaiting David's check before deletion

---

## OASIS Omniverse — Project Layout

```
C:\Source\OASIS2\
  OASIS Omniverse\
    OASIS Hub\                    ← Unity Hub project (submodule → OASIS-Omniverse-Hub repo)
      Assets\StreamingAssets\
        omniverse_host_config.json  ← drives all 23 Hub portals
    OGames\                       ← one folder per OGame
      ODOOM\                      ← integration files + BUILD script + build output
        BUILD ODOOM.bat / BUILD_ODOOM.sh
        build\                    ← executable lives here (what Hub points at)
      OQuake\
      OHalfLife\                  ← added 2026-08-08
        BUILD_OHALFLIFE.bat / BUILD_OHALFLIFE.sh
        ohalflife_ogengine_integration.h/.cpp
        oasisstar.json
        build\
      ... (23 total)
    OGEngineClient\               ← ogengine.h / ogengine_sync.h / ogengine.dll/.lib
    Docs\
      OGENGINE_VISION_AND_ROADMAP.md
      BEST_PORTS_AND_EDITORS.md
    README.md
```

**Game source forks live separately at `C:\Source\{GameName}`** — the BUILD scripts inject integration files into these, build, then copy output back to `OGames/{Game}/build/`.

---

## How the OGames BUILD Scripts Work

Each `OGames/{Game}/BUILD {Game}.bat`:
1. **Copy** integration `.cpp/.h/oasisstar.json` from `OGames/{Game}/` into `C:\Source\{Game}\`
2. **CMake configure + build** inside the source fork
3. **Copy output** (`.exe` + DLLs) back to `OGames/{Game}/build\`

The `C:\Source\{Game}` folder is a build workspace only. OASIS integration files live in OASIS2.

---

## Hub Portal Config

**File:** `OASIS Omniverse\OASIS Hub\Assets\StreamingAssets\omniverse_host_config.json`

**Path resolution:** from `OASIS Hub/Assets/`, all paths resolve via `../../OGames/{Game}/build/` → `OASIS Omniverse/OGames/{Game}/build/`.

**Grid layout (23 portals):**
- **Row 1 (Z=12):** idTech — ODOOM, ODOOM64, OHERETIC, OHEXEN, OHEXENII, OSTRIFE, ODOOM3, ODOOM3-BFG, OWOLF3D, OQUAKE, OQUAKE2 (X: −20 to +20, step 4)
- **Row 2 (Z=20):** RT/Build/Gen-2 — OQUAKE2-RTX, OQUAKE3, ORTCW, ODUKE3D, ODUKE3D-RT, OBLOOD, OEXHUMED, OSHADOWWARRIOR, OSHADOWWARRIOR-RT, OMORROWIND, OMINECRAFT
- **Row 3 (Z=28):** OHALFLIFE at X=−20

**Hub is a git submodule** — commit inside `OASIS Omniverse\OASIS Hub\`, push, then update parent pointer:
```bash
cd "C:\Source\OASIS2\OASIS Omniverse\OASIS Hub"
git add Assets/StreamingAssets/omniverse_host_config.json
git commit -m "..." && git push
cd "C:\Source\OASIS2"
git add "OASIS Omniverse/OASIS Hub"
git commit -m "chore: update Hub submodule pointer" && git push
```

---

## OHalfLife Integration (newest OGame — 2026-08-08)

**Engine:** Xash3D FWGS (`C:\Source\xash3d-fwgs`) — no OASIS code here  
**OASIS hooks:** FWGS/hlsdk-portable fork at `C:\Source\OHalflife\dlls\`

| File | Function | Hook |
|------|----------|------|
| `dlls/game.cpp` | `GameDLLInit()` | `OHalfLife_STAR_Init(apiUrl, configPath)` |
| `dlls/client.cpp` | `ServerDeactivate()` | `OHalfLife_STAR_Cleanup()` |
| `dlls/client.cpp` | `StartFrame()` | `OHalfLife_STAR_Tick()` |
| `dlls/combat.cpp` | `CBaseMonster::Killed()` | `OHalfLife_STAR_OnMonsterKilled(classname, classname, "")` |
| `dlls/player.cpp` | `CBasePlayer::AddPlayerItem()` | `OHalfLife_STAR_OnItemPickup(classname, classname, 1)` |

`oasisstar.json`: 18 enemies (Headcrab 10XP → Nihilanth 500XP), 5 key items (HEV Suit, Long Jump Module, Egon, Gauss, RPG).

---

## OGames Count: 23 total

**Gen-1 FPS (21):** ODOOM, ODOOM64, OHERETIC, OHEXEN, OHEXENII, OSTRIFE, ODOOM3, ODOOM3-BFG, OWOLF3D, OQUAKE, OQUAKE2, OQUAKE2-RTX, OQUAKE3, ORTCW, ODUKE3D, ODUKE3D-RT, OBLOOD, OEXHUMED, OSHADOWWARRIOR, OSHADOWWARRIOR-RT, OHALFLIFE  
**Gen-2 (2):** OMORROWIND (OpenMW), OMINECRAFT (Minetest Lua mod)

---

## IDE (C:\Source\IDE)

**Branch:** `main` — last commit `b64eb1c`

- Build gates: `type-check` (tsc on both tsconfigs) + `check:api` (drift checker) before build
- CI: `.github/workflows/ci.yml` triggers on push/PR to Development/master
- Web7–10 panel: `Web7To10Panel.tsx` — 6 bridged methods in RightPanelStack
- Bridge-contract tests: `tests/bridge-contract.test.ts` (Vitest)

**Pending on new machine:** `npm install` in `C:\Source\IDE` then `npm test`

---

## OPORTAL-JS (C:\Source\OPORTAL-JS) — private

Last commit `6931bd6`. All 11 portal modals are source-aware (green=live, amber=test, muted=demo).  
Known bug: `localStorage` avatar `id` ≠ `avatarId` after login — workaround in `nft-modal.js`.

---

## Static Sites

| Site | Path | Domain |
|------|------|--------|
| OASIS Omniverse | `C:\Source\OASISOmniverseSite\index.html` | oasisomniverse.one |
| OPORTAL | `C:\Source\OPORTAL\` | oportal.oasisomniverse.one (Vercel, vercel.json rewrites) |
| HoloNET | `C:\Source\HoloNETSite\index.html` | holonet.oasisomniverse.one |
| STARNET | `C:\Source\STARNETSite\index.html` | starnet.oasisomniverse.one |
| OGEngine | `C:\Source\OGEngineSite\index.html` | ogengine.oasisomniverse.one |
| OQuake | `C:\Source\OQuakeSite\index.html` | oquake.oasisomniverse.one |
| ODOOM | `C:\Source\ODOOMSite\index.html` | odoom.oasisomniverse.one |
| Our World | `C:\Source\OurWorldSite\index.html` | ourworld.oasisomniverse.one |
| Founders NFT | `C:\Source\NFTFoundersLandingPage\` | founders.oasisomniverse.one |

All sites: `#030714` bg, Orbitron/Rajdhani/Share Tech Mono fonts, animated star canvas.

---

## Canonical URLs

| Resource | URL |
|----------|-----|
| WEB4 OASIS API (live) | https://api.web4.oasisomniverse.one |
| WEB5 STAR API (live) | https://api.starnet.oasisomniverse.one |
| STAR CLI / ODK Runtime v3.5.0 | https://github.com/NextGenSoftwareUK/OASIS/releases/tag/STAR-ODK-Runtime-v3.5.0 |
| OASIS Runtime v4.5.1 | https://github.com/NextGenSoftwareUK/OASIS/releases/tag/OASIS-Runtime-v4.5.1 |
| ODOOM v1.0.0 Alpha | https://github.com/NextGenSoftwareUK/ODOOM/releases/tag/ODOOM_v.1.0.0_ALPHA |
| OQuake v1.0.0 Alpha | https://github.com/NextGenSoftwareUK/OQUAKE/releases/tag/OQUAKE_v1.0.0_ALPHA |
| HoloNET GitHub | https://github.com/NextGenSoftwareUK/holochain-client-csharp |
| OASIS GitHub | https://github.com/NextGenSoftwareUK/OASIS |

---

## Prerequisites for OGames BUILD Scripts

- Visual Studio 2022 (or MSVC Build Tools) + CMake 3.16+
- Python 3.11+
- Quake PAK files at `C:\Program Files (x86)\Steam\steamapps\common\Quake\id1\`
- Half-Life via Steam (copy `valve\` into OHalfLife build output)
- Xash3D FWGS cloned at `C:\Source\xash3d-fwgs`
- Ultimate Doom Builder at `C:\Source\UltimateDoomBuilder\Build\` (optional, for ODOOM editor)
