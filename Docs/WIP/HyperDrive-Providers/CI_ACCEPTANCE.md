# Provider batch CI acceptance

Updated 2026-10-28. [PR495](https://github.com/NextGenSoftwareUK/OASIS/pull/495) was observed already merged at2026-10-27T08:37:44Z, merge `a797a3c8fe2cba6241b228962726d65204596135`; this agent did not issue that merge. Final head `6dd6298c07b8dc2fc5d08678b01f90c03777e996` has no pending checks but five failed checks. Azure local52/52 and111controls do not imply CI green.

## Observed failures and root fixes

- Subscription ledger and MCP build jobs failed the dependency policy before building: manifest Core pin `6abee5de3ce1c96f955061aebae73378e1b7e187` differed from the already committed parent gitlink `ba58ce3957b0111ce5d07547986be80913c7be2b`. The manifest is aligned to that existing gitlink; no Core source/gitlink change. `git ls-remote` confirmed the pinned commit on the Core remote's Development and task branches. Local validator passes across the manifest, seven Dockerfiles and gitlinks.
- KeyDB's native integration job failed avatar/holon saves because the encrypted facade received no separately configured storage key. CI now generates one masked32-byte synthetic storage key per Redis-family job, under its exact provider variable. These are temporary test-only environment values for disposable CI containers, never production/signing keys; no values are recorded here.
- KeyDB image pinned to the digest already resolved by the failed job: `eqalpha/keydb@sha256:6537505c42355ca1f571276bddf83f5b750f760f07b2a185a676481791e388ac`. No local runtime download.

Five inline key mappings were executed in isolated temporary-file controls: names, base64 length and mask-only output verified; no key printed and temporary files removed. Hosted CI rerun remains the acceptance gate. Four old KeyDB smoke cases are not the52-check shared suite and must not promote the matrix to native52/52.

## Bounded evidence and resources

All local paths below are under `C:/Source/OASIS/TestResults/HyperDrive-Providers/`:

- `azure-pr495-checks-current.json`: fixed current job IDs/URLs/statuses; refresh rather than accumulate snapshots.
- `azure-pr495-ledger-failure.log`:65,635 B, workflow run40472204783/job122081746429.
- `azure-pr495-mcp-win-failure.log`:64,167 B, representative run40472204730/job122081747665.
- `azure-pr495-keydb-failure.log`:550,993 B, run40472204794/job122081747670. Logs were captured through the authenticated CLI and terminal escapes removed before inspection.

GitHub-managed ephemeral runners/containers follow the repository's existing CI workflows and caches; they do not allocate additional local environments. Local logs/snapshots are task-owned disposable diagnosis metadata: retain until final CI outcome is documented, then remove exact files; regenerate from these job URLs while GitHub retains logs. Runtime artifacts, native stores and earlier unresolved outcomes are not part of that cleanup.

The PR body uses one fixed `azure-baseline-pr-body.md` path and is removed after successful submission/body verification. No new branch, clone, SDK installation or local container was introduced for CI diagnosis.

Latest merged-PR failed jobs: Solution Integrity122090374355, ledger122090374040, Redis122090374561, STAR CLI122090374684 and Code Quality (run40474833084, ledger run40474833065). Causes at this newer head have not been diagnosed; earlier failure causes above must not be reused as their explanation without reading logs. Keep these CI follow-up gates separate from SQLite local52/52 and101maintained tests.
