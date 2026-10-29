# Native Data API

Turn data other applications keep in their own databases into OASIS holons. Route: `api/native-data`. Full design,
per-provider settings and limits: [NATIVE_DATA_IMPORT.md](../../NATIVE_DATA_IMPORT.md). A ready-to-run Postman collection
is [WEB4-NativeData-Sandbox-CQL.postman_collection.json](WEB4-NativeData-Sandbox-CQL.postman_collection.json).

## Overview

- Every endpoint needs `Authorization: Bearer <JWT>`.
- **Your own database:** send a `connection` (or the name of a saved one, `connectionName`). Any signed-in avatar may do this.
- **The server's configured demo store:** send neither. This needs a Wizard avatar, or the sandbox (DNA `OASIS.Sandbox.Enabled`).
- Importing is repeatable: re-running updates the same holons rather than creating duplicates.
- Record caps come from DNA `OASIS.NativeImport` (sandbox, non-Wizard avatar, Wizard).
- Providers: MongoDB, SQLite, Neo4j, Azure Cosmos DB, SOLID, ActivityPub, IPFS, Solana. `GET /api/native-data/providers` lists them.

### Connection settings by provider

| Provider | Settings |
|---|---|
| `MongoDBOASIS` | `uri`, `database` |
| `Neo4jOASIS` | `uri`, `username`, `password`, optional `database`, `labels` |
| `SQLLiteDBOASIS` | `fileName`, `fileBase64` (a read-only temporary copy, deleted after use) |
| `AzureCosmosDBOASIS` | `endpoint`, `key`, `database`, optional `containers` |
| `SOLIDOASIS` | `podUrl`, `containers`, optional `token` |
| `ActivityPubOASIS` | `instanceUrl`, `accessToken` |
| `IPFSOASIS` | `apiUrl`, `paths` |
| `SolanaOASIS` | `rpcUrl`, `programs` |

Only public internet addresses are reachable unless DNA `OASIS.NativeImport.AllowPrivateAddresses` is true. Secrets are used
for the one call and removed from error messages.

## Endpoints

| Method | Route | Body | Does |
|---|---|---|---|
| GET | `/api/native-data/providers` | | Provider types that accept a connection you supply |
| GET | `/api/native-data/{provider}/sources` | | Sources (collections, tables, labels) of the server's demo store |
| POST | `/api/native-data/{provider}/sources` | `{ connection }` or `{ connectionName }` | Sources of your own database. Saves nothing |
| POST | `/api/native-data/{provider}/sources/{source}/preview?count=5` | `{ mapping?, connection? / connectionName? }` | First records next to the holons they would become. Saves nothing |
| POST | `/api/native-data/{provider}/sources/{source}/import` | `{ mapping?, connection? / connectionName?, pageSize?, maximumRecords? }` | Imports one source; returns the report |
| POST | `/api/native-data/{provider}/import-all` | `{ mappings?, connection? / connectionName?, pageSize?, maximumRecordsPerSource? }` | Imports every source |
| GET | `/api/native-data/{provider}/mappings` | | Your saved mappings |
| PUT | `/api/native-data/{provider}/sources/{source}/mapping` | mapping | Saves how a source becomes holons |
| DELETE | `/api/native-data/{provider}/sources/{source}/mapping` | | Deletes a saved mapping |
| GET | `/api/native-data/{provider}/connections` | | Names of your saved connections (never the settings) |
| PUT | `/api/native-data/{provider}/connections/{name}` | connection settings | Encrypts and saves a connection |
| DELETE | `/api/native-data/{provider}/connections/{name}` | | Deletes a saved connection |

A mapping is `{ "sourceName", "nameField", "excludedFields", "holonType", "fieldRenames" }`; with none, the naming convention applies.

### Example: import your own MongoDB

```http
POST /api/native-data/MongoDBOASIS/sources/customers/import
Authorization: Bearer YOUR_TOKEN
Content-Type: application/json

{
  "connection": { "uri": "mongodb+srv://user:password@cluster.example.net", "database": "shop" },
  "mapping": { "sourceName": "customers", "nameField": "fullName" },
  "pageSize": 200,
  "maximumRecords": 1000
}
```

## Saved connections

Save a connection once, then send `connectionName` instead of the secrets.

```http
PUT /api/native-data/MongoDBOASIS/connections/my-shop
Authorization: Bearer YOUR_TOKEN
Content-Type: application/json

{ "uri": "mongodb+srv://user:password@cluster.example.net", "database": "shop" }
```

```http
POST /api/native-data/MongoDBOASIS/sources
Authorization: Bearer YOUR_TOKEN
Content-Type: application/json

{ "connectionName": "my-shop" }
```

- Stored encrypted (AES-256-GCM) in your avatar record, with the key in DNA `OASIS.NativeImport.ConnectionEncryptionKey`. If the node has no key, saving is refused.
- Names are 1-64 letters, digits, spaces, `-` or `_`. At most `OASIS.NativeImport.MaximumSavedConnectionsPerAvatar` per avatar.
- Only you can use your saved connections. Deleting your avatar deletes them.
- Sending both `connection` and `connectionName` is refused.

## Error codes

| Code | Meaning |
|---|---|
| `NATIVE_IMPORT_FORBIDDEN` (403) | Not signed in, or reading the server's store without being a Wizard |
| `NATIVE_IMPORT_UNSUPPORTED` | The provider has no native reader |
| `NATIVE_IMPORT_CONNECTION_REFUSED` | A setting is missing or unknown, or the address is not allowed |
| `NATIVE_IMPORT_CONNECTION_AMBIGUOUS` | Both `connection` and `connectionName` were sent |
| `NATIVE_IMPORT_CONNECTION_NOT_FOUND` | No saved connection with that name |
| `NATIVE_IMPORT_CONNECTION_UNREADABLE` | A saved connection could not be opened (the key changed); save it again |
| `NATIVE_IMPORT_CONNECTION_INVALID` | Bad name, empty connection, or the node has no encryption key |
| `NATIVE_IMPORT_CONNECTION_LIMIT` | Too many saved connections |
| `NATIVE_IMPORT_MAPPING_INVALID` / `NATIVE_IMPORT_MAPPING_MISMATCH` | The mapping is not valid for that source |

## STAR CLI

`star nativeimport providers|sources|preview|import|import-all|mappings|save-mapping|delete-mapping <provider> ... --connection file.json`.
See [STAR_CLI_Implemented_Commands.md](../../STAR_CLI_Implemented_Commands.md).

---

**← Back to the [WEB4 API index](README.md)**
