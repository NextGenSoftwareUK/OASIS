# NextGen Software WEB4 OASIS API Arbitrum OASIS Provider

## Verification and privacy gate

The existing `ArbitrumOASIS_Web3Core` wrapper now has actual local-EVM coverage in the retained shared runner (`Scripts/TestHosts/Run-ReusableEthereumTests.ps1`): private avatar/detail round trips and inspection of native payload bytes, plus the same V2/SQLite policy fixture used for Polygon. The fixture now uses the supplied provider's real identity rather than hardcoded Polygon metrics/selection IDs. Web3 native scenarios: 4/4 passed, zero skips (2026-10-16), on loopback chain 31337 with an explicitly deployed compatible Web3Core contract. This is not an Arbitrum One/Nova/Sepolia deployment or rollup-finality test, nor certification of all NFT/interfaces. No ABI fallback or default-provider switch was added.

`ArbitrumOASIS` is the chain-specific legacy implementation. Its current save paths serialize avatar/detail/holon JSON directly before contract submission; authenticated private-record encryption and privacy-safe searchable indexes are not established for this path. Do not enable it for private records until that implementation gap is fixed and tested. This is a source audit warning, not a runtime enforcement switch or a claim that existing publications were removed.

`ArbitrumOASIS_Web3Core` is a separate implementation requiring an explicitly configured compatible Web3Core contract. Its inherited functionality does not certify the legacy ABI, the default provider path, or any hosted Arbitrum deployment. Do not silently switch ABI/contract implementations after a failure.

Legacy unit fixtures now use loopback instead of public mainnet RPC. Identity-only test success is not native storage/policy/recovery proof. See `Docs/Devs/HYPERDRIVE_V2_CURRENT_RELEASE_GATES.md` in the parent repository for the current evidence and remaining gates.
