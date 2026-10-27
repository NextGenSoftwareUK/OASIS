# Native data import (holonizing existing provider data)

`IOASISStorageProvider.Import`/`Export` move **OASIS holons** in and out of a provider. Native data import is the other
half: it reads data that **other applications** already wrote into a provider's own store (a Mongo collection, a SQL
table, a wallet's accounts, a SOLID pod) and turns it into holons. After that the data is ordinary OASIS data: CQL queries
it, HyperDrive replicates it and fails over for it.

## One contract, two provider methods

| Layer | Owner | File |
|---|---|---|
| Discover sources, read pages of records | each provider | `INativeDataImportProvider` (Core `Interfaces/Providers`) |
| Record → holon mapping, stable ids | shared | `NativeRecordHolonMapper` (Core `Objects/NativeData`) |
| Paging loop, get-or-create, save, report | shared | `NativeDataImportManager` (Core `Managers`) |

A provider implements only:

```csharp
Task<OASISResult<IReadOnlyList<NativeDataSource>>> ListNativeDataSourcesAsync(CancellationToken ct);
Task<OASISResult<NativeRecordPage>> ReadNativeRecordsAsync(string sourceName, string continuationToken, int pageSize, CancellationToken ct);
```

It returns `NativeRecord`s: the source name, the record's **own native key**, and fields normalized to plain scalars
(`string`, `bool`, `int`, `long`, `double`, `decimal`, `DateTime`, `Guid`, `null`). Nested values (arrays, sub-documents)
are returned as JSON text. The mapper refuses any other type, so a provider cannot hand over values some stores cannot hold.

Do not write per-provider importers, mapping rules or save loops. Those live once in Core.

## Identity: re-importing updates, never duplicates

The holon id is derived from `(provider, source, native key)` (`NativeRecordHolonMapper.StableHolonId`, a name-derived
RFC 9562 version-8 GUID). Importing the same record again addresses the same holon, so the manager loads it
(`allowMissing: true`), applies the record's current values and saves it. That makes scheduled re-syncs safe.
Lineage is kept in holon metadata: `oasis.native.provider`, `oasis.native.source`, `oasis.native.key` (reserved; a native
field mapping onto them is refused).

## Mappings: convention first, overrides only where wanted

Nobody has to map each source before importing. With no mapping, `NativeDataMapping.ForSource` applies the convention:

- **Name**: first non-empty text field among `name`, `title`, `label`, `displayName`, `username`; otherwise `"<source> <key>"`.
- **Description**: first of `description`, `summary`, `bio`, `body`.
- **Every other field** is copied into `MetaData` under its native name.
- **Holon type**: `Holon`.

A `NativeDataMapping` only overrides what it sets: `HolonType`, `NameField`, `DescriptionField`, `FieldRenames`,
`ExcludedFields` (for secrets or internal columns). `ImportAllAsync` takes a dictionary of saved mappings keyed by source
name and uses the convention for every source without one, so a whole provider imports in one call and the user only
writes mappings for the sources they want shaped differently.

## Results

`ImportSourceAsync` returns `OASISResult<NativeDataImportReport>`: records read, holons created / updated / unchanged,
failed records (first 100 reasons), and `Truncated` when the record limit stopped it. A record that fails to map or save is
counted and reported (`IsWarning`); a listing or reading failure stops the import with the provider's error code.

## MongoDB (reference implementation)

`MongoDBOASIS.NativeDataImport.cs`:

- Reads from `NativeImportDatabaseName`, a database on the same deployment that **must differ from the OASIS database**.
  OASIS's own collections therefore never look like foreign data, without a hand-kept exclusion list.
- Lists every collection except `system.*`, with estimated counts.
- Pages in `_id` order using `$expr: { $gt: ["$_id", last] }`, which compares across BSON types, so collections with mixed
  `_id` types (ints, strings, ObjectIds) page completely. The continuation token and native key are canonical extended JSON
  of `_id`, which keeps keys of different types distinct.
- Converts BSON scalars to the supported types; ObjectIds become hex strings; arrays and sub-documents become relaxed JSON.

## Adding a provider

1. Implement `INativeDataImportProvider` in a `<Provider>.NativeDataImport.cs` partial.
2. Pick a native key that never changes for a record (primary key, account address, resource URI).
3. Normalize values to the supported scalars; return nested values as JSON text.
4. Page in a stable order with an opaque continuation token; never return an empty page with a token.
5. Add a provider test that lists sources and pages a seeded source end to end (see `MongoNativeDataImportTests`).

## Tests

- Core: `OASIS Architecture/NextGenSoftware.OASIS.API.Core.UnitTests/NativeDataImportTests.cs` (mapping, ids, paging,
  re-import, failure reporting, limits).
- Mongo: `Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests/MongoNativeDataImportTests.cs`
  needs `OASIS_MONGO_NATIVE_IMPORT_CONNECTION` (a disposable deployment); it creates and drops its own databases.

## API (WEB4 ONODE)

`NativeDataController`, route `api/native-data`. Signed-in **Wizard** avatars only, because it reads the provider's own
databases; on the sandbox (`OASIS_SANDBOX=true`) any signed-in demo avatar may use it, capped at 500 records per source.

| Method | Route | Does |
|---|---|---|
| GET | `providers` | Registered providers that implement `INativeDataImportProvider` |
| GET | `{provider}/sources` | Sources with estimated counts |
| POST | `{provider}/sources/{source}/preview?count=5` | Body: optional mapping (`{}` for the convention). First records next to the holons they would become; saves nothing |
| POST | `{provider}/sources/{source}/import` | Body: `{ mapping, pageSize, maximumRecords }`. Returns the import report |
| POST | `{provider}/import-all` | Body: `{ mappings: { source: mapping }, pageSize, maximumRecordsPerSource }` |

MongoDB reads its native database from DNA `StorageProviders.MongoDBOASIS.NativeImportDBName`.

## Sandbox

`POST api/sandbox/native-demo` (sandbox only) writes a fixed demo shop into `SANDBOX_NATIVE_APPS`: `customers`
(ObjectId keys), `products` (SKU string keys, decimal prices, tag arrays) and `orders` (numeric keys, nested items). The
sandbox page's Import tab seeds it, previews native records beside their holons, imports, and re-imports to show that
holons are updated rather than duplicated.

## Not yet built

- STAR CLI commands for listing sources and importing.
- Persisting mappings (as holons) so they are reused across runs.
- Readers for SQLite, Neo4j, Azure, Solana, IPFS, SOLID and ActivityPub.
