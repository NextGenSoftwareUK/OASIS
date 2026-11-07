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
