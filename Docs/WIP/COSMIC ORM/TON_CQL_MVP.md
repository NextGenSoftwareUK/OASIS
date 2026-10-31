# TON native admission — contract controls pass; provider/CQL demo red

Updated 30 October 2026. This is development evidence, not a claim of native TON provider completion.

## Root issue and correction

The original `TONOASIS` C# class inherited the EVM `Web3CoreOASIS` provider. TON executes TVM contracts, so that inheritance and the previous DNA/README/package claims did not prove TON CRUD, wallet, NFT, replication or CQL support. The native backend now replaces that inheritance, mandatory shared private storage is retained, and the obsolete EVM constructor explicitly rejects its configuration. The fictitious endpoint example stays disabled. Native integration does not relabel an EVM run or add a fallback.

A second bug was reproduced: the shared EVM adapter advertised ID-query capabilities for TON even though its compiler rejected that authority. `Web3CoreCqlCompiler` now owns one authority whitelist used by both capability reporting and compilation. The EVM base rejects TON ID queries during planning; the new native TON adapter advertises its own proved bounded profile. Positive Polygon/Arbitrum planning and compilation controls pass. This does not certify every unsupported plan shape or add native TON execution.

## Native TVM foundation verified

`Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.TONOASIS/Contracts/TonHolonStore.tact` implements an immutable owner, uint256-keyed cell values and owner-only put/delete messages. The tests execute compiled TVM bytecode, not an EVM or a dictionary mock.

- Deployment binds the owner; a missing key returns null.
- Three records preserve Unicode, a precise large-number string and a chained 4 KiB payload.
- Native getters leave stored contract data unchanged.
- Owner update/delete affect the selected key; other records remain intact.
- Foreign put/delete abort with the contract's exact `OWNER_ONLY` exit code, leaving contract data unchanged.
- A separate Node process reloads serialized TVM state and reads the existing values/missing key without redeployment or reseeding.

Run from the worktree:

```powershell
./Scripts/TestHosts/Run-CosmicTonContract.ps1 -Storage
```

The runner owns the provider build lease, compiles in virtual files and writes generated artifacts/TAP only to `TestResults/TonCqlCurrent`. Its temporary Node dependency search path is restored on exit. No public chain, real account, paid tokens, listening service, production key or persistent demo source is created.

## Evidence

Paths are under `TestResults/TonCqlCurrent`; raw files are not committed to Docs.

| Proof | Result | SHA256 |
| --- | --- | --- |
| `contract-tests.tap` | 5 native subcases + parent: 6 passed, no skips | `97E6E478A54FF5EE3B7FCD7D13AB1A2C83D025DD9D1B157E4B3B77B9025DE5AB` |
| `Archive/AAEB2265C8AE71B197C15AE76DDA55596ABE088ED17DD64C35875A9C7B361CF1-TonHolonStore_TonHolonStore.code.boc` | Initial compiled TVM code | filename hash |
| `ton-capability-before.trx` | 1 genuine planning failure before fix | `810E15CE471E71FBC25E04EFDB1429824E86FAD9C1CBE37BB941CBC78D311F80` |
| `ton-capability-fixed.trx` | 8 controls passed, no skips | `6DB195CF75D096B273E3E1E3B460028BD7538A1B4618E29D2F0451D9BEB48DB1` |

Earlier six-control run `ton-admission-controls.trx` proved only execution rejection; the later regression exposed and fixed planning admission. Do not confuse it with native CQL execution.

The reproducible public-test snapshot (285,773 bytes, SHA256 `AD74FF7B695C39DFBE17769655B8FC5B28A8E5910F0CC3E3C0E92FF5DBF8FF95`) was removed after the child and runner exited. The named contract runner regenerates it. Compiled outputs and compact reports remain regenerable; source, lock and reusable dependencies are retained.

## Reused tooling and limits

One SDK tree: `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.TONOASIS/SdkBridge/node_modules`, 96,885,441 bytes / 7,332 files at setup. Node 24.19.0/npm11.17.0 already installed; no duplicate runtime. Pinned sandbox0.45.0/core0.63.1/crypto3.3.0/compiler1.6.13 were downloaded once because scoped caches had none. TypeScript5.9.3 was restored offline from cache. Package lock SHA256 `8CF690F9CC82BF574A3562044C37C0C3648E03538E59343C522EF2B59EFA1058`.

Tact/compiler dependencies emitted deprecation warnings; they are development tooling, not production support/security certification. Compiler maintenance/Tolk evaluation and production contract review remain open. The official [TON Sandbox documentation](https://docs.ton.org/v3/guidelines/smart-contracts/testing/writing-test-examples/) describes local TVM emulation and its lack of network blocks/sharding; the [SDK snapshot helpers](https://github.com/ton-blockchain/sandbox/blob/master/README.md) provide serialized reload. This proof makes no mainnet, consensus, finality or node-restart claim.

## Native SDK/C# storage milestone

The contract now exposes its native record dictionary for complete prefix enumeration required by shared private-storage verification. Earlier bytecode/TAP are hash-archived before that necessary integration change. Current SDK/source ownership code lives in `SdkBridge/sandbox-store.cjs` and `native-worker.cjs`; `TonSandboxStorageBackend.cs` is its bounded C# process client.

- Explicit `LocalTonTvmSandbox` source UUID and expected native code-cell hash; code/address/owner validation on every data operation. No public-network endpoint or protocol substitution.
- Immutable operation IDs, CreateNew pending intent before mutation, atomic snapshot replacement, flushed acknowledgement before pending removal, no automatic replay/reseed. Read-only operation inspection preserves pending outcomes.
- C# OS-owned `source.lock` lease serializes clients; SDK write intent fences interrupted mutations. Readers reject source changes before publishing; complete record/value/request/source/response budgets apply.
- Shared `PersistedPrivateKeyValueBackend` provides canonical key hiding/authenticated encryption. The C# test saves three real canonical holons through the original shared-storage probe (subsequently replaced by the actual adapter), then reopens through separate workers, verifies identities/names/creator and unchanged source bytes. Wrong encryption key, read-only mutation, pre-dispatch cancellation and competing OS lease are rejected.
- Exact SDK and C# synthetic fixtures/ACKs are verified and removed after child/runner exit; no persistent demo source or private credential has been created.

| Current evidence under `TestResults/TonCqlCurrent` | Result | SHA256 |
| --- | --- | --- |
| `Archive/7D8B4E1D25D3317713609C35A0080193AC15A75A179541210624FB3C8F18480D-native-storage-tests.tap` | Prior contract6 + SDK7 results; 13 passed/no skips | filename hash |
| `store-tests.tap` | Initial SDK7 passed/no skips | `17B0D02DABE5E75A1542885F7D8C3737B1F6C68B26F1127251B8E761B9D798F0` |
| `Archive/141F582A4951270CB652C1096B7FAEB062D4583098C8AA15720C5DD2F2E8628A-ton-native-storage.trx` | Prior C# probe scenario passed,18s | filename hash |
| `compiled/TonHolonStore_TonHolonStore.code.boc` | Current native dictionary/getter code | `A590947E35638A919AFDB2B6BD322DD9F08C69B9C32BB680971A5685B4D76A03` |

Receipt inspection regression: the first implementation accepted an ACK under a mismatched configured code hash. `store-inspection-before.tap` records the genuine failing subcase plus parent (2 failed results), SHA256 `C096C304C00EA9E0ACAD1D9F8CC21F7A7A5BBB4913E7A50A37E561A5CD77D872`. Pending intents and ACKs now carry code identity; SDK inspection and C# acknowledgements validate it. The complete13+1 acceptance above passed after the fix. Prior passing receipts are hash-archived.

The artifact SHA256 above differs from the TVM code-cell hash used for runtime identity. The runner computes the latter from the compiled BOC. SDK pending tests inject a clearly marked, never-submitted test intent; they do not prove recovery from every actual process/commit/ACK crash. Mid-flight cancellation/fault schedules, power-loss durability, journal retention/production packaging and signed real-network transport remain open. Physical snapshot serialization does not implement optimistic holon CAS; no conditional-write/HyperDrive parity is claimed.

## Actual adapter / CQL verification

`TONOASIS` now inherits shared `KeyValueStorageProviderBase` and uses one native backend/private-record owner. A lifecycle semaphore orders activation/deactivation/disposal with individual storage operations; inactive storage rejects before file/process access. Runtime verification authenticates the code/owner and existing encrypted records. Native production references no Web3Core adapter; only regression tests retain its reference. Non-native NFT/wallet/network interfaces are not advertised.

The actual adapter now passes encrypted three-holon save/load and independent worker reopen, native CQL full3/subset2/missing0, result-budget rejection, cancellation without partial data, inactive queries, read-only mutation denial, wrong-key/OS-lease rejection and unchanged source bytes/ACK count. Thirteen metadata/capability/lifecycle controls plus this native scenario pass: **14 tests, no skips**,23s. Unsupported name/metadata/order expressions are captured and rejected by planning before runtime access. CQL reuses the shared bounded compiler, canonical record key and private backend; queries do not enumerate the store.

Evidence `TestResults/TonCqlCurrent/ton-adapter-cql.trx`, SHA256 `71C876F8D7AD73DC3D075B7DA5E86E25B607F4A618C0772800DFD29BBE20418A`. Unchanged contract/SDK integration also passed13results: current `native-storage-tests.tap`, SHA256 `53AD9723E75D175C52C4858C63DE25BFC995F58A136F4BF93D08CECE4B418EC6`.

The first adapter test incorrectly duplicated IDs in its trusted catalogue and failed before CQL execution (native saves/loads already passed). Failed TRX `Archive/68E6E1FB2C188A110EF4AA6193EDAE620AAFBC2A29558EA01F2A53C53AD95362-adapter-catalogue-failed.trx` is retained. A separate explicitly opted-in maintenance test verified the original three known synthetic records/version/creator, every four ACK source/code/operation identities, no pending outcome and unchanged source bytes before exact teardown: `ton-fixture-inspection.trx`, SHA256 `4E6B391E0565D71A71F91EA25999BEBEA9429309F9E6ABD2166D40EBE1AF9ED7`,1passed. The corrected catalogue is distinct and checked for capture errors. No production source was reset/replayed.

## Retained source, federation and browser milestone

One reusable owned source is now retained in `LocalState/COSMIC/TonCql`: CurrentUser-DPAPI key, immutable source/actor/three-ID seed, native snapshot and four physical operation ACKs. Setup writes a durable intent before deployment/saves; interrupted setup or missing original key refuses retry/reseed. Size at checkpoint:10files/221,347bytes; protected key262bytes. These files stay outside TestResults and must be retained together.

```powershell
# Native verification / independent reopen of the existing source:
./Scripts/TestHosts/Run-CosmicTonCqlNative.ps1

# Mongo federation and browser demo; open http://127.0.0.1:5109/ when ready:
./Scripts/TestHosts/Run-CosmicTonCqlNative.ps1 -Federated -Serve
```

Initial native setup/query passed1test14s; a separate full runner invocation reopened the same source and passed1test8s. Hashes of source, seed, protected key and every ACK remain identical across that reopen. Current seed SHA256 `8682D1EA05A4C5C4B5FB6CE57B0960BC1678C4F8E4C9D0AEEE3A115C75A51E7D`; native source JSON SHA256 `BFA3DF9BD77229FBB0804BEE553CEF851B8B577BF937F186781ABD6E75312DAA`.

The paired C# walkthrough, HTTP host and both browser buttons returned all5/subset4 physical rows, with3/2 distinct selected identities and both provider labels. The extra Mongo identity is excluded; forged actor/unknown preset reject, and inactive TON returns503 without partial rows. TON is read-only throughout pairing; native snapshot/seed/ACK hashes remain unchanged. Exact temporary `cosmic_cql_mongo_ton_demo` was verified/dropped and confirmed absent. Mongo27029 and localHTTP5109 stopped; pending/ready/setup intents absent; retained source/key untouched.

| Evidence under `TestResults/TonCqlCurrent` | Result | SHA256 |
| --- | --- | --- |
| `Archive/480394AF108BAC8315344D7F0F7D7320BA879663C237A8FBAD31C4F87584BEAE-ton-retained-native.trx` | initial retained native1passed | filename hash |
| `ton-retained-native.trx` | independent retained reopen1passed | `0D730391BC16251DB36EA24A53ABED6639A453F7FE23AF63C7374CD435FA19A3` |
| `ton-mongo-federation.trx` | SDK/HTTP/browser/cleanup1passed; serve duration2m46s | `0D059A6CC606DEAF6D62EB18B44C2846033EC737C780B0126B36FB59C0845DBB` |

Browser images `ton-mongo-all.png`/`ton-mongo-subset.png`. Source/reopen hashes `pre-reopen-hashes.json`; logs `retained-demo-current.log`, `ton-mongo-federation.log`, `retained-reopen-runner.log`. Initial copied-host wiring failed compilation before any data mutation; corrected current consumer build passed. Original key was preserved/reused, not regenerated.

Sandbox **CQL Playground section14** is published and browser-verified: Web4Site main `107ce90d996c6ab3b4326b66f819db8f9b6876ad`, Production `7452230200` success. Expanded command and fixed5109 link are correct. `ton-catalogue-public.png` SHA256 `8510E561749FAA8A93A3B455F383CABEFEA01002D3E981E15FA328BD69D40F65`. The introduction now distinguishes hosted API demos from explicitly labelled local emulators; no hosted sign-in is forwarded.

## Remaining red scope

This closes the local-TVM ID-query/federation demo gate, not signed public TON access, network/finality, production packaging/toolchain review, mid-flight/commit/ACK fault schedules, optimistic CAS, full history/ORM/HyperDrive semantics or general content/query parity. Keep those rows red. The next breadth integration should reuse existing preserved proofs rather than deepen this green demo.

Full goal remains open. See [current status](CURRENT_STATUS.md), [after-demo backlog](AFTER_DEMO_BACKLOG.md) and [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).

Environment-free unit run:13passed and2explicit opt-in tests skipped (native acceptance and manual fixture maintenance), evidence ton-environment-free.trx SHA25640108F405DD44A855008D5869AA6E5932089AAA2377DD2579C5C2F20509D439E. This is CI/metadata coverage only. The configured actual-native14-test profile above executed with zero skips; skipped tests are not counted as native proof.
