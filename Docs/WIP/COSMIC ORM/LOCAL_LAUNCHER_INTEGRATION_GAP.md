# Published local launcher integration gap

Updated 2 November 2026. Reproduced against the current task checkout and website
commit d3dfe48a4d4e4ff0288180303fbbb06ab88eaebb. This is a source-integration
problem, not evidence that the original native runs failed or should be replayed.

The original audit found 35 absent paths among 37 distinct PowerShell launchers.
DuckDB, LocalFile, Redis, SQLServer four Redis-family demos, Memcached, Upstash, CouchDB, Qdrant, Chroma, Weaviate, Couchbase and PocketBase are now recovered; 19 remain absent from the current checkout. DuckDB implementation commit f1aeb0cb5 is not an ancestor of
current HEAD and survives on the earlier sandbox-integration branch. Counts of
80 current integrated query types/59 hosted proofs do not count the remaining missing
local launchers as additional hosted capabilities.

Recovery source: `origin/work/cosmic-cql-sandbox-integration` at `237403a75b6983e013141cf7ba44bf66858abafd`.
Recover only named provider/demo dependencies after reviewing their current
changes; do not merge the whole branch blindly, reseed fixtures or replay accepted
native tests. First target DuckDB's launcher, CQL implementation and controlled
tests, using its retained source and cached runtime. Preserve current Core/API
changes and original source identities/ACKs. Then repair the other advertised
launcher paths. Audit reported file presence only; a recoverable file is not yet
validated against the current Core or its complete dependency closure.

| Originally missing published launcher | Present at retained branch pin |
|---|---|
| `Scripts/TestHosts/Run-CosmicAptosCqlNative.ps1` | Yes |
| `Scripts/TestHosts/Run-CosmicAzureBlobCqlNative.ps1` | Yes |
| `Scripts/TestHosts/Run-CosmicChromaLinuxDemo.ps1` | Yes |
| `Scripts/TestHosts/Run-CosmicClickHouseLinuxDemo.ps1` | Yes |
| `Scripts/TestHosts/Run-CosmicCosmosCqlNative.ps1` | Yes |
| `Scripts/TestHosts/Run-CosmicCouchDbLinuxDemo.ps1` | Yes |
| `Scripts/TestHosts/Run-CosmicCouchbaseLinuxDemo.ps1` | Yes |
| `Scripts/TestHosts/Run-CosmicDragonflyLinuxDemo.ps1` | Yes |
| `Scripts/TestHosts/Run-CosmicDuckDbCqlNative.ps1` | Yes |
| `Scripts/TestHosts/Run-CosmicEthereumCqlNative.ps1` | Yes |
| `Scripts/TestHosts/Run-CosmicFirebaseCqlNative.ps1` | Yes |
| `Scripts/TestHosts/Run-CosmicFirestoreCqlNative.ps1` | Yes |
| `Scripts/TestHosts/Run-CosmicGarnetLinuxDemo.ps1` | Yes |
| `Scripts/TestHosts/Run-CosmicInfluxDbLinuxDemo.ps1` | Yes |
| `Scripts/TestHosts/Run-CosmicKeyDbLinuxDemo.ps1` | Yes |
| `Scripts/TestHosts/Run-CosmicLitestreamLinuxDemo.ps1` | Yes |
| `Scripts/TestHosts/Run-CosmicLocalFileCqlNative.ps1` | Yes |
| `Scripts/TestHosts/Run-CosmicMemcachedLinuxDemo.ps1` | Yes |
| `Scripts/TestHosts/Run-CosmicNearCqlNative.ps1` | Yes |
| `Scripts/TestHosts/Run-CosmicPocketBaseLinuxDemo.ps1` | Yes |
| `Scripts/TestHosts/Run-CosmicPolkadotCqlNative.ps1` | Yes |
| `Scripts/TestHosts/Run-CosmicQdrantLinuxDemo.ps1` | Yes |
| `Scripts/TestHosts/Run-CosmicRedisCqlNative.ps1` | Yes |
| `Scripts/TestHosts/Run-CosmicS3CqlNative.ps1` | Yes |
| `Scripts/TestHosts/Run-CosmicSqlServerCqlNative.ps1` | Yes |
| `Scripts/TestHosts/Run-CosmicSuiCqlNative.ps1` | Yes |
| `Scripts/TestHosts/Run-CosmicSupabaseLinuxDemo.ps1` | Yes |
| `Scripts/TestHosts/Run-CosmicTonCqlNative.ps1` | Yes |
| `Scripts/TestHosts/Run-CosmicTronCqlDeclared.ps1` | Yes |
| `Scripts/TestHosts/Run-CosmicUpstashCqlNative.ps1` | Yes |
| `Scripts/TestHosts/Run-CosmicValKeyLinuxDemo.ps1` | Yes |
| `Scripts/TestHosts/Run-CosmicWeaviateLinuxDemo.ps1` | Yes |
| `Scripts/TestHosts/Run-ReusableOracleMongoTests.ps1` | Yes |
| `Scripts/TestHosts/Run-ReusableProtocolMongoTests.ps1` | Yes |
| `Scripts/TestHosts/Start-CosmicAptosCqlNode.ps1` | Yes |

## DuckDB recovery milestone

Named original CQL/provider/unit/integration/launcher sources restored. Nine CQL
controls and five identity tests pass, consumer build and PowerShell syntax pass.
No original native cohort or database was replayed. The local integration count
is 65 after restoring this historical provider; hosted count remains 59.

## LocalFile recovery milestone

Named source/launcher restored without changing the provider base. Seven controlled
cases, producer/consumer builds and syntax pass. Original source/seed untouched;
no native replay. Local count 66, hosted 59; 19 advertised launchers remain absent.

## Redis / SQL Server recovery milestone

Named source/launchers recovered; Redis8 and SQLServer17 controls pass, both
producer/consumer builds and syntax pass. Missing Shared protected RecordKey
extension reproduced by failed Redis compile and restored as one visibility
change; canonical persistence key remains the single source. No native service
or fixture replay. Local total68/hosted60;31launchers remain missing.

## Four-provider shipping integration guard

DuckDB14 + LocalFile7 + Redis8 + SQLServer17 = 46 controlled cases pass; all
consumer builds/syntax pass. Shipping GraphQL schema builds against the recovered
source batch (1/1 pass), guarding WEB4 startup after Shared extension restoration.
Raw TestResults/LauncherRecoveryCurrent/shipping-schema.trx SHA256 `827417ccb1a7ff28c8498d86c3a9eadb8c1076f98d33a47bc5c22fae79dc7533`; compressed original/manifest retained at LocalState/COSMIC/LauncherRecoveryCurrent. No native service/fixture replay. Next publish verified batch to Development, then continue31missing launchers.

Verified four-provider batch integrated/pushed to Development59ddcca91881102eb441c8b25e439c1914864ce0, remote tips/manifest checked. Exact existing job2d4fa248-74a7-4b26-bf66-3a991f0cab04 QUEUED; no settings changes/native replay. Durable publication intent/ACK HostedLauncherRecoveryReleaseCurrent. Observe same job, no redeploy on timeout; continue31remaining launcher/source gaps.

## Redis-family recovery milestone

KeyDB/ValKey/Dragonfly/Garnet CQL/provider/test/PowerShell/Python closures restored.
Eight CQL cases each, all producer/consumer builds and syntax pass. Missing
cross-project Mongo test-support link reproduced and replaced by one shared
source at Providers/Shared/TestSupport; helper behavior unchanged, never executed.
Garnet test-only hardcoded Linux root reproduced as outside Windows TestResults,
then derived from its script path; six fake-process lifecycle cases pass and owned
tiny fixture is gone. No native service/fixture replay. Task-only batch, local72/
hosted60;27advertised launchers still missing.

## Memcached / Upstash recovery milestone

Named provider/CQL/consumer/launcher source restored. Memcached15/Upstash17
controlled cases, producer/consumer builds and syntax pass. Memcached shares the
recovered Mongo test-support helper. Closed-port failure and temporary HTTP stub
cases do not adopt native environments. No original service/credential/fixture
opened or replayed; task-only batch. Local74/hosted60;25launchers remain absent.

## CouchDB recovery / earlier deployment readiness

CouchDB named source/launcher restored;41in-memory-handler controls, producer/
consumer build and syntax pass, no native replay. Earlier four-provider Dev job
2d4fa248 SUCCESS at59ddcca91; public health and original-owner discovery for
Mongo/SQLite/Neo4j/ThreeFold ready,0nativequeries/0saves. Compact readiness proof
HostedLauncherRecoveryReleaseCurrent/readiness-observed.json. Local75/hosted60;
24launchers remain absent. Seven more recovered providers remain task-only.

## Qdrant recovery milestone

Named CQL/provider/write-hook/consumer/launcher/setup source restored.38controlled
cases, producer/consumer builds and syntax pass. Setup source parsed only, no
provisioning/native I/O; shared Mongo fixture support compile-only. Payloads use
placeholder vectors, not meaningful semantic search. Local76/hosted60;23missing
launcher paths. Eight further provider recoveries await shipping validation/Dev.

## Second recovery batch shipping guard

Eight providers: KeyDB/ValKey/Dragonfly/Garnet, Memcached, Upstash, CouchDB and
Qdrant.149controlled cases (including6fake-process lifecycle cases) and all
consumer/syntax checks pass. Current shipping API rebuild/schema guard1/1pass,
no skips/native fixture replay. TRX SHA256 `9790e606144bed0476b7fc167f46976968200430a40f1feafef6ac5e6eb4da32`; compressed original/manifest under LocalState/COSMIC/LauncherRecoveryCurrent. Next one verified Development code batch, no settings changes.

Second recovery batch published/pushed Developmentfc437e31a99320c623388776b6a054b3dc1181c9; remote tips and seven Dockerfile/manifest/gitlinks validated. Exact new code jobc11ccee5-8330-4a1c-bb31-f66777b0c49a QUEUED, no settings changes/native replay. Intent/ACK HostedLauncherRecoverySecondReleaseCurrent. Observe same job; continue23remaining source/launcher gaps.

## Chroma recovery milestone

Named provider/CQL/schema/test/PS/Python/setup source restored.50controlled
cases, producer/consumer builds and syntax pass. Setup parsed only, no original
runtime/tenant/database/credential/fixture opened. Shared Mongo helper compile-only.
Task-only source recovery, no new native/browser certification. Local77/hosted60;
22published launcher paths remain absent.

## Weaviate recovery / second deployment readiness

Weaviate named closure restored with cached SDK1.2.0;38controlled cases, builds
and syntax pass, setup parsed only. Explicit gRPC CQL client required; no native
fixture replay or hosted inference. Second Dev jobc11ccee5 SUCCESS atfc437e31a;
public health/original-owner Mongo/SQLite/Neo4j/ThreeFold discovery ready with
0nativequeries/0private saves. Metadata-only receipt:
HostedLauncherRecoverySecondReleaseCurrent/readiness-observed.json. Local78/
hosted60;21published launcher paths remain absent. Chroma/Weaviate task-only.

## Couchbase recovery milestone

Named CQL/provider/test/PS/Python/setup closure restored, cached SDK3.7.1 reused.
31controlled cases, producer/consumer builds and syntax pass; setup parsed only.
Exact native CAS acknowledgement does not imply conditional writes. No original
cluster/bucket/credential/data/fixture opened or replayed. Task-only recovery,
local79/hosted60;20published launcher paths remain absent.

## PocketBase recovery milestone

Named provider/CQL/reconciliation/test/PS/Python closure restored.46controlled
cases/builds/syntax pass; unknown outcomes retain proposed identity for explicit
reconciliation, no retry. Original service/account/credential/fixture untouched.
Local80/hosted60;19published launcher paths remain absent. Four task-only providers
Chroma/Weaviate/Couchbase/PocketBase total165controls; shipping check is next.

## Third recovery shipping guard

Chroma50/Weaviate38/Couchbase31/PocketBase46 total165controlled cases, all
producer/consumer/syntax checks and shipping API/schema guard1/1pass, no skips.
No native/accepted fixture replay. TRX SHA256 `8622a10ce699bd38a25c22f3dfb6f84abd36eee8fbd095675d25364fbb252666`; compressed original/manifest LocalState/COSMIC/LauncherRecoveryCurrent. Ready for one Development code batch, settings unchanged.

Third recovery batch integrated/pushed Development5f4b52ae7f9569276df59b843c43a1b250cfe71b, remote tips and manifest/sevenDockerfiles/gitlinks checked. Exact code jobfc345507-9114-44ef-9e93-bb49b99309c8 QUEUED; no settings/native replay. Durable intent/ACK HostedLauncherRecoveryThirdReleaseCurrent. Follow same job, continue19remaining launcher/source gaps.

## InfluxDB recovery milestone

Named Flux compiler/provider/write-ownership/test/launcher closure restored from
retained pin237403a75. 34 controlled tests, provider/native-consumer builds and
PS/Python syntax pass. No service/config/credential/data opened or native replay.
Local81/hosted60;18 published launcher paths remain absent. Original evidence
and SHA256 manifest: LocalState/COSMIC/LauncherRecoveryCurrent/influxdb-recovery-evidence.json.

## Supabase recovery milestone

36 controlled tests, provider/native-consumer compilation and PS/Python syntax
pass. Reviewed named closure restores native GUID filtering, exact range checks
and canonical write acknowledgement. No original service/data/credential opened.
Local82/hosted60;17 published launcher paths remain absent. Durable original
TRX/hash manifest: LocalState/COSMIC/LauncherRecoveryCurrent/supabase-recovery-evidence.json.
Third recovery Development5f4b52ae7 jobfc345507 SUCCESS; healthy and four provider
discovery/federation entries ready, zero native queries/saves. Original readiness
report: HostedLauncherRecoveryThirdReleaseCurrent/readiness-observed.json.

## Litestream recovery milestone

Named compiler/adapter/test/launcher closure restored;14controlled tests and
producer/native-consumer builds/syntax pass. Existing canonical Mongo helper
linked once. Restore initially NU1101 for absent SQLite10.0.12 meta-package;
exact retained cached Core10.0.9/bundle2.1.11 references restored, no download.
No SQLite/sidecar/replica/native fixture opened. Local83/hosted60;16published
launcher paths remain absent. Durable original evidence/hash manifest:
LocalState/COSMIC/LauncherRecoveryCurrent/litestream-recovery-evidence.json.

## Owned ClickHouse recovery milestone

29relevant controlled cases, provider/native-consumer builds and PS/Python syntax
pass. Preserve current public-country adapter/project dependencies. No owned
service/credential/fixture opened, no green public-profile tests/native replay.
Local84/hosted60;15published launcher paths remain missing (37distinct paths).
Original TRX/hash manifest LocalState/COSMIC/LauncherRecoveryCurrent/clickhouse-recovery-evidence.json.

## Fourth recovery shipping guard

InfluxDB34/Supabase36/Litestream14/owned ClickHouse29 total113controlled cases;
producer/consumer/syntax checks and API/schema guard1/1pass, zero skips. No native
fixtures replayed, current public adapters preserved. Shipping TRX SHA256 `e2ef64ffc8eea004b0027747a87c4995f27ef7527d07d7794bf230ab281ef427`; original compressed proof/manifest LocalState/COSMIC/LauncherRecoveryCurrent. Ready for scoped Development publication, no settings changes.

Fourth recovery batch pushed Development2ae3e658f22a636752d5ff6d216e85b754fdd841;
remote task/Development tips verified. Exact code job206de884-49c8-4a6b-988f-8077c3751f5b QUEUED. Original intent/ACK retained under HostedLauncherRecoveryFourthReleaseCurrent; no settings or native replay.

## Owned AWS S3 recovery milestone

Named compiler/encrypted-backend adapter/native test/launcher closure restored.
7controlled compiler/admission/cancellation cases and producer/native-consumer
compilation/syntax pass. Public source unchanged; no bucket/key/MinIO/Mongo
fixture opened. Local85/hosted60;14published launcher paths remain missing.
Original TRX/hash manifest LocalState/COSMIC/LauncherRecoveryCurrent/aws-s3-recovery-evidence.json.

## Owned Azure Blob recovery milestone

14controlled compiler/official-SDK loopback cases pass, provider/native-consumer
compile and PS syntax pass. Current public source/compile links preserved;
retained helper gains explicit data-directory argument required by original
launcher, existing default retained. Original Azurite/config/data untouched.
Local86/hosted60;13published launcher paths remain missing. Durable original
TRX/hash manifest LocalState/COSMIC/LauncherRecoveryCurrent/azure-blob-recovery-evidence.json.

## Owned Firestore recovery milestone

7compiler/admission/cancellation controls and producer/native-consumer compile/PS
syntax pass. Existing public Google source/README/unit walkthrough links kept.
No original emulator/export/credential/Mongo fixture opened. Local87/hosted60;
12published launcher paths remain missing. Original TRX/hash manifest:
LocalState/COSMIC/LauncherRecoveryCurrent/firestore-recovery-evidence.json.

## Firebase recovery milestone

34controlled cases/provider/native-consumer builds/PS+Python syntax pass.
Canonical save projection/acknowledgement and proposed identity reconciliation
restored; no original emulator/export/config/fixture replay. Local88/hosted60;
11published launcher paths remain missing. Durable original TRX/hash manifest:
LocalState/COSMIC/LauncherRecoveryCurrent/firebase-recovery-evidence.json.
Fourth batch exact job206de884 is BUILDING, same commit2ae3e658f, no resubmission.

## Repeatable published-link guard

Scripts/Sandbox/Audit-LocalDemoLaunchers.py checks the existing website HTML
against a selected checkout, deduplicates launcher paths and fails on missing
files. Directory names cannot satisfy a link; resolved paths must stay inside
the checkout. Output must remain under its TestResults; zero discovered links
is an error. Three fixture tests pass; actual report37published/26present/11missing
returns expected exit1. No launcher/native/provider operation executes.
Evidence TestResults/LauncherRecoveryCurrent/local-launcher-audit.json; regenerate
with command in CURRENT_STATUS. Retained EVM launcher includes Ethereum/Polygon/
Arbitrum teaching paths; review shipping factories and test closure before restore.

## EVM shipping-path mismatch

Retained Ethereum native/federated default constructs legacy EthereumOASIS;
shipping V2 factory selects EthereumOASIS_Web3Core, whose shared base lacks owned
CQL. Do not restore historical legacy code as V2 acceptance. Implement shared V2
query boundary first; see [review and exact next steps](EVM_V2_CQL_RECOVERY_GAP.md).
No source/runtime/native fixture changes; coverage and missing-link count unchanged.

Shared Web3Core V2 now implements bounded authorized owned identity CQL through canonical loads.10controlled tests/provider+consumer build pass; actual Arbitrum V2 factory exposes contract. Real RPC/decryption/native/hosted gates and EVM launcher remain open;11missing, coverage unchanged. See EVM_V2_CQL_RECOVERY_GAP.md.

Six Web3OwnedCqlRpcTests pass through actual shipping Arbitrum V2 facade, production ABI/decryption and canonical loads with existing in-process ReadRpc transport. No external RPC/chain fixture; teaching/native consumer wiring is next. Owned CQL controlled total16; coverage/local launcher counts unchanged.

V2 teaching source EvmV2OwnedWalkthrough now compiles/runs through HolonManager+LINQ/SDK/ABI/crypto with controlled transport:full3/subset2. Six boundary cases pass; Mongo helper compiles but native Mongo/chain not run. Native launcher still red; reconcile compatible retained Web3Core deployment next. Fourth job206de884 observed DEPLOYING.

New explicit retained Arbitrum V2 read-only launcher/native consumer compile and syntax pass; no chain service or key touched. Native3/2 remains pending. Fourth recovery job206de884 SUCCESS, health/discovery/federation ready for Mongo/SQLite/Neo4j/ThreeFold;zero native queries/private saves. Original readiness report HostedLauncherRecoveryFourthReleaseCurrent/readiness-observed.json.

Native retained Arbitrum V2 new read-only gate1/1passes through shipping facade and manager/LINQ3/2. Seed unchanged, zero writes requested, original node exited0/portclosed. Restored node helper refuses replacement ledger. Local89/hosted60;11old published launcher paths remain missing. Original TRX/source-hash manifest LauncherRecoveryCurrent/arbitrum-v2-owned-cql-native-read-evidence.json. Next Mongo federation, not replay.

Paired V2 native full5/subset4query assertions passed before cleanup exposed Mongo missing-root IsError=false bug. Common read boundary fixed. Original overall failed TRX preserved; separate native reconciliation1/1passed sync+async absent checks and exact fixture cleanup, no query/reseed replay. See EVM_V2_CQL_RECOVERY_GAP.md. Counts unchanged.

## Fifth scoped batch shipping verification

Owned S3/Blob/Firestore/Firebase62controlled cases;Web3V2owned16controlled cases;
Python launcher audit3cases;provider/native-consumer builds/syntax pass. Native
standalone3/2gate passes;original paired5/4assertions passed then cleanup failed,
separate native reconciliation passes. Original failure retained. Shipping schema
1/1passes, zero skips. Original TRX SHA256 `773ac9920ae1f36eba5e802891348e63d0105f4512cde38450e2a2da07f42799`; retained original/manifest LauncherRecoveryCurrent/fifth-batch-shipping-schema-evidence.json. No accepted cohort replay/settings changes.

Fifth scoped batch published Development5507b0aa0ae660cb28e213e6e22b5ee69c05910d;
remote task/Development tips and manifest/gitlinks verified. Exact new code
job6c53b53c-491d-45c4-9446-a006af5d1206 QUEUED. Original intent/ACK retained
HostedLauncherRecoveryFifthReleaseCurrent; settings unchanged. Observe same job.

## NEAR recovery milestone

Named canonical key/compiler/adapter/SDK token forwarding/native-test/launcher
closure restored. Eight controlled cases/builds/syntax pass; worker PID marker
removed after verified worker exit. Reflective callsites updated only for changed
internal token signatures. Public profile/bridge/config kept, original native
node/ledger/key untouched. Local90/hosted60;10published launcher paths missing.
Original proof/hash manifest LauncherRecoveryCurrent/near-recovery-evidence.json.

## Cosmos blockchain recovery milestone

Seven controlled cases/provider+native-consumer builds/PS syntax pass. Public
validator source and bridge kept; helper dependencies exist/match retained pin.
No original container/node/key/ledger/contract/Mongo opened. Local91/hosted60;
9published launcher paths missing. Original TRX/hash manifest:
LauncherRecoveryCurrent/cosmos-blockchain-recovery-evidence.json.

## Polkadot recovery milestone

Seven controlled cases/provider+native-consumer builds/PS+Bash syntax pass.
Current public header source/provider project/bridge packaging untouched.
Original Substrate node/key/ledger/SDK fixture unopened. Local92/hosted60;
8published launcher paths remain missing. Original TRX/hash manifest:
LauncherRecoveryCurrent/polkadot-recovery-evidence.json.

## Sui recovery milestone

Seven controlled cases/provider+native-consumer builds/PS syntax pass;public
object source/SDK bridge unchanged. Existing helper closure matches retained
pin;native network/key/ledger/fixture untouched. Local93/hosted60;7published
launcher paths missing. Original TRX/hash LauncherRecoveryCurrent/sui-recovery-evidence.json.
