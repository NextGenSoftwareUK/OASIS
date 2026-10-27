import { readFileSync,writeFileSync } from 'node:fs';
// Controlled IPC boundary fixture only: no SDK, signing, network or persistence.
const input=JSON.parse(readFileSync(0,'utf8'));
if(input.operation==='call')input.args=JSON.parse(Buffer.from(input.argsBase64,'base64').toString('utf8'));
const statePath=process.env.OASIS_NEAR_NFT_FOLLOWUP_ROOT?process.env.OASIS_NEAR_NFT_FOLLOWUP_ROOT+'/state.json':null;
const state=statePath?JSON.parse(readFileSync(statePath,'utf8')):null;
let result;
if(input.operation==='probe')result={};
else if(input.operation==='transactionStatus'){
 const mode=process.env.OASIS_NEAR_NFT_SOURCE_MODE;
 if(input.transactionHash!=='source-lock'||input.signerAccountId!=='service.near')throw new Error('Source query identity differs');
 let args={token_id:'owned-test-token',locked:true,lock_id:'1'.repeat(32)};
 if(mode==='stale')args.lock_id='2'.repeat(32);if(mode==='id-format')args.lock_id='INVALID';if(mode==='missing-id')delete args.lock_id;
 if(mode==='token')args.token_id='another-token';if(mode==='unlock')args.locked=false;if(mode==='extra-arg')args.extra=true;
 const call={FunctionCall:{method_name:mode==='method'?'nft_transfer':'nft_set_locked',deposit:mode==='deposit'?'1':'0',gas:100000000000000,args:Buffer.from(JSON.stringify(args)).toString('base64')}};
 result={outcome:{transaction_outcome:{id:mode==='hash'?'other-original':input.transactionHash},
 transaction:{signer_id:mode==='signer'?'attacker.near':'service.near',receiver_id:mode==='contract'?'other.near':'contract.near',actions:mode==='multi'?[call,call]:[call]},
 final_execution_status:mode==='pending'?'EXECUTED':'FINAL',status:mode==='failed'?{Failure:{ActionError:{}}}:{SuccessValue:''}}};
}
else if(input.operation==='generateKey')result={publicKey:'ed25519:fixture-public',privateKey:'ed25519:fixture-private',implicitAccountId:'1'.repeat(64)};
else if(input.operation==='derivePublicKey')result={publicKey:'fixture-public-key',implicitAccountId:'1'.repeat(64)};
else if(input.operation==='deriveMnemonic'){
  if(input.seedPhrase!=='abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about')throw new Error('Unexpected synthetic mnemonic input.');
  result={publicKey:'fixture-public-key',privateKey:'controlled-owner-key',implicitAccountId:'1'.repeat(64)};
}
else if(input.operation==='view'&&input.methodName==='token_balance')result=process.env.OASIS_NEAR_TOKEN_TEST_ZERO==='1'?'0':'1000000000';
else if(input.operation==='transfer'||input.operation==='call'&&input.methodName==='token_transfer'){
  if(input.accountId!==process.env.OASIS_NEAR_TOKEN_TEST_ACCOUNT||input.privateKey!=='controlled-owner-key')throw new Error('Transfer signer was not forwarded.');
  if(input.operation==='call'&&input.args.from_id!==input.accountId)throw new Error('Transfer source differs from signer account.');
  result={transactionHash:'controlled-original-send-transaction'};
}
else if(input.operation==='call'&&input.methodName==='token_burn'){
  if(process.env.OASIS_NEAR_TOKEN_TEST_ZERO==='1')throw new Error('Insufficient balance must stop before mutation.');
  if(input.accountId!==process.env.OASIS_NEAR_TOKEN_TEST_ACCOUNT||input.privateKey!=='controlled-owner-key')throw new Error('Owner signer was not forwarded.');
  result={transactionHash:'controlled-original-token-transaction'};
}
else if(input.operation==='view'&&input.methodName==='nft_escrow_protocol')result=process.env.OASIS_NEAR_NFT_SOURCE_MODE==='protocol'?2:1;
else if(input.operation==='view'&&input.methodName==='nft_lock_get')result=state?.id===input.args.lock_id?{protocol:1,lock_id:state.id,token_id:'owned-test-token',owner_id:'service.near',released:!state.locked}:null;
else if(input.operation==='view'&&input.methodName==='nft_token')result=state?state.calls&&process.env.OASIS_NEAR_NFT_TEST_MODE==='missing'?null:
  {token_id:input.args.token_id,owner_id:'service.near',metadata_json:'{}',locked:state.calls&&process.env.OASIS_NEAR_NFT_TEST_MODE==='present'?!state.locked:state.locked,...(state.id?{lock_id:state.id}:{})}:
  process.env.OASIS_NEAR_NFT_TEST_MODE==='missing'?null:{token_id:input.args.token_id,owner_id:'service.near',metadata_json:'{}',locked:process.env.OASIS_NEAR_NFT_TEST_MODE==='locked'};
else if(input.operation==='call'&&process.env.OASIS_NEAR_NFT_SOURCE_MODE&&process.env.OASIS_NEAR_NFT_SOURCE_MODE!=='valid')throw new Error('INVALID SOURCE REACHED NATIVE MUTATION');
else if(input.operation==='call'&&['nft_transfer','nft_burn','nft_set_locked'].includes(input.methodName))result={transactionHash:input.methodName==='nft_set_locked'&&process.env.OASIS_NEAR_NFT_TEST_MODE==='transfer-failed'?'controlled-original-unlock':'controlled-original-nft-transaction'};
else throw new Error('Unexpected fixture operation; wallet followup must not precede verified ownership.');
if(input.operation==='call'&&input.methodName==='nft_set_locked'&&state){state.id=input.args.lock_id;state.locked=input.args.locked;state.calls++;writeFileSync(statePath,JSON.stringify(state));}
if(result?.transactionHash)result.outcome={transaction_outcome:{id:result.transactionHash},
  transaction:{signer_id:input.accountId,receiver_id:input.operation==='transfer'?input.receiverId:input.targetContractId,
    actions:[input.operation==='transfer'?{Transfer:{deposit:input.amountYocto}}:{FunctionCall:{method_name:input.methodName,
      deposit:input.depositYocto,gas:Number(input.gas),args:input.argsBase64}}]},
  final_execution_status:'FINAL',status:{SuccessValue:''}};
process.stdout.write(JSON.stringify({ok:true,result}));
