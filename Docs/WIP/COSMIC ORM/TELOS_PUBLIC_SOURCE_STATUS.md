# Telos EVM public CQL candidate

1 November 2026. Protocol qualified only. No Telos CQL implementation/admission/hosted claim or count increment. Filecoin hosted acceptance remains the immediate priority.

## Verified native contract

- Current official endpoint: `https://rpc.telos.net/`, mainnet chain40 (`0x28`). Telos documents the old `/evm` endpoints as legacy; this new representation does not change legacy provider code.
- Observed canonical genesis: `0x36fe7024b760365e3970b7b403e161811c1e626edd68460272fcdfa276272563`.
- Shipping .NET transport with Core NativeConnectionGuard and public-address checks succeeds. A finalized header and three balances bound to its hash using EIP1898 `requireCanonical:true` pass. No latest downgrade, endpoint switch, user-agent spoofing or guard bypass.
- One fixture balance is nonzero. Quantities remain hex in diagnostic evidence; the shared reader will map them to exact decimal strings. Finality is RPC-reported, not independent consensus verification.

## Preserved diagnostic history

Python urllib received HTTP403/error1010 before chain identity. That does not establish that the shipping reader cannot connect. The guarded .NET response returned HTTP200 and correct chain0x28, but the probe initially miscompared it because LoadIntoBufferAsync.GetResult emitted a VoidTaskResult object into the PowerShell pipeline.

Offline reproduction proved the root cause. The helper now suppresses that void result; its offline regression asserts exactly one string result and one recorded request. An explicit journalled continuation reused the captured chain response and continued with genesis/finalized/balance reads. It did not replay chain identity or overwrite either original failure.

## Exact next step after Filecoin

Add a thin TelosPublicAccountMetadataSource descriptor over the existing EvmPublicAccountMetadataSource: ProviderType.TelosOASIS, chain0x28, the pinned genesis above, root RPC path and TelosAccountAddress key. Existing finalized/hash-bound behavior should require no shared algorithm change. Add Filecoin-style controlled tests, real HolonManager/LINQ3/2, readable C# walkthrough and CLI, then API/admission/local Mongo/privacy and hosted/card gates. Do not replay protocol qualification or recreate the retained catalogue owner.

This will represent public Telos EVM balances, not native Antelope account names, EOSIO contract storage, native writes or full ORM parity. Counts remain46integrated/42hosted/browser5/open37. Keep green scopes frozen; V2 changes only, legacy gaps documentation-only.

## Evidence and retained state

- `TestResults/TelosPublicSourceCurrent/GuardedDotNetContinuation/qualification.json`: 2391bytes; SHA256 `2897b09b77b9405cae82883152087205307e0b7f6d9e167e9db7883a60d4f63b`.
- Stable `LocalState/COSMIC/HostedTelosPublicSourceCurrent/source.json`: 314bytes; SHA256 `ccb1d5f2d365399fb7f879a07b2bd6bb5951bcaefa386f2741cddae7bc48be83`.
- Original Python failure: TestResults/TelosPublicSourceCurrent/qualification.json.
- Original correct chain reply and false probe comparison: GuardedDotNet/1-eth_chainId.json and qualification.json.
- Three durable qualification/continuation intents under the same LocalState directory; retain all, never replay. Offline helper output is regenerable under TestResults/TelosPublicSourceCurrent/Offline.
- Offline regression: `pwsh -NoProfile -File Scripts/Sandbox/Test-TelosGuardedProbe.ps1` passes1contract. No network/provider activation/DB/source writes.

[Official network and current endpoint](https://docs.telos.net/build/network-info/) · [Legacy endpoint guidance](https://docs.telos.net/evm/smart-contracts/hardhat_sourcify/) · [Resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
