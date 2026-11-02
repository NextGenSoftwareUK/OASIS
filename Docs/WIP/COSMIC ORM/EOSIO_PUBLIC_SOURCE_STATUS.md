# EOSIO / Vaulta public source qualification

Updated 1 November 2026. Native qualification only; CQL and hosted integration remain unimplemented.

## Verified

- The documented public endpoint `https://vaulta.greymass.com/` returned the expected native chain identity `aca376f206b8fc25a6ed44dbdc66547c36c6c33e3a119ffbeaef943642f0e906`.
- Seven guarded, bounded read-only requests succeeded: identity, three account reads, two account reads, identity. Accounts: `eosio`, `eosio.token`, `eosio.msig`.
- Both selections returned their exact requested identities. No wallet, signed transaction, source write, new installation or hosting resource.
- Offline parser regression passes: one response object, preserved creation timestamp and integer above 2^53, HTTP rejection without retry; zero native requests.
- Scripts: [qualification](../../../Scripts/Sandbox/Probe-EosioPublicReadiness.ps1), [offline check](../../../Scripts/Sandbox/Test-EosioPublicProbe.ps1).

## Limits and next work

These are latest native state per account, not a shared finalized snapshot. Identity responses reported head528551466 while account responses reported528551467/468. No common-block freshness guarantee. Native EOSIO/Vaulta is separate from Vaulta EVM17777. Legacy storage gaps remain documented, not modified.

Next: implement a separate guarded read-only account-metadata CQL source in the existing EOSIO provider project. Prove controlled parsing/identity/selection/error behavior, native manager and C#3/2, then API admission, local pairing/privacy and hosted acceptance before publishing a sandbox card. Do not count qualification as CQL support.

## Evidence

Original intent/catalogue remain in `LocalState/COSMIC/HostedEosioPublicSourceCurrent`; never regenerate their owner or replay the native qualification. Raw responses stay under worktree `TestResults/EosioPublicSourceCurrent`, not Docs.

- `LocalState/COSMIC/HostedEosioPublicSourceCurrent/source.json`: 279 bytes; SHA256 `94569948dc6f9295bb3e4b1beceade1cb7e9bcba5fc9b8ac46be4054bcdfad2f`.
- `TestResults/EosioPublicSourceCurrent/qualification.json`: 2904 bytes; SHA256 `f0f3efb434cbb1f038ee9f1a6022dc952397575ad12ed1289f95d3f89526985f`.

Primary references: [Vaulta reading state](https://docs.vaulta.com/docs/latest/web-applications/reading-state/), [native Chain API](https://docs.eosnetwork.com/apis/spring/latest/chain.api/).
