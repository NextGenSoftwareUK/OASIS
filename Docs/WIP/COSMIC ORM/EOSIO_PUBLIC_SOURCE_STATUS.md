# EOSIO / Vaulta public account source status

Updated 11 November 2026. Scoped hosted HTTP and public browser acceptance are complete. Original-owner private paired browser verification and native storage parity remain open.

## Verified contract

- Anonymous native endpoint `https://vaulta.greymass.com/`; chain `aca376f206b8fc25a6ed44dbdc66547c36c6c33e3a119ffbeaef943642f0e906`. Separate from Vaulta EVM17777 and credentialed EOSIO storage.
- Original guarded qualification:7HTTP200 reads, three accounts then two (`eosio`, `eosio.token`, `eosio.msig`). Offline parser/status check passes without native calls.
- Separate read-only CQL source: account-name equality/bounded membership, paging after complete validation, exact integer strings, explicit account/chain identity, cancellation/errors without partial rows.28controlled tests pass.
- Actual C# CLI/manager/LINQ3/2 pass. Core permission1/API admission2/localHTTP1/publisher9/prepared-planner1/client preparation1 pass. Final API rebuild/admission2 pass. No legacy implementation changed.
- Local HTTP native3/2, Mongo5/4, foreign-private0/public3 verified;2SDK version1 saves/9query receipts. Exact marked `cosmic_cql_eosio_http_demo` dropped, HTTP consumers/Mongo stopped. Original local journal retained; no replay.

## Hosted release and acceptance

- Development parent `4d3e0b7cd148bed7ebdf0400ca371867873932c0` / Core `86e38a37ee860785a8273ade3308f6fc1775294d` pushed/ref-verified; manifest validates. Code job `68db832f-1742-423d-9362-febcf9620466` SUCCESS.
- Exactly3settings applied once: source, independent read permission/free tariff,4presets. Full candidate/per-key digests match; unrelated DNA unchanged. Single settings job `20b3a558-1139-429d-aaa3-7a01f1191591` SUCCESS. No reapply/redeploy.
- Original hosted cohort: native3/2, Mongo5/4, repeat0new/2existing, foreign-private0/public3, exact matching replicas2/2 each on SQLite/Neo4j.2normal private saves/version1, no pending preparation intents. Original immutable completion ACK freezes this cohort; no replay/resave/new visitor for the completed gate.
- Website main/task `72dee689bb0df5eb7c27ff0c429a04f88d7756e4` published/ref-verified.96client tests pass. Public HTML166985B/SHA256 `fd9bdb5915f65fd8c12d295c859312f21916b6d99a5bb27e07539146ce9c4149`; HTML and both unchanged JS assets match exact git blobs. This is HTTP/asset proof, not browser interaction.
- [Setup/C#/HTTP guide](../../Devs/EOSIO_PUBLIC_ACCOUNT_CQL_GUIDE.md), [commented walkthrough](../../../Examples/CosmicCqlDemo/EosioPublicWalkthrough.cs). Four native/Mongo presets available through discovery and the sandbox CQL tab.

## Limits and remaining work

Latest state is per account response, not a common finalized snapshot. Original identity heads528551466 lagged account heads528551467/468; no common-block freshness or independent consensus guarantee. No token balances are inferred. Generic browser Mongo copies preserve identity/name/description/provenance, not all native metadata; full payload mapping remains red in AFTER_DEMO_BACKLOG.md. Native signing/writes/full ORM and browser interaction remain separate gaps. Legacy7unimplemented throws are documented only.

Next: preserve this completed scope and qualify the next red shipping provider, starting with ChainLink public feed metadata. Require primary contract/catalogue evidence and a bounded native read contract before CQL implementation. Do not harden green scopes or reopen deep LINQ parity.

## Durable state

- Stable catalogue/native ACKs: `LocalState/COSMIC/HostedEosioPublicSourceCurrent`.
- Cleaned local fixture journal: `LocalState/COSMIC/EosioMongoHttpCurrent`.
- Protected baseline/candidate/key ACKs/redeploy intent+outcome: `LocalState/COSMIC/HostedSourceRolloutCurrent/eosio-public-*`.
- Public release/job ACKs: `LocalState/COSMIC/HostedEosioReleaseCurrent`.
- Hosted protected visitor identities/save ACKs/completion ACK: `LocalState/COSMIC/HostedQueryAcceptance-EOSIOOASIS`. Preserve through reviewed expiry; never print credentials or recreate owners.
- Raw evidence remains in worktree `TestResults/`, not Docs. Initial wrapper/return-type build failures retained; corrected builds pass. Source/ACKs are not disposable test output.

## Evidence hashes

- `LocalState/COSMIC/HostedEosioPublicSourceCurrent/source.json`: 279 bytes; SHA256 `94569948dc6f9295bb3e4b1beceade1cb7e9bcba5fc9b8ac46be4054bcdfad2f`.
- `TestResults/EosioPublicSourceCurrent/qualification.json`: 2904 bytes; SHA256 `f0f3efb434cbb1f038ee9f1a6022dc952397575ad12ed1289f95d3f89526985f`.
- `TestResults/EosioPublicSourceCurrent/controls.trx`: 43194 bytes; SHA256 `4f9a7cd31c017b70aae4960ab99fa031be9460baecfe3e499628e113afcdf793`.
- `TestResults/EosioPublicSourceCurrent/cql-console.log`: 988 bytes; SHA256 `8c090e7172c34585df26dae3dddf25137534d0c4b9af85ba322fe0f3ac2bb7e4`.
- `TestResults/EosioPublicSourceCurrent/demo-fixed-build.log`: 10959 bytes; SHA256 `b4e6c55601566ff1d79a7def548b4228d13ebb7824aa0dbe81b7f9b8a2a87e0c`.
- `TestResults/EosioPublicSourceCurrent/core-admission.trx`: 3294 bytes; SHA256 `10a89c8ad8fd8f38cde3b8285818f902d083d6283aa2964e9df7f3c91ef7a298`.
- `TestResults/EosioPublicSourceCurrent/api-admission.trx`: 4938 bytes; SHA256 `8d7b25c18126bdfe79ee86632a65bb44f75c8c3b07992499ee4dd8f332982d7c`.
- `TestResults/EosioPublicSourceCurrent/mongo-http-native.trx`: 3353 bytes; SHA256 `cb31d21cc409c3a5e46cdf1fd3b72c8abe173e8d87342f0fee6e3c81d509f70c`.
- `TestResults/EosioPublicSourceCurrent/prepared-planner.trx`: 3329 bytes; SHA256 `8de1aff7235b9b72ce756085c2a470d0e0ade441bcb90211c08ba6c2abd3cf33`.
- `TestResults/EosioPublicSourceCurrent/eosio-mongo-native-summary.json`: 223 bytes; SHA256 `66b9b5d7c2ba0c065b3c4ad75075a2a79b248479db3c9fbef43d54e39d2bdc31`.
- `TestResults/EosioPublicSourceCurrent/prepared-public-review.json`: 3633 bytes; SHA256 `edf447e1482573ecb5bec41b8b927e4284f7bd1b1e76ca7fbe8e1d522d205cba`.
- `TestResults/EosioPublicSourceCurrent/api-final-admission.trx`: 4938bytes; SHA256 `9f6cdca9e8de5c6cacd6ffe39c89319f40753fbb230df6c1bbfc40bb305def99`.
- `TestResults/EosioPublicSourceCurrent/client-tests.log`: 9254bytes; SHA256 `7f3a00deee4b85d42c5332206981f6dec42064820965064bb7eb8c9fa073893f`.

- `TestResults/HostedCqlIntegrationCurrent/EOSIOOASIS-native-http.json`: 12697bytes; SHA256 `ee380cb816e63bd365a2f4fbb6ea316a896afa532127d9c770032744201673cc`.

- `TestResults/HostedCqlIntegrationCurrent/EOSIOOASIS-client-http-flow.json`: 34111bytes; SHA256 `8b74015e7f87c7c50b414fb002699b6e6dfd4822541ab66e6e7342d987a31ce3`.

- `TestResults/HostedCqlIntegrationCurrent/EOSIOOASIS-isolation-http.json`: 4775bytes; SHA256 `a01dfa23bf162f23de4aebb8562ea09a8ad90070c270f7c2d2538315256ae8fc`.

Primary references: [Vaulta reading state](https://docs.vaulta.com/docs/latest/web-applications/reading-state/), [native Chain API](https://docs.eosnetwork.com/apis/spring/latest/chain.api/).

### Live public browser acceptance— 10 November 2026

A fresh temporary WEB4 demo session on Development ran both EOSIOOASIS public presets in CQL Playground. The browser rendered three rows for the full account preset and two for the subset, labeled EOSIOOASIS. The original prepared cohort was preserved. Private MongoDB paired browser acceptance remains open.

11 November UTC: Railway WEB4 deployment `10675f44-7aab-4589-83d7-730d248638a1` at EOSIO wallet revision `3f5d781f29` succeeded. Authenticated discovery progressed 70/106 to 106/106 ready. EOSIO, Hedera and the other listed priority sources and LocalFile reported native and federated readiness. TRON and Supabase remain absent. This is CQL readiness evidence; the live wallet test is in the HyperDrive handoff.

11 November UTC: the documented original EOSIO owner journal and HTTP reports are still absent from this recovery checkout. Public browser acceptance and prior hosted HTTP/replica receipts remain recorded. Original-owner private browser verification requires recovering that identity and confirming the existing two copies. HyperDrive's contract artifact CI is separate.

11 November UTC: HyperDrive's [Antelope contract build](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43025526058) passed the ABI build/validation and uploaded the antelope-oasis-contract artifact (13857555218, 8,222 bytes) at d4c06c9c91. This is build evidence. Dedicated live deployment and storage lifecycle remain open.

11 November UTC: Development a5e8fc36c4 includes the verified Antelope contract build workflow and ABI/WASM artifacts. Native live deployment and storage lifecycle remain open.
