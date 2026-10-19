import { ApiPromise } from '@polkadot/api';
import { readFileSync } from 'node:fs';
import { prepareNativeTransfer, validateTransferRequest } from './native-transfer.mjs';
import { ReadOnlyHttpProvider } from './read-only-provider.mjs';
import { verifyNativePreparation } from './verify-preparation.mjs';
import { pathToFileURL } from 'node:url';
import { readFinalizedReceipt, validateReceiptRequest } from './finalized-receipt.mjs';
import { originalSubmission, submitOriginalPreparation } from './original-submission.mjs';
import { readOriginalOutcome, readOriginalNativeFee, validateOutcomeRange } from './original-outcome.mjs';
import { prepareAssetOperation, verifyAssetPreparation, validateAssetRequest } from './asset-operation.mjs';
import { readOriginalAssetOutcome, validateAssetOutcomeRequest } from './asset-outcome.mjs';
import { readWalletHistory, validateHistoryRequest } from './wallet-history.mjs';
import { generateKeyPair, validateGenerationRequest, createAccount, restoreAccount, validateRestorationMaterial } from './key-generation.mjs';
import { readTransactionStatus, validateStatusRequest } from './transaction-status.mjs';

// Metadata/preparation transport is read-only. Only explicitly owned submission gains a one-shot capability.
let api;
let request;
let stage = 'request validation';
try {
  try { request = JSON.parse(readFileSync(0, 'utf8')); }
  catch { throw new Error('SDK request must be valid JSON.'); }
  if (!['balance', 'prepareTransfer', 'verifyNativePreparation', 'persistNativePreparation', 'submitNativePreparation', 'finalizedReceipt', 'originalOutcome', 'originalNativeFee', 'prepareAssetOperation', 'verifyAssetPreparation', 'submitAssetPreparation', 'assetOutcome', 'walletHistory', 'generateKeyPair', 'createAccount', 'restoreAccount', 'transactionStatus'].includes(request.operation)) throw new Error('Unknown Polkadot SDK operation.');
  if (['generateKeyPair', 'createAccount'].includes(request.operation)) validateGenerationRequest(request);
  if (request.operation === 'restoreAccount') await validateRestorationMaterial(request.recoveryMaterial);
  if (request.operation === 'transactionStatus') validateStatusRequest(request);
  if (request.operation === 'walletHistory') validateHistoryRequest(request);
  if (request.operation === 'assetOutcome') validateAssetOutcomeRequest(request);
  if (request.operation === 'originalOutcome') validateOutcomeRange(request);
  if (request.operation === 'finalizedReceipt') validateReceiptRequest(request);
  if (['persistNativePreparation', 'submitNativePreparation', 'submitAssetPreparation'].includes(request.operation) && (!process.env.OASIS_NATIVE_PREPARATION_PIPE || !process.env.OASIS_NATIVE_PREPARATION_MODULE))
    throw new Error('Owned persistence transport is required; no preparation acknowledgement can be invented.');
  const endpoint = new URL(request.endpoint);
  if (!['http:', 'https:'].includes(endpoint.protocol) || endpoint.username || endpoint.password)
    throw new Error('An HTTP RPC endpoint without embedded credentials is required.');
  if (!/^0x[0-9a-f]{64}$/i.test(request.genesisHash)) throw new Error('A pinned genesis hash is required.');
  if (request.operation === 'balance' && (typeof request.address !== 'string' || !request.address.trim()))
    throw new Error('An account address is required.');
  if (request.operation === 'prepareTransfer') await validateTransferRequest(request);
  if (request.operation === 'prepareAssetOperation') await validateAssetRequest(request);
  stage = 'runtime metadata initialization';
  api = await ApiPromise.create({ provider: new ReadOnlyHttpProvider(endpoint.href), noInitWarn: true });
  stage = 'network verification';
  if (api.genesisHash.toHex().toLowerCase() !== request.genesisHash.toLowerCase())
    throw new Error('Polkadot SDK genesis hash differs from the activated network.');
  let decimals;
  if (!['verifyNativePreparation', 'persistNativePreparation', 'submitNativePreparation', 'finalizedReceipt', 'originalOutcome', 'originalNativeFee', 'prepareAssetOperation', 'verifyAssetPreparation', 'submitAssetPreparation', 'assetOutcome', 'generateKeyPair', 'createAccount', 'restoreAccount', 'transactionStatus'].includes(request.operation)) {
  stage = 'native precision query';
  const properties = await api.rpc.system.properties();
  if (properties.tokenDecimals.isNone || properties.tokenDecimals.unwrap().length !== 1)
    throw new Error('Exactly one authoritative native-token precision is required.');
  decimals = properties.tokenDecimals.unwrap()[0].toNumber();
  if (!Number.isInteger(decimals) || decimals < 0 || decimals > 38)
    throw new Error('Native-token precision is outside the supported range.');
  }
  if (request.operation === 'transactionStatus') {
    stage = 'finalized transaction status';
    process.stdout.write(JSON.stringify({ ok: true, result: await readTransactionStatus(api, request) }));
  } else if (request.operation === 'createAccount' || request.operation === 'restoreAccount') {
    stage = 'sr25519 account material';
    process.stdout.write(JSON.stringify({ ok: true, result: request.operation === 'createAccount'
      ? await createAccount(api) : await restoreAccount(api, request.recoveryMaterial) }));
  } else if (request.operation === 'generateKeyPair') {
    stage = 'sr25519 key generation';
    process.stdout.write(JSON.stringify({ ok: true, result: await generateKeyPair(api) }));
  } else if (request.operation === 'walletHistory') {
    stage = 'finalized wallet history';
    process.stdout.write(JSON.stringify({ ok: true, result: await readWalletHistory(api, request, decimals) }));
  } else if (request.operation === 'assetOutcome') {
    stage = 'original asset finalized effect verification';
    process.stdout.write(JSON.stringify({ ok: true, result: await readOriginalAssetOutcome(api, request) }));
  } else if (request.operation === 'prepareAssetOperation') {
    stage = 'asset operation signing and fee estimation';
    process.stdout.write(JSON.stringify({ ok: true, result: await prepareAssetOperation(api, request) }));
  } else if (request.operation === 'verifyAssetPreparation') {
    stage = 'original signed asset intent verification';
    process.stdout.write(JSON.stringify({ ok: true, result: await verifyAssetPreparation(api, request.preparation) }));
  } else if (request.operation === 'originalNativeFee') {
    stage = 'verified original finalized native fee lookup';
    process.stdout.write(JSON.stringify({ ok: true, result: await readOriginalNativeFee(api, request.preparation) }));
  } else if (request.operation === 'originalOutcome') {
    stage = 'original signed finalized outcome recovery';
    process.stdout.write(JSON.stringify({ ok: true, result: await readOriginalOutcome(api, request) }));
  } else if (request.operation === 'finalizedReceipt') {
    stage = 'finalized canonical dispatch and native fee receipt';
    process.stdout.write(JSON.stringify({ ok: true, result: await readFinalizedReceipt(api, request) }));
  } else if (['submitNativePreparation', 'submitAssetPreparation'].includes(request.operation)) {
    stage = 'verified original persistence and one-shot submission';
    const { preparationClient } = await import(pathToFileURL(process.env.OASIS_NATIVE_PREPARATION_MODULE).href);
    const persist = preparationClient(process.env.OASIS_NATIVE_PREPARATION_PIPE);
    const result = await submitOriginalPreparation(api, request.preparation, persist,
      originalSubmission(endpoint.href, request.preparation.signedTransactionBase64),
      request.operation === 'submitAssetPreparation' ? verifyAssetPreparation : verifyNativePreparation);
    process.stdout.write(JSON.stringify({ ok: true, result }));
  } else if (request.operation === 'persistNativePreparation') {
    stage = 'verified original persistence acknowledgement';
    const identity = await verifyNativePreparation(api, request.preparation);
    const { preparationClient } = await import(pathToFileURL(process.env.OASIS_NATIVE_PREPARATION_MODULE).href);
    const persist = preparationClient(process.env.OASIS_NATIVE_PREPARATION_PIPE);
    await persist(request.preparation);
    process.stdout.write(JSON.stringify({ ok: true, result: { transactionHash: identity.transactionHash, persisted: true, submitted: false } }));
  } else if (request.operation === 'verifyNativePreparation') {
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
  for (const secret of [request?.privateKey, request?.mnemonic, request?.recoveryMaterial])
    if (typeof secret === 'string' && secret) reason = reason.replaceAll(secret, '[signing secret]');
  reason = reason.replace(/(?:0x)?[0-9a-f]{64,}/gi, '[binary identity]');
  process.stdout.write(JSON.stringify({ ok: false, error: `Polkadot SDK operation failed during ${stage} (${kind}): ${reason}` }));
  process.exitCode = 1;
} finally {
  if (api) await api.disconnect();
}
