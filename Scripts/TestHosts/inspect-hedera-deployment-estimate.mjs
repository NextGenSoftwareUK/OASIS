import fs from 'node:fs';
import {Client,TransactionId} from './hedera-deployment-sdk.mjs';
import {restoreKey} from '../../Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.HashgraphOASIS/SdkBridge/bridge.mjs';
import {deploymentTransaction} from '../../Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.HashgraphOASIS/SdkBridge/deployment.mjs';
import {estimatedTinybars,quotedDeploymentTinybars} from './hedera-deployment-fee.mjs';

let input='';
for await(const chunk of process.stdin){input+=chunk;if(Buffer.byteLength(input)>16384)throw new Error('Bounded inspection input required.');}
const request=JSON.parse(input);input='';
if(request.network!=='testnet'||request.sender!=='0.0.10993779')throw new Error('Only the retained testnet identity may be inspected.');
const statePath=new URL('../../TestResults/HederaRuntime/deployment-store.json',import.meta.url);
const state=JSON.parse(fs.readFileSync(statePath,'utf8'));
if(state.sender!==request.sender||state.fileId!=='0.0.10994223'||state.contractId!=='0.0.11017885'||!state.verified)
  throw new Error('Retained deployment provenance mismatch.');
async function mirror(route){
  const response=await fetch('https://testnet.mirrornode.hedera.com/api/v1/'+route,{redirect:'error',signal:AbortSignal.timeout(20000)});
  if(!response.ok)throw new Error('Read-only fee inspection failed: HTTP '+response.status);
  return response.json();
}
const client=Client.forTestnet();
try {
  await client.updateNetwork();
  const key=restoreKey(request);
  if('0x'+key.publicKey.toEvmAddress()!==request.senderEvmAddress)throw new Error('Retained public signer mismatch.');
  const modes=[];
  for(const [name,mode] of [['INTRINSIC',1],['STATE',0]]){
    const transaction=deploymentTransaction('contractCreate',{...request,fileId:state.fileId,
      gas:2000000,maxFeeTinybars:'1200000000',transactionId:TransactionId.generate(request.sender).toString()},key.publicKey);
    // Mirror FeeEstimateQuery only: never execute/sign/broadcast this transaction.
    const fee=await transaction.estimateFee().setMode(mode).execute(client);
    const extras=fee.serviceFee.extras.map(extra=>({name:extra.name,count:extra.count,charged:extra.charged,
      feePerUnit:extra.feePerUnit.toString(),subtotal:extra.subtotal.toString()}));
    modes.push({name,tinycents:fee.total.toString(),extras});
  }
  const rates=await mirror('network/exchangerate');
  const fees=await mirror('network/fees');
  if(rates.current_rate.expiration_time<=Math.floor(Date.now()/1000))throw new Error('Current exchange rate expired.');
  const gasEntries=fees.fees.filter(fee=>fee.transaction_type==='ContractCreate');
  if(gasEntries.length!==1||!Number.isSafeInteger(gasEntries[0].gas)||gasEntries[0].gas<=0)throw new Error('Exact unique contract gas price unavailable.');
  const gasPrice=BigInt(gasEntries[0].gas);
  const quote=quotedDeploymentTinybars(modes[0].tinycents,rates.current_rate,2000000,gasPrice,modes[0].extras);
  const report={network:'testnet',fileId:state.fileId,contractId:state.contractId,modes,
    rate:rates.current_rate,gasPriceTinybars:gasPrice.toString(),gasQuoteTimestamp:fees.timestamp,
    maximumDeclaredGas:2000000,maximumGasChargeAtQuotedPrice:(gasPrice*2000000n).toString(),
    intrinsicTinybars:estimatedTinybars(modes[0].tinycents,rates.current_rate).toString(),
    combinedQuotedTinybars:quote.total.toString(),
    submitted:false,explanation:'Read-only estimates and current gas-price snapshot; not an enforceable all-in spend cap.'};
  fs.writeFileSync(new URL('../../TestResults/HederaRuntime/deployment-estimate-inspection.json',import.meta.url),JSON.stringify(report,null,2));
  console.log(JSON.stringify(report));
} finally {client.close();request.privateKey=null;}
