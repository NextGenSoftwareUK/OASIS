# OASIS Platform — New Machine Setup & Handover
*David Ellams · davidellams@hotmail.com · Generated 2026-09-13*

---

## 1. Prerequisites

| Tool | Version | Install |
|------|---------|---------|
| Node.js | 18.x LTS | https://nodejs.org |
| .NET SDK | 10.0 | https://dotnet.microsoft.com/download |
| Git | 2.50+ | https://git-scm.com |
| GitHub CLI | latest | https://cli.github.com |
| VS Code or Rider | latest | https://code.visualstudio.com |
| Claude Code | latest | `npm install -g @anthropic-ai/claude-code` |

```bash
git config --global user.name "David Ellams"
git config --global user.email "davidellams@hotmail.com"
gh auth login
```

---

## 2. Clone everything into C:\Source

```bash
mkdir C:/Source && cd C:/Source

# Main monorepo (has submodules — this takes a few minutes)
git clone --recurse-submodules https://github.com/NextGenSoftwareUK/OASIS.git OASIS2
cd OASIS2 && git checkout Development && git submodule update --init --recursive && cd ..

# IDE (Electron)
git clone https://github.com/NextGenSoftwareUK/OIDE.git IDE
cd IDE && npm install && cd ..

# OPORTAL (private)
git clone https://github.com/NextGenSoftwareUK/OPORTAL-JS.git
git clone https://github.com/NextGenSoftwareUK/OPORTAL-React.git

# Sites
git clone https://github.com/NextGenSoftwareUK/Web4Site.git
git clone https://github.com/NextGenSoftwareUK/Web6Site.git
git clone https://github.com/NextGenSoftwareUK/Web7Site.git
git clone https://github.com/NextGenSoftwareUK/Web8Site.git
git clone https://github.com/NextGenSoftwareUK/Web9Site.git
git clone https://github.com/NextGenSoftwareUK/Web10Site.git
git clone https://github.com/NextGenSoftwareUK/STARWebsite.git
git clone https://github.com/NextGenSoftwareUK/STARNETSite.git
git clone https://github.com/NextGenSoftwareUK/OASISOmniverseSite.git
git clone https://github.com/NextGenSoftwareUK/OASISDocsWebsite.git
git clone https://github.com/NextGenSoftwareUK/OGEngineSite.git
```

### Submodules inside OASIS2 (auto-cloned above)
- `HoloNET-ORM`
- `STAR ODK`
- `WEB6`
- `OASIS Architecture/NextGenSoftware.OASIS.API.Core`
- `ONODE/NextGenSoftware.OASIS.API.ONODE.Core`
- `ONODE/ONODEManager`
- `OASIS Omniverse/OGEngineClient`
- `OASIS Omniverse/OASIS Hub`

---

## 3. Build verification

### OASIS2 (.NET)
```bash
cd C:/Source/OASIS2/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI
dotnet build
```
Must pass before committing any C# changes — interface changes cascade across 33+ provider projects.

### IDE (Electron + TypeScript)
```bash
cd C:/Source/IDE
npm install          # pulls vitest + all deps (do this on first clone)
npm run type-check   # tsc --noEmit on main + preload tsconfigs
npm run check:api    # API drift checker (validates bridge vs OpenAPI specs)
npm test             # Vitest bridge-contract tests
npm run build        # full build — runs all 3 gates above automatically
npm run dev          # dev mode with HMR
```

---

## 4. GitHub Actions secrets (OASIS2 repo)

Go to: GitHub → NextGenSoftwareUK/OASIS → Settings → Secrets

| Secret | Used by |
|--------|---------|
| `PRIVATE_SUBMODULE_PAT` | `ci-cd.yml`, `publish-nuget.yml`, `publish-mcp.yml` |

> **Do NOT add `SUBMODULES_PAT`** — that name is wrong. `PRIVATE_SUBMODULE_PAT` already exists.

---

## 5. Claude Code memory

Copy your entire `C:\Users\David\.claude\` folder to the new machine. It contains:
- `CLAUDE.md` — global rules (PowerShell ban, etc.)
- `projects\C--Source-OASIS2\memory\` — persistent project memory, handovers, feedback

Start a Claude Code session in `C:\Source\OASIS2` and it loads everything automatically.

---

## 6. Hard rules (tell Claude these if starting fresh)

1. **PowerShell: BANNED** — Bash / Python / Edit / Write / Grep / Glob only
2. **All work → `Development` branch first** — never commit directly to `master`
3. **`dotnet build` before committing any C#** — interface changes cascade
4. **Always `git push` after committing**
5. **Never Redis** — persistence via OASIS Data API / provider layer
6. **OPORTAL-JS and OPORTAL-React are private** — never list as public
7. **Never push without David's review** — PIPER repos especially
8. **No Co-Authored-By lines in commits** — commits show only David Ellams

---

## 7. Current state as of 2026-09-13

### OASIS2 — `Development` branch

**New providers sitting untracked (commit these when ready):**
- `Providers/Blockchain/AxelarOASIS/`
- `Providers/Blockchain/WormholeOASIS/`
- `Providers/Maps/GoogleMapsOASIS/`
- `Providers/Maps/HEREMapsOASIS/`
- `Providers/Maps/MapLibreOASIS/`
- `Providers/Maps/NianticLightshipOASIS/`
- `Providers/Network/CeramicOASIS/`
- `Providers/Network/CivicOASIS/`
- `Providers/Network/ReclaimProtocolOASIS/`
- `Providers/Storage/DenoDeployOASIS/`
- `Providers/Storage/FastlyOASIS/`

**Encryption work (3 commits, another session, details in memory):**
- AES-256-GCM quantum layer added on top of existing Rijndael-256 in `KeyManager.cs` + `WalletManager.cs`
- Holon MetaData encryption at rest in `HolonManager-Private.cs` / `HolonManager-Load.cs` (`__oasis_enc__` key)
- `DataEncryptionOverride` property on `Holon.cs` for per-call override
- 23 exempt MetaData keys that stay plain for provider queries
- New `HolonDataEncryption` block in `OASISDNA.cs` / `OASIS_DNA.json` (all-off defaults)

**ONET architecture fixes (on `master` only — NOT yet on Development):**
- Public key bootstrap, authenticated PING, Kademlia seeding, DataDirectory wiring, async deadlock fix
- These need cherry-picking or merging master → Development

**Unit test project (`ONODE/TestProjects/...UnitTests/`) — build broken:**
```bash
# Try this first:
rm -rf ONODE/TestProjects/NextGenSoftware.OASIS.API.ONODE.Core.UnitTests/obj
dotnet build ONODE/TestProjects/...
# If CS0579 duplicate assembly attribute errors persist, add this to the csproj:
# <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
```

**Backlog:**
- 7 C# files >1000 lines pending split (`OAPPManagerBase.cs`, `SOLIDOASIS.cs`, `TelegramBotService.cs` clearest)
- 96 annotated review verdicts awaiting David's check before deletion

### IDE — `main` branch — fully up to date

Latest commit: `b64eb1c`
- `npm run build` runs type-check + API drift check before compiling
- `.github/workflows/ci.yml` — CI on push to Development/master
- `Web7To10Panel` — surfaces Web7/8/9/10 bridge methods (CollectiveConsciousness, Symbiosis, MeshNodes, MeshStatus, SingularityStatus, GetSource)
- `tests/bridge-contract.test.ts` — Vitest: every `ipcRenderer.invoke` channel must have `ipcMain.handle`

**Action needed:** `npm install` on new machine to pull vitest, then `npm test`.

### OPORTAL-JS — fully up to date

Last commit: `6931bd6`. All 11 modals source-aware (live / API test data / demo).
New modals: OApp Launcher, Activity Feed, Holon Browser, Quest Builder, Global Search, Provider Health.

---

## 8. OASIS API layer ports

| Layer | Port | Notes |
|-------|------|-------|
| STAR WebAPI | :5005 | OAPPs, Holons, CelestialBodies, Quests, Zomes |
| Web4 / ONODE | :7777 | Avatar, NFT, Karma, HyperDrive, Providers |
| Web6 | :64596 | AI, A2A, FAHRN, Memory — swagger at `/swagger/v2/swagger.json` |
| Web7 | :62798 | Collective Consciousness, Symbiosis |
| Web8 | :65332 | Mesh Routing |
| Web9 | :65342 | Singularity — swagger at `/swagger/v2/swagger.json` |
| Web10 | :57483 | The Source |

---

## 9. HerzID integration — analysis done, no code written yet

HerzID = white-labelled OASIS Avatar SSO for HERZ mega app (Sarah Elahina, Enlightened Nations).
Format: `052·0·000·000·001·✦` = [3-digit country] + [10 sequential] + [1 QEA seal char]

Build items in priority order:
1. OIDC discovery endpoints on OASIS WebAPI (`/.well-known/openid-configuration`, JWKS, userinfo) — ~2–3 days
2. `HerzId`, `HerzCountryCode`, `HerzSequentialNumber` fields on Avatar
3. Atomic sequential counter microservice (NOT OASIS providers — race condition risk)
4. QEA seal: `HMAC-SHA256(sequential + country + QEA profile + Wiccian seed + join date)`
5. Voice biometrics — Azure Cognitive Services Speaker Recognition, stored encrypted in Avatar MetaData
6. Vouching graph — `VoucherId` + `VouchCount` on Avatar, ghost-account detection rules

Full detail in `C:\Users\David\.claude\projects\C--Source-OASIS2\memory\project_herzid_integration.md`.
