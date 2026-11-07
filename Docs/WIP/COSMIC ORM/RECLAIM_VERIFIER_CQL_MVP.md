# Reclaim verifier public configuration CQL MVP

This read profile covers the official Reclaim verifier deployments on
Base, Arbitrum One and Polygon. It verifies the EVM chain and genesis,
uses a finalized EIP-1898 canonical block per network, and requires the
contract's Semaphore address to match the published deployment. It
projects the epoch number and configured epoch duration with the
verifier, Semaphore, and finalized block identities. No user proof,
witness host or claim data is requested.

| Exact key | Verifier | Observed epoch |
| --- | --- | ---: |
| base | `0x8cdc031d5b7f148ab0435028b16c682c469cefc3` | 1 |
| arbitrum | `0x9f0472fd02ca1bc2d6c3a1702803ba822c7c7e91` | 1 |
| polygon | `0xd6534f52ceb3d0139b915bc0c3278a94687fa5c7` | 2 |

All three returned a configured duration of 86,400 seconds on 6 November
2026. These are configuration observations, independent of current
epoch validity and proof acceptance.

At Core `2324c21cfe680f465907e8dd1175b7229d7ef5d3`, native CQL
returned exact 3/2 rows. Wrong-genesis and wrong-Semaphore controls
passed. Three passed with zero skips. The 5,914-byte TRX SHA-256 is
`6de150892bcb761ebe3afdc0363d23d4f06b480e5b19d68f3c1a49693393ab17`.
The original test project compile lacked explicit framework imports.
The corrected consumer passed against the same pinned provider source.

[Official verifier deployments](https://docs.reclaimprotocol.org/onchain/solidity/supported-networks) ·
[Reclaim verifier ABI source](https://github.com/reclaimprotocol/reclaim-solidity-sdk/blob/3326a4e4b7d747b3f06d027a7c40ec8612bd0b5a/contracts/Reclaim.sol).

## WEB4 admission

The protected profile requires the canonical teaching owner setting
`CQL_RECLAIM_VERIFIER_QUERY_SOURCE_OWNER`. It uses its own read permission
and tariff. At Core `c1354d87da2add7720f9c40d4e502195c01a3f41`, all 29 combined admission
checks passed with zero skips. The initial 27 passed, and the count
guard exposed two absent invalid-owner cases. The expanded consumer
ran against the unchanged producer. The 52,144-byte final TRX
SHA-256 is
`887bf56bef6798423c9d1383ad47b84796df95a9e1b1bc62d34cf54d392dd29c`.
Protected configuration passed 39 tests and response evidence passed 21.

## Local WEB4 and Mongo federation

At Core `c1354d87da2add7720f9c40d4e502195c01a3f41`, local
WEB4 HTTP returned native 3/2, paired 5/4, foreign-private 0 and
foreign public 3. The combined run passed both cases with zero
skips. The 4,724-byte TRX SHA-256 is
`55846eda17f2707263b62c42502c1e6faac38094b859e70e812136dd441d9e7a`.
The exact synthetic database was removed after verification.
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
- ReclaimProtocolOASIS-native-http.json 75feba3952eb7a96ee08aac231c275dae0e36a06a5c7aa2ae779e20ca75e6659
- ReclaimProtocolOASIS-client-http-flow.json fdf1e7d8319ac7b9ebcd3dab9dffec9db99be1ef020b05eee16d27bb99049ece
- ReclaimProtocolOASIS-isolation-http.json 3ea8e1e12dd1724d0958124028f9b0ed8712d5d2994f57a5040e9301c73572df

## Sandbox publication

The [sandbox card](https://web4.oasisomniverse.one/web4-sandbox.html#cql-reclaim-catalogue)
is live at Web4Site 6c95279 with native and MongoDB instructions and a local
C# query example. All 114 website tests passed. The live 230,997-byte HTML
matched Git SHA-256 272edabde09f1ad5eb7936c47e33c28561e413abff40f74ba6e6c1eb5bf3b285.
Interactive browser acceptance remains open.
