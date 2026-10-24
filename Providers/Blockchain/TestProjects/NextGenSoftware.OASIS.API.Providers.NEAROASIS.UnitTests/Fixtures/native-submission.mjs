import {readFileSync} from 'node:fs';
import {submitNativeAction} from '../NearSdkBridge/native-mutation.mjs';
// Invocation policy control only; no SDK/provider/signer/network.
const input=JSON.parse(readFileSync(0,'utf8'));
if(input.operation!=='view')throw new Error('Unexpected policy test operation');
const action={controlled:true},outcome={status:{Failure:{ActionError:{index:0}}}};
let calls=0;
const account={async signAndSendTransaction(options){
  calls++;
  if(options.receiverId!=='receiver.near'||options.actions.length!==1||options.actions[0]!==action
    ||options.waitUntil!=='FINAL'||options.throwOnFailure!==false||options.retries!==0)
    throw new Error('Native submission policy differs');
  return outcome;
}};
const observed=await submitNativeAction(account,'receiver.near',action);
if(observed!==outcome||calls!==1)throw new Error('Original native result was changed or submission duplicated');
process.stdout.write(JSON.stringify({ok:true,result:{calls,nativeFailurePreserved:!!observed.status.Failure}}));
