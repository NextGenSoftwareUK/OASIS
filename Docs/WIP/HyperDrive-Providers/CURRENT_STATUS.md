# Current provider status

Updated 2026-10-28. Read this first; the four current summaries plus this file and README total under 300 lines. Open the archive only for a specific historical question.

## Scope and working rules

- Phase: provider implementations and shared conformance, by family; approximately one day per family.
- Storage gate: `core-storage-v2`, 26 checks per sync/async mode (52 total).
- Non-storage services use `capability-v1` (5 checks); this does not certify every service operation.
- HyperDrive failover/load balancing/replication follows provider baselines. Custom contracts, fees and crash recovery follow afterward. No Polkadot/NEAR/SQLite depth work now.
- Checkout: `C:/Source/OASIS`, branch `fix/hyperdrive-v2-providers`. Never recreate the deleted old checkout.
- COSMIC/CQL uses `C:/Source/OASIS-cosmic`, branch `work/cosmic-cql`; contact it only for overlapping Core/Shared public API changes.
- The user also works here: stage only owned named files; preserve other dirty files and submodules. No broad staging, attribution trailers or force pushes.
- Batch verified scoped integration into `Development` once/twice daily. Fetch first; verify submodule pushes with `git ls-remote` before parent gitlinks. Do not sweep unverified work into deployment.
- Reuse existing runtimes/caches. Keep reports bounded and log resources in the short current inventory/cleanup sections.

## Recorded baseline evidence

| State | Providers / scope |
|---|---|
| Native storage 52/52 each | Oracle, PostgreSQL, Redis, MongoDB, Neo4j, Azure CosmosDB, Google Firestore, AWSOASIS, DynamoDBOASIS, LocalFile, Azure Blob, SQLite |
| Controlled 52/52 only | KeyDB, ValKey, Dragonfly, Garnet; actual product-native runs pending |
| Native social capability 5/5 | ActivityPub public GET probe; 291 controlled tests. Source/result publication verified; full social operation gates remain open |
| Partial native | Solana4/52: unavailable referenced history chunk; original outcomes retained |
| Blocked before native | SOLID, IPFS, Cosmos blockchain, Ethereum/Arbitrum/Polygon, hosted S3; details in release gates |
| Remaining families | Most matrix rows still lack a native shared-suite run |

These are captured local-engine/emulator or public-read results, not full provider, hosted cloud, all-interface or global HyperDrive certification. The working matrix has 229 projects: 56 non-storage, 173 storage/specialized; zero SDK-source-unverified rows and nine reviewed missing adapters. Source detection is not implementation completeness.

## Exact next steps

1. Next: Firebase RTDB canonical binding (23controlled cases pass); S3 CI wiring published with41controls passed. Development f8e872a4f merged as3f9ae8664, Core319bd96 includes7c45e2c; publish and inspect PR501 native CI. Per-avatar routing/owned-write outbox, fault gates and native-import contracts are now present; preserve them and follow the two public guides. Oracle cloud access remains absent.
2. ActivityPub `00235929a` published: Social/Network declaration and account/detail `IsLoaded` fix verified: native5/5, controlled291, source/PDB identities matched; raw proofs moved to TestResults. Next priority: SOLID input recovery, then EVM/remaining family baselines.
3. SOLID: all three inputs rechecked absent after ActivityPub publication; recover the original three protected files listed in release gates. Do not replace a key over retained records. If unavailable, keep the blocker and continue other priority families.
4. EVM breadth: inspect/reuse the retained runtime/cache and configured ABI/key before a minimal local baseline. Last preflight found no Hardhat CLI or protected local-chain configuration; do not start with custom contract/fee depth.
5. Continue Oracle cloud/AWS S3 acceptance when access is configured, then remaining families. No completion claim until every applicable gate has evidence.

## Latest checkpoint and pending work

- Published `f446b70a2`: repo-relative DynamoDB paths, verified rerun, empty-store checks and 200 MB compaction/admission guard.
- Moved DynamoDB JAR hash: `67ED9662164DC9765FF3ABCA1C2F6F4EDB5F6A92269812450215A679BDC71620`.
- DynamoDB rerun: AWS/Dynamo 52/52 each + 27 factory controls; all five tables empty. SQLite database 43,008 bytes; evidence folder about 50.9 MB; container stopped.
- LocalFile current-suite52/52 and20controls passed; counts/delete acknowledgement fixed; old failures preserved, current fixture payloads0. Details: [LocalFile baseline](LOCALFILE_STORAGE_BASELINE.md).
- Azure Blob automatic runner52/52 +111controls passed; six containers empty, stopped metadata0blobs/blocks after official graceful IPC shutdown. Original failed payloads/checkpoints preserved. [PR495](https://github.com/NextGenSoftwareUK/OASIS/pull/495) was observed merged27October by other work; its final run still has five failed checks. See [CI acceptance](CI_ACCEPTANCE.md); do not infer CI green from merge state.
- SQLite52/52 native +101maintained cases passed; initial/metadata failures preserved, current live tables independently0. Exact proof/cleanup: [SQLite baseline](SQLITE_STORAGE_BASELINE.md).
- SOLID/IPFS: incoming Development registry now claims52/52 each, but both referenced TestResults aggregates are absent from that Git tree and this checkout. Do not promote these claims until evidence is recovered/verified; original blocked evidence stays preserved.
- ActivityPub scoped source/tests and evidence summary published as `00235929a`; six red acknowledgement controls and final291/native5 evidence preserved. Latest Dynamo report updates remain separate. No unrelated changes or Core gitlink included.
- Other dirty work includes Core/ONODE replication changes and unrelated WIP moves/reports; inspect `git status` before staging.
- Documentation tidy checkpoint591182879 preserved history and moved only unlinked evidence; subsequent scoped code/test milestones are recorded above.

## Key commands

```powershell
git status --short
git fetch origin
python -B Scripts/Providers/update_provider_matrix.py
python -B Scripts/Providers/test_provider_matrix.py
python -B Scripts/Providers/test_conformance_reports.py
```

Native runners: `Scripts/Providers/Run-SolidStorageConformance.ps1` (only after input/report review), `Run-EvmStorageConformance.ps1`, and `Run-AwsStorageConformance.ps1 -DynamoOnly -RerunVerifiedDynamo`. Fix their evidence destinations before new raw runs; retain local build/environment leases without cross-agent messaging.

## Recovery map

- [Matrix](PROVIDER_MATRIX.md): per-provider details; query relevant rows instead of loading it all.
- [Open gates](HYPERDRIVE_V2_CURRENT_RELEASE_GATES.md), [inventory](ENVIRONMENT_INVENTORY.md), [cleanup](WORKSPACE_CLEANUP_LEDGER.md), [I/O scope](HYPERDRIVE_V2_PROVIDER_IO_COVERAGE.md).
- [Evidence locations](EVIDENCE_LOCATIONS.md): paths/hashes and migration limitations, not raw payloads.
- [Archive index](Archive/ARCHIVE_INDEX.md): historical hashes; not a default reading list.

Update this file at each milestone. Keep at most ten recent entries in current summaries; roll older entries verbatim into their matching archive rather than growing append-only logs again.
