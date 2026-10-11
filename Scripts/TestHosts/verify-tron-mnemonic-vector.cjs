// Public BIP39 test mnemonic only. Never use this key for funded accounts.
// Reuses cached ethers; no network calls or downloads.
const { createRequire } = require('node:module');
const path = require('node:path');
const assert = require('node:assert/strict');
const runtime = createRequire(path.resolve(__dirname, '../../Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS/package.json'));
const { HDNodeWallet } = runtime('ethers');
const wallet = HDNodeWallet.fromPhrase(
  'abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about',
  undefined, "m/44'/195'/0'/0/0");
assert.equal(wallet.privateKey, '0xb5a4cea271ff424d7c31dc12a3e43e401df7a40d7412a15750f3f0b6b5449a28');
assert.equal(wallet.signingKey.publicKey, '0x04ff21f8e64d3a3c0198edfbb7afdc79be959432e92e2f8a1984bb436a414b8edcec0345aad0c1bf7da04fd036dd7f9f617e30669224283d950fab9dd84831dc83');
console.log('Independent ethers TRON BIP44 public test vector: PASS');
