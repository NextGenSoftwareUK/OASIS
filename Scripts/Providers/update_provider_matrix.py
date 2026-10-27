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
def family(name,text,path):
 if 'Nethereum' in text or 'Web3CoreOASIS' in text or name in {'EthereumOASIS','ArbitrumOASIS','PolygonOASIS'}:return 'EVM'
 if any(x in name.lower() for x in ['sql','oracle','cockroach','timescale','supabase','neon','duckdb','mariadb','tidb','yugabyte','singlestore']):return 'SQL'
 if any(x in name.lower() for x in ['ipfs','filecoin','arweave','pinata','storj','sia']):return 'IPFS/content-addressed'
 if name=='AWSOASIS' or 'DynamoDBOASIS' in text or any(x in name.lower() for x in ['mongo','cosmosdb','dynamodb','couch','firestore','documentdb','raven']):return 'Document'
 if 'KeyValueStorageProviderBase' in text or any(x in name.lower() for x in ['redis','valkey','keydb','memcache','s3','minio']):return 'Key-value/object'
 if path.parts[1]=='Blockchain':return 'Non-EVM chain'
 if any(x in name.lower() for x in ['neo4j','tigergraph','neptune','dgraph']):return 'Graph'
 if name in {'SOLIDOASIS','ActivityPubOASIS'}:return 'Federated web'
 if 'LocalFile' in name:return 'Filesystem'
 return 'Other / capability review'
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

def sdk_evidence(project,visited=None):
 visited=set() if visited is None else set(visited)
 if project in visited:return []
 visited.add(project);found=[n for n,pattern in SDK.items() if re.search(pattern,source_text(project))]
 for ref in ET.parse(project).findall('.//ProjectReference'):
  path=(project.parent/ref.attrib['Include'].replace('\\','/')).resolve()
  if path.is_file() and path.name.startswith(('NextGenSoftware.OASIS.API.Providers.','NextGenSoftware.OASIS.Providers.Shared.')):
   found+=sdk_evidence(path,visited)
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
  blockers=list(registry.get('providerBlockers',{}).get(name,[]));stub=len(re.findall(r'throw\s+new\s+NotImplementedException\s*\(',text))
  if stub:blockers.append(f'{stub} explicit unimplemented throw(s): shipping-path review needed')
  e=evidence.get(name)
  if e:
   report=ROOT/e['report']
   if not report.is_file() or hashlib.sha256(report.read_bytes()).hexdigest()!=e['sha256']:raise ValueError('Missing/changed evidence: '+name)
   status=f"{e['passed']}/{e['total']} ({e['mode']}; {e['suite']})";blockers+=e.get('blockers',[])
  else:status='Not run';blockers.append('Shared-suite adapter/run pending')
  if not sdk:blockers.append('Real SDK/API not yet source-reviewed')
  sdk_text='Yes - '+', '.join(sdk) if sdk else 'No - unverified'
  rows.append(f"| [{name}](../../../{rel.as_posix()}) | {family(name,text+project.read_text(encoding='utf-8-sig'),rel)} | {sdk_text} | {status} | {'; '.join(blockers) or 'None recorded within suite scope'} |")
 header=f'''# Provider breadth-first status matrix

Generated from **{len(rows)} shipping provider projects** by `python Scripts/Providers/update_provider_matrix.py`.
One row per provider project; tests/harness/demo and provider-template projects excluded. Alternate implementations remain separate rows. Non-storage providers require capability review rather than disappearing.

**SDK Yes is concrete client construction in current source, not live acceptance or full implementation. No means unverified, not necessarily absent.** Shared provider project references are followed to their source client constructions. Direct HTTP/JS SDK paths still require additional review. Existing provider-specific test totals are NOT shared conformance passes.

`CONFORMANCE_RESULTS.json` holds hash-bound reports with suite/version and controlled/native mode. Until every applicable provider passes, global HyperDrive failover/load-balancing/replication is deferred; depth work follows afterward. No100% certification from this inventory. Initial harness control: both sync/async model runs passed all52 contract checks; LocalFile native runs stopped at avatar-save acknowledgement (2/52), retained below. SQL90 unit passes include family-identity controls and are not shared native conformance.

Family order: EVM, SQL (Oracle priority), Document (AWS priority), key-value/object (AWS S3 priority), IPFS, non-EVM, remaining families. Time-box approximately one working day per family, record blockers then move on. No Polkadot/NEAR/SQLite depth work in this pass.

| Family | Shared owner | Breadth work | Time-box |
|---|---|---|---|
| EVM | Existing Nethereum Web3CoreOASISBaseProvider | Inventory/migrate duplicate chain adapters; one suite | Not started; ~1 day |
| SQL | PostgreSqlFamilyProviderBase reusing canonical PostgreSQLOASIS / Npgsql | Cockroach/Timescale thin facades; product-native runs pending | Foundation started 26 Oct; ~1 day |
| Document | Native document transports + shared record policy | Extract common policy without conflating native dialects | Not started; ~1 day |
| Key-value/object | Existing KeyValueStorageProviderBase / IKeyValueBackend / S3 transport | One suite for all backend factories | Suite implementation started; ~1 day |
| IPFS/content-addressed | Existing IPFS owner + shared record policy | Review pointer/content transport before extraction | Not started; ~1 day |
| Non-EVM | Shared record policy + native SDK transports | Baseline storage only; stop contract/fee/recovery depth | Not started; ~1 day |
| Graph/federated/filesystem/other | Existing native owners | Classify storage applicability and register factories | Not started; ~1 day |

| Provider | Family | Real SDK/API source evidence | Shared conformance passed | Blockers |
|---|---|---|---|---|
'''
 return header+'\n'.join(rows)+'\n'
if __name__=='__main__':
 parser=argparse.ArgumentParser();parser.add_argument('--check',action='store_true');args=parser.parse_args();target=DEST/'PROVIDER_MATRIX.md';content=build()
 if args.check:
  if not target.exists() or target.read_text(encoding='utf-8')!=content:raise SystemExit('Provider matrix stale: regenerate')
 else:target.write_text(content,encoding='utf-8')
 print('Provider matrix checked' if args.check else 'Provider matrix updated')
