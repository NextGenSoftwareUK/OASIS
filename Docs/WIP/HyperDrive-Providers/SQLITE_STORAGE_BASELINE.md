# SQLite shared storage baseline

Updated 2026-10-28. **Native core-storage-v2:52/52; maintained provider tests:101/101**, zero skips. Baseline only; this is not all-interface, CQL, recovery, fee or HyperDrive routing certification.

## What changed

The existing real EF Core/SQLite repositories committed identity records but omitted `SavedCount`. Successful point/collection reads also omitted `LoadedCount`, and metadata filtering omitted the filtered count. Counts now reflect acknowledged records and selected rows at those existing repository/facade boundaries. No alternative backend, fabricated success or new production interface was added.

Four sync/async avatar/detail save-count regressions failed before the fix. Two metadata-query count regressions failed before their fix. The canonical suite first stopped at2/52 on save acknowledgement, then28/52 on metadata collection count, and finally passed52/52. All101 maintained tests passed after the final repair.

`SqliteStorageConformanceTests` is opt-in through `IncludeNativeStorageConformance=true`. It links the same shared `CoreStorageConformance` and `AtomicConformanceCheckpoint` used elsewhere, uses the shipping constructor with explicit fixed file paths, disables connection pooling, and rejects existing DB/WAL/SHM/checkpoint files. Both schedules exercise the same shared interface checks; no special native substitute bypasses the API.

## Verification and preservation

Fixed environment: `C:/Source/OASIS/TestResults/ProviderConformanceCurrent/SQLLiteDBOASIS/{async,sync}/`. No new runtime, environment, repository clone or download. SDK cache: EF Core/Microsoft.Data.Sqlite10.0.9 with SQLitePCLRaw2.1.11/2.1.12 components; exact package list in the proof. Python's read-only inspection engine version is recorded separately and is not claimed as the provider engine version.

Both final reports contain26checks and `FixtureCleanupComplete=true`. Independent read-only SQLite integrity checks passed; final `Avatar`, `AvatarDetail` and `Holon` tables each contain0rows in both databases. This is interface/current-record absence, not physical erasure of auxiliary histories. The complete DB bytes, including those histories, are preserved compressed.

Initial save-failure DBs/reports were verified against the registered report, synthetic owner identity and empty detail/holon tables. Metadata-failure DBs/reports were verified against the exact owner/root/child IDs. Both pairs were moved, never replayed or reset. An initial preservation attempt stopped before any move because its Python inspection connection was still open; explicit connection closure fixed the inspection tool. No process was killed to bypass a lock.

Six owned DB files totaling4,177,920 B were compressed to90,973 B, decompressed/hash-verified, then the closed raw files removed. Original report JSON remains; preservation receipts referring to raw DB names resolve through the `.sqlite.gz` files and their recorded raw hashes. No unrelated database was touched.

Proof directory: `C:/Source/OASIS/TestResults/HyperDrive-Providers/NativeEvidence/`:

- `sqlite-core-final-identities.json`: SHA256 `1d6b91e352a94cafeb4dec2a42a2dd5d7e4127f74577acc7377cc2e95e649b63`; includes report/DB hashes, counts, binary cohort, SDK packages and measured outputs.
- `sqlite-initial-save-preservation.json`, `sqlite-metadata-count-preservation.json`: exact moved-pair receipts; original outcomes retained.
- `sqlite-core-initial-save-red.trx.gz`, `sqlite-core-metadata-count-red.trx.gz`: original native failures; no full frozen binary cohort retained for those early runs, so do not attribute final binaries to them.
- Targeted red/fixed, maintained99/101 and final native TRX files: compressed, decompressed/verified and owned raw files removed.
- `conformance-sqllitedboasis-current.json`: current aggregate, hash-bound by the registry and exact-byte Git attributes.

Final native cohort: Core `90E9D4C0527F255B66FD0B4FD541550C37471E60CB7C0BBBC23A4FFF8992472D`/849source documents; Shared `9BC6271C4F7DB435530DAFB191F8E1E98D12EBDDBAD93D3081DC53DB16C744E5`/56documents; SQLite provider178 and consumer14source documents verified. Unrelated dirty Core/CQL/replication work is not included in this commit.

## Reproduction and next scope

Use the existing build lease and cached producer/consumer projects with `IncludeNativeStorageConformance=true`, `--no-restore`, `BuildProjectReferences=false`, `/m:2 /nr:false /p:UseSharedCompilation=false`. After source/PDB checks, select `ShippingSqliteRunsSharedStorageSuite` with `OASIS_SQLITE_STORAGE_CONFORMANCE=1`. Register with `record_conformance_results.py --providers SQLLiteDBOASIS`, then regenerate the matrix.

Existing successful reports must be reviewed/preserved before a rerun; never delete them or replay failed fixtures to obtain green. The fixed memory-only save/metadata count controls dispose their providers; no durable runtime is allocated for those cases.

Continue other provider families. Oracle local52/52 remains distinct from cloud acceptance; default OCI profile and Oracle/OCI process configuration were rechecked absent on28October. Existing matrix warnings about additional unimplemented SQLite interface methods remain open; this baseline does not erase them.
