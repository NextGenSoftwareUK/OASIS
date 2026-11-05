# Oracle local Mongo/content launcher recovery

Verified2November2026. Source/consumer recovery only; no native execution.

Restored Run-ReusableOracleMongoTests.ps1 and its complete named identity/content/
reconciliation test consumer closure. Inner Run-ReusableOracleTests.ps1 now admits
Cql/CqlContentProbe/CqlContent/CqlReconcile, including its single-mode guard.
Both runners parse; required inner modes verified via PowerShell AST. The actual
native/content/Mongo HTTP consumer compiles using existing cached dependencies.

Preserved newer OracleCqlActivation.cs, schema cancellation and query/provider
sources. Did not replay green compiler/native acceptance, start services, open
connection credentials/wallet or change hosting configuration. Hosted Oracle
remains deferred by David. Existing native/source proof reports remain historical.

Commands for a retained environment (not executed in this recovery):

```powershell
Scripts/TestHosts/Run-ReusableOracleMongoTests.ps1 -Serve
Scripts/TestHosts/Run-ReusableOracleMongoTests.ps1 -Content -Serve
```

These require existing admitted Oracle+Mongo environment and original fixture
journals. Pending/failed outcomes require reconciliation, not reset/reseed.
Source availability is not present runtime, hosted or browser acceptance.

Compact build hash/modes/evidence path:
LocalState/COSMIC/LauncherRecoveryCurrent/oracle-launcher-recovery-evidence.json.
Aggregate query-profile counts stay95local/60hosted because Oracle query code
already existed; published missing launcher paths decrease4to3.

5 November documentation recovery: the content roadmap linked a missing local
`CQL_ORACLE_DEMO_ADMISSION.md`. Restored that historical guide from original
commit f89cf0085/blob c0527a693544d900788a9859d6fc9812349f8694 with an explicit
historical-provenance banner and links back to current status. No native test,
connection, service or hosted source was replayed; no acceptance count changes.
