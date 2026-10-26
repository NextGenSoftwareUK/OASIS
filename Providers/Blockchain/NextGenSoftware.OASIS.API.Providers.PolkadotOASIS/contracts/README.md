# Polkadot storage contract status

The former plaintext projection draft has been replaced by an ink! 5.1.1 conditional opaque-cell contract at `oasis/lib.rs`, with a Cargo manifest and lockfile. Compilation/testing is in progress; there is no reviewed deployed artifact or proven provider integration yet. Source presence is not an implemented native storage provider.

The contract uses explicit selectors: constructor `0x00000001`, get `0x00000002`, put `0x00000003`, delete `0x00000004`, keys `0x00000005`. Writes require the deployment owner and an explicit expectation (any, missing, or exact revision). Revisions increase globally, including deletion, preventing stale revision reuse after delete/recreate. Enumeration uses dense slots, bounded eight-slot pages, and a generation; callers must pin a finalized block across pages. Keys are bounded to 1024 bytes and opaque values to 8192 bytes. Encryption, larger-record framing, indexes, history and canonical OASIS objects remain responsibilities of the existing shared storage layer. This contract does not itself encrypt input: publishing plaintext is still plaintext.

Target scope is the retained pallet-contracts/Wasm runtime, not a claim of relay-chain or pallet-revive compatibility. Missing-delete returns `Ok(None)` without an event; applied mutations emit the caller's operation tag, key and new revision. A finalized transaction and decoded application acknowledgement are both required; pool acceptance alone proves nothing. Off-chain unit tests alone cannot prove chain deployment, native fees, crash recovery or HyperDrive routing.

The provider's remaining REST/custom-runtime storage assumptions are incomplete. A successful network activation, native balance read or signed native-transfer preparation does not provide avatar/holon/NFT contract storage. Do not configure an arbitrary contract address or assume that changing `_contractAddress` enables these capabilities.

The legacy storage transaction helper no longer fabricates XOR signatures or a `0xtest` Base64-JSON fallback. It fails explicitly until a real deployed protocol and SDK encoder exist. This removal is not implementation/completion of storage; public legacy read assumptions, caller-state/error handling and target-chain acceptance remain open. Wallet/native-asset SDK submission owners are separate and unchanged.

Before enabling storage, implement and review the actual target-chain storage protocol and its deployed artifact/interface, preserve private-record encryption and query requirements, use real signed SCALE calls with finalized outcomes, and prove relevant interfaces against retained native data. Reuse the shared OASIS storage/projection/preparation components rather than inventing parallel managers. Any runtime/contract choice and deployment still require evidence; no deployment is claimed here.

See the parent [provider README](../README.md) and repository release-gate/provider-I/O reports for currently implemented operations and remaining gates.

