# NEAR CQL demo MVP

Verified30 October2026. Retained near-sandbox2.6.2/local ledger and original three records. Not hosted NEAR/mainnet or full parity.

Generic adapter supports authorized Id equality/membership, constants/AND/OR and bounded paging into at most100 native record-key reads. Load/save/query share the same unchanged key builder and authenticated SDK snapshot decoder. Deleted rows hidden; identity/access, corruption, transport/cancellation and budget failures publish no partial result. No scan/managed business filtering. Metadata/text, arbitrary ordering/graphs, common-block/finality and wider parity remain open.

Cancellation reaches the existing SDK worker owner, stopping its process rather than abandoning a background read. Internal reflection test callers now explicitly supply the optional cancellation argument; their unrelated native write suites were not rerun.

8query/cancellation controls+10encrypted-snapshot controls+1real paired SDK/HTTP schedule pass/no skips. Actual browser all5(NEAR3+Mongo2)/subset4(2+2)/shutdown verified. Original native-source.json SHA256DC09EFE6C8D9482A67B83D6500D98F35AE27F7C14CBD0A46C0A980004A71E559 and ledger/key retained; no source writes/reseed. Exact owned Mongo fixture removed/pending journal absent;3030/24567/27029/5103listeners absent, runner exit0/exactowned node stopped. Earlier standalone native report hash-archived, not rerun in paired scope. No install/download/restore/new environment.

```powershell
# From C:/Source/OASIS-cosmic; runner owns leases.
pwsh -File Scripts/TestHosts/Run-CosmicNearCqlNative.ps1 -Federated -Serve
```

Open http://127.0.0.1:5103/ . Stop with page button and wait for cleanup. Preserve/hash-archive previous reports. Original ledger/runtime/key/source manifest/SDK required; never reset/reseed unresolved outcomes.

Raw evidence under TestResults/NearCqlCurrent, not committed:

| File | SHA256 |
|---|---|
|near-scoped-controls.trx|CE0FFE44AE26CE8DB45992C9B4E4B5E3C64F9C17EF77CDC61C0007DF6BFAFA68|
|near-scoped-encryption.trx|1F77F0AE0FAF61A30B5E6B2804CE007A70B28E323B93E9EBEE4662689B667D9C|
|near-mongo-federation.trx|0AB4241F31F90365650DAC3A56918574B0A53D30EA2A495264A7E7FE7B6C1478|
|near-browser-scoped-proof.json|23F609D8D754BEFE69EA2A9502E3BF4BA8950AE8FD695888B0605D84B56650D2|

Screenshot near-browser-scoped.png. Resources/retention in shared ledger. Next batched Cosmos/Polkadot/NEAR catalogue enrollment; full roadmap remains active.
