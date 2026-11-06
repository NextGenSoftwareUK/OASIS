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
WEB4 admission, federation and hosted acceptance remain open.

[Official verifier deployments](https://docs.reclaimprotocol.org/onchain/solidity/supported-networks) ·
[Reclaim verifier ABI source](https://github.com/reclaimprotocol/reclaim-solidity-sdk/blob/3326a4e4b7d747b3f06d027a7c40ec8612bd0b5a/contracts/Reclaim.sol).
