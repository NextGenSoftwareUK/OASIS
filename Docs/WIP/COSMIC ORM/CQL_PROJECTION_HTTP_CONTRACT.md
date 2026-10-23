# CQL selected-column HTTP contract

23 October 2026. Endpoint implementation and signed native acceptance are verified on the task branch. Final task-branch acceptance now includes fullCore2,059, broad native147 and fresh30SDK/30HTTP walkthroughs. Deployment integration remains separate.

## Request and ownership

`POST /api/data/query-cql/projection` uses the existing bearer identity, eligible-provider admission, logical-intent idempotency key, resource filter and durable prepared-attempt/receipt owner. Actor identity is derived by the host. Provider may be MongoDBOASIS, SQLLiteDBOASIS or Neo4jOASIS, or omitted for the admitted current default. No implicit alternative provider is selected.

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

The generic Core runtime record shape uses the same expression path compiler and native projection planner/executor as typed C# Select. All raw provider buffers validate before any record publication. HTTP formatting only converts already validated scalar values to the explicit wire representation. Response size is bounded1MiB; failure publishes no partial result. The existing linked HTTP cancellation/deadline owner remains in use; strict interruption of blocked Neo4j4.2 driver I/O remains an open platform limitation.

## Required evidence

- Generic runtime record intent: immutable bounded ordered columns, aliases, exact types, source access/window retention and rejection of arbitrary expressions/conversions without invocation.
- Existing native adapters consume the new shape unchanged; no entity hydration or parallel accounting implementation.
- Signed native HTTP queries on all three stores, privacy before paging, exact large Int64/null/GUID values and unsupported admission before dispatch.
- Invalid/duplicate/oversized shape/model inputs, cancellation/output bounds and no partial response.
- Repeated logical operation rejects execution; receipts and physical attempt counts remain correct, including affirmative no-execution rejection.
- Annotated C# and HTTP demo examples with clear spacing and the manual/architecture/runbook updated after verified acceptance.

## Verified implementation checkpoint

Core945c65e supplies the shared runtime-record shape; parentc692 pins it coherently. FullCore2,059 unit/linked-HTTP controls pass unskipped, including30compiler and11endpoint cases. Dedicated signed native acceptance passes1/1 in24seconds on all three stores, in endpoint-only mode; fresh walkthrough execution remains in the final console-mode gate. Actual native queries verify visibility before projection paging, exact Int64/null/GUID/nested values and provider provenance. Replay preserves one physical attempt/receipt. Invalid aliases on all three stores and unsupported Neo4j DateTime projection settle without execution. Mock endpoint controls additionally cover exact UTC tick formatting, defined enums, output escaping overflow, unsupported read permissions and pre-cancel rejection.

Expanded annotated10-example C#/HTTP walkthroughs, browser preset and Swagger schema example are implemented; their fresh30/30acceptance and broad147 native regression now pass unskipped (2m28s), with195settled operations/190physical attempts. Strict blocked Neo4j driver interruption and later aggregate/grouped/join/federation gates remain open.

## Final acceptance — 23 October 2026

`Run-CosmicCqlV1Demo.ps1 -IncludeRegression` passes the broad147 native cases plus fresh30SDK/30HTTP completed queries in one reused environment lifecycle. The wrapper enables console mode, rejects stale proof and checks both actual counts. All195operations settle and190physical attempts remain accounted for. Generated Swagger includes all three routes and the selected-column schema example; browser JavaScript syntax passes through the retained Node runtime.

Evidence: `TestResults/CoreCurrent/cql-projection-http-endpoint-current.trx`, `TestResults/Neo4jCurrent/neo4j-cql-v1-web4-current.trx`, `TestResults/CosmicCqlDemo/v1-demo-result.json`, `TestResults/CosmicCqlDemo/v1-walkthrough.txt` and `TestResults/CosmicCqlDemo/projection-http-final-demo-regression.log`. The frozen native cohort matches Core945c65e/source721/DLL9662A03A33F03C7FB34BA670F4F429AF0A00298953FB101AA0D8B48794CA2089 and Shared55/DLLD422BD357E113686F3A76C0713F4164ED702D57F70A996CB937737EF11EAA38A. Source-aware producer/consumer checks pass. Owned fixture databases/files are cleaned by their verified lifecycle, and retained services stop. The resource ledger records exact paths and retained evidence.

Min/Max, Sum/Average, GroupBy, joins/federation, extended scalar/authoritative-policy coverage and strict blocked Neo4j driver-I/O interruption remain open roadmap gates. This completed projection slice does not mark the broader goal complete.
