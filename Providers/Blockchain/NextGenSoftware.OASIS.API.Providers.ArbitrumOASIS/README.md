# NextGen Software WEB4 OASIS API Arbitrum OASIS Provider

## Verification and privacy gate

`ArbitrumOASIS` is the chain-specific legacy implementation. Its current save paths serialize avatar/detail/holon JSON directly before contract submission; authenticated private-record encryption and privacy-safe searchable indexes are not established for this path. Do not enable it for private records until that implementation gap is fixed and tested. This is a source audit warning, not a runtime enforcement switch or a claim that existing publications were removed.

`ArbitrumOASIS_Web3Core` is a separate implementation requiring an explicitly configured compatible Web3Core contract. Its inherited functionality does not certify the legacy ABI, the default provider path, or any hosted Arbitrum deployment. Do not silently switch ABI/contract implementations after a failure.

Legacy unit fixtures now use loopback instead of public mainnet RPC. Identity-only test success is not native storage/policy/recovery proof. See `Docs/Devs/HYPERDRIVE_V2_CURRENT_RELEASE_GATES.md` in the parent repository for the current evidence and remaining gates.
