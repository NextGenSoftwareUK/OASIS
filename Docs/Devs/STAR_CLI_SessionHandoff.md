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

**NFT / GeoNFT argv:** `StarCliNftStructuredArgv` + `Program` branches: **mint** / **burn** / **import** (JSON path), **export** (id + path), **remint** (id), **place** (Geo — JSON path), **send** (four tokens). Lib: `NFTs` / `GeoNFTs` scripted wrap, mint/burn/import/export helpers; `InventoryItems` / `GeoHotSpots` skip wizards when scripted params present.

**Scripted web5 `create`:** `StarnetUiScriptedCreateCli` — **`geo-hotspot`** full argv; **`nft` / `geo-nft` / `geonft`** wrap `create <web4Id>`; bypass list is mainly **`plugin`**. **`library`**, minimal **`oapp` / `hApp`**, default holons unchanged. **`STARNETUIBase.Find` / `FindAsync`:** ambiguous name → candidate list (size as above).

**Build verified:** `dotnet build "STAR ODK/NextGenSoftware.OASIS.STAR.CLI/NextGenSoftware.OASIS.STAR.CLI.csproj" -c Release` (fix: `--search-limit` parse + `send` NI branch definite assignment).

## Not done / next priority

1. **Full OAPP Light** wizard on argv.
2. **`plugin`** scripted create (still bypasses base path).
3. **`clone` / `convert`** non-interactive wiring.
4. **Collection** create and richer **GeoNFT export** / **web3 import** argv if needed.

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
