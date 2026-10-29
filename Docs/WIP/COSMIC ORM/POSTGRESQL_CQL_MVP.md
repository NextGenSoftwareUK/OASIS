# PostgreSQL CQL MVP admission

Updated 29 October 2026. **Scoped native/restart and Mongo-paired C#/HTTP/browser MVP verified.**

The adapter uses the existing PostgreSQL storage connection and canonical record
reader. A single parameterized SELECT applies authorized UUID selection and native
paging. The bounded catalogue supports at most 100 identities; unsupported content
predicates, ordering and child traversal reject before native execution. The decoded
response has a 4 MiB aggregate budget; this is not a pre-allocation bound.

PostgreSQL-compatible aliases retain their own identities and do not inherit this
capability admission. PostgreSQL storage is canonical JSON; no application encryption
or hosted-service certification is claimed. Federation observes providers separately.

## Verified controls

Eight tests passed, zero failed/skipped: captured catalogue immutability and authority
intersection, unsupported shapes/bounds, inactive provider, cancellation and alias
capability isolation. Producer and consumer builds passed using cached dependencies.

- Test: `Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.PostgreSQLOASIS.UnitTests/PostgreSqlCqlTests.cs`.
- Evidence: `TestResults/PostgreSqlCqlCurrent/postgres-cql-controls.trx`.
- SHA256: `4A1612248B969CC449F47763B9B7A37EB145FB7A392F17996452632AFE89CFEC`.

## Native and restart proof

The retained PostgreSQL service passed the native test twice (one test, zero failures
or skips per run): initial 751 ms, restart 324 ms. Three acknowledged saves in the
dedicated schema survived service restart with identical JSON hashes and native
`xmin` row versions. Selection returned three/two/zero rows; native paging returned
the expected UUID; a one-row result budget rejected without partial results.

- Fixture: `PostgreSqlCqlNativeTests.cs` in PostgreSQLOASIS.IntegrationTests.
- Initial evidence: `TestResults/PostgreSqlCqlCurrent/postgres-cql-initial.trx`.
- Initial SHA256: `1E68F21F658CFB3382F2225FC7A6DCF9673F8A099A516C954AEFE30F9A80EBBA`.
- Restart evidence: `TestResults/PostgreSqlCqlCurrent/postgres-cql-native.trx`.
- Restart SHA256: `72EFD2A2CE3293EB41FE64B041C137182A2059834DF4C0E40333D1BB06872521`.
- Persistent seed SHA256: `9B5BEFAA86549FAC075191C9418182E81F9013500A9612F938584AFD55B2A33F`.
- Runner: `Scripts/TestHosts/Run-CosmicPostgreSqlCqlNative.ps1`.

The first run exposed a runner ownership-flag scope bug: native verification passed,
but cleanup did not receive the flags. The runner now uses one shared lifecycle
object. The exact owned container was stopped, the restart proof passed, and final
container state was verified `exited`. No source/schema was reset.

## Mongo-paired teaching preset

The C# walkthrough, HTTP endpoints and browser all returned five physical rows
(three PostgreSQL plus two Mongo copies), then four for the two-ID subset.
Provider provenance remains explicit; duplicates are preserved. Forged actor and
unknown preset reject; an inactive PostgreSQL target returns unavailable without
partial rows. The temporary three-record Mongo fixture was verified and dropped.

- Test: `PostgreSqlMongoCqlNativeTests.cs` in PostgreSQLOASIS.IntegrationTests.
- Evidence: `TestResults/PostgreSqlCqlCurrent/postgres-mongo-federation.trx`.
- Result: one passed, zero failed/skipped, 20 seconds.
- SHA256: `60D807085515EE6CD73A2F2BF4959B746A352CB572DC4CFADC413A52BE7A09F4`.
- Browser evidence: `TestResults/PostgreSqlCqlCurrent/postgres-mongo-browser.jpg`.
- Final state: runner exit0, PostgreSQL exited, owned Mongo/HTTP listeners absent,
  fixture journal removed, original source retained.
- Launcher check: 8 single/16 plans, 21 paired/42 plans, 28 paired rejections,
  3 archive rejections; listeners unchanged.

```powershell
# Inspect the ready preset without launching services.
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider PostgreSQL -Federated -ShowPlan

# Run C# and HTTP checks, then keep the browser demo available on loopback5112.
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider PostgreSQL -Federated

# Automated verification with owned service teardown.
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider PostgreSQL -Federated -VerifyOnly
```

Use the page's stop button to complete owned fixture cleanup. Persistent source is
read-only on subsequent runs. This proves bounded identity filtering/native paging,
not arbitrary content predicates, managed cloud products, aliases, production
registration or full PostgreSQL LINQ parity.

## Exact next step

Continue missing provider MVPs from the all-provider inventory. PostgreSQL's wider
parity and alias admission remain red; do not harden this green scope absent a bug.

## Programme priority

MVP breadth now covers **all providers** before deferred parity depth. Each concrete
provider must independently prove its native scope; shared transport or inheritance
is not evidence. See [inventory](CQL_ALL_PROVIDER_MVP_INVENTORY.md) and
[current status](CURRENT_STATUS.md). Hosted/credential/funding gates stay explicit;
free-only infrastructure policy remains in force.
