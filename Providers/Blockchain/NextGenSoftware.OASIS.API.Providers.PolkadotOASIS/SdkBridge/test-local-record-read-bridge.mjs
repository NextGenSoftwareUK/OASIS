import assert from 'node:assert/strict';
import { test } from 'node:test';
import { spawnSync } from 'node:child_process';
import { resolve } from 'node:path';

if (process.env.OASIS_POLKADOT_LOCAL_NATIVE !== '1') throw new Error('Explicit retained local fixture opt-in required; no skips.');
const base = { operation: 'recordRead', endpoint: 'http://127.0.0.1:19944',
  genesisHash: '0xe8cf5d7d636a08721efaa17acc52ed3d895fe2468dccb2520cc8e249ddb6b208',
  contractAddress: '5ESNt37LSXvXuyWxfWai8aghbkCP8QgDaykAcgsN94eY9cx6',
  contractCodeHash: '0x7c102aaaaef5fec4e9f8f035c0542c7c6ca23f8c4e6b8de5e0498862debef0fa',
  origin: '0xd43593c715fdd31c61141abd04a99fd6822c8558854ccde39a5684e7a56da27d' };
function invoke(fields, expectedCode = 0) {
  const child = spawnSync(process.execPath, [resolve(import.meta.dirname, 'bridge.mjs')], {
    input: JSON.stringify({ ...base, ...fields }), encoding: 'utf8', timeout: 45000,
    windowsHide: true, maxBuffer: 1024 * 1024
  });
  assert.equal(child.error, undefined);
  assert.equal(child.status, expectedCode);
  const envelope = JSON.parse(child.stdout);
  assert.equal(envelope.ok, expectedCode === 0);
  return envelope;
}
test('shipping restricted RPC bridge verifies retained native contract', () => {
  const { result } = invoke({ action: 'verify' });
  assert.equal(result.verified, true);
  assert.equal(result.codeHash, base.contractCodeHash);
  assert.match(result.blockHash, /^0x[0-9a-f]{64}$/);
});
test('shipping bridge returns explicit native absence for cleaned fixture key', () => {
  const { result } = invoke({ action: 'get', keyBase64: Buffer.from('oasis:record-contract:fixture:v1').toString('base64') });
  assert.equal(result.observation, null);
});
test('shipping bridge enumerates actual retained empty contract snapshot', () => {
  const { result } = invoke({ action: 'list', keyBase64: '' });
  assert.deepEqual(result.keysBase64, []);
  assert.equal(result.generation, '5');
});
test('shipping bridge rejects wrong code identity instead of inventing absence', () => {
  const result = invoke({ action: 'get', keyBase64: 'AQ==', contractCodeHash: '0x' + '00'.repeat(32) }, 1);
  assert.match(result.error, /code differs/);
  assert.equal(Object.hasOwn(result, 'result'), false);
});
