import { dispatchOutcome } from './finalized-receipt.mjs';

const unsigned = /^(0|[1-9][0-9]*)$/;
const hash = /^0x[0-9a-f]{64}$/i;
function height(value) {
  if (typeof value !== 'string' || value.length > 20 || !unsigned.test(value) || BigInt(value) >= (1n << 64n))
    throw new Error('History heights must be canonical unsigned u64 strings.');
  return BigInt(value);
}
export function validateHistoryRequest(request) {
  if (typeof request.address !== 'string' || !request.address.trim()) throw new Error('Explicit history wallet address required.');
  const from = request.fromBlock == null ? 0n : height(request.fromBlock);
  const to = request.toBlock == null ? null : height(request.toBlock);
  if (to != null && (to < from || to - from >= 128n)) throw new Error('Choose an inclusive history range of at most128blocks.');
  return { address: request.address, from, to };
}
const account = (api, value) => api.createType('AccountId32', value.toString()).toHex();
const units = value => {
  const text = value.toString();
  if (!unsigned.test(text) || BigInt(text) >= (1n << 128n)) throw new Error('Invalid history u128 asset/native units.');
  return text;
};
function precision(value) {
  if (!Number.isInteger(value) || value < 0 || value > 38) throw new Error('Unsupported authoritative history precision.');
  return value;
}

// Effects are actual events, not guesses from requested call arguments. Other signed calls retain
// their identity/status with no invented financial amount. This is not an arbitrary-pallet state index.
export async function collectWalletBlock(api, at, block, events, address, nativeDecimals) {
  const rows = [];
  const timestamp = (await at.query.timestamp.now()).toString();
  if (!unsigned.test(timestamp) || BigInt(timestamp) > 253402300799999n) throw new Error('Invalid authoritative block timestamp.');
  for (let index = 0; index < block.extrinsics.length; index++) {
    const extrinsic = block.extrinsics[index];
    const signer = extrinsic.isSigned ? account(api, extrinsic.signer) : '';
    const own = [];
    events.forEach((record, eventIndex) => {
      if (record.phase.isApplyExtrinsic && record.phase.asApplyExtrinsic.toNumber() === index)
        own.push({ event: record.event, eventIndex });
    });
    const effects = [];
    for (const { event, eventIndex } of own) {
      const data = event.data;
      let from, to, raw, token = null, decimals = nativeDecimals;
      if (event.section === 'balances' && event.method === 'Transfer') {
        if (data.length !== 3) throw new Error('Invalid native Transfer event shape.');
        from = account(api, data[0]); to = account(api, data[1]); raw = units(data[2]);
      } else if (event.section === 'assets' && ['Transferred', 'Issued', 'Burned'].includes(event.method)) {
        if (data.length !== (event.method === 'Transferred' ? 4 : 3)) throw new Error('Invalid Assets history event shape.');
        token = units(data[0]);
        from = event.method === 'Issued' ? '' : account(api, data[1]);
        to = event.method === 'Burned' ? '' : account(api, data[event.method === 'Transferred' ? 2 : 1]);
        raw = units(data[event.method === 'Transferred' ? 3 : 2]);
        if (from !== address && to !== address) continue;
        if (!at.query.assets?.metadata) throw new Error('Historical asset metadata is unavailable.');
        decimals = precision((await at.query.assets.metadata(token)).decimals.toNumber());
      } else continue;
      if (from === address || to === address) effects.push({ eventIndex, from, to, rawAmount: raw,
        decimals, token, amountUnit: token == null ? 'native-base-unit' : `asset:${token}:base-unit`,
        effect: `${event.section}.${event.method}` });
    }
    if (!effects.length && signer !== address) continue;
    const transactionHash = extrinsic.hash.toHex();
    if (!hash.test(transactionHash)) throw new Error('Invalid original history transaction hash.');
    const outcome = dispatchOutcome(own.map(record => record.event));
    const operation = `${extrinsic.method.section}.${extrinsic.method.method}`;
    if (!extrinsic.method.section || !extrinsic.method.method) throw new Error('Historical call metadata is unavailable.');
    const common = { transactionHash, extrinsicIndex: index, timestampMilliseconds: timestamp, operation, signer, ...outcome };
    if (effects.length) effects.forEach(effect => rows.push({ ...common, ...effect, amountKind: 'applied-event' }));
    else rows.push({ ...common, eventIndex: null, from: signer, to: '', rawAmount: null, decimals: null,
      token: null, amountUnit: null, effect: null, amountKind: 'not-interpreted' });
  }
  return rows;
}

export async function readWalletHistory(api, request, nativeDecimals) {
  const selection = validateHistoryRequest(request); // Capture before awaits.
  precision(nativeDecimals);
  const address = account(api, selection.address);
  const finalizedHash = await api.rpc.chain.getFinalizedHead();
  const snapshot = await api.rpc.chain.getHeader(finalizedHash);
  const ceiling = snapshot.number.toBigInt();
  const to = selection.to ?? ceiling;
  if (selection.from > to || to > ceiling || to - selection.from >= 128n)
    throw new Error('Choose an available inclusive finalized history range of at most128blocks using FromBlock/ToBlock.');
  const rows = [];
  let previousHash = null;
  for (let number = selection.from; number <= to; number++) {
    const blockHash = await api.rpc.chain.getBlockHash(number.toString());
    if (!hash.test(blockHash.toHex())) throw new Error('Invalid canonical history block hash.');
    // The pinned SDK swaps to the historic registry for getBlock(hash); at(hash) uses that block's storage metadata.
    const signed = await api.rpc.chain.getBlock(blockHash);
    const block = signed.block;
    if (block.header.number.toBigInt() !== number || block.header.hash.toHex() !== blockHash.toHex()
        || (previousHash != null && block.header.parentHash.toHex() !== previousHash))
      throw new Error('Canonical history block identity/parent linkage changed.');
    const at = await api.at(blockHash);
    if (!at.query.system?.events || !at.query.timestamp?.now) throw new Error('Required historical events/timestamp storage is unavailable.');
    const effects = await collectWalletBlock(api, at, block, await at.query.system.events(), address, nativeDecimals);
    effects.forEach(row => rows.push({ ...row, blockHash: blockHash.toHex(), blockNumber: number.toString() }));
    previousHash = blockHash.toHex();
  }
  if ((await api.rpc.chain.getBlockHash(to.toString())).toHex() !== previousHash
      || (await api.rpc.chain.getBlockHash(ceiling.toString())).toHex() !== finalizedHash.toHex())
    throw new Error('Finalized history anchor changed; no partial result is returned.');
  return { address, genesisHash: api.genesisHash.toHex(), finalized: true, fromBlock: selection.from.toString(),
    toBlock: to.toString(), finalizedSnapshotBlock: ceiling.toString(), rows };
}
