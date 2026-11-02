# Telos EVM public CQL candidate

1 November 2026. Native CQL/manager/commented C# and actualCLI3/2 are verified. HTTP admission, Mongo and hosted gates remain open. No hosted claim.

## Verified native contract

- Current official endpoint: `https://rpc.telos.net/`, mainnet chain40 (`0x28`). Telos documents the old `/evm` endpoints as legacy; this new representation does not change legacy provider code.
- Observed canonical genesis: `0x36fe7024b760365e3970b7b403e161811c1e626edd68460272fcdfa276272563`.
- Shipping .NET transport with Core NativeConnectionGuard and public-address checks succeeds. A finalized header and three balances bound to its hash using EIP1898 `requireCanonical:true` pass. No latest downgrade, endpoint switch, user-agent spoofing or guard bypass.
- One fixture balance is nonzero. Quantities remain hex in diagnostic evidence; the shared reader will map them to exact decimal strings. Finality is RPC-reported, not independent consensus verification.

## Preserved diagnostic history

Python urllib received HTTP403/error1010 before chain identity. That does not establish that the shipping reader cannot connect. The guarded .NET response returned HTTP200 and correct chain0x28, but the probe initially miscompared it because LoadIntoBufferAsync.GetResult emitted a VoidTaskResult object into the PowerShell pipeline.

Offline reproduction proved the root cause. The helper now suppresses that void result; its offline regression asserts exactly one string result and one recorded request. An explicit journalled continuation reused the captured chain response and continued with genesis/finalized/balance reads. It did not replay chain identity or overwrite either original failure.

## Native CQL and developer demo complete

Thin Telos descriptor over unchanged EvmPublicAccountMetadataSource binds ProviderType.TelosOASIS, chain0x28, pinned genesis, root path and TelosAccountAddress.18new controlled tests pass/0skips; wrong chain/genesis/header/envelope, partial/overflow data, precision, paging, cancellation, unsupported keys/scans/writes and credential configuration are covered. Existing green source network tests were not rerun; no shared algorithm change.

One actual published C# test passes3/2/0skips through HolonManager/LINQ. Actual --telos-public console3/2 also passes. Readable guide: Docs/Devs/TELOS_PUBLIC_ACCOUNT_CQL_GUIDE.md. Source-bound native-qualified ACK freezes the original catalogue. First consumer compile failed before tests because the provider had not been rebuilt; proper provider-before-consumer order fixed it. Original controls-build.log is preserved; corrected controls-fixed-build.log succeeds. No native request ran on the compile failure.

## Exact next step

Implement lazy CQL_TELOS_PUBLIC_SOURCE_JSON read-profile registration, independent query-only tariff/permission, four shipping-planned presets and local HTTP/Mongo5/4/privacy0/3 with original operation journals/exact-owned cleanup. Then protected candidate, exact code deployment, guarded single settings rollout, first normal visitor/reuse/replica cohort and sandbox card/public-byte proof. Do not replay native qualification/C# or recreate owner/address data.

Telos represents public EVM balances, not Antelope accounts/EOSIO storage/native writes or full ORM parity. Counts47integrated/42hosted/browser5/open37. Keep green scopes frozen; V2 implementation only, legacy gaps documented only.

## Evidence and retained state

- `TestResults/TelosPublicSourceCurrent/GuardedDotNetContinuation/qualification.json`: 2391bytes; SHA256 `2897b09b77b9405cae82883152087205307e0b7f6d9e167e9db7883a60d4f63b`.
- Stable `LocalState/COSMIC/HostedTelosPublicSourceCurrent/source.json`: 314bytes; SHA256 `ccb1d5f2d365399fb7f879a07b2bd6bb5951bcaefa386f2741cddae7bc48be83`.
- Original Python failure: TestResults/TelosPublicSourceCurrent/qualification.json.
- Original correct chain reply and false probe comparison: GuardedDotNet/1-eth_chainId.json and qualification.json.
- Three durable qualification/continuation intents under the same LocalState directory; retain all, never replay. Offline helper output is regenerable under TestResults/TelosPublicSourceCurrent/Offline.
- Offline regression: `pwsh -NoProfile -File Scripts/Sandbox/Test-TelosGuardedProbe.ps1` passes1contract. No network/provider activation/DB/source writes.

[Official network and current endpoint](https://docs.telos.net/build/network-info/) · [Legacy endpoint guidance](https://docs.telos.net/evm/smart-contracts/hardhat_sourcify/) · [Resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).

- `TestResults/TelosPublicSourceCurrent/telos-controls.trx`: 28748bytes; SHA256 `520b37491a6be1aff40b47067924da3fed0d9c44c3155c8d067e87d5a856172f`.

- `TestResults/TelosPublicSourceCurrent/telos-native.trx`: 2843bytes; SHA256 `62b49f2ed2601c5fc3bb860f07af8e1f8676875f2e6aa3e383ec53e06394bac6`.

- `TestResults/TelosPublicSourceCurrent/console-native.log`: 1440bytes; SHA256 `4f4891e85e1923443350181133f30fa1ef86a286558a79b89450801a3bd8b780`.
