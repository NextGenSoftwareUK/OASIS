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

A provider implements only discovery, paged reading and a description of the connection:

```csharp
Task<OASISResult<IReadOnlyList<NativeDataSource>>> ListNativeDataSourcesAsync(NativeConnection connection, CancellationToken ct);
Task<OASISResult<NativeRecordPage>> ReadNativeRecordsAsync(string sourceName, string continuationToken, int pageSize, NativeConnection connection, CancellationToken ct);
string DescribeNativeConnection(NativeConnection connection);   // see "Bring your own connection"
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

## Bring your own connection

One reader per provider imports whatever the caller points it at. Every call carries a `NativeConnection`: a set of named
settings supplied by the person importing, so no server configuration is needed to import your own database. The
settings in server DNA (`NativeImportDBName`, `NativeImportLabels`, ...) now only define the optional **demo store** that
is read when no connection is sent.

```csharp
Task<OASISResult<IReadOnlyList<NativeDataSource>>> ListNativeDataSourcesAsync(NativeConnection connection = null, CancellationToken ct = default);
Task<OASISResult<NativeRecordPage>> ReadNativeRecordsAsync(string source, string token, int pageSize, NativeConnection connection = null, CancellationToken ct = default);
string DescribeNativeConnection(NativeConnection connection);   // host + database, never credentials; null for the configured store
```

| Provider | Connection settings |
|---|---|
| MongoDB | `uri` (`mongodb://` or `mongodb+srv://`), `database` (or put it in the uri). The `admin`, `local` and `config` databases are refused |
| Neo4j | `uri` (`bolt`, `bolt+s`, `bolt+ssc`, `neo4j`, `neo4j+s`, `neo4j+ssc`, without credentials in it), `username`, `password`, optional `database`, optional `labels` (comma-separated; otherwise every label is listed) |
| SQLite | `fileName` (a plain file name), `fileBase64` (the database file, up to `MaximumSqliteUploadMegabytes`, default 8). It is written to a private temporary file, opened read-only with `query_only` and `trusted_schema=OFF`, and deleted straight after the call |
| Azure Cosmos DB | `endpoint` (`https://<account>.documents.azure.com:443/`), `key`, `database`, optional `containers` (comma-separated; otherwise every container is a source). Read in Gateway mode |
| SOLID | `podUrl` (https, no credentials), optional `token` (bearer; leave out for public data), `containers` (Pod-relative paths such as `public/notes/`; anything under `oasis/` is refused) |
| ActivityPub | `instanceUrl` (the https origin of a Mastodon-compatible server), `accessToken`. Reads that account's statuses, followers and following |
| IPFS | `apiUrl` (the HTTP API of an IPFS node, for example `https://ipfs.example.com:5001/`), `paths` (`/ipfs/<cid>` or `/ipns/<name>` directories). Lists them with `POST /api/v0/ls` through that node |
| Solana | `rpcUrl` (https), `programs` (comma-separated program addresses). One `getProgramAccounts` snapshot per node and program is held for `SolanaSnapshotSeconds` (default 60) so every page of an import is consistent; more than `MaximumSolanaAccountsPerProgram` (default 10,000) accounts is refused |

Unknown setting names are refused, so a typo is reported instead of silently ignored.

**Identity.** `DescribeNativeConnection` is part of every imported holon's id and is stored in the holon's
`oasis.native.connection` metadata. Two systems that both have a `customers` collection with key `1` therefore never
overwrite each other, and re-importing the same system updates its holons. Imports from the configured store keep the ids
they always had.

**What the server will and will not contact.** `NativeConnectionGuard` runs before any connection is opened:
- Only public addresses. Loopback, private (10/8, 172.16/12, 192.168/16), link-local (including the cloud metadata address),
  shared-address (100.64/10), multicast and IPv6 unique-local/link-local addresses are refused, as are IPv4-mapped IPv6
  forms of them.
- Internal names (`*.internal`, `*.local`, `*.localhost`, single-label hosts) are refused without a lookup. For other names
  every resolved address must be public; one private answer refuses the host. A name that does not resolve within `DnsTimeoutSeconds` (default 5) is refused.
- DNA `OASIS.NativeImport.AllowPrivateAddresses` (default `false`) switches the guard off; the bootloader applies it at boot. Use it only on a development machine importing from local services, never on a hosted node.
- **DNS rebinding.** The HTTP-based readers (SOLID, ActivityPub, IPFS, Solana, and Cosmos in Gateway mode) use
  `NativeConnectionGuard.CreateGuardedHttpHandler()`: every connection resolves the host once, checks every address, and
  connects to exactly those addresses, so a hostile DNS server cannot answer differently afterwards. Redirects are never
  followed. The MongoDB and Neo4j drivers open their own sockets, so for them the host is checked just before the driver
  connects and the driver resolves it again: a rebinding window remains there. Run WEB4 in a network that cannot reach
  anything sensitive from the importer's host.

**Secrets.** A connection is used for the request and discarded. It is not stored, not logged, not echoed in responses, and
secret values are replaced with `***` in driver error messages. Saved mappings never contain a connection. Ask users to
create a read-only database user for OASIS.

**Who may import.** A request with a connection is read by a stateless reader of that provider type (`OASISBootLoader.NativeImportReaders`), so it does not depend on the node having the provider configured or enabled. It may come from any signed-in avatar. Without one, the server's configured
demo store is read, which needs a Wizard avatar (or the sandbox). Limits (DNA `OASIS.NativeImport`): the sandbox caps imports at `SandboxMaximumRecordsPerSource`;
other non-Wizard avatars at `UserMaximumRecordsPerSource`; Wizards are not capped. Connections time out after `ConnectTimeoutSeconds` and reads after `ReadTimeoutSeconds`.

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

## Saved connections

A connection can be saved under a name so it need not be re-entered. **Where:** in the avatar's own record, `MetaData.preferences.nativeImportConnections`, the same place as saved mappings and per-avatar HyperDrive settings. It is
written through the avatar manager to whichever providers that avatar's HyperDrive routes to, so the stored value is replicated like the rest of the avatar. **How:** each connection is serialised and encrypted with AES-256-GCM
(`NativeConnectionVault`, using the existing `PasswordEncryptionHelper` quantum layer) before it is stored, with the key from DNA `OASIS.NativeImport.ConnectionEncryptionKey`. The key is never in the database.

- With no key configured, saving is refused (`NATIVE_IMPORT_CONNECTION_INVALID`); nothing is ever stored in the clear, and a stored value that is not ciphertext is refused when opened.
- Listing returns names only. Settings are decrypted only inside an import, preview or source listing for the same avatar, so one avatar can never use another's connection.
- Names are 1-64 letters, digits, spaces, `-` or `_`. At most `MaximumSavedConnectionsPerAvatar` per avatar (`NATIVE_IMPORT_CONNECTION_LIMIT`).
- Deleting an avatar deletes its saved connections, so the sandbox cleanup removes a demo avatar's along with everything else.
- Changing or losing the key makes existing saved connections unreadable (`NATIVE_IMPORT_CONNECTION_UNREADABLE`); save them again.
- STAR CLI does not save connections: it reads `--connection file.json` from the user's own disk.

## Saved mappings

An avatar can save a mapping per provider and source (`PUT native-data/{provider}/sources/{source}/mapping`, list with
`GET native-data/{provider}/mappings`, remove with `DELETE`). Imports and previews that send no mapping use the saved one,
and `import-all` merges saved mappings with any sent in the request. They are stored with the avatar's settings
(`SettingsManager`, category `nativeImportMappings`).

## Adding a provider

1. Implement `INativeDataImportProvider` in a `<Provider>.NativeDataImport.cs` partial. Take the `NativeConnection` on every call: read the caller's store when it is given, the configured one when it is `null`. If you cannot read a caller-supplied connection yet, call `NativeConnectionGuard.RefuseUnsupported` instead of ignoring it. Run every host through `NativeConnectionGuard.EnsurePublicHostAsync` before connecting, refuse unknown settings with `connection.RequireOnly(...)`, and redact secrets from driver messages.
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

`NativeDataController`, route `api/native-data`. Any signed-in avatar may import from a connection it sends. Reading the server's configured store (no connection) needs a **Wizard** avatar, because it reads the provider's own
databases; on the sandbox (DNA `OASIS.Sandbox.Enabled`) any signed-in demo avatar may use it, capped by `SandboxMaximumRecordsPerSource`.

| Method | Route | Does |
|---|---|---|
| GET | `providers` | Registered providers that implement `INativeDataImportProvider` |
| GET | `{provider}/sources` | Sources of the server's configured demo store (Wizard or sandbox only) |
| POST | `{provider}/sources` | Body: `{ connection }`. Sources of the caller's own database; nothing is saved |
| POST | `{provider}/sources/{source}/preview?count=5` | Body: `{ mapping, connection }`, both optional. First records next to the holons they would become; saves nothing |
| POST | `{provider}/sources/{source}/import` | Body: `{ mapping, connection, pageSize, maximumRecords }`. Returns the import report |
| POST | `{provider}/import-all` | Body: `{ mappings: { source: mapping }, connection, pageSize, maximumRecordsPerSource }` |
| GET | `{provider}/connections` | Names of this avatar's saved connections (never their settings) |
| PUT | `{provider}/connections/{name}` | Body: the connection settings. Encrypts and saves them under that name |
| DELETE | `{provider}/connections/{name}` | Removes a saved connection |

Every request above that takes `connection` also takes `connectionName` instead, to use a saved one. Sending both is refused (`NATIVE_IMPORT_CONNECTION_AMBIGUOUS`).

MongoDB reads its native database from DNA `StorageProviders.MongoDBOASIS.NativeImportDBName`.

## Sandbox

`POST api/sandbox/native-demo` (sandbox nodes only) writes a fixed demo shop: MongoDB `SANDBOX_NATIVE_APPS` gets
`customers` (ObjectId keys), `products` (SKU keys, decimal prices, tag arrays) and `orders` (numeric keys, nested items);
when configured, the SQLite native file gets `suppliers` and Neo4j gets `ShopLoyaltyMember` nodes. The
sandbox page's Import tab seeds it, previews native records beside their holons, imports, and re-imports to show that
holons are updated rather than duplicated. Choosing "Import my own database" swaps the demo shop for a form for any of the eight database types: a MongoDB, Neo4j, Cosmos, SOLID, ActivityPub, IPFS or Solana connection, or an uploaded SQLite file. The same preview, mapping and import steps then run against the visitor's own data.

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
  resource contributes `contentType` and `size`. Resources over `MaximumSolidResourceMegabytes` (default 1) fail the read.

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
- `getProgramAccounts` cannot be paged, so one fetch is held for `SolanaSnapshotSeconds` and every page comes from that snapshot. A
  program with more than `MaximumSolanaAccountsPerProgram` accounts is refused rather than partly imported.

## STAR CLI

`nativeimport` runs the same manager, identity and saved mappings as the API, as the beamed-in avatar (`avatar beamin`
first). Add `--json` for machine output.

```
nativeimport providers
nativeimport sources mongodb                                  # the server's demo store
nativeimport sources mongodb --connection mine.json            # your own database
nativeimport preview mongodb customers --count 3
nativeimport save-mapping mongodb customers --name-field fullName --rename dob=birthDate --exclude passwordHash --type Holon
nativeimport import mongodb customers --max 500
nativeimport import neo4j Person --connection aura.json        # import from your own Neo4j
nativeimport import-all sqllite
nativeimport mappings mongodb
nativeimport delete-mapping mongodb customers
```


`--connection` reads a JSON file of string settings (see "Bring your own connection"), for example
`{ "uri": "mongodb+srv://reader:...@cluster.example.net", "database": "shop" }`. It is a file, not a command-line value, so
credentials stay out of shell history and process listings. Without it the server's configured demo store is read.
The CLI runs on your own machine, so it may connect to local and private databases (`localhost`, a LAN host): the public-address
guard is switched off inside the CLI process (it sets the guard flag for itself). The guard only applies to a hosted WEB4, where "localhost" would mean the server itself.
Provider names accept the full type (`MongoDBOASIS`) or the short form (`mongodb`, `sqllite`, `neo4j`). `preview` saves
nothing. `import` and `preview` use the saved mapping for the source when there is one, and the naming convention
otherwise; `import-all` applies every saved mapping. Implementation: `STAR ODK/NextGenSoftware.OASIS.STAR.CLI.Lib/NativeImportCli.cs`
(parsing and execution, unit-tested) and `Program.NativeImportCommands.cs` (wiring).

## Settings (all in DNA, none hard-coded)

Every limit and timeout is a setting in `OASIS.NativeImport` of the node's DNA (`OASIS_DNA.json`, or the `OASIS_DNA_JSON`
variable on Railway). The defaults are written once, in `NativeImportSettings`; the shipped `Default/OASIS_DNA.json` lists every
setting with the same values and a test keeps the two identical. The readers take them from DNA each time (`NativeImportOptions`),
so a change applies without a code change. A value below 1 fails with a message naming the setting.

| Setting | Default | Meaning |
|---|---|---|
| `AllowPrivateAddresses` | `false` | Let imports reach loopback and private addresses. Never on a hosted node |
| `SandboxMaximumRecordsPerSource` | `500` | Records per source for an import on the sandbox |
| `UserMaximumRecordsPerSource` | `5000` | Records per source for a non-Wizard avatar importing its own database |
| `DefaultPreviewRecords` / `MaximumPreviewRecords` | `5` / `20` | Preview size when not given / the most a preview may ask for |
| `DefaultPageSize` / `MaximumPageSize` | `200` / `1000` | Records per page when not given / the most a reader is asked for |
| `DefaultMaximumRecordsPerSource` | `10000` | Records per source when a request does not say |
| `ConnectTimeoutSeconds` | `10` | Opening a connection to a caller's system |
| `ReadTimeoutSeconds` | `30` | One read from a caller's system |
| `DnsTimeoutSeconds` | `5` | Resolving a host name before refusing it |
| `MaximumSqliteUploadMegabytes` | `8` | Largest SQLite file a caller may upload |
| `MaximumSolanaAccountsPerProgram` | `10000` | Accounts one program may own for an import to proceed |
| `SolanaSnapshotSeconds` | `60` | How long one `getProgramAccounts` snapshot is reused |
| `MaximumIpfsListingMegabytes` | `4` | Largest IPFS directory listing read |
| `MaximumSolidResourceMegabytes` | `1` | Largest SOLID resource read |
| `MaximumNeo4jDiscoveredLabels` | `200` | Labels listed when a Neo4j connection does not name them |
| `ConnectionEncryptionKey` | empty | Secret that encrypts saved connections. Empty means saving is refused. Set a long random value on any node that offers them |
| `MaximumSavedConnectionsPerAvatar` | `10` | Connections one avatar may save |

The sandbox settings (`OASIS.Sandbox`) are described in `SANDBOX_DEMO_AVATARS.md`. The only values still fixed in code are protocol
facts, such as Mastodon's page sizes (40 statuses, 80 accounts) and the SQLite file header.

## Not yet built

- Live verification of caller-supplied connections. All eight readers are covered by unit tests against scripted services and real files (SQLite); the MongoDB, SQLite and Neo4j readers were verified live on dev WEB4 against the configured demo store, but not yet with a connection a user supplies.
