import {createHash} from 'node:crypto';
import {actions,baseEncode,encodeTransaction,SignedTransaction,JsonRpcProvider} from 'near-api-js';
import {decodeCallArguments} from './call-arguments.mjs';

export const deploymentProtocol='oasis.near.code-deployment.preparation.v1';
export const preparationProtocol='oasis.near.native-action.preparation.v1';
const maximum=1048576; // Existing shared preparation pipe/envelope budget.
function base64(value){
  if(typeof value!=='string'||!value||value.length>maximum)throw new Error('Bounded original base64 required.');
  const bytes=Buffer.from(value,'base64');
  if(!bytes.length||bytes.length>maximum||bytes.toString('base64')!==value)throw new Error('Canonical original base64 required.');
  return bytes;
}
function accountId(value){
  // near/nearcore docs/DataStructures/Account.md: bounded lower-case parts,
  // no adjacent/prefix/suffix separators; system is a receipt-only identity.
  if(typeof value!=='string'||value==='system'||value.length<2||value.length>64||!/^([a-z0-9]+[._-])*[a-z0-9]+$/.test(value))throw new Error('Canonical NEAR account ID required.');
  return value;
}
function units(value,bits,positive=false){
  if(typeof value!=='string'||!/^(0|[1-9][0-9]*)$/.test(value)||BigInt(value)>=(1n<<BigInt(bits))||(positive&&BigInt(value)===0n))
    throw new Error('Exact bounded native integer required.');
  return value;
}
function network(value){
  if(typeof value!=='string'||!value||value.length>128||value.trim()!==value)throw new Error('Explicit NEAR network identity required.');
  return value;
}
export function nativeActionIntent(request){
  const recipient=accountId(request.recipient),depositUnits=units(request.depositUnits,128);
  if(request.kind==='transfer'){
    if(request.methodName!==undefined||request.argsBase64!==undefined||request.gasUnits!==undefined)throw new Error('Transfer cannot carry contract-call intent.');
    return {kind:'transfer',recipient,depositUnits};
  }
  if(request.kind!=='functionCall'||typeof request.methodName!=='string'||!request.methodName||Buffer.byteLength(request.methodName)>256)
    throw new Error('Explicit supported native action required.');
  const gasUnits=units(request.gasUnits,64,true);
  const args=decodeCallArguments(request.argsBase64);
  return {kind:'functionCall',recipient,depositUnits,methodName:request.methodName,gasUnits,argsBase64:Buffer.from(args).toString('base64')};
}
function decodedIntent(transaction){
  if(!Array.isArray(transaction.actions)||transaction.actions.length!==1)throw new Error('Exactly one native action required.');
  return nearActionIntent(transaction.receiverId,transaction.actions[0]);
}
export function nearActionIntent(receiverId,action){
  const keys=Object.keys(action).filter(key=>key!=='enum');
  if((Object.hasOwn(action,'enum')&&action.enum!==keys[0])||keys.length!==1)throw new Error('Ambiguous native action.');
  if(keys[0]==='transfer')return nativeActionIntent({kind:'transfer',recipient:receiverId,depositUnits:action.transfer.deposit.toString()});
  if(keys[0]!=='functionCall')throw new Error('Unreviewed native action family.');
  const call=action.functionCall;
  return nativeActionIntent({kind:'functionCall',recipient:receiverId,depositUnits:call.deposit.toString(),methodName:call.methodName,
    gasUnits:call.gas.toString(),argsBase64:Buffer.from(call.args).toString('base64')});
}
function verifyOriginal(preparation,expectedNetwork,expectedIntent,deployment){
  const original=structuredClone(preparation);
  if(Buffer.byteLength(JSON.stringify(original))>maximum)throw new Error('Original exceeds the shared preparation envelope budget.');
  if(original.protocol!==(deployment?deploymentProtocol:preparationProtocol)||original.chainId!==network(expectedNetwork))throw new Error('Original protocol/network differs.');
  const bytes=base64(original.signedTransactionBase64),unsigned=base64(original.signDocBase64);
  const signed=SignedTransaction.decode(bytes),transaction=signed.transaction;
  const encoded=Buffer.from(encodeTransaction(transaction));
  const hash=createHash('sha256').update(encoded).digest();
  if(!Buffer.from(signed.encode()).equals(bytes)||!(deployment?hash:encoded).equals(unsigned))throw new Error('Original Borsh bytes/signing document differ.');
  if(baseEncode(hash)!==original.transactionHash||accountId(transaction.signerId)!==original.sender)throw new Error('Original transaction hash/signer differs.');
  const tags=['ed25519Signature','secp256k1Signature','mlDsa65Signature'];
  const signature=signed.signature,tag=tags[transaction.publicKey.keyType];
  if(!tag||Object.keys(signature).length!==1||!signature[tag])throw new Error('Original signature algorithm differs from public key.');
  const signatureBytes=Buffer.from(signature[tag].data);
  // The canonical Borsh original already contains the entire native signature.
  // Do not duplicate it in Core's optional Signature field (ML-DSA-65 would
  // exceed that field's4096character bound). All algorithms use this one format.
  if(original.signature!==null||!transaction.publicKey.verify(hash,signatureBytes))throw new Error('Original signature verification failed.');
  if(transaction.blockHash.length!==32||transaction.nonce<=0n||transaction.nonce>=(1n<<64n))throw new Error('Invalid original checkpoint/nonce.');
  const intent=deployment?decodedDeployment(transaction):decodedIntent(transaction);
  const publicKey=transaction.publicKey.toString();
  if(original.publicKey!==undefined&&original.publicKey!==publicKey)throw new Error('Declared public key differs from signed key.');
  if(original.intent!==undefined&&JSON.stringify(original.intent)!==JSON.stringify(intent))throw new Error('Declared original intent differs from signed action.');
  if(expectedIntent!==undefined&&JSON.stringify(deployment?deploymentIntent(expectedIntent):nativeActionIntent(expectedIntent))!==JSON.stringify(intent))throw new Error('Signed action differs from admitted intent.');
  return {verified:true,transactionHash:original.transactionHash,chainId:original.chainId,sender:original.sender,
    publicKey,checkpointHash:baseEncode(Uint8Array.from(transaction.blockHash)),nonce:transaction.nonce.toString(),intent};
}
export function verifyNearSignedOriginal(preparation,expectedNetwork,expectedIntent){
  return verifyOriginal(preparation,expectedNetwork,expectedIntent,false);
}
export function verifyNearDeploymentOriginal(preparation,expectedNetwork,expectedIntent){
  return verifyOriginal(preparation,expectedNetwork,expectedIntent,true);
}
function deploymentIntent(value){
  if(value.kind!=='deployContract'||!Number.isSafeInteger(value.codeBytes)||value.codeBytes<=0
    ||typeof value.codeSha256!=='string'||! /^[0-9a-f]{64}$/.test(value.codeSha256))throw new Error('Exact deployment code identity required.');
  return {kind:'deployContract',recipient:accountId(value.recipient),codeBytes:value.codeBytes,codeSha256:value.codeSha256};
}
function decodedDeployment(transaction){
  if(transaction.receiverId!==transaction.signerId||!Array.isArray(transaction.actions)||transaction.actions.length!==1)
    throw new Error('Code-only deployment must target its signing account.');
  const action=transaction.actions[0],keys=Object.keys(action).filter(key=>key!=='enum');
  if(keys.length!==1||keys[0]!=='deployContract'||Object.hasOwn(action,'enum')&&action.enum!=='deployContract')throw new Error('Exactly one deployment action required.');
  const code=Buffer.from(action.deployContract.code);
  return deploymentIntent({kind:'deployContract',recipient:transaction.receiverId,codeBytes:code.length,codeSha256:createHash('sha256').update(code).digest('hex')});
}
export async function verifyNearPreparation(provider,preparation,expectedNetwork,expectedIntent){
  return verifyCheckpoint(provider,preparation,expectedNetwork,expectedIntent,false);
}
export async function verifyNearDeployment(provider,preparation,expectedNetwork,expectedIntent){
  return verifyCheckpoint(provider,preparation,expectedNetwork,expectedIntent,true);
}
async function verifyCheckpoint(provider,preparation,expectedNetwork,expectedIntent,deployment){
  const original=structuredClone(preparation);
  const identity=verifyOriginal(original,expectedNetwork,expectedIntent,deployment);
  if(await provider.getNetworkId()!==identity.chainId)throw new Error('Observed NEAR network differs.');
  const checkpoint=await provider.viewBlock({blockId:identity.checkpointHash});
  const height=checkpoint?.header?.height;
  if(checkpoint?.header?.hash!==identity.checkpointHash||!Number.isSafeInteger(height)||height<0)throw new Error('Original checkpoint is unavailable or invalid.');
  const [canonical,finalized]=await Promise.all([provider.viewBlock({blockId:height}),provider.viewBlock({finality:'final'})]);
  if(canonical?.header?.hash!==identity.checkpointHash||!Number.isSafeInteger(finalized?.header?.height)||finalized.header.height<height)
    throw new Error('Original checkpoint is not a finalized canonical ancestor.');
  return {...identity,checkpointHeight:height};
}
export async function prepareNearAction(account,request){
  request=structuredClone(request);
  const chainId=network(request.chainId),sender=accountId(request.sender),intent=nativeActionIntent(request);
  if(account.accountId!==sender||await account.provider.getNetworkId()!==chainId)throw new Error('Signing account/network differs from admitted request.');
  const action=intent.kind==='transfer'?actions.transfer(BigInt(intent.depositUnits)):
    actions.functionCall(intent.methodName,base64(intent.argsBase64),BigInt(intent.gasUnits),BigInt(intent.depositUnits));
  const signed=await account.createSignedTransaction({receiverId:intent.recipient,actions:[action]});
  const bytes=Buffer.from(signed.encode()),unsigned=Buffer.from(encodeTransaction(signed.transaction));
  const preparation={protocol:preparationProtocol,chainId,sender,transactionHash:baseEncode(createHash('sha256').update(unsigned).digest()),
    signedTransactionBase64:bytes.toString('base64'),signDocBase64:unsigned.toString('base64'),
    signature:null,intent};
  await verifyNearPreparation(account.provider,preparation,chainId,intent);
  return preparation;
}
export async function recoverNearPreparation(common,expectedNetwork){
  // Format is selected by this explicit recovery API, never by an unsigned
  // action hint. Native signed bytes reconstruct the entire supported action.
  common=structuredClone(common);
  if(common.protocol!==undefined&&common.protocol!==preparationProtocol)throw new Error('Original protocol differs.');
  const original={protocol:preparationProtocol,chainId:common.chainId,sender:common.sender,transactionHash:common.transactionHash,
    signedTransactionBase64:common.signedTransactionBase64,signDocBase64:common.signDocBase64,signature:common.signature??null};
  const identity=verifyNearSignedOriginal(original,expectedNetwork);
  return {...original,publicKey:identity.publicKey,intent:identity.intent,recoveredFromSignedOriginal:true};
}
// One SDK submission attempt for one frozen original. SDK `retries` counts
// total RPC attempts: 1 is one attempt; 0 would send no request at all.
export function exactNearSubmission(endpoint,signedTransactionBase64){
  const url=new URL(endpoint);
  if(!['http:','https:'].includes(url.protocol)||url.username||url.password||url.hash)throw new Error('Explicit RPC endpoint required.');
  const bytes=base64(signedTransactionBase64);
  const provider=new JsonRpcProvider({url:url.href},{retries:1,wait:0});
  let used=false;
  return async()=>{
    if(used)throw new Error('Original submission consumed; reconcile, never replay.');
    used=true;
    return await provider.sendTransactionUntil(SignedTransaction.decode(bytes),'FINAL');
  };
}
export async function submitNearPreparation(provider,preparation,expectedNetwork,persist,submit){
  return submitOriginal(provider,preparation,expectedNetwork,persist,submit,false);
}
export async function submitNearDeployment(provider,preparation,expectedNetwork,persist,submit){
  return submitOriginal(provider,preparation,expectedNetwork,persist,submit,true);
}
async function submitOriginal(provider,preparation,expectedNetwork,persist,submit,deployment){
  if(typeof persist!=='function'||typeof submit!=='function')throw new Error('Existing owned persistence and submission capabilities required.');
  const original=structuredClone(preparation);
  const identity=await verifyCheckpoint(provider,original,expectedNetwork,undefined,deployment);
  await persist(original);
  try {
    const outcome=await submit();
    if(outcome?.transaction_outcome?.id!==identity.transactionHash)throw new Error('Submission reply differs from retained original.');
    return {transactionHash:identity.transactionHash,persisted:true,submitted:true,outcome,reconciliationRequired:true};
  } catch(cause){
    const error=new Error('Original NEAR submission requires reconciliation; never re-sign or replay.',{cause});
    error.originalTransactionId=identity.transactionHash;error.reconciliationRequired=true;throw error;
  }
}

// Explicit code-only codec: NEAR signs SHA256(Borsh transaction). Persist that
// exact32byte signing message plus the complete signed Borsh, without duplicating code.
export async function prepareNearDeployment(account,request,code){
  request=structuredClone(request);code=Buffer.from(code);
  const chainId=network(request.chainId),sender=accountId(request.sender),intent=deploymentIntent(request);
  if(sender!==intent.recipient||account.accountId!==sender||await account.provider.getNetworkId()!==chainId
    ||code.length!==intent.codeBytes||createHash('sha256').update(code).digest('hex')!==intent.codeSha256)
    throw new Error('Deployment signer/network/code differs from admitted request.');
  const signed=await account.createSignedTransaction({receiverId:sender,actions:[actions.deployContract(code)]});
  const digest=createHash('sha256').update(encodeTransaction(signed.transaction)).digest();
  const original={protocol:deploymentProtocol,chainId,sender,transactionHash:baseEncode(digest),
    signedTransactionBase64:Buffer.from(signed.encode()).toString('base64'),signDocBase64:digest.toString('base64'),signature:null,intent};
  await verifyNearDeployment(account.provider,original,chainId,intent);return original;
}
export async function recoverNearDeployment(common,expectedNetwork){
  common=structuredClone(common);
  if(common.protocol!==undefined&&common.protocol!==deploymentProtocol)throw new Error('Deployment protocol differs.');
  const original={protocol:deploymentProtocol,chainId:common.chainId,sender:common.sender,transactionHash:common.transactionHash,
    signedTransactionBase64:common.signedTransactionBase64,signDocBase64:common.signDocBase64,signature:common.signature??null};
  const identity=verifyNearDeploymentOriginal(original,expectedNetwork);
  return {...original,publicKey:identity.publicKey,intent:identity.intent,recoveredFromSignedOriginal:true};
}
