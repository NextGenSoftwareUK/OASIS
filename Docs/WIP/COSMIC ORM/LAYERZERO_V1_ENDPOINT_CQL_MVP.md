# LayerZero v1 deployed endpoint CQL MVP

The profile reads three published LayerZero v1 endpoint deployments
through fixed public Ethereum, Polygon and Arbitrum RPCs. Each
read verifies the EVM chain and genesis, then uses one finalized
block hash and EIP-1898 canonical selectors for the endpoint chain
ID and default send and receive library versions.

| Network | Deployed contract chain ID | Published v1 chain ID |
| --- | ---: | ---: |
| Ethereum | 1 | 101 |
| Polygon | 9 | 109 |
| Arbitrum | 10 | 110 |

The contract-returned and published identifiers are distinct fields.
The initial native projection failed because OASIS metadata requires
normalization of native unsigned integers. The provider now validates
the exact ABI `uint16` and projects it as an OASIS integer.

At Core `06a4363e1c26e616eddda34c2959acadab8dbba8`, the native
contracts returned exact 3/2 rows. The wrong-genesis and wrong
contract-identity controls passed. Three passed with zero skips.
The 5,870-byte passing TRX SHA-256 is
`56e17257988ef887e92459423be308c9f96794f3cea5a9cb42b140bbf1cae52a`.
The original failing 6,397-byte TRX SHA-256 is
`50709d42fbfbe5c4412e6c8083ee371330d3fa3975ef5966a0e4221605827272`.

The explicit WEB4 read profile requires the protected setting `CQL_LAYERZERO_V1_ENDPOINT_QUERY_SOURCE_OWNER`.
At Core `2324c21cfe680f465907e8dd1175b7229d7ef5d3`, all 23 admission checks
passed with zero skips. The 41,744-byte admission TRX SHA-256 is
`b51d80c1903740dddf00ad219da9a9deea541cae29630dab236f0b87e069b2b2`.
The protected publisher passed 39 settings and 21 evidence tests.
This profile covers deployed v1 endpoint configuration. Its contracts
and acceptance are separate from message delivery and LayerZero v2.
[Published v1 deployments and ABI](https://github.com/LayerZero-Labs/endpoint-v1-solidity-examples/tree/cdc93994911829b1348f6ac18000000a43432ef1/constants) ·
[Archived v1 endpoint source](https://github.com/LayerZero-Labs/LayerZero-v1/blob/main/contracts/Endpoint.sol) ·
[Official v1 support update](https://layerzero.network/blog/ongoing-security-updates-3).

## Local WEB4 and Mongo federation

At Core `2324c21cfe680f465907e8dd1175b7229d7ef5d3`, WEB4 HTTP
returned native 3/2, paired 5/4, foreign-private 0 and foreign public 3.
The shared run passed three provider cases with zero skips. The
6,208-byte TRX SHA-256 is
`ce187305a3ebafd94bf5fd2fbe2734fb097153f258ebdc5231752f8c869bf6eb`.
The fixture confirmed its exact owned database was absent after cleanup.
Hosted HTTP and browser acceptance remain open.

## Joint Development candidate

LayerZero v1 and Reclaim share a scoped WEB4 image candidate
2cd6ef3d6b002d45fba0909f4f250f88614490c2 with Core
cb99792e65854c4152e4a89e7fb7b827ebe8133e. Both refs are published on
work/cosmic-layerzero-reclaim-release-candidate. All Railway gitlinks
and dependency pins matched, and the provider files match the tested
task branch. The [image gate](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42328208850)
runs against the exact candidate. The six-value protected candidate under
LocalState/COSMIC/HostedSourceRolloutCurrent uses the layerzero-reclaim-public
prefix. It includes four presets per provider and preserves the existing
Push Protocol enrollment. Hosted settings have not been applied.

The guarded publisher accepts both provider names in one invocation:
Publish-HostedFixedPublicCatalogue.py LayerZeroOASIS ReclaimProtocolOASIS
--prefix layerzero-reclaim-public --release-ref
work/cosmic-layerzero-reclaim-release-candidate --deployment-id <code-job>.
Use the exact expected commit environment value and preflight without
--apply first. The publisher recomputes the shared admission and grants
once before it writes either preset partition. Five publisher tests and
39 exact-key configuration tests passed.

The first image build failed with CS0246 in both providers because the
scoped candidate omitted the shared ReviewedEvmReadClient source.
Candidate f9d77e9dc79e632c714a5aa244a94e1f69a38352 adds that exact tested
blob. Core and settings snapshots are unchanged. The corrected
[image gate](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42328625924)
must pass before promotion.

The corrected image gate passed. The candidate tag resolved to
sha256:2189a5fd52fc35cff47611b5e2e5bc2bdea7f9bf09fc849b479193909bb2a6f5.
The protected settings reproduced exactly, and the live baseline matched
before fast-forwarding Development. Railway code deployment
24c2dab0-d1f6-414c-889b-c1a2094b3299 is queued at f9d77e9dc. Observe this
exact job before applying any settings.

Code deployment 24c2dab0 succeeded. The guarded batch applied and read
back all six settings. A same-release redeploy is building as
e8a57bf6-5a96-42c5-a41f-ce350711f272. The exact deployment must succeed
before testing the two live catalogues and MongoDB federation.

## Development HTTP acceptance

The exact settings deployment e8a57bf6 succeeded and matched the protected
candidate. Discovery reached activation generation 1. Hosted native
queries returned 3/2 and MongoDB federation returned 5/4. First prep
saved two private copies. Repeat saved zero and reused both. Both replicas
held two owned copies, and the second visitor saw all three public records
and zero private copies on MongoDB, SQLite and Neo4j. Sandbox card and
interactive browser verification remain open.

Evidence under TestResults/HostedCqlIntegrationCurrent, SHA-256:
- LayerZeroOASIS-native-http.json 947d809d41a7e71470e9a18420103f319a450b5af83c67c9154be3413ac66d45
- LayerZeroOASIS-client-http-flow.json 8af900c308dba957901410291df617dd4415d357885195d27283aec1ae9343b6
- LayerZeroOASIS-isolation-http.json 20bf219e2264faf9a4f3213ab0ac2815fcc1f790e17bccb432a09d5f8ff6fc13

## Sandbox publication

The [sandbox card](https://web4.oasisomniverse.one/web4-sandbox.html#cql-layerzero-v1-catalogue)
is live at Web4Site 6c95279 with native and MongoDB instructions and a local
C# query example. All 114 website tests passed. The live 230,997-byte HTML
matched Git SHA-256 272edabde09f1ad5eb7936c47e33c28561e413abff40f74ba6e6c1eb5bf3b285.
Interactive browser acceptance remains open.

11 November UTC: the pinned Ethereum RPC returns pruned-history error 4444 for genesis. The reviewed Ethereum origin in the task branch now points to dRPC. Laptop checks passed all three canonical endpoint ABI calls and chain ID. Focused CI and hosted verification are pending. Preserved genesis, contract, network and source identities.

11 November UTC: dRPC preflight from the live Development WEB4 container passed exact Ethereum genesis, finalized block and all three LayerZero v1 canonical ABI calls, including contract chain ID. The replacement code and focused test are in candidate a2990f712c. CI 43020496094 is active. Live activation and browser verification remain pending.

11 November UTC: the focused LayerZero v1 test step passed for exact candidate a2990f712c in [CI 43020496094](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43020496094). The full production build is running. Development integration and live browser acceptance remain pending.

11 November UTC: the focused LayerZero v1 tests passed on exact Development candidate dcb8ad88bd in [CI 43022922018](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43022922018). Production compilation is active. Development is still at base 8207703684.

11 November UTC: exact candidate dcb8ad88bd passed [combined CI](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43022922018) and was fast-forwarded into Development. Live browser verification remains open.

11 November UTC: Railway deployment `ba604fae-510c-4e38-9ca8-3c511884eaf3` succeeded at Development `dcb8ad88bd`. On the live WEB4 CQL Playground, `LayerZeroOASIS` returned three rows for the native public teaching preset and two for the filtered preset after the dRPC fix. Operations `11f7b081-7025-9916-e2db-48a267789bd5` and `5331dd0c-ca02-1476-bddc-e341c210f2a4`. Earlier paired and replica evidence remains recorded. Original-owner private browser verification is still open.
