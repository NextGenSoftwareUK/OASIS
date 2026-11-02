# Aztec public-read demo discovery

Reviewed 2 November 2026 while the original Bittensor deployment builds.
Documentation discovery only: no native request, CQL support, admission or
provider-count change. Bittensor hosted acceptance remains the immediate priority.

## Documented current interface

The [official network table](https://docs.aztec.network/networks) lists Alpha
(mainnet) version6.0.0, Ethereum L1 chain1, rollup version2070860440 and public
RPC `https://aztec-mainnet.drpc.org`. It publishes rollup contract
`0xE37A2663Cc9c119BA41572C40fAb0bbb23a5402f`. These are documentation values,
not independently observed native identity or a production-maturity claim.

The [current node API](https://docs.aztec.network/operate/operators/reference/node_api_reference)
defines `aztec_getNodeInfo`, `aztec_getChainTips`, `aztec_getBlock` and
`aztec_getBlockData`. The latter is documented as lightweight block metadata
without transaction bodies. Unified getBlock accepts narrowing options.
Examples include placeholder/type inconsistencies; inspect pinned v6source
request/response schemas before using them, rather than guessing params.
The versioned rc.1documentation link redirects to the current v6reference.

[Migration notes](https://docs.aztec.network/developers/docs/resources/migration_notes)
record renamed/unified APIs. A shipping adapter must select the current contract,
not silently retry old node_* prefixes or deprecated methods. Protocol identity
must bind L1 chain/rollup contract/version and the original selected source.

## Existing provider and next qualification

Existing AztecOASIS defaults to a local8080client with sandbox/bridge settings.
Its presence is not evidence for public v6mainnet CQL. Legacy implementation
remains unchanged; native/shared-suite acceptance remains open.

After the Bittensor hosted milestone, inspect pinned official v6 NodeInfo and
BlockData serialization, confirm operator public access/limits, then define a
bounded first qualification with exclusive durable intent. Verify network
identity first and select three metadata records, with strict IDs/errors/byte
budgets, no redirects/retries or body/private-note fetches. Do not claim ZK
proof verification, private-state querying, finalized settlement or storage
writes from node-reported metadata. No SDK/node/account/key, database, dataset,
fixture, source owner, hosted settings or browser card was created.
No David action or new environment is needed for this discovery.

The upstream v6.0.0tag resolved to `0219f11d8133970df3974fe5ae65e006bf8c193a`.
Guessed NodeInfo/BlockData paths were unavailable via the documentation tool;
a bounded GitHub contents request for the interfaces directory returned404.
No SDK tree/file was cached or installed, and no native RPC ran. Inspect the
correct pinned source layout before implementation; do not infer an unavailable
chain from a source-path lookup failure. Bittensor hosted milestone is now green.
