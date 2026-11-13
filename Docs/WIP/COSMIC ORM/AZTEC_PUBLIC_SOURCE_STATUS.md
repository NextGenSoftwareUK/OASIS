# Aztec public-read demo discovery

Reviewed 2 November 2026 while the original Bittensor deployment builds.
First bounded native qualification denied. No CQL support, admission or
provider-count change. Bittensor hosted milestone is complete and frozen.

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

## Resolved source layout and first native outcome

GitHub connector resolved the earlier source-path issue: parentv6.0.0at
0219f11d8133970df3974fe5ae65e006bf8c193a contains a `labs` submodule pointing
to [aztec-labs-eng/aztec-node](https://github.com/aztec-labs-eng/aztec-node/tree/1b6de149f4fe1cde3c5fb876d3822289f490cd2a).
Read the exact pinned NodeInfo, node API, block response/parameter/header/data
and L1-address schemas.7files/65498bytes cached under sdk/, no clone/SDK install.
Two independent Git blob hashes matched. sdk/manifest.json records all source
byte hashes and both repository pins.

The first qualifier selected the documented anonymous dRPC endpoint; operator
[public-rate documentation](https://drpc.org/docs/howitworks/ratelimiting) allows
network-specific exceptions. Maximum5reads/one per second/256KiB per response:
NodeInfo identity, finalized height,3narrowed block responses. All optional
transaction/L1/attestation payload flags false; no private-note or write API.
6offline tests pass for network/version/contract identity, metadata projection,
number/key/shape/body checks, RPC error/id/duplicate JSON and numeric precision.
These tests do not establish native or CQL support.

Exactly1request (`aztec_getNodeInfo`) returned403/Cloudflare1010,712bytes,
`browser_signature_banned`, `retryable:false`, explicit do-not-retry instruction.
No identity/block data accepted; remaining4requests never ran. No header
rotation, alternate endpoint, browser/proxy/transport bypass or account setup.
Original durable intent consumed; never delete it to replay qualification.

Original raw evidence TestResults/AztecPublicSourceCurrent; required denial
bytes copied exclusively into durable AztecPublicQualificationCurrent with
matching digest to survive cleanup. All evidence stays outside docs/git.

| Artifact | Bytes | SHA256 |
|---|---:|---|
| `qualification.intent.json` | 483 | `8fb84009b9e62e35658e460a815057601a7991f9df5e72d5e277600b9d8b9af6` |
| `qualification.json` | 422 | `b85eb424b98f12b7c2ca31c291430028c16df4512e581987a3cd70a9a6ff2f4f` |
| `rpc-1.json` | 712 | `111c13aaf05cb17552f1fae45b5508dd07b2d9561c7f600d368f566b4a7ba82b` |
| `sdk/manifest.json` | 1822 | `e9f043c8c082b7042cd0d0b499849f25666c33690e3de94e4b42cdd2c61fe4e7` |

Aztec remains red pending legitimate permitted access and an explicit lineage-
bound continuation. This establishes denial from this environment at that time,
not chain unavailability. Existing legacy provider unchanged; no source owner,
fixture, tariff, hosting setting, demo avatar or sandbox card created. No David
setup question while other work is available. Next review Zcash's official/free
public read options; do not install a full node or allocate another environment.
