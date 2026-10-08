import { Ed25519Keypair } from '@mysten/sui/keypairs/ed25519';
import { SuiGrpcClient } from '@mysten/sui/grpc';
import { Transaction } from '@mysten/sui/transactions';
import { isValidSuiAddress, normalizeSuiAddress } from '@mysten/sui/utils';
import { bcs } from '@mysten/sui/bcs';
import { ObjectError } from '@mysten/sui/client';
import { fileURLToPath } from 'node:url';
import { resolve } from 'node:path';
import { generateMnemonic } from '@scure/bip39';
import { wordlist } from '@scure/bip39/wordlists/english.js';

function wallet(keypair) {
  return { privateKey: keypair.getSecretKey(), publicKey: keypair.getPublicKey().toBase64(), address: keypair.toSuiAddress() };
}

const Storage = bcs.struct('Storage', {
  id: bcs.Address, owner: bcs.Address,
  values: bcs.struct('Table', { id: bcs.Address, size: bcs.u64() }),
});
const NFT = bcs.struct('NFT', { id: bcs.Address, creator: bcs.Address, metadata: bcs.string() });

async function storageInfo(client, request) {
  if (!isValidSuiAddress(request.packageAddress) || !isValidSuiAddress(request.storageObjectId))
    throw new Error('Published OASIS package and shared storage object IDs are required');
  const { object } = await client.getObject({ objectId: request.storageObjectId, include: { content: true } });
  if (object.type !== `${normalizeSuiAddress(request.packageAddress)}::storage::Storage`)
    throw new Error('Storage object does not belong to the configured OASIS package ABI');
  if (!object.content) throw new Error('Sui returned no BCS storage content');
  return Storage.parse(object.content);
}

async function execute(client, tx, signer, include = {}) {
  tx.setSender(signer.toSuiAddress());
  const response = await client.signAndExecuteTransaction({ transaction: tx, signer, include: { effects: true, ...include } });
  if (response.FailedTransaction) throw new Error(JSON.stringify(response.FailedTransaction.status));
  if (!response.Transaction?.digest) throw new Error('Sui SDK returned no successful transaction digest');
  await client.waitForTransaction({ digest: response.Transaction.digest });
  return response.Transaction;
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
  if (request.operation === 'nftMint') {
    if (!isValidSuiAddress(request.packageAddress) || !isValidSuiAddress(request.recipient)
        || !Array.isArray(request.metadata) || request.metadata.some(value => typeof value !== 'string')
        || !Number.isSafeInteger(request.count) || request.count < 1 || request.metadata.length !== request.count)
      throw new Error('Published package, recipient, serialized metadata and positive integral mint count are required');
    const signer = Ed25519Keypair.fromSecretKey(request.privateKey);
    const tx = new Transaction();
    for (let i = 0; i < request.count; i++)
      tx.moveCall({ target: `${request.packageAddress}::nft::mint`, arguments: [tx.pure.string(request.metadata[i]), tx.pure.address(request.recipient)] });
    const committed = await execute(client, tx, signer, { objectTypes: true });
    const tokenIds = Object.entries(committed.objectTypes).filter(([, type]) => type === `${normalizeSuiAddress(request.packageAddress)}::nft::NFT`).map(([id]) => id);
    if (tokenIds.length !== request.count) throw new Error(`Mint ${committed.digest} committed but returned an unexpected NFT object count`);
    return { transactionHash: committed.digest, tokenIds };
  }
  if (['nftGet', 'nftSend', 'nftBurn'].includes(request.operation)) {
    if (!isValidSuiAddress(request.packageAddress) || !isValidSuiAddress(request.tokenId))
      throw new Error('Published package and NFT object ID are required');
    const { object } = await client.getObject({ objectId: request.tokenId, include: { content: true } });
    if (object.type !== `${normalizeSuiAddress(request.packageAddress)}::nft::NFT` || !object.content)
      throw new Error('NFT object does not match the configured package ABI');
    const nft = NFT.parse(object.content);
    if (request.operation === 'nftGet') return { ...nft, owner: object.owner };
    const signer = Ed25519Keypair.fromSecretKey(request.privateKey);
    if (request.fromWalletAddress && normalizeSuiAddress(request.fromWalletAddress) !== signer.toSuiAddress())
      throw new Error('Signing key does not own sender wallet');
    const tx = new Transaction();
    if (request.operation === 'nftBurn')
      tx.moveCall({ target: `${request.packageAddress}::nft::burn`, arguments: [tx.object(request.tokenId)] });
    else {
      if (!isValidSuiAddress(request.recipient)) throw new Error('Valid NFT recipient is required');
      tx.transferObjects([tx.object(request.tokenId)], tx.pure.address(request.recipient));
    }
    const committed = await execute(client, tx, signer);
    return { transactionHash: committed.digest };
  }
  if (request.operation === 'publish') {
    if (!Array.isArray(request.modules) || request.modules.length === 0 || !Array.isArray(request.dependencies))
      throw new Error('Compiled Move modules and dependencies are required');
    const signer = Ed25519Keypair.fromSecretKey(request.privateKey);
    const tx = new Transaction();
    const [cap] = tx.publish({ modules: request.modules, dependencies: request.dependencies });
    tx.transferObjects([cap], tx.pure.address(signer.toSuiAddress()));
    const committed = await execute(client, tx, signer, { objectTypes: true });
    const packageId = committed.effects.changedObjects.find(object => object.outputState === 'PackageWrite' && object.idOperation === 'Created')?.objectId;
    const storageId = Object.entries(committed.objectTypes).find(([, type]) => type === `${packageId}::storage::Storage`)?.[0];
    if (!packageId || !storageId) throw new Error(`Publication ${committed.digest} committed but no OASIS storage object was returned`);
    return { transactionHash: committed.digest, packageAddress: packageId, storageObjectId: storageId };
  }
  if (['storageProbe', 'get', 'put', 'delete', 'putMany', 'deleteMany', 'list'].includes(request.operation)) {
    const storage = await storageInfo(client, request);
    if (request.operation === 'storageProbe') return { owner: storage.owner, tableId: storage.values.id, chainIdentifier };
    if (request.operation === 'get') {
      if (typeof request.key !== 'string') throw new Error('Storage key is required');
      try {
        const { dynamicField } = await client.core.getDynamicField({ parentId: storage.values.id,
          name: { type: '0x1::string::String', bcs: bcs.string().serialize(request.key).toBytes() } });
        return bcs.string().parse(dynamicField.value.bcs);
      } catch (error) {
        if (error instanceof ObjectError && error.reason === 'notFound') return null;
        throw error;
      }
    }
    if (request.operation === 'list') {
      const keys = [];
      const seen = new Set();
      let cursor = null;
      do {
        const page = await client.listDynamicFields({ parentId: storage.values.id, limit: 100, cursor });
        for (const field of page.dynamicFields) {
          const key = bcs.string().parse(field.name.bcs);
          if (key.startsWith(request.prefix ?? '')) keys.push(key);
        }
        if (!page.hasNextPage) return keys.sort();
        if (!page.cursor || seen.has(page.cursor)) throw new Error('Sui returned a non-advancing storage cursor');
        seen.add(page.cursor);
        cursor = page.cursor;
      } while (true);
    }
    const signer = Ed25519Keypair.fromSecretKey(request.privateKey);
    const tx = new Transaction();
    const putting = request.operation === 'put' || request.operation === 'putMany';
    const entries = request.operation.endsWith('Many') ? request.entries : [{ key: request.key, value: request.value }];
    if (!Array.isArray(entries) || entries.length === 0) throw new Error('Nonempty storage mutations are required');
    for (const entry of entries) {
      if (typeof entry.key !== 'string' || (putting && typeof entry.value !== 'string'))
        throw new Error('Storage key and serialized value are required');
      const args = [tx.object(request.storageObjectId), tx.pure.string(entry.key)];
      if (putting) args.push(tx.pure.string(entry.value));
      tx.moveCall({ target: `${request.packageAddress}::storage::${putting ? 'put' : 'remove'}`, arguments: args });
    }
    const committed = await execute(client, tx, signer);
    return { transactionHash: committed.digest };
  }
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
    const committed = await execute(client, tx, signer);
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
