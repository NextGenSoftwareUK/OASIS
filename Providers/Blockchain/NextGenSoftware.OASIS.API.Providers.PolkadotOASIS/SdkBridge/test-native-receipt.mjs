import assert from 'node:assert/strict';
import { ApiPromise } from '@polkadot/api';
import { ReadOnlyHttpProvider } from './read-only-provider.mjs';
import { readFinalizedReceipt } from './finalized-receipt.mjs';
import { readVerifiedOriginalRange } from './original-outcome.mjs';

if (process.env.OASIS_POLKADOT_READONLY_NATIVE !== '1') throw new Error('Explicit read-only native opt-in required.');
const api = await ApiPromise.create({ provider: new ReadOnlyHttpProvider('https://rpc.polkadot.io'), noInitWarn: true });
try {
  assert.equal(api.genesisHash.toHex(), '0x91b171bb158e2d3848fa23a9f1c25182fb8e20313b2c1eb49219da7a70ce90c3');
  let hash = await api.rpc.chain.getFinalizedHead();
  let receipt;
  // Bounded fixture discovery ONLY, not the provider's transaction lookup contract.
  for (let inspected = 0; inspected < 20 && !receipt; inspected++) {
    const block = await api.rpc.chain.getBlock(hash);
    const at = await api.at(hash);
    const events = await at.query.system.events();
    const paid = events.find(({ phase, event }) => phase.isApplyExtrinsic && event.section === 'transactionPayment' && event.method === 'TransactionFeePaid');
    if (paid) {
      const index = paid.phase.asApplyExtrinsic.toNumber();
      receipt = await readFinalizedReceipt(api, { blockHash: hash.toHex(), transactionHash: block.block.extrinsics[index].hash.toHex() });
      assert.equal(receipt.actualFeeUnits, paid.event.data[1].toString());
      assert.equal(receipt.tipUnits, paid.event.data[2].toString());
      assert.equal(receipt.extrinsicIndex, index);
      assert.equal(receipt.finalized, true);
    } else hash = block.block.header.parentHash;
  }
  assert.ok(receipt, 'No paid native extrinsic found in the bounded fixture discovery window.');
  // Native range reader proof using public block identity; NOT independent signed-original verification.
  const ranged = await readVerifiedOriginalRange(api, { transactionHash: receipt.transactionHash,
    sender: api.createType('AccountId32', receipt.payer).toHex() }, { fromBlockNumber: receipt.blockNumber, maximumBlocks: 1 });
  assert.equal(ranged.found, true);
  assert.equal(ranged.receipt.transactionHash, receipt.transactionHash);
  assert.equal(ranged.receipt.actualFeeUnits, receipt.actualFeeUnits);
  console.log(JSON.stringify({ network: 'Polkadot', submitted: false, nativeRangeReaderFound: true, receipt }));
} finally { await api.disconnect(); }
