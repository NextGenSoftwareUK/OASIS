# Current resource and cleanup ledger

Updated 2026-10-27. Owner: HyperDrive provider task. Use [CURRENT_STATUS.md](CURRENT_STATUS.md) and [current inventory](ENVIRONMENT_INVENTORY.md), not thousands of historical entries, to resume.

## Current rules

- Reuse `C:/Source/OASIS`, existing SDK caches, runtimes and fixed test directories. No duplicate setup or per-run environments.
- Before/alongside resource use, record exact identity/path, version/digest, purpose, ownership, size (or unmeasured), process/port, retention and safe recreation here/in the inventory.
- Preserve source, user/other-agent data, credentials, unresolved outcomes and shared dependencies. A stopped service is not disposable.
- Validate resolved targets and ownership before removal; use literal paths. Stop only owned processes and verify teardown.
- New raw evidence belongs in `TestResults/HyperDrive-Providers/`; docs contain summaries, hashes and paths. Keep only current/previous compact reports rather than unbounded run directories.
- Retain at most ten concise milestones here. Append older entries verbatim to the matching archive; do not rewrite/delete history.

## Current lifecycle and cleanup state

| Resource | State / cleanup and recreation |
|---|---|
| Dynamo evidence `TestResults/AWSProviderEvidence` | Reusable; moved JAR hash verified, same named container rebound, no old-root recreation. DB43,008 B, folder~50.9 MB; all five tables empty. Stop, integrity-check/checkpoint/VACUUM when needed; refuse unknown fixture erasure |
| Podman runtimes/volumes | All listed inventory containers observed stopped; keep data, keys and original failure journals. Shared Neo/Mongo/CQL data must not be deleted by this task |
| NuGet/npm/tools and provider outputs | Inherited/reused; sizes partly unmeasured. Rebuild from existing cache/runner; no install/download in this checkpoint |
| SOLID/IPFS/EVM missing inputs | Missing is not permission to overwrite an existing identity. Preserve original store and record recovery/configuration gates |
| Other Node/Mongo processes | Ownership unverified; not stopped or cleaned |
| Archived docs | Retained documentation, moved without duplicating environments; exact pre-trim hashes in Archive/ARCHIVE_INDEX.md |
| Evidence relocation | Only unreferenced raw files may move; exact paths/hashes and retained-reference reasons are in EVIDENCE_LOCATIONS.md |

## Recent milestones

- `f446b70a2`: repo-relative Dynamo paths; native AWS/Dynamo52/52 each + factory27; raw table counts0; stopped compaction reduced data66,871â†’43,319 B including marker. No new download/image/environment.
- ActivityPub (pending publication): existing HTTP clients/hosts reused; controlled291 and native capability5/5 passed. Initial/red/final evidence stays distinct; no token/social mutation.
- Docs tidy-up: no code/test changes or test execution. Four chronologies and old README preserved byte-for-byte in Archive; two stale handoffs retain their original bodies after a superseded note. Moved 27 unreferenced raw files (633,925 B), hashes unchanged; 269 linked files retained. Scan/move metadata are fixed under TestResults/HyperDrive-Providers; no disk reclaim is claimed.

No broad filesystem or cache cleanup was performed here. Current free-space measurements are machine-wide observations, not proof of exclusive reclaim; inspect current disk state before substantial work.

Complete history: [Archive/WORKSPACE_CLEANUP_LEDGER.md](Archive/WORKSPACE_CLEANUP_LEDGER.md). Do not read it by default. Historical provenance/ownership is not retrospectively asserted complete.
