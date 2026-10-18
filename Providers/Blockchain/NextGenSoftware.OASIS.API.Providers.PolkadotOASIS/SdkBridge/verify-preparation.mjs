import { blake2AsU8a, cryptoWaitReady, signatureVerify } from '@polkadot/util-crypto';

export function canonicalBase64(value, maximum = 1048576) {
  if (typeof value !== 'string' || !value || value.length > Math.ceil(maximum / 3) * 4)
    throw new Error('Original signed evidence must be bounded nonempty base64.');
  const bytes = Buffer.from(value, 'base64');
  if (!bytes.length || bytes.length > maximum || bytes.toString('base64') !== value)
    throw new Error('Original signed evidence must use canonical base64.');
  return bytes;
}

export function signingDocument(signingPayload) {
  return Buffer.from(JSON.stringify({ protocol: 'oasis.polkadot.signing-document.v1', signingPayload }), 'utf8').toString('base64');
}

export async function verifyNativePreparation(api, preparation) {
  if (!preparation || typeof preparation !== 'object') throw new Error('Original preparation is required.');
  const genesis = api.genesisHash.toHex();
  if (preparation.chainId !== genesis || (preparation.genesisHash != null && preparation.genesisHash !== genesis))
    throw new Error('Prepared chain identity differs from the verified network.');
  if (preparation.protocol != null && preparation.protocol !== 'oasis.polkadot.native-transfer.preparation.v2')
    throw new Error('Unsupported preparation protocol; no legacy format guessing.');
  const bytes = canonicalBase64(preparation.signedTransactionBase64);
  const documentBytes = canonicalBase64(preparation.signDocBase64);
  const text = new TextDecoder('utf-8', { fatal: true }).decode(documentBytes);
  let document;
  try { document = JSON.parse(text); } catch { throw new Error('Signing document is not valid JSON.'); }
  if (document.protocol !== 'oasis.polkadot.signing-document.v1' || JSON.stringify(document) !== text
      || Object.keys(document).length !== 2 || !document.signingPayload)
    throw new Error('Signing document is not the canonical versioned envelope.');
  const payload = document.signingPayload;
  if (payload.version !== 4 || payload.genesisHash !== genesis
      || BigInt(payload.specVersion) !== BigInt(api.runtimeVersion.specVersion.toString())
      || BigInt(payload.transactionVersion) !== BigInt(api.runtimeVersion.transactionVersion.toString())
      || JSON.stringify(payload.signedExtensions) !== JSON.stringify(api.registry.signedExtensions))
    throw new Error('Original runtime/signing context differs; historical metadata verification is not implemented.');
  const decoded = api.registry.createType('Extrinsic', bytes);
  if (!decoded.isSigned || decoded.type !== 4 || !Buffer.from(decoded.toU8a()).equals(bytes)
      || decoded.hash.toHex() !== preparation.transactionHash)
    throw new Error('Prepared signed SCALE bytes do not bind the original transaction hash.');
  const sender = api.createType('AccountId32', decoded.signer.toString()).toHex();
  if (preparation.sender !== sender || api.createType('AccountId32', payload.address).toHex() !== sender)
    throw new Error('Prepared sender does not match the signed account.');
  const signature = preparation.signature;
  if (typeof signature !== 'string' || !/^0x01[0-9a-f]{128}$/i.test(signature)
      || decoded.signature.toHex().toLowerCase() !== ('0x' + signature.slice(4)).toLowerCase())
    throw new Error('Original sr25519 signature differs from the signed extrinsic.');
  if (payload.method !== decoded.method.toHex()) throw new Error('Signing document call differs from signed call.');
  await cryptoWaitReady();
  const raw = api.registry.createType('ExtrinsicPayload', payload, { version: payload.version }).toU8a({ method: true });
  const message = raw.length > 256 ? blake2AsU8a(raw) : raw;
  if (!signatureVerify(message, signature, sender).isValid) throw new Error('Original signature failed verification.');
  const reconstructed = api.tx(decoded.method).addSignature(sender, signature, payload);
  if (!Buffer.from(reconstructed.toU8a()).equals(bytes))
    throw new Error('Signed extrinsic extensions do not match the verified signing document.');
  let transfer = decoded.method, memo = null;
  if (transfer.section === 'utility' && transfer.method === 'batchAll') {
    const calls = transfer.args[0];
    if (calls.length !== 2 || calls[1].section !== 'system' || calls[1].method !== 'remarkWithEvent')
      throw new Error('Only an atomic native transfer plus memo is authorized by this verifier.');
    const memoBytes = calls[1].args[0].toU8a(true);
    if (memoBytes.length > 256) throw new Error('Signed memo exceeds the supported limit.');
    memo = new TextDecoder('utf-8', { fatal: true }).decode(memoBytes);
    transfer = calls[0];
  }
  if (transfer.section !== 'balances' || transfer.method !== 'transferKeepAlive' || transfer.args.length !== 2)
    throw new Error('Signed call is not an authorized native keep-alive transfer.');
  const recipient = api.createType('AccountId32', transfer.args[0].toString()).toHex();
  const units = transfer.args[1].toString();
  if (!/^[1-9][0-9]*$/.test(units) || BigInt(units) >= (1n << 128n)
      || (preparation.recipient != null && preparation.recipient !== recipient)
      || (preparation.units != null && preparation.units !== units))
    throw new Error('Declared transfer intent differs from the signed recipient/units.');
  return { verified: true, transactionHash: decoded.hash.toHex(), chainId: genesis, sender, recipient, units, memo,
    feeCoverage: 'identity-only-not-a-fee-cap-or-receipt' };
}
