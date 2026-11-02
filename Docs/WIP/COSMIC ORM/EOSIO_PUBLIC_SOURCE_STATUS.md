# EOSIO / Vaulta public source qualification

Updated 1 November 2026. Native qualification and local CQL/actual C# CLI verified; API/hosted integration remains unimplemented.

## Verified

- The documented public endpoint `https://vaulta.greymass.com/` returned the expected native chain identity `aca376f206b8fc25a6ed44dbdc66547c36c6c33e3a119ffbeaef943642f0e906`.
- Seven guarded, bounded read-only requests succeeded: identity, three account reads, two account reads, identity. Accounts: `eosio`, `eosio.token`, `eosio.msig`.
- Both selections returned their exact requested identities. No wallet, signed transaction, source write, new installation or hosting resource.
- Offline parser regression passes: one response object, preserved creation timestamp and integer above 2^53, HTTP rejection without retry; zero native requests.
- Scripts: [qualification](../../../Scripts/Sandbox/Probe-EosioPublicReadiness.ps1), [offline check](../../../Scripts/Sandbox/Test-EosioPublicProbe.ps1).

## CQL implementation milestone

- Separate `EosioPublicAccountMetadataSource` in the existing EOSIO project; legacy storage unchanged.
- 28 controlled tests pass, zero failures/skips: key-set/point/window selection, exact integers above2^53, account/chain identity, timestamp shape, status/native/duplicate/size errors, cancellation, result budget, no partial publication and read-only saves.
- Producer and actual console builds pass. First console build caught the new top-level branch returning no integer; corrected to `return 0`, rebuilt successfully. Original failure retained. Initial producer wrapper had a shell-quoting error after the successful compiler result; later batches use direct scriptblocks and checked exit codes.
- Actual CLI/manager/LINQ returned3full/2filtered native accounts. No source transaction or Mongo save. Durable `cql-console.intent.json` and `cql-console.outcome.json` preserve this completed read gate; no replay needed.
- [Developer guide](../../Devs/EOSIO_PUBLIC_ACCOUNT_CQL_GUIDE.md), [commented C# demo](../../../Examples/CosmicCqlDemo/EosioPublicWalkthrough.cs).

## Limits and next work

These are latest native state per account, not a shared finalized snapshot. Identity responses reported head528551466 while account responses reported528551467/468. No common-block freshness guarantee. Native EOSIO/Vaulta is separate from Vaulta EVM17777. Legacy storage gaps remain documented, not modified.

Next: wire explicit query-only API admission and configuration, add API tests, prove local Mongo pairing/privacy and actual prepared presets, then hosted rollout/acceptance and a sandbox card. No EOSIO hosted or browser claim yet.

## Evidence

Original intent/catalogue remain in `LocalState/COSMIC/HostedEosioPublicSourceCurrent`; never regenerate their owner or replay the native qualification. Raw responses stay under worktree `TestResults/EosioPublicSourceCurrent`, not Docs.

- `LocalState/COSMIC/HostedEosioPublicSourceCurrent/source.json`: 279 bytes; SHA256 `94569948dc6f9295bb3e4b1beceade1cb7e9bcba5fc9b8ac46be4054bcdfad2f`.
- `TestResults/EosioPublicSourceCurrent/qualification.json`: 2904 bytes; SHA256 `f0f3efb434cbb1f038ee9f1a6022dc952397575ad12ed1289f95d3f89526985f`.

Primary references: [Vaulta reading state](https://docs.vaulta.com/docs/latest/web-applications/reading-state/), [native Chain API](https://docs.eosnetwork.com/apis/spring/latest/chain.api/).

- `TestResults/EosioPublicSourceCurrent/controls.trx`: 43194 bytes; SHA256 `4f9a7cd31c017b70aae4960ab99fa031be9460baecfe3e499628e113afcdf793`.

- `TestResults/EosioPublicSourceCurrent/cql-console.log`: 988 bytes; SHA256 `8c090e7172c34585df26dae3dddf25137534d0c4b9af85ba322fe0f3ac2bb7e4`.

- `TestResults/EosioPublicSourceCurrent/demo-fixed-build.log`: 10959 bytes; SHA256 `b4e6c55601566ff1d79a7def548b4228d13ebb7824aa0dbe81b7f9b8a2a87e0c`.
