// Actual retained CosmJS signing and production guard, controlled transport only. No RPC/ledger.
import assert from 'node:assert/strict';
import {createRequire} from 'node:module';
import {pathToFileURL} from 'node:url';
import {createHash} from 'node:crypto';
const bridge=process.env.OASIS_COSMOS_TEST_FEE_LIMIT_REAL_BRIDGE;
assert.ok(bridge,'Retained production SDK required');
const resolve=createRequire(pathToFileURL(bridge));
const {DirectSecp256k1Wallet}=await import(pathToFileURL(resolve.resolve('@cosmjs/proto-signing')).href);
const {SigningCosmWasmClient}=await import(pathToFileURL(resolve.resolve('@cosmjs/cosmwasm')).href);
const {AuthInfo,SignDoc,TxRaw}=await import(pathToFileURL(resolve.resolve('cosmjs-types/cosmos/tx/v1beta1/tx')).href);
const {guardCosmosSigningClient,withCosmosNativeEvidence,cosmosErrorEnvelope,verifyCosmosPreparedIdentity}=await import(new URL('transaction-results.mjs',pathToFileURL(bridge)).href);
let input='';for await(const chunk of process.stdin)input+=chunk;
const request=JSON.parse(input);
assert.equal(request.operation,'put');assert.equal(request.key,'fee-limit-fixture');
assert.equal(request.rpcEndpoint,'http://127.0.0.1:1');assert.equal(process.env.OASIS_NATIVE_PREPARATION_PIPE,undefined);
assert.deepEqual(Object.keys(request.maximumFeeUnitsByDenomination),['stake']);
const ceiling=request.maximumFeeUnitsByDenomination.stake;assert.ok(['5','6'].includes(ceiling));
const wallet=await DirectSecp256k1Wallet.fromKey(new Uint8Array(32).fill(7),'wasm');
const [{address}]=await wallet.getAccounts();const client=await SigningCosmWasmClient.offline(wallet);
const initial=await client.sign(address,[],{amount:[{denom:'stake',amount:'3'}],gas:'100000'},'',{accountNumber:7,sequence:3,chainId:'controlled-chain'});
const auth=AuthInfo.decode(initial.authInfoBytes);auth.tip={amount:[{denom:'stake',amount:'3'}],tipper:address};
const doc=SignDoc.fromPartial({bodyBytes:initial.bodyBytes,authInfoBytes:AuthInfo.encode(auth).finish(),chainId:'controlled-chain',accountNumber:7n});
const signed=await wallet.signDirect(address,doc);
const bytes=TxRaw.encode({...initial,authInfoBytes:signed.signed.authInfoBytes,signatures:[Buffer.from(signed.signature.signature,'base64')]}).finish();
const hash=createHash('sha256').update(bytes).digest('hex').toUpperCase();
assert.equal(verifyCosmosPreparedIdentity({transactionHash:hash,sender:address,chainId:'controlled-chain',signedTransactionBase64:Buffer.from(bytes).toString('base64'),signDocBase64:Buffer.from(SignDoc.encode(signed.signed).finish()).toString('base64')}).verified,true);
let preparations=0,broadcasts=0;
const transport={async broadcastTx(original){broadcasts++;assert.deepEqual(original,bytes);return {transactionHash:hash,height:42,code:0};},async signAndBroadcast(){throw new Error('Unused');}};
guardCosmosSigningClient(transport,async evidence=>{preparations++;assert.equal(evidence.transactionHash,hash);},request.maximumFeeUnitsByDenomination);
try{
 const result=await withCosmosNativeEvidence(()=>transport.broadcastTx(bytes));
 assert.equal(ceiling,'6');assert.equal(preparations,1);assert.equal(broadcasts,1);
 process.stdout.write(JSON.stringify({ok:true,result:{transactionHash:result.transactionHash,preparations,broadcasts,ceiling}}));
}catch(error){
 assert.equal(ceiling,'5');assert.match(error.message,/fee and tip exceed/);assert.equal(preparations,0);assert.equal(broadcasts,0);
 const envelope=cosmosErrorEnvelope(error);assert.equal(envelope.nativeMutation,undefined);
 envelope.error+=';preparations=0;broadcasts=0;ceiling=5';process.stdout.write(JSON.stringify(envelope));process.exitCode=1;
}
