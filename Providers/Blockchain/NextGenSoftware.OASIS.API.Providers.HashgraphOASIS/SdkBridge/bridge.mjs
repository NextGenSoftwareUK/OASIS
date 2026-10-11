import { Client, PrivateKey, AccountId, TransactionId, TransferTransaction, TransactionReceiptQuery, TokenMintTransaction, TokenId, Hbar, TokenInfoQuery } from '@hiero-ledger/sdk';
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

export async function execute(action, request) {
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
  if (!['transfer', 'tokenTransfer', 'tokenMint'].includes(action)) throw new Error('Unknown Hedera SDK operation.');
  if (action !== 'transfer') TokenId.fromString(request.tokenId);
  if (action === 'transfer' && (!/^[1-9][0-9]*$/.test(request.tinybars ?? '') || BigInt(request.tinybars) > 9223372036854775807n))
    throw new Error('Transfer requires positive exact int64 tinybars.');
  if (Buffer.byteLength(request.memo ?? '', 'utf8') > 100)
    throw new Error('Hedera transaction memo exceeds 100 UTF-8 bytes.');
  const sender = AccountId.fromString(request.sender);
  const recipient = AccountId.fromString(request.recipient);
  if (action !== 'tokenMint' && sender.toString() === recipient.toString()) throw new Error('Sender and recipient must differ.');
  const key = restoreKey(request);
  if (request.publicKey && request.publicKey.toLowerCase() !== key.publicKey.toStringDer().toLowerCase())
    throw new Error('Signing key does not match the supplied public key.');
  const transactionId = request.transactionId ? TransactionId.fromString(request.transactionId) : TransactionId.generate(sender);
  if (transactionId.accountId.toString() !== sender.toString())
    throw new Error('Transaction ID payer does not match the sender.');
  const client = createClient(request);
  try {
    client.setOperator(sender, key);
    const transfer = (action === 'tokenMint' ? new TokenMintTransaction() : new TransferTransaction())
      .setTransactionId(transactionId).setTransactionMemo(request.memo ?? '');
    if (action === 'transfer') {
      transfer.addHbarTransfer(sender, Hbar.fromTinybars('-' + request.tinybars))
        .addHbarTransfer(recipient, Hbar.fromTinybars(request.tinybars));
    } else {
      const info = await new TokenInfoQuery().setTokenId(request.tokenId).execute(client);
      if (info.tokenType.toString() !== 'FUNGIBLE_COMMON') throw new Error('Fungible token operation requires a fungible HTS asset.');
      const units = tokenUnits(request.amount, info.decimals);
      if (action === 'tokenMint') {
        if (info.treasuryAccountId.toString() !== recipient.toString())
          throw new Error('HTS mint credits the actual treasury; the requested recipient must match it.');
        transfer.setTokenId(request.tokenId).setAmount(units);
      } else transfer.addTokenTransfer(request.tokenId, sender, -units).addTokenTransfer(request.tokenId, recipient, units);
    }
    transfer.freezeWith(client);
    const signed = await transfer.sign(key);
    const submitted = await signed.execute(client);
    const receipt = await submitted.getReceipt(client);
    if (receipt.status.toString() !== 'SUCCESS') throw new Error('Consensus receipt reports failure.');
    return { transactionId: submitted.transactionId.toString(), status: receipt.status.toString() };
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
