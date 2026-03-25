# OASIS — notes for AI / Cursor agents

## Root cause only — no hacks, fallbacks, or workarounds (all code)

Applies to **every change** in this repo — C#, C++, ZScript, HTTP/API, DB, scripts — not only packaging.

**Do not** paper over bugs with parallel code paths: legacy API “if new fails,” silent `catch`, duplicate “safety” updates, un-spec’d retries, or optional `dlsym` for symbols that are part of the shipped contract. That hides **broken invariants** and makes behaviour depend on which path ran.

**Do** identify the **one invariant** that should hold, fix **the layer that broke it** (await ordering, cache invalidation, correct id persisted, deploy artifact match), use **`OASISResult<T>`** / real errors, and add **tests or build checks** where they lock the invariant in.

**Deploy / native:** missing `star_api_*` symbol → rebuild STARAPIClient, run full `BUILD_ODOOM.sh`, verify exports in scripts — see **`Docs/Devs/ODOOM_UZDoom_Build_Sync.md`**.

Full policy (code patterns + build): **`Docs/Devs/AGENT_Root_Cause_No_Fallbacks.md`**. Read it before adding shims or “fallbacks.”

## If the chat “crashed” or context was truncated

Resume from **files in git**, not from chat history. Read:

| Topic | Doc / entry point |
|--------|-------------------|
| **Root cause vs hacks / fallbacks** (policy for agents) | `Docs/Devs/AGENT_Root_Cause_No_Fallbacks.md` |
| STAR CLI shell / `--non-interactive` / `--json` | `Docs/Devs/STAR_CLI_NonInteractive.md` |
| **Session handoff** (what’s done, what’s next, file map) | `Docs/Devs/STAR_CLI_SessionHandoff.md` |
| **ODOOM quest list + STAR** (CVars, scroll, do-not-break invariants) | `Docs/Devs/ODOOM_Quest_List_STAR.md` |
| **ODOOM vs UZDoom** (why HUD/timer/toggle edits seem ignored; copy step) | `Docs/Devs/ODOOM_UZDoom_Build_Sync.md` |
| **`star_api.so` / `star_api.h` drift** (undefined symbol at launch; fix deploy, not game shims) | `Docs/Devs/ODOOM_UZDoom_Build_Sync.md` (heading: STAR native library must match star_api.h) |

For a **new chat**, paste: goal + “see `Docs/Devs/STAR_CLI_SessionHandoff.md`”.

## Conventions (this repo)

- Prefer **real implementations**; avoid TODOs/placeholders for shipping paths.
- **No workaround-first coding** — see `Docs/Devs/AGENT_Root_Cause_No_Fallbacks.md` (section A: general code).
- OASIS APIs often use **`OASISResult<T>`** — keep that pattern for new surface area.
- **NextGenSoftware-Libraries** lives as a **sibling** of this repo (e.g. `../NextGenSoftware-Libraries`), not under `OASIS/external-libs`.

## High-churn STAR CLI paths

- `STAR ODK/NextGenSoftware.OASIS.STAR.CLI/Program.cs` — command router (very large).
- `STAR ODK/NextGenSoftware.OASIS.STAR.CLI/StarCliInvocation.cs` — global flags.
- `STAR ODK/NextGenSoftware.OASIS.STAR.CLI/StarCliShellOutput.cs` — JSON/errors.
- `STAR ODK/NextGenSoftware.OASIS.STAR.CLI/StarCliStarnetNonInteractiveGuard.cs` — argv checks before `ShowSubCommandAsync` predicates.
- `STAR ODK/NextGenSoftware.OASIS.STAR.CLI.Lib/StarnetUiScriptedCreateCli.cs` — generic argv → `CustomCreateParams` for scripted create; `StarCliNonInteractiveCreateKeys.cs` + `STARNETUIBase.CreateAsync` consume it. Prefer extending these over per-holon `Program.cs` forks (see `Docs/Devs/STAR_CLI_NonInteractive.md` § Generic design).
- `STAR ODK/NextGenSoftware.OASIS.STAR.CLI.Lib/STARNETUIBase.cs` — wizards / `GetConfirmation` (non-interactive work often lands here).
- Shared prompts: `NextGenSoftware-Libraries/NextGenSoftware.CLI.Engine/CLIEngine.cs` (`NonInteractive`, `AssumeYes`, etc.).
