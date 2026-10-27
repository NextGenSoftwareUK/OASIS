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
- ActivityPub (verified baseline): existing HTTP clients/hosts reused; controlled291 and native capability5/5 passed. Initial/red/final evidence stays distinct; no token/social mutation.
- Docs tidy-up: no code/test changes or test execution. Four chronologies and old README preserved byte-for-byte in Archive; two stale handoffs retain their original bodies after a superseded note. Moved 27 unreferenced raw files (633,925 B), hashes unchanged; 269 linked files retained. Scan/move metadata are fixed under TestResults/HyperDrive-Providers; no disk reclaim is claimed.

No broad filesystem or cache cleanup was performed here. Current free-space measurements are machine-wide observations, not proof of exclusive reclaim; inspect current disk state before substantial work.

Complete history: [Archive/WORKSPACE_CLEANUP_LEDGER.md](Archive/WORKSPACE_CLEANUP_LEDGER.md). Do not read it by default. Historical provenance/ownership is not retrospectively asserted complete.
- 2026-10-27 report-writer migration: reused Python312; six report controls passed. Temporary `%TEMP%/oasis-report-control-*` directories held synthetic JSON only (size unmeasured), created/removed by TemporaryDirectory within the test; no services, SDK restores or native data touched. Future aggregates/archival outputs reuse `C:/Source/OASIS/TestResults/HyperDrive-Providers/NativeEvidence`; retained evidence is hash-bound by the registry. No download or environment created.

- 2026-10-27 ActivityPub publication: moved nine unpublished proof files (144,754 B) byte-for-byte to `C:/Source/OASIS/TestResults/HyperDrive-Providers/NativeEvidence`; audit lists exact sizes/hashes. Existing Python312 and frozen provider/test bin/PDB files reused, no rebuild/download/native call. PDB/source and compressed report hashes verified;13 matrix +6 report controls passed. Temporary report controls removed their synthetic directories. C free125,954,445,312 B observed; proof files retained for reproducibility, no process/port dependency.

- 2026-10-27 LocalFile breadth: reused fixed `TestResults/ProviderConformanceCurrent/LocalFileOASIS/{async,sync}` and cached .NET outputs. Exact registered v1 reports/sole synthetic avatars moved to `report-core-storage-v1.json`/`data-core-storage-v1`, hash receipts retained. Current-suite red run reproduced save acknowledgement/count (2/52),20 controls passed; original red fixtures retained pending exact v2 preservation. No SDK download/service/port. New run uses the same fixed environment; no unknown fixtures erased or replayed. Sizes to reconcile after test.

- LocalFile outcome: native52/52 plus20controls passed; current fixture files0 in both modes. Original failure reports/fixtures preserved by verified moves (21,939 B); final proof hash and exact paths in LOCALFILE_STORAGE_BASELINE.md. Raw red/final TRX compressed, decompressed/hash-verified, then raw files removed. Reused producer bin7,534,576 B/obj1,003,703 B; shared consumer bin47,392,246 B/obj2,417,814 B. Retain these shared reusable outputs; no blanket bin/cache deletion. Two retirement controls and six report controls pass; their synthetic temporary directories are removed on exit. No downloads or new runtime.

- 2026-10-27 Azure Blob breadth: reused pinned Azurite3.37.0 at `C:/Users/david/.cache/oasis-runtimes/azurite-3.37.0` (148831879 B; entry SHA256 `2f3f13f68be0cdf2d6c7c8ebeb786f1c3a3c7c98fa1b490680716282b9c64a13`), existing Node24/Python312/.NET10/NuGet cache. Use fixed `TestResults/AzureCurrent` lock/logs and `conformance-data` persistent synthetic store on loopback10000; no second emulator/download. Old routing-file admission gate preserved. Native test uses six explicit conformance containers and real shipping factory/Blob SDK; refuses existing payloads, retains failed store, verifies successful fixtures absent. Current/previous compact proofs stay under TestResults/HyperDrive-Providers. Output/store sizes and teardown to reconcile after run. C free before 125922750464 B.

- Azure Blob outcome:95controls passed; native2/52 exposed omitted save counts. Initial hidden startup timed out; same retained CLI then listened, ran the probe and was stopped (original process15684, historical only). Persistent `TestResults/AzureCurrent/conformance-data`12,472 B retained with original fixtures; do not reset/replay. Both raw TRX files compressed/verified and removed; hash inventory in AZURE_BLOB_STORAGE_BASELINE.md. C free125,692,551,168 B observed, not exclusive attribution. No download or second runtime; automatic startup remains unverified.
