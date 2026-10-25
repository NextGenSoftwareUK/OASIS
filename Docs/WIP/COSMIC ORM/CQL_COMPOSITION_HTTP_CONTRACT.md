# CQL selected-result composition over HTTP v1

Implementation date: 25 October 2026. Native HTTP acceptance passes on all three
providers; final evidence and remaining scope are recorded below.

`POST /api/data/query-cql/composition` runs ordered result stages after a bounded
projection source. It uses the generic Core/HolonManager composition planner,
proof verifier and provider execution owner on MongoDB, SQLite and Neo4j.

The source window and result windows have different meanings. For source rows
`C-public, D-owned`, `Where(Name != C-public).Take(1)` returns `D-owned`.
`Take(1).Where(Name != C-public)` returns no rows. Providers must preserve the
declared order; they cannot push a selected filter ahead of a preceding window.

## Request

Authenticate with a WEB4 bearer token. Supply one `Idempotency-Key` per logical
request and retain that key after an uncertain outcome. A repeated key is rejected
before another provider attempt. Actor identity comes from the server.

```json
{
  "provider": "MongoDBOASIS",
  "source": {
    "filters": [
      {
        "metadataPath": ["fixture"],
        "scalar": "String",
        "operator": "Equal",
        "value": "REPLACE_WITH_DEMO_FIXTURE_TAG"
      }
    ],
    "orderBy": "Name",
    "skip": 1,
    "take": 2,
    "columns": [
      {
        "alias": "Name",
        "field": "Name"
      },
      {
        "alias": "Precision",
        "metadataPath": ["precision"],
        "valueType": "Int64"
      }
    ]
  },
  "stages": [
    {
      "operation": "Where",
      "filters": [
        {
          "alias": "Name",
          "operator": "NotEqual",
          "value": "C-public"
        }
      ]
    },
    {
      "operation": "OrderBy",
      "order": [
        { "alias": "Precision", "descending": true },
        { "alias": "Name", "descending": false }
      ]
    },
    {
      "operation": "Take",
      "count": 1
    }
  ]
}
```

Use `SQLLiteDBOASIS` or `Neo4jOASIS` to select another eligible provider. Omitting
the outer provider uses the host's admitted default. This v1 route selects one
provider; it does not expose a federated scan. `source.provider` is forbidden.
The source uses the existing projection field/metadata/relationship description.
Graph expansion sources are not enrolled for composition and are rejected.

## Stages and exact values

| Operation | Required fields | Meaning |
|---|---|---|
| `Where` | `filters` and/or `anyOf` | AND all filters with an optional OR group |
| `OrderBy` | `order` | First term starts a new ordering; later terms are ThenBy |
| `Skip` | `count` | Skip selected rows at this exact point |
| `Take` | `count` | Limit selected rows at this exact point; zero is meaningful |

Unused stage fields are rejected. Aliases are exact, case-sensitive selected
column names. They cannot introduce new source paths or native identifiers.
Each Where comparison names an alias, operator and exact typed value; supported
operators are equality/inequality, applicable relational comparisons, ordinal
string Contains/StartsWith/EndsWith, and IsNull. IsNull omits `value` and requires
a nullable column. Selected aliases always exist, so Exists is rejected.

Int64, UInt64, Decimal, Double and Single literals use invariant JSON strings.
For example, compare Precision with `"9007199254740993"`, not a JavaScript number.
GUIDs use canonical D strings; DateTime uses exact UTC text. Small integers use
JSON integers, Boolean uses JSON Boolean, and enums use exact defined names.
Null is accepted only by nullable selected columns. No selector delegate is
compiled or executed by this transport.

## Bounds and publication

| Boundary | Limit |
|---|---:|
| Request body | 32 KiB |
| Selected columns | 1–32 |
| Ordered stages | 1–16 |
| Selected comparison terms across stages | 32 |
| Terms in one OrderBy / across stages | 8 / 32 |
| Source Take / published rows | 0–100 / 100 |
| Selected Take / Skip | 0–100 / 0–10000 |
| Source proof payload | 1 MiB |
| Cumulative stage input work | 1600 rows |
| Stage identity proof payload | 25600 bytes |
| Public serialized response | 1 MiB |
| HTTP cumulative deadline | 5 seconds |

Limits reject excess work rather than silently truncate a successful result.
All selected source cells are validated before a later result filter/window can
hide required-value errors. Core verifies complete native stage membership and
order before constructing detached records. Output uses the projection wire
format with `version: "cql-composition-v1"`, provider, columns and rows; exact
large/numeric/date values retain their existing string representation.

The shared exact CQL route registry requires actor-bound storage admission and
query tariffs. A complete composition owns one admitted provider query attempt
and receipt; internal native commands stay beneath that owner. SQLite/MongoDB
use their snapshot owners. Neo4j captures cells once in one native statement
under read committed and does not promise a repeatable snapshot.

## Developer demo

Run `Scripts/TestHosts/Run-CosmicCqlV1Demo.ps1` for the console walkthrough, or
add `-Serve` and open the reported `/demo` or `/swagger` URL. Examples32–34 in
`Examples/CosmicCqlDemo/V1NativeQueries.cs` and `V1Walkthrough.cs` show the same
three composition queries in spaced, commented C# and HTTP code. Three browser
presets reproduce their operation order. Swagger includes a composition body;
replace its fixture tag using the current demo session.

See the [manual](CQL_MANUAL.md), [architecture](CQL_ARCHITECTURE.md),
[composition ownership/proof contract](CQL_COMPOSITION_CONTRACT.md) and
[remaining CQL roadmap](CQL_COMPLETION_ROADMAP.md).

## HTTP composition and annotated demos — 25 October2026

`POST /api/data/query-cql/composition` now uses the generic Core/HolonManager
composition planner/proof barrier on MongoDB, SQLite and Neo4j. The exact route
registry requires authenticated actor-bound admission and query tariffs. Stage
order is explicit; nested provider/graph overrides and ambiguous stage fields
are rejected before dispatch. Grouped/composed filters and ordering share one
selected-value binding owner, and projection/composition share exact wire output.

Final gates pass **2,940/2,940 Core/linked WEB4 tests**, zero skips (63new), both
Core frameworks, **102 SDK /102 signed HTTP demo examples**, and the dedicated
joint native test1/1. The live fixture settles **309 operations /301 provider
attempts**. New native controls verify owner/visitor visibility, stage order,
exact Int64 output, repeated-key denial without replay, and malformed-stage MVC
rejection with zero attempts/receipts. Twenty offline browser presets and replay/
exact JSON checks pass; the standalone demo builds with zero errors. Source
identity snapshots remained unchanged throughout the final native gate.

Examples32–34 in `V1NativeQueries.cs` and `V1Walkthrough.cs` demonstrate source
paging versus result paging, filter/window order and exact selected Int64 values.
The browser and Swagger offer matching composition requests. See the
[HTTP contract and examples](CQL_COMPOSITION_HTTP_CONTRACT.md).
Evidence: `TestResults/CosmicCqlDemo/composition-http-core-final.log`,
`TestResults/CoreCurrent/composition-http-current.trx`,
`TestResults/CosmicCqlDemo/composition-http-native-final.log`,
`TestResults/Neo4jCurrent/neo4j-cql-v1-demo-current.trx`,
`TestResults/CosmicCqlDemo/v1-demo-result.json` and
`TestResults/CosmicCqlDemo/composition-browser-presets.log`.

This closes bounded flat selected-result composition across SDK/HTTP on the
three providers. Graph-source composition is explicitly unadmitted. Broader
CQL relationship/join/group/scalar certification, authoritative tenant/sharing
policy, federation and the remaining ORM roadmap still require work.
