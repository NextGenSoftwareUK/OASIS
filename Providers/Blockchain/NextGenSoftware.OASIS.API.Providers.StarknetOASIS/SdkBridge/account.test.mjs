import test from 'node:test';
import assert from 'node:assert/strict';
import { accountAddress, deploymentAddress, keysFromMnemonic, keysFromPrivateKey, newMnemonic, validatePrivateKey } from './account.mjs';
import { assertAccountClass, run } from './bridge.mjs';

const OZ_CLASS = '0x540d7f5ec7ecf317e68d48564934cb99259781b1ee3cedbbc37ec5337f8e688';
const SEPOLIA = '0x534e5f5345504f4c4941';

test('private key 1 maps to the Stark curve generator x coordinate', () => {
  assert.equal(keysFromPrivateKey('0x1').publicKey, '0x1ef15c18599971b7beced415a40f0c7deacfd9b0d1819e03d723d8bc943cfca');
});

test('addresses match deployments observed on Starknet mainnet', () => {
  // DEPLOY_ACCOUNT 0x17affadf...a13, block 16623578 (5-argument proxy account).
  assert.equal(deploymentAddress('0x4286191d2ac7eb9432fb032aef81d399bff4513075fe37bea6e961800fb0cf8',
    '0x3530cc4759d78042f1b543bf797f5f3d647cde0388c33734cf91b7f7b9314a9',
    ['0x309c042d3729173c7f2f91a34f04d8c509c1b292d334679ef1aabf8da0899cc', '0x79dc0da7c54b95f10aa182ad0a46400db63156920adb65eca2654c0945a463', '0x2',
     '0x4286191d2ac7eb9432fb032aef81d399bff4513075fe37bea6e961800fb0cf8', '0x0']),
    '0x1a5b2f0ce5be0a4dfb15a89469aabd0e00d55d91bcdf727e1888faaddda4b8f');
  // DEPLOY_ACCOUNT 0x57d04f83...e29 (single public-key constructor, salt = public key).
  assert.equal(accountAddress('0x15a0d23fe349d3348a9fac74209074c8f342fb7d5f93f066ec134377d50e1f7', '0x3d16c7a9a60b0593bd202f660a28c5d76e0403601d9ccc7e4fa253b6a70c201'),
    '0x66df29983da0fcda7fed58acfcaaaabc400ebafd2c5867ff8db00b701fa5fe1');
});

test('mnemonics are BIP-39 and derivation is deterministic per index', () => {
  const mnemonic = newMnemonic();
  assert.equal(mnemonic.split(' ').length, 12);
  assert.equal(newMnemonic(24).split(' ').length, 24);
  assert.notEqual(newMnemonic(), mnemonic);
  const a = keysFromMnemonic(mnemonic, 0);
  assert.deepEqual(keysFromMnemonic('  ' + mnemonic.toUpperCase().replace(/ /g, '   ') + ' ', 0), a, 'whitespace and case normalised');
  assert.notEqual(keysFromMnemonic(mnemonic, 1).privateKey, a.privateKey);
  assert.equal(keysFromPrivateKey(a.privateKey).publicKey, a.publicKey);
  assert.throws(() => keysFromMnemonic('abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon', 0), /valid English BIP-39/);
  assert.throws(() => keysFromMnemonic(mnemonic, -1), /index/);
  assert.throws(() => newMnemonic(16), /12 or 24/);
});

test('private keys must be non-zero hex felts below the curve order', () => {
  for (const bad of ['0x0', '1', '0xzz', '0x0800000000000010ffffffffffffffffb781126dcae7b2321e66a241adc64d2f', '', null])
    assert.throws(() => validatePrivateKey(bad));
});

test('only OpenZeppelin-shaped account classes are accepted', () => {
  const account = [{ type: 'constructor', name: 'constructor', inputs: [{ name: 'public_key', type: 'core::felt252' }] },
    { type: 'function', name: '__validate_deploy__' }, { type: 'function', name: '__execute__' }];
  assert.doesNotThrow(() => assertAccountClass({ abi: JSON.stringify(account) }));
  assert.throws(() => assertAccountClass({ abi: [account[0], account[2]] }), /deployable account/);
  assert.throws(() => assertAccountClass({ abi: [{ type: 'constructor', inputs: [{ type: 'core::felt252' }, { type: 'core::felt252' }] }, account[1], account[2]] }), /exactly one/);
  assert.throws(() => assertAccountClass({}), /ABI/);
});

function fakeProvider({ chainId = SEPOLIA, deployed = new Set(), receipt = { execution_status: 'SUCCEEDED', finality_status: 'ACCEPTED_ON_L2' } } = {}) {
  const calls = [];
  return {
    calls,
    getChainId: async () => chainId,
    getClassHashAt: async address => { calls.push(['classAt', address]); if (deployed.has(address)) return '0xabc'; throw new Error('Contract not found'); },
    getNonceForAddress: async () => '0x3',
    getClassByHash: async () => ({ abi: [{ type: 'constructor', inputs: [{ type: 'core::felt252' }] }, { name: '__validate_deploy__' }, { name: '__execute__' }] }),
    waitForTransaction: async () => receipt,
  };
}

test('offline operations produce addresses for the configured class without a network', async () => {
  const created = await run({ operation: 'createAccount', classHash: OZ_CLASS });
  assert.equal(created.address, accountAddress(created.publicKey, OZ_CLASS));
  const restored = await run({ operation: 'restoreAccount', mnemonic: created.mnemonic, classHash: OZ_CLASS });
  assert.equal(restored.privateKey, created.privateKey);
  assert.equal(restored.address, created.address);
});

test('network operations refuse an RPC on the wrong chain before anything else', async () => {
  const provider = fakeProvider({ chainId: '0x534e5f4d41494e' });
  await assert.rejects(run({ operation: 'accountStatus', endpoint: 'https://rpc.example', chainId: SEPOLIA, address: '0x1' }, () => provider), /differs from the configured chain/);
  assert.equal(provider.calls.length, 0);
});

test('endpoints with embedded credentials are refused', async () => {
  await assert.rejects(run({ operation: 'accountStatus', endpoint: 'https://user:pass@rpc.example', chainId: SEPOLIA, address: '0x1' }, fakeProvider), /without embedded credentials/);
});

test('account status distinguishes deployed from undeployed', async () => {
  const provider = fakeProvider({ deployed: new Set(['0x1']) });
  assert.deepEqual(await run({ operation: 'accountStatus', endpoint: 'https://rpc.example', chainId: SEPOLIA, address: '0x1' }, () => provider),
    { address: '0x1', deployed: true, classHash: '0xabc', nonce: '0x3' });
  assert.deepEqual(await run({ operation: 'accountStatus', endpoint: 'https://rpc.example', chainId: SEPOLIA, address: '0x2' }, () => provider),
    { address: '0x2', deployed: false });
});

test('deploy refuses an address that is already deployed', async () => {
  const keys = keysFromMnemonic(newMnemonic(), 0);
  const provider = fakeProvider({ deployed: new Set([accountAddress(keys.publicKey, OZ_CLASS)]) });
  await assert.rejects(run({ operation: 'deployAccount', endpoint: 'https://rpc.example', chainId: SEPOLIA, classHash: OZ_CLASS, privateKey: keys.privateKey }, () => provider), /already deployed/);
});
