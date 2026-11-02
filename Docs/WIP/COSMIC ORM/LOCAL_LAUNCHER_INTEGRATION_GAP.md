# Published local launcher integration gap

Updated 2 November 2026. Reproduced against the current task checkout and website
commit d3dfe48a4d4e4ff0288180303fbbb06ab88eaebb. This is a source-integration
problem, not evidence that the original native runs failed or should be replayed.

The page references 37 distinct PowerShell launcher paths; 35 are absent from the
current checkout. DuckDB implementation commit f1aeb0cb5 is not an ancestor of
current HEAD and survives on the earlier sandbox-integration branch. Counts of
64 current integrated query types/59 hosted proofs do not count these missing
local launchers as additional hosted capabilities.

Recovery source: `origin/work/cosmic-cql-sandbox-integration` at `237403a75b6983e013141cf7ba44bf66858abafd`.
Recover only named provider/demo dependencies after reviewing their current
changes; do not merge the whole branch blindly, reseed fixtures or replay accepted
native tests. First target DuckDB's launcher, CQL implementation and controlled
tests, using its retained source and cached runtime. Preserve current Core/API
changes and original source identities/ACKs. Then repair the other advertised
launcher paths. Audit reported file presence only; a recoverable file is not yet
validated against the current Core or its complete dependency closure.

| Missing published launcher | Present at retained branch pin |
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
