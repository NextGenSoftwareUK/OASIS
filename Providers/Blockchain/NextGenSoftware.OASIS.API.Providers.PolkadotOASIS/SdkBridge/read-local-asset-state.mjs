// Read-only LOCAL fixture probe for independent C# balance/supply assertions. No signing or mutation.
import { ApiPromise } from '@polkadot/api';
import { ReadOnlyHttpProvider } from './read-only-provider.mjs';
if (process.env.OASIS_POLKADOT_LOCAL_NATIVE !== '1') throw new Error('Explicit local fixture opt-in required.');
const api = await ApiPromise.create({ provider: new ReadOnlyHttpProvider('http://127.0.0.1:19944'), noInitWarn: true });
try {
  if (api.genesisHash.toHex() !== '0xe8cf5d7d636a08721efaa17acc52ed3d895fe2468dccb2520cc8e249ddb6b208')
    throw new Error('Not the retained fixture ledger.');
  const blockHash = await api.rpc.chain.getFinalizedHead();
  const at = await api.at(blockHash);
  const details = (await at.query.assets.asset('424242')).unwrap();
  const account = (await at.query.assets.account('424242', '0x1a4fee48c1ba1a48e8cd43782a8485d635aa91cfb82cbb477f0c1c576bc4031c')).unwrap();
  process.stdout.write(JSON.stringify({ units: account.balance.toString(), supply: details.supply.toString(), blockHash: blockHash.toHex() }));
} finally { await api.disconnect(); }
