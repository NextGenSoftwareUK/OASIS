import { TimeoutError } from '@cosmjs/stargate';
import { fromBech32 } from '@cosmjs/encoding';
import { decodeTxRaw } from '@cosmjs/proto-signing';
import { createHash } from 'node:crypto';
import { AsyncLocalStorage } from 'node:async_hooks';

export class CosmosNativeMutationReconciliationError extends Error {
  constructor(message, transactionHash, executionSucceeded, cause) {
    super(message, cause ? { cause } : undefined);
    this.nativeMutation = { transactionHash: typeof transactionHash === 'string' && /^[0-9a-fA-F]{64}$/.test(transactionHash) ? transactionHash : null, executionSucceeded };
  }
}

const nativeContext = new AsyncLocalStorage();
export async function withCosmosNativeEvidence(operation) {
  return nativeContext.run({ evidence: null }, async () => {
    try { return await operation(); }
    catch (error) {
      const evidence = nativeContext.getStore().evidence;
      if (evidence && !(error instanceof CosmosNativeMutationReconciliationError))
        throw new CosmosNativeMutationReconciliationError('Cosmos operation did not complete after submission began; reconcile the original transaction before retrying.', evidence.transactionHash, evidence.executionSucceeded, error);
      throw error;
    }
  });
}

export function cosmosErrorEnvelope(error) {
  const nativeMutation = error instanceof CosmosNativeMutationReconciliationError ? error.nativeMutation
    : error instanceof TimeoutError ? { transactionHash: typeof error.txId === 'string' && /^[0-9a-fA-F]{64}$/.test(error.txId) ? error.txId : null, executionSucceeded: null } : null;
  return { ok: false, error: cosmosFailureMessage(error), ...(nativeMutation ? { nativeMutation } : {}) };
}

// CosmJS execute/upload/instantiate discard failed DeliverTxResponse fields in plain errors.
// Validate the one real broadcast response before their higher-level mapping runs.
export function guardCosmosSigningClient(client, persistPreparedSubmission) {
  if (persistPreparedSubmission !== undefined && typeof persistPreparedSubmission !== 'function')
    throw new TypeError('Prepared Cosmos submission persistence must be an awaited callback');
  const broadcast = client.broadcastTx.bind(client);
  client.broadcastTx = async (bytes, ...args) => {
    if (!(bytes instanceof Uint8Array)) throw new Error('Cosmos broadcast requires exact signed transaction bytes');
    const signedBytes = Uint8Array.from(bytes);
    const hash = createHash('sha256').update(signedBytes).digest('hex').toUpperCase();
    // Persistence failure is pre-submission: no broadcast or native-outcome claim.
    // The callback must durably bind this identity to its authenticated operation.
    if (persistPreparedSubmission) await persistPreparedSubmission({ transactionHash: hash });
    const context = nativeContext.getStore();
    if (context) context.evidence = { transactionHash: hash, executionSucceeded: null };
    let receipt;
    try { receipt = await broadcast(signedBytes, ...args); }
    catch (error) { throw new CosmosNativeMutationReconciliationError('Cosmos original signed submission outcome is unverified; reconcile before retrying.', hash, null, error); }
    if (typeof receipt?.transactionHash !== 'string' || receipt.transactionHash.toUpperCase() !== hash)
      throw new CosmosNativeMutationReconciliationError('Cosmos delivery identity does not match the original signed transaction; reconcile before retrying.', hash, null);
    if (context && Number.isSafeInteger(receipt.height) && receipt.height > 0
        && Number.isInteger(receipt.code) && receipt.code >= 0 && receipt.code <= 4294967295)
      context.evidence.executionSucceeded = receipt.code === 0;
    return receipt;
  };
  const submit = client.signAndBroadcast.bind(client);
  client.signAndBroadcast = async (...args) => verifiedBankTransfer(await submit(...args));
  return client;
}

export async function indexedHistorySnapshot(client, address) {
  fromBech32(address, 90);
  const height = await client.getHeight();
  if (!Number.isSafeInteger(height) || height <= 0) throw new Error('Cosmos history requires a committed snapshot height');
  const bound = ` AND tx.height<=${height}`;
  const received = await client.searchTx(`transfer.recipient='${address}'${bound}`);
  const sent = await client.searchTx(`message.sender='${address}'${bound}`);
  const indexed = new Map();
  for (const tx of [...received, ...sent]) {
    if (!/^[0-9A-F]{64}$/.test(tx.hash ?? '') || !Number.isSafeInteger(tx.height) || tx.height <= 0 || tx.height > height
        || !Number.isSafeInteger(tx.txIndex) || tx.txIndex < 0 || !Number.isInteger(tx.code) || tx.code < 0 || tx.code > 4294967295
        || !(tx.tx instanceof Uint8Array)
        || createHash('sha256').update(tx.tx).digest('hex').toUpperCase() !== tx.hash)
      throw new Error('Cosmos indexed history does not establish exact transaction identity within its snapshot');
    const prior = indexed.get(tx.hash);
    if (prior && (prior.height !== tx.height || prior.txIndex !== tx.txIndex || prior.code !== tx.code
        || !Buffer.from(prior.tx).equals(Buffer.from(tx.tx))))
      throw new Error('Cosmos sender and recipient indexes disagree on the original transaction');
    indexed.set(tx.hash, tx);
  }
  return [...indexed.values()].sort((a, b) => a.height - b.height || a.txIndex - b.txIndex);
}

export function cosmosNativeFeeReceipt(tx, requestedHash) {
  if (!(tx?.tx instanceof Uint8Array)
      || createHash('sha256').update(tx.tx).digest('hex').toUpperCase() !== tx.hash)
    throw new Error('Cosmos fee receipt identity does not match exact signed transaction bytes');
  if (!tx || !/^[0-9a-fA-F]{64}$/.test(requestedHash ?? '') || tx.hash !== requestedHash.toUpperCase()
      || !Number.isSafeInteger(tx.height) || tx.height <= 0 || !Number.isInteger(tx.code) || tx.code < 0 || tx.code > 4294967295
      || typeof tx.gasUsed !== 'bigint' || tx.gasUsed < 0n || typeof tx.gasWanted !== 'bigint' || tx.gasWanted < 0n)
    throw new Error('Cosmos fee receipt lacks exact committed identity, execution code or gas evidence');
  const entries = tx.events.filter(event => event.type === 'tx' && event.attributes.some(attribute => attribute.key === 'fee'));
  if (entries.length !== 1) throw new Error('Cosmos receipt must contain one observed fee deduction event');
  function attribute(key) {
    const values = entries[0].attributes.filter(item => item.key === key);
    if (values.length !== 1 || typeof values[0].value !== 'string') throw new Error('Ambiguous or missing Cosmos fee event attribute');
    return values[0].value;
  }
  const payer = attribute('fee_payer');
  if (!fromBech32(payer, 90).data.length) throw new Error('Invalid actual Cosmos fee payer');
  const value = attribute('fee');
  const fees = value === '' ? [] : value.split(',').map(coin => {
    const match = /^(0|[1-9][0-9]*)([a-zA-Z][a-zA-Z0-9/:._-]{2,127})$/.exec(coin);
    if (!match || match[1].length > 78 || BigInt(match[1]) >= (1n << 256n)) throw new Error('Malformed exact Cosmos fee coin');
    return { denom: match[2], amount: match[1] };
  });
  if (new Set(fees.map(coin => coin.denom)).size !== fees.length) throw new Error('Duplicate Cosmos fee denominations');
  const fee = decodeTxRaw(tx.tx).authInfo.fee;
  if (!fee) throw new Error('Signed Cosmos fee declaration is absent');
  const normalize = coins => [...coins].filter(coin => coin.amount !== '0').map(coin => `${coin.amount}${coin.denom}`).sort();
  if (JSON.stringify(normalize(fees)) !== JSON.stringify(normalize(fee.amount)))
    throw new Error('Observed Cosmos fee deduction disagrees with the signed transaction');
  if ((fee.granter && payer !== fee.granter) || (!fee.granter && fee.payer && payer !== fee.payer))
    throw new Error('Observed Cosmos fee payer disagrees with explicit payer/granter');
  return { transactionHash: tx.hash, success: tx.code === 0, executionCode: tx.code, height: tx.height,
    payer, feeGranter: fee.granter || null, declaredPayer: fee.payer || null, fees,
    gasUsed: tx.gasUsed.toString(), gasWanted: tx.gasWanted.toString(), basis: 'observed-ante-fee-deduction' };
}

export function verifiedNftCustodyReceipt(tx, expected, registry) {
  if (!tx || tx.hash !== expected.transactionHash || tx.code !== 0
      || !Number.isSafeInteger(tx.height) || tx.height <= 0
      || !(tx.tx instanceof Uint8Array)
      || createHash('sha256').update(tx.tx).digest('hex').toUpperCase() !== tx.hash)
    throw new Error('Cosmos source receipt does not establish the exact successful custody transaction');
  const raw = decodeTxRaw(tx.tx);
  const matching = raw.body.messages.filter(message => {
    if (message.typeUrl !== '/cosmwasm.wasm.v1.MsgExecuteContract') return false;
    const value = registry.decode(message);
    if (value.contract !== expected.contractAddress || !value.sender || value.sender === expected.custody) return false;
    const payload = JSON.parse(new TextDecoder('utf-8', { fatal: true }).decode(value.msg));
    const transfer = payload.asset?.message?.nft_transfer;
    return transfer?.token_id === expected.tokenId && transfer?.recipient === expected.custody;
  });
  if (matching.length !== 1) throw new Error('Cosmos source receipt does not uniquely transfer the exact NFT into custody');
  return tx;
}

export function bankTransferLegs(encoded, registry) {
  if (encoded.typeUrl === '/cosmos.bank.v1beta1.MsgSend') {
    const value = registry.decode(encoded);
    return [{from:value.fromAddress,to:value.toAddress,coins:value.amount,transferIndex:0,operation:encoded.typeUrl}];
  }
  if (encoded.typeUrl === '/cosmos.bank.v1beta1.MsgMultiSend') {
    const value = registry.decode(encoded);
    if (value.inputs.length !== 1 || !value.inputs[0].address || value.outputs.length === 0)
      throw new Error('Cosmos multi-send requires one identifiable input and outputs; ambiguous transfer pairings are not fabricated');
    return value.outputs.map((output,transferIndex) => ({from:value.inputs[0].address,to:output.address,coins:output.coins,transferIndex,operation:encoded.typeUrl}));
  }
  return [];
}

export function verifiedUpload(result) {
  verifiedContractExecution(result);
  if (!Number.isSafeInteger(result.codeId) || result.codeId <= 0)
    throw new CosmosNativeMutationReconciliationError(`Cosmos upload returned an invalid code ID. Original transaction ${result.transactionHash}. Reconcile submission before retrying.`, result.transactionHash, true);
  return result;
}

export function verifiedInstantiation(result, expectedPrefix) {
  verifiedContractExecution(result);
  try {
    const decoded = fromBech32(result.contractAddress, 90);
    if (decoded.data.length === 0 || (expectedPrefix && decoded.prefix !== expectedPrefix)) throw new Error('Invalid contract address');
  } catch {
    throw new CosmosNativeMutationReconciliationError(`Cosmos instantiation returned an invalid contract address. Original transaction ${result.transactionHash}. Reconcile submission before retrying.`, result.transactionHash, true);
  }
  return result;
}

export function verifiedContractExecution(result) {
  const hash = result?.transactionHash;
  const validHash = typeof hash === 'string' && /^[0-9a-fA-F]{64}$/.test(hash);
  if (!validHash || !Number.isSafeInteger(result.height) || result.height <= 0)
    throw new CosmosNativeMutationReconciliationError(`Cosmos contract execution lacks valid committed hash/height evidence.${validHash ? ` Original transaction ${hash}.` : ''} Reconcile submission before retrying.`, hash, null);
  return result;
}

export function cosmosFailureMessage(error) {
  if (error instanceof TimeoutError)
    return `Cosmos submission outcome is not verified; ${typeof error.txId === 'string' && /^[0-9a-fA-F]{64}$/.test(error.txId) ? `original transaction ${error.txId}` : 'original transaction identity is unavailable'}. Reconcile this transaction before retrying.`;
  return error instanceof Error ? error.message : String(error);
}

export function verifiedBankTransfer(result) {
  if (!result || !Number.isSafeInteger(result.code) || result.code < 0 || result.code > 4294967295
      || !/^[0-9a-fA-F]{64}$/.test(result.transactionHash ?? ''))
    throw new CosmosNativeMutationReconciliationError('Cosmos transfer response does not establish an exact execution code and transaction hash. Reconcile submission before retrying.', result?.transactionHash, null);
  if (result.code !== 0)
    throw new CosmosNativeMutationReconciliationError(`Cosmos transfer failed with execution code ${result.code}; transaction ${result.transactionHash}. Reconcile this original transaction before retrying.`, result.transactionHash, Number.isSafeInteger(result.height) && result.height > 0 ? false : null);
  verifiedContractExecution(result);
  return result;
}
