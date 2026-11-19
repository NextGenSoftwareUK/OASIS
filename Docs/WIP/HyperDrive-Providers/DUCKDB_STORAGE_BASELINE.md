# DuckDB native storage baseline

28October2026. Shipping adapter passes canonical core-storage-v2:26async+26sync checks on DuckDB.NET1.1.3, plus6native control cases. This is a local baseline, not all-interface/version/concurrency/cloud/HyperDrive certification.

## Implementation

- Replaced roughly1,100lines of duplicated record policy with KeyValueStorageProviderBase and a small real DuckDB SQL transport. Canonical codec, identity/query/metadata/history/children/acknowledgements/import/export handling is reused.
- One native oasis_record_store table: primary record key, payload and database-generated UUID revision. Parameterized native SQL provides create-if-absent, guarded replacement and guarded deletion. No retry/fallback or whole-graph atomicity claim.
- DuckDB1.1.3 UPDATE exposed a primary-index duplicate-key error in real tests. Guarded INSERT/ON CONFLICT with native revision predicates implements replacement in one statement; a dedicated update regression now passes. No catch-and-replay or destructive delete/recreate repair.
- Original public constructor, version-control property and soft-delete helper signatures retained. Default holon deletion remains soft; canonical fixture maintenance explicitly hard-deletes only verified synthetic identities afterward.
- Activation refuses legacy three-table files before creating a new store, preventing old data being silently hidden. Two original avatar fixtures were independently read by native SQL, refused by activation, and file hashes remained unchanged. Automatic legacy migration is deliberately absent; original files remain intact.

## Verification and evidence

Initial native2/52 stopped at missing save acknowledgement/count; shared transport advanced to22/52 before the native UPDATE defect. Original failed DBs/reports were moved byte-for-byte to fixed initial-save/initial-update names with SHA-bound preservation receipts. Intermediate runs and default-deletion correction are preserved separately; they are not labelled current passes.

Final batch:2canonical cases (52checks),2legacy-file guards,1update case,1native conditional case, then2independent post-run inspections. Conditional case covers stale/missing replacement/deletion, duplicate create, changed revisions, unconditional overwrite, exact Unicode/quote keys and prefix listing. Current canonical and update fixtures have zero native record rows; conditional fixture's native list is empty. No physical/cryptographic erasure claim.

Producer/consumer dependency SHA and portable PDB source documents matched before and after final execution: Core851documents, Shared56; owned DuckDB producer4 and consumer7(including linked suite) separately source-verified. Consumer Core SHA256CA0A453096801BD225EC0D113532D98ADA0CCD0A16E39BF1E5D2270ECBE5859B; SharedF068432BF85894F551030109706F3366BBEC54D592A38478CD321D69BCFDFF26. The local working tree includes unrelated uncommitted Core work; this batch does not publish it or claim immutable release-artifact acceptance.

- Compact tracked summary: TestResults/HyperDrive-Providers/NativeEvidence/conformance-duckdboasis-current.json, SHA2567297d27d62a7a4b50ae268a68fe8da03c6ff37ee30bbcf276880d49c853464d1.
- Full local hash/size/binary/fixture receipt: TestResults/HyperDrive-Providers/NativeEvidence/duckdb-core-final-identities.json, SHA2561c1cac2eb3bd366b61aa8d40d37af609a0d5b3119788293f4ba2b3e7e11a79c4.
- Original identities and phase receipts: duckdb-initial-identities.json, duckdb-initial-preservation.json, duckdb-update-preservation.json, duckdb-intermediate-default-preservation.json, duckdb-soft-default-preservation.json and duckdb-prior-cohort-preservation.json in the same local evidence directory.
- Databases/reports stay under fixed TestResults/ProviderConformanceCurrent/DuckDBOASIS. Total retained fixture files20,672,035 B, including original failures/intermediate closed stores. No per-run environment created; do not reset or replay unknown old outcomes.

## Resources and rerun

One new pinned SDK installation after cache inspection: NuGet DuckDB.NET.Data.Full1.1.3 (716,956 B installed) and DuckDB.NET.Bindings.Full1.1.3 (359,389,618 B installed). Compressed packages234,907/85,544,817 B remain part of the reusable NuGet installation cache, not separate download archives. Existing .NET/compiler/shared dependencies reused; no server or clone installed. First mixed-source restore failed before installation due URL interpreted as a local path; one canonical-feed restore completed, subsequent restores were cache-only.

Fixed provider bin7,872,059/obj862,994 B; integration bin320,551,210/obj1,107,475 B retained reusable. Foreign-platform generated runtime assets247,549,744 B are disposable on this WindowsX64 test host, but execution policy rejected their removal with only blocked-by-policy reason. They remain; no cleanup claimed. Exact parent: Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.DuckDBOASIS.IntegrationTests/bin/Debug/net10.0/runtimes; candidates linux-arm64,linux-x64,osx,unix,win-arm,win-arm64,win-x86. Preserve win/win-x64 and NuGet cache. Rebuild regenerates outputs from installed packages. Last C free100,033,736,704 B, a shared-machine observation.

Reproduce only after reviewing/retiring exact existing fixtures: IncludeCoreStorageConformance=true and OASIS_DUCKDB_STORAGE_CONFORMANCE=1, fixed shipping-suite/update/condition tests; post-run OASIS_DUCKDB_POSTRUN_INSPECTION=1 for Completed* inspections. Run phases explicitly, not an unreviewed whole-class replay. Use one provider build lease around Core/Shared/provider/consumer bounded cached builds, source verification and dispatch; /m:2 /nr:false /p:UseSharedCompilation=false. Existing guards refuse databases/WAL/checkpoints instead of deleting them.
