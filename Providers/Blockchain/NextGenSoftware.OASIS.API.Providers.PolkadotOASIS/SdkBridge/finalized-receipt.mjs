const hashPattern = /^0x[0-9a-f]{64}$/i;

export function validateReceiptRequest(request) {
  if (!hashPattern.test(request.blockHash) || !hashPattern.test(request.transactionHash))
    throw new Error('An explicit block hash and transaction hash are required.');
}

// An explicit block avoids pretending that a bounded scan is a complete transaction index.
export async function readFinalizedExtrinsicContext(api, request) {
  validateReceiptRequest(request);
  const finalizedHash = await api.rpc.chain.getFinalizedHead();
  const finalized = await api.rpc.chain.getHeader(finalizedHash);
  const header = await api.rpc.chain.getHeader(request.blockHash);
  if (header.number.toBigInt() > finalized.number.toBigInt())
    throw new Error('Requested block is not finalized.');
  const canonical = await api.rpc.chain.getBlockHash(header.number);
  if (canonical.toHex().toLowerCase() !== request.blockHash.toLowerCase())
    throw new Error('Requested block is not on the finalized canonical chain.');
  const signedBlock = await api.rpc.chain.getBlock(request.blockHash);
  const matches = [];
  signedBlock.block.extrinsics.forEach((extrinsic, index) => {
    if (extrinsic.hash.toHex().toLowerCase() === request.transactionHash.toLowerCase()) matches.push(index);
  });
  if (matches.length !== 1) throw new Error('Transaction must occur exactly once in the specified finalized block.');
  const index = matches[0];
  const at = await api.at(request.blockHash);
  const events = await at.query.system.events();
  const own = events.filter(({ phase }) => phase.isApplyExtrinsic && phase.asApplyExtrinsic.toNumber() === index)
    .map(({ event }) => event);
  return { header, index, own };
}

export function dispatchAndFeeReceipt({ header, index, own }, request) {
  const outcomes = own.filter(event => event.section === 'system' && ['ExtrinsicSuccess', 'ExtrinsicFailed'].includes(event.method));
  if (outcomes.length !== 1) throw new Error('Exactly one terminal dispatch event is required.');
  const fees = own.filter(event => event.section === 'transactionPayment' && event.method === 'TransactionFeePaid');
  if (fees.length !== 1) throw new Error('Exactly one native TransactionFeePaid event is required; asset fees are not inferred.');
  const [payer, actualFee, tip] = fees[0].data;
  const units = actualFee.toString();
  const tipUnits = tip.toString();
  for (const value of [units, tipUnits])
    if (!/^(0|[1-9][0-9]*)$/.test(value) || BigInt(value) >= (1n << 128n)) throw new Error('Invalid native fee amount.');
  if (BigInt(tipUnits) > BigInt(units)) throw new Error('Tip exceeds total actual fee.');
  return {
    transactionHash: request.transactionHash.toLowerCase(), blockHash: request.blockHash.toLowerCase(),
    blockNumber: header.number.toString(), extrinsicIndex: index, finalized: true,
    dispatchSucceeded: outcomes[0].method === 'ExtrinsicSuccess',
    dispatchError: outcomes[0].method === 'ExtrinsicFailed' ? outcomes[0].data[0].toString() : null,
    payer: payer.toString(), actualFeeUnits: units, tipUnits,
    // FRAME actual_fee already includes the tip. Never add it a second time.
    feeCoverage: 'finalized-native-transaction-payment-event-tip-included'
  };
}

export async function readFinalizedReceipt(api, request) {
  return dispatchAndFeeReceipt(await readFinalizedExtrinsicContext(api, request), request);
}
