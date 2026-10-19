# Polkadot storage contract status

A legacy ink!-style draft exists at `oasis/lib.rs`, but there is no Cargo manifest, reviewed deployable artifact/metadata, or proven deployment here. Source presence is not an implemented native storage protocol. Earlier build/instantiate instructions and claimed custom OASIS runtime mappings were not executable deployment instructions and have been removed.

The provider's remaining REST/custom-runtime storage assumptions are incomplete. A successful network activation, native balance read or signed native-transfer preparation does not provide avatar/holon/NFT contract storage. Do not configure an arbitrary contract address or assume that changing `_contractAddress` enables these capabilities.

The legacy storage transaction helper no longer fabricates XOR signatures or a `0xtest` Base64-JSON fallback. It fails explicitly until a real deployed protocol and SDK encoder exist. This removal is not implementation/completion of storage; public legacy read assumptions, caller-state/error handling and target-chain acceptance remain open. Wallet/native-asset SDK submission owners are separate and unchanged.

Before enabling storage, implement and review the actual target-chain storage protocol and its deployed artifact/interface, preserve private-record encryption and query requirements, use real signed SCALE calls with finalized outcomes, and prove relevant interfaces against retained native data. Reuse the shared OASIS storage/projection/preparation components rather than inventing parallel managers. Any runtime/contract choice and deployment still require evidence; no deployment is claimed here.

See the parent [provider README](../README.md) and repository release-gate/provider-I/O reports for currently implemented operations and remaining gates.

