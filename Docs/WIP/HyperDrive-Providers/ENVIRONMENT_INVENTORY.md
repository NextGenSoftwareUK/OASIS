# Current provider environment inventory

DuckDB28October: one new NuGet SDK installation under C:/Users/david/.nuget/packages/{duckdb.net.data.full,duckdb.net.bindings.full}/1.1.3,716,956/359,389,618 B installed, reused thereafter. No server/environment/clone. Fixed provider bin7,872,059/obj862,994 B; integration bin320,551,210/obj1,107,475 B. Native fixture/evidence20,672,035 B under TestResults/ProviderConformanceCurrent/DuckDBOASIS (original failures preserved, current rows0). Foreign integration-output runtimes247,549,744 B removal rejected by policy; remain pending cleanup, exact paths/recreation and hash receipts in DUCKDB_STORAGE_BASELINE.md. Cache retained reusable; clean only verified owned artifacts after handles stop.

28October addition: official RTDB4.11.2 JAR at C:/Users/david/.cache/firebase/emulators/firebase-database-emulator-v4.11.2.jar,34,495,935 B, SHA256b70d99344caf17c98b6f910fa8f6edf32a7c016cb1035e8915f70d38901eb97f, downloaded once after cache inspection. Reuse Java21.0.12 at C:/Program Files/Microsoft/jdk-21.0.12.101-hotspot/bin/java.exe (sizeunmeasured). Launch rejected by policy; no owner file/19000listener; no bypass. Retain reusable JAR; remove only when retired, recreate via pinned official Firebase URL/hash in ledger. S3 artifact3,205 B retrieved once to fixed TestResults/HyperDrive-Providers/S3CompatibleCurrent; retain two reports and1,244B gzipTRX, no local MinIO image/runtime.

Observed 2026-10-27; owner is the HyperDrive provider task unless marked shared/unattributed. Entries are inherited/reused, not new installs in this checkpoint. Sizes below are logical container root filesystems (including shared image layers), not exclusive disk consumption; named-volume sizes remain unmeasured. All nine listed Podman containers were observed exited. This is a checked current subset, not a claim that every historical resource still exists.

## Fixed reusable resources

| Resource / exact identity | Location / port | Size and lifecycle / retention |
|---|---|---|
| Oracle `oasis-oracle-free` | WSL root Podman volume `oasis-oracle-data` at `/opt/oracle/oradata`; loopback11521 | Root2,214,854,616 B; reusable, preserve client/native data |
| PostgreSQL `oasis-postgres` | Volume `oasis-postgres-data` at `/var/lib/postgresql/data`; loopback15432 | Root300,047,678 B; reusable, preserved failed fixtures |
| Neo4j `oasis-neo4j` (shared) | Data volume `db5b0ed7dfe7b115c845cf7efe40afd8d4d8b4b594147bb03ba74a5c34af4d4b` at `/data`; loopback17687 | Root663,291,184 B; shared with CQL; retain original892A8 journal/data |
| Neo4j logs (shared) | Volume `3a3e642a2f6d8224bec840b2bcb4805bb6eb8d7bebb6ead17bc84baa2c5d1909` at `/logs` | Volume size unmeasured; no blanket removal |
| Azure Cosmos `oasis-cosmos` | Preserved writable-layer data; loopback1234/8080/8081 | Root3,317,634,753 B; no reset/container removal without preserved-data review |
| Redis `oasis-redis-retained` | Volume `oasis-redis-data` at `/data`; loopback18679 | Root71,406,018 B; protected files in `C:/Users/david/AppData/Local/OASIS/ReusableProviderTests/Redis/` |
| SOLID `oasis-solid` | Volume `oasis-solid-data` at `/data`; loopback13000 | Root336,269,470 B; preserve Pod; original client/storage-key files missing |
| Dynamo `oasis-dynamo-retained` / JAR3.3.2 | `C:/Source/OASIS/TestResults/AWSProviderEvidence/{runtime,retained-history-data}`; loopback18000 | Root529,396,246 B; DB43,008 B; whole evidence folder~50.9 MB; active/reusable, not old-history recovery |
| Cosmos chain `oasis-wasmd` / v0.70.3 | Retained root Podman writable state; loopback26657 | Root311,763,912 B; preserve keys/outcomes; SDK bridge unavailable |
| TRON `oasis-tron-tre` / TRE2.0.0 | Retained root Podman writable state; loopback9090/50051/50052 | Root1,298,103,540 B; preserve unresolved evidence/state |

Dynamo JAR SHA-256: `67ED9662164DC9765FF3ABCA1C2F6F4EDB5F6A92269812450215A679BDC71620`. Its moved binds now use the current repo; the deleted old checkout must not be recreated. Original historical Dynamo data was previously missing; the current synthetic store is explicitly marked as such.

## Hosts, tools and caches

- Windows checkout `C:/Source/OASIS`, branch `fix/hyperdrive-v2-providers`; user changes coexist here.
- CQL checkout `C:/Source/OASIS-cosmic`, branch `work/cosmic-cql`; no build/ownership messaging, only overlapping public Core/Shared API coordination.
- Existing executables: `.NET` at `C:/Program Files/dotnet/dotnet.exe`, Node at `C:/Program Files/nodejs/node.exe`, Python at `C:/Users/david/AppData/Local/Programs/Python/Python312/python.exe`; tool/cache sizes unmeasured.
- Reuse `C:/Users/david/.nuget/packages` and the existing npm cache; no restore/install/download occurred in this docs-only checkpoint. Never clear shared caches during active builds.
- Reuse existing provider `bin/obj` and fixed `TestResults/*Current` directories. Latest known per-host measurements are retained in scoped evidence/inventory history; do not count copies as independent environments.
- Mongo test owner uses loopback27029 and `TestResults/SubscriptionUsageCurrent/mongo-data`; revalidate current ownership/path before use. Unattributed MongoPID7508 and NodePIDs19812/23848/26788/30024/32256 were observed; preserve them.
- SOLID protected inputs expected at `TestResults/SolidCurrent/{storage-key.dpapi,private-local-client.dpapi,private-local-pod.dpapi}` remain absent. Only metadata was recreated.
- Original IPFS key/repo and EVM runtime/configuration were missing at last task preflight. No substitute identity has been installed.

## Safe cleanup and recreation

Keep reusable runtimes, protected keys, volumes and failure evidence. Stop only an owned process, then validate exact paths and artifact ownership. Use existing pinned runners/caches for recreation; no fresh clones or duplicate environments. Dynamo enforces a 200 MB store guard and stopped SQLite compaction; tests independently check all five tables empty. Never infer safe deletion from a stopped container or a folder name.

New evidence belongs under `TestResults/HyperDrive-Providers/`; [evidence locations](EVIDENCE_LOCATIONS.md) tracks this checkpoint. Linked legacy evidence stays until consumers are migrated in a separate code change.

Docs checkpoint resources: `TestResults/HyperDrive-Providers/documentation-evidence-reference-scan.json` (113,730 B) and `documentation-evidence-moves.json` (10,332 B) are retained hash/path metadata, with no process/port dependencies. The 27 moved reports retain their original bytes in that folder's NativeEvidence subtree; retain them for evidence recovery. Archive documents are retained history, not duplicate runtimes.

History (not a default reading list): [Archive/ENVIRONMENT_INVENTORY.md](Archive/ENVIRONMENT_INVENTORY.md), byte-preserved with [recorded hashes](Archive/ARCHIVE_INDEX.md). Append only concise current resource changes here; roll older entries into the archive before 150 lines.

28October SQLite baseline: reused cached EF Core/Microsoft.Data.Sqlite10.0.9, SQLitePCLRaw2.1.11/2.1.12 and the fixed ProviderConformanceCurrent/SQLLiteDBOASIS sync/async paths; no server/download. Six owned DB artifacts4,177,920 B compressed/verified to90,973 B, raw removed after explicit connection closure. Current live Avatar/AvatarDetail/Holon counts0; auxiliary histories preserved. Producer bin9,694,778/obj3,401,433 B; consumer bin88,334,683/obj1,293,314 B reused. Final proof, exact paths/hashes and safe recreation: SQLITE_STORAGE_BASELINE.md. Retain compact failures/current evidence; do not touch unrelated user databases.
Older compact output/store measurements: [archived checkpoints](Archive/ENVIRONMENT_INVENTORY_CHECKPOINTS_2026-10-28.md).

28October S3 family: no install/download/runtime. Reused pinned AWSSDK.S3 3.7.502 and existing shared/AWS/MinIO/ThreeFold output hosts. Exact paths, sizes and regeneration/retention in TestResults/HyperDrive-Providers/S3FamilyCurrent/resources.json (ledger hash). Four compact controlled reports, in-memory SDK records0; no local service. Native CI reuses its existing one MinIO container/bucket, not hosted AWS/QSS.

28October Convex: one official pinned backend at /root/oasis-runtimes/convex,215,216,349B total (binary213,980,664B; state628,958B; log605,847B), now stopped/records0,13210/13211loopback. ZIPverified/removed. One reused NodeTools installation53,750,184B (Convex1.48.0 plus4dependencies); companion junction reuses it, existing TypeScript5.5.4 compiler retained. Protected Linux0600admin/instance files and WindowsDPAPIstorage key preserved. Exact paths/packages/output sizes/cleanup in TestResults/HyperDrive-Providers/ConvexCurrent/resources.json and CONVEX_STORAGE_BASELINE.md. No dashboard/frontend/image/clone.
