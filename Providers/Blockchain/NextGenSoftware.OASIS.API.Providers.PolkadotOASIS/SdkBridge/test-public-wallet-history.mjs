// Explicit opt-in, one public finalized block. No keys, signing, submission or index/full-wallet claim.
import assert from 'node:assert/strict';
import { ApiPromise } from '@polkadot/api';
import { ReadOnlyHttpProvider } from './read-only-provider.mjs';
import { readWalletHistory } from './wallet-history.mjs';
if (process.env.OASIS_POLKADOT_READONLY_NATIVE !== '1') throw new Error('Explicit public read-only opt-in required.');
const deadline = setTimeout(() => { process.stderr.write('Read-only public history deadline exceeded.\n'); process.exit(1); }, 45000);
deadline.unref();
const api = await ApiPromise.create({ provider: new ReadOnlyHttpProvider('https://rpc.polkadot.io'), noInitWarn: true });
try {
  const genesis = '0x91b171bb158e2d3848fa23a9f1c25182fb8e20313b2c1eb49219da7a70ce90c3';
  assert.equal(api.genesisHash.toHex(), genesis);
  const hash = await api.rpc.chain.getFinalizedHead();
  const header = await api.rpc.chain.getHeader(hash);
  const number = header.number.toString();
  const properties = await api.rpc.system.properties();
  assert.equal(properties.tokenDecimals.isSome, true);
  assert.equal(properties.tokenDecimals.unwrap().length, 1);
  const wallet = '0x189dac29296d31814dc8c56cf3d36a0543372bba7538fa322a4aebfebc39e056'; // Public unfunded fixture address only.
  const result = await readWalletHistory(api, { address: wallet, fromBlock: number, toBlock: number }, properties.tokenDecimals.unwrap()[0].toNumber());
  assert.equal(result.genesisHash, genesis); assert.equal(result.finalized, true);
  assert.equal(result.fromBlock, number); assert.equal(result.toBlock, number);
  for (const row of result.rows) assert.ok(row.from === wallet || row.to === wallet || row.signer === wallet);
  console.log(JSON.stringify({ genesis, blockNumber: number, blockHash: hash.toHex(), wallet, rows: result.rows.length,
    currentPublicMetadataHistoryRead: true, signed: false, submitted: false, fullWalletHistoryClaim: false }));
} finally { await api.disconnect(); clearTimeout(deadline); }
