import assert from 'node:assert/strict';
import test from 'node:test';
import { TypeRegistry } from '@polkadot/types/create';
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
