# OASIS — notes for AI / Cursor agents

## If the chat “crashed” or context was truncated

Resume from **files in git**, not from chat history. Read:

| Topic | Doc / entry point |
|--------|-------------------|
| STAR CLI shell / `--non-interactive` / `--json` | `Docs/Devs/STAR_CLI_NonInteractive.md` |
| **Session handoff** (what’s done, what’s next, file map) | `Docs/Devs/STAR_CLI_SessionHandoff.md` |

For a **new chat**, paste: goal + “see `Docs/Devs/STAR_CLI_SessionHandoff.md`”.

## Conventions (this repo)

- Prefer **real implementations**; avoid TODOs/placeholders for shipping paths.
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
