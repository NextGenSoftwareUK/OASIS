// Explicit OASIS PSP22 per-deposit custody ABI. Not pallet-assets/freeze/thaw.
// Pure SCALE codec; signing/admission/submission remain in their existing owner.
import { decodeInkScaleResult } from './ink-scale-result.mjs';
const zero = '0x' + '00'.repeat(32);
function account(value, label) {
  if (typeof value !== 'string' || !/^0x[0-9a-f]{64}$/.test(value) || value === zero)
    throw new Error(`Canonical nonzero ${label} required.`);
  return value;
}
function amount(value) {
  if (typeof value !== 'string' || value.length > 39 || !/^[1-9][0-9]*$/.test(value) || BigInt(value) > (1n << 128n) - 1n)
    throw new Error('Exact positive canonical u128 base-unit amount required.');
  return value;
}
export function encodeTokenEscrowMutation(registry, request) {
  const id = account(request.depositId, 'deposit ID');
  if (request.to !== undefined) throw new Error('Escrow has no arbitrary release recipient.');
  if (request.action === 'lock') {
    const token = account(request.tokenAddress, 'PSP22 token account');
    return '0x00000020' + Buffer.from(registry.createType('(H256,AccountId32,u128)',
      [id, token, amount(request.amountUnits)]).toU8a()).toString('hex');
  }
  if (request.action !== 'unlock' || request.tokenAddress !== undefined || request.amountUnits !== undefined)
    throw new Error('Unlock uses only the original deposit ID; no substituted token or amount.');
  return '0x00000021' + id.slice(2);
}
export function decodeTokenEscrowMutation(registry, input) {
  if (typeof input !== 'string' || !/^0x(?:[0-9a-f]{2}){4,}$/.test(input)) throw new Error('Canonical escrow call bytes required.');
  const selector = input.slice(2, 10);
  if (!['00000020', '00000021'].includes(selector)) throw new Error('Unknown escrow selector.');
  const raw = Buffer.from(input.slice(10), 'hex');
  const args = registry.createType(selector === '00000020' ? '(H256,AccountId32,u128)' : 'H256', raw);
  if (!Buffer.from(args.toU8a()).equals(raw)) throw new Error('Noncanonical escrow arguments or trailing data.');
  const request = selector === '00000020'
    ? { action: 'lock', depositId: args[0].toHex(), tokenAddress: args[1].toHex(), amountUnits: args[2].toString() }
    : { action: 'unlock', depositId: args.toHex() };
  if (encodeTokenEscrowMutation(registry, request) !== input) throw new Error('Escrow intent does not round-trip.');
  return request;
}
export function encodeTokenEscrowDepositQuery(depositId) {
  return '0x00000022' + account(depositId, 'deposit ID').slice(2);
}
export function registerTokenEscrowTypes(registry) {
  registry.register({
    OasisTokenEscrowDeposit: { owner: 'AccountId32', token: 'AccountId32', amount: 'u128', released: 'bool' },
    OasisTokenEscrowError: { _enum: ['InvalidDeposit', 'AlreadyExists', 'Missing', 'NotOwner', 'Released', 'TokenCallFailed'] },
    OasisTokenEscrowLangError: { _enum: { CouldNotReadInput: 1 } }
  });
}
export function decodeTokenEscrowResult(registry, kind, bytes) {
  if (!['mutation', 'deposit'].includes(kind) || !(bytes instanceof Uint8Array) || bytes.length === 0)
    throw new Error('Explicit escrow return type and bytes required.');
  registerTokenEscrowTypes(registry);
  const type = kind === 'mutation' ? 'Result<(),OasisTokenEscrowError>' : 'Option<OasisTokenEscrowDeposit>';
  const result = decodeInkScaleResult(registry, type, 'OasisTokenEscrowLangError', bytes, 'Escrow');
  if (kind === 'mutation') return true;
  if (result.isNone) return null;
  const deposit = result.unwrap();
  return { owner: deposit.owner.toHex(), tokenAddress: deposit.token.toHex(),
    amountUnits: deposit.amount.toString(), released: deposit.released.isTrue };
}
