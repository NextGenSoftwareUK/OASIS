# CQL developer manual

Current support and limitations: [parity checklist](CQL_PARITY_CHECKLIST.md). Active work/commands: [CURRENT_STATUS](CURRENT_STATUS.md).

Start with the maintained, spaced/commented [C# and HTTP demo](../../../Examples/CosmicCqlDemo/README.md).
CQL captures immutable query intent, plans against explicit eligible provider capabilities, then executes through native adapters and the V2 HolonManager owner.
Check every OASISResult error/acknowledgement before reading Result. Unsupported semantics and exhausted work limits are errors.

## Reference map

| Topic | Detailed preserved guide |
|---|---|
| C# query construction, provider scope, metadata, filters/order/windows | [Full manual](Archive/CQL_MANUAL_history_2026-10-27.md#cql-developer-manual) |
| HTTP/API, SDK examples, typed hydration, projections, joins/grouping, scalar domains | [Full manual](Archive/CQL_MANUAL_history_2026-10-27.md) and maintained demo |
| Child/descendant filtering and graph loading | [Graph reference](Archive/CQL_GRAPH_LOADING.md), [relationships](Archive/CQL_RELATIONSHIPS.md) |
| Native provider details | [MongoDB](Archive/CQL_MONGO_NATIVE.md), [SQLite](Archive/CQL_SQLITE_NATIVE.md), [Neo4j](Archive/CQL_NEO4J_NATIVE.md) |
| Projected All and source-composition caveats | [Active contract](CQL_PROJECTED_ALL_EXECUTION_CONTRACT.md) |
| Exact per-database feature support/proofs | [Parity checklist](CQL_PARITY_CHECKLIST.md) |

Detailed references retain historical checkpoints; use the checklist/current status for present support, not old dated completion claims.
Investor-demo rows are green. Projected All has native implementations on the three focus stores, but complete Q23e acceptance remains red.
Nested source relationship composition is Q49a: independent capability/expansion admission exists; native enrollment/translation proof is incomplete.
