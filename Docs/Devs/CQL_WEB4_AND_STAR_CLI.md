# Calling CQL: WEB4 API and STAR CLI

CQL v1 is a **bounded JSON query description**, not a text language. You post a JSON object to WEB4 and the server compiles
it for the chosen provider. The query can never contain executable C#, SQL or Cypher. This page covers how to call it; the
language itself (filters, operators, scalars, joins, grouping, federation, per-provider profiles) is in the CQL manual
(`Docs/WIP/COSMIC ORM/CQL_MANUAL.md`) and the runnable demos in `Examples/CosmicCqlDemo` (`README.md`).

## The endpoint

`POST {web4}/api/data/query-cql` (`CqlV1Controller`).

| Requirement | Detail |
|---|---|
| Auth | `Authorization: Bearer <JWT>` of a signed-in avatar. |
| `Idempotency-Key` | Required header. Use a new value for each new intent. Sending the same key again is refused with HTTP 409 (no replay). |
| Body | JSON object, at most 32,768 bytes. Unknown members are rejected. |
| Availability | The node needs a configured usage/storage admission route; a node without one refuses CQL. |

### Query kinds

Each kind has its own route and request shape.

| Route | Variant name in the CLI |
|---|---|
| `/api/data/query-cql` | `query` |
| `/api/data/query-cql/indexed` | `indexed` |
| `/api/data/query-cql/indexed/scalar` | `indexed-scalar` |
| `/api/data/query-cql/scalar` | `scalar` |
| `/api/data/query-cql/projection` | `projection` |
| `/api/data/query-cql/grouped` | `grouped` |
| `/api/data/query-cql/join` | `join` |
| `/api/data/query-cql/composition` | `composition` |
| `/api/data/query-cql/federation` | `federation` |

The request shapes are the `Cql*V1Request` classes in `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Models/Data`. WEB4's
Swagger page shows each one with examples you can try.

### The basic query

```json
{
  "provider": "MongoDBOASIS",
  "filters": [
    { "field": "Name", "scalar": "String", "operator": "Contains", "value": "coffee" }
  ],
  "orderBy": "Name",
  "descending": false,
  "skip": 0,
  "take": 10,
  "includeMetadata": false
}
```

A filter names either one whitelisted canonical `field` or a typed `metadataPath` (up to four segments), a `scalar` type
(`String`, `Integer`, `Boolean`, `Guid`, `DateTime`, `UInt64`, `Decimal`, `Double`, `Single`), an `operator` (`Equal`,
`NotEqual`, `GreaterThan`, `GreaterThanOrEqual`, `LessThan`, `LessThanOrEqual`, `Contains`, `StartsWith`, `EndsWith`,
`Exists`, `IsNull`) and a `value`. `filters` are ANDed; `anyOf` is ORed. `anyChildren` and `anyDescendants` match holons by
their children.

### With curl

```bash
curl -X POST "https://api.web4.oasisomniverse.one/api/data/query-cql" \
  -H "Authorization: Bearer $OASIS_JWT" \
  -H "Idempotency-Key: $(uuidgen)" \
  -H "Content-Type: application/json" \
  -d @query.json
```

## From the STAR CLI

Beam in first (`avatar beamin <username> <password>`); `cql` runs as that avatar with its JWT.

```
cql variants                                  # list the query kinds
cql example                                   # print a starter query
cql run query --file query.json               # run a query from a file
cql run query --json '{"provider":"MongoDBOASIS","take":5}'
cql run grouped --file grouped.json --web4 https://dev.api.web4.oasisomniverse.one
cql run query --file query.json --idempotency-key my-intent-1
```

- `--web4` (or the `OASIS_WEB4_URL` environment variable) picks the WEB4 node. It must be `https`, or loopback `http`.
- A fresh `Idempotency-Key` is generated for every run unless you pass `--idempotency-key`. Re-running a query therefore
  works; re-sending a fixed key returns the endpoint's 409.
- The CLI checks only that the query is a JSON object within the 32 KiB limit. The server validates everything else and its
  error is shown as returned, with the HTTP status.
- With `--json` the response is machine-readable: `{ "success": true, "message": ..., "data": <server response> }`.

Implementation: `STAR ODK/NextGenSoftware.OASIS.STAR.CLI.Lib/StarCqlCommand.cs` (parsing and request, unit-tested) and
`STAR ODK/NextGenSoftware.OASIS.STAR.CLI/Program.CqlCommands.cs` (wiring).

## From C# (SDK)

Post the same JSON with any `HttpClient`, as in `Examples/CosmicCqlDemo`. `StarCqlCommand.ExecuteAsync` is a small worked
example of the headers and body the endpoint expects.

## Related

- Native import (holonizing existing provider data): `NATIVE_DATA_IMPORT.md`.
- STAR CLI command reference: `STAR_CLI_Implemented_Commands.md`.
