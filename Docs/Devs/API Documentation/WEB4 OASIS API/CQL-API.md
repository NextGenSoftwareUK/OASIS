# CQL API

CQL v1 is a bounded JSON query description, not a text language: the server compiles it for the chosen provider, so a query can
never contain executable C#, SQL or Cypher. Route: `api/data/query-cql`. The full guide (request shapes, curl, the STAR CLI `cql`
command) is [CQL_WEB4_AND_STAR_CLI.md](../../CQL_WEB4_AND_STAR_CLI.md); the language is in the CQL manual
(`Docs/WIP/COSMIC ORM/CQL_MANUAL.md`). Postman:
[WEB4-NativeData-Sandbox-CQL.postman_collection.json](WEB4-NativeData-Sandbox-CQL.postman_collection.json).

## Requirements

- `Authorization: Bearer <JWT>`.
- `Idempotency-Key: <new value per intent>`. Reusing a key is refused with HTTP 409.
- Body is a JSON object of at most 32,768 bytes; unknown members are rejected.
- The node needs a configured usage/storage admission route; a node without one refuses CQL.

## Endpoints

| Method | Route | Does |
|---|---|---|
| GET | `/api/data/query-cql/providers?examplesVersion=2` | Enrolled readiness and checked Rows/Federation examples; version1 defaults to leaf-only |
| POST | `/api/data/query-cql/demo-identities` | Authenticated owner-scoped sample IDs for1–2 native keys; no provider I/O or access grant |
| POST | `/api/data/query-cql` | Filtered, ordered, paged query |
| POST | `/api/data/query-cql/indexed` | Query against an indexed alias |
| POST | `/api/data/query-cql/indexed/scalar` | Scalar over an indexed alias |
| POST | `/api/data/query-cql/scalar` | Count, sum and other scalars |
| POST | `/api/data/query-cql/projection` | Selected columns |
| POST | `/api/data/query-cql/grouped` | Grouping and aggregates |
| POST | `/api/data/query-cql/join` | Bounded join of two sources |
| POST | `/api/data/query-cql/composition` | Staged composition over a projection |
| POST | `/api/data/query-cql/federation` | One query across several providers |

### Example

```http
POST /api/data/query-cql
Authorization: Bearer YOUR_TOKEN
Idempotency-Key: 6f1f0c1e-3a55-4a0e-9b8f-2f9c1d7b9a10
Content-Type: application/json

{
  "provider": "MongoDBOASIS",
  "filters": [ { "field": "Name", "scalar": "String", "operator": "Contains", "value": "coffee" } ],
  "orderBy": "Name",
  "take": 10
}
```

## STAR CLI

`star cql variants`, `star cql example`, `star cql run <variant> --file query.json [--web4 <url>] [--idempotency-key <key>]`.

---

**← Back to the [WEB4 API index](README.md)**
