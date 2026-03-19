# STAR CLI: non-interactive & JSON shell mode

Run STAR from scripts and CI without menus or stdin prompts.

## Global flags (any position on the command line)

| Flag | Short | Purpose |
|------|-------|---------|
| `--non-interactive` | `-n` | No stdin prompts; wizards blocked; missing args → errors |
| `--json` | | Machine-readable JSON on stdout for some commands; implies quieter startup |
| `--quiet` | `-q` | Minimal banner (also set automatically with `-n` or `--json`) |
| `--yes` | `-y` | In non-interactive mode, `GetConfirmation` behaves as “yes” (use carefully for destructive flows) |
| `--username` | | Beam-in username (avoid in shared logs; prefer env vars) |
| `--password` | | Beam-in password |
| `--search-limit` | | Optional positive `N`: caps STARNET `search` rows and **ambiguous find** candidate lines when a name matches multiple holons. **Omit for unlimited** (all provider results; full candidate list in errors). Per-search trailing integer still overrides the global cap for that `search` only. |

## Exit codes

| Code | Meaning |
|------|---------|
| `0` | Success |
| `1` | General error / unknown command |
| `2` | Usage / missing credentials or subcommand in non-interactive mode |
| `3` | Interactive input was required but disabled (`CLIEngineNonInteractiveInputRequiredException`) |

## Beam-in (authentication)

Most commands need a beamed-in avatar.

### Commands that **skip** beam-in

No credentials required: `help`, `version`, `status`, `dna`, `ignite`, `extinguish`, `exit`.

### Otherwise

Set **environment variables** (recommended for CI):

- `STAR_CLI_USERNAME`
- `STAR_CLI_PASSWORD`

Optional if the API reports an unverified avatar:

- `STAR_CLI_EMAIL_VERIFY_TOKEN`

Or pass on the command line (less secure):

```bash
star --non-interactive --username MYUSER --password '***' oapp list
```

Or prefix the command with an **avatar beamin** sequence (credentials are stripped before the rest runs):

```bash
star --non-interactive avatar beamin MYUSER '***' oapp list
```

Beam-in **only** (no further command):

```bash
export STAR_CLI_USERNAME=...
export STAR_CLI_PASSWORD=...
star --non-interactive
```

## Examples

```bash
# Versions as JSON (no login)
star --json version

# Status
star -n status

# List OAPPs (requires credentials)
STAR_CLI_USERNAME=u STAR_CLI_PASSWORD=p star --non-interactive oapp list

# JSON + non-interactive
star --non-interactive --json version
```

## Interactive-only commands

These refuse `--non-interactive`: `bang`, `wiz`, and `light` without full positional arguments. Use explicit `light …` args or interactive mode.

## Subcommands

If you run only `oapp`, `holon`, etc. with **no** subcommand, interactive mode shows a help menu. In `--non-interactive` mode you get exit code **2** and an error: you must pass a full subcommand (e.g. `oapp list`, `holon show <id>`).

### Generic design: `STARNETUIBase` and minimal duplication

Most STARNET holon CLIs are **not** implemented per-type in `Program.cs`. They share:

| Layer | Role |
|--------|------|
| **`Program.ShowSubCommandAsync<T>`** | One generic router: parses argv, dispatches `create` / `show` / `list` / … to the same delegate shape for every holon type `T`. |
| **`StarCliStarnetNonInteractiveGuard`** (STAR.CLI) | Generic argv validation for verbs that need ids or block wizard-only flows before any `T`-specific code runs. |
| **`STARNETUIBase<T1,T2,T3,T4>.CreateAsync`** (STAR.CLI.Lib) | **Single** scripted-create implementation: reads `ISTARNETCreateOptions.CustomCreateParams` using **`StarCliNonInteractiveCreateKeys`**, then calls `STARNETManager.CreateAsync`. Any holon whose `CreateAsync` **reaches this base method** with those params gets scripted `--non-interactive` create without new CLI code. |
| **`StarnetUiScriptedCreateCli`** (STAR.CLI.Lib) | **Single** argv → `CustomCreateParams` mapper (`TryParseCreateArgv`, `BuildScriptedCustomCreateParams`) so parsing stays next to the base, not duplicated per holon. |
| **`StarnetUiScriptedCreateCli.HolonLabelBypassesBaseScriptedCreate`** | One explicit list of holon **labels** (the same `subCommand` string passed into `ShowSubCommandAsync`) whose **`CreateAsync` overrides do not delegate to the base scripted path** (or prompt *before* base). Add a label here only when introducing or discovering such an override; prefer refactoring the override to call `base.CreateAsync` with scripted options when feasible. |

**Implications for contributors**

- Prefer **generic** fixes in `STARNETUIBase`, `StarnetUiScriptedCreateCli`, or `ShowSubCommandAsync` over copy-pasted per-holon branches in `Program.cs`.
- **Plugin** create still bypasses the base scripted path; **NFT / GeoNFT** wrap + JSON/file argv for several verbs are wired (see below). **`library`** and **minimal `oapp` / `hApp`** scripted create are supported; the **full OAPP Light wizard** (geo, sprites, nested holons, etc.) remains interactive-only.

### STARNET holon verbs (`ShowSubCommandAsync` routes)

For entities such as **oapp**, **oapp template**, **nft**, **nft collection**, **geonft**, **geonft collection**, **runtime**, **holon**, etc., non-interactive mode enforces:

- **Target id or name** on the argv for: `show`, `update`, `delete`, `download`, `install`, `uninstall`, `publish`, `unpublish`, `republish`, `activate`, `deactivate`, `adddependency`, `removedependency`, and collection `add` / `remove` (collection id plus NFT id). Missing tokens → exit **2** with an example line.
- If you pass a **name** that matches **more than one** holon, non-interactive mode fails with a message that lists **GUID — name** for **all** matches by default. Pass **`--search-limit N`** to shorten that list (and search row caps) when output would be too large.
- **`search`:** criteria are the tokens after `search` on the pipeline. If the **last** token is a positive integer, it is **max result rows** for that invocation only; otherwise **`--search-limit N`** applies when set; otherwise **no cap** (all rows returned by the provider). An empty criteria string in `--non-interactive` → exit **2**. Examples: `oapp search billing` (unlimited), `holon search sensor 50` (cap 50), `star --search-limit 20 -n oapp search foo 10` (trailing `10` wins for that search).
- **NFT / GeoNFT structured argv** (non-interactive): `mint` / `burn` / `import` take an **existing JSON file path** (mint: `MintWeb4NFTRequest` / `IMintWeb4NFTRequest`; burn: `BurnWeb3NFTRequest`; import: same file contract as interactive). `export`: `<web4NftIdOrName> <destinationFilePath>`. `remint`: `<web4NftOrGeoIdOrName>`. `place` (GeoNFT): JSON path for `PlaceWeb4GeoSpatialNFTRequest`. `send`: `<fromWallet> <toWallet> <tokenAddress> <memo>`. Omit `--non-interactive` for interactive wizards where argv is incomplete.
- **Scripted web5 `create`** (STARNET holons that use `STARNETUIBase.CreateAsync`):  
  Default: `create <name> <description> <categoryEnum> [parentFolder]` after the entity segment.  
  **`library`:** `library create <name> <description> <categoryEnum> <languageEnum> [parentFolder]` where `languageEnum` is a `Languages` value (case-insensitive), e.g. `CSharp`.  
  **`oapp` / `hApp` (minimal):** same default argv; `categoryEnum` must be an **`OAPPType`** value (e.g. `OAPPTemplate`, `GeneratedCodeOnly`). This creates the STARNET folder/DNA via `base.CreateAsync` — **not** the full Light wizard.  
  **`geo-hotspot`:** positional argv documented on `StarnetUiScriptedCreateCli.TryParseGeoHotSpotCreateArgv` (name, description, type, lat, long, radius, trigger, optional time, optional parent folder).  
  **Wrap existing WEB4 asset:** `nft create <web4NftIdOrGuid>`, `geo-nft` / `geonft create <web4GeoSpatialNftIdOrGuid>`.  
  **`inventoryitem`:** scripted params skip image/object wizards when `CustomCreateParams` are supplied.  
  Examples: `runtime create MyRt "Desc" Console`, `oapp template create MyTpl "Desc" Console /tmp/oapp-templates`, `oapp create MyOapp "Desc" OAPPTemplate`, `library create MyLib "Desc" Console CSharp`.  
  If `parentFolder` is omitted, the STARDNA default source path is used (no path prompts).  
  **Still not argv-complete:** **full OAPP Light wizard**, **`plugin`** (bypasses base scripted create), **`clone` / `convert`** in non-interactive (exit **2** “not wired”), **collection** create and some GeoNFT **export** parity vs NFT — prefer interactive or APIs until extended.

Web4/Web3 variants: for `show` / `update` / `delete` with `web3` or `web4`, the id token is the argument **after** that keyword (same as interactive parsing).

## Engine behaviour (`NextGenSoftware.CLI.Engine`)

When `CLIEngine.NonInteractive` is true:

- `GetValidInput`, `ReadPassword`, enum/guid prompts, etc. throw `CLIEngineNonInteractiveInputRequiredException` instead of reading stdin.
- `GetConfirmation` returns `false` unless `CLIEngine.AssumeYes` is true (`--yes`).

STAR CLI sets these from the parsed invocation at startup.

## Further work

`STARNETUIBase.Find` / `FindAsync` throw `CLIEngineNonInteractiveInputRequiredException` when id/name is empty or search is ambiguous; ambiguous name matches include a **GUID — name** candidate list (size capped as above). Remaining gaps: **full OAPP Light** wizard on argv; **plugin** scripted create; **clone** / **convert** non-interactive; **collection** flows; optional **GeoNFT export** parity and **web3 import** argv contracts.

**Cursor / AI sessions:** If context was lost mid-task, use **`Docs/Devs/STAR_CLI_SessionHandoff.md`** (file map + next steps) and repo root **`AGENTS.md`**.
