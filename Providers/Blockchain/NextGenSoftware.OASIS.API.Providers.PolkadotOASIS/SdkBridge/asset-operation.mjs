import { nativeUnits, validateExplicitSigner, withExplicitSigner, signOriginalCall, withOriginalFeeEstimate } from './native-transfer.mjs';
import { verifySignedPreparation } from './verify-preparation.mjs';

const protocol = 'oasis.polkadot.asset-operation.preparation.v1';
export async function validateAssetRequest(request) {
  if (!['mint', 'burn'].includes(request.action)) throw new Error('An explicit Assets mint or burn action is required.');
  if (typeof request.assetId !== 'string' || request.assetId.length > 39 || !/^(0|[1-9][0-9]*)$/.test(request.assetId)
      || BigInt(request.assetId) >= (1n << 128n)) throw new Error('Canonical unsigned asset ID required.');
  await validateExplicitSigner(request);
  if (request.memo != null && request.memo !== '') throw new Error('Asset mutation memo support is not enabled; never silently discard it.');
  // Precision is read from Assets.Metadata, never the native token or a caller-supplied guess.
  if (typeof request.amount !== 'string' || request.amount.length > 100 || !/^(0|[1-9][0-9]*)(\.[0-9]+)?$/.test(request.amount)
      || !/[1-9]/.test(request.amount)) throw new Error('A positive invariant asset amount is required.');
}

export async function prepareAssetOperation(api, request) {
  request = structuredClone(request);
  await validateAssetRequest(request);
  if (!api.tx.assets?.[request.action] || !api.query.assets?.asset || !api.query.assets?.metadata)
    throw new Error('Configured runtime does not expose the required Assets pallet operation/storage.');
  const stored = await api.query.assets.asset(request.assetId);
  if (stored.isNone) throw new Error('Asset does not exist on this configured chain.');
  const details = stored.unwrap();
  const metadata = await api.query.assets.metadata(request.assetId);
  const decimals = metadata.decimals.toNumber();
  const units = nativeUnits(request.amount, decimals);
  return withExplicitSigner(api, request, async (pair, sender) => {
    const role = details[request.action === 'mint' ? 'issuer' : 'admin'];
    if (!role || api.createType('AccountId32', role).toHex() !== sender)
      throw new Error('Explicit signer does not hold the asset issuer/admin role.');
    const recipient = api.createType('AccountId32', request.recipient).toHex();
    if (request.action === 'burn') {
      if (!api.query.assets.account) throw new Error('Assets account storage is unavailable.');
      const account = await api.query.assets.account(request.assetId, recipient);
      const balance = account.isNone ? 0n : BigInt(account.unwrap().balance.toString());
      const remaining = balance - BigInt(units);
      if (remaining < 0n) throw new Error('Requested burn exceeds the observed asset balance.');
      if (remaining > 0n && remaining < BigInt(details.minBalance.toString()))
        throw new Error('Requested burn would reap asset dust beyond the requested amount.');
      // This is a preparation-time check, not a guarantee against concurrent balance changes.
      // Final Assets.Burned amount MUST be checked before an ordinary API reports exact burn success.
    }
    const tx = api.tx.assets[request.action](request.assetId, recipient, units);
    const prepared = await signOriginalCall(api, tx, pair, protocol,
      { action: request.action, assetId: request.assetId, sender, recipient, units, decimals });
    await verifyAssetPreparation(api, prepared);
    return withOriginalFeeEstimate(api, prepared);
  });
}

export async function verifyAssetPreparation(api, preparation) {
  const { decoded, payload, genesis, sender } = await verifySignedPreparation(api, preparation, protocol);
  const call = decoded.method;
  if (call.section !== 'assets' || !['mint', 'burn'].includes(call.method) || call.args.length !== 3)
    throw new Error('Signed call is not an authorized Assets mint/burn operation.');
  const action = call.method;
  const assetId = call.args[0].toString();
  const recipient = api.createType('AccountId32', call.args[1].toString()).toHex();
  const units = call.args[2].toString();
  if (!/^[1-9][0-9]*$/.test(units) || BigInt(units) >= (1n << 128n)
      || (preparation.action != null && preparation.action !== action)
      || (preparation.assetId != null && preparation.assetId !== assetId)
      || (preparation.recipient != null && preparation.recipient !== recipient)
      || (preparation.units != null && preparation.units !== units))
    throw new Error('Declared asset intent differs from the exact signed action/asset/account/units.');
  return { verified: true, chainId: genesis, sender, transactionHash: decoded.hash.toHex(),
    action, assetId, recipient, units, signingCheckpointHash: payload.blockHash,
    feeCoverage: 'identity-only-not-a-fee-cap-or-receipt' };
}
