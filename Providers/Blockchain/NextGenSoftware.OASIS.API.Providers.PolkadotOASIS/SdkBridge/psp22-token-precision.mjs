import { contractReadBytes } from './pinned-contract-query.mjs';
import { decodeInkScaleResult } from './ink-scale-result.mjs';

// Decimal API amounts use this token's actual metadata at one finalized view,
// never the native currency's precision or an inferred default. The code hash
// is observed provenance, not a claim that arbitrary token code was reviewed.
export async function readPsp22Precision(api, request) {
  request = structuredClone(request);
  for (const field of ['tokenAddress', 'sender'])
    if (typeof request[field] !== 'string' || !/^0x[0-9a-f]{64}$/.test(request[field]))
      throw new Error('Canonical token and metadata origin required.');
  const finalized = await api.rpc.chain.getFinalizedHead();
  const at = await api.at(finalized);
  if (!at.query.contracts?.contractInfoOf || !at.call.contractsApi?.call)
    throw new Error('Runtime cannot read PSP22Metadata precision.');
  const info = await at.query.contracts.contractInfoOf(request.tokenAddress);
  if (info.isNone) throw new Error('PSP22 token does not exist at finalized snapshot.');
  const codeHash = info.unwrap().codeHash.toHex();
  api.registry.register({ OasisPsp22MetadataLangError: { _enum: { CouldNotReadInput: 1 } } });
  const bytes = contractReadBytes(await at.call.contractsApi.call(request.sender, request.tokenAddress, 0, null, null, '0x7271b782'));
  const decimals = decodeInkScaleResult(api.registry, 'u8', 'OasisPsp22MetadataLangError', bytes, 'PSP22Metadata').toNumber();
  if (decimals > 38) throw new Error('PSP22 precision exceeds supported u128 decimal scaling.');
  return { decimals, codeHash, blockHash: finalized.toHex() };
}
