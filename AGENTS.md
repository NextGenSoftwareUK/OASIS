# OASIS — notes for AI / Cursor agents

## Root cause only — no hacks, fallbacks, or workarounds (all code)

Applies to **every change** in this repo — C#, C++, ZScript, HTTP/API, DB, scripts — not only packaging.

**Do not** paper over bugs with parallel code paths: legacy API “if new fails,” silent `catch`, duplicate “safety” updates, un-spec’d retries, or optional `dlsym` for symbols that are part of the shipped contract. That hides **broken invariants** and makes behaviour depend on which path ran.

**Do** identify the **one invariant** that should hold, fix **the layer that broke it** (await ordering, cache invalidation, correct id persisted, deploy artifact match), use **`OASISResult<T>`** / real errors, and add **tests or build checks** where they lock the invariant in.

**Deploy / native:** missing `star_api_*` symbol → rebuild STARAPIClient, run full `BUILD_ODOOM.sh`, verify exports in scripts — see **`OASIS Omniverse/Docs/ODOOM_UZDoom_Build_Sync.md`**.

Full policy (code patterns + build): **`Docs/Devs/AGENT_Root_Cause_No_Fallbacks.md`**. Read it before adding shims or “fallbacks.”

## If the chat “crashed” or context was truncated

Resume from **files in git**, not from chat history. Read:

| Topic | Doc / entry point |
|--------|-------------------|
| **Root cause vs hacks / fallbacks** (policy for agents) | `Docs/Devs/AGENT_Root_Cause_No_Fallbacks.md` |
| STAR CLI shell / `--non-interactive` / `--json` | `Docs/Devs/STAR_CLI_NonInteractive.md` |
| STAR CLI hub (users & devs, Light/templating, STARNET, AI/MCP) | `Docs/Devs/STAR_CLI_Comprehensive_Guide.md` |
| **Session handoff** (what’s done, what’s next, file map) | `Docs/Devs/STAR_CLI_SessionHandoff.md` |
| **ODOOM quest list + STAR** (CVars, scroll, do-not-break invariants) | `OASIS Omniverse/Docs/ODOOM_Quest_List_STAR.md` |
| **ODOOM vs UZDoom** (why HUD/timer/toggle edits seem ignored; copy step) | `OASIS Omniverse/Docs/ODOOM_UZDoom_Build_Sync.md` |
| **`star_api.so` / `star_api.h` drift** (undefined symbol at launch; fix deploy, not game shims) | `OASIS Omniverse/Docs/ODOOM_UZDoom_Build_Sync.md` (heading: STAR native library must match star_api.h) |
| **STAR `star_transport` remote vs native** (size, AOT, BootLoader, split-build recommendation) | `OASIS Omniverse/Docs/STAR_API_Native_Transport_Architecture.md` |
| **STAR Quest system** (WEB5 API, STARAPIClient, `star_api_*`, game hooks) | `OASIS Omniverse/Docs/STAR_Quest_System_Developer_Guide.md` |
| **OQuake / ODOOM STAR user guide** (beam-in, inventory, quest keys) | `OASIS Omniverse/Docs/STAR_Games_User_Guide.md` |

For a **new chat**, paste: goal + “see `Docs/Devs/STAR_CLI_SessionHandoff.md`”.

## Conventions (this repo)

- **Doc locations:** transient documents — progress reports, audits, session handoffs, status/completeness reports, evidence logs — go in `Docs/WIP/`. Only official, lasting docs (guides, policies, architecture, process runbooks) belong in the `Docs` root or `Docs/Devs`.

- Commit ongoing work on the agreed task branch, not directly on `Development`. Batch verified merges/pushes to `Development` once or twice daily rather than pushing every small commit: each dev push triggers Railway builds and frequent pushes can create hundreds of queued deployments. This cadence supersedes the earlier frequent-integration instruction. Do not wait for the entire provider programme to finish or allow task branches to diverge unchecked. Fetch and inspect current shared branch/submodule state first, preserve concurrent contributors' changes, and integrate only the scoped batch with its verification evidence and documented remaining gaps. Incomplete or unverified changes must not be swept into a deployment merely to synchronize branches. Coordinate branch switches in shared checkouts; never force-push shared branches or create another worktree/environment just for this workflow.
- Reuse the existing task worktree and one reusable provider-test environment. Do not create repeated fresh worktrees, runtime installs or per-run database copies.
- Cache pinned runtime executables once; retain compact test reports, but remove disposable test databases and generated outputs after owned processes stop. Validate exact cleanup paths before removal. Never delete source, credentials, user databases or unrelated environments.
- Bound .NET build/test workers with `/m:2 /nr:false /p:UseSharedCompilation=false` unless a measured requirement warrants more. Avoid retaining dozens of idle MSBuild nodes and inflating the Windows pagefile. Shut down owned idle build servers only after confirming no active build/test operation.
- Serialize producer/consumer builds that share Core, Shared or provider outputs with `Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1 -Owner <task-label> -Operation { ... }`. Wrap an entire reusable provider runner or one build batch once; never nest leases. Acquire before touching shared build outputs. A busy lease must stop the operation; do not bypass it with manual DLL copies or another build path. Cross-session messages and process scans are not mutual exclusion. Tests using already-frozen private outputs with `--no-build` may run separately when no other operation mutates those outputs. Verify the lease contract with `Scripts/TestHosts/Test-ProviderBuildLease.ps1`.

- Prefer **real implementations**; avoid TODOs/placeholders for shipping paths.
- **No workaround-first coding** — see `Docs/Devs/AGENT_Root_Cause_No_Fallbacks.md` (section A: general code).
- OASIS APIs often use **`OASISResult<T>`** — keep that pattern for new surface area.
- **NextGenSoftware-Libraries** lives as a **sibling** of this repo (e.g. `../NextGenSoftware-Libraries`), not under `OASIS/external-libs`.

## Railway WEB4-WEB10 dependency policy

- `Docker/oasis-dependency-versions.env` is the single source of deployed dependency SHAs. Never duplicate commit pins in individual WEB4-WEB10 Dockerfiles or clone moving branch tips.
- When advancing a deployed private submodule, update its parent gitlink and manifest value together, then run `python3 Scripts/validate_railway_dependency_manifest.py --require-gitlinks` and publish the affected services.
- CI enforces Dockerfile usage and exact manifest/gitlink equality on both deployment branches, `Development` and `master`. The legacy parent `main` branch has no WEB4-WEB10 Dockerfiles, so this policy is not applicable there.
- Full workflow: `Docs/Devs/RAILWAY_DEPENDENCY_PINS.md`.
- Development-to-production release sequence: `Docs/Devs/DEVELOPMENT_TO_MASTER_PROMOTION.md`.
- Stale branch cleanup (parent + submodules): `Scripts/branch_cleanup.sh` (dry run by default), process in `Docs/Devs/BRANCH_CLEANUP_RUNBOOK.md`.
- Normal developer release checklist: `Docs/Devs/MERGE_DEVELOPMENT_TO_MASTER_QUICK_START.md`.

## High-churn STAR CLI paths

- `STAR ODK/NextGenSoftware.OASIS.STAR.CLI/Program.cs` — command router (very large).
- `STAR ODK/NextGenSoftware.OASIS.STAR.CLI/StarCliInvocation.cs` — global flags.
- `STAR ODK/NextGenSoftware.OASIS.STAR.CLI/StarCliShellOutput.cs` — JSON/errors.
- `STAR ODK/NextGenSoftware.OASIS.STAR.CLI/StarCliStarnetNonInteractiveGuard.cs` — argv checks before `ShowSubCommandAsync` predicates.
- `STAR ODK/NextGenSoftware.OASIS.STAR.CLI.Lib/StarnetUiScriptedCreateCli.cs` — generic argv → `CustomCreateParams` for scripted create; `StarCliNonInteractiveCreateKeys.cs` + `STARNETUIBase.CreateAsync` consume it. Prefer extending these over per-holon `Program.cs` forks (see `Docs/Devs/STAR_CLI_NonInteractive.md` § Generic design).
- `STAR ODK/NextGenSoftware.OASIS.STAR.CLI.Lib/STARNETUIBase.cs` — wizards / `GetConfirmation` (non-interactive work often lands here).
- Shared prompts: `NextGenSoftware-Libraries/NextGenSoftware.CLI.Engine/CLIEngine.cs` (`NonInteractive`, `AssumeYes`, etc.).
