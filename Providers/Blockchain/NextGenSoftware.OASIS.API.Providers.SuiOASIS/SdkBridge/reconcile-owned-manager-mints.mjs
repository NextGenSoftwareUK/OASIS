// Exact local test reconciliation. Read-only unless explicitly invoked with --burn.
import assert from 'node:assert/strict';
import { readFile, writeFile } from 'node:fs/promises';
import { SuiGrpcClient } from '@mysten/sui/grpc';
import { Ed25519Keypair } from '@mysten/sui/keypairs/ed25519';
import { Transaction } from '@mysten/sui/transactions';
import { bcs } from '@mysten/sui/bcs';
const fixture = JSON.parse(await readFile(process.env.OASIS_SUI_TEST_DEPLOYMENT, 'utf8'));
const wallet = JSON.parse(await readFile(process.env.OASIS_SUI_TEST_DEPLOYMENT + '.token-wallet.json', 'utf8'));
assert.equal(wallet.chainId, fixture.chainId);
const key = Ed25519Keypair.fromSecretKey(wallet.wallet.privateKey);
const owner = key.toSuiAddress();
const client = new SuiGrpcClient({ network: 'localnet', baseUrl: 'http://127.0.0.1:9000' });
assert.equal((await client.getChainIdentifier()).chainIdentifier, fixture.chainId);
const start = Date.parse(process.env.OASIS_SUI_RECONCILE_START);
const end = Date.parse(process.env.OASIS_SUI_RECONCILE_END);
assert.ok(Number.isFinite(start) && Number.isFinite(end) && end >= start);
const schema = bcs.struct('NFT', { id: bcs.Address, creator: bcs.Address, metadata: bcs.string() });
const matches = [];
let cursor;
do {
  const page = await client.listOwnedObjects({ owner, type: `${fixture.packageAddress}::nft::NFT`,
    include: { content: true, previousTransaction: true }, limit: 100, cursor });
  for (const object of page.objects) {
    const nft = schema.parse(object.content);
    const metadata = JSON.parse(nft.metadata);
    if (metadata.Title !== 'Retained native manager oak') continue;
    assert.equal(nft.creator, owner);
    assert.equal(metadata.ImageUrl, 'https://example.invalid/oak.png');
    assert.match(metadata.MintedByAvatarId, /^[0-9a-f-]{36}$/i);
    assert.notEqual(metadata.MintedByAvatarId, '00000000-0000-0000-0000-000000000000');
    const response = await client.getTransaction({ digest: object.previousTransaction, include: { transaction: true } });
    const tx = response.Transaction;
    assert.ok(tx && tx.status.success === true && tx.digest === object.previousTransaction);
    assert.equal(tx.transaction.sender, owner);
    assert.ok(Number(tx.timestampMs) >= start && Number(tx.timestampMs) <= end,
      JSON.stringify({ error: 'Object is not from the exact failed test window', objectId: object.objectId, digest: object.previousTransaction, timestampMs: tx.timestampMs, start, end }));
    matches.push({ objectId: object.objectId, originalDigest: object.previousTransaction, actor: metadata.MintedByAvatarId });
  }
  cursor = page.hasNextPage ? page.cursor : null;
} while (cursor);
assert.equal(new Set(matches.map(item => item.actor)).size, 1);
let evidence;
if (process.argv.includes('--resume-preconsensus-rejection')) {
  evidence = JSON.parse(await readFile(process.env.OASIS_SUI_RECONCILE_EVIDENCE, 'utf8'));
  assert.equal(evidence.chainId, fixture.chainId); assert.equal(evidence.owner, owner);
  assert.equal(evidence.targets.length, 3); assert.equal(evidence.burned.length, 1);
  const remaining = evidence.targets.filter(target => !evidence.burned.some(burn => burn.objectId === target.objectId));
  assert.deepEqual(matches.map(item => item.objectId).sort(), remaining.map(item => item.objectId).sort());
  for (const target of matches) assert.deepEqual(target, remaining.find(item => item.objectId === target.objectId));
  for (const burn of evidence.burned) {
    await client.waitForTransaction({ digest: burn.digest });
    const response = await client.getTransaction({ digest: burn.digest });
    assert.ok(response.Transaction?.status.success === true && response.Transaction.digest === burn.digest);
  }
} else {
  assert.equal(matches.length, 3, 'Refuse broad or ambiguous cleanup');
  evidence = { chainId: fixture.chainId, owner, targets: matches, burned: [] };
  await writeFile(process.env.OASIS_SUI_RECONCILE_EVIDENCE, JSON.stringify(evidence), { flag: 'wx' });
}
if (process.argv.includes('--burn')) {
  for (const target of matches) {
    const tx = new Transaction();
    tx.moveCall({ target: `${fixture.packageAddress}::nft::burn`, arguments: [tx.object(target.objectId)] });
    const response = await client.signAndExecuteTransaction({ transaction: tx, signer: key, include: { effects: true } });
    assert.ok(response.Transaction?.status.success === true, 'Burn outcome unknown; reconcile evidence before retrying');
    await client.waitForTransaction({ digest: response.Transaction.digest });
    evidence.burned.push({ objectId: target.objectId, digest: response.Transaction.digest });
    await writeFile(process.env.OASIS_SUI_RECONCILE_EVIDENCE, JSON.stringify(evidence));
  }
}
console.log(JSON.stringify({ targets: matches.length, burned: evidence.burned.length, originalDigests: matches.map(item => item.originalDigest) }));
