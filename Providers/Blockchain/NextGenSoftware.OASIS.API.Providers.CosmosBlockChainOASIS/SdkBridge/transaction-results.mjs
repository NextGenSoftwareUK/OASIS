import { TimeoutError } from '@cosmjs/stargate';
import { fromBech32 } from '@cosmjs/encoding';
import { decodeTxRaw } from '@cosmjs/proto-signing';

export function verifiedNftCustodyReceipt(tx, expected, registry) {
  if (!tx || tx.hash !== expected.transactionHash || tx.code !== 0
      || !Number.isSafeInteger(tx.height) || tx.height <= 0)
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
    throw new Error(`Cosmos upload returned an invalid code ID. Original transaction ${result.transactionHash}. Reconcile submission before retrying.`);
  return result;
}

export function verifiedInstantiation(result, expectedPrefix) {
  verifiedContractExecution(result);
  try {
    const decoded = fromBech32(result.contractAddress, 90);
    if (decoded.data.length === 0 || (expectedPrefix && decoded.prefix !== expectedPrefix)) throw new Error('Invalid contract address');
  } catch {
    throw new Error(`Cosmos instantiation returned an invalid contract address. Original transaction ${result.transactionHash}. Reconcile submission before retrying.`);
  }
  return result;
}

export function verifiedContractExecution(result) {
  const hash = result?.transactionHash;
  const validHash = typeof hash === 'string' && /^[0-9a-fA-F]{64}$/.test(hash);
  if (!validHash || !Number.isSafeInteger(result.height) || result.height <= 0)
    throw new Error(`Cosmos contract execution lacks valid committed hash/height evidence.${validHash ? ` Original transaction ${hash}.` : ''} Reconcile submission before retrying.`);
  return result;
}

export function cosmosFailureMessage(error) {
  if (error instanceof TimeoutError && /^[0-9a-fA-F]{64}$/.test(error.txId ?? ''))
    return `Cosmos submission outcome is not verified; original transaction ${error.txId}. Reconcile this transaction before retrying.`;
  return error instanceof Error ? error.message : String(error);
}

export function verifiedBankTransfer(result) {
  if (!result || !Number.isSafeInteger(result.code) || result.code < 0
      || !/^[0-9a-fA-F]{64}$/.test(result.transactionHash ?? ''))
    throw new Error('Cosmos transfer response does not establish an exact execution code and transaction hash. Reconcile submission before retrying.');
  if (result.code !== 0)
    throw new Error(`Cosmos transfer failed with execution code ${result.code}; transaction ${result.transactionHash}. Reconcile this original transaction before retrying.`);
  verifiedContractExecution(result);
  return result;
}
