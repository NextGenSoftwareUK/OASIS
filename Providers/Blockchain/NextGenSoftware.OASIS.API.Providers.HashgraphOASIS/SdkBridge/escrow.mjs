import { AccountId, ContractId, TokenId, TransactionId, ContractExecuteTransaction, AccountAllowanceApproveTransaction, Hbar } from '@hiero-ledger/sdk';
import { Interface, getBytes, keccak256 } from 'ethers';
import { AccountInfoQuery, ContractInfoQuery, ContractByteCodeQuery, TokenNftInfoQuery, ContractCallQuery } from '@hiero-ledger/sdk';

export const escrowAbi = new Interface([
  'function lockNFT(address token,uint256 serial)',
  'function unlockNFT(address token,uint256 serial)',
  'function nftDepositor(address token,uint256 serial) view returns(address)',
  'function lockAll(address token) returns(uint256)',
  'function unlock(address token) returns(uint256)',
  'function locked(address token,address depositor) view returns(uint256)'
]);

export function validateEscrowRequest(request) {
  const context = validateEscrowContext(request);
  if (!/^[1-9][0-9]*$/.test(request.serial ?? '') || BigInt(request.serial) > 9223372036854775807n)
    throw new Error('Escrow requires one positive exact int64 NFT serial.');
  return context;
}

function validateEscrowContext(request) {
  if (!/^0x[0-9a-f]{64}$/i.test(request.runtimeCodeHash ?? '')) throw new Error('An explicit escrow runtime code hash is required.');
  for (const field of ['sender', 'contractId', 'tokenId'])
    if (!/^[0-9]+\.[0-9]+\.[1-9][0-9]*$/.test(request[field] ?? ''))
      throw new Error(`Escrow requires explicit nonzero numeric ${field}.`);
  const sender = AccountId.fromString(request.sender);
  const contract = ContractId.fromString(request.contractId);
  const token = TokenId.fromString(request.tokenId);
  if (!Number.isSafeInteger(request.gas) || request.gas <= 0)
    throw new Error('Escrow requires an explicit positive safe-integer gas limit.');
  if (!/^[1-9][0-9]*$/.test(request.maxFeeTinybars ?? '') || BigInt(request.maxFeeTinybars) > 9223372036854775807n)
    throw new Error('Escrow requires an explicit positive int64 fee ceiling.');
  const id = TransactionId.fromString(request.transactionId);
  if (id.accountId.toString() !== sender.toString()) throw new Error('Escrow transaction payer mismatch.');
  return { sender, contract, token, id };
}

export function verifyEscrowRuntime(bytes, expectedHash) {
  if (!bytes.length || keccak256(bytes).toLowerCase() !== expectedHash.toLowerCase())
    throw new Error('Escrow runtime code does not match the configured verified deployment.');
}

export async function verifyDepositorAccount(sender, address, queryAccount) {
  if (!/^0x[0-9a-f]{40}$/i.test(address) || /^0x0{40}$/i.test(address)) throw new Error('Escrow has no valid EVM depositor.');
  const alias = AccountId.fromEvmAddress(sender.shard, sender.realm, address);
  const info = await queryAccount(alias);
  if (info.isDeleted || info.accountId.toString() !== sender.toString())
    throw new Error('Escrow EVM depositor does not resolve to the requesting account.');
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
  const approvalId = validateApprovalId(request, sender, id);
  return new AccountAllowanceApproveTransaction()
    .approveTokenNftAllowance(token.toString() + '/' + request.serial, sender, contract)
    .setMaxTransactionFee(Hbar.fromTinybars(request.maxFeeTinybars))
    .setTransactionId(approvalId).setMaxAttempts(1).setRegenerateTransactionId(false);
}

function validateApprovalId(request, sender, mutationId) {
  const approvalId = TransactionId.fromString(request.approvalTransactionId);
  if (approvalId.accountId.toString() !== sender.toString() || approvalId.toString() === mutationId.toString())
    throw new Error('Escrow approval requires a distinct transaction ID for the same payer.');
  return approvalId;
}

export function tokenEscrowTransaction(action, request) {
  if (!['tokenEscrowLock', 'tokenEscrowUnlock'].includes(action)) throw new Error('Unknown token escrow mutation.');
  const { contract, token, id } = validateEscrowContext(request);
  return new ContractExecuteTransaction().setContractId(contract).setGas(request.gas)
    .setFunctionParameters(getBytes(escrowAbi.encodeFunctionData(action === 'tokenEscrowLock' ? 'lockAll' : 'unlock', ['0x' + token.toSolidityAddress()])))
    .setMaxTransactionFee(Hbar.fromTinybars(request.maxFeeTinybars))
    .setTransactionId(id).setMaxAttempts(1).setRegenerateTransactionId(false);
}

export function tokenEscrowApprovalTransaction(request) {
  const { sender, contract, token, id } = validateEscrowContext(request);
  if (typeof request.amountUnits !== 'string' || !/^[1-9][0-9]*$/.test(request.amountUnits) || BigInt(request.amountUnits) > 9223372036854775807n)
    throw new Error('Token escrow approval requires exact positive int64 token units.');
  return new AccountAllowanceApproveTransaction()
    .approveTokenAllowance(token, sender, contract, BigInt(request.amountUnits))
    .setMaxTransactionFee(Hbar.fromTinybars(request.maxFeeTinybars))
    .setTransactionId(validateApprovalId(request, sender, id)).setMaxAttempts(1).setRegenerateTransactionId(false);
}

export async function executeEscrow(action, request, { createClient, restoreKey }) {
  const mutation = escrowTransaction(action, request);
  const approval = action === 'nftEscrowLock' ? escrowApprovalTransaction(request) : null;
  const { sender, contract, token } = validateEscrowRequest(request);
  const key = restoreKey(request);
  if (request.publicKey && request.publicKey.toLowerCase() !== key.publicKey.toStringDer().toLowerCase())
    throw new Error('Escrow signing key mismatch.');
  const client = createClient(request);
  let approvalTransactionId = null;
  let stage = 'validation';
  try {
    client.setOperator(sender, key);
    const payment = Hbar.fromTinybars(request.maxFeeTinybars);
    const account = await new AccountInfoQuery().setAccountId(sender).setMaxQueryPayment(payment).execute(client);
    if (account.accountId.toString() !== sender.toString() || account.isDeleted ||
        typeof account.key?.toStringDer !== 'function' || account.key.toStringDer().toLowerCase() !== key.publicKey.toStringDer().toLowerCase())
      throw new Error('Escrow requires the real active sender account single signing key.');
    const contractInfo = await new ContractInfoQuery().setContractId(contract).setMaxQueryPayment(payment).execute(client);
    if (contractInfo.contractId.toString() !== contract.toString() || contractInfo.isDeleted)
      throw new Error('Escrow contract identity mismatch.');
    const runtimeCode = await new ContractByteCodeQuery().setContractId(contract).setMaxQueryPayment(payment).execute(client);
    verifyEscrowRuntime(runtimeCode, request.runtimeCodeHash);
    const queryAccount = alias => new AccountInfoQuery().setAccountId(alias).setMaxQueryPayment(payment).execute(client);
    const nftId = token.toString() + '/' + request.serial;
    async function nftInfo() {
      const records = await new TokenNftInfoQuery().setNftId(nftId).setMaxQueryPayment(payment).execute(client);
      if (records.length !== 1 || records[0].nftId.toString() !== nftId) throw new Error('Escrow NFT identity mismatch.');
      return records[0];
    }
    async function depositor() {
      const read = await new ContractCallQuery().setContractId(contract).setGas(request.gas).setMaxQueryPayment(payment)
        .setFunctionParameters(getBytes(escrowAbi.encodeFunctionData('nftDepositor', ['0x' + token.toSolidityAddress(), BigInt(request.serial)]))).execute(client);
      if (read.errorMessage) throw new Error('Escrow state query reverted.');
      return escrowAbi.decodeFunctionResult('nftDepositor', read.bytes)[0].toLowerCase();
    }
    const zero = '0x' + '0'.repeat(40);
    const current = await nftInfo();
    const recorded = await depositor();
    const locking = action === 'nftEscrowLock';
    if (current.accountId.toString() !== (locking ? sender.toString() : contractInfo.accountId.toString()) ||
        (locking ? recorded !== zero : recorded === zero)) throw new Error('Escrow NFT custody/deposit precondition failed.');
    if (!locking) await verifyDepositorAccount(sender, recorded, queryAccount);
    if (locking && current.spenderId && current.spenderId.toString() !== contractInfo.accountId.toString())
      throw new Error('An existing different NFT allowance requires explicit revocation.');
    if (locking && !current.spenderId) {
      stage = 'approval';
      approval.freezeWith(client);
      const submitted = await (await approval.sign(key)).execute(client);
      const receipt = await submitted.getReceipt(client);
      if (receipt.status.toString() !== 'SUCCESS') throw new Error('Escrow approval did not establish consensus success.');
      approvalTransactionId = submitted.transactionId.toString();
      const approved = await nftInfo();
      if (approved.accountId.toString() !== sender.toString() || approved.spenderId?.toString() !== contractInfo.accountId.toString())
        throw new Error('Confirmed NFT allowance readback mismatch.');
    }
    stage = 'mutation';
    mutation.freezeWith(client);
    const submitted = await (await mutation.sign(key)).execute(client);
    const receipt = await submitted.getReceipt(client);
    if (receipt.status.toString() !== 'SUCCESS') throw new Error('Escrow mutation did not establish consensus success.');
    stage = 'readback';
    const after = await nftInfo();
    const recordedAfter = await depositor();
    if (after.accountId.toString() !== (locking ? contractInfo.accountId.toString() : sender.toString()) ||
        (locking ? recordedAfter === zero : recordedAfter !== zero)) throw new Error('Confirmed escrow custody readback mismatch.');
    if (locking) await verifyDepositorAccount(sender, recordedAfter, queryAccount);
    return { transactionId: submitted.transactionId.toString(), approvalTransactionId, status: 'SUCCESS', depositor: recordedAfter };
  } catch (cause) {
    const error = new Error('Hedera escrow did not establish verified success. Reconcile the original mutation and any approval before retrying.');
    error.transactionId = stage === 'approval' ? request.approvalTransactionId : request.transactionId;
    error.mutationTransactionId = request.transactionId;
    error.approvalTransactionId = approvalTransactionId;
    error.stage = stage;
    error.status = cause.status?.toString();
    throw error;
  } finally { client.close(); }
}
