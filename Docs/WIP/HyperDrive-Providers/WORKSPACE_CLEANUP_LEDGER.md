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


No broad filesystem or cache cleanup was performed here. Current free-space measurements are machine-wide observations, not proof of exclusive reclaim; inspect current disk state before substantial work.

Complete history: [Archive/WORKSPACE_CLEANUP_LEDGER.md](Archive/WORKSPACE_CLEANUP_LEDGER.md). Do not read it by default. Historical provenance/ownership is not retrospectively asserted complete.










- 2026-10-28 SQLite baseline: reuse existing checkout, SQLite/EF SDK cache, Core/Shared/provider/test outputs; no service/download. Opt-in canonical suite uses fixed `TestResults/ProviderConformanceCurrent/SQLLiteDBOASIS/{async,sync}/storage.sqlite` plus report/lock; refuses any prior DB/WAL/SHM/checkpoint. Two small synthetic DBs planned (sizes unmeasured until run), retain failures and original identities, cleanup only verified owned fixtures after success. This is baseline work, not SQLite depth; unrelated CQL/replication files remain untouched.

- 28October SQLite outcome: native52/52,101maintained cases,6targeted red regressions reproduced before fixes. Initial/metadata DB/report pairs moved with exact identity/integrity/hash checks; six closed DBs4,177,920 B compressed/decompressed/hash-verified to90,973 B, raw removed. Raw TRX likewise compacted. Native current Avatar/AvatarDetail/Holon tables0 in both modes; internal histories retained in compressed DBs. Cached SDKs/outputs only; proof1d6b91e352a94cafeb4dec2a42a2dd5d7e4127f74577acc7377cc2e95e649b63 in SQLITE_STORAGE_BASELINE.md. C free110,874,394,624 B observed after work (before-day measurement unavailable; no exclusive attribution). PR495 merged by other work with CI failures still recorded; no unrelated replication/Core changes staged.

- 28October S3-family binding: reusing AWS unit-test/provider outputs and cached AWSSDK.S3; no restore/download/container started. New opt-in MinIO binding uses fixed TestResults/ProviderConformanceCurrent/AWSS3OASIS-MinIO/{sync,async}/report.json + lock, existing dedicated empty bucket and protected key supplied externally. Build outputs reused (sizes pending); preserve failed reports/objects, never auto-delete unknown bucket contents. C free110,092,222,464 B before build. Native execution remains pending retained/CI service configuration.
- S3 binding build outcome: cached consumer build passed after correcting namespace qualification; 14 warnings,0errors. Two opt-in cases skipped without configuration,0native passes. Reused consumer bin 51613876 B / obj 1272808 B (shared dependencies retained); no new store/service/download or raw test report. Next: existing CI MinIO wiring and actual native run. Outputs regenerate via bounded cached build under provider build lease.
