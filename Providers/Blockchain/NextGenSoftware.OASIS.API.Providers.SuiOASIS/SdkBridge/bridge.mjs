import { Ed25519Keypair } from '@mysten/sui/keypairs/ed25519';
import { SuiGrpcClient } from '@mysten/sui/grpc';
import { Transaction } from '@mysten/sui/transactions';
import { isValidSuiAddress, normalizeSuiAddress, normalizeStructTag } from '@mysten/sui/utils';
import { bcs } from '@mysten/sui/bcs';
import { ObjectError } from '@mysten/sui/client';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { resolve } from 'node:path';
import { AsyncLocalStorage } from 'node:async_hooks';
import { verifyTransactionSignature } from '@mysten/sui/verify';
import { readTokenDeposit, executeTokenEscrow, createTokenEscrow, readTokenEscrowBalance } from './token-escrow.mjs';

export async function verifyPreparedIdentity(evidence) {
  if(typeof evidence?.signedTransactionBase64!=='string' || !evidence.signedTransactionBase64
      || evidence.signedTransactionBase64.length>1048576 || typeof evidence.signature!=='string' || evidence.signature.length>4096)
    throw new Error('Bounded original signed Sui evidence is required');
  const bytes=Buffer.from(evidence.signedTransactionBase64,'base64');
  if(!bytes.length || bytes.toString('base64')!==evidence.signedTransactionBase64)
    throw new Error('Original Sui bytes must be canonical base64');
  const tx=Transaction.from(bytes);
  if(await tx.getDigest()!==evidence.transactionHash)throw new Error('Prepared Sui digest does not match original bytes');
  const signer=await verifyTransactionSignature(bytes,evidence.signature);
  if(signer.toSuiAddress()!==evidence.sender || tx.getData().sender!==evidence.sender)
    throw new Error('Prepared Sui signature/sender does not match original transaction');
  const gasPayer=tx.getData().gasData.owner;
  if(!isValidSuiAddress(gasPayer))throw new Error('Original signed Sui transaction has no valid gas payer');
  return {verified:true,transactionHash:evidence.transactionHash,sender:evidence.sender,gasPayer:normalizeSuiAddress(gasPayer),gasBudgetMist:positiveUnits(tx.getData().gasData.budget,'gas budget').toString()};
}
import { generateMnemonic } from '@scure/bip39';
import { wordlist } from '@scure/bip39/wordlists/english.js';

function wallet(keypair) {
  return { privateKey: keypair.getSecretKey(), publicKey: keypair.getPublicKey().toBase64(), address: keypair.toSuiAddress() };
}

export function verifiedHistoryTransaction(tx, digest) {
  if (!tx || tx.digest !== digest || typeof tx.status?.success !== 'boolean'
      || !isValidSuiAddress(tx.transaction?.sender) || !Number.isSafeInteger(tx.timestampMs) || tx.timestampMs <= 0
      || typeof tx.checkpoint !== 'string' || !/^(?:0|[1-9][0-9]*)$/.test(tx.checkpoint)
      || BigInt(tx.checkpoint) > 18446744073709551615n
      || tx.effects?.transactionDigest !== digest || !Array.isArray(tx.balanceChanges))
    throw new Error('Sui returned mismatched or incomplete checkpointed wallet history');
  return tx;
}

export function transactionStatusReceipt(transaction, digest) {
  if (typeof digest !== 'string' || !digest || transaction?.digest !== digest
      || transaction.effects?.transactionDigest !== digest
      || typeof transaction.status?.success !== 'boolean')
    throw new Error('Sui transaction status lacks matching original digest, effects and explicit execution outcome');
  return { transactionHash: transaction.digest, status: transaction.status, gasUsed: transaction.effects.gasUsed };
}

export function historyScanComplete(reason, coveredCheckpoint, targetCheckpoint) {
  // QueryEnd's ledger tip is the indexed tip, not necessarily the committed tip.
  if (reason === 3 || reason === 5) {
    if (typeof coveredCheckpoint !== 'bigint' || coveredCheckpoint < targetCheckpoint)
      throw new Error('Sui history index has not covered the fixed committed checkpoint');
    return true;
  }
  // This query sets no exclusive cursor bound, so CURSOR_BOUND cannot prove completion.
  if (reason !== 1 && reason !== 2) throw new Error('Sui history stream returned an unexpected terminal reason');
  return false;
}

export function nativeFeeReceipt(tx, digest) {
  if (!tx || tx.digest !== digest || typeof tx.status?.success !== 'boolean'
      || tx.effects?.transactionDigest !== digest
      || typeof tx.checkpoint !== 'string' || !/^(?:0|[1-9][0-9]*)$/.test(tx.checkpoint)
      || BigInt(tx.checkpoint) > 18446744073709551615n
      || !Number.isSafeInteger(tx.timestampMs) || tx.timestampMs <= 0
      || !isValidSuiAddress(tx.transaction?.gasData?.owner))
    throw new Error('Sui fee receipt lacks original transaction, outcome, effects or actual gas payer');
  const gas = tx.effects.gasUsed;
  const fields = ['computationCost', 'storageCost', 'storageRebate', 'nonRefundableStorageFee'];
  for (const field of fields)
    if (typeof gas?.[field] !== 'string' || !/^(?:0|[1-9][0-9]*)$/.test(gas[field]) || BigInt(gas[field]) > 18446744073709551615n)
      throw new Error('Sui fee receipt lacks exact unsigned gas components');
  // Protocol net cost: non-refundable storage fee is already included, not added twice.
  const netUnits = BigInt(gas.computationCost) + BigInt(gas.storageCost) - BigInt(gas.storageRebate);
  return { transactionHash: digest, checkpoint:tx.checkpoint, success: tx.status.success, payer: normalizeSuiAddress(tx.transaction.gasData.owner),
    currency: 'SUI', unit: 'MIST', netUnits: netUnits.toString(), components: gas };
}

const Storage = bcs.struct('Storage', {
  id: bcs.Address, owner: bcs.Address,
  values: bcs.struct('Table', { id: bcs.Address, size: bcs.u64() }),
});
const NFT = bcs.struct('NFT', { id: bcs.Address, creator: bcs.Address, metadata: bcs.string() });
const LockedNFT = bcs.struct('LockedNFT', { id: bcs.Address, owner: bcs.Address, nft: NFT });
const Currency = bcs.struct('Currency', { id: bcs.Address, issuer: bcs.Address,
  treasury: bcs.struct('TreasuryCap', { id: bcs.Address, supply: bcs.u64() }) });
const LockedCoin = bcs.struct('LockedCoin', { id: bcs.Address, owner: bcs.Address,
  coin: bcs.struct('Coin', { id: bcs.Address, balance: bcs.u64() }) });

async function pages(fetch) {
  const objects = [];
  const seen = new Set();
  let cursor = null;
  do {
    const page = await fetch(cursor);
    objects.push(...page.objects);
    if (!page.hasNextPage) return objects;
    if (!page.cursor || seen.has(page.cursor)) throw new Error('Sui returned a non-advancing object cursor');
    seen.add(page.cursor);
    cursor = page.cursor;
  } while (true);
}

function positiveUnits(value, label='token') {
  if (typeof value !== 'string' || !/^[1-9][0-9]*$/.test(value) || BigInt(value) > 18446744073709551615n)
    throw new Error(`Positive exact u64 ${label} units are required`);
  return BigInt(value);
}

async function storageInfo(client, request) {
  if (!isValidSuiAddress(request.packageAddress) || !isValidSuiAddress(request.storageObjectId))
    throw new Error('Published OASIS package and shared storage object IDs are required');
  const { object } = await client.getObject({ objectId: request.storageObjectId, include: { content: true } });
  if (object.type !== `${normalizeSuiAddress(request.packageAddress)}::storage::Storage`)
    throw new Error('Storage object does not belong to the configured OASIS package ABI');
  if (!object.content) throw new Error('Sui returned no BCS storage content');
  return Storage.parse(object.content);
}

export class NativeMutationReconciliationError extends Error {
  constructor(message, transactionHash, executionSucceeded, cause) {
    super(message, cause ? { cause } : undefined);
    this.nativeMutation = { transactionHash: transactionHash ?? null, executionSucceeded };
  }
}

export function verifyStorageAuthority(owner,sender) {
  if(!isValidSuiAddress(owner)||!isValidSuiAddress(sender)||normalizeSuiAddress(owner)!==normalizeSuiAddress(sender))
    throw new Error('Sui storage owner does not match the configured signing account');
}

export function sdkErrorEnvelope(error) {
  return { ok: false, error: error.message,
    ...(error instanceof NativeMutationReconciliationError ? { nativeMutation: error.nativeMutation } : {}) };
}

const nativeMutationContext = new AsyncLocalStorage();
export async function withNativeMutationEvidence(operation, persistPreparedSubmission, maximumGasBudgetMist) {
  if (persistPreparedSubmission !== undefined && typeof persistPreparedSubmission !== 'function')
    throw new TypeError('Prepared Sui persistence must be an awaited callback');
  const maximumGasBudget=maximumGasBudgetMist===undefined?undefined:positiveUnits(maximumGasBudgetMist,'gas budget ceiling');
  return nativeMutationContext.run({ nativeMutation: null, persistPreparedSubmission, maximumGasBudget }, async () => {
    try { return await operation(); }
    catch (error) {
      const evidence = nativeMutationContext.getStore().nativeMutation;
      if (evidence && !(error instanceof NativeMutationReconciliationError))
        throw new NativeMutationReconciliationError(`Sui operation did not complete after submission began for original signed transaction ${evidence.transactionHash}; reconcile before retrying.`, evidence.transactionHash, evidence.executionSucceeded, error);
      throw error;
    }
  });
}

export async function execute(client, tx, signer, include = {}) {
  tx.setSender(signer.toSuiAddress());
  const transactionBytes = await tx.build({ client });
  const context = nativeMutationContext.getStore();
  const original=Transaction.from(transactionBytes);
  const gasBudget=positiveUnits(original.getData().gasData.budget,'gas budget');
  if(context?.maximumGasBudget!==undefined && gasBudget>context.maximumGasBudget)
    throw new Error('Built Sui gas budget exceeds the configured ceiling');
  const { signature } = await signer.signTransaction(transactionBytes);
  const originalDigest = await original.getDigest();
  if (context?.persistPreparedSubmission)
    await context.persistPreparedSubmission(Object.freeze({ transactionHash: originalDigest,
      signedTransactionBase64: Buffer.from(transactionBytes).toString('base64'), signature, sender: signer.toSuiAddress(),
      ...(context.chainIdentifier ? {chainId:context.chainIdentifier} : {}) }));
  if (context) context.nativeMutation = { transactionHash: originalDigest, executionSucceeded: null };
  let response;
  try {
    response = await client.executeTransaction({ transaction: transactionBytes, signatures: [signature], include: { effects: true, ...include } });
  } catch (error) {
    throw new NativeMutationReconciliationError(`Sui submission outcome for original signed transaction ${originalDigest} is unverified. Reconcile this transaction before any retry.`, originalDigest, null, error);
  }
  if (response?.FailedTransaction) {
    const failed = response.FailedTransaction;
    if (failed.digest !== originalDigest)
      throw new NativeMutationReconciliationError('Sui failed execution response does not match the original signed transaction; reconcile before retrying.', originalDigest, null);
    throw new NativeMutationReconciliationError(`Sui execution failed for original transaction ${failed.digest ?? '(digest unavailable)'}: ${JSON.stringify(failed.status)}. Reconcile this transaction before any retry.`, failed.digest, failed.status?.success === false ? false : null);
  }
  if (response?.Transaction?.digest !== originalDigest)
    throw new NativeMutationReconciliationError('Sui execution response does not match the original signed transaction; reconcile submission before any retry.', originalDigest, null);
  if (response.Transaction.status?.success !== true)
    throw new NativeMutationReconciliationError(`Sui original transaction ${response.Transaction.digest} lacks an explicit successful execution outcome; reconcile submission before any retry.`, response.Transaction.digest, response.Transaction.status?.success === false ? false : null);
  if (context) context.nativeMutation.executionSucceeded = true;
  try {
    await client.waitForTransaction({ digest: response.Transaction.digest });
  } catch (error) {
    throw new NativeMutationReconciliationError(`Sui execution returned original transaction ${response.Transaction.digest} but transaction readback failed: ${error.message}. Reconcile this transaction before any retry.`, response.Transaction.digest, true, error);
  }
  return response.Transaction;
}

export function publicationArtifacts(committed) {
  const packageId = committed.effects.changedObjects.find(object => object.outputState === 'PackageWrite' && object.idOperation === 'Created')?.objectId;
  const storageId = Object.entries(committed.objectTypes).find(([, type]) => type === `${packageId}::storage::Storage`)?.[0];
  const currencyObjectId = Object.entries(committed.objectTypes).find(([, type]) => type === `${packageId}::token::Currency`)?.[0];
  const coinType = `${packageId}::token::TOKEN`;
  const registrationId = Object.entries(committed.objectTypes).find(([, type]) => type !== 'package'
    && normalizeStructTag(type) === normalizeStructTag(`0x2::coin_registry::Currency<${coinType}>`))?.[0];
  if (!packageId || !storageId || !currencyObjectId || !registrationId)
    throw new NativeMutationReconciliationError(`Publication ${committed.digest} committed but required OASIS objects were absent`, committed.digest, true);
  return { packageAddress: packageId, storageObjectId: storageId, currencyObjectId, coinType, registrationId };
}

export function verifiedCustodyChange(transaction, request) {
  transactionStatusReceipt(transaction, request.transactionHash);
  if (transaction.status.success !== true || !Array.isArray(transaction.effects.changedObjects))
    throw new Error('Source transaction did not succeed on Sui');
  const changes = transaction.effects.changedObjects.filter(item => item.objectId === normalizeSuiAddress(request.tokenId));
  if (changes.length !== 1) throw new Error('Source receipt must uniquely identify the custody object change');
  const change = changes[0];
  if (change.outputOwner?.AddressOwner !== normalizeSuiAddress(request.custody)
      || !change.inputOwner?.AddressOwner || change.inputOwner.AddressOwner === normalizeSuiAddress(request.custody)
      || change.outputState !== 'ObjectWrite' || change.idOperation !== 'None')
    throw new Error('Source digest does not prove this NFT entered custody');
  return change;
}

async function custodyReceipt(client, request) {
  if (!isValidSuiAddress(request.tokenId) || !isValidSuiAddress(request.custody) || !request.transactionHash)
    throw new Error('NFT object ID, custody wallet and source digest are required');
  const response = await client.getTransaction({ digest: request.transactionHash, include: { effects: true } });
  const transaction = response.Transaction;
  const change = verifiedCustodyChange(transaction, request);
  const { object } = await client.getObject({ objectId: request.tokenId });
  if (object.objectId !== normalizeSuiAddress(request.tokenId) || object.version !== change.outputVersion || object.owner.AddressOwner !== normalizeSuiAddress(request.custody))
    throw new Error('Custody receipt is stale or the NFT has already been released');
  return { objectId: object.objectId, version: object.version, digest: object.digest };
}

// One history owner shared by the real SDK path and controlled stream-boundary tests.
export async function readWalletHistory(client,request) {
    if (!isValidSuiAddress(request.walletAddress)) throw new Error('Valid Sui history wallet is required');
    const walletAddress = normalizeSuiAddress(request.walletAddress);
    const maximum=18446744073709551615n;
    function bound(value){
      if(value===undefined||value===null)return undefined;
      if(typeof value!=='string'||!/^(?:0|[1-9][0-9]*)$/.test(value)||value.length>20||BigInt(value)>maximum)
        throw new Error('Sui history checkpoint range requires canonical u64 strings');
      return BigInt(value);
    }
    const lower=bound(request.fromBlock)??0n;const requestedUpper=bound(request.toBlock);
    if(requestedUpper!==undefined&&lower>requestedUpper)throw new Error('Sui history checkpoint range is reversed');
    const { response: info } = await client.ledgerService.getServiceInfo({});
    if (typeof info.checkpointHeight!=='bigint'||typeof info.lowestAvailableCheckpoint!=='bigint'
        ||info.checkpointHeight<0n||info.checkpointHeight>maximum||info.lowestAvailableCheckpoint<0n||info.lowestAvailableCheckpoint>info.checkpointHeight)
      throw new Error('Sui node returned no history availability bounds');
    const upper=requestedUpper===undefined||requestedUpper>info.checkpointHeight?info.checkpointHeight:requestedUpper;
    if(lower>upper)return [];
    if(lower<info.lowestAvailableCheckpoint)throw new Error('Requested Sui history checkpoint range has been pruned');
    if(upper===maximum)throw new Error('Sui exclusive history end checkpoint exceeds u64');
    const filter = { terms: [
      { literals: [{ negated: false, predicate: { oneofKind: 'sender', sender: { address: walletAddress } } }] },
      { literals: [{ negated: false, predicate: { oneofKind: 'affectedAddress', affectedAddress: { address: walletAddress } } }] },
    ] };
    const records = [];
    const cursors = new Set();
    const digests = new Set();
    const decimals = new Map();
    let after;
    do {
      let cursor;
      let end;
      let coveredCheckpoint;
      const call = client.ledgerService.listTransactions({ filter, startCheckpoint:lower,endCheckpoint:upper+1n,
        readMask: { paths: ['digest'] }, options: { limit: 50, ordering: 0, after } });
      for await (const frame of call.responses) {
        if (frame.watermark?.cursor) cursor = frame.watermark.cursor;
        if (frame.watermark?.checkpoint !== undefined) coveredCheckpoint = frame.watermark.checkpoint;
        if (frame.end) end = frame.end.reason;
        if (!frame.transaction) continue;
        const digest = frame.transaction.digest;
        if (!digest || digests.has(digest)) throw new Error('Sui returned missing or duplicate history digests');
        digests.add(digest);
        const fetched = await client.getTransaction({ digest,
          include: { effects: true, balanceChanges: true, transaction: true, objectTypes: true } });
        const tx = verifiedHistoryTransaction(fetched.Transaction ?? fetched.FailedTransaction, digest);
        if (BigInt(tx.checkpoint)>upper||BigInt(tx.checkpoint)<lower)
          throw new Error('Sui history receipt is outside the requested checkpoint range');
        const balances = tx.balanceChanges.filter(change => change.address === walletAddress);
        for (const change of balances) {
          if (!decimals.has(change.coinType)) {
            const { coinMetadata } = await client.getCoinMetadata({ coinType: change.coinType });
            if (!coinMetadata || !Number.isInteger(coinMetadata.decimals)) throw new Error('History asset has no on-chain decimals metadata');
            decimals.set(change.coinType, coinMetadata.decimals);
          }
          const recipients = [...new Set(tx.balanceChanges.filter(item => item.coinType === change.coinType
            && item.address !== walletAddress && BigInt(item.amount) > 0n).map(item => item.address))];
          records.push({ digest, sender: tx.transaction.sender, walletAddress, amountUnits: change.amount,
            decimals: decimals.get(change.coinType), coinType: change.coinType, recipients,
            checkpoint:tx.checkpoint,timestampMs: tx.timestampMs, success: tx.status.success });
        }
        if (!balances.length) records.push({ digest, sender: tx.transaction.sender, walletAddress,
          amountUnits: '0', decimals: 0, coinType: null, recipients: [], timestampMs: tx.timestampMs,
          checkpoint:tx.checkpoint,success: tx.status.success, objectChanges: tx.effects.changedObjects.map(item => item.objectId) });
      }
      if (historyScanComplete(end, coveredCheckpoint, upper)) return records;
      if (!cursor) throw new Error('Sui history stream returned no valid terminal cursor');
      const encoded = Buffer.from(cursor).toString('base64');
      if (cursors.has(encoded)) throw new Error('Sui returned a non-advancing history cursor');
      cursors.add(encoded);
      after = cursor;
    } while (true);
}

export async function invoke(request, persistPreparedSubmission) {
  return withNativeMutationEvidence(() => invokeCore(request), persistPreparedSubmission, request.maximumGasBudgetMist);
}

async function invokeCore(request) {
  if(request.operation==='verifyNativePreparation')return verifyPreparedIdentity(request.preparation);
  if (request.operation === 'generateKey') return wallet(Ed25519Keypair.generate());
  if (request.operation === 'restoreKey') return wallet(Ed25519Keypair.fromSecretKey(request.privateKey));
  if (request.operation === 'createAccount') {
    const seedPhrase = generateMnemonic(wordlist, 256);
    return { ...wallet(Ed25519Keypair.deriveKeypair(seedPhrase)), seedPhrase };
  }
  if (request.operation === 'restoreAccount') return wallet(Ed25519Keypair.deriveKeypair(request.seedPhrase));
  let signer;
  let amount;
  if (request.operation === 'transfer') {
    signer = Ed25519Keypair.fromSecretKey(request.privateKey);
    if (signer.toSuiAddress() !== request.fromWalletAddress) throw new Error('Signing key does not own sender wallet');
    if (!isValidSuiAddress(request.toWalletAddress)) throw new Error('Valid Sui recipient is required');
    if (!/^[1-9][0-9]*$/.test(request.amountUnits)) throw new Error('Positive integral MIST amount is required');
    amount = BigInt(request.amountUnits);
    if (amount > 18446744073709551615n) throw new Error('Sui amount exceeds u64');
  }
  const url = new URL(request.rpcEndpoint);
  if (!['http:', 'https:'].includes(url.protocol)) throw new Error('Sui RPC must use HTTP(S)');
  const client = new SuiGrpcClient({ network: request.network, baseUrl: url.href });
  const { chainIdentifier } = await client.getChainIdentifier();
  if (!chainIdentifier) throw new Error('Sui node returned no genesis chain identifier');
  if (request.chainId && request.chainId !== chainIdentifier) throw new Error('Sui chain identifier mismatch');
  const nativeScope=nativeMutationContext.getStore();
  if(nativeScope)nativeScope.chainIdentifier=chainIdentifier;
  if (request.operation === 'probe') return { chainIdentifier };
  if (request.operation === 'tokenEscrowCreate') return createTokenEscrow(client,request,execute);
  if (request.operation === 'tokenEscrowGet') return readTokenDeposit(client,{...request});
  if (request.operation === 'tokenEscrowBalance') return readTokenEscrowBalance(client,request);
  if (request.operation === 'tokenEscrowLock' || request.operation === 'tokenEscrowRelease')
    return executeTokenEscrow(client,request,execute,pages);
  if (request.operation === 'nativeFeeReceipt') {
    const fetched = await client.getTransaction({ digest: request.transactionHash, include: { effects: true, transaction: true } });
    return { ...nativeFeeReceipt(fetched.Transaction ?? fetched.FailedTransaction, request.transactionHash), chainIdentifier };
  }
  if (request.operation === 'history') return readWalletHistory(client,request);
  if (request.operation === 'nftMint') {
    if (!isValidSuiAddress(request.packageAddress) || !isValidSuiAddress(request.recipient)
        || !Array.isArray(request.metadata) || request.metadata.some(value => typeof value !== 'string')
        || !Number.isSafeInteger(request.count) || request.count < 1 || request.metadata.length !== request.count)
      throw new Error('Published package, recipient, serialized metadata and positive integral mint count are required');
    const signer = Ed25519Keypair.fromSecretKey(request.privateKey);
    const tx = new Transaction();
    for (let i = 0; i < request.count; i++)
      tx.moveCall({ target: `${request.packageAddress}::nft::mint`, arguments: [tx.pure.string(request.metadata[i]), tx.pure.address(request.recipient)] });
    const committed = await execute(client, tx, signer, { objectTypes: true });
    const tokenIds = Object.entries(committed.objectTypes).filter(([, type]) => type === `${normalizeSuiAddress(request.packageAddress)}::nft::NFT`).map(([id]) => id);
    if (tokenIds.length !== request.count) throw new NativeMutationReconciliationError(`Mint ${committed.digest} committed but returned an unexpected NFT object count`, committed.digest, true);
    return { transactionHash: committed.digest, tokenIds, recipient: normalizeSuiAddress(request.recipient) };
  }
  if (['nftEscrowGet', 'nftUnlock'].includes(request.operation)) {
    if (!isValidSuiAddress(request.packageAddress) || !isValidSuiAddress(request.tokenId))
      throw new Error('Published package and original NFT object ID are required');
    const signer = Ed25519Keypair.fromSecretKey(request.privateKey);
    const owner = signer.toSuiAddress();
    const escrows = await pages(cursor => client.listOwnedObjects({ owner,
      type: `${request.packageAddress}::nft::LockedNFT`, include: { content: true }, limit: 100, cursor }));
    const escrow = escrows.find(object => LockedNFT.parse(object.content).nft.id === normalizeSuiAddress(request.tokenId));
    if (!escrow) throw new Error('No owned escrow exists for this NFT');
    if (request.operation === 'nftEscrowGet') return LockedNFT.parse(escrow.content);
    const tx = new Transaction();
    const [nft] = tx.moveCall({ target: `${request.packageAddress}::nft::unlock`, arguments: [tx.object(escrow.objectId)] });
    tx.transferObjects([nft], tx.pure.address(owner));
    const committed = await execute(client, tx, signer);
    return { transactionHash: committed.digest };
  }
  if (['nftGet', 'nftSend', 'nftBurn', 'nftLock'].includes(request.operation)) {
    if (!isValidSuiAddress(request.packageAddress) || !isValidSuiAddress(request.tokenId))
      throw new Error('Published package and NFT object ID are required');
    const { object } = await client.getObject({ objectId: request.tokenId, include: { content: true } });
    if (object.type !== `${normalizeSuiAddress(request.packageAddress)}::nft::NFT` || !object.content)
      throw new Error('NFT object does not match the configured package ABI');
    const nft = NFT.parse(object.content);
    // Wrapped objects are not directly readable by object ID; escrow reads use owned objects.
    if (request.operation === 'nftGet') return { ...nft, owner: object.owner };
    const signer = Ed25519Keypair.fromSecretKey(request.privateKey);
    if (request.fromWalletAddress && normalizeSuiAddress(request.fromWalletAddress) !== signer.toSuiAddress())
      throw new Error('Signing key does not own sender wallet');
    const tx = new Transaction();
    if (request.operation === 'nftLock')
      tx.moveCall({ target: `${request.packageAddress}::nft::lock`, arguments: [tx.object(request.tokenId)] });
    else if (request.operation === 'nftBurn')
      tx.moveCall({ target: `${request.packageAddress}::nft::burn`, arguments: [tx.object(request.tokenId)] });
    else {
      if (!isValidSuiAddress(request.recipient)) throw new Error('Valid NFT recipient is required');
      const input = request.sourceTransactionHash
        ? tx.objectRef(await custodyReceipt(client, { tokenId: request.tokenId,
            custody: signer.toSuiAddress(), transactionHash: request.sourceTransactionHash }))
        : tx.object(request.tokenId);
      tx.transferObjects([input], tx.pure.address(request.recipient));
    }
    const committed = await execute(client, tx, signer);
    return { transactionHash: committed.digest };
  }
  if (['publish', 'publicationResume'].includes(request.operation)) {
    const signer = Ed25519Keypair.fromSecretKey(request.privateKey);
    let committed;
    if (request.operation === 'publicationResume') {
      const owned = await pages(cursor => client.listOwnedObjects({ owner: signer.toSuiAddress(), cursor,
        type: '0x2::package::UpgradeCap', include: { previousTransaction: true } }));
      if (owned.length !== 1 || !owned[0].previousTransaction)
        throw new Error('Publication reconciliation requires exactly one retained publisher capability.');
      const response = await client.getTransaction({ digest: owned[0].previousTransaction,
        include: { effects: true, objectTypes: true } });
      committed = response.Transaction;
      if (!committed?.status.success) throw new Error('Retained publication receipt did not succeed.');
    } else {
      if (!Array.isArray(request.modules) || request.modules.length === 0 || !Array.isArray(request.dependencies))
        throw new Error('Compiled Move modules and dependencies are required');
      const tx = new Transaction();
      const [cap] = tx.publish({ modules: request.modules, dependencies: request.dependencies });
      tx.transferObjects([cap], tx.pure.address(signer.toSuiAddress()));
      committed = await execute(client, tx, signer, { objectTypes: true });
    }
    const artifacts = publicationArtifacts(committed);
    const { object: registration } = await client.getObject({ objectId: artifacts.registrationId, include: { previousTransaction: true } });
    let registrationHash;
    if (registration.owner.AddressOwner === normalizeSuiAddress('0xc')) {
      const registrationTx = new Transaction();
      registrationTx.moveCall({ target: '0x2::coin_registry::finalize_registration', typeArguments: [artifacts.coinType],
        arguments: [registrationTx.object('0xc'), registrationTx.receivingRef(registration)] });
      registrationHash = (await execute(client, registrationTx, signer)).digest;
    } else if (registration.owner.Shared) registrationHash = registration.previousTransaction;
    else throw new Error(`Publication ${committed.digest} has an unexpected currency registration owner.`);
    return { transactionHash: committed.digest, registrationHash, ...artifacts };
  }
  if (['tokenProbe', 'tokenMetadata', 'tokenBalance', 'tokenLockedBalance', 'tokenMint', 'tokenSend', 'tokenBurn', 'tokenLock', 'tokenUnlock'].includes(request.operation)) {
    if (!isValidSuiAddress(request.packageAddress) || !isValidSuiAddress(request.currencyObjectId))
      throw new Error('Published OASIS package and currency object IDs are required');
    const coinType = `${normalizeSuiAddress(request.packageAddress)}::token::TOKEN`;
    const { object } = await client.getObject({ objectId: request.currencyObjectId, include: { content: true } });
    if (object.type !== `${normalizeSuiAddress(request.packageAddress)}::token::Currency` || !object.content)
      throw new Error('Currency object does not match the configured package ABI');
    const currency = Currency.parse(object.content);
    if (request.operation === 'tokenProbe') return { ...currency, coinType };
    if (request.operation === 'tokenMetadata') return (await client.getCoinMetadata({ coinType })).coinMetadata;
    if (request.operation === 'tokenLockedBalance') {
      if (!isValidSuiAddress(request.walletAddress)) throw new Error('Valid Sui wallet is required');
      const objects = await pages(cursor => client.listOwnedObjects({ owner: request.walletAddress,
        type: `${request.packageAddress}::token::LockedCoin`, include: { content: true }, limit: 100, cursor }));
      return objects.reduce((sum, object) => sum + BigInt(LockedCoin.parse(object.content).coin.balance), 0n).toString();
    }
    if (request.operation === 'tokenBalance') {
      if (!isValidSuiAddress(request.walletAddress)) throw new Error('Valid Sui wallet is required');
      return (await client.getBalance({ owner: request.walletAddress, coinType })).balance.balance;
    }
    const signer = Ed25519Keypair.fromSecretKey(request.privateKey);
    const owner = signer.toSuiAddress();
    if (request.fromWalletAddress && normalizeSuiAddress(request.fromWalletAddress) !== owner)
      throw new Error('Signing key does not own sender wallet');
    const tx = new Transaction();
    if (request.operation === 'tokenMint') {
      if (!isValidSuiAddress(request.recipient)) throw new Error('Valid token recipient is required');
      const [coin] = tx.moveCall({ target: `${request.packageAddress}::token::mint`,
        arguments: [tx.object(request.currencyObjectId), tx.pure.u64(positiveUnits(request.amountUnits))] });
      tx.transferObjects([coin], tx.pure.address(request.recipient));
    } else if (request.operation === 'tokenUnlock') {
      const locked = await pages(cursor => client.listOwnedObjects({ owner, type: `${request.packageAddress}::token::LockedCoin`, limit: 100, cursor }));
      if (!locked.length) throw new Error('No owned locked tokens exist');
      for (const object of locked) {
        const [coin] = tx.moveCall({ target: `${request.packageAddress}::token::unlock`, arguments: [tx.object(object.objectId)] });
        tx.transferObjects([coin], tx.pure.address(owner));
      }
    } else {
      const coins = await pages(cursor => client.listCoins({ owner, coinType, limit: 100, cursor }));
      if (!coins.length) throw new Error('No spendable token coins exist');
      const coin = tx.object(coins[0].objectId);
      if (coins.length > 1) tx.mergeCoins(coin, coins.slice(1).map(item => tx.object(item.objectId)));
      if (request.operation === 'tokenSend') {
        if (!isValidSuiAddress(request.recipient)) throw new Error('Valid token recipient is required');
        const [sent] = tx.splitCoins(coin, [tx.pure.u64(positiveUnits(request.amountUnits))]);
        tx.transferObjects([sent], tx.pure.address(request.recipient));
      } else if (request.operation === 'tokenBurn') {
        const [burned] = tx.splitCoins(coin, [tx.pure.u64(positiveUnits(request.amountUnits))]);
        tx.moveCall({ target: `${request.packageAddress}::token::burn`, arguments: [tx.object(request.currencyObjectId), burned] });
      }
      else tx.moveCall({ target: `${request.packageAddress}::token::lock`, arguments: [coin] });
    }
    const committed = await execute(client, tx, signer);
    return { transactionHash: committed.digest };
  }
  if (['storageProbe', 'get', 'put', 'delete', 'putMany', 'deleteMany', 'list'].includes(request.operation)) {
    const storage = await storageInfo(client, request);
    if (request.operation === 'storageProbe') {
      verifyStorageAuthority(storage.owner,Ed25519Keypair.fromSecretKey(request.privateKey).toSuiAddress());
      return { owner: storage.owner, tableId: storage.values.id, chainIdentifier };
    }
    if (request.operation === 'get') {
      if (typeof request.key !== 'string') throw new Error('Storage key is required');
      try {
        const { dynamicField } = await client.core.getDynamicField({ parentId: storage.values.id,
          name: { type: '0x1::string::String', bcs: bcs.string().serialize(request.key).toBytes() } });
        return bcs.string().parse(dynamicField.value.bcs);
      } catch (error) {
        if (error instanceof ObjectError && error.reason === 'notFound') return null;
        throw error;
      }
    }
    if (request.operation === 'list') {
      const keys = [];
      const seen = new Set();
      let cursor = null;
      do {
        const page = await client.listDynamicFields({ parentId: storage.values.id, limit: 100, cursor });
        for (const field of page.dynamicFields) {
          const key = bcs.string().parse(field.name.bcs);
          if (key.startsWith(request.prefix ?? '')) keys.push(key);
        }
        if (!page.hasNextPage) return keys.sort();
        if (!page.cursor || seen.has(page.cursor)) throw new Error('Sui returned a non-advancing storage cursor');
        seen.add(page.cursor);
        cursor = page.cursor;
      } while (true);
    }
    const signer = Ed25519Keypair.fromSecretKey(request.privateKey);
    const tx = new Transaction();
    const putting = request.operation === 'put' || request.operation === 'putMany';
    const entries = request.operation.endsWith('Many') ? request.entries : [{ key: request.key, value: request.value }];
    if (!Array.isArray(entries) || entries.length === 0) throw new Error('Nonempty storage mutations are required');
    for (const entry of entries) {
      if (typeof entry.key !== 'string' || (putting && typeof entry.value !== 'string'))
        throw new Error('Storage key and serialized value are required');
      const args = [tx.object(request.storageObjectId), tx.pure.string(entry.key)];
      if (putting) args.push(tx.pure.string(entry.value));
      tx.moveCall({ target: `${request.packageAddress}::storage::${putting ? 'put' : 'remove'}`, arguments: args });
    }
    const committed = await execute(client, tx, signer);
    return { transactionHash: committed.digest };
  }
  if (request.operation === 'nftCustodyReceipt') {
    return custodyReceipt(client, request);
  }
  if (request.operation === 'transaction') {
    if (!request.transactionHash) throw new Error('Transaction digest is required');
    const response = await client.getTransaction({ digest: request.transactionHash, include: { effects: true } });
    const transaction = response.Transaction ?? response.FailedTransaction;
    return transactionStatusReceipt(transaction, request.transactionHash);
  }
  if (request.operation === 'balance') {
    if (!isValidSuiAddress(request.walletAddress)) throw new Error('Valid Sui wallet address is required');
    return (await client.getBalance({ owner: request.walletAddress, coinType: '0x2::sui::SUI' })).balance;
  }
  if (request.operation === 'transfer') {
    const tx = new Transaction();
    tx.setSender(signer.toSuiAddress());
    const [coin] = tx.splitCoins(tx.gas, [tx.pure.u64(amount)]);
    tx.transferObjects([coin], tx.pure.address(request.toWalletAddress));
    const committed = await execute(client, tx, signer);
    return { transactionHash: committed.digest };
  }
  throw new Error(`Unsupported Sui SDK operation: ${request.operation}`);
}

// Credentials enter through stdin, never command-line arguments.
if (process.argv[1] && fileURLToPath(import.meta.url) === resolve(process.argv[1])) {
  try {
    let input = '';
    for await (const chunk of process.stdin) input += chunk;
    let persistPreparedSubmission;
    if (process.env.OASIS_NATIVE_PREPARATION_PIPE) {
      const modulePath=process.env.OASIS_NATIVE_PREPARATION_MODULE;
      if(!modulePath)throw new Error('Parent preparation transport module is required');
      const {preparationClient}=await import(pathToFileURL(modulePath).href);
      persistPreparedSubmission=preparationClient(process.env.OASIS_NATIVE_PREPARATION_PIPE);
    }
    const result = await invoke(JSON.parse(input), persistPreparedSubmission);
    process.stdout.write(JSON.stringify({ ok: true, result }));
  } catch (error) {
    process.stdout.write(JSON.stringify(sdkErrorEnvelope(error)));
    process.exitCode = 1;
  }
}
