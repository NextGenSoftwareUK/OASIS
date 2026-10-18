import { ApiPromise } from '@polkadot/api';
import { readFileSync } from 'node:fs';
import { prepareNativeTransfer, validateTransferRequest } from './native-transfer.mjs';
import { ReadOnlyHttpProvider } from './read-only-provider.mjs';
import { verifyNativePreparation } from './verify-preparation.mjs';

// Network operations are read-only. Preparation can sign but NEVER submits an extrinsic.
let api;
let request;
let stage = 'request validation';
try {
  try { request = JSON.parse(readFileSync(0, 'utf8')); }
  catch { throw new Error('SDK request must be valid JSON.'); }
  if (!['balance', 'prepareTransfer', 'verifyNativePreparation'].includes(request.operation)) throw new Error('Unknown Polkadot SDK operation.');
  const endpoint = new URL(request.endpoint);
  if (!['http:', 'https:'].includes(endpoint.protocol) || endpoint.username || endpoint.password)
    throw new Error('An HTTP RPC endpoint without embedded credentials is required.');
  if (!/^0x[0-9a-f]{64}$/i.test(request.genesisHash)) throw new Error('A pinned genesis hash is required.');
  if (request.operation === 'balance' && (typeof request.address !== 'string' || !request.address.trim()))
    throw new Error('An account address is required.');
  if (request.operation === 'prepareTransfer') await validateTransferRequest(request);
  stage = 'runtime metadata initialization';
  api = await ApiPromise.create({ provider: new ReadOnlyHttpProvider(endpoint.href), noInitWarn: true });
  stage = 'network verification';
  if (api.genesisHash.toHex().toLowerCase() !== request.genesisHash.toLowerCase())
    throw new Error('Polkadot SDK genesis hash differs from the activated network.');
  let decimals;
  if (request.operation !== 'verifyNativePreparation') {
  stage = 'native precision query';
  const properties = await api.rpc.system.properties();
  if (properties.tokenDecimals.isNone || properties.tokenDecimals.unwrap().length !== 1)
    throw new Error('Exactly one authoritative native-token precision is required.');
  decimals = properties.tokenDecimals.unwrap()[0].toNumber();
  if (!Number.isInteger(decimals) || decimals < 0 || decimals > 38)
    throw new Error('Native-token precision is outside the supported range.');
  }
  if (request.operation === 'verifyNativePreparation') {
    stage = 'original signed identity verification';
    process.stdout.write(JSON.stringify({ ok: true, result: await verifyNativePreparation(api, request.preparation) }));
  } else if (request.operation === 'prepareTransfer') {
    stage = 'native transfer signing and fee estimation';
    const result = await prepareNativeTransfer(api, request, decimals);
    process.stdout.write(JSON.stringify({ ok: true, result }));
  } else {
  stage = 'address validation';
  const accountId = api.createType('AccountId32', request.address);
  stage = 'finalized storage query';
  const blockHash = await api.rpc.chain.getFinalizedHead();
  const at = await api.at(blockHash);
  if (!at.query.system?.account) throw new Error('Runtime has no System.Account storage.');
  const account = await at.query.system.account(accountId);
  if (!account.data?.free) throw new Error('Runtime System.Account has no free balance.');
  const units = account.data.free.toString();
  if (!/^(0|[1-9][0-9]*)$/.test(units) || BigInt(units) >= (1n << 128n))
    throw new Error('Runtime returned an invalid native u128 balance.');
  process.stdout.write(JSON.stringify({ ok: true, result: { units, decimals, blockHash: blockHash.toHex() } }));
  }
} catch (error) {
  // Preserve failure stage/type/reason without echoing endpoint URLs or request content.
  const kind = error instanceof Error ? error.name.replace(/[^A-Za-z0-9]/g, '').slice(0, 64) : 'UnknownError';
  let reason = error instanceof Error ? error.message.replace(/https?:\/\/[^\s"'<>]+/gi, '[RPC endpoint]').slice(0, 512) : 'Unknown SDK failure';
  for (const secret of [request?.privateKey, request?.mnemonic])
    if (typeof secret === 'string' && secret) reason = reason.replaceAll(secret, '[signing secret]');
  reason = reason.replace(/(?:0x)?[0-9a-f]{64,}/gi, '[binary identity]');
  process.stdout.write(JSON.stringify({ ok: false, error: `Polkadot SDK operation failed during ${stage} (${kind}): ${reason}` }));
  process.exitCode = 1;
} finally {
  if (api) await api.disconnect();
}
