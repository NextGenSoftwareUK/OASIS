# Polkadot storage contract status

No ink! contract implementation, Cargo manifest, compiled artifact or deployment is present in this directory. The previous README described an implementation that was not supplied. Its build/instantiate instructions and claimed OASIS message mappings were not executable deployment instructions and have been removed.

The provider's remaining REST/custom-runtime storage assumptions are incomplete. A successful network activation, native balance read or signed native-transfer preparation does not provide avatar/holon/NFT contract storage. Do not configure an arbitrary contract address or assume that changing `_contractAddress` enables these capabilities.

Before enabling storage, implement and review the actual target-chain storage protocol and its deployed artifact/interface, preserve private-record encryption and query requirements, use real signed SCALE calls with finalized outcomes, and prove relevant interfaces against retained native data. Reuse the shared OASIS storage/projection/preparation components rather than inventing parallel managers. Any runtime/contract choice and deployment still require evidence; no deployment is claimed here.

See the parent [provider README](../README.md) and repository release-gate/provider-I/O reports for currently implemented operations and remaining gates.

