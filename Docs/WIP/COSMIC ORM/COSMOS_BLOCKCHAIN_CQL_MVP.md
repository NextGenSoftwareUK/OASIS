# Cosmos blockchain CQL demo MVP

Verified30 October2026. CosmosBlockChainOASIS on retained local wasmd chain/contract; distinct from Azure Cosmos DB. No Cosmos Hub/mainnet or full-parity claim.

Generic CQL adapter supports authorized Id equality/membership, constants/AND/OR and bounded paging, at most100 native keys. It uses the shipping encrypted backend and existing shared RecordKey builder (compact GUID layout), not a guessed parallel key format. Native CosmJS contract reads/canonical decoder enforce identity/access; malformed/corrupt/foreign results or later transport failure discard partial rows. No scan, managed business predicate, source substitution or query writes. Metadata/text, ordering/aggregates, finality/snapshot and wider parity remain open.

14compiler/encryption controls+2nativecases pass/no skips. Original3records/subset2; SDK/HTTP/browser federation5(Cosmos3+Mongo2)/subset4(2+2). Forged/unknown requests reject, unavailable source fails complete query. Exact owned Mongo fixture removed/pending journal absent;26657/27029/5101listeners absent and runner exit0. wasmd SIGINT stop completed before Podman killed the remaining idle container process after10seconds; container exited. Original contract/key/4signed submissions remain intact, no reseed/new contract/query transaction. Seed SHA256D981416656A1ABC862D033D8E5C0FCB62BF11026ECE24605314CFB0B368F5B66 unchanged;11retained state files.

```powershell
# From C:/Source/OASIS-cosmic; runner owns leases.
pwsh -File Scripts/TestHosts/Run-CosmicCosmosCqlNative.ps1 -Federated -Serve
```

Open http://127.0.0.1:5101/ . Stop with page button and await runner teardown. Preserve/hash-archive previous reports before rerunning. Retain node/contract/seed/keys; unresolved native outcomes require reconciliation, never blind restart/reseed. Cached CosmJS installation reused through output dependency junction; no download/install/restore/new environment.

Raw evidence under TestResults/CosmosCqlCurrent, not committed:

| File | SHA256 |
|---|---|
|cosmos-scoped-controls.trx|4769C60B623549B5C55D3BEB6D26B0DAF3E797B14F2DA773EBCB463C56817FBF|
|cosmos-cql-native.trx|FB4543981448AEE84F4AD53C1188CEA44D3EF61E66238C50E9E7A956F94822E1|
|cosmos-mongo-federation.trx|6B1FC0F241EE668ED5723F601DEE8A6D6DF8D1D0A934678B83BA6613819A7548|
|cosmos-browser-scoped-proof.json|404A87711A9DD5D4E494FDBBA614D2D0298400B0D84842335D5D2C842F846983|

Screenshot cosmos-browser-scoped.png. Prior reports hash-preserved in Archive. Resources/cleanup in shared ledger. Next Polkadot/NEAR breadth then batched catalogue enrollment; full roadmap active.
