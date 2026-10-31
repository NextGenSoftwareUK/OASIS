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
