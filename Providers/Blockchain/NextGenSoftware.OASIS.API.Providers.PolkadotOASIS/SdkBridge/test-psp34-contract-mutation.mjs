import { test } from 'node:test';
import assert from 'node:assert/strict';
import { TypeRegistry } from '@polkadot/types/create';
import { validateNftMutation, nftMutationData, prepareNftMutation, verifyNftPreparation, encodeNftOwnerWitness, unwrapNftCall } from './psp34-contract-mutation.mjs';
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

test('native escrow destinations and owner witnesses cannot be redirected before signing', async () => {
  for (const action of ['lock', 'unlock']) {
    const value = request();value.action = action;delete value.attributes;
    value.to = action === 'lock' ? contract : account;
    value.expectedFrom = action === 'lock' ? account : contract;
    await validateNftMutation(value);
    assert.equal(nftMutationData(new TypeRegistry(), value).slice(0, 10), action === 'lock' ? '0x00000012' : '0x00000013');
    await assert.rejects(validateNftMutation({ ...value, to: '0x' + '11'.repeat(32) }), /deterministic/);
    await assert.rejects(validateNftMutation({ ...value, expectedFrom: undefined }), /witness/);
  }
});

test('unlock requires the observed active beneficiary before contract signing access', async () => {
  for (const observedBeneficiary of [null, contract]) {
    const registry = new TypeRegistry();let queries = 0;
    const value = request();value.action = 'unlock';delete value.attributes;
    value.to = account;value.expectedFrom = contract;
    const at = { query: { contracts: { contractInfoOf: async () => ({ isNone: false,
      unwrap: () => ({ codeHash: { toHex: () => hash } }) }) } },
      call: { contractsApi: { call: async () => {
        const owner = registry.createType('Result<Option<AccountId32>,OasisPsp34LangError>', { Ok: queries++ === 0 ? contract : observedBeneficiary });
        return { result: { isErr: false, asOk: { flags: { get: () => registry.createType('u32', 0) },
          data: registry.createType('Bytes', owner.toHex()) } } };
      } } } };
    const api = { registry, createType: (...args) => registry.createType(...args),
      rpc: { chain: { getFinalizedHead: async () => ({ toHex: () => hash }) } }, at: async () => at };
    await assert.rejects(prepareNftMutation(api, value), /beneficiary/);
    assert.equal(queries, 2);
  }
});
test('bad input is rejected before native metadata, state, signing or submission access', async () => {
  let accesses = 0;
  const api = new Proxy({}, { get() { accesses++;throw new Error('Unexpected native access'); } });
  for (const change of [value => value.action = 'approve', value => value.tokenId = '1',
    value => value.tokenId = { Bytes: '0xAA' }, value => value.tokenId = { U128: 1 },
    value => value.contractCodeHash = 'guessed-code', value => value.sender = 'inferred-account',
    value => value.recipient = account, value => value.memo = 'discard-me',
    value => value.privateKey = '', value => value.attributes = [['bmFtZQ==', 'not base64']],
    value => value.expectedFrom = account,
    value => value.attributes = [['bmFtZQ==', ''], ['bmFtZQ==', 'VHJlZQ==']]]) {
    const value = request();change(value);
    await assert.rejects(prepareNftMutation(api, value));
  }
  assert.equal(accesses, 0);
});
test('requested source-owner mismatch is rejected before signing the real contract call', async () => {
  const registry = new TypeRegistry();let ownerQueries = 0;
  const value = request();value.action = 'burn';delete value.attributes;delete value.to;value.expectedFrom = account;
  const at = { query: { contracts: { contractInfoOf: async () => ({ isNone: false,
    unwrap: () => ({ codeHash: { toHex: () => hash } }) }) } },
    call: { contractsApi: { call: async () => {
      ownerQueries++;
      const owner = registry.createType('Result<Option<AccountId32>,OasisPsp34LangError>', { Ok: contract });
      return { result: { isErr: false, asOk: { flags: { get: () => registry.createType('u32', 0) },
        data: registry.createType('Bytes', owner.toHex()) } } };
    } } } };
  const api = { registry, createType: (...args) => registry.createType(...args),
    rpc: { chain: { getFinalizedHead: async () => ({ toHex: () => hash }) } }, at: async () => at };
  await assert.rejects(prepareNftMutation(api, value), /Observed NFT owner differs/);
  assert.equal(ownerQueries, 1);
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
test('signed source-owner witness is strictly domain-separated and atomically wraps one NFT call', () => {
  const inner = { section: 'contracts', method: 'call' };
  const bytes = Buffer.from(encodeNftOwnerWitness(account).slice(2), 'hex');
  const remark = raw => ({ section: 'system', method: 'remarkWithEvent', args: [{ toU8a: () => raw }] });
  const wrap = calls => ({ method: { section: 'utility', method: 'batchAll', args: [calls] } });
  assert.deepEqual(unwrapNftCall(wrap([inner, remark(bytes)])), { call: inner, expectedFrom: account });
  assert.deepEqual(unwrapNftCall({ method: inner }), { call: inner });
  for (const calls of [[inner], [inner, remark(bytes), inner], [inner, remark(bytes.subarray(1))],
    [inner, remark(Buffer.alloc(bytes.length))], [{ section: 'utility', method: 'batchAll' }, remark(bytes)],
    [inner, { ...remark(bytes), method: 'remark' }]]) assert.throws(() => unwrapNftCall(wrap(calls)));
  assert.throws(() => encodeNftOwnerWitness('inferred-owner'));
});
