# CQL selected-column HTTP contract

23 October 2026. Implementation in progress. This document specifies the next acceptance gate; it does not certify a deployed endpoint.

## Request and ownership

`POST /api/data/query-cql/projection` will use the existing bearer identity, eligible-provider admission, logical-intent idempotency key, resource filter and durable prepared-attempt/receipt owner. Actor identity is derived by the host. Provider may be MongoDBOASIS, SQLLiteDBOASIS or Neo4jOASIS, or omitted for the admitted current default. No implicit alternative provider is selected.

Root filters, relationship groups, protected access, ordering, Skip and graph consistency reuse the existing source compiler. Take defaults to25 and is bounded0–100; Skip remains0–1000. Graph options retain consistency intent and source traversal limits; selected-column results do not hydrate child graphs.

Columns contain1–32 entries. Each alias is unique (ordinal), begins with an ASCII letter and contains at most64 ASCII letters/digits/underscores. Each entry selects exactly one canonical field or one metadata path of1–4 bounded segments. Canonical types are inferred; metadata declares its exact ValueType. Nullable applies to value types; string results retain the existing nullable string contract. Native identifiers remain compiler-owned ordinals.

```json
{
  "provider": "Neo4jOASIS",
  "filters": [{ "field": "IsActive", "operator": "Equal", "scalar": "Boolean", "value": true }],
  "orderBy": "Name",
  "take": 10,
  "columns": [
    { "alias": "name", "field": "Name" },
    { "alias": "stock", "metadataPath": ["stock"], "valueType": "Int64", "nullable": false }
  ]
}
```

Filter enum names reuse the maintained row contract. Metadata projection types planned for v1 are String, Boolean, Int32, Int64, Byte, SByte, Int16, UInt16, UInt32, Guid, DateTime, HolonType and ProviderType. Actual adapter projection capability readmission remains mandatory. Unsupported DateTime metadata on Neo4j is rejected before provider execution.

## Results and limits

The response version is `cql-projection-v1`, with provider provenance, ordered column schema and ordered row value arrays. Int64 uses canonical decimal text so JavaScript retains all bits; Guid uses D text, DateTime uses UTC round-trip text preserving ticks, and defined enums use their canonical names. Other admitted integer widths remain JSON numbers. Null stays JSON null. No arbitrary serializer, getter or client-selected CLR type is executed.

The generic Core runtime record shape will use the same expression path compiler and native projection planner/executor as typed C# Select. All raw provider buffers validate before any record publication. HTTP formatting only converts already validated scalar values to the explicit wire representation. Response size is bounded1MiB; failure publishes no partial result. The existing linked HTTP cancellation/deadline owner remains in use; strict interruption of blocked Neo4j4.2 driver I/O remains an open platform limitation.

## Required evidence

- Generic runtime record intent: immutable bounded ordered columns, aliases, exact types, source access/window retention and rejection of arbitrary expressions/conversions without invocation.
- Existing native adapters consume the new shape unchanged; no entity hydration or parallel accounting implementation.
- Signed native HTTP queries on all three stores, privacy before paging, exact large Int64/null/GUID values and unsupported admission before dispatch.
- Invalid/duplicate/oversized shape/model inputs, cancellation/output bounds and no partial response.
- Repeated logical operation rejects execution; receipts and physical attempt counts remain correct, including affirmative no-execution rejection.
- Annotated C# and HTTP demo examples with clear spacing and the manual/architecture/runbook updated after verified acceptance.
