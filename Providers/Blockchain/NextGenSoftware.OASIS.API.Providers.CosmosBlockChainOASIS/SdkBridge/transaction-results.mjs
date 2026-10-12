export function verifiedBankTransfer(result) {
  if (!result || !Number.isSafeInteger(result.code) || result.code < 0
      || !/^[0-9a-fA-F]{64}$/.test(result.transactionHash ?? ''))
    throw new Error('Cosmos transfer response does not establish an exact execution code and transaction hash. Reconcile submission before retrying.');
  if (result.code !== 0)
    throw new Error(`Cosmos transfer failed with execution code ${result.code}; transaction ${result.transactionHash}. Reconcile this original transaction before retrying.`);
  return result;
}
