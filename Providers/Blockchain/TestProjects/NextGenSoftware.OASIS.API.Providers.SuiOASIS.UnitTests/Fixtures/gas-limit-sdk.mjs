// Controlled child transport; reuses the retained official SDK and production executor. No RPC/ledger.
import assert from 'node:assert/strict';
import {createRequire} from 'node:module';
import {pathToFileURL} from 'node:url';
const bridge=process.env.OASIS_SUI_TEST_GAS_LIMIT_REAL_BRIDGE;
assert.ok(bridge,'Retained production bridge required');
const resolve=createRequire(pathToFileURL(bridge));
const {Ed25519Keypair}=await import(pathToFileURL(resolve.resolve('@mysten/sui/keypairs/ed25519')).href);
const {Transaction}=await import(pathToFileURL(resolve.resolve('@mysten/sui/transactions')).href);
const {toBase58}=await import(pathToFileURL(resolve.resolve('@mysten/sui/utils')).href);
const {execute,withNativeMutationEvidence,sdkErrorEnvelope}=await import(pathToFileURL(bridge).href);
let input='';for await(const chunk of process.stdin)input+=chunk;
const request=JSON.parse(input);
assert.equal(request.operation,'put');assert.equal(request.key,'fee-limit-fixture');assert.equal(request.privateKey,'');
assert.equal(request.rpcEndpoint,'http://127.0.0.1:1');assert.equal(process.env.OASIS_NATIVE_PREPARATION_PIPE,undefined);
assert.ok(['999','1000'].includes(request.maximumGasBudgetMist),'C# configured ceiling must reach the child unchanged');
const signer=Ed25519Keypair.fromSecretKey(new Uint8Array(32).fill(7));
const tx=new Transaction();tx.setSender(signer.toSuiAddress());tx.setGasPrice(1);tx.setGasBudget(1000);
tx.setGasPayment([{objectId:'0x'+'2'.repeat(64),version:'1',digest:toBase58(new Uint8Array(32).fill(1))}]);
let signatures=0,transportCalls=0;const sign=signer.signTransaction.bind(signer);
signer.signTransaction=async bytes=>{signatures++;return sign(bytes);};
try{
 const result=await withNativeMutationEvidence(()=>execute({async executeTransaction(value){
  transportCalls++;const original=Transaction.from(value.transaction);
  assert.equal(original.getData().gasData.budget,'1000');
  return {Transaction:{digest:await original.getDigest(),status:{success:true}}};
 },async waitForTransaction(){}},tx,signer),undefined,request.maximumGasBudgetMist);
 assert.equal(request.maximumGasBudgetMist,'1000');assert.equal(signatures,1);assert.equal(transportCalls,1);
 process.stdout.write(JSON.stringify({ok:true,result:{transactionHash:result.digest,signatures,transportCalls,ceiling:request.maximumGasBudgetMist}}));
}catch(error){
 assert.equal(request.maximumGasBudgetMist,'999');assert.match(error.message,/gas budget exceeds/);
 assert.equal(signatures,0);assert.equal(transportCalls,0);
 const envelope=sdkErrorEnvelope(error);assert.equal(envelope.nativeMutation,undefined);
 envelope.error+=';signatures=0;transportCalls=0;ceiling=999';
 process.stdout.write(JSON.stringify(envelope));process.exitCode=1;
}
