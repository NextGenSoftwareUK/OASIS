# Ethereum CQL demo MVP

Verified30 October2026 on work/cosmic-cql-sandbox-integration. Local Ganache7.9.2/chain31338, original contract/three encrypted records reused. No hosted/mainnet or full-parity claim.

## Scope

Generic ICosmicQueryStorageProvider adapter compiles Id equality/membership, constants/AND/OR and bounded paging into at most100 authorized native keys. Each lookup uses the observed block number and existing authenticated encrypted-record decoder. Missing tuples are absent; corrupt/foreign records, RPC failure or cancellation fail without partial rows. No chain scan/managed business filtering. Metadata/text predicates, explicit ordering, aggregates, reorg/finality guarantees and broader parity remain open.

12compiler/RPC controls and2native cases passed/no skips. Native3records/subset2 and shared HolonManager SDK/HTTP federation5(Ethereum3+Mongo2)/subset4(2+2) pass. Actual browser all/subset/shutdown clicks verified. Unknown/forged requests reject, unavailable source fails complete query. Exact owned Mongo fixture removed/pending journal absent;18547/27029/5097 listeners absent, runner exit0. Chain/key/seed retained; query acceptance verifies no transaction submissions.

## Launch

```powershell
# From C:/Source/OASIS-cosmic; runner owns build/environment leases.
pwsh -File Scripts/TestHosts/Run-CosmicEthereumCqlNative.ps1 -Federated -Serve
```

Open http://127.0.0.1:5097/ . Stop with page button and await runner teardown. Preserve/hash-archive previous reports before rerunning. Existing runtime/chain/key required; never reset/reseed unknown outcomes. Offline restore only if suitable assets missing. No downloads/install/restore occurred this verification.

## Evidence

Raw paths relative to TestResults/EthereumCqlCurrent; summaries only committed:

| File | SHA256 |
|---|---|
|ethereum-scoped-controls.trx|E8CA52BB918D6B2DA1EBC55F3A5A823159929AE46EAFB6B0336A34D4C40ADC37|
|ethereum-cql-native.trx|A452DA07966D8427C7B102426189CC60285C36F914C4E69EE7A757499597F4BD|
|ethereum-mongo-federation.trx|4A5E74F235CC80C0ABB1FBE09AB04590544E7A26FEB6D427E286CCEABDB2F5B7|
|ethereum-browser-scoped-proof.json|D0A473915C7E843579301722B5AF93527AEF30D17EBBC822539BB5AC2FDDAD5E|

Screenshot ethereum-browser-scoped.png. Earlier passed reports hash-preserved in Archive. Resources/ownership/teardown in ENVIRONMENT_AND_CLEANUP_LEDGER.md. Next catalogue enrollment, then Polygon/Arbitrum breadth per user order. Full backlog remains active.
