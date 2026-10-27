import {readFileSync,existsSync} from 'node:fs';
import {createHash} from 'node:crypto';
import {spawnSync} from 'node:child_process';
const repo='/mnt/c/Source/OASIS',retained='/var/tmp/oasis-near-evidence';
const endpoint='http://127.0.0.1:3030',chain='oasis-near-local',account='test.near';
if(process.env.OASIS_NEAR_PREPARED_STORAGE_RECOVERY!=='1'&&existsSync(`${repo}/TestResults/NearRuntime/${process.env.OASIS_NEAR_NFT_DEPOSIT==='1'?'near-prepared-nft-deposit':process.env.OASIS_NEAR_PREPARED_TOKEN==='1'?'near-prepared-token':process.env.OASIS_NEAR_PREPARED_LIFECYCLE==='1'?'near-prepared-lifecycle':process.env.OASIS_NEAR_PREPARED_STORAGE==='1'?'near-prepared-storage':process.env.OASIS_NEAR_PREPARED_ORDINARY==='1'?'near-prepared-ordinary':process.env.OASIS_NEAR_PREPARED_ACTION==='1'?'near-prepared-native':process.env.OASIS_NEAR_ORDINARY_WRITE==='1'?'near-ordinary-storage':'near-signed-ack'}.pending.json`))throw new Error('Original signed acknowledgement pending; no replay');
async function rpc(method,params){
  const response=await fetch(endpoint,{method:'POST',headers:{'content-type':'application/json'},
    body:JSON.stringify({jsonrpc:'2.0',id:'signed-ack-admission',method,params}),signal:AbortSignal.timeout(15000)});
  if(!response.ok)throw new Error('Retained RPC admission failed');
  const body=await response.json();if(body.error||body.result===undefined)throw new Error('Retained RPC admission rejected');return body.result;
}
if((await rpc('status',[])).chain_id!==chain)throw new Error('Retained chain identity differs');
const code=await rpc('query',{request_type:'view_code',account_id:account,finality:'final'});
const sha=value=>createHash('sha256').update(value).digest('hex');
if(typeof code.code_base64!=='string'||sha(Buffer.from(code.code_base64,'base64'))!==sha(readFileSync(`${retained}/build/oasis_storage.wasm`)))
  throw new Error('Retained deployed code differs; no upload/replacement');
const signer=JSON.parse(readFileSync(`${retained}/ledger/validator_key.json`,'utf8'));
if(signer.account_id!==account||typeof signer.secret_key!=='string')throw new Error('Retained signer identity differs');
const settings={NEAROASIS_RPCENDPOINT:endpoint,NEAROASIS_NETWORKID:chain,NEAROASIS_CHAINID:chain,
  NEAROASIS_CONTRACTADDRESS:account,NEAROASIS_ACCOUNTID:account,NEAROASIS_PRIVATEKEY:signer.secret_key,
  OASIS_NEAR_NODE:'C:\\Program Files\\nodejs\\node.exe',
  OASIS_NEAR_SDK_BRIDGE:'C:\\Source\\OASIS\\Providers\\Blockchain\\NextGenSoftware.OASIS.API.Providers.NEAROASIS\\SdkBridge\\bridge.mjs'};
// Credentials only through the owned child stdin, never argv/files/log output.
const child=spawnSync('/mnt/c/Windows/System32/WindowsPowerShell/v1.0/powershell.exe',
  ['-NoProfile','-NonInteractive','-ExecutionPolicy','Bypass','-File',
    'C:\\Source\\OASIS\\Scripts\\TestHosts\\near-local\\run-dotnet-evidence.ps1',
    '-SettingsFromStdin','-RepositoryRoot','C:\\Source\\OASIS',
    process.env.OASIS_NEAR_NFT_DEPOSIT==='1'?'-PreparedNftDepositActions':process.env.OASIS_NEAR_PREPARED_TOKEN==='1'?'-PreparedTokenActions':process.env.OASIS_NEAR_PREPARED_LIFECYCLE==='1'?'-PreparedLifecycleActions':process.env.OASIS_NEAR_PREPARED_STORAGE_RECOVERY==='1'?'-ReconcilePreparedStorage':process.env.OASIS_NEAR_PREPARED_STORAGE==='1'?'-PreparedStorageActions':process.env.OASIS_NEAR_PREPARED_ORDINARY==='1'?'-PreparedOrdinaryActions':process.env.OASIS_NEAR_PREPARED_ACTION==='1'?'-PreparedNativeActions':process.env.OASIS_NEAR_ORDINARY_WRITE==='1'?'-OrdinaryRecordWrites':process.env.OASIS_NEAR_CHECKED_WRITE==='1'?'-CheckedRecordWrites':'-SignedAcknowledgements'],
  {input:JSON.stringify(settings),stdio:['pipe','inherit','inherit']});
if(child.error||child.status!==0)throw new Error('Signed acknowledgement native verification failed; preserve original journal, no replay');
