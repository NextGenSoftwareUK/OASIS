import { test } from 'node:test';
import assert from 'node:assert/strict';
import { TypeRegistry } from '@polkadot/types/create';
import { registerPsp34Types } from './psp34-contract-codec.mjs';
import { exactNftEffect, validateNftOutcome, readNftCustodyReceipt } from './psp34-contract-outcome.mjs';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
const registry = new TypeRegistry();registerPsp34Types(registry);
const api = { registry, createType: (...args) => registry.createType(...args) };
const contract = '0x' + '11'.repeat(32), sender = '0x' + '22'.repeat(32), recipient = '0x' + '33'.repeat(32);
const tokenId = { U128: '18446744073709551617' };
const identity = { action: 'mint', sender, contractAddress: contract, tokenId, to: recipient,
  attributes: [['bmFtZQ==', 'VHJlZQ==']] };
function event(type, value, address = contract, suffix = '') {
  return { section: 'contracts', method: 'ContractEmitted', data: [registry.createType('AccountId32', address),
    registry.createType('Bytes', registry.createType(type, value).toHex() + suffix)] };
}
const attribute = (id = tokenId, key = '0x6e616d65', value = '0x54726565') =>
  event('(OasisPsp34Id,Bytes,Bytes)', [id, key, value]);
const transfer = (from = null, to = recipient, id = tokenId, address = contract, suffix = '') =>
  event('(Option<AccountId32>,Option<AccountId32>,OasisPsp34Id)', [from, to, id], address, suffix);

test('custody source proof binds finalized dispatch, exact token/pool/code and original extrinsic', async () => {
  const blockHash = '0x' + 'aa'.repeat(32), transactionHash = '0x' + 'bb'.repeat(32), code = '0x' + 'cc'.repeat(32);
  const request = { transactionHash, blockHash, contractAddress: contract, contractCodeHash: code, tokenId, custody: recipient };
  async function prove(mode) {
    const own = [{ section: 'system', method: mode === 'failed' ? 'ExtrinsicFailed' : 'ExtrinsicSuccess', data: ['failure'] },
      transfer(sender, mode === 'pool' ? sender : recipient, mode === 'token' ? { U8: '1' } : tokenId)];
    const hex = value => ({ toHex: () => value });
    const reader = { ...api, rpc: { chain: {
      getFinalizedHead: async () => hex(blockHash),
      getHeader: async () => ({ number: registry.createType('u32', 1) }),
      getBlockHash: async () => hex(blockHash),
      getBlock: async () => ({ block: { extrinsics: [{ hash: hex(transactionHash) }] } })
    } }, at: async () => ({ query: {
      system: { events: async () => own.map(value => ({ event: value, phase: {
        isApplyExtrinsic: true, asApplyExtrinsic: { toNumber: () => mode === 'foreign' ? 1 : 0 }
      } })) },
      contracts: { contractInfoOf: async () => ({ isNone: false, unwrap: () => ({ codeHash: hex(mode === 'code' ? blockHash : code) }) }) }
    } }) };
    return readNftCustodyReceipt(reader, request);
  }
  const proof = await prove('accepted');
  assert.equal(proof.transactionHash, transactionHash);
  assert.equal(proof.custodyReceiptVerified, true);
  assert.equal(proof.from, sender);assert.equal(proof.to, recipient);
  for (const mode of ['failed', 'pool', 'token', 'code', 'foreign']) await assert.rejects(prove(mode));
});

test('mint proves every ordered metadata acknowledgement and the exact typed token/recipient', () => {
  assert.deepEqual(exactNftEffect(api, identity, [attribute(), transfer()], true),
    { applied: true, nftEffectMatchesRequest: true, from: null, to: recipient });
});
test('transfer and burn report actual source owner, which can differ from approved signer', () => {
  assert.deepEqual(exactNftEffect(api, { ...identity, action: 'transfer' }, [transfer(sender)], true),
    { applied: true, nftEffectMatchesRequest: true, from: sender, to: recipient });
  assert.deepEqual(exactNftEffect(api, { ...identity, sender: recipient, action: 'burn' }, [transfer(sender, null)], true),
    { applied: true, nftEffectMatchesRequest: true, from: sender, to: null });
});
test('wrong metadata, token variant, ordering, count, emitter or recipient cannot acknowledge mint', () => {
  for (const events of [[transfer()], [attribute(), transfer(), transfer()], [transfer(), attribute()],
    [attribute(tokenId, '0x6e616d65', '0x00'), transfer()], [attribute({ U64: '1' }), transfer()],
    [attribute(), transfer(null, recipient, { U128: '18446744073709551618' })],
    [attribute(), transfer(null, sender)], [attribute(), transfer(sender)],
    [attribute(), transfer(null, recipient, tokenId, sender)],
    [attribute(), transfer(null, recipient, tokenId, contract, '00')]])
    assert.throws(() => exactNftEffect(api, identity, events, true));
});
test('non-mint actions require a real source and burn requires no destination', () => {
  assert.throws(() => exactNftEffect(api, { ...identity, action: 'transfer' }, [transfer()], true));
  assert.throws(() => exactNftEffect(api, { ...identity, action: 'burn' }, [transfer(sender)], true));
  assert.throws(() => exactNftEffect(api, { ...identity, action: 'transfer' }, [transfer(sender, null)], true));
  assert.throws(() => exactNftEffect(api, { ...identity, action: 'transfer', expectedFrom: recipient }, [transfer(sender)], true));
});
test('empty metadata bytes remain exact and byte token identities retain their variant', () => {
  const id = { Bytes: '0x0001ff' };
  assert.equal(exactNftEffect(api, { ...identity, tokenId: id, attributes: [['bmFtZQ==', '']] },
    [attribute(id, '0x6e616d65', '0x'), transfer(null, recipient, id)], true).nftEffectMatchesRequest, true);
});
test('failed dispatch is known non-success, not an effect acknowledgement', () => {
  assert.deepEqual(exactNftEffect(api, identity, [], false),
    { applied: false, nftEffectMatchesRequest: false, from: null, to: null });
});
test('recovery scan remains bounded and an unknown action cannot become success', () => {
  validateNftOutcome({ maximumBlocks: 32 });
  for (const maximumBlocks of [0, 129, '32']) assert.throws(() => validateNftOutcome({ maximumBlocks }));
  assert.throws(() => validateNftOutcome({ maximumBlocks: 32, fromBlockNumber: '01' }));
  assert.throws(() => exactNftEffect(api, { ...identity, action: 'invented' }, [], true));
});
test('shipping NFT submission rejects absent durable owner before RPC initialization', () => {
  const env = { ...process.env };
  delete env.OASIS_NATIVE_PREPARATION_PIPE;delete env.OASIS_NATIVE_PREPARATION_MODULE;
  const child = spawnSync(process.execPath, [fileURLToPath(new URL('./bridge.mjs', import.meta.url))], {
    env, input: JSON.stringify({ operation: 'submitNftPreparation', endpoint: 'http://127.0.0.1:1' }),
    encoding: 'utf8', timeout: 10000, windowsHide: true
  });
  assert.equal(child.status, 1);const reply = JSON.parse(child.stdout);
  assert.equal(reply.ok, false);assert.match(reply.error, /request validation.*Owned persistence transport/);
});
test('shipping NFT preparation rejects invalid intent before native access', () => {
  const child = spawnSync(process.execPath, [fileURLToPath(new URL('./bridge.mjs', import.meta.url))], {
    input: JSON.stringify({ operation: 'prepareNftMutation', action: 'invented' }), encoding: 'utf8', timeout: 10000, windowsHide: true
  });
  assert.equal(child.status, 1);const reply = JSON.parse(child.stdout);
  assert.equal(reply.ok, false);assert.match(reply.error, /request validation.*Explicit OASIS PSP34/);
});
