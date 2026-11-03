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
