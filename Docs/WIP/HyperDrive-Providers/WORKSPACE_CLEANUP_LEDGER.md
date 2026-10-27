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


- 2026-10-27 LocalFile breadth: reused fixed `TestResults/ProviderConformanceCurrent/LocalFileOASIS/{async,sync}` and cached .NET outputs. Exact registered v1 reports/sole synthetic avatars moved to `report-core-storage-v1.json`/`data-core-storage-v1`, hash receipts retained. Current-suite red run reproduced save acknowledgement/count (2/52),20 controls passed; original red fixtures retained pending exact v2 preservation. No SDK download/service/port. New run uses the same fixed environment; no unknown fixtures erased or replayed. Sizes to reconcile after test.

- LocalFile outcome: native52/52 plus20controls passed; current fixture files0 in both modes. Original failure reports/fixtures preserved by verified moves (21,939 B); final proof hash and exact paths in LOCALFILE_STORAGE_BASELINE.md. Raw red/final TRX compressed, decompressed/hash-verified, then raw files removed. Reused producer bin7,534,576 B/obj1,003,703 B; shared consumer bin47,392,246 B/obj2,417,814 B. Retain these shared reusable outputs; no blanket bin/cache deletion. Two retirement controls and six report controls pass; their synthetic temporary directories are removed on exit. No downloads or new runtime.

- 2026-10-27 Azure Blob breadth: reused pinned Azurite3.37.0 at `C:/Users/david/.cache/oasis-runtimes/azurite-3.37.0` (148831879 B; entry SHA256 `2f3f13f68be0cdf2d6c7c8ebeb786f1c3a3c7c98fa1b490680716282b9c64a13`), existing Node24/Python312/.NET10/NuGet cache. Use fixed `TestResults/AzureCurrent` lock/logs and `conformance-data` persistent synthetic store on loopback10000; no second emulator/download. Old routing-file admission gate preserved. Native test uses six explicit conformance containers and real shipping factory/Blob SDK; refuses existing payloads, retains failed store, verifies successful fixtures absent. Current/previous compact proofs stay under TestResults/HyperDrive-Providers. Output/store sizes and teardown to reconcile after run. C free before 125922750464 B.

- Azure Blob outcome:95controls passed; native2/52 exposed omitted save counts. Initial hidden startup timed out; same retained CLI then listened, ran the probe and was stopped (original process15684, historical only). Persistent `TestResults/AzureCurrent/conformance-data`12,472 B retained with original fixtures; do not reset/replay. Both raw TRX files compressed/verified and removed; hash inventory in AZURE_BLOB_STORAGE_BASELINE.md. C free125,692,551,168 B observed, not exclusive attribution. No download or second runtime; automatic startup remains unverified.

- Azure acknowledgement repair (27 October): reused same producer/consumer and cache, six real red count regressions reproduced;107controls now pass. Exact reconciliation is limited to the two registered initial avatar-save failures, saves their native bytes/hashes before ETag-conditional deletion and refuses additional rows/revisions/checkpoints. Retain native store and receipts on any failure; no identity reset, download or second environment. Reuse fixed AzureCurrent logs/port10000; verify owned process before stopping.

- Azure count checkpoint:109controls pass; native remains25/52 with graph fixtures and async report.json.next preserved. Two initial synthetic avatars were saved as native bytes/hash receipts and ETag-conditionally removed; reconciliation test passed. Same hidden runtime opened in1.4426s and was stopped after native failure. Red/control/native TRX compressed/verified, raw files removed; exact proof hashes and cohort boundaries in AZURE_BLOB_STORAGE_BASELINE.md. No downloads/second environment; retain current store and checkpoints for exact reconciliation. C free125506592768 B observed.
