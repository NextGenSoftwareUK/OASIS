"""Reproducible breadth-first inventory; source evidence is not live conformance."""
import argparse, hashlib, json, re, os, functools, xml.etree.ElementTree as ET
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
DEST=ROOT/'Docs/WIP/HyperDrive-Providers'
EXCLUDED=re.compile(r'test|harness|example|sample|demo|benchmark',re.I)
SDK={'Nethereum':r'new (?:Nethereum\.)?Web3\b|new Web3CoreOASIS\b','Npgsql':r'new NpgsqlConnection\b',
 'MongoDB.Driver':r'new MongoClient\b','Oracle managed SDK':r'new OracleConnection\b',
 'Microsoft.Data.Sqlite':r'new SqliteConnection\b','Neo4j.Driver':r'GraphDatabase\.Driver\(',
 'AWS SDK':r'new (?:Amazon\.[\w.]+\.)?Amazon(?:S3|DynamoDB)Client\b',
 'Google Firestore':r'FirestoreDb\.(?:Create|CreateAsync)|new FirestoreDbBuilder\b',
 'Azure Cosmos':r'new CosmosClient\b','Solnet':r'ClientFactory\.GetClient\(','IPFS client':r'new IpfsClient\b'}
# Explicit product roles override inherited template capabilities and incidental crypto SDK imports.
NON_STORAGE=set("CargoOASIS HolonymOASIS ProofOfHumanityOASIS SelfProtocolOASIS CivicOASIS ReclaimProtocolOASIS BittensorOASIS GaladrielOASIS RitualOASIS IntelOpenVINOOASIS MapboxOASIS GoogleMapsOASIS GOMapOASIS HEREMapsOASIS MapLibreOASIS NianticLightshipOASIS WRLD3DOASIS ReadyPlayerMeOASIS DecentralandOASIS TheSandboxOASIS DuneAnalyticsOASIS CovalentOASIS TheGraphOASIS SubqueryOASIS GoldskyOASIS NansenOASIS ZapperOASIS BlockscoutOASIS MoralisOASIS MoralisStreamsOASIS AlchemyOASIS AnkrOASIS InfuraOASIS QuickNodeOASIS TenderlyOASIS ReservoirOASIS ENSOASIS ENSOffchainOASIS PolygonIDOASIS PrivyOASIS PrivyServerWalletsOASIS zkPassOASIS GitcoinPassportOASIS SuiZkLoginOASIS SafeOASIS ZkSyncSSOOASIS WorldIDOASIS LayerZeroOASIS PushProtocolOASIS WakuOASIS AkashOASIS LivepeerOASIS TemporalOASIS OrionProtocolOASIS LitProtocolOASIS".split())
FAMILY_NAMES={
 'IPFS/content-addressed':{'FilecoinOASIS'},
 'Document':set("FirebaseOASIS AppwriteOASIS FaunaOASIS ConvexOASIS PocketBaseOASIS PolybaseOASIS GUNOASIS OrbitDBOASIS PouchDBOASIS SurrealDBOASIS AWSOASIS GoogleCloudOASIS".split()),
 'SQL':set("CloudflareD1OASIS TursoOASIS TablelandOASIS PlanetScaleOASIS NileOASIS PgVectorOASIS MotherDuckOASIS FlyIOOASIS".split()),
 'Key-value/object':set("UpstashOASIS AzureStorageOASIS CloudflareOASIS ThreeFoldOASIS CloudinaryOASIS OracleObjectStorageOASIS DragonflyOASIS GarnetOASIS".split()),
 'Graph':set("ArangoDBOASIS ArcadeDBOASIS".split()),
 'Search/vector storage':set("AlgoliaOASIS ChromaOASIS ElasticsearchOASIS LanceDBOASIS MarqoOASIS MeilisearchOASIS MilvusOASIS OpenSearchOASIS PineconeOASIS QdrantOASIS SolrOASIS TypesenseOASIS WeaviateOASIS XataOASIS ZillizOASIS".split()),
 'Analytical SQL':set("BigQueryOASIS ClickHouseOASIS DatabricksOASIS DruidOASIS InfluxDBOASIS PinotOASIS QuestDBOASIS RedshiftOASIS SnowflakeOASIS".split()),
 'Wide-column':set("CassandraOASIS ScyllaDBOASIS".split()),
 'Federated web':set("SOLIDOASIS ActivityPubOASIS ScuttlebuttOASIS UrbitOASIS BlueSkyOASIS DiscordOASIS FarcasterOASIS LensOASIS LensV2OASIS LoomOASIS MatrixOASIS NostrOASIS StoryProtocolOASIS TelegramOASIS".split()),
 'Distributed application storage':set("HoloOASIS HoloOASIS.Desktop HoloOASIS.Edge HoloOASIS.Unity PLANOASIS ONION-Protocol".split()),
 'Non-EVM chain':set("CosmosBlockChainOASIS CelestiaOASIS EclipseOASIS EOSIOOASIS SEEDSOASIS AztecOASIS".split())}
def family(name,text,path):
 if name in NON_STORAGE or path.parts[1]=='AI':return 'Non-storage'
 for category,names in FAMILY_NAMES.items():
  if name in names:return category
 if 'Web3CoreOASISBaseProvider' in text or name in {'EthereumOASIS','ArbitrumOASIS','PolygonOASIS','BaseOASIS','ChainLinkOASIS'}:return 'EVM'
 if path.parts[1]=='Blockchain':return 'Non-EVM chain'
 if any(x in name.lower() for x in ['sql','oracle','cockroach','timescale','supabase','neon','duckdb','mariadb','tidb','yugabyte','singlestore']):return 'SQL'
 if any(x in name.lower() for x in ['ipfs','filecoin','arweave','pinata','storj','sia']):return 'IPFS/content-addressed'
 if 'DynamoDBOASIS' in text or any(x in name.lower() for x in ['mongo','cosmosdb','dynamodb','couch','firestore','documentdb','raven']):return 'Document'
 if 'KeyValueStorageProviderBase' in text or any(x in name.lower() for x in ['redis','valkey','keydb','memcache','s3','minio']):return 'Key-value/object'
 if any(x in name.lower() for x in ['neo4j','tigergraph','neptune','dgraph']):return 'Graph'
 if 'LocalFile' in name:return 'Filesystem'
 return 'Other / capability review'

# Match actual client use, including injected clients and target-typed constructors.
# Namespace/type guard avoids confusing an unrelated JsonRpcClientFactory with Solnet.
CS_SDK_RULES=[
 ('DuckDB.NET','\\bDuckDB\\.NET\\.Data\\b','new\\s+DuckDBConnection\\b'),
 ('MySqlConnector','\\bMySqlConnector\\b','new\\s+MySqlConnection\\b'),
 ('Snowflake.Data','\\bSnowflake\\.Data\\.Client\\b','new\\s+SnowflakeDbConnection\\b'),
 ('Cassandra C# driver','\\bCassandra\\b','\\bCluster\\.Builder\\('),
 ('Couchbase SDK','\\bCouchbase\\b','\\b(?:Couchbase\\.)?Cluster\\.ConnectAsync\\('),
 ('RavenDB client','\\bRaven\\.Client\\.Documents\\b','new\\s+DocumentStore\\b'),
 ('NATS.Net','\\bNATS\\.Client\\.Core\\b','new\\s+NatsConnection\\b'),
 ('Dapr SDK','\\bDapr\\.Client\\b','new\\s+DaprClientBuilder\\b'),
 ('Temporal SDK','\\bTemporalio\\.Client\\b','\\bTemporalClient\\.ConnectAsync\\('),
 ('ClickHouse.Client','\\bClickHouse\\.Client\\.ADO\\b','new\\s+ClickHouseConnection\\b'),
 ('InfluxDB.Client','\\bInfluxDB\\.Client\\b','\\bInfluxDBClientFactory\\.Create\\('),
 ('Enyim Memcached','\\bEnyim\\.Caching\\b','new\\s+MemcachedClient\\b'),
 ('SurrealDb.Net','\\bSurrealDb\\.Net\\b','new\\s+SurrealDbClient\\b'),
 ('OCI Object Storage SDK',r'\bOci\.ObjectstorageService\b',r'\.(?:GetObject|PutObject|ListObjects|DeleteObject)\('),
 ('StackExchange.Redis',r'\bStackExchange\.Redis\b',r'\bConnectionMultiplexer\.(?:Connect|ConnectAsync)\(|\.(?:StringGetAsync|StringSetAsync|ScriptEvaluateAsync)\('),
 ('Microsoft.Data.SqlClient',r'\bMicrosoft\.Data\.SqlClient\b',r'new\s+SqlConnection\b|\bSqlConnection\s+\w+\s*=\s*new\s*\('),
 ('Nethereum',r'\bNethereum\b',r'CreateWeb3WithAccount\s*\(|\.Eth\.[\w.]+(?:SendRequestAsync|SendTransactionAsync)\s*\(|new\s+(?:Nethereum\.Web3\.)?Web3\b'),
 ('MongoDB.Driver',r'MongoDB\.Driver|\bIMongoDatabase\b',r'new\s+MongoClient\b|\.MongoClient\.(?:GetDatabase|StartSessionAsync)|\.(?:FindAsync|InsertOneAsync|ReplaceOneAsync)\('),
 ('AWS S3 SDK',r'Amazon\.S3|\bIAmazonS3\b',r'AmazonS3Client|\.(?:GetObjectAsync|PutObjectAsync|ListObjectsV2Async)\('),
 ('AWS DynamoDB SDK',r'Amazon\.DynamoDBv2|\bIAmazonDynamoDB\b',r'AmazonDynamoDBClient|\.(?:GetItemAsync|PutItemAsync|TransactWriteItemsAsync)\('),
 ('Google Cloud SDK',r'Google\.Cloud\.',r'new\s+(?:StorageClientBuilder|FirestoreDbBuilder|BigQueryClientBuilder)\b|\b(?:StorageClient|FirestoreDb|BigQueryClient)\.(?:Create|CreateAsync)\('),
 ('Azure SDK',r'Azure\.Storage|Microsoft\.Azure\.Cosmos',r'new\s+(?:BlobServiceClient|BlobContainerClient|CosmosClient)\b|\.(?:ReadItemAsync|UpsertItemAsync|UploadAsync)\('),
 ('Solnet',r'\bSolnet\.',r'\bClientFactory\.GetClient\(|\.(?:SendTransactionAsync|GetAccountInfoAsync)\('),
 ('HTTP API client',r'System\.Net\.Http|\bHttpClient\b',r'\.(?:SendAsync|GetAsync|GetStringAsync|PostAsync|PutAsync|DeleteAsync|PostAsJsonAsync|PutAsJsonAsync)\(')]

def strip_cs_comments(text):
 # Preserve quoted strings while removing ordinary line/block comments from evidence.
 token=re.compile(r'@"(?:""|[^"])*"|"(?:\\.|[^"\\])*"|//[^\n]*|/\*.*?\*/',re.S)
 return token.sub(lambda match: match.group(0) if match.group(0).startswith(('"','@"')) else ' ',text)

def detect_cs_sdk(text):
 text=strip_cs_comments(text)
 found=[name for name,guard,call in CS_SDK_RULES if re.search(guard,text) and re.search(call,text)]
 for name,pattern in SDK.items():
  if name not in {'Solnet','Nethereum','AWS SDK'} and re.search(pattern,text):found.append(name)
 return sorted(set(found))

def source_files(directory):
 for current,dirs,files in os.walk(directory):
  dirs[:]=[d for d in dirs if d not in {'bin','obj','node_modules','.git'}]
  for name in sorted(files):
   if name.endswith('.cs'):yield Path(current)/name

def projects():
 for current,dirs,files in os.walk(ROOT/'Providers'):
  dirs[:]=[d for d in dirs if d not in {'bin','obj','node_modules','.git'}]
  for name in sorted(files):
   if name.endswith('.csproj'):yield Path(current)/name

@functools.lru_cache(None)
def source_text(project):
 return '\n'.join(p.read_text(encoding='utf-8-sig',errors='replace') for p in sorted(source_files(project.parent)))

def node_sdk_evidence(directory):
 known={'arweave','near-api-js','@cosmjs/stargate','@cosmjs/cosmwasm','@polkadot/api','@mysten/sui','@aptos-labs/ts-sdk','@solana/web3.js'}
 found=[]
 for current,dirs,files in os.walk(directory):
  dirs[:]=[d for d in dirs if d not in {'bin','obj','node_modules','.git'}]
  if 'package.json' not in files:continue
  package=Path(current)/'package.json'
  try:deps=json.loads(package.read_text(encoding='utf-8-sig')).get('dependencies',{})
  except (ValueError,OSError):continue
  modules=[]
  for p in Path(current).glob('*'):
   if p.suffix in {'.js','.mjs','.cjs','.ts'} and not re.search(r'(^test-|\.test\.|\.spec\.|^bootstrap-)',p.name):modules.append(p.read_text(encoding='utf-8-sig',errors='replace'))
  source='\n'.join(modules)
  for name in known.intersection(deps):
   if re.search(r"(?:from\s*|require\s*\()?[\"']"+re.escape(name)+r"(?:/[^\"']*)?[\"']",source):found.append(name+' '+str(deps[name])+' (bridge import)')
 return sorted(set(found))

def sdk_evidence(project,visited=None):
 visited=set() if visited is None else set(visited)
 if project in visited:return []
 visited.add(project);found=detect_cs_sdk(source_text(project))
 found+=node_sdk_evidence(project.parent)
 for ref in ET.parse(project).findall('.//ProjectReference'):
  path=(project.parent/ref.attrib['Include'].replace('\\','/')).resolve()
  if path.is_file() and path.name.startswith(('NextGenSoftware.OASIS.API.Providers.','NextGenSoftware.OASIS.Providers.Shared.')):
   namespace=ET.parse(path).findtext('.//RootNamespace') or path.stem
   current_source=strip_cs_comments(source_text(project))
   short=namespace.rsplit('.',1)[-1]
   relative_base=re.search(r':\s*'+re.escape(short)+r'\.'+re.escape(short)+r'\b',current_source)
   same_provider_root=namespace.rsplit('.',1)[0] in current_source
   if namespace in current_source or (relative_base and same_provider_root):found+=sdk_evidence(path,visited)
 return sorted(set(found))

def build():
 registry=json.loads((DEST/'CONFORMANCE_RESULTS.json').read_text(encoding='utf-8-sig'))
 evidence={r['provider']:r for r in registry['results']};rows=[]
 for project in sorted(projects()):
  rel=project.relative_to(ROOT)
  if rel.parts[1]=='Template' or not project.stem.startswith('NextGenSoftware.OASIS.API.Providers.') or any(EXCLUDED.search(p) for p in rel.parts):continue
  name=project.stem.removeprefix('NextGenSoftware.OASIS.API.Providers.')
  sources=list(source_files(project.parent))
  text=source_text(project)
  sdk=sdk_evidence(project)
  if name=='LocalFileOASIS':sdk.append('System.IO (native file adapter)')
  role=family(name,text+project.read_text(encoding='utf-8-sig'),rel)
  blockers=list(registry.get('providerBlockers',{}).get(name,[]));stub=len(re.findall(r'throw\s+new\s+NotImplementedException\s*\(',text))
  if stub:blockers.append(f'{stub} explicit unimplemented throw(s): shipping-path review needed')
  if not sources:blockers.append('No local C# implementation source; linked/generated sources and factory require review')
  e=evidence.get(name)
  if e:
   report=ROOT/e['report']
   if not report.is_file() or hashlib.sha256(report.read_bytes()).hexdigest()!=e['sha256']:raise ValueError('Missing/changed evidence: '+name)
   status=f"{e['passed']}/{e['total']} ({e['mode']}; {e['suite']})";blockers+=e.get('blockers',[])
   if e['mode']=='controlled':status='Controlled '+status;blockers.append('Controlled backend is not native product acceptance')
   if role!='Non-storage' and e['suite']!=registry.get('currentCoreSuite','core-storage-v1'):
    status='Historical '+status;blockers.append('Current '+registry['currentCoreSuite']+' not executed; older results are not re-labelled')
  else:status='Not run (capability-v1)' if role=='Non-storage' else 'Not run';blockers.append('Capability identity/activation/read-only probe pending; storage suite N/A' if role=='Non-storage' else 'Shared-suite adapter/run pending')
  if not sdk:blockers.append('Real SDK/API not yet source-reviewed')
  sdk_text='Yes - '+', '.join(sdk) if sdk else 'No - unverified'
  rows.append(f"| [{name}](../../../{rel.as_posix()}) | {role} | {sdk_text} | {status} | {'; '.join(blockers) or 'None recorded within suite scope'} |")
 header=f'''# Provider breadth-first status matrix

Generated from **{len(rows)} shipping provider projects** by `python Scripts/Providers/update_provider_matrix.py`.
Current roles: **{sum("| Non-storage |" in row for row in rows)} non-storage**, **{sum("| Non-storage |" not in row for row in rows)} storage/specialized-storage**. SDK/API source evidence remains unverified for **{sum("| No - unverified |" in row for row in rows)}** providers.

Current required storage suite: **{registry.get('currentCoreSuite','core-storage-v1')}**. Earlier-suite totals remain historical and do not establish a current-suite pass.

One row per provider project; tests/harness/demo and provider-template projects excluded. Alternate implementations remain separate rows. Non-storage providers require capability review rather than disappearing.

**SDK Yes means detected client construction/use, service HTTP calls, or a declared Node SDK imported by bridge source; it is not live acceptance or full implementation. No means unverified, not necessarily absent.** Referenced provider namespaces actually used by source are followed; unused project references do not earn SDK credit; package presence alone does not earn Yes. Non-storage products use capability-v1 identity/activation/read-only service checks, not storage CRUD. Persistent search/vector databases remain storage; chain indexers/maps/AI/wallet/auth services are Non-storage. Existing provider-specific test totals are NOT shared conformance passes.

`CONFORMANCE_RESULTS.json` holds hash-bound reports with suite/version and controlled/native mode. Until every applicable provider passes, global HyperDrive failover/load-balancing/replication is deferred; depth work follows afterward. No100% certification from this inventory. Initial v1 harness control: both sync/async model runs passed all52 contract checks; LocalFile native runs stopped at avatar-save acknowledgement (2/52), retained below. SQL90 unit passes include family-identity controls and are not shared native conformance.

Family order: EVM, SQL (Oracle priority), Document (AWS priority), key-value/object (AWS S3 priority), IPFS, non-EVM, remaining families. Time-box approximately one working day per family, record blockers then move on. No Polkadot/NEAR/SQLite depth work in this pass.

| Family | Shared owner | Breadth work | Time-box |
|---|---|---|---|
| EVM | Existing Nethereum Web3CoreOASISBaseProvider | Ethereum/Arbitrum/Polygon V2 share one base; 4factory controls/BootLoader build pass, 6native cases blocked by local config | Started 26 Oct; ~1 day |
| SQL | PostgreSqlFamilyProviderBase reusing canonical PostgreSQLOASIS / Npgsql | Cockroach/Timescale thin facades; PostgreSQL v1 native12/52 historical, detail read-count fixed/targeted2PASS; Oracle v2 native52/52PASS; other product-native runs pending | Foundation started 26 Oct; ~1 day |
| Document | Existing DynamoDB owner for AWS alias + shared record policy | AWS/Dynamo factories bound; 27factory controlsPASS, native runtime missing0/52 | Started 26 Oct; ~1 day |
| Key-value/object | Existing KeyValueStorageProviderBase / IKeyValueBackend / S3 transport | Redis v2 native52/52PASS; KeyDB/Valkey/Dragonfly/Garnet thin shared facades52/52CONTROLLED each/native pending; AWS S3 native config missing0/52 historical | Started 26 Oct; ~1 day |
| IPFS/content-addressed | Existing IPFS owner + encrypted record policy; distinct pin/Lotus transports | IPFS canonical binding compiled; 0/52 blocked by missing original repo/key; Pinata/Filecoin record-contract gaps recorded before extraction | Started 26 Oct; ~1 day |
| Non-EVM | Existing encrypted record/version/query helpers + native SDK transports | Solana canonical native4/52: activation/save pass, ID read fails; original fixtures preserved. Baseline only, no contract/fee/recovery depth | Started 26 Oct; ~1 day |
| Graph/federated/filesystem/other | Existing native owners | Classify storage applicability and register factories | Not started; ~1 day |
| Non-storage | NonStorageConformance / capability-v1 | Identity, honest capabilities, activation, read-only SDK/API probe, deactivation; controlled harness7PASS, native factories pending | Foundation started 26 Oct; ~1 day |

| Provider | Family | Real SDK/API source evidence | Applicable conformance passed | Blockers |
|---|---|---|---|---|
'''
 return header+'\n'.join(rows)+'\n'
if __name__=='__main__':
 parser=argparse.ArgumentParser();parser.add_argument('--check',action='store_true');args=parser.parse_args();target=DEST/'PROVIDER_MATRIX.md';content=build()
 if args.check:
  if not target.exists() or target.read_text(encoding='utf-8')!=content:raise SystemExit('Provider matrix stale: regenerate')
 else:target.write_text(content,encoding='utf-8')
 print('Provider matrix checked' if args.check else 'Provider matrix updated')
