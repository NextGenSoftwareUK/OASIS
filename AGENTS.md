# OASIS — notes for AI / Cursor agents

## Owner actions — one David list

Every agent/session must add actions requiring David's accounts, access, devices,
approval, funding or decisions to `Docs/WIP/Release and Status/ACTIONS-FOR-DAVID.md`
in its assigned worktree before asking him, or in the same turn. Include context,
concrete steps, reason, status/date and links; never secrets. Update the existing
entry on his answer or verified completion. Preserve deferred/declined decisions,
do not re-ask or create competing lists, and keep engineering/tool failures as
agent-owned work. Preserve other agents' entries and integrate named doc changes
through the normal branch workflow.

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
| **API versions WEB4-WEB10** (one `advance_api_versions` switch in the global release) | `Docs/Devs/GLOBAL_RELEASE_AUTOMATION.md` |
| **Long-running agent work** (one worktree per agent, model/effort, cost) | `Docs/Devs/AGENT_Long_Running_Work_Guide.md` |
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

- Commit ongoing work on the agreed task branch, not directly on `Development`. Batch verified merges/pushes to `Development` once or twice daily rather than pushing every small commit: each dev push triggers Railway builds and frequent pushes can create hundreds of queued deployments. For COSMIC/CQL and HyperDrive provider coordination, review Development and the HyperDrive provider branch every 30 minutes and integrate any verified, non-overlapping batch. This exception does not authorize unverified deployment or empty pushes. Do not wait for the entire provider programme to finish or allow task branches to diverge unchecked. Fetch and inspect current shared branch/submodule state first, preserve concurrent contributors' changes, and integrate only the scoped batch with its verification evidence and documented remaining gaps. Incomplete or unverified changes must not be swept into a deployment merely to synchronize branches. Coordinate branch switches in shared checkouts; never force-push shared branches or create another worktree/environment just for this workflow.
- **Releases to master promote a snapshot, not `Development`.** Other agents push to `Development` continuously, so cut `release/YYYY-MM-DD` from one chosen commit, tag it `release-YYYY-MM-DD`, and run **Promote Development to master** with `source_ref=release/YYYY-MM-DD`. Fix blockers on the release branch, merge it back afterwards, never move it. Procedure: `Docs/Devs/DEVELOPMENT_TO_MASTER_PROMOTION.md` (section "Release branch snapshot").
- Reuse the existing task worktree and one reusable provider-test environment. Do not create repeated fresh worktrees, runtime installs or per-run database copies. Exception: one long-lived worktree per concurrent agent is allowed (see `Docs/Devs/AGENT_Long_Running_Work_Guide.md`).
- COSMIC ORM / CQL / HyperDrive uses one feature branch, `work/cosmic-cql-ton-public`. Use that branch for implementation, fixes, and handoffs. Integrate scoped, verified batches into `Development` as needed to verify the live WEB4 sandbox, at most once an hour under normal conditions. Critical fixes may go sooner. This supersedes the general integration cadence above for COSMIC work. Confirm the deployed revision and run the relevant sandbox checks before recording live acceptance.
- Cache pinned runtime executables once; retain compact test reports, but remove disposable test databases and generated outputs after owned processes stop. Validate exact cleanup paths before removal. Never delete source, credentials, user databases or unrelated environments.
- Bound .NET build/test workers with `/m:2 /nr:false /p:UseSharedCompilation=false` unless a measured requirement warrants more. Avoid retaining dozens of idle MSBuild nodes and inflating the Windows pagefile. Shut down owned idle build servers only after confirming no active build/test operation.
- Serialize producer/consumer builds that share Core, Shared or provider outputs with `Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1 -Owner <task-label> -Operation { ... }`. Wrap an entire reusable provider runner or one build batch once; never nest leases. Acquire before touching shared build outputs. A busy lease must stop the operation; do not bypass it with manual DLL copies or another build path. Cross-session messages and process scans are not mutual exclusion. Tests using already-frozen private outputs with `--no-build` may run separately when no other operation mutates those outputs. Verify the lease contract with `Scripts/TestHosts/Test-ProviderBuildLease.ps1`.

- Prefer **real implementations**; avoid TODOs/placeholders for shipping paths.
- **No workaround-first coding** — see `Docs/Devs/AGENT_Root_Cause_No_Fallbacks.md` (section A: general code).
- OASIS APIs often use **`OASISResult<T>`** — keep that pattern for new surface area.
- **NextGenSoftware-Libraries** lives as a **sibling** of this repo (e.g. `../NextGenSoftware-Libraries`), not under `OASIS/external-libs`.

## COSMIC breadth-first delivery (David, 20 November 2026)

Deliver feature breadth before exhaustive depth. Current order: (1) functional EF/NHibernate feature parity, starting with MongoDB/SQLite/Neo4j and shared Core implementations; (2) complete SQL/CQL text and WEB6 Ask Your Data user flows; (3) full/virtual/hybrid Holon workflows; then expanded edge coverage, optimisation and hardening. Preserve the complete provider roadmap and existing provider priorities.

Work in cohesive batches across shared Core and real shipping providers. Use representative end-to-end checks for the newly changed feature/provider paths. Do not multiply scalar variants, edge-case matrices, benchmarks or soak runs while major feature families remain unimplemented. Preserve accepted evidence; rerun only changed behavior, failures or required repository checks. Fix real build and functional blockers promptly.

Keep builds, caller permissions, source/data preservation, acknowledged writes, isolated tests and required CI/deployment gates. Defer nonessential performance tuning and hardening; address serious regressions immediately. Report implementation, representative functional verification and hardened coverage separately. Do not mark full parity or deployment acceptance merely from source presence or a smoke test. Publish usable demo slices as they become available instead of waiting for the whole programme. Follow the existing single-branch, coordination, cleanup and rollout rules.

## Railway WEB4-WEB10 dependency policy

- `Docker/oasis-dependency-versions.env` is the single source of deployed dependency SHAs. Never duplicate commit pins in individual WEB4-WEB10 Dockerfiles or clone moving branch tips.
- When advancing a deployed private submodule, update its parent gitlink and manifest value together, then run `python3 Scripts/validate_railway_dependency_manifest.py --require-gitlinks` and publish the affected services.
- CI enforces Dockerfile usage and exact manifest/gitlink equality on both deployment branches, `Development` and `master`. The legacy parent `main` branch has no WEB4-WEB10 Dockerfiles, so this policy is not applicable there.
- **Railway builds the manifest pins, NOT the parent gitlinks.** Moving a submodule gitlink alone ships nothing: Railway keeps building the old source until `OASIS_*_COMMIT` in the manifest moves too. Always run `python3 Scripts/sync_railway_dependency_manifest.py` after advancing a gitlink and commit both together.
- Safety nets (do not rely on them instead of the step above): `.github/workflows/sync-railway-pins.yml` commits corrected pins after a push to `Development`/`master`, and `.githooks/pre-push` refuses a push whose pins differ from the gitlinks. Enable the hook once per clone (and in any agent or worktree checkout you push from) with `git config core.hooksPath .githooks`.
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

### Building from a fresh checkout

Core is **not** a NuGet package: `OASIS Architecture/NextGenSoftware.OASIS.API.Core`, `ONODE/NextGenSoftware.OASIS.API.ONODE.Core` and `HoloNET-ORM` are git submodules, and the WEB4 projects reference their source. A checkout without them fails restore with NU1605/NU1202 errors that look like a package problem. Before building run:

```bash
git submodule update --init "OASIS Architecture/NextGenSoftware.OASIS.API.Core" "ONODE/NextGenSoftware.OASIS.API.ONODE.Core" "HoloNET-ORM"
```

Then, for example, `dotnet test ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests --filter "FullyQualifiedName~Subscription" -m:2 -nr:false -p:UseSharedCompilation=false`. In Git Bash write MSBuild switches with a dash (`-m:2`), because `/m:2` is rewritten into a path. The unit test project can contain compile errors from other in-flight work; check `git log` for the failing file before assuming your change broke it.

## Shared MongoDB Atlas test isolation — owner instruction 2026-11-18

The OASIS shared Atlas cluster used by dev, staging and production is not a test backend. Do not run provider tests, benchmarks, stress/soak loops, replication/sync tests or diagnostic polling against it. Reuse isolated local/CI Mongo containers (real-provider acceptance) or unit-test in-memory substitutes; never substitute mocks for shipping provider implementations. A separate throwaway Atlas project requires its own explicitly identified disposable configuration.

Any exceptional shared-cluster test must be announced to David before it starts, with an explicit small operation bound, pauses between calls and capped retries. Client HTTP timeouts do not bound server-side operations or cancel background work. Do not infer permission from access to deployed credentials. Never copy secrets into source/evidence. Preserve blocked replication deliveries, including535/543; do not replay, clear, reauthorize or advance them to reduce load. Operational worker pauses must be explicit and preserve durable queues. Read Docs/WIP/HyperDrive-Providers/SHARED_MONGO_STANDDOWN_2026-11-18.md before resuming shared diagnostics.

## Keep every API surface in sync (required)

Whenever you add, change or remove an endpoint, operation or tool in any OASIS API (WEB4 to WEB10, STAR/WEB5, ONODE/ONET, REST, gRPC or GraphQL), update every surface that exposes or documents it in the same piece of work. They must never be stale:

- REST controllers, gRPC services and protos, GraphQL schema and resolvers, kept consistent with each other.
- The npm SDKs (`@oasisomniverse/web4-api` to `web10-api`): methods, `.d.ts` types, docs and tests.
- MCP tools (`@oasisomniverse/mcp-server` and the WEB6 MCP tool catalogue).
- Postman collections (`Scripts/generate_web4_full_postman.py`; the STAR `generate_postman_collection.py`).
- Docs: `Docs/Devs/API Documentation`, the OASISDocsWebsite REST reference and SDK pages, the OASISLearnWebsite tutorials that use the endpoint, and the STAR CLI if it wraps the endpoint.

List every surface you updated in your final report.
