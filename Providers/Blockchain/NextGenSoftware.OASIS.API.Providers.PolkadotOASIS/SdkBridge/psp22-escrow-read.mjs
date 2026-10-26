import { openPinnedContractQuery } from './pinned-contract-query.mjs';
import { encodeTokenEscrowDepositQuery, decodeTokenEscrowResult } from './oasis-psp22-escrow-codec.mjs';

export async function readTokenEscrowDeposit(api, request) {
  request = structuredClone(request);
  const query = encodeTokenEscrowDepositQuery(request.depositId);
  const view = await openPinnedContractQuery(api, request);
  const deposit = decodeTokenEscrowResult(api.registry, 'deposit', await view.query(query));
  return { ...view.identity, depositId: request.depositId, deposit };
}
