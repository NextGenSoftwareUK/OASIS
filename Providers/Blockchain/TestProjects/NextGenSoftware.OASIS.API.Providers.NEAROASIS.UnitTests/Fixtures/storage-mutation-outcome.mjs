import {readFileSync} from 'node:fs';
const input=JSON.parse(readFileSync(0,'utf8'));
const legacy=['put','delete'].includes(input.operation);
const method=legacy?(input.operation==='put'?'put':'delete_record'):input.methodName;
if(!['put','delete_record'].includes(method))throw new Error('Unexpected storage mutation');
const args=legacy?(method==='put'?{key:input.key,value:input.value}:{key:input.key}):JSON.parse(Buffer.from(input.argsBase64,'base64').toString('utf8'));
if(args.key!=='holon:11111111-1111-1111-1111-111111111111')throw new Error('Wrong admitted storage key');
if(method==='put'&&(!args.value||args.value.includes('private-plaintext')))throw new Error('Storage ciphertext required');
const action={FunctionCall:{method_name:method,deposit:'0',gas:100000000000000,args:Buffer.from(JSON.stringify(args)).toString('base64')}};
const outcome={transaction_outcome:{id:'original-storage-transaction'},transaction:{signer_id:input.accountId,
  receiver_id:input.contractId,actions:[action]},final_execution_status:'FINAL',status:{SuccessValue:''}};
const result={transactionHash:'original-storage-transaction',outcome};
const scenario=Number(process.env.OASIS_NEAR_STORAGE_MUTATION_CASE);
switch(scenario){
  case 1:outcome.status={Failure:{ActionError:{index:0}}};break;
  case 2:delete result.outcome;break;
  case 3:outcome.final_execution_status='EXECUTED';break;
  case 4:outcome.transaction_outcome.id='different';break;
  case 5:outcome.transaction.signer_id='different.near';break;
  case 6:outcome.transaction.receiver_id='different.near';break;
  case 7:action.FunctionCall.method_name='different_method';break;
  case 8:action.FunctionCall.args=Buffer.from(JSON.stringify({...args,key:'other-record'})).toString('base64');break;
  case 9:action.FunctionCall.gas=1;break;
  case 10:outcome.transaction.actions.push(structuredClone(action));break;
  case 11:process.stdout.write(JSON.stringify({ok:false,error:'controlled lost outcome'}));process.exit(1);
}
process.stdout.write(JSON.stringify({ok:true,result}));
