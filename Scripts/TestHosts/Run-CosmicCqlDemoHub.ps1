#Requires -Version 7.0
[CmdletBinding()]
param(
    [ValidateSet('SQLite','IPFS','SOLID','ActivityPub','Solana','SolanaMetadata','Databases','CosmosDB','Oracle','AWS','Ethereum','Polygon','Arbitrum','Google','CosmosChain','Polkadot','NEAR','Base','Avalanche','Optimism','Rootstock','BNBChain','Fantom','Linea','Scroll','Monad','AzureBlob','LocalFile','Sui','Redis','PostgreSQL','SQLServer','DuckDB','Upstash','Litestream','Memcached','Valkey','KeyDB','Dragonfly','Garnet','PocketBase','Supabase','CouchDB','SurrealDB','ArangoDB','RavenDB','CockroachDB','TimescaleDB','Cassandra','ScyllaDB','Elasticsearch','OpenSearch','Couchbase','ClickHouse','InfluxDB','Qdrant','Weaviate','Meilisearch','Typesense','Solr','Chroma','Firebase')][string]$Provider='SQLite',
    [switch]$VerifyOnly,
    [switch]$Federated,
    [switch]$MultiRecord,
    [switch]$ArchiveCompletedDemo,
    [switch]$ShowPlan
)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
# Paired runners already own their build lease and fixture lifecycle. Dispatch
# before the single-provider lease below, so no nested lease can be acquired.
if($Provider -in @('Oracle','AWS','Ethereum','Polygon','Arbitrum','Google','CosmosChain','Polkadot','NEAR','Base','Avalanche','Optimism','Rootstock','BNBChain','Fantom','Linea','Scroll','Monad','AzureBlob','LocalFile','Sui','Redis','PostgreSQL','SQLServer','DuckDB','Upstash','Litestream','Memcached','Valkey','KeyDB','Dragonfly','Garnet','PocketBase','Supabase','CouchDB','SurrealDB','ArangoDB','RavenDB','CockroachDB','TimescaleDB','Cassandra','ScyllaDB','Elasticsearch','OpenSearch','Couchbase','ClickHouse','InfluxDB','Qdrant','Weaviate','Meilisearch','Typesense','Solr','Chroma','Firebase') -and !$Federated){throw 'This provider requires its explicit Mongo-paired teaching preset; specify -Federated.'}
if($Federated){
    if($Provider -notin @('CosmosDB','Oracle','AWS','Ethereum','Polygon','Arbitrum','Google','CosmosChain','Polkadot','NEAR','Base','Avalanche','Optimism','Rootstock','BNBChain','Fantom','Linea','Scroll','Monad','AzureBlob','LocalFile','Sui','Redis','PostgreSQL','SQLServer','DuckDB','Upstash','Litestream','Memcached','Valkey','KeyDB','Dragonfly','Garnet','PocketBase','Supabase','CouchDB','SurrealDB','ArangoDB','RavenDB','CockroachDB','TimescaleDB','Cassandra','ScyllaDB','Elasticsearch','OpenSearch','Couchbase','ClickHouse','InfluxDB','Qdrant','Weaviate','Meilisearch','Typesense','Solr','Chroma','Firebase','SolanaMetadata','IPFS','SOLID','ActivityPub')){throw 'This provider has no enrolled Mongo-paired preset.'}
    if($MultiRecord -and $Provider -notin @('IPFS','SOLID','ActivityPub')){throw 'MultiRecord is a protocol-only option.'}
    if($Provider -in @('CosmosDB','AWS','Ethereum','Polygon','Arbitrum','Google','CosmosChain','Polkadot','NEAR','Base','Avalanche','Optimism','Rootstock','BNBChain','Fantom','Linea','Scroll','Monad','AzureBlob','LocalFile','Sui','Redis','PostgreSQL','SQLServer','DuckDB','Upstash','Litestream','Memcached','Valkey','KeyDB','Dragonfly','Garnet','PocketBase','Supabase','CouchDB','SurrealDB','ArangoDB','RavenDB','CockroachDB','TimescaleDB','Cassandra','ScyllaDB','Elasticsearch','OpenSearch','Couchbase','ClickHouse','InfluxDB','Qdrant','Weaviate','Meilisearch','Typesense','Solr','Chroma','Firebase') -and $ArchiveCompletedDemo){throw 'This paired source preserves its original seed and evidence; omit ArchiveCompletedDemo.'}
    $pairedRunner=switch($Provider){
        'CosmosDB' {'Run-ReusableCosmosCqlDemo.ps1'}
        'Oracle' {'Run-ReusableOracleMongoTests.ps1'}
        'AWS' {'Run-CosmicAwsCqlNative.ps1'}
        'Ethereum' {'Run-CosmicEthereumCqlNative.ps1'}
        'Polygon' {'Run-CosmicEthereumCqlNative.ps1'}
        'Arbitrum' {'Run-CosmicEthereumCqlNative.ps1'}
        'Google' {'Run-CosmicFirestoreCqlNative.ps1'}
        'CosmosChain' {'Run-CosmicCosmosCqlNative.ps1'}
        'Polkadot' {'Run-CosmicPolkadotCqlNative.ps1'}
        'NEAR' {'Run-CosmicNearCqlNative.ps1'}
        'Base' {'Run-CosmicEthereumCqlNative.ps1'}
        'Avalanche' {'Run-CosmicEthereumCqlNative.ps1'}
        'Optimism' {'Run-CosmicEthereumCqlNative.ps1'}
        'Rootstock' {'Run-CosmicEthereumCqlNative.ps1'}
        'BNBChain' {'Run-CosmicEthereumCqlNative.ps1'}
        'Fantom' {'Run-CosmicEthereumCqlNative.ps1'}
        'Linea' {'Run-CosmicEthereumCqlNative.ps1'}
        'Scroll' {'Run-CosmicEthereumCqlNative.ps1'}
        'Monad' {'Run-CosmicEthereumCqlNative.ps1'}
        'AzureBlob' {'Run-CosmicAzureBlobCqlNative.ps1'}
        'LocalFile' {'Run-CosmicLocalFileCqlNative.ps1'}
        'Sui' {'Run-CosmicSuiCqlNative.ps1'}
        'Redis' {'Run-CosmicRedisCqlNative.ps1'}
        'PostgreSQL' {'Run-CosmicPostgreSqlCqlNative.ps1'}
        'SQLServer' {'Run-CosmicSqlServerCqlNative.ps1'}
        'DuckDB' {'Run-CosmicDuckDbCqlNative.ps1'}
        'Upstash' {'Run-CosmicUpstashCqlNative.ps1'}
        'Litestream' {'Run-CosmicLitestreamLinuxDemo.ps1'}
        'Memcached' {'Run-CosmicMemcachedLinuxDemo.ps1'}
        'Valkey' {'Run-CosmicValKeyLinuxDemo.ps1'}
        'KeyDB' {'Run-CosmicKeyDbLinuxDemo.ps1'}
        'Dragonfly' {'Run-CosmicDragonflyLinuxDemo.ps1'}
        'Garnet' {'Run-CosmicGarnetLinuxDemo.ps1'}
        'PocketBase' {'Run-CosmicPocketBaseLinuxDemo.ps1'}
        'Supabase' {'Run-CosmicSupabaseLinuxDemo.ps1'}
        'CouchDB' {'Run-CosmicCouchDbLinuxDemo.ps1'}
        'SurrealDB' {'Run-CosmicSurrealDbLinuxDemo.ps1'}
        'ArangoDB' {'Run-CosmicArangoDbLinuxDemo.ps1'}
        'RavenDB' {'Run-CosmicRavenDbLinuxDemo.ps1'}
        'CockroachDB' {'Run-CosmicCockroachDbLinuxDemo.ps1'}
        'TimescaleDB' {'Run-CosmicTimescaleDbLinuxDemo.ps1'}
        'Cassandra' {'Run-CosmicCassandraLinuxDemo.ps1'}
        'Elasticsearch' {'Run-CosmicElasticsearchLinuxDemo.ps1'}
        'OpenSearch' {'Run-CosmicOpenSearchLinuxDemo.ps1'}
        'Couchbase' {'Run-CosmicCouchbaseLinuxDemo.ps1'}
        'ClickHouse' {'Run-CosmicClickHouseLinuxDemo.ps1'}
        'InfluxDB' {'Run-CosmicInfluxDbLinuxDemo.ps1'}
        'Qdrant' {'Run-CosmicQdrantLinuxDemo.ps1'}
        'Weaviate' {'Run-CosmicWeaviateLinuxDemo.ps1'}
        'Meilisearch' {'Run-CosmicMeilisearchLinuxDemo.ps1'}
        'Typesense' {'Run-CosmicTypesenseLinuxDemo.ps1'}
        'Solr' {'Run-CosmicSolrLinuxDemo.ps1'}
        'Chroma' {'Run-CosmicChromaLinuxDemo.ps1'}
        'ScyllaDB' {'Run-CosmicScyllaDbLinuxDemo.ps1'}
        'Firebase' {'Run-CosmicFirebaseCqlNative.ps1'}
        'SolanaMetadata' {'Run-ReusableSolanaMongoTests.ps1'}
        default {'Run-ReusableProtocolMongoTests.ps1'}
    }
    $url=switch($Provider){
        'CosmosDB' {'http://127.0.0.1:5092/'}
        'Oracle' {'http://127.0.0.1:5090/'}
        'AWS' {'http://127.0.0.1:5096/'}
        'Ethereum' {'http://127.0.0.1:5097/'}
        'Polygon' {'http://127.0.0.1:5098/'}
        'Arbitrum' {'http://127.0.0.1:5099/'}
        'Google' {'http://127.0.0.1:5100/'}
        'CosmosChain' {'http://127.0.0.1:5101/'}
        'Polkadot' {'http://127.0.0.1:5102/'}
        'NEAR' {'http://127.0.0.1:5103/'}
        'Base' {'http://127.0.0.1:5104/'}
        'Avalanche' {'http://127.0.0.1:5105/'}
        'Optimism' {'http://127.0.0.1:5125/'}
        'Rootstock' {'http://127.0.0.1:5126/'}
        'BNBChain' {'http://127.0.0.1:5127/'}
        'Fantom' {'http://127.0.0.1:5128/'}
        'Linea' {'http://127.0.0.1:5129/'}
        'Scroll' {'http://127.0.0.1:5130/'}
        'Monad' {'http://127.0.0.1:5131/'}
        'AzureBlob' {'http://127.0.0.1:5106/'}
        'LocalFile' {'http://127.0.0.1:5108/'}
        'Sui' {'http://127.0.0.1:5110/'}
        'Redis' {'http://127.0.0.1:5111/'}
        'PostgreSQL' {'http://127.0.0.1:5112/'}
        'SQLServer' {'http://127.0.0.1:5113/'}
        'DuckDB' {'http://127.0.0.1:5114/'}
        'Upstash' {'http://127.0.0.1:5115/'}
        'Litestream' {'http://127.0.0.1:5116/'}
        'Memcached' {'http://127.0.0.1:5117/'}
        'Valkey' {'http://127.0.0.1:5118/'}
        'KeyDB' {'http://127.0.0.1:5119/'}
        'Dragonfly' {'http://127.0.0.1:5120/'}
        'Garnet' {'http://127.0.0.1:5121/'}
        'PocketBase' {'http://127.0.0.1:5122/'}
        'Supabase' {'http://127.0.0.1:5123/'}
        'CouchDB' {'http://127.0.0.1:5132/'}
        'SurrealDB' {'http://127.0.0.1:5133/'}
        'ArangoDB' {'http://127.0.0.1:5134/'}
        'RavenDB' {'http://127.0.0.1:5135/'}
        'CockroachDB' {'http://127.0.0.1:5136/'}
        'TimescaleDB' {'http://127.0.0.1:5137/'}
        'Cassandra' {'http://127.0.0.1:5138/'}
        'Elasticsearch' {'http://127.0.0.1:5140/'}
        'OpenSearch' {'http://127.0.0.1:5141/'}
        'Couchbase' {'http://127.0.0.1:5142/'}
        'ClickHouse' {'http://127.0.0.1:5143/'}
        'InfluxDB' {'http://127.0.0.1:5144/'}
        'Qdrant' {'http://127.0.0.1:5145/'}
        'Weaviate' {'http://127.0.0.1:5146/'}
        'Meilisearch' {'http://127.0.0.1:5147/'}
        'Typesense' {'http://127.0.0.1:5148/'}
        'Solr' {'http://127.0.0.1:5149/'}
        'Chroma' {'http://127.0.0.1:5150/'}
        'ScyllaDB' {'http://127.0.0.1:5139/'}
        'Firebase' {'http://127.0.0.1:5124/'}
        'SolanaMetadata' {'http://127.0.0.1:5091/'}
        default {'http://127.0.0.1:5088/'}
    }
    $scope=switch($Provider){
        'CosmosDB' {'Mongo + local Azure Cosmos emulator:5rows/4subset; hosted Azure remains separate'}
        'Oracle' {'Mongo + retained Oracle Database:5rows/4subset; no hosted OCI claim'}
        'AWS' {'Mongo + DynamoDB Local:5rows/4subset; bounded native keys, no hosted AWS or shared-snapshot claim'}
        'Ethereum' {'Mongo + retained local EVM chain31338:5rows/4subset; bounded keys, no mainnet or shared-snapshot claim'}
        'Polygon' {'Mongo + concrete Polygon adapter on local EVM chain31338:5rows/4subset; no mainnet or shared-snapshot claim'}
        'Arbitrum' {'Mongo + shipping Arbitrum V2 Web3Core adapter on local EVM chain31338:5rows/4subset; no rollup/mainnet/finality claim'}
        'Google' {'Mongo + retained Firestore emulator:5rows/4subset; bounded native keys, no hosted Google or shared-snapshot claim'}
        'CosmosChain' {'Mongo + retained local Cosmos blockchain:5rows/4subset; encrypted native keys, no Cosmos Hub/mainnet/snapshot claim'}
        'Polkadot' {'Mongo + retained local Polkadot chain:5rows/4subset; encrypted native keys, no public-network/snapshot claim'}
        'NEAR' {'Mongo + retained local NEAR chain:5rows/4subset; original encrypted source, no signing/mainnet/snapshot claim'}
        'Base' {'Mongo + Base Web3Core on local EVM chain31338:5rows/4subset; no Base public-network/rollup/finality claim'}
        'Avalanche' {'Mongo + Avalanche Web3Core on local EVM chain31338:5rows/4subset; no public Avalanche/finality claim'}
        'Optimism' {'Mongo + Optimism V2 Web3Core on retained local EVM31338:5rows/4subset; direct construction, no public OP Stack/sequencer/finality claim'}
        'Rootstock' {'Mongo + Rootstock shipping V2 factory on retained local EVM31338:5rows/4subset; no public Rootstock/Bitcoin anchoring/finality claim'}
        'BNBChain' {'Mongo + BNB V2 on retained local EVM31338:5rows/4subset; direct construction, no public BNB consensus/bridge/finality claim'}
        'Fantom' {'Mongo + Fantom V2 on retained local EVM31338:5rows/4subset; direct construction, no public Fantom consensus/bridge/finality or network migration claim'}
        'Linea' {'Mongo + Linea V2 on retained local EVM31338:5rows/4subset; direct construction, no public Linea consensus/bridge/finality or network migration claim'}
        'Scroll' {'Mongo + Scroll V2 on retained local EVM31338:5rows/4subset; direct construction, no public Scroll consensus/bridge/finality or network migration claim'}
        'Monad' {'Mongo + Monad V2 on retained local EVM31338:5rows/4subset; direct construction, no public Monad consensus/bridge/finality or network migration claim'}
        'AzureBlob' {'Mongo + Azure Blob via retained Azurite:5rows/4subset; no hosted Azure or distributed snapshot claim'}
        'LocalFile' {'Mongo + retained local files:5rows/4subset; no encryption or distributed snapshot claim'}
        'Sui' {'Mongo + retained local Sui encrypted source:5rows/4subset; no hosted chain or distributed snapshot claim'}
        'Redis' {'Mongo + retained encrypted Redis DB11:5rows/4subset; direct registration, no distributed snapshot or alias certification'}
        'PostgreSQL' {'Mongo + retained native PostgreSQL JSON source:5rows/4subset; direct registration, no distributed snapshot or alias certification'}
        'SQLServer' {'Mongo + retained native SQL Server LocalDB JSON source:5rows/4subset; direct registration, no hosted or distributed snapshot claim'}
        'DuckDB' {'Mongo + retained native DuckDB JSON source:5rows/4subset; direct registration, no MotherDuck or distributed snapshot claim'}
        'Upstash' {'Mongo + retained hosted Upstash free scratch:5rows/4subset; expiry admission required, plaintext JSON, no atomic-save or distributed snapshot claim'}
        'Litestream' {'Mongo + admitted Linux Litestream source:5rows/4subset; local-file live replication/restore proved; Windows fsync and remote/PITR/crash scopes separate'}
        'Memcached' {'Mongo + explicitly initialized volatile Linux Memcached generation:5rows/4subset; native CAS/values unchanged by queries, no cache persistence or distributed snapshot claim'}
        'Valkey' {'Mongo + retained native Valkey9.0.4 encrypted source:5rows/4subset; clean restart/ciphertext preservation verified, no cluster or distributed snapshot claim'}
        'KeyDB' {'Mongo + independently built native KeyDB6.3.4 encrypted source:5rows/4subset; clean restart/ciphertext preservation verified, no cluster or distributed snapshot claim'}
        'Dragonfly' {'Mongo + native Dragonfly2.0.2 encrypted source:5rows/4subset; completed RDB snapshot/restart proof, no AOF/crash/cluster or distributed snapshot claim'}
        'Garnet' {'Mongo + native Garnet2.2.5 explicitly admitted generation2:5rows/4subset; checkpoint/restart scope, initial fixture loss retained separately; no AOF/crash or distributed snapshot claim'}
        'PocketBase' {'Mongo + native PocketBase0.40.6:5rows/4subset; retained plaintext JSON source/restart; no atomic concurrency or distributed snapshot claim'}
        'Supabase' {'Mongo + retained native PostgREST16.5 data plane for Supabase:5rows/4subset, JWT and PostgreSQL restart; plaintext JSON, no hosted gateway/Auth or distributed snapshot claim'}
        'CouchDB' {'Mongo + retained authenticated CouchDB3.5.1:5rows/4subset, original source/restart; plaintext JSON, no production concurrency or distributed snapshot claim'}
        'SurrealDB' {'Mongo + retained authenticated SurrealDB3.3.5:5rows/4subset, original source/restart; plaintext JSON, no production concurrency or distributed snapshot claim'}
        'ArangoDB' {'Mongo + retained authenticated ArangoDB3.12.12:5rows/4subset, original source/restart; plaintext JSON, no production concurrency or distributed snapshot claim'}
        'RavenDB' {'Mongo + retained authenticated RavenDB7.2.7:5rows/4subset, original source/restart; plaintext JSON, no production concurrency or distributed snapshot claim'}
        'CockroachDB' {'Mongo + retained authenticated CockroachDB26.2.9:5rows/4subset, original source/restart; plaintext JSON, no production concurrency or distributed snapshot claim'}
        'TimescaleDB' {'Mongo + retained authenticated TimescaleDB2.31.0/PG17:5rows/4subset, original source/restart; plaintext JSON, no production concurrency or distributed snapshot claim'}
        'Cassandra' {'Mongo + retained authenticated Cassandra4.1.12:5rows/4subset, original source/restart; plaintext JSON, no production concurrency or distributed snapshot claim'}
        'Elasticsearch' {'Mongo + retained authenticated Elasticsearch9.5.5:5rows/4subset, original source/restart; HTTPS/Basic license, plaintext source JSON, no production concurrency or distributed snapshot claim'}
        'OpenSearch' {'Mongo + retained authenticated OpenSearch3.9.0:5rows/4subset, original source/restart; HTTPS/security plugin, plaintext source JSON, no production concurrency or distributed snapshot claim'}
        'Couchbase' {'Mongo + retained authenticated Couchbase Community8.0.2:5rows/4subset, original JSON/unsigned CAS across restart; local plaintext loopback, no production concurrency or distributed snapshot claim'}
        'ClickHouse' {'Mongo + retained authenticated ClickHouse26.8.36.3:5rows/4subset, original JSON/UInt64 versions across restart; HTTPS/private CA, FINAL analytical merge semantics, no atomic CAS or distributed snapshot claim'}
        'InfluxDB' {'Mongo + retained authenticated InfluxDB2.7.12:5rows/4subset, original points/timestamps across restart; HTTPS/private CA, future-head and tied-head controls, no atomic CAS/history or distributed snapshot claim'}
        'Qdrant' {'Mongo + retained authenticated Qdrant1.16.3:5rows/4subset, original native peer/payload/vector/config across restart; HTTPS/private CA, bounded IDs only, no similarity/atomic CAS/distributed snapshot claim'}
        'Weaviate' {'Mongo + retained authenticated Weaviate1.39.8:5rows/4subset, gRPC TLS with GraphQL disabled, original JSON/timestamps/schema across restart; bounded IDs only, no similarity/atomic CAS/distributed snapshot claim'}
        'Meilisearch' {'Mongo + retained authenticated Meilisearch1.56.0:5rows/4subset, native HTTPS key fetch and completed write tasks, original JSON/index timestamps/task receipts across restart; bounded IDs only, no similarity/atomic CAS/distributed snapshot claim'}
        'Typesense' {'Mongo + retained authenticated Typesense30.2:5rows/4subset, native HTTPS exact-key filtered search and acknowledged writes, original JSON/collection schema/native database identities across restart; bounded IDs only, no similarity/atomic CAS/distributed snapshot claim'}
        'Solr' {'Mongo + retained authenticated Solr10.1.0:5rows/4subset, native HTTPS real-time key retrieval and hard-committed canonical payloads, original JSON/native versions/schema across restart across restart; bounded IDs only, no similarity/atomic CAS/distributed snapshot claim'}
        'Chroma' {'Mongo + retained local Chroma1.5.9:5rows/4subset, native multi-ID get and original JSON/collection schema across restart; loopback plaintext bounded IDs only, no similarity/atomic CAS/distributed snapshot claim'}
        'ScyllaDB' {'Mongo + retained authenticated ScyllaDB2026.3.4:5rows/4subset, original source/restart; plaintext JSON, no production concurrency or distributed snapshot claim'}
        'Firebase' {'Mongo + Firebase Realtime Database4.11.2 emulator:5rows/4subset, bounded native key-query union; original export restore, no disk/crash or hosted-auth claim'}
        'SolanaMetadata' {'Mongo native + retained Solana via explicit SQLite index:2matching/0empty with read assurance; no source writes'}
        default {"Mongo + $Provider bounded native membership:5rows/4subset; per-provider provenance"}
    }
    $path=Join-Path $PSScriptRoot $pairedRunner
    if($ShowPlan){
        [pscustomobject]@{Provider=$Provider;Federated=$true;Mode=$(if($VerifyOnly){'bounded verification'}else{'interactive until explicit shutdown'});Runner=$path;Url=$url;Scope=$scope}
        return
    }
    switch($Provider){
        'CosmosDB' { & $path -Mongo -Serve:(!$VerifyOnly) }
        'AWS' { & $path -Federated -Serve:(!$VerifyOnly) }
        'Ethereum' { & $path -Federated -Serve:(!$VerifyOnly) }
        'Polygon' { & $path -Provider Polygon -Federated -Serve:(!$VerifyOnly) }
        'Arbitrum' { & $path -Provider Arbitrum -Federated -Serve:(!$VerifyOnly) }
        'Google' { & $path -Federated -Serve:(!$VerifyOnly) }
        'CosmosChain' { & $path -Federated -Serve:(!$VerifyOnly) }
        'Polkadot' { & $path -Federated -Serve:(!$VerifyOnly) }
        'NEAR' { & $path -Federated -Serve:(!$VerifyOnly) }
        'Base' { & $path -Provider Base -Federated -Serve:(!$VerifyOnly) }
        'Avalanche' { & $path -Provider Avalanche -Federated -Serve:(!$VerifyOnly) }
        'Optimism' { & $path -Provider Optimism -Federated -Serve:(!$VerifyOnly) }
        'Rootstock' { & $path -Provider Rootstock -Federated -Serve:(!$VerifyOnly) }
        'BNBChain' { & $path -Provider BNBChain -Federated -Serve:(!$VerifyOnly) }
        'Fantom' { & $path -Provider Fantom -Federated -Serve:(!$VerifyOnly) }
        'Linea' { & $path -Provider Linea -Federated -Serve:(!$VerifyOnly) }
        'Scroll' { & $path -Provider Scroll -Federated -Serve:(!$VerifyOnly) }
        'Monad' { & $path -Provider Monad -Federated -Serve:(!$VerifyOnly) }
        'AzureBlob' { & $path -Federated -Serve:(!$VerifyOnly) }
        'LocalFile' { & $path -Federated -Serve:(!$VerifyOnly) }
        'Sui' { & $path -Federated -Serve:(!$VerifyOnly) }
        'Redis' { & $path -Federated -Serve:(!$VerifyOnly) }
        'PostgreSQL' { & $path -Federated -Serve:(!$VerifyOnly) }
        'SQLServer' { & $path -Federated -Serve:(!$VerifyOnly) }
        'DuckDB' { & $path -Federated -Serve:(!$VerifyOnly) }
        'Upstash' { & $path -Federated -Serve:(!$VerifyOnly) }
        'Litestream' { & $path -Federated -Serve:(!$VerifyOnly) }
        'Memcached' { & $path -Federated -Serve:(!$VerifyOnly) }
        'Valkey' { & $path -Federated -Serve:(!$VerifyOnly) }
        'KeyDB' { & $path -Federated -Serve:(!$VerifyOnly) }
        'Dragonfly' { & $path -Federated -Serve:(!$VerifyOnly) }
        'Garnet' { & $path -Federated -Serve:(!$VerifyOnly) }
        'PocketBase' { & $path -Federated -Serve:(!$VerifyOnly) }
        'Supabase' { & $path -Federated -Serve:(!$VerifyOnly) }
        'CouchDB' { & $path -Federated -Serve:(!$VerifyOnly) }
        'SurrealDB' { & $path -Federated -Serve:(!$VerifyOnly) }
        'ArangoDB' { & $path -Federated -Serve:(!$VerifyOnly) }
        'RavenDB' { & $path -Federated -Serve:(!$VerifyOnly) }
        'CockroachDB' { & $path -Federated -Serve:(!$VerifyOnly) }
        'TimescaleDB' { & $path -Federated -Serve:(!$VerifyOnly) }
        'Cassandra' { & $path -Federated -Serve:(!$VerifyOnly) }
        'Elasticsearch' { & $path -Federated -Serve:(!$VerifyOnly) }
        'OpenSearch' { & $path -Federated -Serve:(!$VerifyOnly) }
        'Couchbase' { & $path -Federated -Serve:(!$VerifyOnly) }
        'ClickHouse' { & $path -Federated -Serve:(!$VerifyOnly) }
        'InfluxDB' { & $path -Federated -Serve:(!$VerifyOnly) }
        'Qdrant' { & $path -Federated -Serve:(!$VerifyOnly) }
        'Weaviate' { & $path -Federated -Serve:(!$VerifyOnly) }
        'Meilisearch' { & $path -Federated -Serve:(!$VerifyOnly) }
        'Typesense' { & $path -Federated -Serve:(!$VerifyOnly) }
        'Solr' { & $path -Federated -Serve:(!$VerifyOnly) }
        'Chroma' { & $path -Federated -Serve:(!$VerifyOnly) }
        'ScyllaDB' { & $path -Federated -Serve:(!$VerifyOnly) }
        'Firebase' { & $path -Federated -Serve:(!$VerifyOnly) }
        'Oracle' { & $path -ArchiveCompletedDemo:$ArchiveCompletedDemo -Serve:(!$VerifyOnly) }
        'SolanaMetadata' { & $path -ArchiveCompletedDemo:$ArchiveCompletedDemo -Serve:(!$VerifyOnly) }
        default { & $path -Provider $Provider -ArchiveCompletedDemo:$ArchiveCompletedDemo -Serve:(!$VerifyOnly) }
    }
    return
}

$runner=switch($Provider){
    'SQLite' {'Scripts/TestHosts/Run-CosmicCqlDemo.ps1'}
    'Databases' {'Scripts/TestHosts/Run-CosmicCqlV1Demo.ps1'}
    'CosmosDB' {'Scripts/TestHosts/Run-ReusableCosmosCqlDemo.ps1'}
    'Solana' {'Scripts/TestHosts/Run-ReusableSolanaTests.ps1'}
    'SolanaMetadata' {'Scripts/TestHosts/Run-ReusableSolanaTests.ps1'}
    'IPFS' {'Scripts/TestHosts/Run-ReusableIpfsTests.ps1'}
    'SOLID' {'Scripts/TestHosts/Run-ReusableSolidReadiness.ps1'}
    'ActivityPub' {'Scripts/TestHosts/Run-ReusableActivityPubPointDemo.ps1'}
}
if($Provider -in @('SQLite','Databases','CosmosDB') -and $ArchiveCompletedDemo){throw 'Database demos own their fixture lifecycle; protocol archive options do not apply.'}
if($MultiRecord -and $Provider -notin @('ActivityPub','IPFS','SOLID')){throw 'MultiRecord launcher is enrolled only for IPFS/SOLID/ActivityPub.'}
if($ShowPlan){
    [pscustomobject]@{
        Provider=$Provider;Mode=$(if($VerifyOnly){'bounded verification'}else{'interactive until explicit shutdown'})
        Runner=(Join-Path $root $runner);Url=$(if($Provider -eq 'SQLite'){'http://127.0.0.1:5087/swagger'}elseif($Provider -eq 'Databases'){'http://127.0.0.1:5088/demo'}elseif($Provider -in @('Solana','SolanaMetadata')){'http://127.0.0.1:5089/'}else{'http://127.0.0.1:5088/'})
        Scope=$(if($Provider -eq 'SQLite'){'Synthetic coffee queries/save/deltas/concurrency'}elseif($Provider -eq 'Databases'){'MongoDB/SQLite/Neo4j group: existing native CQL v1 walkthrough, lifecycle and settlement checks'}elseif($Provider -eq 'CosmosDB'){'Retained local Azure Cosmos DB emulator; native rows/projection/LongCount, controlled identity and receipts'}elseif($Provider -eq 'SolanaMetadata'){'Explicit schema3 metadata profile; one-time journaled local seed, then source-read-only reuse'}elseif($Provider -eq 'Solana'){'Declared-range Solana source / SQLite index; rows, projection and cardinality; no source writes'}elseif($Provider -eq 'ActivityPub'){if($MultiRecord){'Three existing public Mastodon statuses; bounded native membership; GET only'}else{'One existing public Mastodon status; GET only'}}elseif($MultiRecord){'Three owned encrypted native records; bounded membership'}else{'One owned synthetic native point record'})
    };return
}

# The grouped runner owns the one build lease and its native fixture lifecycle.
if($Provider -in @('Databases','CosmosDB')){
    & (Join-Path $root $runner) -Serve:(!$VerifyOnly)
    return
}

# Preserve the existing SQLite runner's assembly identity, result and cleanup gates.
# It owns its build lease; never nest another lease around it.
if($Provider -eq 'SQLite'){
    & (Join-Path $root $runner) -NoBuild -Serve:(!$VerifyOnly)
    return
}

# Use the existing worktree, cached dependencies and one native environment per provider.
# Run one interactive host at a time. The lease lasts through graceful fixture teardown.
& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner "cosmic-demo-$($Provider.ToLowerInvariant())" -Operation {
    switch($Provider){
        'SolanaMetadata' {
            & (Join-Path $root $runner) -IndexedMetadataDemo -ArchiveCompletedDemo:$ArchiveCompletedDemo -ServeIndexedDemo:(!$VerifyOnly)
        }
        'Solana' {
            & (Join-Path $root $runner) -IndexedHttpDemo -ArchiveCompletedDemo:$ArchiveCompletedDemo -ServeIndexedDemo:(!$VerifyOnly)
        }
        'IPFS' {
            & (Join-Path $root $runner) -CqlInteractive:(!$MultiRecord) -CqlKeySet:$MultiRecord -ArchiveCompletedDemo:$ArchiveCompletedDemo -ServePointDemo:(!$VerifyOnly)
        }
        'SOLID' {
            & (Join-Path $root $runner) -CqlInteractive:(!$MultiRecord) -CqlKeySet:$MultiRecord -ArchiveCompletedDemo:$ArchiveCompletedDemo -ServePointDemo:(!$VerifyOnly) -RequireCleanShutdown
        }
        'ActivityPub' {
            & (Join-Path $root $runner) -MultiRecord:$MultiRecord -ArchiveCompletedReport:$ArchiveCompletedDemo -ServePointDemo:(!$VerifyOnly)
        }
    }
}
