import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { ApiPromise } from '@polkadot/api';
import { ReadOnlyHttpProvider } from './read-only-provider.mjs';
import { readOriginalNativeFee } from './original-outcome.mjs';

if (process.env.OASIS_POLKADOT_LOCAL_NATIVE !== '1' || !process.env.OASIS_POLKADOT_LOCAL_NATIVE_JOURNAL)
  throw new Error('Explicit local-native evidence prefix required.');
const api = await ApiPromise.create({ provider: new ReadOnlyHttpProvider('http://127.0.0.1:19944'), noInitWarn: true });
try {
  assert.equal(api.genesisHash.toHex(), '0xe8cf5d7d636a08721efaa17acc52ed3d895fe2468dccb2520cc8e249ddb6b208');
  for (const kind of ['sync', 'async']) {
    const stored = JSON.parse(readFileSync(`${process.env.OASIS_POLKADOT_LOCAL_NATIVE_JOURNAL}.${kind}.completed.json`, 'utf8'));
    const receipt = await readOriginalNativeFee(api, stored.original);
    assert.equal(receipt.transactionHash, stored.original.transactionHash);
    assert.equal(receipt.actualFeeUnits, stored.actualFeeUnits);
    // SDK display blockNumber is not the signed checkpoint hash. A forged hint must not skip history.
    const changed = structuredClone(stored.original);
    const document = JSON.parse(Buffer.from(changed.signDocBase64, 'base64').toString('utf8'));
    document.signingPayload.blockNumber = '0xffffffff';
    changed.signDocBase64 = Buffer.from(JSON.stringify(document), 'utf8').toString('base64');
    const anchored = await readOriginalNativeFee(api, changed);
    assert.equal(anchored.transactionHash, receipt.transactionHash);
    assert.equal(anchored.actualFeeUnits, receipt.actualFeeUnits);
    console.log(JSON.stringify({ kind, transactionHash: receipt.transactionHash, actualFeeUnits: receipt.actualFeeUnits,
      signedCheckpointUsedInsteadOfUnboundHeight: true, submitted: false }));
  }
} finally { await api.disconnect(); }
