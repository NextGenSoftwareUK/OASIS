import {readFileSync,writeFileSync,appendFileSync} from 'node:fs';import {join} from 'node:path';
// Controlled process responses only: no SDK/signature/native-chain/financial proof.
const root=process.env.OASIS_NEAR_ESCROW_FIXTURE_ROOT,input=JSON.parse(readFileSync(0,'utf8'));
const path=join(root,'state.json'),state=JSON.parse(readFileSync(path,'utf8'));let result;
appendFileSync(join(root,'calls.ndjson'),JSON.stringify({operation:input.operation,method:input.methodName??null,account:input.accountId??null})+'\n');
if(input.operation==='probe')result={};
else if(input.operation==='derivePublicKey'){
 if(input.secretKey!=='owner-key')throw new Error('Actual requested signer key was not selected');
 result={publicKey:'ed25519:fixture-public',implicitAccountId:'1'.repeat(64)};
}else if(input.operation==='view'){
 if(input.targetContractId!=='contract.near')throw new Error('Read selected another contract');
 if(input.methodName==='token_lock_get'&&input.args.lock_id!=='1'.repeat(32))throw new Error('Read selected another deposit');
 if(['token_get','token_balance'].includes(input.methodName)&&input.args.symbol!=='OWN')throw new Error('Read selected another token');
 if(input.methodName==='token_balance'&&input.args.account_id!=='holder.near')throw new Error('Read selected another holder balance');
 switch(input.methodName){
  case 'token_escrow_protocol':result=state.mode==='protocol'?2:1;break;
  case 'token_lock_get':{
   result=state.deposit===null?null:structuredClone(state.deposit);
   if(state.calls&&result){if(state.mode==='post-id')result.lock_id='2'.repeat(32);if(state.mode==='post-owner')result.owner_id='attacker.near';if(state.mode==='post-amount')result.amount='1';if(state.mode==='post-state')result.released=!result.released;if(state.mode==='post-extra')result.extra=true;}break;
  }
  case 'token_balance':result=state.mode==='balance-format'?'01000000000':state.calls&&state.mode==='post-balance'?'1':state.balance;break;
  case 'token_get':result={symbol:'OWN',issuer_id:state.mode==='issuer'?'attacker.near':'holder.near',locked:state.calls&&state.mode==='post-freeze'?!state.frozen:state.frozen};break;
  default:throw new Error('Unexpected controlled read');
 }
}else if(input.operation==='call'){
 if(input.accountId!=='holder.near'||input.privateKey!=='owner-key'||input.targetContractId!=='contract.near')throw new Error('Signer/contract replaced by provider defaults');
 const args=JSON.parse(Buffer.from(input.argsBase64,'base64').toString('utf8'));if(args.symbol!=='OWN')throw new Error('Caller symbol changed after capture');
 if(input.methodName==='token_lock'||input.methodName==='token_release'){
  if(args.lock_id!=='1'.repeat(32))throw new Error('Caller deposit identity changed');
  if(input.methodName==='token_lock'&&args.amount!=='250000000')throw new Error('Exact caller amount changed');
 }else if(input.methodName!=='token_set_locked')throw new Error('Unexpected native operation');
 state.calls++;
 if(state.mode!=='reject'){
  if(input.methodName==='token_lock'){state.deposit={protocol:1,lock_id:args.lock_id,symbol:args.symbol,owner_id:input.accountId,amount:args.amount,released:false};state.balance=(BigInt(state.balance)-BigInt(args.amount)).toString();}
  else if(input.methodName==='token_release'){state.deposit.released=true;state.balance=(BigInt(state.balance)+BigInt(state.deposit.amount)).toString();}
  else state.frozen=args.locked;
 }
 writeFileSync(path,JSON.stringify(state));
 result={transactionHash:'controlled-token-escrow-original',outcome:{transaction_outcome:{id:'controlled-token-escrow-original'},
  transaction:{signer_id:input.accountId,receiver_id:input.targetContractId,actions:[{FunctionCall:{method_name:input.methodName,deposit:input.depositYocto,gas:Number(input.gas),args:input.argsBase64}}]},
  final_execution_status:'FINAL',status:state.mode==='reject'?{Failure:{ActionError:{index:0}}}:{SuccessValue:''}}};
}else throw new Error('Unexpected controlled operation');
process.stdout.write(JSON.stringify({ok:true,result}));
