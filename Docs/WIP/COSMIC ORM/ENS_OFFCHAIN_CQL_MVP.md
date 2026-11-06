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
identities. WEB4 admission and live federation remain open.

[Web3.bio Profile API](https://api.web3.bio/).
