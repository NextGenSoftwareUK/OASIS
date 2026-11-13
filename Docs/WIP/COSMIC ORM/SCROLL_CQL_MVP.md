# Scroll CQL MVP admission

Updated 29 October 2026. Local native/restart and Mongo-paired C#/HTTP/browser MVP verified.

## Root fixes and proof

The original three-string constructor now supplies declared chain534352 to the
shared encryption boundary, preserving its public signature. An explicit positive
chain-ID overload supports configured networks; zero/negative values reject before
native access. The shared CQL compiler admits Scroll's explicit authorized record
catalogue, preserving immutable ID capture, authority separation and bounded reads.
Unsupported business/metadata predicates, ordering, graph and over-bound catalogues
reject; inactive/cancellation paths behave correctly without opening RPC.

Initial actual reproduction3failed/5passed. Fixed Scroll11passed; affected EVM
family105passed/0failed/0skipped. Test `ScrollCqlTests.cs` in the Ethereum integration
project. Evidence `C:/Source/OASIS-cosmic/TestResults/ScrollCqlCurrent/scroll-family-controls.trx`.
SHA256: `4346fd2d848b8ed83883354cc134f2a29a413f973e9cb68dc28f6c3a605701a0`.

## Native and restart proof

The shipping Scroll provider used the existing Ganache7.9.2 chain31338 and pinned
Web3Core contract, with an independently journalled logical fixture. Initial1/1 and
restart1/1 pass. Three encrypted records return Espresso/Latte/Tea; explicit ID
membership selects Espresso/Latte. Native response hashes match the retained
baseline before/after queries and after process restart; account nonce is unchanged.
Original metadata885B under `LocalState/COSMIC/EvmCql/Scroll`, no pending intent.
Both owned nodes flushed/exited. No public Scroll network behavior is proven.

Run `Scripts/TestHosts/Run-CosmicEthereumCqlNative.ps1 -Provider Scroll`.
Test: `ScrollCqlNativeTests.cs` in the Ethereum integration project.

Evidence `TestResults/ScrollCqlCurrent/native-first.trx`, SHA256 `3f23d165dd6c098aff5b1ba159649f2a0ec8bbee2970357a6bc2203b33dba9b0`.

Evidence `TestResults/ScrollCqlCurrent/native-restart.trx`, SHA256 `67b4d83921f727e02ffaa6cc01f1d2789c1c36dc39ebf993d5954e4b9ad12dd6`.

## Ready Mongo-paired demo

Run `Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Scroll -Federated`.
Add `-VerifyOnly` for bounded verification. Browser `http://127.0.0.1:5130/`:
all records5physical rows (Scroll3/Mongo2), subset4rows (Scroll2/Mongo2).
Shared `IdentityFederationWalkthrough` uses explicit provider scopes and separate
trusted catalogues through HolonManager; copies retain their provider provenance.

```csharp
// Select requested IDs inside each provider's authorized catalogue.
source.Where(row => selectedIds.Contains(row.Id));

// Execute native reads and retain the source of each returned copy.
await manager.ExecuteFederationQueryAsync(plan, adapters);
```

Bounded paired1/1 and interactive paired1/1 pass. HTTP rejects forged actor and
unknown preset400, inactive Scroll503 without partial rows. Browser accessibility
results verified5/4. Screenshot capture failed twice; no screenshot proof retained.
Shutdown verifies fixture ownership/contents and drops only its temporary Mongo
database; original Scroll source/key/contract retained. Pending/ready files absent,
owned18547/27029/5130 listeners stopped and tab43 closed. Mongo termination is not
proof of clean native disk flush. Hub controls8single/16plans,39paired/78plans,
64paired rejections,3archive rejections pass; listener inventory unchanged.

Central registration/public Scroll sequencer/finality/bridge/distributed snapshots
and full parity remain unverified. Next zkSync requires native-runtime compatibility
audit before choosing its demonstration environment.

Evidence `TestResults/ScrollCqlCurrent/paired-bounded.trx`, SHA256 `1b59b7a45f3c79fce32a7acf527150de52f30faa82a9c1b3db63a8adbc101bd7`.

Evidence `TestResults/EthereumCqlCurrent/scroll-mongo-federation.trx`, SHA256 `feecdb55cf1309eca006df71c26965c03f3acba2a5a8d2ec0f769973de172ea4`.
