import { ApiPromise, HttpProvider } from '@polkadot/api';
import { readFileSync } from 'node:fs';

// Read-only operations only. No signer or submission path is exposed here.
let api;
let stage = 'request validation';
try {
  const request = JSON.parse(readFileSync(0, 'utf8'));
  if (request.operation !== 'balance') throw new Error('Unknown Polkadot SDK operation.');
  const endpoint = new URL(request.endpoint);
  if (!['http:', 'https:'].includes(endpoint.protocol) || endpoint.username || endpoint.password)
    throw new Error('An HTTP RPC endpoint without embedded credentials is required.');
  if (!/^0x[0-9a-f]{64}$/i.test(request.genesisHash)) throw new Error('A pinned genesis hash is required.');
  if (typeof request.address !== 'string' || !request.address.trim()) throw new Error('An account address is required.');
  stage = 'runtime metadata initialization';
  api = await ApiPromise.create({ provider: new HttpProvider(endpoint.href), noInitWarn: true });
  stage = 'network verification';
  if (api.genesisHash.toHex().toLowerCase() !== request.genesisHash.toLowerCase())
    throw new Error('Polkadot SDK genesis hash differs from the activated network.');
  // Validate the address against the actual runtime before querying storage.
  stage = 'address validation';
  const accountId = api.createType('AccountId32', request.address);
  stage = 'native precision query';
  const properties = await api.rpc.system.properties();
  if (properties.tokenDecimals.isNone || properties.tokenDecimals.unwrap().length !== 1)
    throw new Error('Exactly one authoritative native-token precision is required.');
  const decimals = properties.tokenDecimals.unwrap()[0].toNumber();
  if (!Number.isInteger(decimals) || decimals < 0 || decimals > 38)
    throw new Error('Native-token precision is outside the supported range.');
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
} catch (error) {
  // Preserve failure stage/type/reason without echoing endpoint URLs or request content.
  const kind = error instanceof Error ? error.name.replace(/[^A-Za-z0-9]/g, '').slice(0, 64) : 'UnknownError';
  const reason = error instanceof Error ? error.message.replace(/https?:\/\/[^\s"'<>]+/gi, '[RPC endpoint]').slice(0, 512) : 'Unknown SDK failure';
  process.stdout.write(JSON.stringify({ ok: false, error: `Polkadot SDK balance query failed during ${stage} (${kind}): ${reason}` }));
  process.exitCode = 1;
} finally {
  if (api) await api.disconnect();
}
