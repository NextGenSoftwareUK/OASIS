import { bcs } from '@mysten/sui/bcs';
import { ObjectError } from '@mysten/sui/client';
import { Ed25519Keypair } from '@mysten/sui/keypairs/ed25519';
import { Transaction } from '@mysten/sui/transactions';
import { isValidSuiAddress, normalizeSuiAddress, normalizeStructTag } from '@mysten/sui/utils';

export const Escrow = bcs.struct('Escrow', { id:bcs.Address, protocol:bcs.u64(),
  deposits:bcs.struct('Table',{id:bcs.Address,size:bcs.u64()}) });
export const Deposit = bcs.struct('Deposit',{owner:bcs.Address,amount:bcs.u64(),released:bcs.bool(),funds:bcs.u64()});

function selectors(request) {
  if(!isValidSuiAddress(request.packageAddress)||!isValidSuiAddress(request.escrowObjectId)
    ||typeof request.depositId!=='string'||!/^[0-9a-f]{64}$/.test(request.depositId))
    throw new Error('Explicit package, shared escrow and canonical original SHA256 deposit ID required');
  return {packageId:normalizeSuiAddress(request.packageAddress),escrowId:normalizeSuiAddress(request.escrowObjectId),
    id:Buffer.from(request.depositId,'hex')};
}
function units(value) {
  if(typeof value!=='string'||!/^[1-9][0-9]*$/.test(value)||BigInt(value)>18446744073709551615n)
    throw new Error('Positive canonical exact u64 deposit units required');
  return value;
}

async function registryInfo(client,packageId,escrowId) {
  const {object}=await client.getObject({objectId:escrowId,include:{content:true,previousTransaction:true}});
  if(object.objectId!==escrowId || object.type!==`${packageId}::token_escrow::Escrow`
    ||object.owner?.$kind!=='Shared'||!object.content)
    throw new Error('Exact shared escrow object and package ABI required');
  const registry=Escrow.parse(object.content);
  if(registry.id!==escrowId || registry.protocol!=='1')throw new Error('Exact escrow registry identity/protocol1 required');
  return {registry,object};
}

export async function createTokenEscrow(client,request,execute) {
  const intent=Object.freeze({...request});
  if(!isValidSuiAddress(intent.packageAddress)||intent.escrowObjectId)
    throw new Error('Explicit package required; registry creation cannot replace a selected escrow');
  const packageId=normalizeSuiAddress(intent.packageAddress);
  const signer=Ed25519Keypair.fromSecretKey(intent.privateKey);
  if(intent.fromWalletAddress&&normalizeSuiAddress(intent.fromWalletAddress)!==signer.toSuiAddress())
    throw new Error('Registry creator must own requested signer wallet');
  const tx=new Transaction();tx.moveCall({target:`${packageId}::token_escrow::create`,arguments:[]});
  const committed=await execute(client,tx,signer,{objectTypes:true});
  if(committed.effects?.transactionDigest!==committed.digest||!Array.isArray(committed.effects.changedObjects))
    throw new Error('Original successful creation effects required');
  const created=committed.effects.changedObjects.filter(item=>item.idOperation==='Created'&&item.outputState==='ObjectWrite'
    &&item.outputOwner?.Shared&&committed.objectTypes?.[item.objectId]===`${packageId}::token_escrow::Escrow`);
  if(created.length!==1||!isValidSuiAddress(created[0].objectId)
    ||typeof created[0].outputVersion!=='string'||!/^[1-9][0-9]*$/.test(created[0].outputVersion)
    ||BigInt(created[0].outputVersion)>18446744073709551615n
    ||created[0].outputOwner.Shared.initialSharedVersion!==created[0].outputVersion)
    throw new Error('Exactly one original shared registry creation/version required');
  const escrowObjectId=normalizeSuiAddress(created[0].objectId);
  const {registry,object}=await registryInfo(client,packageId,escrowObjectId);
  if(registry.deposits.size!=='0'||object.previousTransaction!==committed.digest||object.version!==created[0].outputVersion
    ||object.owner.Shared.initialSharedVersion!==created[0].outputVersion)
    throw new Error('Created registry readback does not match original empty creation');
  return {transactionHash:committed.digest,escrowObjectId,creator:signer.toSuiAddress(),protocol:1};
}

export async function readTokenDeposit(client,request) {
  const {packageId,escrowId,id}=selectors(request);
  const {registry}=await registryInfo(client,packageId,escrowId);
  let field;
  try {
    field=(await client.core.getDynamicField({parentId:registry.deposits.id,
      name:{type:'vector<u8>',bcs:bcs.vector(bcs.u8()).serialize([...id]).toBytes()}})).dynamicField;
  }catch(error){if(error instanceof ObjectError&&error.reason==='notFound')return null;throw error;}
  if(normalizeStructTag(field.value.type)!==`${packageId}::token_escrow::Deposit`)
    throw new Error('Deposit field has an unexpected Move type');
  const saved=Deposit.parse(field.value.bcs);
  units(saved.amount);
  if(saved.funds!==(saved.released?'0':saved.amount))throw new Error('Deposit funds disagree with original amount/release state');
  return {...saved,depositId:request.depositId,escrowObjectId:escrowId};
}

// Signing/preparation/submission stays owned by bridge.execute. This builds only
// the selected operation, then validates its original record after that receipt.
export async function executeTokenEscrow(client,request,execute,pages) {
  const intent=Object.freeze({...request});
  const {packageId,escrowId,id}=selectors(intent);
  const locking=intent.operation==='tokenEscrowLock';
  if(!locking&&intent.operation!=='tokenEscrowRelease')throw new Error('Explicit token escrow operation required');
  const amount=locking?units(intent.amountUnits):null;
  const signer=Ed25519Keypair.fromSecretKey(intent.privateKey);const owner=signer.toSuiAddress();
  if(intent.fromWalletAddress&&normalizeSuiAddress(intent.fromWalletAddress)!==owner)
    throw new Error('Escrow signing key does not own requested sender');
  const original=await readTokenDeposit(client,intent);
  if(locking&&original!==null)throw new Error('Deposit ID already used; reconcile original operation');
  if(!locking&&(!original||original.owner!==owner||original.released))throw new Error('Original active owned deposit required');
  const exact=locking?amount:original.amount;
  const tx=new Transaction();
  const argumentsPrefix=[tx.object(escrowId),tx.pure.vector('u8',[...id])];
  if(locking){
    const coins=await pages(cursor=>client.listCoins({owner,coinType:`${packageId}::token::TOKEN`,limit:100,cursor}));
    if(!coins.length)throw new Error('No spendable token coins exist');
    const seen=new Set();let total=0n;
    for(const item of coins){
      if(!isValidSuiAddress(item.objectId)||seen.has(normalizeSuiAddress(item.objectId))
        ||typeof item.balance!=='string'||!/^(?:0|[1-9][0-9]*)$/.test(item.balance)||BigInt(item.balance)>18446744073709551615n)
        throw new Error('Invalid or duplicate spendable coin evidence');
      seen.add(normalizeSuiAddress(item.objectId));total+=BigInt(item.balance);
    }
    if(total<BigInt(exact))throw new Error('Insufficient spendable token units');
    const coin=tx.object(coins[0].objectId);
    if(coins.length>1)tx.mergeCoins(coin,coins.slice(1).map(item=>tx.object(item.objectId)));
    const [selected]=tx.splitCoins(coin,[tx.pure.u64(exact)]);
    tx.moveCall({target:`${packageId}::token_escrow::lock`,arguments:[...argumentsPrefix,selected]});
  }else{
    const [restored]=tx.moveCall({target:`${packageId}::token_escrow::release`,arguments:[...argumentsPrefix,tx.pure.u64(exact)]});
    tx.transferObjects([restored],tx.pure.address(owner));
  }
  const committed=await execute(client,tx,signer);
  const saved=await readTokenDeposit(client,intent);
  if(!saved||saved.owner!==owner||saved.amount!==exact||saved.released===locking)
    throw new Error('Committed original deposit readback differs from requested custody');
  return {transactionHash:committed.digest,depositId:intent.depositId,amountUnits:exact,owner,released:!locking,escrowObjectId:escrowId};
}
