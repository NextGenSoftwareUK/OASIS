# Mongo + local Cosmos CQL federation

28October2026. Verified with Corea2e9b6cc50d8d88af1fb398d96b89fd3b23372e4,
which includes Development HyperDriveV2 routing/replication. This is retained local
emulator proof, not hosted Azure or production authentication/billing acceptance.

## Run and inspect

```powershell
& Scripts/TestHosts/Run-ReusableCosmosCqlDemo.ps1 -Mongo -Serve
```

Open http://127.0.0.1:5092/ . **Query all records** returns5physical rows
(Mongo2/Cosmos3); **Query two-record subset** returns4 (Mongo2/Cosmos2).
Each duplicate identity retains its native provider copy. The fixture host owns
trusted catalogues independently for each source; these synthetic records were
created by the acceptance fixture. Browser input selects only fixed all/subset
presets. Unknown presets and forged actor fields reject400 before query dispatch.
The unrelated Mongo document is excluded. Errors return503 without connection
secrets or partial-source substitution. Reads do not claim a distributed snapshot.

The commented [C# sample](../../../Examples/CosmicCqlDemo/CloudFederationWalkthrough.cs)
executes through HolonManager and shared capability-aware federation. The
[teaching host](../../../Examples/CosmicCqlDemo/CloudFederationApiHost.cs) shares
that exact path. Native acceptance calls both samples, then verifies ownership.

## Evidence and lifecycle

- Native SDK/HTTP/browser acceptance1/1pass/no skips. Actual browser5/4rows,
  source labels and shutdown clicks verified. Screenshot spacing readable.
  No durable screenshot artifact created; pixels inspected in browser tool.
- Four Cosmos fixture records deleted only after identity/name validation using
  independently observed ETags; physical record count0/journal absent.
- Three Mongo fixture records and exact owned DB cosmic_cql_mongo_cosmos_demo
  removed only after owners/names/IDs/complete collection set/count verification;
  pending journal absent. Retained Cosmos database/index schema remains reusable.
- Cosmos container exited0/no OOM; Mongo27029/host5092/Cosmos8081 listeners absent.
- TestResults/CosmosCqlCurrent/cosmos-cql-demo-current.trx SHA256
  84348625F80B90BE0E7773FAF4F107EFEAD8971408B016DBEE3D77C59655008D.
- Prior SDK-only pairing passed1/1; hash129885F38030B444CCF9800CB60102B6F5F54A0F26C7D1B82E8411A6DFDD39A7
  preserved as hash-named TRX in the same output folder before reuse.
- After visual verification, corrected the displayed provider-enum spelling to
  AzureCosmosDBOASIS; execution/provider response was already correct.

No paid resource/account/subscription, new environment/runtime/download was created.
[Resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md) owns retained paths/lifecycle.
[Hosted free admission](CQL_HOSTED_AZURE_FREE_ADMISSION.md) remains red until an
actual authenticated zero-cost hosted endpoint is available and tested.
