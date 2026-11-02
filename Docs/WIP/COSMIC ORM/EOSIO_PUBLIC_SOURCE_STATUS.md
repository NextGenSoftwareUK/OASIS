# EOSIO / Vaulta public source qualification

Updated 1 November 2026. Native qualification and local CQL/actual C# CLI verified; API admission/local HTTP federation verified; hosted rollout remains pending.

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

Next: publish the verified Core/parent code batch to Development, observe its exact deployment, apply the reviewed three-key configuration once, observe that deployment, then run the original hosted acceptance cohort and publish its sandbox card. No EOSIO hosted or browser claim yet.

## API / local HTTP milestone

- Lazy anonymous `CQL_EOSIO_PUBLIC_SOURCE_JSON` registration, explicit read-profile compiler admission and separate Core unsigned-read tariff. No signed write tariff/default storage change. Core permission test1 and API admission/discovery tests2 pass.
- Real loopback HTTP test1 passes: native3/2, Mongo-preserving federation5/4, foreign-private0/foreign-public3; validates every returned identity, provider, visibility, chain and per-response head marker. Two SDK saves/version1, nine measured query receipts. Controlled auth/billing infrastructure is not production billing proof or replication acceptance.
- Exact owned database `cosmic_cql_eosio_http_demo` dropped after marker/count/identity checks, HTTP consumers stopped, owned Mongo stopped/27029 absent. Original journal and save acknowledgements retained under `LocalState/COSMIC/EosioMongoHttpCurrent`; do not rerun/reseed.
- Publisher9pure tests pass; candidate changes only source, explicit read admission/tariff and four examples. Original source-bound native/local ACKs retained. Protected baseline/candidate `LocalState/COSMIC/HostedSourceRolloutCurrent/eosio-public-*`; no settings applied yet.
- Actual candidate planner1 passes for all4presets against shipping source/Mongo capabilities. Frontend EOSIO preparation test1 passes: two private Mongo identity/name/description/provenance copies, repeat0new/2existing, no native write. Full native metadata mapping remains a separate red backlog item.
- Core task commit `86e38a37ee860785a8273ade3308f6fc1775294d` pushed and remote-ref verified; parent gitlink/manifest updated together. No Development deployment claimed yet.


## Evidence

Original intent/catalogue remain in `LocalState/COSMIC/HostedEosioPublicSourceCurrent`; never regenerate their owner or replay the native qualification. Raw responses stay under worktree `TestResults/EosioPublicSourceCurrent`, not Docs.

- `LocalState/COSMIC/HostedEosioPublicSourceCurrent/source.json`: 279 bytes; SHA256 `94569948dc6f9295bb3e4b1beceade1cb7e9bcba5fc9b8ac46be4054bcdfad2f`.
- `TestResults/EosioPublicSourceCurrent/qualification.json`: 2904 bytes; SHA256 `f0f3efb434cbb1f038ee9f1a6022dc952397575ad12ed1289f95d3f89526985f`.

Primary references: [Vaulta reading state](https://docs.vaulta.com/docs/latest/web-applications/reading-state/), [native Chain API](https://docs.eosnetwork.com/apis/spring/latest/chain.api/).

- `TestResults/EosioPublicSourceCurrent/controls.trx`: 43194 bytes; SHA256 `4f9a7cd31c017b70aae4960ab99fa031be9460baecfe3e499628e113afcdf793`.

- `TestResults/EosioPublicSourceCurrent/cql-console.log`: 988 bytes; SHA256 `8c090e7172c34585df26dae3dddf25137534d0c4b9af85ba322fe0f3ac2bb7e4`.

- `TestResults/EosioPublicSourceCurrent/demo-fixed-build.log`: 10959 bytes; SHA256 `b4e6c55601566ff1d79a7def548b4228d13ebb7824aa0dbe81b7f9b8a2a87e0c`.

- `TestResults/EosioPublicSourceCurrent/core-admission.trx`: 3294 bytes; SHA256 `10a89c8ad8fd8f38cde3b8285818f902d083d6283aa2964e9df7f3c91ef7a298`.

- `TestResults/EosioPublicSourceCurrent/api-admission.trx`: 4938 bytes; SHA256 `8d7b25c18126bdfe79ee86632a65bb44f75c8c3b07992499ee4dd8f332982d7c`.

- `TestResults/EosioPublicSourceCurrent/mongo-http-native.trx`: 3353 bytes; SHA256 `cb31d21cc409c3a5e46cdf1fd3b72c8abe173e8d87342f0fee6e3c81d509f70c`.

- `TestResults/EosioPublicSourceCurrent/prepared-planner.trx`: 3329 bytes; SHA256 `8de1aff7235b9b72ce756085c2a470d0e0ade441bcb90211c08ba6c2abd3cf33`.

- `TestResults/EosioPublicSourceCurrent/eosio-mongo-native-summary.json`: 223 bytes; SHA256 `66b9b5d7c2ba0c065b3c4ad75075a2a79b248479db3c9fbef43d54e39d2bdc31`.

- `TestResults/EosioPublicSourceCurrent/prepared-public-review.json`: 3633 bytes; SHA256 `edf447e1482573ecb5bec41b8b927e4284f7bd1b1e76ca7fbe8e1d522d205cba`.

Final API producer rebuild passes; final admission2/2 pass after preserving original UTF8 text outside intended EOSIO additions.

- `TestResults/EosioPublicSourceCurrent/api-final-admission.trx`: 4938bytes; SHA256 `9f6cdca9e8de5c6cacd6ffe39c89319f40753fbb230df6c1bbfc40bb305def99`.
