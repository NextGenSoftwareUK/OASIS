import { test } from 'node:test';
import assert from 'node:assert/strict';
import { TypeRegistry } from '@polkadot/types/create';
import { validateNftMutation, nftMutationData, prepareNftMutation, verifyNftPreparation } from './psp34-contract-mutation.mjs';
import { encodePsp34Arguments } from './psp34-contract-codec.mjs';
const account = '0x' + 'ab'.repeat(32), contract = '0x' + 'cd'.repeat(32), hash = '0x' + 'ef'.repeat(32);
const request = () => ({ action: 'mint', tokenId: { U128: '18446744073709551617' }, to: account,
  attributes: [['bmFtZQ==', 'VHJlZQ==']], sender: account, recipient: contract,
  contractAddress: contract, contractCodeHash: hash, privateKey: '0x' + '02'.repeat(32) }); // Synthetic local key only.
test('JSON mutation data preserves exact u128/byte identity and base64 metadata', async () => {
  await validateNftMutation(request());
  const registry = new TypeRegistry();
  const value = request();value.tokenId = { Bytes: '0x0001ff' };
  assert.equal(nftMutationData(registry, value), '0x00000010' + 'ab'.repeat(32) + '050c0001ff04106e616d651054726565');
  value.action = 'burn';delete value.attributes;delete value.to;
  assert.equal(nftMutationData(registry, value), '0x00000011050c0001ff');
});
test('bad input is rejected before native metadata, state, signing or submission access', async () => {
  let accesses = 0;
  const api = new Proxy({}, { get() { accesses++;throw new Error('Unexpected native access'); } });
  for (const change of [value => value.action = 'approve', value => value.tokenId = '1',
    value => value.tokenId = { Bytes: '0xAA' }, value => value.tokenId = { U128: 1 },
    value => value.contractCodeHash = 'guessed-code', value => value.sender = 'inferred-account',
    value => value.recipient = account, value => value.memo = 'discard-me',
    value => value.privateKey = '', value => value.attributes = [['bmFtZQ==', 'not base64']],
    value => value.attributes = [['bmFtZQ==', ''], ['bmFtZQ==', 'VHJlZQ==']]]) {
    const value = request();change(value);
    await assert.rejects(prepareNftMutation(api, value));
  }
  assert.equal(accesses, 0);
});
test('non-mint calls cannot discard mint-only attributes or burn recipient', async () => {
  for (const action of ['transfer', 'burn']) {
    const value = request();value.action = action;
    await assert.rejects(validateNftMutation(value));
  }
});
test('unknown preparation protocol fails before native access', async () => {
  let accesses = 0;
  await assert.rejects(verifyNftPreparation(new Proxy({}, { get() { accesses++; } }), { protocol: 'other' }), /Versioned/);
  assert.equal(accesses, 0);
});
test('shared primitive encoder rejects unknown types and malformed type/argument arrays', () => {
  const registry = new TypeRegistry();
  for (const kinds of [null, ['invented'], ['toString']]) assert.throws(() => encodePsp34Arguments(registry, kinds, []));
  assert.throws(() => encodePsp34Arguments(registry, ['account'], []));
});
