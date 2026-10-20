// Offline Starknet account material: BIP-39 mnemonics, Stark-curve keys and account addresses.
// Pure functions only; no network access. Uses the official starknet.js stack (@scure/starknet, @scure/bip32/39).
import { generateMnemonic, mnemonicToSeedSync, validateMnemonic } from '@scure/bip39';
import { wordlist } from '@scure/bip39/wordlists/english';
import { HDKey } from '@scure/bip32';
import { getStarkKey, grindKey } from '@scure/starknet';
import { hash, num } from 'starknet';

// Starknet's registered SLIP-44 coin type is 9004: m/44'/9004'/0'/0/{index}, then ground into the Stark field.
export const STARKNET_DERIVATION_PREFIX = "m/44'/9004'/0'/0/";
const FELT = /^0x[0-9a-f]{1,64}$/i;
const STARK_ORDER = 0x0800000000000010ffffffffffffffffb781126dcae7b2321e66a241adc64d2fn;

export function normalizeFelt(value, name) {
  if (typeof value !== 'string' || !FELT.test(value.trim())) throw new Error(`${name} must be a 0x-prefixed hex felt.`);
  const n = BigInt(value.trim());
  if (n === 0n) throw new Error(`${name} must be non-zero.`);
  return num.toHex(n);
}

export function validatePrivateKey(value) {
  const key = normalizeFelt(value, 'privateKey');
  if (BigInt(key) >= STARK_ORDER) throw new Error('privateKey must be below the Stark curve order.');
  return key;
}

export function validateIndex(value) {
  if (!Number.isInteger(value) || value < 0 || value >= 0x80000000) throw new Error('index must be a non-hardened BIP-32 index.');
  return value;
}

export function newMnemonic(words = 12) {
  if (words !== 12 && words !== 24) throw new Error('words must be 12 or 24.');
  return generateMnemonic(wordlist, words === 12 ? 128 : 256);
}

export function keysFromMnemonic(mnemonic, index = 0) {
  if (typeof mnemonic !== 'string' || !validateMnemonic(mnemonic.trim().toLowerCase().replace(/\s+/g, ' '), wordlist))
    throw new Error('A valid English BIP-39 mnemonic is required.');
  validateIndex(index);
  const seed = mnemonicToSeedSync(mnemonic.trim().toLowerCase().replace(/\s+/g, ' '));
  const node = HDKey.fromMasterSeed(seed).derive(STARKNET_DERIVATION_PREFIX + index);
  if (!node.privateKey) throw new Error('BIP-32 derivation produced no private key.');
  const privateKey = num.toHex(BigInt('0x' + grindKey(node.privateKey)));
  return { privateKey, publicKey: getStarkKey(privateKey) };
}

export function keysFromPrivateKey(privateKey) {
  const key = validatePrivateKey(privateKey);
  return { privateKey: key, publicKey: getStarkKey(key) };
}

/** OpenZeppelin-style account: constructor(public_key), salt = public key, deployed by the zero (counterfactual) deployer. */
export function accountAddress(publicKey, classHash) {
  const pk = normalizeFelt(publicKey, 'publicKey');
  const ch = normalizeFelt(classHash, 'classHash');
  return num.toHex(hash.calculateContractAddressFromHash(pk, ch, [pk], 0));
}

/** Address of any counterfactual deployment, for verifying against transactions observed on chain. */
export function deploymentAddress(salt, classHash, constructorCalldata) {
  if (!Array.isArray(constructorCalldata)) throw new Error('constructorCalldata must be an array.');
  return num.toHex(hash.calculateContractAddressFromHash(normalizeFelt(salt, 'salt'), normalizeFelt(classHash, 'classHash'),
    constructorCalldata.map((v, i) => num.toHex(BigInt(v))), 0));
}

export function validateEndpoint(endpoint) {
  const url = new URL(endpoint);
  if (!['http:', 'https:'].includes(url.protocol) || url.username || url.password)
    throw new Error('An HTTP RPC endpoint without embedded credentials is required.');
  return url.href;
}

export function validateChainId(chainId) {
  if (typeof chainId !== 'string' || !/^0x[0-9a-f]+$/i.test(chainId)) throw new Error('A pinned hex chainId is required.');
  return chainId.toLowerCase();
}
