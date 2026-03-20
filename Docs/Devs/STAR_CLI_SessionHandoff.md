# STAR CLI — AI session handoff

**Purpose:** When a Cursor/chat session hits context limits, open a **new chat** and point the model here so work continues without re-explaining the whole thread.

---

## Quick resume line (copy into a new chat)

> Continue STAR CLI **non-interactive / shell** work. Read `Docs/Devs/STAR_CLI_SessionHandoff.md` and `Docs/Devs/STAR_CLI_NonInteractive.md`. Build: `dotnet build "STAR ODK/NextGenSoftware.OASIS.STAR.CLI/NextGenSoftware.OASIS.STAR.CLI.csproj" -c Release`.

---

## Implemented (baseline)

### Global behaviour

- Flags parsed from **any** argv position: `--non-interactive` (`-n`), `--json`, `--quiet` (`-q`), `--yes` (`-y`), `--username`, `--password`, `--search-limit N` (sets `CLIEngine.MaxHolonSearchResults` when `N` &gt; 0).
- `NextGenSoftware.CLI.Engine`: `CLIEngine.NonInteractive`, `AssumeYes`, `JsonOutput`, `Quiet`; prompts throw `CLIEngineNonInteractiveInputRequiredException` or `GetConfirmation` respects `AssumeYes`.
- Beam-in: env `STAR_CLI_USERNAME` / `STAR_CLI_PASSWORD`; optional `STAR_CLI_EMAIL_VERIFY_TOKEN`; or prefix `avatar beamin <user> <pass>` (stripped before command).
- Commands that **skip** beam-in: `help`, `version`, `status`, `dna`, `ignite`, `extinguish`, `exit`.
- `ShowSubCommandAsync` **else** branch (no subcommand): in non-interactive → error exit **2**, no wizard menu.
- Interactive-only top-level commands blocked in non-interactive: `bang`, `wiz`; `light` without full args.
- JSON output for some top-level commands: `version`, `status`, `help` (pointer), unknown command errors.

### Key files

| File | Role |
|------|------|
| `STAR ODK/NextGenSoftware.OASIS.STAR.CLI/Program.cs` | `Main`, `ReadyPlayerOne`, `ShowSubCommandAsync`, command `switch` |
| `STAR ODK/NextGenSoftware.OASIS.STAR.CLI/StarCliStarnetNonInteractiveGuard.cs` | Non-interactive argv checks before STARNET predicates |
| `STAR ODK/NextGenSoftware.OASIS.STAR.CLI.Lib/StarnetUiScriptedCreateCli.cs` | Generic argv → `CustomCreateParams`; holon labels that bypass base scripted create |
| `STAR ODK/NextGenSoftware.OASIS.STAR.CLI.Lib/StarCliLightRequest.cs` | JSON schema for non-interactive **Light** (`oapp light` / `light <file.json>` / `light json` alias / `oapp create light`) |
| `STAR ODK/NextGenSoftware.OASIS.STAR.CLI.Lib/OAPPs.NonInteractiveLight.cs` | `OAPPs.LightFromJsonFileAsync` + STARNET registration after `STAR.LightAsync` |
| `STAR ODK/NextGenSoftware.OASIS.STAR.CLI.Lib/StarCliNonInteractiveCreateKeys.cs` | `CustomCreateParams` keys (consumed by `STARNETUIBase.CreateAsync`) |
| `STAR ODK/NextGenSoftware.OASIS.STAR.CLI/StarCliInvocation.cs` | Flag parsing (`--search-limit`), `CommandSkipsAvatarBeamIn`, `GetCommandArgsAfterOptionalAvatarBeamIn` |
| `STAR ODK/NextGenSoftware.OASIS.STAR.CLI/StarCliStarnetSearchArgv.cs` | `search` argv: criteria + optional trailing max rows |
| `STAR ODK/NextGenSoftware.OASIS.STAR.CLI/StarCliNftStructuredArgv.cs` | NFT / GeoNFT mint/burn/remint/place/send/export/import argv |
| `STAR ODK/NextGenSoftware.OASIS.STAR.CLI/StarCliShellOutput.cs` | JSON + exit codes |
| `STAR ODK/NextGenSoftware.OASIS.STAR.CLI.Lib/Avatars.cs` | `BeamInWithCredentialsAsync`; non-interactive guards on email/username loops |
| `NextGenSoftware-Libraries/NextGenSoftware.CLI.Engine/CLIEngine.cs` | Prompt behaviour; `MaxHolonSearchResults` |
| `NextGenSoftware-Libraries/NextGenSoftware.CLI.Engine/CLIEngineNonInteractiveInputRequiredException.cs` | Exception type |

### User docs

- `Docs/Devs/STAR_CLI_NonInteractive.md` — flags, exit codes, beam-in, limitations.

---

## Done recently

**`ShowSubCommandAsync` + `StarCliStarnetNonInteractiveGuard`:** In `--non-interactive`, holon subcommands that need a target require argv ids/names (exit **2** + example) for `show`/`update`/`delete`/install-family/publish-family/activate/deps/collection add-remove. **`search`:** `StarCliStarnetSearchArgv` + optional trailing **max rows**; merged with global `--search-limit` in `Program` → `STARNETUIBase.SearchAsync(..., maxResults)` (**unlimited** when both are unset). Ambiguous find: list **all** candidates unless `MaxHolonSearchResults` &gt; 0 (`--search-limit`).

**NFT / GeoNFT argv:** `StarCliNftStructuredArgv` + `Program`: **mint** / **burn** / **import**, **export**, **remint**, **place**, **send**, **`clone` / `convert`** first token after verb. **`STARNETUIBase.CloneAsync`** calls **`ISTARNETManagerBase.CloneAsync`** (ONODE `STARNETManagerBase`). **ConvertNFTAsync** / **ConvertGeoNFTAsync**: explicit `OASISResult` errors (no ONODE convert API wired). Lib: `GeoNFTs.BurnGeoNFTAsync` accepts **BurnWeb3NFTRequest** JSON like `NFTs`.

**`--json` + `-n` NFT/GeoNFT verbs:** `Program.EmitNiJsonForOasisResult` emits structured stdout for **mint**, **burn**, **import** (including web3-mint/web3-token), **export**, **remint**, **convert**, **place** (geo-nft JSON file), **send** (errors → exit **1**). **`BurnNFTAsync` / `BurnGeoNFTAsync`** now set **`OASISResult.Message`** / **`IsError`** on success/failure for consumers. **`PlaceGeoNFTFromJsonFileAsync`** returns **`OASISResult<IWeb4GeoSpatialNFT>`**; **`SendNFTAsync` / `SendGeoNFTAsync` (4-arg)** return **`OASISResult<ISendWeb4NFTResponse>`**.

**STARNET `clone` + `--json`:** `ShowSubCommandAsync` **`clonePredicate`** is **`Func<object, Task<OASISResult<T>>>`**; after **`CloneAsync`**, **`EmitNiJsonForOasisResult`** runs when **`CLIEngine.JsonOutput`**. **`Program`** registrations use each holon manager’s **`CloneAsync`** (not **`OAPPTemplates.CloneAsync`**) except **`oapp template`**. **`STARNETUIBase.CloneAsync`:** skip **`ShowSuccessMessage`** when **`JsonOutput`**; skip **`ShowAsync`** when **`NonInteractive`** or **`JsonOutput`**.

**`OAPPs.CreateAsync`:** **`star.cli.lightRequestJsonPath`** → **`LightFromJsonFileAsync`** (full **`STAR.LightAsync`** + STARNET OAPP record + runtimes + DNA refresh). If **`CLIEngine.NonInteractive`** and neither light JSON nor scripted **`star.cli.scriptedCreate`**, returns an **`OASISResult`** error instead of **LightWizardAsync**. **`oapp light <file>`** / **`happ light <file>`** (primary) and alias **`oapp create light <file>`**; top-level **`light <file>`** / **`light json <file>`**; **`--json`** uses labels **`OAPP light`** / **`light`** as wired in **`Program`**. **`nft import <file>`** auto-detects WEB3 mint vs token vs WEB4; legacy **`import web3-mint`** / **`web3-token`** remain.

**Scripted `create`:** `StarnetUiScriptedCreateCli` — **`geo-hotspot`**, **`nft` / `geo-nft`** wrap, **`nft collection` / `geo-nft collection`**: **wrap** = exactly 4 argv tokens (`… collection create <web4IdOrName>`); **new minimal WEB4 + WEB5 wrap** = 5 tokens (`… collection create <name> <description>`) via `CreateMinimalNftCollection` / `CreateMinimalGeoNftCollection` keys in **`StarCliNonInteractiveCreateKeys`**. **`GeoNFTCollections.CreateAsync`** handles **`--non-interactive`** wrap at the top (same pattern as **`NFTCollections`**). Default **`TryParseCreateArgv`** holons; **`plugin`** `TryParsePluginCreateArgv` → `Plugins.CreateAsync` scripted branch. **`HolonSubCommandLabelsThatBypassBaseScriptedCreate`** is empty; **`nft collection` / `geo-nft collection`** are excluded from the legacy “NFT substring” bypass. **`STARNETUIBase.Find` / `FindAsync`:** ambiguous name → full candidate list unless `--search-limit`.

**Wallet import:** **`wallet import <file.json>`** (existing path, `.json` extension) imports one wallet; **`wallet import all <file.json>`** bulk import; legacy **`wallet import json <file>`** / **`wallet import json all <file>`** unchanged.

**NFT import (non-interactive):** primary **`nft import <file>`** with JSON shape auto-detection (**`StarCliNftStructuredArgv.TryResolveNftNonInteractiveImport`**); legacy **`nft import web3-mint <file>`** / **`web3-token <file>`**. **`convert`:** explicit `OASISResult` error (no ONODE convert API wired).

**Build verified:** `dotnet build "STAR ODK/NextGenSoftware.OASIS.STAR.CLI/NextGenSoftware.OASIS.STAR.CLI.csproj" -c Release`.

## Not done / next priority

1. **NFT / GeoNFT convert** — requires a real **ONODE / NFTManager (or API) convert** implementation; CLI can then call it (today: clear `OASISResult` error only).

---

## Exit codes (reminder)

| Code | Meaning |
|------|---------|
| 0 | Success |
| 1 | General / unknown command |
| 2 | Usage / missing subcommand or credentials |
| 3 | Console input required but disabled |

---

## After you change code

```bash
dotnet build "STAR ODK/NextGenSoftware.OASIS.STAR.CLI/NextGenSoftware.OASIS.STAR.CLI.csproj" -c Release
```

Update **this handoff** when a milestone is finished (e.g. “oapp show/update/delete validated non-interactive”).
