# Current provider environment inventory

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

Latest LocalFile (27 October): reused `Providers/Storage/NextGenSoftware.OASIS.API.Providers.LocalFileOASIS/{bin,obj}` (7,534,576/1,003,703 B) and `Providers/Shared/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests/{bin,obj}` (47,392,246/2,417,814 B), cached .NET10/Python312, no download or server port. Fixed `TestResults/ProviderConformanceCurrent/LocalFileOASIS/{async,sync}` current data is empty; preserved original reports/fixtures total21,939 B. Raw proof identities/hashes and cleanup/recreation gates: [LocalFile baseline](LOCALFILE_STORAGE_BASELINE.md). Status: reusable outputs; retained failure evidence, not disposable unknown records.

Azure Blob (27 October): reused Azurite3.37.0 `C:/Users/david/.cache/oasis-runtimes/azurite-3.37.0`148,831,879 B, Node24.19.0, existing .NET10/cache. Fixed AzureCurrent persistent conformance store12,472 B, runtime stopped; retain failure fixtures until reconciliation. Reused outputs: `Providers/Cloud/NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS/bin` 7545311 B; `Providers/Cloud/NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS/obj` 1026560 B; `Providers/Cloud/TestProjects/NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS.UnitTests/bin` 48230375 B; `Providers/Cloud/TestProjects/NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS.UnitTests/obj` 1189797 B. No new installation/download; regenerate via cached runner after admission gates. Details and hashes: [Azure Blob baseline](AZURE_BLOB_STORAGE_BASELINE.md).
