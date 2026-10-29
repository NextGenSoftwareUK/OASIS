# SQL Server CQL MVP admission

Updated 29 October 2026. **Native and Mongo-paired admission remain red.**

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
its native round trip is still to verify. No encryption, Azure SQL, cloud, alias or
production registration claim follows from these controls.

## Verified controls

Nine passed, zero failed/skipped, 127 ms. Covers captured catalogue immutability,
authority intersection, unsupported query shapes/bounds, inactive/pre-cancelled
requests, alias isolation, empty selection without native I/O and deactivation.
Producer and unit consumer built using cached dependencies.

- Test: `Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.SQLServerDBOASIS.UnitTests/SqlServerCqlTests.cs`.
- Evidence: `TestResults/SqlServerCqlCurrent/sqlserver-cql-controls.trx`.
- SHA256: `673C37F69FBBD64CB0AD96D55BCB54EFC5BB87335DB159AC5DB0993403AD6DB1`.

## Exact next step

Reuse installed SQL Server170 LocalDB at
`C:/Program Files/Microsoft SQL Server/170/Tools/Binn/SqlLocalDB.exe`.
Inspection finds no created automatic MSSQLLocalDB instance. Admit one fixed task
instance/database, persist setup intent before writes, then prove three acknowledged
saves, three/two/missing queries, native paging/result-budget rejection and unchanged
source across restart. Preserve unknown outcomes. Add the Mongo-paired C#/HTTP/browser
preset only after native proof. Do not mark green from compiler/control tests.

All-provider [inventory](CQL_ALL_PROVIDER_MVP_INVENTORY.md) / [status](CURRENT_STATUS.md).
