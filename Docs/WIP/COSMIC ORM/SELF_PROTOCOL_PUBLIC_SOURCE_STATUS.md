# Self Protocol public CQL source qualification

7 November 2026. Native CQL, live Development HTTP/reuse/privacy/replicas and card79 accepted.
Functional browser remains open.
Hosted/browser gates remain open. No runtime installation or native business write.
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

## Native CQL accepted on task source f5c4826c5

Corrected cloud42446401193/job128626132502 passed26controls (21registry+5existing
proof),1296shared cases and1actual native CQL3/2 case, all executed/no skips.
Source `f5c4826c5eaf8a09dcebacec7878018366a89379`. Artifact13603612128/307467bytes
SHA2562f12103b6958a011e167f2ff779827b2640d85a230f7ebf72b0c78cadbf089ec,
all three TRX counters/hashes verified entirely in memory. The initial inspector
refused an incorrect expected source SHA before artifact access; full SHA then
resolved from Git. Initial256KiB compressed bound refused the307467byte shared
report; metadata checked, bound512KiB, uncompressed8MiB; no archive retained.

Local scope count121; live HTTP93/browser28 unchanged. WEB4 qualification
42446401228/job128626132649 remains active. Affected SubQuery native regression
42446401144 succeeded; its WEB4 regression42446401174 is also active.

Protected fixed-catalogue planning now includes SelfProtocolOASIS.39candidate
controls (including cross-provider subcases) and5publisher guards pass; fixture
messages are simulated configuration writes, not live mutations. Cross-language
GUID fixture added to the existing catalogue test, keeping26controlled cases.
Offline source ownerc7efecf1-7536-44bd-82bf-54b67c67b026 is fixed in
HostedSelfProtocolRegistrySourceCurrent. Public IDsdae615d6-c682-865f-bdb7-0fc6801c8b99,
415214d6-445b-861b-b65f-c4c1f6ac11f7,bf1a83eb-8cf5-8b82-8fb1-86fe294e5eb4.
Native source writes0. Protected before253/candidate255 settings prepared,
exactly owner/admission/grants/provider-presets change. Initial receipt assumed
an optional publicIds field, then reconciliation compared serialized sorted
review ordering; both corrected using the same descriptor and original snapshots,
no owner regeneration or live request. Fresh live baseline/pins and exact scoped
candidate qualification are still required before any rollout.

## WEB4 task proof and exact release candidate

Task sourcef5c4826c5 WEB4 cloud42446401228/job128626132649 succeeds:8controls
and1real HTTP/Mongo case, no skips. Artifact13604156126/4642bytes SHA256
0b4a39141d5d00d88ba07e4fc76212542844a72e3aea8ef22221cd56c9d58bc2
and both TRX verified in memory. Actual native3/2,paired5/4,foreign private0/
public3,2SDK saves,9receipts, owned databaseAbsent:true. This does not claim
hosted snapshot reuse/replica/browser acceptance.

Candidate916c62210a1eb51fa23353bccfca3024750f452b/Core25a296e986f35fe118f30e7489c655151d57e97e
prepared directly from accepted9fd968a7b/Core06df088 Git objects and taskfe5afa673.
16changed paths, only Self CQL closure/new shared methods/catalogue; unrelated
HostedBasicSqlQueryReader, dirty enum, STAR/WEB6 and other branches excluded.
Temporary indices removed in finally. Initial exact diff guard stopped because
Git auto-abbreviated blob hashes differently; replaced with full blob identity
comparison of old bases. No qualification weakened or checkout/environment.
Existing candidate refs advanced normally after fresh base checks; manifest
validation passes. No Development push or deployment requested.

Exact registry42446975238/job128627863798 and WEB442446977425/job128627870278
were dispatched once. Admission already triggered by candidate push42446952053/
job128627789633. A redundant manual admission42446979977 was dispatched before
recognizing that trigger; cancellation202 acknowledged and terminalCANCELLED
observed. Retain original push admission, no duplicate restart. Automatic
SubQuery native/WEB4 candidate regressions also run on their existing triggers.

Private dispatch-self-protocol-image.py is syntax-checked, not invoked. It
requires all three exact success receipts and matches current candidate ref,
then requests one explicit commit-specific teaching tag (no mutable sandbox tag).
Website8a650c6 backs up conditional card79 on the original website task branch;
main104a0064/public remains unchanged. Promote card79 only after hosted acceptance.

## Exact candidate qualified; image active

Source916c62210a1eb51fa23353bccfca3024750f452b/Core25a296e exact gates all pass:
- Registry42446975238:26controlled+1296shared+1actualnative3/2; artifact13604230606/307639bytes, SHA256131ba26403feef504a6e26510b9479abd381563f215d77836631410894c99d2d.
- WEB442446977425:8boundary+1actualHTTP/Mongo; artifact13603598148/4644bytes, SHA256a3e659a8e65255813ba7a902c9f1483c588b0229531ed91c6873c1591b456423. Actual3/2,5/4,private0/public3,9receipts,ownedDBabsent.
- Admission42446952053:25controls; artifact13604635191/4385bytes, SHA25619739040781ee6c823908a1e8b7b7616ca23bb176c90f2822066c92ff98686f2.
All TRX counts/hashes verified in memory, no skips or retained archive.

One exact image request acknowledged:42447469381/job128629322069 is active,
Build and push. Explicit tag916c62210a1eb51fa23353bccfca3024750f452b-teaching
with teachingSidecars:true; no mutable sandbox tag. Do not dispatch again.
Read-only prerequisite review succeeds: seven pins advertised at expected refs,
complete253settings unchanged, baselineeb09ea25/source9fd/actual510762ee57.
No Development push/settings/source operation. Guarded publisher prepared only;
requires successful exact image receipt and rechecks all prior assumptions.
Read-only package inspector prepared/Python+remote JS syntax verified, not run;
preserves accepted SubQuery and Dapr while verifying Self/shared reader pins and
SDK assemblies. Latest actionable handle and sequence are inCURRENT_STATUS.md.

8 November Development batch: image42447469381 SUCCESS, exact916c62210-teaching tag/provenance verified, GHCRsha256:bb86863c7bf41311cf3e49b77203f87d41d39c66de2cb14f65c674bf27c66067. Fresh publisher rechecks seven advertised pins, all253settings, exact accepted baseline and branch ancestry, then one normal Development push acknowledged. Code deployment131667e6-1d02-4958-8584-448520365dac isQUEUED; original protected metadata/handle retained. No settings/native operation or duplicate request. Inspect actual running code package before staging/activation.

8 November: code131667e6 SUCCESS, actualsha256:d8755e8788ef050419ccddd7eaca3e9b3978ad6658e740906af888d81a84b970, instance03a1c671-2988-4d35-8df6-14db5ea73eef. Read-only inspection verifies253settings/206critical,6helpers,11SDK/provider assemblies/native proof and preserved Dapr/SubQuery sources; Self correctly disabled. Four settings preflight/apply ACK exactly once, without deploys; one activation request acknowledged. Activation1fe29f38-1ddb-4ca1-82ba-4c000dac26d7 isBUILDING. Protected original response and handle retained. No Self visitor cohort/native source write yet. Require exact255setting/running-package inspection before HTTP acceptance.

## Live Development HTTP acceptance sealed — 8 November

Activation1fe29f38-1ddb-4ca1-82ba-4c000dac26d7 SUCCESS on instance
ff2628fd-b511-4e26-9bc6-cb7b86a34b43. Actual Railway image
sha256:e2f01ef06f3471231df01ef5d09dd1af19551a46311ef7f21d2dae25565c0dd8.
All255settings/208critical,6helpers,11assemblies/native/runtime proof verified;
Self enabled, SubQuery/Dapr source/runtime acknowledgements preserved.

First live cohort owner2826f75b-27ec-40f4-a259-edaa1921c81b, foreign
16769240-3a08-4e78-a1d2-2b8e6b7e4bac, reused the acknowledged PouchDB readiness
visitor. No new readiness actor or public/native source data created.
Native3/2, Mongo federation5/4, first2saves/repeat0new+2existing, exact SQLite/
Neo4j replicas2/2 and foreign private0/public3 all pass. Empty pending intents,
exactly2save ACKs and original reports composed into durable acceptance receipt
without another query/save/session. No continuation/replacement cohort needed.
Browser remains open; this verifies HTTP, not functional browser interaction.

Evidence SHA256:
- native:60f0551de351595ab30cbc883f7c6749466911fddbf5bf91c9303a3b0cb1c6f9
- client:861906c417423b7b7eada3477ad2c3dd3315406254790fd8272b562a6030d7d2
- isolation:b282fdacfd1d0f379e6ce29ddd517a761b3b8561f7d96a9a0e1b64e698a40584

Original protected sessions, journals and cql-http-acceptance.acknowledged.json
are under HostedQueryAcceptance-SelfProtocolOASIS. Never rerun --prepare or
replace these originals to regenerate evidence. Scope122local/94HTTP/28browser.
Card79 may now be published with this immutable proof and explicit browser gap.

Card79 published atomically to website task/mainfc2714b50fb032c8c8e0c3e7dbb9006ce039b883 after fresh refs/ancestry checks. Immutable acceptance link892a215ec, C# source916c62210. Public HTML HTTP200/270083bytes/SHA2564f9bade880ef8c24bfbdb8007a2fb9dbf9f3baa437bb5183ebf18547837aec80 matches exact Git bytes, observed in memory only. Existing119client controls cover unchanged client logic; publication changes HTML wording/link only. Browser interaction remains unverified; no policy bypass or repeated accepted flow.
