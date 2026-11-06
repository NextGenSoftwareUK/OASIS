# ENS Offchain CQL MVP

The source reads a reviewed catalogue of canonical ENS names from the
public Web3.bio index. It projects only the name, the observed Ethereum
address and provenance. Stable logical Holon IDs come from the source
owner, provider, index namespace and ENS name. The read profile has no
source writes or scans. It preserves the distinction between Web3.bio
index observations and direct ENS resolver evidence.

Five controlled tests and one real Web3.bio CQL test passed with zero
skips at Core `fb544f634f9daef20d028c76fec58eb8b9da21bf`. The native
test verified three public names and a two-name subset. Its TRX SHA-256 is
`e448108a0509e20be0cf37b34946c7d35eb3f13ae720e01fdb6f5d913f31c8df`.
The controlled tests reject foreign, malformed and out-of-catalogue
identities. The opt-in WEB4 registration takes the protected
`CQL_ENS_OFFCHAIN_QUERY_SOURCE_JSON` with the owner, names and probe.
The reviewed publisher derives the same logical IDs as the .NET mapper.
Nine WEB4 checks and nine Turso regressions passed with zero skips at
Core `472018f5c5df368b14ce4541045cbb6c0b95c790`. The admission TRX SHA-256
is `bfad70cf91531abe7e00b2cb2f1797cc4280931cbd2d7ecb104506c09d33b9e7`.
All 69 offline hosted tooling tests passed. Live federation remains open.

[Web3.bio Profile API](https://api.web3.bio/).
