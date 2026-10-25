# CQL scalar capability matrix

25 October2026. This table separates stored metadata domains from aggregate result
types. A query must still pass actual-provider admission and execution budgets.
Historical test cohorts are not interchangeable with a new full-suite gate.

| Domain/operation | SQLite | MongoDB | Neo4j |
|---|---|---|---|
| String, Boolean; nullable supported where declared | Native | Native | Native |
| Byte/SByte/Int16/UInt16/Int32/UInt32/Int64 | Native exact integer domains | Native BSON Int32/Int64 domains | Native integer projection domains |
| HolonType/ProviderType | Defined enum values | Defined enum values | Defined enum values |
| Guid | Canonical D text identity | Supported native UUID/text identity | Canonical D text identity |
| UTC DateTime | Exact JSON calendar text | BSON date plus exact tick companion | Exact schema3 calendar text |
| UInt64/nullable UInt64 | Verified1,435 integration cohort incl22 new cases | Verified205 CQL/478 unit cohort incl14/5 new cases | Verified205 CQL/453 unit cohort incl13/5 new cases |
| Decimal/nullable Decimal metadata | Verified1,459 integration cohort incl24 Decimal cases | Verified228 CQL/483 unit cohort incl23/5 new cases | Verified217 CQL/455 unit cohort incl12/2 new cases |
| Double/nullable Double metadata | Verified1,485 full integration cohort incl26 Double cases | Verified254 native CQL/493 units incl23 new Double cases | Verified232 native CQL/457 units incl15/2 new Double cases |
| Single/nullable Single metadata selectors | Verified 1,516 full native cohort, including 31 Single cases | Verified 281 native / 494 unit cohort, including 24 Single query cases | Verified 249 native / 459 unit cohort, including 16 Single query cases |
| Count/LongCount/Any | Native | Native | Native |
| Min/Max | Enrolled selected scalar domains | Enrolled selected scalar domains | Enrolled selected scalar domains |
| Sum/Average inputs | Int32/Int64/UInt64/Decimal, nullable allowed | Int32/Int64/UInt64/Decimal, nullable allowed | Int32/Int64/UInt64/Decimal, nullable allowed |
| Average result, selected filters/order | Exact Decimal normalization | Exact Decimal normalization | Exact Decimal normalization |

The [floating foundation contract](CQL_FLOATING_SCALAR_CONTRACT.md) records the
shared finite bit-key codec and Mongo Single persistence prerequisites. Double SDK enrollment passes full native gates on all three providers.
Single SDK and HTTP/demo acceptance are verified on SQLite, Mongo and Neo; Double HTTP/demo acceptance now passes81SDK/81HTTP on all three providers.

UInt64 arithmetic is explicitly enrolled and verified on all three stores, including
111SDK/111signedHTTP demos. SQLite Decimal arithmetic is explicitly enrolled (1,702native/72unit), while
MongoDB Decimal arithmetic passes23 focused native tests and550 units, with full416 native acceptance passing without failures or skips. Neo4j Decimal arithmetic passes416 native/529 unit tests without failures or skips. The
[scaled-packet contract](CQL_DECIMAL_AGGREGATE_CONTRACT.md) defines exact reduction
and rounding. Decimal HTTP arithmetic remains outside published v1 enrollment.
Decimal Average output does not itself grant Decimal metadata selection.
All three providers' explicit Decimal enrollment is documented in the
[Decimal contract](CQL_DECIMAL_METADATA_CONTRACT.md). Arbitrary enum types and client functions are
not admitted by these profiles. Provider target scope remains explicit.

Stored representations differ: MongoDB UInt64 admits bounded integral BSON
Decimal128 as well as nonnegative signed BSON integers. SQLite/Neo4j require the
admitted JSON integer representation. Existing numeric-looking strings are not
implicitly converted. Use typed UInt64 values when writing portable unsigned
counters; schema-driven conversion of historical strings requires explicit intent.
Mixed fractional/out-of-range literals remain an explicit provider admission
restriction where exact normalized-key translation is not enrolled.

The UInt64 HTTP extension is additive and passes20 new wire controls inside the
full2,474-case Core cohort: scalar UInt64, selected UInt64/nullable schemas and
selected group alias predicates use exact digit strings. Live UInt64 HTTP/demo
acceptance passes69SDK/69HTTP examples; the earlier63/63 date demo retains its own scope.

Current contracts: [UInt64](CQL_UINT64_CONTRACT.md), [UTC dates](CQL_UTC_DATETIME_CONTRACT.md),
[numeric aggregates](CQL_NUMERIC_AGGREGATES.md), [manual](CQL_MANUAL.md),
[remaining roadmap](CQL_COMPLETION_ROADMAP.md).


Decimal source predicates, selected/nullable schemas and grouped alias literals
pass23 new wire controls inside the full2,500 linked-unit cohort. Request/result
values use exact invariant strings. Live Decimal HTTP/SDK/Swagger
acceptance passes75SDK/75HTTP examples on all three providers; browser script
behavior passes the reusable offline check (no interactive browser run claimed); this does not grant Decimal Sum/Average inputs.

Double source filters, selected/nullable schemas and grouped alias literals now
pass35 new compiler/endpoint controls inside the full2,542 linked-unit cohort.
Request/response values are canonical invariant finite binary64 round-trip
strings. Existing enum identities remain stable (Double appended as7).
Live all-provider Double HTTP/demo acceptance passes81SDK/81HTTP; no Single or
floating Sum/Average capability is granted.

Mongo's newer full255native/493unit cohort includes a verified native binary64
toSingle component with12,313vectors. Its Single row/selected/grouped query
profiles remain unenrolled pending exactInt64/Decimal128 source normalization
and complete query acceptance; the table's Single status remains unchanged.

Mongo's latest full cohort passes 256 native CQL tests and 493 unit tests,
including verified binary64 and exact integer Single components (12,313 and
9,700 vectors respectively). Mongo Single query capability remains **not
enrolled** until original Decimal128 and complete query execution are verified.

Latest Single component gate: Mongo 257 native / 493 unit tests pass including
1,873 original Decimal128 vectors. Binary64, integer and Decimal128 components
are verified; full Mongo Single query capability remains disabled pending native
query integration. SQLite's promotion correction passes full 1,516 native tests
and Core 2,558 tests; widened Double thresholds are never narrowed to Single.

## Mongo Single query enrollment — 24 October 2026

Supersedes the historical component-only checkpoints above. Original BSON
Double, Int32/Int64 and Decimal128 dispatch to their certified binary32 owners.
Single/nullable predicates, membership, field comparisons, ordering/paging,
selected values, nullable group keys, grouped filtering and extrema now run
natively. Gate33648 passes281native/494unit cases, zero failures/skips, with
unchanged sources and matching producer/consumer identities. Neo Single, Single
HTTP/demos and floating Sum/Average remain outside this acceptance.

NeoSinglequery enrollment now passes249native/459units, with16newnativequery
cases plus original209midpoint vectors. SingleHTTP/demos are still pending.
See the floating contract for exact scope and final76459 evidence.

Single HTTP/compiler/endpoint coverage passes 2,595 linked tests, including 37
new controls. The fresh live gate passes 87 SDK and 87 signed HTTP examples,
with 267 settled operations and 262 provider attempts across all three providers.
Six offline browser presets and Swagger examples also pass. Earlier notes that
Single HTTP is pending are historical. Floating Sum/Average and broader unchecked
certification remain open. See gate 37983 in the floating contract.


Latest Neo4j transport regression:267 native CQL tests pass on committed867182ad8,
including 18 cold/warm cancellation/deadline cases. Six driver tests and 465 provider unit tests
pass at their separate checkpoints. These extend transport certification rather
than granting additional scalar operators. Current live walkthrough acceptance
is87 SDK / 87 signed HTTP examples across three stores; older cohort counts above
retain their original domains. See [transport evidence](CQL_DESCENDANT_CONTRACT.md).
