import { Ed25519Keypair } from '@mysten/sui/keypairs/ed25519';
import { SuiGrpcClient } from '@mysten/sui/grpc';
import { Transaction } from '@mysten/sui/transactions';
import { isValidSuiAddress } from '@mysten/sui/utils';
import { fileURLToPath } from 'node:url';
import { resolve } from 'node:path';
import { generateMnemonic } from '@scure/bip39';
import { wordlist } from '@scure/bip39/wordlists/english.js';

function wallet(keypair) {
  return { privateKey: keypair.getSecretKey(), publicKey: keypair.getPublicKey().toBase64(), address: keypair.toSuiAddress() };
}

export async function invoke(request) {
  if (request.operation === 'generateKey') return wallet(Ed25519Keypair.generate());
  if (request.operation === 'restoreKey') return wallet(Ed25519Keypair.fromSecretKey(request.privateKey));
  if (request.operation === 'createAccount') {
    const seedPhrase = generateMnemonic(wordlist, 256);
    return { ...wallet(Ed25519Keypair.deriveKeypair(seedPhrase)), seedPhrase };
  }
  if (request.operation === 'restoreAccount') return wallet(Ed25519Keypair.deriveKeypair(request.seedPhrase));
  let signer;
  let amount;
  if (request.operation === 'transfer') {
    signer = Ed25519Keypair.fromSecretKey(request.privateKey);
    if (signer.toSuiAddress() !== request.fromWalletAddress) throw new Error('Signing key does not own sender wallet');
    if (!isValidSuiAddress(request.toWalletAddress)) throw new Error('Valid Sui recipient is required');
    if (!/^[1-9][0-9]*$/.test(request.amountUnits)) throw new Error('Positive integral MIST amount is required');
    amount = BigInt(request.amountUnits);
    if (amount > 18446744073709551615n) throw new Error('Sui amount exceeds u64');
  }
  const url = new URL(request.rpcEndpoint);
  if (!['http:', 'https:'].includes(url.protocol)) throw new Error('Sui RPC must use HTTP(S)');
  const client = new SuiGrpcClient({ network: request.network, baseUrl: url.href });
  const { chainIdentifier } = await client.getChainIdentifier();
  if (!chainIdentifier) throw new Error('Sui node returned no genesis chain identifier');
  if (request.chainId && request.chainId !== chainIdentifier) throw new Error('Sui chain identifier mismatch');
  if (request.operation === 'probe') return { chainIdentifier };
  if (request.operation === 'transaction') {
    if (!request.transactionHash) throw new Error('Transaction digest is required');
    const response = await client.getTransaction({ digest: request.transactionHash, include: { effects: true } });
    const transaction = response.Transaction ?? response.FailedTransaction;
    if (!transaction?.digest || !transaction.effects) throw new Error('Sui returned no transaction effects');
    return { transactionHash: transaction.digest, status: transaction.status, gasUsed: transaction.effects.gasUsed };
  }
  if (request.operation === 'balance') {
    if (!isValidSuiAddress(request.walletAddress)) throw new Error('Valid Sui wallet address is required');
    return (await client.getBalance({ owner: request.walletAddress, coinType: '0x2::sui::SUI' })).balance;
  }
  if (request.operation === 'transfer') {
    const tx = new Transaction();
    tx.setSender(signer.toSuiAddress());
    const [coin] = tx.splitCoins(tx.gas, [tx.pure.u64(amount)]);
    tx.transferObjects([coin], tx.pure.address(request.toWalletAddress));
    const executed = await client.signAndExecuteTransaction({ transaction: tx, signer, include: { effects: true } });
    if (executed.FailedTransaction) throw new Error(JSON.stringify(executed.FailedTransaction.status));
    const committed = executed.Transaction;
    if (!committed?.digest) throw new Error('Sui SDK returned no successful transaction digest');
    await client.waitForTransaction({ digest: committed.digest });
    return { transactionHash: committed.digest };
  }
  throw new Error(`Unsupported Sui SDK operation: ${request.operation}`);
}

// Credentials enter through stdin, never command-line arguments.
if (process.argv[1] && fileURLToPath(import.meta.url) === resolve(process.argv[1])) {
  try {
    let input = '';
    for await (const chunk of process.stdin) input += chunk;
    const result = await invoke(JSON.parse(input));
    process.stdout.write(JSON.stringify({ ok: true, result }));
  } catch (error) {
    process.stdout.write(JSON.stringify({ ok: false, error: error.message }));
    process.exitCode = 1;
  }
}
