import { Keyring } from '@polkadot/api';
import { cryptoWaitReady, mnemonicValidate } from '@polkadot/util-crypto';
import { signingDocument, verifyNativePreparation } from './verify-preparation.mjs';

export function nativeUnits(amount, decimals) {
  if (!Number.isInteger(decimals) || decimals < 0 || decimals > 38)
    throw new Error('Native precision is outside u128-supported bounds.');
  if (typeof amount !== 'string' || amount.length > 100 || !/^(0|[1-9][0-9]*)(\.[0-9]+)?$/.test(amount))
    throw new Error('A positive invariant decimal amount is required.');
  const [whole, fraction = ''] = amount.split('.');
  if (fraction.slice(decimals).replaceAll('0', '') !== '')
    throw new Error('Transfer amount is not exact in native base units.');
  const units = BigInt(whole + fraction.slice(0, decimals).padEnd(decimals, '0'));
  if (units <= 0n || units >= (1n << 128n)) throw new Error('Transfer amount must fit positive u128 base units.');
  return units.toString();
}

export async function validateTransferRequest(request) {
  if (request.tokenAddress || request.tokenId) throw new Error('This preparation supports the native Balances pallet, not asset tokens.');
  if (typeof request.sender !== 'string' || !request.sender.trim() || typeof request.recipient !== 'string' || !request.recipient.trim())
    throw new Error('Explicit sender and recipient addresses are required.');
  const privateKey = request.privateKey || '';
  const mnemonic = request.mnemonic || '';
  if (Boolean(privateKey) === Boolean(mnemonic)) throw new Error('Supply exactly one explicit private seed or mnemonic.');
  if (privateKey && !/^0x[0-9a-f]{64}$/i.test(privateKey)) throw new Error('Private seed must be exactly32bytes in0xhex.');
  if (mnemonic) {
    await cryptoWaitReady();
    if (!mnemonicValidate(mnemonic)) throw new Error('A valid BIP39 mnemonic is required.');
  }
  if (request.memo != null && (typeof request.memo !== 'string' || Buffer.byteLength(request.memo, 'utf8') > 256))
    throw new Error('Memo must be UTF8 text of at most256bytes.');
  // Precision is obtained from the actual network later, but reject non-decimal/non-positive input now.
  if (typeof request.amount !== 'string' || request.amount.length > 100
      || !/^(0|[1-9][0-9]*)(\.[0-9]+)?$/.test(request.amount) || !/[1-9]/.test(request.amount))
    throw new Error('A positive invariant decimal amount is required.');
}

export async function prepareNativeTransfer(api, request, decimals) {
  await validateTransferRequest(request);
  await cryptoWaitReady();
  const keyring = new Keyring({ type: 'sr25519' });
  const seed = request.privateKey ? Uint8Array.from(Buffer.from(request.privateKey.slice(2), 'hex')) : null;
  let pair;
  try {
    pair = seed ? keyring.addFromSeed(seed) : keyring.addFromMnemonic(request.mnemonic);
    const sender = api.createType('AccountId32', request.sender).toHex();
    const publicKey = '0x' + Buffer.from(pair.publicKey).toString('hex');
    if (sender.toLowerCase() !== publicKey.toLowerCase()
        || (request.publicKey && api.createType('AccountId32', request.publicKey).toHex().toLowerCase() !== publicKey.toLowerCase()))
      throw new Error('Explicit signer does not match the requested sender/public key.');
    const recipient = api.createType('AccountId32', request.recipient).toHex();
    const units = nativeUnits(request.amount, decimals);
    if (!api.tx.balances?.transferKeepAlive) throw new Error('Runtime has no Balances.transferKeepAlive call.');
    let tx = api.tx.balances.transferKeepAlive(recipient, units);
    if (request.memo) {
      if (!api.tx.utility?.batchAll || !api.tx.system?.remarkWithEvent)
        throw new Error('Runtime cannot atomically include the requested transfer memo.');
      tx = api.tx.utility.batchAll([tx, api.tx.system.remarkWithEvent('0x' + Buffer.from(request.memo, 'utf8').toString('hex'))]);
    }
    let payloadJson, signature;
    await tx.signAsync(pair.address, { tip: 0, signer: { signPayload: async payload => {
      payloadJson = structuredClone(payload);
      const encoded = api.registry.createType('ExtrinsicPayload', payload, { version: payload.version });
      signature = encoded.sign(pair).signature;
      return { id: 0, signature };
    } } });
    const bytes = tx.toU8a();
    const prepared = { protocol: 'oasis.polkadot.native-transfer.preparation.v2', submitted: false,
      chainId: api.genesisHash.toHex(), genesisHash: api.genesisHash.toHex(), sender, recipient, units, decimals,
      transactionHash: tx.hash.toHex(), signedTransactionBase64: Buffer.from(bytes).toString('base64'),
      signDocBase64: signingDocument(payloadJson), signature };
    await verifyNativePreparation(api, prepared);
    // Read-only runtime fee estimate against these exact signed bytes, NOT an all-fees cap or receipt.
    if (!api.call.transactionPaymentApi?.queryInfo) throw new Error('Runtime has no transaction fee estimation API.');
    const info = await api.call.transactionPaymentApi.queryInfo(bytes, bytes.length);
    const partialFeeUnits = info.partialFee.toString();
    if (!/^(0|[1-9][0-9]*)$/.test(partialFeeUnits)) throw new Error('Runtime fee estimate is malformed.');
    return { ...prepared, partialFeeUnits, feeCoverage: 'estimate-only-not-final-or-capped' };
  } finally {
    seed?.fill(0);
    pair?.lock();
  }
}
