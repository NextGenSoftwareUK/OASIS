# Native set-query family: implementation queue

Status: planned, not implemented or accepted. Q50/Q51/Q53/Q54 remain open.
Continue after the isolated stateless Get qualification44959006866. Preserve its exact Coreb8 source and results.

Deliver Union, Concat, Intersect and Except as a cohesive family through Core and real MongoDB/SQLite/Neo4j.
Keep typed API, standard LINQ and SQL-text exposure in the delivery scope; an entity-only first slice cannot complete selected-value/set equality requirements.
Preserve each input's filters, ordering and window before the operation, then independently apply result ordering and paging.
Bind both operands to compatible types, the same actor policy and named native authority. Do not silently combine authorities or drop either branch's visibility.
Cross-provider federation and additional providers remain explicit follow-on scope, not implicit certification.

Union/Intersect/Except remove duplicates according to the declared entity or selected-value equality domain.
Concat preserves duplicates and the left sequence before the right, including duplicate identity occurrences.
An outer Take must not hide an invalid, unauthorized-scope or over-budget input.
Use a single native statement/aggregation and existing hydration/error/permission owners, not two buffered client queries.
Preserve cancellation, separate operand/result limits and payload/work bounds; return OASISResult errors on unsupported semantics.
Do not introduce automatic read retries or claim transaction isolation beyond the provider's actual operation.

Implementation owners to inspect:
- Core Queries/CosmicQueryPlanning.cs, CosmicQueryExecutor, CosmicEntityCompositionPlanning and CosmicLinq.
- Core session publication/identity resolution: explicitly audit duplicate-row handling before enabling Concat.
- SQLite HolonRepository.Cql.EntityCompositionSql.cs and existing native parameter translation.
- Mongo aggregation compiler/executor and its read/transaction owner.
- Neo4j native entity/composition compiler. CI is pinned to Neo4j5.26; do not use Cypher25-only syntax.

Primary-source findings reviewed21November2026:
- SQLite supports compound UNION/UNION ALL/INTERSECT/EXCEPT. Independent operand windows require proper subquery boundaries; a compound's outer LIMIT is not an operand limit.
  https://www.sqlite.org/lang_select.html
- Mongo unionWith preserves duplicates and cannot run inside transactions. Its presence alone does not implement the whole set family or certify snapshot semantics.
  https://www.mongodb.com/docs/v8.0/reference/operator/aggregation/unionwith/
- Cypher UNION requires matching column names/counts; UNION ALL keeps duplicates; post-union processing uses a CALL subquery.
  Current documentation also includes newer features; retain the pinned5.26 contract.
  https://neo4j.com/docs/cypher-manual/current/queries/composed-queries/combined-queries/

Before coding the native form, select and document one operation owner compatible with current protected-read and transaction invariants.
Representative new three-store acceptance should cover overlapping operands, distinct versus duplicate multiplicity, independent input/result windows and an excluded foreign private row.
Keep broader converted-value equality, composition and edge coverage explicit rather than multiplying scalar variants before feature breadth.
No parity credit, CI dispatch, local installation, shared-cluster query or deployment from this planning note.
