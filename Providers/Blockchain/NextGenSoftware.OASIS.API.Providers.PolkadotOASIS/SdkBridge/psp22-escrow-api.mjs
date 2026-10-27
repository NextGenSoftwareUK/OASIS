import { nativeUnits } from './native-transfer.mjs';
import { readPsp22Precision } from './psp22-token-precision.mjs';
import { validateTokenEscrowIdentity, validateTokenEscrowMutation, prepareTokenEscrowMutation } from './psp22-escrow-mutation.mjs';

export async function validateTokenEscrowApiRequest(request) {
  if (request.amount === undefined) return validateTokenEscrowMutation(request);
  if (request.action !== 'lock' || request.amountUnits !== undefined || request.to !== undefined)
    throw new Error('Decimal Amount belongs to lock only and cannot replace base units or a release destination.');
  if (typeof request.amount !== 'string' || request.amount.length > 100
      || !/^(0|[1-9][0-9]*)(\.[0-9]+)?$/.test(request.amount) || !/[1-9]/.test(request.amount))
    throw new Error('Exact positive invariant decimal token amount required.');
  await validateTokenEscrowIdentity(request);
}
export async function prepareTokenEscrowApiMutation(api, request) {
  request = structuredClone(request);
  await validateTokenEscrowApiRequest(request);
  if (request.amount === undefined) return prepareTokenEscrowMutation(api, request);
  const precision = await readPsp22Precision(api, request);
  const amountUnits = nativeUnits(request.amount, precision.decimals);
  delete request.amount;
  const prepared = await prepareTokenEscrowMutation(api, { ...request, amountUnits });
  return { ...prepared, amountDecimals: precision.decimals, precisionBlockHash: precision.blockHash,
    tokenCodeHashObserved: precision.codeHash };
}
