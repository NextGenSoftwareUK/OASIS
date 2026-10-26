import {readFileSync} from 'node:fs';
import {pathToFileURL} from 'node:url';
const input=JSON.parse(readFileSync(0,'utf8')),mode=process.env.OASIS_NEAR_PREPARED_CASE;
const hash='controlled-near-original';let result;
if(input.operation==='prepareNativeAction'){
  const {chainId,sender,...intent}=input.request;
  if(mode==='prepared-mismatch')intent.recipient='different.near';
  result={protocol:'oasis.near.native-action.preparation.v1',chainId,sender,transactionHash:hash,
    signedTransactionBase64:'AQID',signDocBase64:'BAUG',signature:null,intent};
} else if(input.operation==='verifyNativePreparation'){
  const p=input.preparation;result={verified:true,chainId:p.chainId,sender:p.sender,transactionHash:p.transactionHash,intent:p.intent};
} else if(['submitNativePreparation','call','transfer','putRecordChecked'].includes(input.operation)){
  if(input.operation!=='submitNativePreparation'){
    const intent=input.operation==='transfer'?{kind:'transfer',recipient:input.receiverId,depositUnits:input.amountYocto}:
      {kind:'functionCall',recipient:input.targetContractId??input.contractId,depositUnits:input.depositYocto??'0',
        methodName:input.operation==='putRecordChecked'?'put_record_checked':input.methodName,gasUnits:'100000000000000',
        argsBase64:input.argsBase64};
    if(mode==='prepared-mismatch')intent.recipient='different.near';
    input.preparation={protocol:'oasis.near.native-action.preparation.v1',chainId:input.networkId,sender:input.accountId,
      transactionHash:hash,signedTransactionBase64:'AQID',signDocBase64:'BAUG',signature:null,intent};
  }
  if(!process.env.OASIS_NATIVE_PREPARATION_PIPE||!process.env.OASIS_NATIVE_PREPARATION_MODULE)throw new Error('Owner pipe missing');
  const {preparationClient}=await import(pathToFileURL(process.env.OASIS_NATIVE_PREPARATION_MODULE).href);
  await preparationClient(process.env.OASIS_NATIVE_PREPARATION_PIPE)(input.preparation);
  if(mode==='transport'){process.stdout.write(JSON.stringify({ok:false,error:'controlled lost original response'}));process.exit(1);}
  const p=input.preparation,i=p.intent;
  const action=i.kind==='transfer'?{Transfer:{deposit:i.depositUnits}}:{FunctionCall:{method_name:i.methodName,
    deposit:i.depositUnits,gas:Number(i.gasUnits),args:mode==='outcome-mismatch'?Buffer.from('{"different":true}').toString('base64'):i.argsBase64}};
  result={transactionHash:mode==='reply-mismatch'?'different-original':hash,persisted:true,submitted:true,outcome:{
    transaction_outcome:{id:hash},transaction:{signer_id:p.sender,receiver_id:i.recipient,actions:[action]},final_execution_status:'FINAL',
    status:mode==='native-failed'?{Failure:{ActionError:{index:0}}}:{SuccessValue:''}}};
  if(input.operation==='putRecordChecked'){
    result.finalExecutionStatus=result.outcome.final_execution_status;
    result.nativeStatus=mode==='projection-mismatch'?{SuccessValue:'wrong'}:result.outcome.status;
  }
} else throw new Error('Unexpected prepared action fixture operation');
process.stdout.write(JSON.stringify({ok:true,result}));
