import { AccountId, ContractId, TokenId, TransactionId, ContractExecuteTransaction, AccountAllowanceApproveTransaction, Hbar } from '@hiero-ledger/sdk';
import { Interface, getBytes } from 'ethers';

export const escrowAbi = new Interface([
  'function lockNFT(address token,uint256 serial)',
  'function unlockNFT(address token,uint256 serial)',
  'function nftDepositor(address token,uint256 serial) view returns(address)'
]);

export function validateEscrowRequest(request) {
  for (const field of ['sender', 'contractId', 'tokenId'])
    if (!/^[0-9]+\.[0-9]+\.[1-9][0-9]*$/.test(request[field] ?? ''))
      throw new Error(`Escrow requires explicit nonzero numeric ${field}.`);
  const sender = AccountId.fromString(request.sender);
  const contract = ContractId.fromString(request.contractId);
  const token = TokenId.fromString(request.tokenId);
  if (!/^[1-9][0-9]*$/.test(request.serial ?? '') || BigInt(request.serial) > 9223372036854775807n)
    throw new Error('Escrow requires one positive exact int64 NFT serial.');
  if (!Number.isSafeInteger(request.gas) || request.gas <= 0)
    throw new Error('Escrow requires an explicit positive safe-integer gas limit.');
  if (!/^[1-9][0-9]*$/.test(request.maxFeeTinybars ?? '') || BigInt(request.maxFeeTinybars) > 9223372036854775807n)
    throw new Error('Escrow requires an explicit positive int64 fee ceiling.');
  const id = TransactionId.fromString(request.transactionId);
  if (id.accountId.toString() !== sender.toString()) throw new Error('Escrow transaction payer mismatch.');
  return { sender, contract, token, id };
}

export function escrowTransaction(action, request) {
  if (!['nftEscrowLock', 'nftEscrowUnlock'].includes(action)) throw new Error('Unknown NFT escrow mutation.');
  const { contract, token, id } = validateEscrowRequest(request);
  const name = action === 'nftEscrowLock' ? 'lockNFT' : 'unlockNFT';
  return new ContractExecuteTransaction().setContractId(contract).setGas(request.gas)
    .setFunctionParameters(getBytes(escrowAbi.encodeFunctionData(name, ['0x' + token.toSolidityAddress(), BigInt(request.serial)])))
    .setMaxTransactionFee(Hbar.fromTinybars(request.maxFeeTinybars))
    .setTransactionId(id).setMaxAttempts(1).setRegenerateTransactionId(false);
}

export function escrowApprovalTransaction(request) {
  const { sender, contract, token, id } = validateEscrowRequest(request);
  const approvalId = TransactionId.fromString(request.approvalTransactionId);
  if (approvalId.accountId.toString() !== sender.toString() || approvalId.toString() === id.toString())
    throw new Error('Escrow approval requires a distinct transaction ID for the same payer.');
  return new AccountAllowanceApproveTransaction()
    .approveTokenNftAllowance(token.toString() + '/' + request.serial, sender, contract)
    .setMaxTransactionFee(Hbar.fromTinybars(request.maxFeeTinybars))
    .setTransactionId(approvalId).setMaxAttempts(1).setRegenerateTransactionId(false);
}
