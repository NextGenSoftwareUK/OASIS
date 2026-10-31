import {readFileSync} from 'node:fs';
import {pathToFileURL} from 'node:url';
import {decodeCallArguments} from '../NearSdkBridge/call-arguments.mjs';
// Real retained SDK action encoding only; no provider, signer, network or ledger.
const {actions}=await import(pathToFileURL(process.env.OASIS_NEAR_REAL_SDK_MODULE).href);
const input=JSON.parse(readFileSync(0,'utf8'));
if(input.operation==='view'&&input.methodName==='validate-arguments'){
  let valid=true;try{decodeCallArguments(input.args.encoded);}catch{valid=false;}
  process.stdout.write(JSON.stringify({ok:true,result:{valid}}));
}else{
if(input.operation!=='call')throw new Error('Unexpected SDK argument test operation');
const action=actions.functionCall(input.methodName,decodeCallArguments(input.argsBase64),BigInt(input.gas),BigInt(input.depositYocto));
const encoded=Buffer.from(action.functionCall.args).toString('base64');
if(encoded!==process.env.OASIS_NEAR_EXPECTED_ARGS_BASE64)throw new Error('Real SDK action bytes differ from original C# argument bytes');
const hash='controlled-sdk-action-transaction';
process.stdout.write(JSON.stringify({ok:true,result:{transactionHash:hash,outcome:{transaction_outcome:{id:hash},
  transaction:{signer_id:input.accountId,receiver_id:input.targetContractId,actions:[{FunctionCall:{
    method_name:input.methodName,deposit:input.depositYocto,gas:Number(input.gas),args:encoded}}]},
  final_execution_status:'FINAL',status:{SuccessValue:''}}}}));
}
