import assert from 'node:assert/strict';
import { ApiPromise } from '@polkadot/api';
import { ReadOnlyHttpProvider } from './read-only-provider.mjs';
import { prepareAssetOperation, verifyAssetPreparation } from './asset-operation.mjs';
import { verifyNativePreparation } from './verify-preparation.mjs';
import { prepareNativeTransfer } from './native-transfer.mjs';

if (process.env.OASIS_POLKADOT_LOCAL_NATIVE !== '1') throw new Error('Explicit LOCAL-only opt-in required.');
const api = await ApiPromise.create({ provider: new ReadOnlyHttpProvider('http://127.0.0.1:19944'), noInitWarn: true });
try {
  assert.equal(api.genesisHash.toHex(), '0xe8cf5d7d636a08721efaa17acc52ed3d895fe2468dccb2520cc8e249ddb6b208');
  const request = { action: 'mint', assetId: '424242', amount: '1.250001',
    sender: '0x1a4fee48c1ba1a48e8cd43782a8485d635aa91cfb82cbb477f0c1c576bc4031c',
    recipient: '0x1a4fee48c1ba1a48e8cd43782a8485d635aa91cfb82cbb477f0c1c576bc4031c',
    privateKey: '0x' + '02'.repeat(32) }; // Public LOCAL-only fixture.
  const before = (await api.query.assets.account(request.assetId, request.recipient)).unwrap().balance.toString();
  const supplyBefore = (await api.query.assets.asset(request.assetId)).unwrap().supply.toString();
  const mint = await prepareAssetOperation(api, request);
  assert.equal(mint.decimals, 6);
  assert.equal(mint.units, '1250001');
  assert.equal(mint.submitted, false);
  assert.equal((await verifyAssetPreparation(api, mint)).action, 'mint');
  const burn = await prepareAssetOperation(api, { ...request, action: 'burn', amount: '0.000001' });
  assert.equal(burn.units, '1');
  assert.equal((await verifyAssetPreparation(api, burn)).action, 'burn');
  let rejected = 0;
  for (const fields of [{ action: 'burn' }, { assetId: '424243' }, { units: '9' },
    { recipient: '0xd43593c715fdd31c61141abd04a99fd6822c8558854ccde39a5684e7a56da27d' }]) {
    await assert.rejects(verifyAssetPreparation(api, { ...mint, ...fields })); rejected++;
  }
  await assert.rejects(verifyNativePreparation(api, mint)); rejected++;
  const withoutProtocol = { ...mint }; delete withoutProtocol.protocol;
  await assert.rejects(verifyNativePreparation(api, withoutProtocol)); rejected++;
  const native = await prepareNativeTransfer(api, { ...request, amount: '1' }, 12);
  await assert.rejects(verifyAssetPreparation(api, { ...native, protocol: mint.protocol })); rejected++;
  await assert.rejects(prepareAssetOperation(api, { ...request, amount: '1.0000001' })); rejected++;
  // Reused ledger balances change after verified mints/burns. Test the live bound, not the initial seed balance.
  const aboveBalance = (BigInt(before) + 1n).toString().padStart(mint.decimals + 1, '0');
  const excessiveAmount = aboveBalance.slice(0, -mint.decimals) + '.' + aboveBalance.slice(-mint.decimals);
  await assert.rejects(prepareAssetOperation(api, { ...request, action: 'burn', amount: excessiveAmount }),
    /exceeds the observed asset balance/); rejected++;
  await assert.rejects(prepareAssetOperation(api, { ...request, assetId: '424243' })); rejected++;
  await assert.rejects(prepareAssetOperation(api, { ...request, sender: '0x8ee504148e75c34e8f051899b3c6e4241ff18dc1c9211260b6a6a434bedb485f', privateKey: '0x' + '03'.repeat(32) })); rejected++;
  assert.equal((await api.query.assets.account(request.assetId, request.recipient)).unwrap().balance.toString(), before);
  assert.equal((await api.query.assets.asset(request.assetId)).unwrap().supply.toString(), supplyBefore);
  console.log(JSON.stringify({ assetId: request.assetId, decimals: 6, mintUnits: mint.units, burnUnits: burn.units,
    signedAndIndependentlyVerified: true, rejectedCases: rejected, assetBalanceAndSupplyUnchanged: true, submitted: false }));
} finally { await api.disconnect(); }
