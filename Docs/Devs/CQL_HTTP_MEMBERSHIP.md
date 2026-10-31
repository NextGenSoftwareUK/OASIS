# CQL HTTP membership filters

Use `operator: "In"` with an array in `value` to select several exact values.
The WEB4 compiler produces one CQL membership predicate; it does not expand the
request into point queries or filter an unbounded scan in application memory.

For an enrolled native ActivityPub multi-status source:

```json
{
  "provider": "ActivityPubOASIS",
  "filters": [
    {
      "metadataPath": ["MastodonStatusId"],
      "scalar": "String",
      "operator": "In",
      "value": [
        "117466985005297414",
        "117466983307949301",
        "117411420916699261"
      ]
    }
  ],
  "orderBy": "None",
  "skip": 0,
  "take": 25,
  "includeMetadata": true
}
```

Send it to `POST /api/data/query-cql` with the authenticated WEB4 session and
normal usage-admission/idempotency headers. The example addresses public posts;
native records can be removed, so display the actual result rather than inventing
missing rows. The source must be configured, read-only activated and tariff-enrolled.
Never forward the WEB4 bearer to Mastodon. Readiness discovery is available from
`GET /api/data/query-cql/providers` in hosts shipping the source catalogue.

The HTTP bound is1–100 members. Each provider retains its independent bounds and
capabilities: ActivityPub's enrolled key-set profile accepts at most40 keys;
a point-only profile rejects `In`. Unsupported or oversized plans fail before
native execution, without point fan-out or alternate-provider fallback.

Canonical fields use their declared type. Metadata uses the explicit `scalar`.
Every member follows the same exact literal contract as a scalar comparison:

- Strings are JSON strings, up to512 characters before provider-specific limits.
- Integer and Boolean use exact JSON integer/Boolean values; mixed arrays reject.
- Guid uses D-format strings; DateTime uses the bounded UTC `Z` spelling with at
  most seven fractional digits.
- UInt64, Decimal, Double and Single use the existing canonical string encodings;
  values that round or lose precision reject the whole query.
- Null, nested arrays, empty arrays and more than100 members reject.

Core normalizes UInt64 literals to exact Decimal and Single literals to exact
Double while preserving the declared storage path/type guard. This preserves
value precision; it does not declare every provider capable of these scalar types.

Provider visibility, graph/window restrictions and overall request/result budgets
still apply. `In` is also available on supported canonical fields, for example
`field: "Id"` with an array of D-format GUID strings. No source permission is
granted merely by submitting an ID or native key.

## Federate a protocol source with MongoDB

A host shipping protocol federation accepts the same membership source through
`POST /api/data/query-cql/federation`. Omit `source.provider`: the outer scope owns
the concrete targets. For example:

```json
{
  "scope": "Named",
  "providers": ["MongoDBOASIS", "ActivityPubOASIS"],
  "source": {
    "orderBy": "None",
    "take": 25,
    "includeMetadata": true,
    "filters": [
      {
        "metadataPath": ["MastodonStatusId"],
        "scalar": "String",
        "operator": "In",
        "value": [
          "117466985005297414",
          "117466983307949301",
          "117411420916699261"
        ]
      }
    ]
  },
  "identityMode": "PreserveCopies",
  "maximumProviderAttempts": 2,
  "take": 25
}
```

This query does not import records. MongoDB must already hold the intended
owner-visible records with matching, legitimately queryable native-key metadata.
Three public source posts and two matching Mongo records produce five physical
rows; selecting the first two keys produces four. Inspect the actual counts and
provider labels, rather than assuming the data has been prepared.

WEB4 binds authorization independently for every target and admits the complete
native capability set before reading either provider. A tariff, permission,
capability or minimum attempt-budget failure rejects the complete query. Raising
the caller's attempt limit cannot raise the server's captured admission ceiling.
Per-attempt durable admission still applies at execution.

`AllEligible` includes the supported read profiles with query tariffs, rather than
silently omitting protocol sources. Its complete set must fit the server admission
and query budgets. Ordinary federation accepts at most eight named targets,
800 input rows and eight attempts, retaining the independent per-source bounds.
Unsupported common predicates reject; providers are never silently skipped.

Unordered sources and the additional read profiles require bounded requests
without continuation. The existing protected three-database cursor contract is
separate. Protocol row federation does not imply support for every composition,
aggregate, graph, migration or write operation.
