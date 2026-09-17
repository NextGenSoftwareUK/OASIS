# STAR CLI — Comprehensive guide (users & developers)

This document complements the existing STAR CLI docs by tying together **end-user workflows**, **developer extension points** (templates, DNA, provider native codegen), **STARNET store concepts**, **non-interactive automation**, and **AI/scripting integrations**. It is the recommended hub page; deeper references are linked below.

---

## 1. Documentation map

| Topic | Document |
|--------|-----------|
| **Implemented command inventory** (router + STARNET verbs, `help full` drift) | [STAR_CLI_Implemented_Commands.md](./STAR_CLI_Implemented_Commands.md) |
| Command cheat sheet (long holon tables) | [STAR_CLI_DOCUMENTATION.md](./STAR_CLI_DOCUMENTATION.md) |
| Fast onboarding | [STAR_CLI_QUICK_START_GUIDE.md](./STAR_CLI_QUICK_START_GUIDE.md) |
| Install per OS | [STAR_CLI_GettingStarted_Windows.md](./STAR_CLI_GettingStarted_Windows.md) · [Linux](./STAR_CLI_GettingStarted_Linux.md) · [Mac](./STAR_CLI_GettingStarted_Mac.md) |
| Packaging / installers | [STAR_CLI_INSTALLERS_AND_PACKAGING.md](./STAR_CLI_INSTALLERS_AND_PACKAGING.md) |
| **`--non-interactive`**, `--json`, scripted create, exit codes | [STAR_CLI_NonInteractive.md](./STAR_CLI_NonInteractive.md) |
| AI session handoff (file map, build command) | [STAR_CLI_SessionHandoff.md](./STAR_CLI_SessionHandoff.md) |
| DNA dependency JSON model | [DNA_SYSTEM_GUIDE.md](./DNA_SYSTEM_GUIDE.md) |
| STARNET App Store (UI / product view) | [STARNET_APP_STORE_GUIDE.md](./STARNET_APP_STORE_GUIDE.md) |
| STARNET assets / operations (broader) | [STARNET_ASSET_MANAGEMENT_GUIDE.md](./STARNET_ASSET_MANAGEMENT_GUIDE.md) |
| OAPP Builder UI | [STARNET_OAPP_BUILDER_UI_GUIDE.md](./STARNET_OAPP_BUILDER_UI_GUIDE.md) |
| WEB5 STAR API (HTTP surface for holons, templates, OAPPs, …) | [API Documentation/WEB5 STAR API/README.md](./API%20Documentation/WEB5%20STAR%20API/README.md) |
| Repo policy for agents (no fallback hacks) | [AGENT_Root_Cause_No_Fallbacks.md](./AGENT_Root_Cause_No_Fallbacks.md) |

---

## 2. For users — what STAR CLI is

STAR CLI is the **command-line front end** for OASIS **STAR ODK**: authenticate an avatar (“beam in”), manage **STARNET** holons (OAPPs, zomes, holons, NFTs, quests, …), run **Light** code generation from DNA + templates, and drive publishing workflows. Many teams also use it from **CI, scripts, and AI tools** via non-interactive mode.

**Typical flow (interactive)**

1. Build or install the CLI (see Getting Started guides).
2. Run `dotnet run` or the `star` executable; complete avatar / provider setup as prompted.
3. Use `help` or `help full` for command discovery.
4. Create or manage assets with holon commands (`oapp`, `holon`, `nft`, …) or run **`light`** / **`light wiz`** to generate an OAPP from DNA.

**Beam-in**

Most commands expect a logged-in avatar. Non-interactive flows use environment variables and flags — see [STAR_CLI_NonInteractive.md](./STAR_CLI_NonInteractive.md).

---

## 3. Top-level commands (shell router)

The interactive shell dispatches on the first token. The following reflect `NextGenSoftware.OASIS.STAR.CLI/Program.cs` (names are stable; **implementation status** varies — see §3.1).

For an **exhaustive, code-truth list** (including `game` ONODE verbs, `cosmic`, `happ`, and where `help full` diverges), see [STAR_CLI_Implemented_Commands.md](./STAR_CLI_Implemented_Commands.md).

| Command | Typical intent |
|---------|----------------|
| `ignite` | Start STAR / OASIS session in the CLI |
| `extinguish` | Shut down STAR session |
| `help` / `help full` | Short or long help |
| `version` | STAR ODK / related component versions |
| `status` | STAR ODK status |
| `dna` | DNA-related operations |
| `exit` | Leave the CLI |
| `light` | **Light** generation (positional args, `light wiz`, or JSON file in scripted mode) |
| `bang` | Large-scale / metaverse-oriented wizard (**interactive**; blocked in `-n`) |
| `wiz` | STAR ODK wizard (**interactive**; blocked in `-n`) |
| `flare`, `shine`, `dim`, `twinkle`, `dust`, `radiate`, `emit`, `reflect`, `evolve`, `mutate`, `love`, `burst`, `super`, `net` | Reserved / future UX; many currently show **“Coming soon…”** in the shell — do not rely on them for automation |
| `seed` | Wired to **publish** OAPP flow (`OAPPs.PublishAsync`) |
| `unseed` | Wired to **unpublish** (`OAPPs.UnpublishAsync`) |
| `gate` | Opens the OASIS portal URL in the default browser |
| `api` / `api oasis` | Opens the configured WEB4/STAR site in the browser |

### 3.1 STARNET holon namespaces (second-level commands)

These enter the generic **STARNET UI** router (`ShowSubCommandAsync`) with verbs such as `create`, `show`, `list`, `search`, `update`, `delete`, `publish`, `download`, `install`, … (exact set per holon type):

`oapp`, `happ`, `runtime`, `lib`, `celestialspace`, `celestialbody`, `zome`, `holon`, `chapter`, `mission`, `quest`, `game`, `nft`, `geonft`, `geohotspot`, `inventoryitem`, `plugin`, `avatar`, `karma`, `keys`, `wallet`, `map`, `seeds`, `data`, `oland`, `search`, `onode`, `hypernet`, `onet`, `config`, `cosmic`, `runcosmictests`, `runoasisapitests`

**OAPP** also supports nested `oapp template …` and publish helpers. For exhaustive verb tables, use **`help full`** in the CLI and [STAR_CLI_DOCUMENTATION.md](./STAR_CLI_DOCUMENTATION.md).

---

## 4. Wizards vs scripted flows

| Entry | Mode | Notes |
|--------|------|--------|
| `light wiz` | Interactive | Full Light wizard; **not** available with `--non-interactive` |
| `wiz` | Interactive | STAR ODK tailored OAPP wizard |
| `bang` | Interactive | Metaverse-scale generation wizard |
| `light <LightRequest.json>`, `light json <file>`, `oapp light <file>`, `happ light <file>`, `oapp create light <file>` | Scripted | Same core pipeline as the wizard: `STAR.LightAsync` + STARNET registration (see `OAPPs.LightFromJsonFileAsync`). Schema: `StarCliLightRequest` in `STAR ODK/NextGenSoftware.OASIS.STAR.CLI.Lib/StarCliLightRequest.cs` |
| `oapp create …` / other holons | Scripted (`-n`) | Generic argv → `CustomCreateParams` via `StarnetUiScriptedCreateCli` and `STARNETUIBase.CreateAsync` — see [STAR_CLI_NonInteractive.md](./STAR_CLI_NonInteractive.md) |

---

## 5. Low-code / templating engine (how **Light** works)

**Light** is the main **low-code generator**: it turns **Celestial Body DNA** (declarative descriptions of zomes, holons, fields, and optional star/planet/moon structure) into **generated C#** (and optionally **provider-native** artifacts).

### 5.1 High-level pipeline (`Star.LightInternalAsync`)

1. **Avatar / ignite**: Requires beamed-in avatar; may call `IgniteStarAsync` / inner star initialization.
2. **Paths**: Resolves `celestialBodyDNAFolder`, `genesisFolder`, `genesisNameSpace` from arguments or **STARDNA** defaults (`STARBasePath`, `OAPPMetaDataDNAFolder`, `DefaultOAPPsSourcePath`, `DefaultGenesisNamespace`, etc.).
3. **ValidateLightDNA**: Validates DNA inputs before generation.
4. **InitOAPPFolderAsync**: Prepares the OAPP folder (including **OAPP template** copy when `OAPPType` / template id/version call for it).
5. **C# template load**: Reads template files from `STARDNA.CSharpDNATemplateFolder` (relative to `STARBasePath` when not rooted), e.g.:
   - Holon / IHolon, Zome / IZome, CelestialBody / ICelestialBody shells
   - Load/save holon snippets
   - Primitive field snippets (`Int`, `String`, `Bool`)
6. **DNA parse**: Walks `CelestialBodyDNA` files; for each holon/zome, **string-substitutes** placeholders (`HolonDNATemplate`, namespaces, IDs, parent links) and emits `.cs` under `…/CSharp/Zomes`, `…/CSharp/Holons`, `…/CSharp/CelestialBodies`, interfaces, etc.
7. **ApplyOAPPTemplate**: Merges **application-level OAPP templates** (sample Program/Main, wiring) using meta holon/tag mappings when present.
8. **NativeCodeGenesis** (optional): If the **current storage provider** implements **`IOASISSuperStar`**, calls `NativeCodeGenesis(ICelestialBody, outputFolder, nativeParams)` so the **active provider** can emit chain-specific or DB-specific artifacts (see §7).
9. **Persistence**: Saves zomes/holons/celestial bodies through the OASIS stack (`SaveAsync`, `AddMoonAsync`, `AddPlanetAsync`, …) depending on `GenesisType`.

### 5.2 STARDNA configuration

**STARDNA** holds paths and template file names (C# DNA templates, meta folders, STARNET base path, generated code folder name, etc.). Extending the **C#** shape of generated holons/zomes usually means **adding or editing template files** and, if needed, new keys in STARDNA mapping to those files (see `Star.cs` for the list of template key names such as `CSharpTemplateHolonDNA`, `CSharpTemplateZomeDNA`, …).

### 5.3 Genesis types

- **`GenesisType.Star` / `Planet` / `Moon`**: Full celestial hierarchy rules (avatar level gates apply in code).
- **`GenesisType.ZomesAndHolonsOnly`**: Generates zomes/holons without a full celestial body package — used for lighter pipelines and supported by **non-interactive** JSON (`skipStarnetOappCreate` and related options on `StarCliLightRequest`).

---

## 6. Extending generation

### 6.1 OAPP templates (application scaffold)

OAPP templates are selected by **OAPP type**, **template id/version**, and **OAPPTemplateType** (e.g. Console) — see `StarCliLightRequest` and the interactive wizard. **Installing** templates from STARNET uses the same holon machinery as other STARNET assets (`oapp template …` commands / WEB5 Templates API).

**Repo template library:** `STAR ODK/STAR OAPP DNA Templates/README.md` lists **Blazor**, **Web MVC**, **Web API**, **Minimal API**, **Worker**, **WPF**, **WinForms**, **MAUI** (placeholder + README), **gRPC**, **GraphQL**, and use-case starters (**Blog**, **Forum**, **Landing**, **Admin dashboard**, **E-commerce**). Each folder includes `OasisHolonStarBootstrap.cs` (same holon/meta **CMS tags** as the Sample Console template) plus `_Shared/README.md` for the token table. New `OAPPTemplateType` values: `Blog`, `Forum`, `LandingPage`, `AdminDashboard`, `ECommerceStorefront`, `MinimalApi` (see `NextGenSoftware.OASIS.API.Core/Enums/OAPPTemplateType.cs`).

### 6.2 DNA templates (C# holon/zome/celestial bodies)

To customize **.NET** output:

1. Copy and edit files under the folder referenced by **`CSharpDNATemplateFolder`** (see STARDNA).
2. Keep placeholder names consistent with `Star.LightInternalAsync` (`HolonDNATemplate`, `NAMESPACE`, `ID`, load/save snippets, etc.) or update `Star.cs` to match your placeholders (developer change).

### 6.3 NativeCodeGenesis — provider-specific artifacts (`IOASISSuperStar`)

The interface in code is **`IOASISSuperStar`** (extends `IOASISStorageProvider`):

```csharp
bool NativeCodeGenesis(ICelestialBody celestialBody, string outputFolder, string nativeParams);
```

- **`celestialBody`**: Root celestial body for the OAPP when applicable; may be null for **ZomesAndHolonsOnly** flows — callers still invoke native codegen where configured.
- **`outputFolder`**: OAPP root (same tree **Light** populated with C#).
- **`nativeParams`**: Opaque string; **Light** currently passes JSON (e.g. `celestialBodyDNAFolder` for HoloOASIS).

**Provider examples in the repo** (each implements its own logic):

- **HoloOASIS**: Holochain DNA / zome **Rust** generation (comments in `Star.cs` note Rust moved out of STAR into `HoloOASIS.NativeCodeGenesis`).
- **MongoDB / SQLite / Neo4j / …**: Document or SQL-oriented scaffolding from metadata.
- **Blockchain providers** (Ethereum, Solana, Web3Core, TRON, EOSIO, …): Contract or program stubs with provider-specific headers.

To add a **new** backend:

1. Implement **`IOASISSuperStar.NativeCodeGenesis`** on your provider.
2. Register and select that provider as the **current storage provider** when running **Light** (STAR uses `ProviderManager.Instance.CurrentStorageProvider` for this hook).
3. Parse `nativeParams` or extend the JSON payload from `Star.cs` if you need extra flags (requires STAR ODK change).

*Naming note:* documentation sometimes says “ISuperSTAR”; the compile-time interface name is **`IOASISSuperStar`**.

---

## 7. STARNET asset & app store (CLI + platform)

**STARNET** is the shared namespace for versioned **holons** (OAPPs, templates, libraries, NFTs, quests, …) with **DNA** (dependencies, metadata). The **App Store** (web UI) is the storefront; the **CLI** and **WEB5 API** are the automation surfaces.

### 7.1 CLI operations users care about

- **List / search / show**: `oapp list`, `oapp search …`, `template list`, etc. (see [STAR_CLI_DOCUMENTATION.md](./STAR_CLI_DOCUMENTATION.md)).
- **Publish / unpublish**: Top-level **`seed` / `unseed`** map to OAPP publish/unpublish; holon-level **`publish` / `unpublish` / `republish`** apply across types.
- **Install / download**: Pull holons from STARNET into local STARNET folders for Light / dev.
- **Dependencies**: `adddependency` / `removedependency` — aligns with [DNA_SYSTEM_GUIDE.md](./DNA_SYSTEM_GUIDE.md).

### 7.2 Web and API

- UI guides: [STARNET_APP_STORE_GUIDE.md](./STARNET_APP_STORE_GUIDE.md), [STARNET_DASHBOARD_GUIDE.md](./STARNET_DASHBOARD_GUIDE.md), [STARNET_OAPP_BUILDER_UI_GUIDE.md](./STARNET_OAPP_BUILDER_UI_GUIDE.md).
- HTTP: **OAPPs**, **Templates**, **Holons**, **Zomes**, … under [WEB5 STAR API](./API%20Documentation/WEB5%20STAR%20API/README.md).

---

## 8. Non-interactive mode (scripting & AI)

Full detail: [STAR_CLI_NonInteractive.md](./STAR_CLI_NonInteractive.md).

**Summary**

- **`--non-interactive` (`-n`)**: No stdin prompts; wizards disabled unless replaced by JSON or full argv.
- **`--json`**: Structured stdout + stable errors for tooling; combines with `-n` for NFT/GeoNFT, `clone`, `light` JSON, etc.
- **`--yes` (`-y`)**: Auto-confirm where `GetConfirmation` is used (destructive flows — use carefully).
- **Beam-in**: `STAR_CLI_USERNAME`, `STAR_CLI_PASSWORD`, optional `STAR_CLI_EMAIL_VERIFY_TOKEN`, or `avatar beamin user pass` prefix.
- **Exit codes**: `0` success, `1` error, `2` usage, `3` interactive input required.

**Architecture for contributors:** Prefer extending **`STARNETUIBase`**, **`StarnetUiScriptedCreateCli`**, or **`ShowSubCommandAsync`** over duplicating per-holon logic in `Program.cs` (documented in STAR_CLI_NonInteractive.md).

---

## 9. STAR CLI + AI / MCP / automation — what you can build

- **Deterministic CI**: Provision OAPPs, run `light` from checked-in `LightRequest.json`, publish with `-n`, gate on exit codes.
- **LLM agents**: Natural language → generated **`StarCliLightRequest`** JSON or argv → `star -n --json …`; parse JSON result lines for id/name.
- **MCP servers**: Thin wrappers that expose `star` subcommands as tools (list holons, search, clone, mint with JSON files).
- **Idempotency**: Use `search` + explicit ids in scripts; handle ambiguous names (CLI lists matches; `--search-limit` caps noise).
- **Omniverse / games**: Same STAR backend; games use **STARAPIClient** / native `star_api`; CLI remains the **authoring** and **admin** surface.

Limitations called out in docs today include **NFT/GeoNFT convert** not argv-complete until ONODE exposes APIs — see [STAR_CLI_NonInteractive.md](./STAR_CLI_NonInteractive.md) “Further work”.

---

## 10. Source map (for developers)

| Area | Location |
|------|-----------|
| Shell loop, `help`, metaphor commands | `STAR ODK/NextGenSoftware.OASIS.STAR.CLI/Program.cs` |
| Invocation flags, argv helpers | `StarCliInvocation.cs`, `StarCliStarnetNonInteractiveGuard.cs`, `StarCliShellOutput.cs` |
| Generic holon CRUD / search | `STAR ODK/NextGenSoftware.OASIS.STAR.CLI.Lib/STARNETUIBase.cs` |
| Scripted `create` argv | `StarnetUiScriptedCreateCli.cs`, `StarCliNonInteractiveCreateKeys.cs` |
| Non-interactive Light | `OAPPs.NonInteractiveLight.cs`, `StarCliLightRequest.cs` |
| Light / templates / NativeCodeGenesis | `STAR ODK/NextGenSoftware.OASIS.STAR/Star.cs` |
| SuperSTAR hook | `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Interfaces/Providers/IOASISSuperStar.cs` |
| CLI engine behaviour | `NextGenSoftware-Libraries/NextGenSoftware.CLI.Engine/CLIEngine.cs` |

---

## 11. Changelog suggestion for this doc

When you add holon verbs, wire metaphor commands, or change `Light` / `NativeCodeGenesis`, update **§3**, **§5**, and [STAR_CLI_NonInteractive.md](./STAR_CLI_NonInteractive.md) together so scripted and interactive stories stay aligned.
