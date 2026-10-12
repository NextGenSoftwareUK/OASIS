import { TimeoutError } from '@cosmjs/stargate';
import { fromBech32 } from '@cosmjs/encoding';

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
  return result;
}
