import { Client, PrivateKey, AccountId, TransactionId, TransferTransaction, TransactionReceiptQuery, TokenMintTransaction, TokenBurnTransaction, TokenId, Hbar, TokenInfoQuery, FileId, FileCreateTransaction, FileUpdateTransaction, FileDeleteTransaction, FileContentsQuery } from '@hiero-ledger/sdk';
import { pathToFileURL } from 'node:url';

export function restoreKey(request) {
  if (typeof request.privateKey !== 'string' || !request.privateKey)
    throw new Error('A signing key is required.');
  switch (request.keyType) {
    case 'ed25519': return PrivateKey.fromStringED25519(request.privateKey);
    case 'ecdsa': return PrivateKey.fromStringECDSA(request.privateKey);
    case 'der':
      if (!/^30[0-9a-f]+$/i.test(request.privateKey) || request.privateKey.length <= 64)
        throw new Error('DER key encoding is required; raw keys need an explicit algorithm.');
      return PrivateKey.fromString(request.privateKey);
    default: throw new Error('Signing key type must be ed25519, ecdsa or der.');
  }
}

function createClient(request) {
  if (request.network === 'local') {
    const nodes = request.nodes;
    if (!nodes || !Object.keys(nodes).length || Object.keys(nodes).some(endpoint => !/^127\.0\.0\.1:[0-9]+$/.test(endpoint)))
      throw new Error('Local consensus nodes must use explicitly configured loopback endpoints.');
    for (const id of Object.values(nodes)) AccountId.fromString(id);
    return Client.forNetwork(nodes);
  }
  if (!['mainnet', 'testnet', 'previewnet'].includes(request.network))
    throw new Error('An explicit supported Hedera network is required.');
  return Client.forName(request.network);
}

export function tokenUnits(amount, decimals) {
  if (!Number.isInteger(decimals) || decimals < 0 || decimals > 255 || !/^[0-9]+(?:\.[0-9]+)?$/.test(amount ?? ''))
    throw new Error('Exact positive decimal token amount and ledger decimals are required.');
  const [whole, fractional = ''] = amount.split('.');
  if (fractional.length > decimals && /[1-9]/.test(fractional.slice(decimals)))
    throw new Error('Token amount exceeds ledger decimal precision.');
  const units = BigInt(whole + fractional.slice(0, decimals).padEnd(decimals, '0'));
  if (units <= 0n || units > 9223372036854775807n) throw new Error('Token units must fit positive int64.');
  return units;
}

export function nftTransaction(action, request) {
  const token = TokenId.fromString(request.tokenId);
  if (action === 'nftMint') {
    if (!Array.isArray(request.metadata) || !request.metadata.length)
      throw new Error('NFT mint requires explicit metadata for each serial.');
    const metadata = request.metadata.map(value => {
      if (typeof value !== 'string' || !value || Buffer.byteLength(value, 'utf8') > 100)
        throw new Error('NFT metadata must contain 1 to 100 UTF-8 bytes per serial.');
      return Buffer.from(value, 'utf8');
    });
    return new TokenMintTransaction().setTokenId(token).setMetadata(metadata);
  }
  if (!['nftTransfer', 'nftBurn'].includes(action)) throw new Error('Unknown NFT operation.');
  if (!Array.isArray(request.serials) || !request.serials.length
      || request.serials.some(value => typeof value !== 'string' || !/^[1-9][0-9]*$/.test(value)
        || BigInt(value) > 9223372036854775807n)
      || new Set(request.serials).size !== request.serials.length)
    throw new Error('Unique positive exact int64 NFT serial strings are required.');
  if (action === 'nftBurn') return new TokenBurnTransaction().setTokenId(token).setSerials(request.serials);
  const transfer = new TransferTransaction();
  for (const serial of request.serials) transfer.addNftTransfer(token, serial, request.sender, request.recipient);
  return transfer;
}

export function fileTransaction(action, request, publicKey) {
  if (!['fileCreate', 'fileUpdate', 'fileDelete'].includes(action)) throw new Error('Unknown file mutation.');
  if (action !== 'fileCreate') FileId.fromString(request.fileId);
  if (action !== 'fileDelete' && (typeof request.contents !== 'string' || Buffer.byteLength(request.contents, 'utf8') > 2048))
    throw new Error('File mutations require explicit UTF-8 contents of at most 2048 bytes.');
  if (action === 'fileCreate') return new FileCreateTransaction().setKeys([publicKey]).setContents(Buffer.from(request.contents, 'utf8'));
  if (action === 'fileUpdate') return new FileUpdateTransaction().setFileId(request.fileId).setContents(Buffer.from(request.contents, 'utf8'));
  return new FileDeleteTransaction().setFileId(request.fileId);
}

async function executeFile(action, request) {
  const key = restoreKey(request);
  if (request.publicKey && request.publicKey.toLowerCase() !== key.publicKey.toStringDer().toLowerCase())
    throw new Error('File signing key does not match the supplied public key.');
  const payer = AccountId.fromString(request.sender);
  if (action === 'fileRead') FileId.fromString(request.fileId);
  const mutation = action === 'fileRead' ? null : fileTransaction(action, request, key.publicKey);
  const id = mutation ? TransactionId.fromString(request.transactionId) : null;
  if (id && id.accountId.toString() !== payer.toString()) throw new Error('File transaction payer mismatch.');
  if (!/^[1-9][0-9]*$/.test(request.maxFeeTinybars ?? '') || BigInt(request.maxFeeTinybars) > 9223372036854775807n)
    throw new Error('An explicit positive int64 fee ceiling is required.');
  const client = createClient(request);
  try {
    client.setOperator(payer, key);
    const ceiling = Hbar.fromTinybars(request.maxFeeTinybars);
    if (!mutation) {
      const bytes = await new FileContentsQuery().setFileId(request.fileId).setMaxQueryPayment(ceiling).execute(client);
      return { fileId: FileId.fromString(request.fileId).toString(), contents: new TextDecoder('utf-8', { fatal: true }).decode(bytes) };
    }
    mutation.setTransactionId(id).setMaxTransactionFee(ceiling).setMaxAttempts(1).setRegenerateTransactionId(false).freezeWith(client);
    const submitted = await (await mutation.sign(key)).execute(client);
    const receipt = await submitted.getReceipt(client);
    if (receipt.status.toString() !== 'SUCCESS') throw new Error('File mutation failed consensus.');
    return { transactionId: submitted.transactionId.toString(), status: 'SUCCESS',
      fileId: action === 'fileCreate' ? receipt.fileId.toString() : FileId.fromString(request.fileId).toString() };
  } catch (cause) {
    const error = new Error('Hedera file operation failed.');
    error.transactionId = id?.toString();
    error.status = cause.status?.toString();
    throw error;
  } finally { client.close(); }
}

export async function execute(action, request) {
  if (['fileCreate', 'fileUpdate', 'fileDelete', 'fileRead'].includes(action)) return executeFile(action, request);
  if (action === 'generateKey') {
    const key = request.keyType === 'ed25519' ? PrivateKey.generateED25519()
      : request.keyType === 'ecdsa' ? PrivateKey.generateECDSA() : null;
    if (!key) throw new Error('Key generation requires an explicit algorithm.');
    // A generated key is not a ledger account; never fabricate an account ID.
    return { privateKey: key.toStringDer(), publicKey: key.publicKey.toStringDer() };
  }
  if (action === 'restoreKey') {
    const key = restoreKey(request);
    return { privateKey: key.toStringDer(), publicKey: key.publicKey.toStringDer() };
  }
  if (action === 'receipt') {
    const id = TransactionId.fromString(request.transactionId);
    const client = createClient(request);
    try {
      const receipt = await new TransactionReceiptQuery().setTransactionId(id).setValidateStatus(false).execute(client);
      return { transactionId: id.toString(), status: receipt.status.toString() };
    } finally { client.close(); }
  }
  const nft = ['nftTransfer', 'nftMint', 'nftBurn'].includes(action);
  if (!nft && !['transfer', 'tokenTransfer', 'tokenMint', 'tokenBurn'].includes(action)) throw new Error('Unknown Hedera SDK operation.');
  if (action !== 'transfer') TokenId.fromString(request.tokenId);
  // Validate NFT payloads before opening a consensus client or paying for a query.
  const nftOperation = nft ? nftTransaction(action, request) : null;
  if (action === 'transfer' && (!/^[1-9][0-9]*$/.test(request.tinybars ?? '') || BigInt(request.tinybars) > 9223372036854775807n))
    throw new Error('Transfer requires positive exact int64 tinybars.');
  if (Buffer.byteLength(request.memo ?? '', 'utf8') > 100)
    throw new Error('Hedera transaction memo exceeds 100 UTF-8 bytes.');
  const sender = AccountId.fromString(request.sender);
  const recipient = AccountId.fromString(request.recipient);
  if (['transfer', 'tokenTransfer', 'nftTransfer'].includes(action) && sender.toString() === recipient.toString()) throw new Error('Sender and recipient must differ.');
  const key = restoreKey(request);
  if (request.publicKey && request.publicKey.toLowerCase() !== key.publicKey.toStringDer().toLowerCase())
    throw new Error('Signing key does not match the supplied public key.');
  const transactionId = request.transactionId ? TransactionId.fromString(request.transactionId) : TransactionId.generate(sender);
  if (transactionId.accountId.toString() !== sender.toString())
    throw new Error('Transaction ID payer does not match the sender.');
  const client = createClient(request);
  try {
    client.setOperator(sender, key);
    const transfer = (nftOperation ?? (action === 'tokenMint' ? new TokenMintTransaction()
      : action === 'tokenBurn' ? new TokenBurnTransaction() : new TransferTransaction()))
      .setTransactionId(transactionId).setTransactionMemo(request.memo ?? '');
    if (action === 'transfer') {
      transfer.addHbarTransfer(sender, Hbar.fromTinybars('-' + request.tinybars))
        .addHbarTransfer(recipient, Hbar.fromTinybars(request.tinybars));
    } else {
      const info = await new TokenInfoQuery().setTokenId(request.tokenId).execute(client);
      if (info.tokenType.toString() !== (nft ? 'NON_FUNGIBLE_UNIQUE' : 'FUNGIBLE_COMMON'))
        throw new Error('HTS token type does not match the requested operation.');
      if (['nftMint', 'nftBurn'].includes(action)) {
        if (info.treasuryAccountId.toString() !== recipient.toString())
          throw new Error('HTS NFT mint/burn affects the actual treasury; the requested account must match it.');
      } else if (!nft) {
        const units = tokenUnits(request.amount, info.decimals);
        if (action === 'tokenMint' || action === 'tokenBurn') {
          if (info.treasuryAccountId.toString() !== recipient.toString())
            throw new Error('HTS mint/burn affects the actual treasury; the requested account must match it.');
          transfer.setTokenId(request.tokenId).setAmount(units);
        } else transfer.addTokenTransfer(request.tokenId, sender, -units).addTokenTransfer(request.tokenId, recipient, units);
      }
    }
    transfer.freezeWith(client);
    const signed = await transfer.sign(key);
    const submitted = await signed.execute(client);
    const receipt = await submitted.getReceipt(client);
    if (receipt.status.toString() !== 'SUCCESS') throw new Error('Consensus receipt reports failure.');
    return { transactionId: submitted.transactionId.toString(), status: receipt.status.toString(),
      ...(action === 'nftMint' ? { serials: receipt.serials.map(serial => serial.toString()) } : {}) };
  } catch (cause) {
    const error = new Error('Hedera transfer did not establish consensus success. Track the original transaction before retrying.');
    error.transactionId = transactionId.toString();
    error.status = cause.status?.toString();
    throw error;
  } finally { client.close(); }
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  try {
    let input = '';
    for await (const chunk of process.stdin) {
      input += chunk;
      if (Buffer.byteLength(input, 'utf8') > 1048576) throw new Error('Request exceeds bridge input limit.');
    }
    const envelope = JSON.parse(input);
    process.stdout.write(JSON.stringify(await execute(envelope.action, envelope.request)));
  } catch (error) {
    // Do not expose SDK exception text or stacks which could contain key input.
    process.stderr.write(JSON.stringify({ error: 'Hedera SDK operation failed',
      transactionId: error.transactionId, status: error.status }));
    process.exitCode = 1;
  }
}
