import {readFileSync} from 'node:fs';
// Controlled C# IPC contract only: no SDK, signing, chain or persistence.
const input=JSON.parse(readFileSync(0,'utf8'));
if(input.operation!=='call'||input.methodName!=='token_mint')throw new Error('Unexpected mint fixture operation');
const args=input.args;
const explicitContract=args.symbol.endsWith('-contract');
const symbol=explicitContract?args.symbol.slice(0,-9):args.symbol;
const expected=symbol==='owned-original-default'?'service.near':symbol==='owned-original-target'?'receiver.near':null;
if(input.targetContractId!==(explicitContract?'external.near':'contract.near'))throw new Error('Mint contract differs from admitted intent');
if(!expected||args.owner_id!==expected||args.amount!=='100000000')throw new Error('Mint recipient/amount/symbol differs from admitted intent');
const metadata=JSON.parse(args.metadata_json);
if(metadata.Title!=='original title'||metadata.Description!=='original description'||metadata.MetaData.zone!=='north'
  ||metadata.Tags.length!==1||metadata.Tags[0]!=='original')throw new Error('Mint metadata differs from admitted intent');
if(input.accountId!=='service.near'||input.privateKey!=='controlled-service-key')throw new Error('Configured mint authority changed');
const hash='controlled-original-mint-transaction';
process.stdout.write(JSON.stringify({ok:true,result:{transactionHash:hash,outcome:{transaction_outcome:{id:hash},
  transaction:{signer_id:input.accountId,receiver_id:input.targetContractId},final_execution_status:'FINAL',status:{SuccessValue:''}}}}));
