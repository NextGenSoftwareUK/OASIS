import { Interface } from 'ethers';

export const idBoundEscrowAbi = new Interface([
  'function escrowProtocolVersion() view returns(uint256)',
  'function isTokenAssociated(address token) view returns(bool)',
  'function associateToken(address token)',
  'function deposits(bytes32 id) view returns(address owner,address token,uint256 amount,bool released)',
  'function lock(bytes32 id,address token,uint256 amount)',
  'function unlock(bytes32 id)'
]);

export function isIdBoundEscrow(request) {
  if (request.escrowProtocol !== undefined && request.escrowProtocol !== 'aggregate-v1' && request.escrowProtocol !== 'id-bound-v1')
    throw new Error('An explicitly supported escrow protocol is required.');
  const selected=request.escrowProtocol === 'id-bound-v1';
  if(!selected && (request.depositId!==undefined || (request.amount!==undefined && request.amount!=='0')))
    throw new Error('Aggregate escrow cannot ignore explicit deposit identity or amount.');
  return selected;
}

export function validateIdBoundIntent(action, request) {
  if (!['tokenEscrowLock','tokenEscrowUnlock','tokenEscrowAssociate'].includes(action)) throw new Error('Unknown id-bound escrow mutation.');
  if(action==='tokenEscrowAssociate'){
    if(request.depositId!==undefined || request.amount!==undefined || request.amountUnits!==undefined || request.approvalTransactionId!=null)
      throw new Error('Association is a separate single-stage operation without deposit, amount or approval intent.');
    return;
  }
  if (!/^0x[0-9a-f]{64}$/i.test(request.depositId ?? '') || /^0x0{64}$/i.test(request.depositId))
    throw new Error('A nonempty canonical bytes32 deposit identity is required.');
  if (action === 'tokenEscrowLock' && (typeof request.amount !== 'string' || !/^[0-9]+(?:\.[0-9]+)?$/.test(request.amount) || !/[1-9]/.test(request.amount)))
    throw new Error('An exact positive decimal deposit amount is required.');
  if (action === 'tokenEscrowUnlock' && request.amount !== undefined && request.amount !== '0')
    throw new Error('Release uses the whole identified deposit, not a supplied partial amount.');
}

export function validateDeposit(record, owner, token, locking) {
  const same=(a,b)=>a.toLowerCase()===b.toLowerCase();
  const empty=same(record.owner,'0x'+'0'.repeat(40));
  if (locking ? (!empty || !same(record.token,'0x'+'0'.repeat(40)) || record.amount !== 0n || record.released)
    : (empty || !same(record.owner,owner) || !same(record.token,token) || record.released || record.amount <= 0n || record.amount > 9223372036854775807n))
    throw new Error('Deposit identity is used, missing, released or not owned by the requested signer/token.');
}

export function sameDeposit(left,right) { return left.length === right.length && left.every((value,index)=>value===right[index]); }

export function verifyDepositReadback(record, owner, token, amount, released) {
  if (record.owner.toLowerCase()!==owner.toLowerCase() || record.token.toLowerCase()!==token.toLowerCase()
    || record.amount!==amount || record.released!==released)
    throw new Error('Confirmed escrow deposit identity/state readback mismatch.');
}
