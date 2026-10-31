import { cryptoWaitReady, decodeAddress } from '@polkadot/util-crypto';

// AccountId32 has one identity across SS58 network presentations. The SDK
// verifies SS58 checksums; a guessed prefix or home-grown decoder must not.
export async function canonicalAccountIdentities(addresses) {
  if (!Array.isArray(addresses) || addresses.length < 1 || addresses.length > 16)
    throw new Error('Between one and sixteen account identities are required.');
  await cryptoWaitReady();
  return addresses.map(address => {
    if (typeof address !== 'string' || address.length < 1 || address.length > 128 || address !== address.trim())
      throw new Error('An exact bounded account identity is required.');
    const decoded = decodeAddress(address);
    if (decoded.length !== 32) throw new Error('Only AccountId32 identities are supported.');
    return '0x' + Buffer.from(decoded).toString('hex');
  });
}
