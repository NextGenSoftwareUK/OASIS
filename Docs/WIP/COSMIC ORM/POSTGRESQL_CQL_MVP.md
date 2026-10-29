# PostgreSQL CQL MVP admission

Updated 29 October 2026. **Native/demo admission remains red.**

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

## Exact next step

Reuse the retained `oasis-postgres` container and protected local configuration.
Admit a dedicated `cosmic_cql` schema only after checking original source/pending
state. Verify three saved records, a filtered two-record query, missing IDs, paging,
result-budget rejection and unchanged native records across query/restart. Then add
one Mongo-paired C#/HTTP/browser preset with explicit provider provenance and owned
fixture cleanup. Do not recreate a missing original source or claim green from controls.

## Programme priority

MVP breadth now covers **all providers** before deferred parity depth. Each concrete
provider must independently prove its native scope; shared transport or inheritance
is not evidence. See [inventory](CQL_ALL_PROVIDER_MVP_INVENTORY.md) and
[current status](CURRENT_STATUS.md). Hosted/credential/funding gates stay explicit;
free-only infrastructure policy remains in force.
