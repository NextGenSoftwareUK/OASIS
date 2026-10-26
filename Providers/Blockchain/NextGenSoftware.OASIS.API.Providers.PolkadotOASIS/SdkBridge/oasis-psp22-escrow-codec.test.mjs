import assert from 'node:assert/strict';
import test from 'node:test';
import { TypeRegistry } from '@polkadot/types/create';
import { readTokenEscrowDeposit } from './psp22-escrow-read.mjs';
import { validateTokenEscrowMutation, prepareTokenEscrowMutation, verifyTokenEscrowPreparation,
  recoverTokenEscrowPreparation } from './psp22-escrow-mutation.mjs';
import { tokenEscrowEventSignature, exactTokenEscrowEffect } from './psp22-escrow-outcome.mjs';
import { encodeTokenEscrowMutation, decodeTokenEscrowMutation, encodeTokenEscrowDepositQuery,
  registerTokenEscrowTypes, decodeTokenEscrowResult } from './oasis-psp22-escrow-codec.mjs';
const registry = new TypeRegistry();
const id = '0x' + '11'.repeat(32), token = '0x' + '22'.repeat(32);
test('lock retains exact deposit, token and maximum u128 above JS numeric precision', () => {
  const request = { action: 'lock', depositId: id, tokenAddress: token, amountUnits: ((1n << 128n) - 1n).toString() };
  const encoded = encodeTokenEscrowMutation(registry, request);
  assert.equal(encoded.slice(0, 10), '0x00000020');
  assert.equal((encoded.length - 2) / 2, 84);
  assert.deepEqual(decodeTokenEscrowMutation(registry, encoded), request);
});
test('unlock binds only the original ID; destination/token/amount cannot be substituted', () => {
  const request = { action: 'unlock', depositId: id };
  assert.deepEqual(decodeTokenEscrowMutation(registry, encodeTokenEscrowMutation(registry, request)), request);
  for (const extra of [{ to: token }, { tokenAddress: token }, { amountUnits: '1' }])
    assert.throws(() => encodeTokenEscrowMutation(registry, { ...request, ...extra }));
  assert.equal(encodeTokenEscrowDepositQuery(id), '0x00000022' + id.slice(2));
});
test('invalid IDs, fractional/rounded/noncanonical/overflow quantities reject before RPC', () => {
  for (const value of ['0', '-1', '01', '1.1', '1e3', 100, (1n << 128n).toString()])
    assert.throws(() => encodeTokenEscrowMutation(registry, { action: 'lock', depositId: id, tokenAddress: token, amountUnits: value }));
  for (const depositId of ['0x' + '00'.repeat(32), 'guid', id.toUpperCase()])
    assert.throws(() => encodeTokenEscrowDepositQuery(depositId));
});
test('signed-call recovery rejects unknown selectors and appended bytes', () => {
  const encoded = encodeTokenEscrowMutation(registry, { action: 'unlock', depositId: id });
  assert.throws(() => decodeTokenEscrowMutation(registry, encoded + '00'));
  assert.throws(() => decodeTokenEscrowMutation(registry, encoded.replace('00000021', '00000012')));
});
test('language/application failures and trailing result data cannot become successful custody', () => {
  registerTokenEscrowTypes(registry);
  const type = 'Result<Result<(),OasisTokenEscrowError>,OasisTokenEscrowLangError>';
  const success = registry.createType(type, { Ok: { Ok: null } }).toU8a();
  assert.equal(decodeTokenEscrowResult(registry, 'mutation', success), true);
  for (const name of ['InvalidDeposit', 'AlreadyExists', 'Missing', 'NotOwner', 'Released', 'TokenCallFailed'])
    assert.throws(() => decodeTokenEscrowResult(registry, 'mutation', registry.createType(type, { Ok: { Err: name } }).toU8a()), /application failure/);
  assert.throws(() => decodeTokenEscrowResult(registry, 'mutation', registry.createType(type, { Err: 'CouldNotReadInput' }).toU8a()), /language failure/);
  assert.throws(() => decodeTokenEscrowResult(registry, 'mutation', Uint8Array.from([...success, 0])), /trailing/);
});
test('deposit query preserves original owner, exact amount and released tombstone', () => {
  registerTokenEscrowTypes(registry);
  const type = 'Result<Option<OasisTokenEscrowDeposit>,OasisTokenEscrowLangError>';
  assert.equal(decodeTokenEscrowResult(registry, 'deposit', registry.createType(type, { Ok: null }).toU8a()), null);
  const value = { owner: id, token, amount: '18446744073709551620', released: true };
  assert.deepEqual(decodeTokenEscrowResult(registry, 'deposit', registry.createType(type, { Ok: value }).toU8a()),
    { owner: id, tokenAddress: token, amountUnits: value.amount, released: true });
});
test('invalid deposit query rejects before finalized-head or contract access', async () => {
  await assert.rejects(readTokenEscrowDeposit({}, { depositId: 'guid' }), /deposit ID/);
});
test('deposit read uses one finalized code-pinned snapshot and preserves large amounts', async () => {
  registerTokenEscrowTypes(registry);
  const hash = '0x' + '33'.repeat(32), code = '0x' + '44'.repeat(32);
  const finalized = { toHex: () => hash };
  const counts = { heads: 0, at: 0, query: 0 };
  const api = { registry, createType: (...args) => registry.createType(...args),
    rpc: { chain: { getFinalizedHead: async () => { counts.heads++; return finalized; } } },
    at: async block => { counts.at++; assert.equal(block, finalized); return {
      query: { contracts: { contractInfoOf: async address => { assert.equal(address, token); return {
        isNone: false, unwrap: () => ({ codeHash: { toHex: () => code } }) }; } } },
      call: { contractsApi: { call: async (origin, address, value, gas, deposit, input) => {
        counts.query++; assert.equal(origin, id); assert.equal(address, token);
        assert.equal(input, encodeTokenEscrowDepositQuery(id));
        assert.equal(value, 0); assert.equal(gas, null); assert.equal(deposit, null);
        const encoded = registry.createType('Result<Option<OasisTokenEscrowDeposit>,OasisTokenEscrowLangError>',
          { Ok: { owner: id, token, amount: '18446744073709551620', released: false } });
        return { result: { isErr: false, asOk: { flags: { get: () => registry.createType('u32', 0) },
          data: registry.createType('Bytes', encoded.toHex()) } } };
      } } } }; } };
  const result = await readTokenEscrowDeposit(api, { contractAddress: token, contractCodeHash: code, origin: id, depositId: id });
  assert.equal(result.blockHash, hash); assert.equal(result.deposit.amountUnits, '18446744073709551620');
  assert.deepEqual(counts, { heads: 1, at: 1, query: 1 });
});
test('missing or substituted contract cannot become a successful empty token deposit', async () => {
  for (const missing of [false, true]) {
    const api = { createType: (...args) => registry.createType(...args),
      rpc: { chain: { getFinalizedHead: async () => ({ toHex: () => id }) } },
      at: async () => ({ query: { contracts: { contractInfoOf: async () => ({ isNone: missing,
        unwrap: () => ({ codeHash: { toHex: () => id } }) }) } },
        call: { contractsApi: { call: async () => { throw new Error('Unexpected unpinned query'); } } } }) };
    await assert.rejects(readTokenEscrowDeposit(api, { contractAddress: token, contractCodeHash: token, origin: id, depositId: id }),
      missing ? /does not exist at finalized snapshot/ : /differs from the explicitly pinned artifact/);
  }
});
const escrowRequest = () => ({ action: 'lock', depositId: id, tokenAddress: token, amountUnits: '18446744073709551620',
  contractAddress: '0x' + '33'.repeat(32), contractCodeHash: '0x' + '44'.repeat(32),
  sender: id, recipient: '0x' + '33'.repeat(32), privateKey: '0x' + '02'.repeat(32) });
test('public preparation shape retains explicit token and rejects redirected unlock amounts', async () => {
  await validateTokenEscrowMutation(escrowRequest());
  const unlock = { ...escrowRequest(), action: 'unlock' }; delete unlock.amountUnits;
  await validateTokenEscrowMutation(unlock);
  await assert.rejects(validateTokenEscrowMutation({ ...unlock, amountUnits: '1' }), /complete original deposit/);
  await assert.rejects(validateTokenEscrowMutation({ ...unlock, to: token }), /complete original deposit/);
});
test('malformed escrow preparation fails before RPC, signing or submission access', async () => {
  let accesses = 0;
  const api = new Proxy({}, { get() { accesses++; throw new Error('Unexpected native access'); } });
  for (const extra of [{ action: 'freeze' }, { depositId: 'guid' }, { amountUnits: 1 }, { amountUnits: '1.1' },
    { tokenAddress: '1' }, { recipient: token }, { contractAddress: token }, { memo: 'silently discard' },
    { privateKey: '' }, { contractCodeHash: 'unknown' }, { sender: id.toUpperCase() }])
    await assert.rejects(prepareTokenEscrowMutation(api, { ...escrowRequest(), ...extra }));
  assert.equal(accesses, 0);
});
test('duplicate deposits and foreign/released/missing unlocks fail before signing boundary', async () => {
  for (const [action, observed] of [['lock', { owner: id, token, amount: '42', released: true }],
    ['unlock', null], ['unlock', { owner: token, token, amount: '42', released: false }],
    ['unlock', { owner: id, token: id, amount: '42', released: false }],
    ['unlock', { owner: id, token, amount: '42', released: true }]]) {
    registerTokenEscrowTypes(registry); let calls = 0, signingAccess = 0;
    const api = { registry, createType: (...args) => registry.createType(...args),
      rpc: { chain: { getFinalizedHead: async () => ({ toHex: () => id }) } },
      get tx() { signingAccess++; throw new Error('Unexpected signing access'); },
      at: async () => ({ query: { contracts: { contractInfoOf: async () => ({ isNone: false,
        unwrap: () => ({ codeHash: { toHex: () => escrowRequest().contractCodeHash } }) }) } },
        call: { contractsApi: { call: async () => { calls++;
          const encoded = registry.createType('Result<Option<OasisTokenEscrowDeposit>,OasisTokenEscrowLangError>', { Ok: observed });
          return { result: { isErr: false, asOk: { flags: { get: () => registry.createType('u32', 0) },
            data: registry.createType('Bytes', encoded.toHex()) } } };
        } } } }) };
    const request = { ...escrowRequest(), action }; if (action === 'unlock') delete request.amountUnits;
    await assert.rejects(prepareTokenEscrowMutation(api, request), /does not authorize/);
    assert.equal(calls, 1); assert.equal(signingAccess, 0);
  }
});
test('preparation/recovery protocol or code identity cannot be guessed before native access', async () => {
  let accesses = 0; const api = new Proxy({}, { get() { accesses++; throw new Error('Unexpected native access'); } });
  await assert.rejects(verifyTokenEscrowPreparation(api, { protocol: 'other' }), /Versioned/);
  await assert.rejects(recoverTokenEscrowPreparation(api, { contractCodeHash: 'guessed' }), /reviewed escrow code/);
  assert.equal(accesses, 0);
});
function escrowEvent(identity, change = {}) {
  const value = { ...identity, ...change };
  const data = registry.createType('(H256,AccountId32,AccountId32,u128)',
    [value.depositId, value.sender, value.tokenAddress, value.amountUnits]);
  return { event: { section: 'contracts', method: 'ContractEmitted', data: [registry.createType('AccountId32', value.contractAddress),
    registry.createType('Bytes', data.toHex())] },
    topics: [tokenEscrowEventSignature(value.action), value.depositId, value.sender].map(topic => registry.createType('H256', topic)) };
}
const effectApi = { registry, createType: (...args) => registry.createType(...args) };
test('native effect requires exact action signature, deposit, owner, token and u128 amount', () => {
  for (const action of ['lock', 'unlock']) {
    const identity = { ...escrowRequest(), action };
    assert.deepEqual(exactTokenEscrowEffect(effectApi, identity, [escrowEvent(identity)], true),
      { applied: true, tokenEscrowEffectMatchesRequest: true, released: action === 'unlock' });
  }
});
test('identical event payload cannot confuse lock and unlock acknowledgement', () => {
  const identity = escrowRequest();
  const record = escrowEvent(identity, { action: 'unlock' });
  assert.throws(() => exactTokenEscrowEffect(effectApi, identity, [record], true), /signature/);
});
test('wrong emitter, duplicate events, substituted amounts or indexed identities cannot acknowledge custody', () => {
  const identity = escrowRequest();
  for (const change of [{ depositId: token }, { sender: token }, { tokenAddress: id },
    { amountUnits: '18446744073709551621' }, { contractAddress: token }])
    assert.throws(() => exactTokenEscrowEffect(effectApi, identity, [escrowEvent(identity, change)], true));
  assert.throws(() => exactTokenEscrowEffect(effectApi, identity, [escrowEvent(identity), escrowEvent(identity)], true), /Exactly one/);
  const noTopics = escrowEvent(identity); noTopics.topics = [];
  assert.throws(() => exactTokenEscrowEffect(effectApi, identity, [noTopics], true), /signature/);
});
test('dispatch failure remains known non-success and unknown action never becomes an effect', () => {
  assert.deepEqual(exactTokenEscrowEffect(effectApi, escrowRequest(), [], false),
    { applied: false, tokenEscrowEffectMatchesRequest: false });
  assert.throws(() => exactTokenEscrowEffect(effectApi, { action: 'freeze' }, [], true), /Explicit/);
});
