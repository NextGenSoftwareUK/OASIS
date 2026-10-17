import { Client, PrivateKey, AccountId, TransactionId, TransferTransaction, TransactionReceiptQuery, TokenMintTransaction, TokenBurnTransaction, TokenId, Hbar, TokenInfoQuery, FileId, FileCreateTransaction, FileUpdateTransaction, FileDeleteTransaction, FileContentsQuery } from '@hiero-ledger/sdk';
import { pathToFileURL } from 'node:url';
import { Mnemonic } from '@hiero-ledger/sdk';
import { executeEscrow, executeTokenEscrow,verifyDepositorAccount } from './escrow.mjs';
import {AccountInfoQuery} from '@hiero-ledger/sdk';
import {executeDeployment} from './deployment.mjs';
import { ContractId, ContractCallQuery, ContractExecuteTransaction } from '@hiero-ledger/sdk';
import { Interface, getBytes } from 'ethers';

const storageAbi = new Interface([
  'function owner() view returns(address)',
  'function put(string key,string value)',
  'function remove(string key)',
  'function get(string key) view returns(bool exists,string value)',
  'function revision() view returns(uint256)',
  'function keyPage(uint256 expectedRevision,uint256 cursor,uint256 limit) view returns(string[] page,uint256 next,uint256 total)'
]);

export function storageTransaction(action, request) {
  ContractId.fromString(request.contractId);
  if (!['storagePut', 'storageDelete'].includes(action)) throw new Error('Unknown storage mutation.');
  if (typeof request.key !== 'string' || !request.key) throw new Error('An explicit nonempty storage key is required.');
  if (action === 'storagePut' && typeof request.value !== 'string') throw new Error('An explicit storage value is required.');
  if (!Number.isSafeInteger(request.gas) || request.gas <= 0) throw new Error('An explicit positive transaction gas limit is required.');
  const name = action === 'storagePut' ? 'put' : 'remove';
  const args = action === 'storagePut' ? [request.key, request.value] : [request.key];
  return new ContractExecuteTransaction().setContractId(request.contractId).setGas(request.gas)
    .setFunctionParameters(getBytes(storageAbi.encodeFunctionData(name, args)));
}

import {persistSignedSubmission,verifyPreparedIdentity} from './native-preparation.mjs';
import {verifyPreparedQueryIdentity,createPaidQuery} from './paid-query-preparation.mjs';
export {persistSignedSubmission} from './native-preparation.mjs';
async function storageWrite(action, request, persist) {
  const mutation = storageTransaction(action, request);
  const key = restoreKey(request);
  if (request.publicKey && request.publicKey.toLowerCase() !== key.publicKey.toStringDer().toLowerCase())
    throw new Error('Storage signing key mismatch.');
  const payer = AccountId.fromString(request.sender);
  const id = TransactionId.fromString(request.transactionId);
  if (id.accountId.toString() !== payer.toString()) throw new Error('Storage transaction payer mismatch.');
  if (!/^[1-9][0-9]*$/.test(request.maxFeeTinybars ?? '') || BigInt(request.maxFeeTinybars) > 9223372036854775807n)
    throw new Error('An explicit positive int64 fee ceiling is required.');
  const client = await createClient(request);
  let stage = 'construction';
  try {
    client.setOperator(payer, key);
    singleSubmission(mutation, id).setMaxTransactionFee(Hbar.fromTinybars(request.maxFeeTinybars)).freezeWith(client);
    stage = 'signing';
    const signed = await mutation.sign(key);
    stage = 'preparation';await persistSignedSubmission(signed,request,persist);
    stage = 'submission';
    const submitted = await signed.execute(client);
    stage = 'receipt';
    const receipt = await submitted.getReceipt(client);
    if (receipt.status.toString() !== 'SUCCESS') throw new Error('Storage mutation failed consensus.');
    return { transactionId: submitted.transactionId.toString(), status: 'SUCCESS' };
  } catch (cause) {
    const error = new Error('Hedera storage mutation did not establish consensus success. Track the original transaction before retrying.');
    if(stage==='preparation')throw cause;
    error.transactionId = id.toString();
    error.stage = stage;
    error.status = nativeFailureStatus(cause);
    throw error;
  } finally { client.close(); }
}

export function storageReadCall(name, args) {
  if (!['owner', 'get', 'revision', 'keyPage'].includes(name)) throw new Error('Unknown storage read.');
  return getBytes(storageAbi.encodeFunctionData(name, args));
}

export function nativeFailureStatus(cause) {
  const status = cause.status?.toString();
  if (status) return status;
  // Executable's attempt-limit Error encodes the native precheck status here.
  return /^max attempts of [0-9]+ was reached for request with last error being: ([A-Z][A-Z0-9_]{0,100})$/.exec(cause.message ?? '')?.[1];
}

export function singleSubmission(transaction, transactionId) {
  return transaction.setTransactionId(transactionId).setMaxAttempts(1).setRegenerateTransactionId(false);
}

export function validateStoragePage(page, cursor, total) {
  if (page[0].length > 256 || page[1] !== cursor + BigInt(page[0].length) || page[1] > page[2]
      || (total !== null && total !== page[2]) || (page[0].length === 0 && page[1] < page[2]))
    throw new Error('Storage pagination is inconsistent.');
}

export function verifyStorageAccount(account,payer,key) {
  if(!account||account.accountId?.toString()!==payer.toString()||account.isDeleted!==false||typeof account.key?.toStringDer!=='function'
      ||account.key.toStringDer().toLowerCase()!==key.publicKey.toStringDer().toLowerCase())
    throw new Error('Storage activation requires the real active sender single signing key');
}

async function storageRead(action, request,persistPaidQueryPayment) {
  ContractId.fromString(request.contractId);
  if (action === 'storageGet' && typeof request.key !== 'string') throw new Error('An explicit storage key is required.');
  if (action === 'storageList' && typeof request.prefix !== 'string') throw new Error('An explicit prefix is required.');
  if (!Number.isSafeInteger(request.gas) || request.gas <= 0) throw new Error('An explicit positive query gas limit is required.');
  if (!/^[1-9][0-9]*$/.test(request.maxFeeTinybars ?? '') || BigInt(request.maxFeeTinybars) > 9223372036854775807n)
    throw new Error('An explicit positive int64 query fee ceiling is required.');
  const key = restoreKey(request);
  const payer = AccountId.fromString(request.sender);
  if (request.publicKey && request.publicKey.toLowerCase() !== key.publicKey.toStringDer().toLowerCase())
    throw new Error('Storage query signing key mismatch.');
  const client = await createClient(request);
  try {
    client.setOperator(payer, key);
    async function query(name, args) {
      const response = await createPaidQuery(ContractCallQuery,request,client,key,persistPaidQueryPayment).setContractId(request.contractId).setGas(request.gas)
        .setFunctionParameters(storageReadCall(name, args)).setMaxQueryPayment(Hbar.fromTinybars(request.maxFeeTinybars)).execute(client);
      if (response.errorMessage) throw new Error('Storage contract query reverted.');
      return storageAbi.decodeFunctionResult(name, response.bytes);
    }
    if(action==='storageProbe'){
      const payment=Hbar.fromTinybars(request.maxFeeTinybars);
      const account=await createPaidQuery(AccountInfoQuery,request,client,key,persistPaidQueryPayment).setAccountId(payer).setMaxQueryPayment(payment).execute(client);
      verifyStorageAccount(account,payer,key);
      const owner=(await query('owner',[]))[0];
      await verifyDepositorAccount(payer,owner,alias=>createPaidQuery(AccountInfoQuery,request,client,key,persistPaidQueryPayment).setAccountId(alias).setMaxQueryPayment(payment).execute(client));
      return {owner,sender:payer.toString(),contractId:request.contractId};
    }
    if (action === 'storageGet') {
      const result = await query('get', [request.key]);
      return result[0] ? result[1] : null;
    }
    const revision = (await query('revision', []))[0];
    let cursor = 0n;
    let total = null;
    const keys = [];
    do {
      const page = await query('keyPage', [revision, cursor, 256]);
      validateStoragePage(page, cursor, total);
      total = page[2];
      keys.push(...page[0]);
      cursor = page[1];
    } while (cursor < total);
    if ((await query('revision', []))[0] !== revision) throw new Error('Storage changed during enumeration.');
    if (new Set(keys).size !== keys.length) throw new Error('Storage returned duplicate keys.');
    return keys.filter(value => value.startsWith(request.prefix)).sort();
  } finally { client.close(); }
}

export function restoreKey(request) {
  if (typeof request.privateKey !== 'string' || !request.privateKey)
    throw new Error('A signing key is required.');
  switch (request.keyType) {
    case 'ed25519': return PrivateKey.fromStringED25519(request.privateKey);
    case 'ecdsa': return PrivateKey.fromStringECDSA(request.privateKey);
    case 'der':
      if (!/^30[0-9a-f]+$/i.test(request.privateKey) || request.privateKey.length <= 64)
        throw new Error('DER key encoding is required; raw keys need an explicit algorithm.');
      return PrivateKey.fromString(request.privateKey);
    default: throw new Error('Signing key type must be ed25519, ecdsa or der.');
  }
}

async function createClient(request) {
  if (request.network === 'local') {
    const nodes = request.nodes;
    if (!nodes || !Object.keys(nodes).length || Object.keys(nodes).some(endpoint => !/^127\.0\.0\.1:[0-9]+$/.test(endpoint)))
      throw new Error('Local consensus nodes must use explicitly configured loopback endpoints.');
    for (const id of Object.values(nodes)) AccountId.fromString(id);
    return Client.forNetwork(nodes);
  }
  if (!['mainnet', 'testnet', 'previewnet'].includes(request.network))
    throw new Error('An explicit supported Hedera network is required.');
  const client = Client.forName(request.network);
  try {
    await client.updateNetwork();
    return client;
  } catch (error) { client.close(); throw error; }
}

export function tokenUnits(amount, decimals) {
  if (!Number.isInteger(decimals) || decimals < 0 || decimals > 255 || !/^[0-9]+(?:\.[0-9]+)?$/.test(amount ?? ''))
    throw new Error('Exact positive decimal token amount and ledger decimals are required.');
  const [whole, fractional = ''] = amount.split('.');
  if (fractional.length > decimals && /[1-9]/.test(fractional.slice(decimals)))
    throw new Error('Token amount exceeds ledger decimal precision.');
  const units = BigInt(whole + fractional.slice(0, decimals).padEnd(decimals, '0'));
  if (units <= 0n || units > 9223372036854775807n) throw new Error('Token units must fit positive int64.');
  return units;
}

export function nftTransaction(action, request) {
  const token = TokenId.fromString(request.tokenId);
  if (action === 'nftMint') {
    if (!Array.isArray(request.metadata) || !request.metadata.length || request.metadata.length > 10)
      throw new Error('NFT mint requires explicit metadata for one to ten serials.');
    const metadata = request.metadata.map(value => {
      if (typeof value !== 'string' || !value || /[\uD800-\uDFFF]/u.test(value) || Buffer.byteLength(value, 'utf8') > 100)
        throw new Error('NFT metadata must contain 1 to 100 UTF-8 bytes per serial.');
      return Buffer.from(value, 'utf8');
    });
    return new TokenMintTransaction().setTokenId(token).setMetadata(metadata);
  }
  if (!['nftTransfer', 'nftBurn'].includes(action)) throw new Error('Unknown NFT operation.');
  if (!Array.isArray(request.serials) || !request.serials.length
      || request.serials.some(value => typeof value !== 'string' || !/^[1-9][0-9]*$/.test(value)
        || BigInt(value) > 9223372036854775807n)
      || new Set(request.serials).size !== request.serials.length)
    throw new Error('Unique positive exact int64 NFT serial strings are required.');
  if (action === 'nftBurn') return new TokenBurnTransaction().setTokenId(token).setSerials(request.serials);
  const transfer = new TransferTransaction();
  for (const serial of request.serials) transfer.addNftTransfer(token, serial, request.sender, request.recipient);
  return transfer;
}

export function fileTransaction(action, request, publicKey) {
  if (!['fileCreate', 'fileUpdate', 'fileDelete'].includes(action)) throw new Error('Unknown file mutation.');
  if (action !== 'fileCreate') FileId.fromString(request.fileId);
  if (action !== 'fileDelete' && (typeof request.contents !== 'string' || Buffer.byteLength(request.contents, 'utf8') > 2048))
    throw new Error('File mutations require explicit UTF-8 contents of at most 2048 bytes.');
  if (action === 'fileCreate') return new FileCreateTransaction().setKeys([publicKey]).setContents(Buffer.from(request.contents, 'utf8'));
  if (action === 'fileUpdate') return new FileUpdateTransaction().setFileId(request.fileId).setContents(Buffer.from(request.contents, 'utf8'));
  return new FileDeleteTransaction().setFileId(request.fileId);
}

async function executeFile(action, request, persist,persistPaidQueryPayment) {
  const key = restoreKey(request);
  if (request.publicKey && request.publicKey.toLowerCase() !== key.publicKey.toStringDer().toLowerCase())
    throw new Error('File signing key does not match the supplied public key.');
  const payer = AccountId.fromString(request.sender);
  if (action === 'fileRead') FileId.fromString(request.fileId);
  const mutation = action === 'fileRead' ? null : fileTransaction(action, request, key.publicKey);
  const id = mutation ? TransactionId.fromString(request.transactionId) : null;
  if (id && id.accountId.toString() !== payer.toString()) throw new Error('File transaction payer mismatch.');
  if (!/^[1-9][0-9]*$/.test(request.maxFeeTinybars ?? '') || BigInt(request.maxFeeTinybars) > 9223372036854775807n)
    throw new Error('An explicit positive int64 fee ceiling is required.');
  const client = await createClient(request);
  let stage = 'construction';
  try {
    client.setOperator(payer, key);
    const ceiling = Hbar.fromTinybars(request.maxFeeTinybars);
    if (!mutation) {
      const bytes = await createPaidQuery(FileContentsQuery,request,client,key,persistPaidQueryPayment).setFileId(request.fileId).setMaxQueryPayment(ceiling).execute(client);
      return { fileId: FileId.fromString(request.fileId).toString(), contents: new TextDecoder('utf-8', { fatal: true }).decode(bytes) };
    }
    singleSubmission(mutation, id).setMaxTransactionFee(ceiling).freezeWith(client);
    stage = 'signing';
    const signed = await mutation.sign(key);
    stage = 'preparation';await persistSignedSubmission(signed,request,persist);
    stage = 'submission';
    const submitted = await signed.execute(client);
    stage = 'receipt';
    const receipt = await submitted.getReceipt(client);
    if (receipt.status.toString() !== 'SUCCESS') throw new Error('File mutation failed consensus.');
    return { transactionId: submitted.transactionId.toString(), status: 'SUCCESS',
      fileId: action === 'fileCreate' ? receipt.fileId.toString() : FileId.fromString(request.fileId).toString() };
  } catch (cause) {
    const error = new Error('Hedera file operation failed.',{cause});
    if(stage==='preparation')throw cause;
    error.transactionId = id?.toString();
    error.stage = stage;
    error.status = nativeFailureStatus(cause);
    throw error;
  } finally { client.close(); }
}

export async function execute(action, request, persistPreparedSubmission,persistPaidQueryPayment) {
  if(persistPreparedSubmission!==undefined&&typeof persistPreparedSubmission!=='function')throw new TypeError('Prepared Hedera persistence must be an awaited callback.');
  if(persistPaidQueryPayment!==undefined&&typeof persistPaidQueryPayment!=='function')throw new TypeError('Paid-query persistence must be an awaited callback.');
  if(action==='verifyNativePreparation'){
    if(persistPreparedSubmission)throw new Error('Read-only verification cannot prepare a submission.');
    return verifyPreparedIdentity(request.preparation,request.expectedPublicKey);
  }
  if(action==='verifyNativeQueryPreparation'){
    if(persistPreparedSubmission)throw new Error('Read-only query verification cannot prepare a submission.');
    return verifyPreparedQueryIdentity(request.preparation,request.expectedPublicKey,request.expectedNodes,request.maxPaymentTinybars,request.maxTransactionFeeTinybars);
  }
  if(['bytecodeFileCreate','bytecodeFileAppend','contractCreate','nftCollectionCreate','tokenCollectionDelete','nftMetadataUpdate'].includes(action)) return executeDeployment(action,request,{createClient,restoreKey,persistPreparedSubmission});
  if(persistPreparedSubmission&&!['storagePut','storageDelete','fileCreate','fileUpdate','fileDelete','transfer','tokenTransfer','tokenMint','tokenBurn','nftTransfer','nftMint','nftBurn','nftEscrowLock','nftEscrowUnlock','tokenEscrowLock','tokenEscrowUnlock'].includes(action))throw new Error('Prepared submission is not yet supported for this Hedera action.');
  if (['nftEscrowLock', 'nftEscrowUnlock'].includes(action)) return executeEscrow(action, request, { createClient, restoreKey,persistPreparedSubmission,persistPaidQueryPayment });
  if (['tokenEscrowLock', 'tokenEscrowUnlock'].includes(action)) return executeTokenEscrow(action, request, { createClient, restoreKey,persistPreparedSubmission,persistPaidQueryPayment });
  if (['generateMnemonic', 'restoreMnemonic'].includes(action)) {
    if (request.keyType !== 'ed25519') throw new Error('Mnemonic derivation requires the explicit standard Ed25519 algorithm.');
    if (action === 'restoreMnemonic' && (typeof request.seedPhrase !== 'string' || !request.seedPhrase.trim()))
      throw new Error('A validated Hedera SDK mnemonic is required.');
    const mnemonic = action === 'generateMnemonic' ? await Mnemonic.generate12() : await Mnemonic.fromString(request.seedPhrase);
    const key = await mnemonic.toStandardEd25519PrivateKey('', 0);
    return { privateKey: key.toStringDer(), publicKey: key.publicKey.toStringDer(), seedPhrase: mnemonic.toString() };
  }
  if (['storagePut', 'storageDelete'].includes(action)) return storageWrite(action, request,persistPreparedSubmission);
  if (['storageProbe','storageGet', 'storageList'].includes(action)) return storageRead(action, request,persistPaidQueryPayment);
  if (['fileCreate', 'fileUpdate', 'fileDelete', 'fileRead'].includes(action)) return executeFile(action, request,persistPreparedSubmission,persistPaidQueryPayment);
  if (action === 'generateKey') {
    const key = request.keyType === 'ed25519' ? PrivateKey.generateED25519()
      : request.keyType === 'ecdsa' ? PrivateKey.generateECDSA() : null;
    if (!key) throw new Error('Key generation requires an explicit algorithm.');
    // A generated key is not a ledger account; never fabricate an account ID.
    return { privateKey: key.toStringDer(), publicKey: key.publicKey.toStringDer() };
  }
  if (action === 'restoreKey') {
    const key = restoreKey(request);
    return { privateKey: key.toStringDer(), publicKey: key.publicKey.toStringDer() };
  }
  if (action === 'receipt') {
    const id = TransactionId.fromString(request.transactionId);
  const client = await createClient(request);
    try {
      const receipt = await new TransactionReceiptQuery().setTransactionId(id).setValidateStatus(false).execute(client);
      return { transactionId: id.toString(), status: receipt.status.toString() };
    } finally { client.close(); }
  }
  const nft = ['nftTransfer', 'nftMint', 'nftBurn'].includes(action);
  if (!nft && !['transfer', 'tokenTransfer', 'tokenMint', 'tokenBurn'].includes(action)) throw new Error('Unknown Hedera SDK operation.');
  if (action !== 'transfer') TokenId.fromString(request.tokenId);
  // Validate NFT payloads before opening a consensus client or paying for a query.
  const nftOperation = nft ? nftTransaction(action, request) : null;
  if (action === 'transfer' && (!/^[1-9][0-9]*$/.test(request.tinybars ?? '') || BigInt(request.tinybars) > 9223372036854775807n))
    throw new Error('Transfer requires positive exact int64 tinybars.');
  if (Buffer.byteLength(request.memo ?? '', 'utf8') > 100)
    throw new Error('Hedera transaction memo exceeds 100 UTF-8 bytes.');
  const sender = AccountId.fromString(request.sender);
  const recipient = AccountId.fromString(request.recipient);
  if (['transfer', 'tokenTransfer', 'nftTransfer'].includes(action) && sender.toString() === recipient.toString()) throw new Error('Sender and recipient must differ.');
  const key = restoreKey(request);
  if (request.publicKey && request.publicKey.toLowerCase() !== key.publicKey.toStringDer().toLowerCase())
    throw new Error('Signing key does not match the supplied public key.');
  if (typeof request.transactionId !== 'string' || !request.transactionId.trim())
    throw new Error('An explicit original transaction ID is required before submission.');
  const transactionId = TransactionId.fromString(request.transactionId);
  if (transactionId.accountId.toString() !== sender.toString())
    throw new Error('Transaction ID payer does not match the sender.');
  const client = await createClient(request);
  let transferStage = 'construction';
  try {
    client.setOperator(sender, key);
    const transfer = (nftOperation ?? (action === 'tokenMint' ? new TokenMintTransaction()
      : action === 'tokenBurn' ? new TokenBurnTransaction() : new TransferTransaction()))
      .setTransactionMemo(request.memo ?? '');
    if (action === 'transfer') {
      transfer.addHbarTransfer(sender, Hbar.fromTinybars('-' + request.tinybars))
        .addHbarTransfer(recipient, Hbar.fromTinybars(request.tinybars));
    } else {
      const info = await createPaidQuery(TokenInfoQuery,request,client,key,persistPaidQueryPayment).setTokenId(request.tokenId).execute(client);
      if (info.tokenType.toString() !== (nft ? 'NON_FUNGIBLE_UNIQUE' : 'FUNGIBLE_COMMON'))
        throw new Error('HTS token type does not match the requested operation.');
      if (['nftMint', 'nftBurn'].includes(action)) {
        if (info.treasuryAccountId.toString() !== recipient.toString())
          throw new Error('HTS NFT mint/burn affects the actual treasury; the requested account must match it.');
      } else if (!nft) {
        const units = tokenUnits(request.amount, info.decimals);
        if (action === 'tokenMint' || action === 'tokenBurn') {
          if (info.treasuryAccountId.toString() !== recipient.toString())
            throw new Error('HTS mint/burn affects the actual treasury; the requested account must match it.');
          transfer.setTokenId(request.tokenId).setAmount(units);
        } else transfer.addTokenTransfer(request.tokenId, sender, -units).addTokenTransfer(request.tokenId, recipient, units);
      }
    }
    singleSubmission(transfer, transactionId).freezeWith(client);
    transferStage = 'signing';
    const signed = await transfer.sign(key);
    transferStage = 'preparation';await persistSignedSubmission(signed,request,persistPreparedSubmission);
    transferStage = 'submission';
    const submitted = await signed.execute(client);
    transferStage = 'receipt';
    const receipt = await submitted.getReceipt(client);
    if (receipt.status.toString() !== 'SUCCESS') throw new Error('Consensus receipt reports failure.');
    return { transactionId: submitted.transactionId.toString(), status: receipt.status.toString(),
      ...(action === 'nftMint' ? { serials: receipt.serials.map(serial => serial.toString()) } : {}) };
  } catch (cause) {
    const error = new Error('Hedera transfer did not establish consensus success. Track the original transaction before retrying.');
    if(transferStage==='preparation')throw cause;
    error.transactionId = transactionId.toString();
    error.status = nativeFailureStatus(cause);
    error.stage = transferStage;
    throw error;
  } finally { client.close(); }
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  try {
    let input = '';
    for await (const chunk of process.stdin) {
      input += chunk;
      if (Buffer.byteLength(input, 'utf8') > 1048576) throw new Error('Request exceeds bridge input limit.');
    }
    const envelope = JSON.parse(input);
    let persistPreparedSubmission;
    if(process.env.OASIS_NATIVE_PREPARATION_PIPE||process.env.OASIS_NATIVE_PREPARATION_MODULE){
      if(['nftEscrowLock','tokenEscrowLock'].includes(envelope.action) &&
        (!envelope.request.approvalTransactionId || envelope.request.approvalTransactionId===envelope.request.transactionId ||
         process.env.OASIS_NATIVE_PREPARATION_APPROVAL_ID!==envelope.request.approvalTransactionId ||
         process.env.OASIS_NATIVE_PREPARATION_MUTATION_ID!==envelope.request.transactionId))
        throw new Error('Escrow lock requires its exact optional approval stage-aware parent preparation transport.');
      if(!process.env.OASIS_NATIVE_PREPARATION_PIPE||!process.env.OASIS_NATIVE_PREPARATION_MODULE)throw new Error('Complete parent preparation configuration required.');
      const {preparationClient}=await import(pathToFileURL(process.env.OASIS_NATIVE_PREPARATION_MODULE).href);
      persistPreparedSubmission=preparationClient(process.env.OASIS_NATIVE_PREPARATION_PIPE);
    }
    let querySequence;
    if(process.env.OASIS_NATIVE_QUERY_PREPARATION_PIPE||process.env.OASIS_NATIVE_QUERY_PREPARATION_MODULE){
      if(!process.env.OASIS_NATIVE_QUERY_PREPARATION_PIPE||!process.env.OASIS_NATIVE_QUERY_PREPARATION_MODULE)
        throw new Error('Complete parent query preparation configuration required.');
      const {preparationSequenceClient}=await import(pathToFileURL(process.env.OASIS_NATIVE_QUERY_PREPARATION_MODULE).href);
      querySequence=preparationSequenceClient(process.env.OASIS_NATIVE_QUERY_PREPARATION_PIPE,envelope.request.queryMaximumRequests);
    }
    const result=await execute(envelope.action,envelope.request,persistPreparedSubmission,querySequence?.persist);
    if(querySequence){
      try{await querySequence.complete();}
      catch(error){error.transactionId=envelope.request.transactionId;error.mutationTransactionId=envelope.request.transactionId;
        error.approvalTransactionId=result?.approvalTransactionId;error.stage='query-completion';throw error;}
    }
    process.stdout.write(JSON.stringify(result));
  } catch (error) {
    // Do not expose SDK exception text or stacks which could contain key input.
    process.stderr.write(JSON.stringify({ error: 'Hedera SDK operation failed',
      transactionId: error.transactionId, mutationTransactionId: error.mutationTransactionId,
      approvalTransactionId: error.approvalTransactionId, stage: error.stage, status: error.status }));
    process.exitCode = 1;
  }
}
