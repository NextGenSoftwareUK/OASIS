import { Keyring } from '@polkadot/api';
import { cryptoWaitReady, randomAsU8a, mnemonicGenerate, mnemonicValidate, mnemonicToMiniSecret } from '@polkadot/util-crypto';

export function validateGenerationRequest(request) {
  if (['privateKey', 'mnemonic', 'publicKey', 'address'].some(field => request[field] != null))
    throw new Error('New key generation accepts no existing keys/address; it is not wallet recovery or replacement.');
}

function addressFormat(api) {
  // registry.chainSS58 is populated by the SDK's actual chain properties/runtime constants.
  // Never guess a mainnet/testnet prefix from a human-readable network name.
  const ss58Format = api.registry.chainSS58;
  if (!Number.isInteger(ss58Format) || ss58Format < 0 || ss58Format > 16383 || [46, 47].includes(ss58Format))
    throw new Error('An authoritative supported SS58 format is required before generating keys.');
  return ss58Format;
}

function materialFromSeed(api, seed, ss58Format) {
  let pair;
  try {
    pair = new Keyring({ type: 'sr25519', ss58Format }).addFromSeed(seed);
    const publicKey = '0x' + Buffer.from(pair.publicKey).toString('hex');
    const address = pair.address;
    if (api.createType('AccountId32', address).toHex() !== publicKey)
      throw new Error('Generated SS58 address does not identify its sr25519 AccountId32.');
    return { chainId: api.genesisHash.toHex(), cryptoType: 'sr25519', ss58Format,
      privateKey: '0x' + Buffer.from(seed).toString('hex'), publicKey, address };
  } finally {
    seed.fill(0);
    pair?.lock();
  }
}

export async function generateKeyPair(api) {
  const format = addressFormat(api);
  await cryptoWaitReady();
  return materialFromSeed(api, randomAsU8a(32), format);
}

export async function validateRestorationMaterial(material) {
  if (typeof material !== 'string' || !material.trim()) throw new Error('Explicit restoration material is required.');
  if (/^(0x)?[0-9a-fA-F]{64}$/.test(material)) return;
  await cryptoWaitReady();
  if (!mnemonicValidate(material))
    throw new Error('Only a valid BIP39 mnemonic or exact32byte hex seed is supported; no hash/pad/truncate recovery.');
}

export async function createAccount(api) {
  const format = addressFormat(api);
  await cryptoWaitReady();
  const seedPhrase = mnemonicGenerate(24);
  return { ...materialFromSeed(api, mnemonicToMiniSecret(seedPhrase), format), seedPhrase };
}

export async function restoreAccount(api, material) {
  const format = addressFormat(api);
  await validateRestorationMaterial(material);
  await cryptoWaitReady();
  const seed = /^(0x)?[0-9a-fA-F]{64}$/.test(material)
    ? Uint8Array.from(Buffer.from(material.replace(/^0x/, ''), 'hex')) : mnemonicToMiniSecret(material);
  return materialFromSeed(api, seed, format);
}
