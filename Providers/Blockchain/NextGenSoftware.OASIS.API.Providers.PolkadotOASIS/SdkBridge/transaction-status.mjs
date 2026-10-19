import { readFinalizedExtrinsicContext, dispatchOutcome } from './finalized-receipt.mjs';

const hash = /^0x[0-9a-f]{64}$/i;
export function validateStatusRequest(request) {
  if (typeof request.transactionHash !== 'string' || !hash.test(request.transactionHash)
      || (request.blockHash != null && (typeof request.blockHash !== 'string' || !hash.test(request.blockHash))))
    throw new Error('An exact transaction hash and, if supplied, exact block hash are required.');
  return { transactionHash: request.transactionHash.toLowerCase(), blockHash: request.blockHash?.toLowerCase() };
}

// Standard Substrate RPC has no arbitrary transaction-hash index. An explicit finalized
// block is efficient; otherwise search ONE frozen finalized ancestry, bounded by caller/process
// deadline. Absence/pruning/timeout never means rejection, zero fees or permission to replay.
export async function readTransactionStatus(api, request) {
  const selected = validateStatusRequest(request);
  let blockHash = selected.blockHash;
  if (!blockHash) {
    const finalized = await api.rpc.chain.getFinalizedHead();
    let cursor = finalized.toHex().toLowerCase();
    if (!hash.test(cursor)) throw new Error('Invalid finalized status snapshot.');
    let expectedHeight = null;
    while (true) {
      const signed = await api.rpc.chain.getBlock(cursor);
      const block = signed.block;
      const number = block.header.number.toBigInt();
      if (number < 0n || block.header.hash.toHex().toLowerCase() !== cursor
          || (expectedHeight != null && number !== expectedHeight))
        throw new Error('Finalized status ancestry is inconsistent.');
      const matches = block.extrinsics.filter(ext => ext.hash.toHex().toLowerCase() === selected.transactionHash);
      if (matches.length > 1) throw new Error('Ambiguous original transaction identity.');
      if (matches.length === 1) { blockHash = cursor; break; }
      if (number === 0n) break;
      cursor = block.header.parentHash.toHex().toLowerCase();
      if (!hash.test(cursor)) throw new Error('Invalid finalized status parent identity.');
      expectedHeight = number - 1n;
    }
    if (!blockHash) throw new Error('Transaction absent from finalized snapshot; pending/unknown outcome remains unresolved, never replay.');
  }
  const context = await readFinalizedExtrinsicContext(api, { ...selected, blockHash });
  const outcome = dispatchOutcome(context.own);
  return { transactionHash: selected.transactionHash, blockHash, blockNumber: context.header.number.toString(),
    extrinsicIndex: context.index, finalized: true, status: outcome.dispatchSucceeded ? 'Completed' : 'Failed', ...outcome };
}
