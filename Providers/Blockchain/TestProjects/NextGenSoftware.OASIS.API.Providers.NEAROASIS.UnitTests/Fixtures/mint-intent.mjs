import {readFileSync} from 'node:fs';
// Controlled C# IPC contract only: no SDK, signing, chain or persistence.
const input=JSON.parse(readFileSync(0,'utf8'));
if(input.operation==='call')input.args=JSON.parse(Buffer.from(input.argsBase64,'base64').toString('utf8'));
if(input.operation!=='call'||!['token_mint','nft_mint'].includes(input.methodName))throw new Error('Unexpected mint fixture operation');
const args=input.args;
const metadata=JSON.parse(args.metadata_json);
const nft=input.methodName==='nft_mint';
const requestedSymbol=nft?metadata.Symbol:args.symbol;
const explicitContract=requestedSymbol.endsWith('-contract');
const symbol=explicitContract?requestedSymbol.slice(0,-9):requestedSymbol;
const expected=symbol==='owned-original-default'?'service.near':symbol==='owned-original-target'?'receiver.near':null;
if(input.targetContractId!==(explicitContract?'external.near':'contract.near'))throw new Error('Mint contract differs from admitted intent');
if(!expected||args.owner_id!==expected||(!nft&&args.amount!=='100000000'))throw new Error('Mint recipient/amount/symbol differs from admitted intent');
if(metadata.Title!=='original title'||metadata.Description!=='original description'||metadata.MetaData.zone!=='north'
  ||metadata.Tags.length!==1||metadata.Tags[0]!=='original')throw new Error('Mint metadata differs from admitted intent');
if(input.accountId!=='service.near'||input.privateKey!=='controlled-service-key')throw new Error('Configured mint authority changed');
const hash='controlled-original-mint-transaction';
process.stdout.write(JSON.stringify({ok:true,result:{transactionHash:hash,outcome:{transaction_outcome:{id:hash},
  transaction:{signer_id:input.accountId,receiver_id:input.targetContractId,actions:[{FunctionCall:{method_name:input.methodName,
    deposit:input.depositYocto,gas:Number(input.gas),args:input.argsBase64}}]},final_execution_status:'FINAL',status:{SuccessValue:''}}}}));
