# CQL UInt64 metadata

24 October 2026. Core/SQLite implementation is verified. This does not declare
MongoDB, Neo4j, HTTP transport or full scalar-matrix completion.

## Core capture

UInt64 and nullable UInt64 retain their declared CLR path/key types. Literal
packets use the existing Decimal numeric family: all UInt64 integers fit Decimal
exactly, and no signed Int64 or binary64 conversion occurs. Literal values are
normalized to Decimal consistently. Lossless UInt64-to-Decimal conversions are
captured; conversions to binary32/binary64 and signed narrowing are rejected.
Integer conversion range checks use Decimal bounds to include the full UInt64
range. Grouping retains UInt64 identity through the existing detached proof owner.

## SQLite execution

SQLite json_each exposes numbers above Int64.MaxValue as REAL. The native CQL
operand instead reads the original JSON token through json_each.json/fullkey.
Only unsigned JSON integer tokens in0..18446744073709551615 are admitted. A
20-digit zero-padded binary-collated key provides exact comparison/order/group
identity. Literals use the same key. Native selected values decode with invariant
UInt64 parsing; malformed mandatory selections return CQL_DATA_INVALID.

This extends row comparisons, membership, field comparison, ordering, projection,
Min/Max and grouped keys/results through their existing owners. It introduces no
storage migration, entity scan, approximate cast or separate execution path.
Sum/Average remain their explicitly enrolled Int32/Int64 domains. No UInt64
numeric aggregate capability is inferred from the new metadata domain.

Fractional or out-of-range Decimal comparisons against UInt64 metadata remain
unenrolled and return CQL_CAPABILITY_UNSUPPORTED before SQL execution. They never
round or wrap into an integer. Actual provider capability checks remain mandatory.

## Evidence

Initial bounded gate51804 terminated0: both Core targets built, four capture
controls and11 native SQLite cases passed without skips. Full Core now passes
2,428 cases, zero failures/skips. A broad SQLite gate reached1,435 cases with
1,427 passes/eight failures: four new test expressions used forbidden captured
path arrays, and four independent search baselines belonged to the other session.
The test expressions now use explicit segments and the search owner repaired its
paths. The final unfiltered1,435-case gate now passes; exact evidence follows below.
The current fixed environment and cleanup ownership are recorded in
[the resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).


## Exact UInt64 Core/SQLite acceptance — 24 October 2026

Core9d5efa9 adds UInt64/nullable capture and grouped keys, with exact Decimal
literal packets and lossless conversion bounds. SQLite reads original JSON
integer tokens rather than rounded REAL values and uses20-digit ordinal keys.
No storage migration or reindex is required. Row predicates/membership/field
comparison/order, detached projections, Min/Max and grouped keys/result filters
share the existing generic planners, read owner and detached proof validation.
Mixed fractional/out-of-range literals reject explicitly; Sum/Average retain their
Int32/Int64 enrollment. Other providers/HTTP do not inherit UInt64 permission.

Actual52891 terminated0: **all1,435 SQLite integration tests pass, zero skips**,
including22 new UInt64 cases and four independently repaired legacy-search cases.
The separate full Core cohort passes**2,428 cases**, with four new capture controls.
Initial11 native cases passed; eight broader red cases were four test-expression
mistakes and four peer search baselines, all resolved before the final unfiltered
run. This is not a rerun of MongoDB/Neo4j suites or the63/63 HTTP demo.

Core/SQL/test source hashes stayed unchanged during the final gate. Producer/native
copies match Core35AE105E, Shared50CABB2E and SQLite014AAE08; the full Core unit
consumer matches the same Core producer. Exact identities/source hashes:
TestResults/SQLiteCurrent/uint64-artifacts.json and uint64-final-source-hashes.json.
Reports: TestResults/CosmicCurrent/uint64-full-current.trx and
TestResults/SQLiteCurrent/uint64-final-current.trx. Log:
TestResults/CosmicCqlDemo/uint64-final.log. The exact owned SQLite DB/WAL/SHM are
absent after teardown; no downloads/restores/new environments were needed.

Next: MongoDB and Neo4j UInt64 persistence/query domains, transport/examples,
then remaining Decimal/double coverage and the broader unchanged roadmap.
See [the UInt64 contract](CQL_UINT64_CONTRACT.md). The full goal remains active.
