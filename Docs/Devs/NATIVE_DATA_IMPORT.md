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

The holon id is derived from `(importing avatar, provider, source, native key)` (`NativeRecordHolonMapper.StableHolonId`,
a name-derived RFC 9562 version-8 GUID). Importing the same record again addresses the same holon, so the manager loads
it (`allowMissing: true`), applies the record's current values and saves it; scheduled re-syncs are safe. Because the
importing avatar is part of the id, two avatars importing the same source each own separate holons and can never
overwrite each other's. Lineage is kept in holon metadata: `oasis.native.provider`, `oasis.native.source`,
`oasis.native.key` (reserved; a native field mapping onto them is refused).

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

## SQLite (`SQLLiteDBOASIS.NativeDataImport.cs`)

- Reads another application's SQLite file from DNA `SQLLiteDBOASIS.NativeImportConnectionString`, opened read-only;
  the OASIS database file itself is refused.
- Sources are user tables (`sqlite_*` excluded). Rows page in rowid order; the native key is the declared primary key as
  a JSON array of its values (`[3]`, `[10,1]`), or `rowid:N` for tables without one. WITHOUT ROWID tables are refused.
- INTEGER and REAL stay numbers, TEXT stays text, BLOB becomes base64.

## Neo4j (`Neo4jOASIS.NativeDataImport.cs`)

- OASIS shares the Neo4j database with other applications, so only labels listed in DNA
  `Neo4jOASIS.NativeImportLabels` (comma-separated) are ever read, through a read-only session.
- Each label is a source; each node a record keyed by its element id, paged in element-id order. Properties keep their
  scalar types; lists and maps become JSON text.

## Saved mappings

An avatar can save a mapping per provider and source (`PUT native-data/{provider}/sources/{source}/mapping`, list with
`GET native-data/{provider}/mappings`, remove with `DELETE`). Imports and previews that send no mapping use the saved one,
and `import-all` merges saved mappings with any sent in the request. They are stored with the avatar's settings
(`SettingsManager`, category `nativeImportMappings`).

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

`POST api/sandbox/native-demo` (sandbox nodes only) writes a fixed demo shop: MongoDB `SANDBOX_NATIVE_APPS` gets
`customers` (ObjectId keys), `products` (SKU keys, decimal prices, tag arrays) and `orders` (numeric keys, nested items);
when configured, the SQLite native file gets `suppliers` and Neo4j gets `ShopLoyaltyMember` nodes. The
sandbox page's Import tab seeds it, previews native records beside their holons, imports, and re-imports to show that
holons are updated rather than duplicated.

## Azure Cosmos DB (`AzureCosmosDBOASIS.NativeDataImport.cs`)

- Reads the database named by DNA `AzureCosmosDBOASIS.NativeImportDatabaseName` and only the containers listed in
  `NativeImportContainers` (comma-separated). When that database is the OASIS database, OASIS's own containers are refused.
- Each container is a source; each document a record keyed by its string `id`, paged with `ORDER BY c.id`. Top-level
  scalars keep their types, nested objects and arrays become JSON text, and Cosmos system properties (`_rid`, `_etag`, ...)
  are dropped. A document without a string `id` has no stable key and fails the read.

## SOLID (`SOLIDOASIS.NativeDataImport.cs`)

- Reads the containers listed in DNA `SOLIDOASIS.NativeImportContainers`, as paths relative to the Pod root
  (`public/people/`). Anything under `oasis/` or containing `..` is refused.
- Each container is a source; each resource directly inside it a record keyed by its path from the Pod root, paged in path
  order. Turtle resources contribute their literal and link properties (typed integers, decimals and booleans); any other
  resource contributes `contentType` and `size`. Resources over 1 MiB fail the read.

## ActivityPub (`ActivityPubOASIS.NativeDataImport.cs`)

- Reads the **signed-in account's own** data from its Mastodon-compatible instance, so an access token is required.
  Sources: `statuses`, `followers`, `following`. Records are keyed by the instance's decimal id and paged with the
  instance's own `max_id` cursor; a `next` link to any other host is refused.
- Statuses carry content, visibility, timestamps and counts; accounts carry username, display name, note and counts.

## IPFS (`IPFSOASIS.NativeDataImport.cs`)

- IPFS holds files, not rows, so each directory listed in DNA `IPFSOASIS.NativeImportPaths` (`/ipfs/<cid>` or
  `/ipns/<name>`) is a source and each entry a record keyed by name with `name`, `cid` and `size`. The content stays on
  IPFS, reachable through `cid`. OASIS's own lookup index is refused, as is a directory that lists a name twice.

## Solana (`SolanaOasis.NativeDataImport.cs`)

- Each program address in DNA `SolanaOASIS.NativeImportPrograms` is a source; each account the program owns a record keyed by
  its address, with `lamports`, `owner`, `executable`, `rentEpoch`, `dataLength` and the raw `data` as base64.
- `getProgramAccounts` cannot be paged, so one fetch is held for 60 seconds and every page comes from that snapshot. A
  program with more than 10,000 accounts is refused rather than partly imported.

## STAR CLI

`nativeimport` runs the same manager, identity and saved mappings as the API, as the beamed-in avatar (`avatar beamin`
first). Add `--json` for machine output.

```
nativeimport providers
nativeimport sources mongodb
nativeimport preview mongodb customers --count 3
nativeimport save-mapping mongodb customers --name-field fullName --rename dob=birthDate --exclude passwordHash --type Holon
nativeimport import mongodb customers --max 500
nativeimport import-all sqllite
nativeimport mappings mongodb
nativeimport delete-mapping mongodb customers
```

Provider names accept the full type (`MongoDBOASIS`) or the short form (`mongodb`, `sqllite`, `neo4j`). `preview` saves
nothing. `import` and `preview` use the saved mapping for the source when there is one, and the naming convention
otherwise; `import-all` applies every saved mapping. Implementation: `STAR ODK/NextGenSoftware.OASIS.STAR.CLI.Lib/NativeImportCli.cs`
(parsing and execution, unit-tested) and `Program.NativeImportCommands.cs` (wiring).

## Not yet built

- Live verification of the Azure, SOLID, ActivityPub, IPFS and Solana readers against real services. They are covered by
  unit tests against scripted services; the MongoDB, SQLite and Neo4j readers are also verified live on dev WEB4.
