# S3 compatible-service CQL MVP

Verified30 October2026. AWSS3OASIS against owned loopback MinIO, **not hosted AWS**. Full roadmap remains incomplete.

## Proven scope

Generic CQL adapter admits authorized Id equality/membership, constants/AND/OR and bounded paging, at most100 selected keys. Shared bounded-identity compiler is reused; encrypted storage/backend/canonical RecordKey own physical reads and identity/access validation. No bucket scan/managed business predicate, retry or partial publication. Metadata/text/ordering/aggregates and full provider/HyperDrive parity remain open.

7controls+3native schedules pass/no skips: initial selected3/subset2, same-store restart preserving encrypted objects/source IDs, Mongo federation SDK/HTTP5/4. Actual browser all5(S3 3+Mongo2)/subset4(2+2)/shutdown pass. Invalid/forged requests reject and unavailable S3 fails complete query. Exact temporary Mongo fixture removed/pending journal absent;19100/19101/27029/5107listeners absent, runner exit0, owned compatible-service container exited. Original seed173bytes/SHA256813DB2D8975DADD106D068EE6C688606B528B92D99FB7332BC5BDC412808240C/protected key262bytes and persistent store remain reusable.

Initial compilation-only missing shared compiler was resolved by scoped shared-owner port. First native-run preparation stopped on Windows-to-WSL path conversion before any container/key/source write; forward-slash path normalization fixes that boundary. Earlier historical combined launch review rejection remains preserved; this separately implemented loopback-only ownership-checked runner executed successfully. No cloud account/billable calls/install/download/restore.

## Launch and ownership

```powershell
# From C:/Source/OASIS-cosmic; runner owns build/service locks.
pwsh -File Scripts/TestHosts/Run-CosmicS3CqlNative.ps1 -Serve
```

Open http://127.0.0.1:5107/ . Stop with page button; wait for runner cleanup. Bounded mode omits -Serve. Passed evidence hash-archives automatically; failed reports/pending writes block replay. The owned MinIO store is persistent under LocalState/COSMIC/S3Cql/data, CurrentUserDPAPI key/seed outside cleanup targets. Container uses cached pinned image, read-only root and loopback ports with public test-only credentials; these are not AWS credentials. No unrelated environment is adopted or deleted.

## Evidence

Raw files under TestResults/AwsS3CqlCurrent, not committed:

| File | SHA256 |
|---|---|
|s3-scoped-controls.trx|AD3A506D760D22B4D29BF95B6B756E9376E6E949B24E73CE0D17FBA257E6AE97|
|s3-cql-native.trx|3C595908FBFC561FAFC830FDB793199DD142DDC04EF64328A082D4399D0B6E4D|
|s3-cql-restart.trx|EA5277E15A26A3C0B5DABED44117276EBDB8BB3C1D5ACCA43F2523E371F5457D|
|s3-mongo-federation.trx|792DDEAF9649D5A5F8F7979ED3BFF35091A6F6BCEFCD5D9BE0A6EB2BEF0F9653|
|s3-browser-native-proof.json|976FEEB7B1BD72F6F6E40B9FA2172B996B6F67C85AB83B5A02F1630880496E51|

Screenshot s3-browser-native.png. Logs scoped-controls.log(initial compile failure), scoped-controls-current.log(pass), scoped-native-run.log(initial path failure), scoped-native-current.log(pass) retained. Resource identities/size/lifecycle in ENVIRONMENT_AND_CLEANUP_LEDGER.md. Next catalogue enrollment and remaining red breadth gates; preserved green scopes remain frozen.
