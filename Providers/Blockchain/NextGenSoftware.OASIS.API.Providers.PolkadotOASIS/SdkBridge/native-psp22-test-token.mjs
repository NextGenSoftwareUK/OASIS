// Test-only native asset fixture; no shipping provider selects this token.
import { openPinnedContractQuery } from './pinned-contract-query.mjs';
import { decodeInkScaleResult } from './ink-scale-result.mjs';

export const nativePsp22Fixture = Object.freeze({ prefix: 'psp22-test-token', artifact: 'oasis-native-psp22-test-token',
  supply: '1208925819614629174706176', salt: '0x' + Buffer.from('oasis-native-psp22-token-v1').toString('hex') });
export function nativeTestTokenConstructor(registry) {
  return '0x00000001' + Buffer.from(registry.createType('u128', nativePsp22Fixture.supply).toU8a()).toString('hex');
}
export async function readNativeTestToken(api, request) {
  request = structuredClone(request);
  for (const name of ['owner', 'spender'])
    if (typeof request[name] !== 'string' || !/^0x[0-9a-f]{64}$/.test(request[name]))
      throw new Error('Exact native acceptance token owner/spender required.');
  const owner = Buffer.from(api.registry.createType('AccountId32', request.owner).toU8a()).toString('hex');
  const spender = Buffer.from(api.registry.createType('AccountId32', request.spender).toU8a()).toString('hex');
  api.registry.register({ NativePsp22TestLangError: { _enum: { CouldNotReadInput: 1 } } });
  const view = await openPinnedContractQuery(api, request);
  const query = async (selector, type) => decodeInkScaleResult(api.registry, type, 'NativePsp22TestLangError',
    await view.query(selector), 'Native test token');
  return { ...view.identity, owner: request.owner, spender: request.spender,
    totalSupply: (await query('0x162df8c2', 'u128')).toString(),
    ownerBalance: (await query('0x6568382f' + owner, 'u128')).toString(),
    spenderBalance: (await query('0x6568382f' + spender, 'u128')).toString(),
    allowance: (await query('0x4d47d921' + owner + spender, 'u128')).toString(),
    decimals: (await query('0x7271b782', 'u8')).toNumber() };
}
