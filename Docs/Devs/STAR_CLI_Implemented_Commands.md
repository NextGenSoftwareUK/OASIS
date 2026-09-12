# STAR CLI — Implemented command surface (code-truth)

This document lists what the STAR CLI **actually parses and dispatches** in `STAR ODK/NextGenSoftware.OASIS.STAR.CLI/Program.cs`, plus the standard STARNET verb set emitted by `DisplaySTARNETHolonCommands` / consumed by `ShowSubCommandAsync`. It is the **implementation-grounded** counterpart to narrative docs; use it when you need an exhaustive inventory, not examples.

**Primary sources**

- Shell router: `switch (inputArgs[0].ToLower())` in `Program.cs` (top-level token after global flags).
- Human-readable catalog: `ShowCommands(bool showFullCommands)` — `help` → `ShowCommands(false)`, `help full` → `ShowCommands(true)`.

**How to refresh**

1. Diff `Program.cs` for new `case` labels and submenu handlers.
2. Optionally run `help` / `help full` in the CLI; long help should match §5–§7 below except where §10 notes drift.

---

## 1. Invocation pattern

- `star {SUBCOMMAND} …`
- Global automation flags may appear anywhere: `--non-interactive` (`-n`), `--json`, `--quiet` (`-q`), `--yes` (`-y`), `--username`, `--password` (see [STAR_CLI_NonInteractive.md](./STAR_CLI_NonInteractive.md)).

---

## 2. Top-level commands (shell router)

Every token below is a **real** `case` in `Program.cs`. Sub-behavior is summarized; deep wizards and STARNET handlers live in `STARNETUIBase` / `ShowSubCommandAsync`.

| Token | Router behavior (summary) |
|--------|----------------------------|
| `ignite` | Ignite STAR if not already ignited. |
| `extinguish` | Extinguish STAR if ignited. |
| `help` | `help` short list; `help full` → full list from `ShowCommands(true)`. JSON mode returns a short machine-oriented hint. |
| `version` | Print or JSON-emit version fields. |
| `status` | STAR/COSMIC/runtime status strings + DNA paths (non-JSON). |
| `dna` | Show DNA paths. |
| `exit` | Exit (confirmation unless non-interactive). |
| `light` | Light generation: wizard, JSON file, or positional args (see §3). |
| `bang` | Interactive-only metaverse prompt; **blocked** with `--non-interactive`; body is largely incomplete (no full pipeline after enum pick). |
| `wiz` | Interactive-only OAPP wizard; **blocked** with `--non-interactive`; implementation incomplete (generation not wired through). |
| `flare`, `shine`, `dim`, `twinkle`, `dust`, `radiate`, `emit`, `reflect`, `evolve`, `mutate`, `love`, `burst`, `super`, `net` | **Stub:** prints “Coming soon…” (or equivalent). |
| `seed` | `OAPPs.PublishAsync()` (no id in this top-level path). |
| `unseed` | `OAPPs.UnpublishAsync()`. |
| `gate` | Opens OASIS portal URL in the default browser. |
| `api` | Opens configured site; optional second token `oasis` (see code for URL). |
| `oapp` | STARNET OAPP router; special cases `oapp publish`, `oapp template` (see §4). |
| `happ` | Same overall shape as `oapp` (hApp), including `happ publish` (see §4). |
| `runtime`, `lib`, `celestialspace`, `celestialbody`, `zome`, `holon`, `chapter`, `mission`, `quest` | Generic STARNET holon router (§5). |
| `game` | ONODE `GameManager` commands **or** STARNET `Game` holon router (§6). |
| `nft` | NFT STARNET router + `nft collection` subtree (§7). |
| `geonft` | GeoNFT STARNET router + `geonft collection` subtree (§7). |
| `geohotspot` | STARNET router (internal label `geo-hotspot`). |
| `inventoryitem`, `plugin` | STARNET routers. |
| `avatar`, `karma`, `keys`, `wallet`, `map`, `seeds`, `data`, `oland` | Dedicated submenu implementations in `Program.cs`. |
| `search` | **Stub:** “Coming soon…” (top-level `search` only). |
| `onode` | ONODE / local API processes (§8). |
| `hypernet` | Subcommands exist; each shows **“Coming soon…”** in the switch. |
| `onet` | ONET network operations (§9). |
| `config` | DNA paths + toggles (§10). |
| `cosmic` | COSMIC wizards / scenarios (§11). |
| `runcosmictests` | COSMIC test harness with optional OAPP type / folders. |
| `runoasisapitests` | OASIS API tests. |

---

## 3. `light` (implemented branches)

- **`light`** (no further args, interactive): prints parameter help and may start `LightWizardAsync` after confirmation. In `--non-interactive`, errors with guidance to use JSON or full positional args.
- **`light wiz`**: Light wizard; **blocked** in `--non-interactive`.
- **`light <file.json>`** (single arg, extension `.json`, file must exist): `LightFromJsonFileAsync`.
- **`light json <file>`**: same JSON path as above.
- **Positional Light** (multiple args): only entered when the parser validates `OAPPType` on `inputArgs[3]` and follows the `STAR.LightAsync(...)` call chain in `Program.cs` (ordering is strict; see TODO in source).

**Not implemented in the router:** `light transmute` appears only in `help full` text (`DisplayCommand`); there is **no** matching branch under `case "light"`.

---

## 4. `oapp` and `happ`

- **`oapp publish`**: Optional path + optional `dotnetpublish`; non-interactive requires path (see `StarCliShellOutput` error text in `Program.cs`).
- **`oapp template`**: Full STARNET template holon verb set (§5).
- **Any other `oapp …`**: Routed as STARNET `OAPP` holon (§5).

- **`happ`**: Parallel structure: `happ publish` plus default `ShowSubCommandAsync` for the hApp/OAPP holon type.

---

## 5. Generic STARNET holon verbs

For each holon namespace listed in §2 (and `oapp template`, `nft collection`, `geonft collection`), the CLI accepts the same **verb set** (exact spelling as typed by users — typically lowercase in argv):

`create`, `update`, `clone`, `adddependency`, `removedependency`, `delete`, `publish`, `unpublish`, `republish`, `activate`, `deactivate`, `download`, `install`, `uninstall`, `reinstall`, `show`, `list`, `list installed`, `list uninstalled`, `list unpublished`, `list deactivated`, `search`

**Optional NFT / collection modifiers** (appended per `DisplaySTARNETHolonCommands` for `nft`, `nft collection`, `geonft`, `geonft collection`): `[web3] [web4]` on the generated help lines for create/update/delete/show/list/search (behavior is holon-specific inside `ShowSubCommandAsync`).

**`oapp` help-text shortcuts** (from `DisplaySTARNETHolonCommands("oapp", …)`): `create` / `publish` / `unpublish` / `republish` descriptions reference Light / seed / unseed / re-seed wording; behavior is still the STARNET delegates passed into `ShowSubCommandAsync`.

**Namespaces included in `help full` holon tables:** `oapp`, `oapp template`, `runtime`, `lib`, `celestialspace`, `celestialbody`, `zome`, `holon`, `chapter`, `mission`, `quest`, `nft`, `nft collection`, `geonft`, `geonft collection`, `geohotspot`, `inventoryitem`, `plugin`.

**Router-present but omitted from `help full` holon tables:** `happ` (same verb set as `oapp` / hApp holon).

---

## 6. `game` — ONODE runtime commands (checked before STARNET)

If the second token matches one of these, the **GameManager** path runs; otherwise argv falls through to `ShowSubCommandAsync<Game>` (§5).

**Session** (`game <cmd> <gameId>`): `start`, `end`, `load`, `unload`

**Level** (`game <cmd> <gameId> <level>` …): `loadlevel`, `unloadlevel`, `jumptolevel`; `jumptopoint` also requires `<x> <y> <z>`

**Area:** `loadarea <gameId> <x> <y> <z> <radius>`, `unloadarea <gameId> <areaId>`, `jumptoarea <gameId> <x> <y> <z>`

**UI** (`game <cmd> <gameId>`): `showtitlescreen`, `showmainmenu`, `showoptions`, `showcredits`

**Audio:** `setmastervolume|setvoicevolume|setsoundvolume <gameId> <0.0–1.0>`; `getmastervolume|getvoicevolume|getsoundvolume <gameId>`

**Video:** `setvideosetting <gameId> <Low|Medium|High|Custom>`; `getvideosetting <gameId>`

**Input:** `bindkeys` — stub message (“coming soon”).

**Inventory** (`game inventory …`): `list`; `add` (stub message); `remove <itemId>`; `has <itemId>`; `hasbyname <name>`

---

## 7. `nft` and `geonft` extra verbs (before generic STARNET table)

From `ShowCommands(true)` / router:

- **NFT:** `nft mint`, `nft burn`, `nft send`, `nft import`, `nft export`, `nft convert`, **`nft remint`** (wired in `ShowSubCommandAsync`; not duplicated in `ShowCommands(true)` `DisplayCommand` lines), then generic `nft` verbs (§5); `nft collection add|remove`, then generic `nft collection` verbs.
- **GeoNFT:** `geonft mint`, `geonft burn`, `geonft place`, `geonft send`, `geonft import`, `geonft export`, **`geonft remint`** (same note as NFT), then generic `geonft` verbs; `geonft collection add|remove`, then generic `geonft collection` verbs.

---

## 8. `onode` subcommands

`start [web4|web5]`, `stop [web4|web5]`, `status`, `config [web4|web5]`, `providers`, `startprovider {name}`, `stopprovider {name}` — see `ShowONODEMenuAsync`. Help text in `Program.cs` describes WEB4/WEB5 process spawning vs experimental ONET service.

---

## 9. `onet` subcommands

Implemented switches: `start [web4|web5|default]`, `stop [web4|web5|default]`, `status`, `providers`, `discover`, `connect {address}`, `disconnect {address}`, `topology`.

The zero-argument help menu in code comments out `start`/`stop` lines but the **router still implements** them.

---

## 10. `config` subcommands

- `config` / `config dna`: DNA path display (`ShowDNAPaths`).
- `config cosmicdetailedoutput [enabled|disabled|status]` — **code uses `enabled` / `disabled`, not `enable` / `disable`** (help text in `ShowCommands` says enable/disable; treat CLI tokens as **enabled/disabled**).
- `config starstatusdetailedoutput [enabled|disabled|status]` — same pattern (note: toggles `STAR.IsDetailedCOSMICOutputsEnabled` in current source).
- `config logproviderswitching [enabled|disabled|status]` — implemented; **not** listed in `help full`’s `DisplayCommand` block.

---

## 11. `cosmic` subtree

Top-level second token:

- **`body` / `celestialbody`:** `create|add`, `read|show|get`, `update|edit`, `delete|remove`, `list`, `search|find`
- **`space` / `celestialspace`:** same verb aliases as body
- **`find`:** optional id/name tail → `COSMIC.FindAsync`
- **`scenarios` / `scenario` / `createscenario` / `createusecase` / `createcommonusecase`:** `universe`, `multiverse`, `galaxy`, `solarsystem`, `planet`, `star` (each with `create…` aliases per switch)
- **`simulation`:** `propose`; `list`; `list proposals [onlymine]`
- **`magicverse` / `listmagicverse`:** list MagicVerse wizard

---

## 12. Other subsystems (as in `help full`)

The following match `DisplayCommand` / submenu code in `Program.cs` and are **implemented as more than “Command unknown”** (exact parameters are in `help full` or the `Show*SubCommandAsync` helpers):

- **Avatar:** `beamin`, `beamout`, `whoisbeamedin`, `show me`, `show`, `edit`, `list`, `search`, `forgotpassword`, `resetpassword`
- **Karma:** `list`
- **Keys:** `link privateKey|publicKey|genKeyPair`, `generateKeyPair`, `clearCache`, `get provideruniquestoragekey|providerpublickeys|avataridforprovideruniquestoragekey`, `list`
- **Wallet:** `sendtoken`, `transfer`, `get`, `getDefault`, `setDefault`, `import` variants (`privateKey`, `publicKey`, `secretPhase`, JSON file, `all`, `json` alias), `add`, `list`, `balance`
- **Map:** `setprovider`, `draw3dobject`, `draw2dsprite`, `draw2dspriteonhud`, `placeHolon|placeBuilding|placeQuest|placeGeoNFT|placeGeoHotSpot|placeOAPP`, pan/zoom/route subcommands as in `help full`
- **Seeds:** `balance`, `organisations`, `organisation`, `pay`, `donate`, `reward`, `invite`, `accept`, `qrcode`
- **Data:** `save`, `load`, `delete`, `list`
- **Oland:** `price`, `purchase`, `load`, `save`, `delete`, `list`
- **Tests:** `runcosmictests`, `runoasisapitests`

---

## 13. `help full` vs router drift (intentional checklist)

| Item | Notes |
|------|--------|
| `reseed` | Listed in `help full`; **no** top-level `case "reseed"` — use holon `republish` / `oapp republish` / `seed` flows. |
| `light transmute` | Listed in `help full`; **no** router branch. |
| `search` (top-level) | Listed in `help full`; router prints **Coming soon**. |
| `happ` | Routed like `oapp`; **omitted** from `DisplaySTARNETHolonCommands` in `ShowCommands(true)`. |
| `game …` ONODE verbs | Implemented (§6); **not** enumerated in `help full`. |
| `cosmic …` | Full tree (§11); **not** in `help full`. |
| `config logproviderswitching` | Implemented (§10); **not** in `help full` `DisplayCommand` list. |
| `nft remint` / `geonft remint` | Implemented in `ShowSubCommandAsync`; **not** in `help full` `DisplayCommand` list (inline holon help may still mention `remint`). |
| `config cosmicdetailedoutput` tokens | Help says enable/disable; parser expects **enabled/disabled**. |
| `hypernet` | Help describes service; switch body is **Coming soon** for each subcommand. |

---

## 14. Related documentation

- Hub: [STAR_CLI_Comprehensive_Guide.md](./STAR_CLI_Comprehensive_Guide.md)
- Non-interactive contracts: [STAR_CLI_NonInteractive.md](./STAR_CLI_NonInteractive.md)
- Long narrative / examples (may include aspirational examples): [STAR_CLI_DOCUMENTATION.md](./STAR_CLI_DOCUMENTATION.md)
