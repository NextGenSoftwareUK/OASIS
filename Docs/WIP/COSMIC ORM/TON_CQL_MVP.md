# TON native admission — contract controls pass; provider/CQL demo red

Updated 30 October 2026. This is development evidence, not a claim of native TON provider completion.

## Root issue and correction

The existing `TONOASIS` C# class inherits the EVM `Web3CoreOASIS` provider. TON executes TVM contracts, so that inheritance and the previous DNA/README/package claims did not prove TON CRUD, wallet, NFT, replication or CQL support. Those claims are corrected and the fictitious endpoint example is disabled. Native integration must replace the protocol owner, not relabel an EVM run or add a silent fallback.

A second bug was reproduced: the shared EVM adapter advertised ID-query capabilities for TON even though its compiler rejected that authority. `Web3CoreCqlCompiler` now owns one authority whitelist used by both capability reporting and compilation. TON ID queries reject during planning; positive Polygon/Arbitrum planning and compilation controls pass. This does not certify every unsupported plan shape or add native TON execution.

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
- Shared `PersistedPrivateKeyValueBackend` provides canonical key hiding/authenticated encryption. The C# test saves three real canonical holons through a **test-only** shared-storage probe, then reopens through separate workers, verifies identities/names/creator and unchanged source bytes. Wrong encryption key, read-only mutation, pre-dispatch cancellation and competing OS lease are rejected.
- Exact SDK and C# synthetic fixtures/ACKs are verified and removed after child/runner exit; no persistent demo source or private credential has been created.

| Current evidence under `TestResults/TonCqlCurrent` | Result | SHA256 |
| --- | --- | --- |
| `native-storage-tests.tap` | Contract6 + SDK7 results; 13 passed/no skips | `7D8B4E1D25D3317713609C35A0080193AC15A75A179541210624FB3C8F18480D` |
| `store-tests.tap` | Initial SDK7 passed/no skips | `17B0D02DABE5E75A1542885F7D8C3737B1F6C68B26F1127251B8E761B9D798F0` |
| `ton-native-storage.trx` | C# encrypted canonical roundtrip/ownership scenario passed,18s | `141F582A4951270CB652C1096B7FAEB062D4583098C8AA15720C5DD2F2E8628A` |
| `compiled/TonHolonStore_TonHolonStore.code.boc` | Current native dictionary/getter code | `A590947E35638A919AFDB2B6BD322DD9F08C69B9C32BB680971A5685B4D76A03` |

Receipt inspection regression: the first implementation accepted an ACK under a mismatched configured code hash. `store-inspection-before.tap` records the genuine failing subcase plus parent (2 failed results), SHA256 `C096C304C00EA9E0ACAD1D9F8CC21F7A7A5BBB4913E7A50A37E561A5CD77D872`. Pending intents and ACKs now carry code identity; SDK inspection and C# acknowledgements validate it. The complete13+1 acceptance above passed after the fix. Prior passing receipts are hash-archived.

The artifact SHA256 above differs from the TVM code-cell hash used for runtime identity. The runner computes the latter from the compiled BOC. SDK pending tests inject a clearly marked, never-submitted test intent; they do not prove recovery from every actual process/commit/ACK crash. Mid-flight cancellation/fault schedules, power-loss durability, journal retention/production packaging and signed real-network transport remain open. Physical snapshot serialization does not implement optimistic holon CAS; no conditional-write/HyperDrive parity is claimed.

## Next red demo gates

1. Replace `TONOASIS` EVM inheritance with the now-proved native backend and mandatory shared private storage; move activation/deactivation to that one owner. The shipping adapter is still EVM-based and is not a native demo.
2. Create one explicitly owned protected demo source outside TestResults and prove the actual TON adapter saves/loads three holons across process reopen. Canonical encryption is now proved through the test probe, not yet through the shipping adapter.
3. Add capability-aware CQL controls/native ID selection, smaller subset, missing/rejected/failed requests and unchanged source evidence. Keep general content/LINQ depth deferred.
4. Add Mongo SDK/HTTP/browser all5/subset4, exact fixture cleanup, then publish an honestly labelled local-TVM catalogue card. TON remains red and absent from the demo catalogue until those gates pass.

Full ORM/concurrency/history/HyperDrive and real-network parity remain open. See [current status](CURRENT_STATUS.md), [after-demo backlog](AFTER_DEMO_BACKLOG.md) and [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
