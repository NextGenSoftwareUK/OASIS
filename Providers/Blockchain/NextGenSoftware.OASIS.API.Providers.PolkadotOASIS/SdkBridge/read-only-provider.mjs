import { HttpProvider } from '@polkadot/api';

const allowed = new Set([
  'rpc_methods', 'system_chain', 'system_name', 'system_version', 'system_properties', 'system_health', 'system_accountNextIndex',
  'chain_getBlockHash', 'chain_getFinalizedHead', 'chain_getHeader', 'chain_getBlock',
  'state_getMetadata', 'state_getRuntimeVersion', 'state_getStorage', 'state_getStorageAt',
  'state_queryStorageAt', 'state_getKeysPaged', 'state_call', 'payment_queryInfo', 'payment_queryFeeDetails'
]);

export class ReadOnlyHttpProvider extends HttpProvider {
  async send(method, params, ...rest) {
    if (!allowed.has(method)) throw new Error('Polkadot preparation transport rejects non-read-only RPC methods.');
    return super.send(method, params, ...rest);
  }
}
