# Self Protocol public CQL source qualification

7 November 2026. Native network/hub/registry-root protocol qualified; CQL implementation pending. No CQL or hosted count
increase, provider modification, runtime installation or native business write.
SubQuery live HTTP acceptance and card78 publication are complete; browser remains open.

## Existing implementation and primary contract

The existing SelfProtocolOASIS provider already contains real Nethereum proof
verification, typed hub/registry calls, context/scope hashing and disclosure
validation. Preserve that work; a new public CQL representation must not replace
it or claim that registry metadata proves an individual's identity.

The maintained [Self MCP source](https://github.com/selfxyz/self-mcp/tree/b4ca8f27ee97cddbac196a1343a58c3a3bfbddf1)
was pinned at b4ca8f27ee97cddbac196a1343a58c3a3bfbddf1 without cloning or installing
the server. Its [configuration](https://github.com/selfxyz/self-mcp/blob/b4ca8f27ee97cddbac196a1343a58c3a3bfbddf1/src/config.ts)
selects https://forno.celo.org for mainnet. Its
[contract tools](https://github.com/selfxyz/self-mcp/blob/b4ca8f27ee97cddbac196a1343a58c3a3bfbddf1/src/tools/contracts.ts)
name chain42220, the same hub0xe57F4773bd9c9d8b6Cd70431117d353298B9f5BF as the
existing provider, registry(bytes32), discloseVerifier(bytes32) and the mandatory
getIdentityCommitmentMerkleRoot() view. Published mainnet registry references:

| Registry | Attestation ID described by upstream | Published address |
|---|---:|---|
| Passport |1|0xd603Fa8C8f4694E8DD1DcE1f27C0C3fc91e32Ac4|
| Aadhaar |3|0x70D543432782D460C96753b52c2aC2797f26924B|
| KYC |4|0x9cABdeBC3aF136efD69EB881e02118AC612c63b9|

These are source references, not observed on-chain bindings. Resolve each selected
attestation through the hub and verify the actual native contract before admission.
Do not infer support for document types3/4 in the existing proof verifier merely
from these public metadata references. Existing proof schemas remain unchanged.
Upstream catches unsupported OFAC getter failures and returns null; do not copy
that pattern into mandatory CQL reads. Declare each representation's actual
getters and fail a selected mandatory read without publishing partial rows.

## First anonymous protocol observation

One journalled singleton JSON-RPC POST, eth_chainId/id1, to the documented mainnet
endpoint returned HTTP200/application-json,43bytes, SHA256
`5e4b9353ad4649715e16eb95f059be7cc3fe2253d7e4af996da4a3021a112b09`.
Its validated result is0xa4ec (42220). This confirms reported chain ID/access,
not genesis, finalized state, contract deployment, identity proof or CQL execution.
Redirects and proxy use were disabled; no retry, alternate endpoint, wallet,
account, private document, signature, funding or transaction was involved.

Raw/status were persisted before parsing. Original intent,43byte response,
observation and validated receipt are under
`LocalState/COSMIC/RecoveryLaptopResume20261107/self-protocol-chain-readiness.*`.
The source tree response30589bytes was inspected in memory and hashed:
`5a5cedbd3eb7d1f375db596785d308beb9e01390e0c593fcbaa69a9eaf932282`;
only its compact path/hash receipt is retained. No source archive or dependency
environment was created.

## Remaining gates

Independently bind mainnet genesis/network identity, resolve one RPC-reported
finalized canonical block, then qualify actual hub/registry code and three
mandatory registry-root views at that same block. Preserve complete response
identity, exact uint256 values and registry provenance. No source writes.

If that protocol contract qualifies, implement bounded mapped-ID CQL over the
three public registry observations using the existing SDK/shared query pipeline.
Require controlled failures, actual native3/2, manager/WEB4/Mongo5/4 and privacy,
then exact release/image, hosted snapshot reuse/replicas and browser checks.
This is a prospective public registry scope; general identity verification,
storage and full provider/ORM parity remain their separately tracked roadmap work.

[Provider inventory](PROVIDER_CQL_AND_SANDBOX_STATUS.md) · [Active rollout](CURRENT_STATUS.md).

## Native protocol qualification — 7 November

One bounded journalled network batch returned HTTP200/21286bytes, SHA256
193e91f535152b4e0503c3451c0778dae8388f32d08d6e8348353202f2887c2a.
Reported chain42220 matches. Historic block0 hash
`0x19ea3339d3c8cda97235bc8293240d5b9dadcdfbb5d4b0b90ee731cac1bd11c3`
matches pinned [Celo client configuration](https://raw.githubusercontent.com/celo-org/celo-blockchain/b1b88599dae455ccc08a1084acea6ccdcb817612/params/config.go).
The1797byte [Superchain registry](https://raw.githubusercontent.com/ethereum-optimism/superchain-registry/3273a28575300b76cf82f282d34c2221f81cbefd/superchain/configs/mainnet/celo.toml) independently binds transition
block31056500 to
`0x7586014e20c69b3fa7c9070baf1a7edd95833db57853126f32593b455da2e5c5`,
which also matches the RPC. The transition anchor is not historic block0 genesis.

RPC-reported finalized block `0x4e66733` / `0xe8e3a373b127a614a3df09c20e23085c2881804d05aea7f3122039f08f7586ec` is the common
observation point. Hub code and all registry calls used EIP1898 blockHash plus
requireCanonical:true, with no number/latest substitution or retry. This binds
reported RPC observations; it does not independently establish consensus.

The pinned helper's listed registry constants differ from all three actual
hub-resolved addresses. The first local observer stopped on that comparison;
its complete612byte response/status/hash remain preserved. Reconciliation read
that same response without network replay. The documented registry(bytes32)
lookup through the pinned hub is the binding contract; published constants
were explicitly historical references. Existing proof verifier uses this same
right-aligned32byte integer encoding. No new left-aligned encoding path added.

| Attestation | Actual hub-resolved registry | Exact uint256 root at this block |
|---|---|---|
|1|`0x37f5cb8cb1f6b00aa768d8aa99f1a9289802a968`|`16467781073809921235718380216749760257063201868872900454449846404863196687156`|
|3|`0x61473e587319297d197aa33072f2837515d1ca4a`|`15132602829901602832616170110205297154820203316036968499170345415260925905600`|
|4|`0x238f83c641020ef9636694a846019fd2a105c4f0`|`17403457413196001059533281012865110387998358053288702523938180990850375925989`|

One subsequent six-call batch verified nonempty registry code and all three
mandatory getIdentityCommitmentMerkleRoot() ABI uint256 results at the same
canonical hash: HTTP200/1350bytes, SHA256
42b0414e1ed8e499b4c288b1ec9d4a375ad140a0fa054630b4277cbb12b094cf.
Selectors7ef50298/4bf4dc7a computed using the already installed @noble/hashes
Keccak implementation; no runtime/package install. No OFAC getter, personal
document, wallet, proof submission, signing or transaction was involved.
Raw/status persisted before parsing for every RPC batch. Compact source hashes,
original responses and validated receipts remain under the existing recovery
folder: self-protocol-{network-binding,hub-binding,registry-roots}.*.

Next implementation invariant: a bounded CQL record identifies attestation and
one explicitly reviewed finalized block hash; resolve registry through the hub
and read its mandatory root at that same canonical hash. Include network, hub,
registry, attestation, block and exact uint256 provenance. Fixed observation
identity prevents moving roots/finalized cursors from silently changing a
prepared snapshot's content. Validate chain/genesis and code; never trust a
historical helper address instead of the actual binding. Preserve existing
verification classes and proof schemas. Unknown CQL IDs must fail before
transport; one failed selected mandatory read must publish no partial rows.

CQL/native3/2, WEB4/Mongo5/4, hosted deployment, reuse/privacy/replicas and browser
remain unimplemented/unaccepted for this source. Provider counts stay unchanged.

## CQL implementation pending cloud qualification

SelfProtocolRegistryQuerySource now maps the three configured attestation IDs
through the shared bounded CQL compiler/mapper. The logical namespace includes
the reviewed block hash, and every observation verifies chain/genesis, exact
canonical block identity, deployed hub code, actual hub-resolved registry code
and mandatory root at EIP1898 requireCanonical:true. Root hex/unsigned decimal,
registry/hub, code hashes and block provenance are retained. No legacy proof
verifier or proof schema changed.

Shared ReviewedEvmReadClient owns canonical block/code checks; no extra RPC
parser. Shared bounded query accepts a copied explicit ID catalogue, admitting
the complete selected set before any native dispatch, including mixed known/
unknown requests. Existing consumers retain their configured behaviour.

New cloud workflow qualify-self-protocol-registry.yml requires21new controlled
cases plus5existing proof controls, all shared consumer checks, then one real
CQL3/2 case without skips. Existing Live forged-proof test is outside this CQL
qualification schedule, not counted or claimed. No local .NET build or hosted
registration/settings/deployment. Counts remain120local/93HTTP/28browser until
the corresponding exact evidence is observed.

First cloud42446173870/job128625460437 compiled source06b2951dd and passed all26controlled cases/no skips. Shared suite passed1295/1296, failed ParentAcknowledgementFollowsRealMongoPreparedIdentityWrite because required127.0.0.1:27017 MongoDB was absent. Native CQL stage skipped. New workflow now supplies runner-owned Mongo8.0.15 service with bounded health readiness; no test weakened or local service installed. WEB4 explicit owner/profile, compiler allowance, Core read-only tariff, eight boundary controls and real shared HTTP/Mongo fixture are prepared for the next exact qualification. Core88eefb17b committed/pushed on original task branch; unrelated dirty enum remains excluded, parent gitlink/manifest advance together. Not deployed.
