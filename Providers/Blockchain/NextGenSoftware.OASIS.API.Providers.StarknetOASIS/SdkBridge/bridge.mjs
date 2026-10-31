// Starknet SDK bridge: one JSON request on stdin, one {ok, result} or {ok:false, error} envelope on stdout.
// Secrets arrive only on stdin and are never echoed. Network calls pin the chain id first.
import { readFileSync } from 'node:fs';
import { pathToFileURL } from 'node:url';
import { Account, RpcProvider, num } from 'starknet';
import { accountAddress, keysFromMnemonic, keysFromPrivateKey, newMnemonic, normalizeFelt, validateChainId, validateEndpoint, validateIndex } from './account.mjs';

const OPERATIONS = ['createAccount', 'restoreAccount', 'deriveFromPrivateKey', 'verifyAccountClass', 'accountStatus', 'estimateDeployFee', 'deployAccount'];
const NETWORK = ['verifyAccountClass', 'accountStatus', 'estimateDeployFee', 'deployAccount'];

/** The class must be an account whose constructor takes exactly one felt public key (OpenZeppelin account shape). */
export function assertAccountClass(contractClass) {
  const abi = typeof contractClass?.abi === 'string' ? JSON.parse(contractClass.abi) : contractClass?.abi;
  if (!Array.isArray(abi)) throw new Error('Account class ABI is unavailable.');
  const ctor = abi.find(entry => entry.type === 'constructor');
  if (!ctor || ctor.inputs?.length !== 1 || ctor.inputs[0].type !== 'core::felt252')
    throw new Error('Account class constructor must take exactly one felt252 public key.');
  const text = JSON.stringify(abi);
  if (!text.includes('__validate_deploy__') || !text.includes('__execute__'))
    throw new Error('Class is not a deployable account (missing __validate_deploy__ or __execute__).');
}

export async function run(request, connect = endpoint => new RpcProvider({ nodeUrl: endpoint })) {
  if (!request || !OPERATIONS.includes(request.operation)) throw new Error('Unknown Starknet SDK operation.');
  const op = request.operation;

  if (op === 'createAccount') {
    const mnemonic = newMnemonic(request.words ?? 12);
    const keys = keysFromMnemonic(mnemonic, 0);
    return { mnemonic, index: 0, ...keys, address: accountAddress(keys.publicKey, request.classHash) };
  }
  if (op === 'restoreAccount') {
    const index = validateIndex(request.index ?? 0);
    const keys = keysFromMnemonic(request.mnemonic, index);
    return { index, ...keys, address: accountAddress(keys.publicKey, request.classHash) };
  }
  if (op === 'deriveFromPrivateKey') {
    const keys = keysFromPrivateKey(request.privateKey);
    return { ...keys, address: accountAddress(keys.publicKey, request.classHash) };
  }

  const provider = connect(validateEndpoint(request.endpoint));
  const expected = validateChainId(request.chainId);
  const actual = (await provider.getChainId()).toLowerCase();
  if (actual !== expected) throw new Error(`RPC chain ${actual} differs from the configured chain ${expected}.`);

  if (op === 'accountStatus') {
    const address = normalizeFelt(request.address, 'address');
    try {
      const classHash = await provider.getClassHashAt(address);
      return { address, deployed: true, classHash: num.toHex(classHash), nonce: num.toHex(await provider.getNonceForAddress(address)) };
    } catch (error) {
      if (/contract not found|CONTRACT_NOT_FOUND|"code":\s*20\b/i.test(String(error?.message ?? error))) return { address, deployed: false };
      throw error;
    }
  }

  const classHash = normalizeFelt(request.classHash, 'classHash');
  assertAccountClass(await provider.getClassByHash(classHash));
  if (op === 'verifyAccountClass') return { classHash, valid: true };

  const keys = keysFromPrivateKey(request.privateKey);
  const address = accountAddress(keys.publicKey, classHash);
  const account = new Account({ provider, address, signer: keys.privateKey });
  const payload = { classHash, constructorCalldata: [keys.publicKey], addressSalt: keys.publicKey, contractAddress: address };

  if (op === 'estimateDeployFee') {
    const fee = await account.estimateAccountDeployFee(payload);
    return { address, overallFee: num.toHex(fee.overall_fee), unit: fee.unit ?? 'FRI' };
  }

  // deployAccount: refuse an already-deployed address; submit once; report the hash even if confirmation fails.
  try {
    await provider.getClassHashAt(address);
    throw new Error(`Account ${address} is already deployed.`);
  } catch (error) {
    if (!/contract not found|CONTRACT_NOT_FOUND|"code":\s*20\b/i.test(String(error?.message ?? error))) throw error;
  }
  const { transaction_hash: transactionHash } = await account.deployAccount(payload);
  try {
    const receipt = await provider.waitForTransaction(transactionHash, { retryInterval: 3000 });
    const execution = receipt.execution_status ?? receipt.value?.execution_status;
    const finality = receipt.finality_status ?? receipt.value?.finality_status;
    if (execution !== 'SUCCEEDED') {
      const failure = new Error(`Deploy-account transaction ${transactionHash} did not succeed (${execution}).`);
      failure.transactionHash = transactionHash;
      throw failure;
    }
    return { address, transactionHash, executionStatus: execution, finalityStatus: finality };
  } catch (error) {
    error.transactionHash ??= transactionHash;
    throw error;
  }
}

if (import.meta.url === pathToFileURL(process.argv[1]).href) {
  // stdout carries exactly one envelope; SDK logging (which uses console.log for some levels) goes to stderr.
  console.log = console.info = console.debug = (...args) => console.error(...args);
  let envelope;
  try {
    let request;
    try { request = JSON.parse(readFileSync(0, 'utf8')); } catch { throw new Error('SDK request must be valid JSON.'); }
    if (NETWORK.includes(request?.operation)) validateEndpoint(request.endpoint);
    envelope = { ok: true, result: await run(request) };
  } catch (error) {
    // Never echo the request: it may carry a private key or mnemonic.
    envelope = { ok: false, error: String(error?.message ?? error).slice(0, 500), ...(error?.transactionHash ? { transactionHash: error.transactionHash } : {}) };
  }
  process.stdout.write(JSON.stringify(envelope));
  process.exitCode = envelope.ok ? 0 : 1;
}
