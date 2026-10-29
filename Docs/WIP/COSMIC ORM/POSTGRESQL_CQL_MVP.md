# PostgreSQL CQL MVP admission

Updated 29 October 2026. **Native/restart verified; paired demo admission remains red.**

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

## Exact next step

Add the Mongo-paired C#/HTTP/browser preset with provider provenance, failure
controls and validated owned fixture cleanup. Keep the original schema, seed,
receipts and snapshot; reuse them read-only. Admission stays red until the paired
proof is complete.

## Programme priority

MVP breadth now covers **all providers** before deferred parity depth. Each concrete
provider must independently prove its native scope; shared transport or inheritance
is not evidence. See [inventory](CQL_ALL_PROVIDER_MVP_INVENTORY.md) and
[current status](CURRENT_STATUS.md). Hosted/credential/funding gates stay explicit;
free-only infrastructure policy remains in force.
