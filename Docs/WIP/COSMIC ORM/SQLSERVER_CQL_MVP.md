# SQL Server CQL MVP admission

Updated 29 October 2026. **Scoped native/restart and Mongo-paired C#/HTTP/browser MVP verified.**

## Implemented scope

The adapter binds the shared bounded identity compiler to SQLServerDBOASIS. It
admits an explicit authoritative catalogue of at most 100 UUIDs, then uses one
parameterized native SELECT with OFFSET/FETCH. UUID text ordering uses a binary
collation. Empty logical selections return without opening a connection, after
capability/cancellation/activation checks. Unsupported content predicates, child
traversal and arbitrary ordering reject instead of scanning. A 4 MiB aggregate
decoded response budget is checked after string allocation; no pre-allocation bound
is claimed. Errors publish no partial page. Native isolation follows database
configuration; federation observes providers separately.

The provider now marks successful activation and clears activation on activation
failure/deactivation. Previously the activation method returned success without
setting the public lifecycle state. The existing save/load JSON codec is reused;
its native three-record save/load round trip now passes. No encryption, Azure SQL, cloud, alias or
production registration claim follows from these controls.

## Verified controls

Nine passed, zero failed/skipped, 127 ms. Covers captured catalogue immutability,
authority intersection, unsupported query shapes/bounds, inactive/pre-cancelled
requests, alias isolation, empty selection without native I/O and deactivation.
Producer and unit consumer built using cached dependencies.

- Test: `Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.SQLServerDBOASIS.UnitTests/SqlServerCqlTests.cs`.
- Evidence: `TestResults/SqlServerCqlCurrent/sqlserver-cql-controls.trx`.
- SHA256: `673C37F69FBBD64CB0AD96D55BCB54EFC5BB87335DB159AC5DB0993403AD6DB1`.

## Native and restart verification

Installed LocalDB17.0.4025.3 is reused through one Windows-user-private task instance,
`CosmicCql`, and database `cosmic_cql_demo`. No runtime was downloaded/installed.
Database files, admission, seed, three write receipts and native snapshot remain in
`LocalState/COSMIC/SqlServerCql`. The named-pipe fixture uses Windows authentication;
this is local SQL Server, not Azure SQL or a remotely exposed service.

Initial native test1 passed891ms; restart test1 passed (including final-source rerun); zero failures/skips.
Three acknowledged saves load the expected IDs, names and creator; selection returns
three/two/zero rows; paging returns the expected UUID; result-budget failure returns
no partial records. Native JSON hashes and modified timestamps remain unchanged
through queries and instance stop/start. Final instance state: **Stopped**.

SaveHolonAsync previously returned success without IsSaved/SavedCount. It now requires
one acknowledged SQL upsert row, then sets IsSaved=true/SavedCount=1. Native save
assertions verify this at the write boundary; wider save/concurrency parity is open.

- Fixture: `SqlServerCqlNativeTests.cs` in SQLServerDBOASIS.IntegrationTests.
- Runner: `Scripts/TestHosts/Run-CosmicSqlServerCqlNative.ps1`.
- Initial evidence: `TestResults/SqlServerCqlCurrent/sqlserver-cql-initial.trx`.
- Initial SHA256: `3943CCEB9752274478C16E7948108D478ED6975F25226DF3A3533C466CA49EC1`.
- Restart evidence: `TestResults/SqlServerCqlCurrent/sqlserver-cql-native.trx`.
- Restart SHA256: `BFC39406CE22D9530EECCC8777093F8B0AA29756EAD31005F7E9C181CFDD451E`.
- Seed SHA256: `E8845E29EB703BD1495C51FE9CFCA734C3DB624D2B599952C82C002F39AAC914`.

## Mongo-paired teaching demo

The shared C# walkthrough, HTTP endpoints and browser return five physical rows
(three SQL Server plus two Mongo copies), then four for the two-ID subset. Copies
remain labelled by provider. Forged actor and unknown preset reject; an inactive
SQL Server target returns unavailable without partial rows.

- Test: `SqlServerMongoCqlNativeTests.cs` in SQLServerDBOASIS.IntegrationTests.
- Evidence: `TestResults/SqlServerCqlCurrent/sqlserver-mongo-federation.trx`.
- Result: one passed, zero failed/skipped, 32 seconds.
- SHA256: `52DCCB5C526E06A43C02AD6F7A00FE6C0BC04DD279368DFA66D972B92039F235`.
- Browser: `TestResults/SqlServerCqlCurrent/sqlserver-mongo-browser.jpg`.
- Final state: runner8528 exit0; LocalDB **Stopped**, Mongo/HTTP listeners absent;
  exact temporary Mongo fixture verified/dropped and ownership journal removed.
- Launcher routing: 8 single/16 plans, 22 paired/44 plans, 30 paired rejections,
  3 archive rejections; listeners unchanged.

```powershell
# Inspect the preset without starting services.
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider SQLServer -Federated -ShowPlan

# Run SDK/HTTP verification, then serve the browser demo on loopback5113.
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider SQLServer -Federated

# Automated verification and teardown.
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider SQLServer -Federated -VerifyOnly
```

Stop with the page button to finish owned fixture cleanup. The original SQL Server
source remains intact. This proves bounded UUID selection/native paging on local
SQL Server; arbitrary predicates, full LINQ parity, cloud/alias/product registration
and broader concurrency/child/history acceptance remain open.

## Exact next step

Continue the next red provider MVP from the all-provider inventory. Preserve this
source and reuse it; no further hardening of the green scope absent a reproduced bug.

All-provider [inventory](CQL_ALL_PROVIDER_MVP_INVENTORY.md) / [status](CURRENT_STATUS.md).
