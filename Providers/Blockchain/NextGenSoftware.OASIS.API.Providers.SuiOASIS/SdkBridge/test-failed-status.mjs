// One bounded failure on the retained local fixture, no setup, publication or credentials in output.
import { readFileSync, writeFileSync, existsSync } from 'node:fs';
import { SuiGrpcClient } from '@mysten/sui/grpc';
import { Ed25519Keypair } from '@mysten/sui/keypairs/ed25519';
import { Transaction } from '@mysten/sui/transactions';
const endpoint = process.env.OASIS_SUI_TEST_RPC;
if (!endpoint || !['127.0.0.1', 'localhost', '[::1]'].includes(new URL(endpoint).hostname)) throw new Error('Local chain required');
const fixture = JSON.parse(readFileSync(process.env.OASIS_SUI_TEST_DEPLOYMENT === '-' ? 0 : process.env.OASIS_SUI_TEST_DEPLOYMENT, 'utf8'));
const signer = Ed25519Keypair.fromSecretKey(fixture.privateKey);
const client = new SuiGrpcClient({ network: 'localnet', baseUrl: endpoint });
const journalPath = process.env.OASIS_SUI_TEST_FAILED_JOURNAL;
if (!journalPath) throw new Error('An explicit fixed failed-status journal path is required');
if (existsSync(journalPath)) {
  const journal = JSON.parse(readFileSync(journalPath, 'utf8'));
  if (journal.chainId !== fixture.chainId || journal.packageAddress !== fixture.packageAddress || journal.sender !== signer.toSuiAddress()
      || typeof journal.digest !== 'string' || !journal.digest)
    throw new Error('Retained failed-status journal does not match this owned deployment');
  const response = await client.getTransaction({ digest: journal.digest, include: { effects: true } });
  const failed = response.FailedTransaction;
  if (failed?.digest !== journal.digest || failed.status?.success !== false)
    throw new Error('Retained submission is not verified as failed; reconcile original digest, never resubmit');
  console.log(journal.digest);
} else {
const tx = new Transaction();
tx.setSender(signer.toSuiAddress());
tx.setGasBudget(10000000);
// Resolve the kind with the official SDK; zero mint aborts in the deployed Move contract.
const coin = tx.moveCall({ target: `${fixture.packageAddress}::token::mint`, arguments: [tx.object(fixture.currencyObjectId), tx.pure.u64(0)] });
tx.transferObjects([coin], tx.pure.address(signer.toSuiAddress()));
const resolved = Transaction.fromKind(await tx.build({ client, onlyTransactionKind: true }));
resolved.setSender(signer.toSuiAddress());
resolved.setGasBudget(10000000);
resolved.setGasPrice((await client.getReferenceGasPrice()).referenceGasPrice);
const coins = await client.listCoins({ owner: signer.toSuiAddress(), coinType: '0x2::sui::SUI' });
const gas = coins.objects.find(coin => BigInt(coin.balance) >= 10000000n);
if (!gas) throw new Error('Retained local fixture has no sufficient gas coin');
resolved.setGasPayment([{ objectId: gas.objectId, version: gas.version, digest: gas.digest }]);
const bytes = await resolved.build(); // All inputs resolved: no failing simulation or SDK patch required.
const originalDigest = await resolved.getDigest();
// Exclusive creation happens before submission. A crash/unknown outcome leaves identity for read-only reconciliation.
writeFileSync(journalPath, JSON.stringify({ chainId: fixture.chainId, packageAddress: fixture.packageAddress,
  sender: signer.toSuiAddress(), digest: originalDigest }), { flag: 'wx', mode: 0o600 });
const response = await client.signAndExecuteTransaction({ transaction: bytes, signer, include: { effects: true } });
const failed = response.FailedTransaction;
if (failed?.digest !== originalDigest || failed.status?.success !== false) throw new Error('Expected original committed failed transaction; reconcile journal before retrying');
await client.waitForTransaction({ digest: failed.digest });
console.log(failed.digest);
}
