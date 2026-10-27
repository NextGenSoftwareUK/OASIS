# CQL completion roadmap

Current phase: [CURRENT_STATUS](CURRENT_STATUS.md). The [parity checklist](CQL_PARITY_CHECKLIST.md) remains authoritative.

1. Q49a:216 native source checks pass across MongoDB, SQLite and Neo4j; freeze verified source composition.
2. Q23e: completed168-check native gate; freeze verified projected-All behavior. Parent Q23 audit and24 shipping C# demo examples now pass; freeze these rows.
3. Promote a database cell only when proving native evidence covers its full row. Unit/structural compilation does not establish native parity.
4. Source/projected-All C# demos and maintained docs are updated; the24-example native demo passes. Continue Q01 IQueryable/IQueryProvider integration: selected elements, selected cardinality and entity cardinality predicates after windows are verified; SQLite signed-integer staged Sum/Average now verified (Q01a); next MongoDB then Neo4j Q01a, wider domains Q01b and selected Min/Max Q01c. Keep Q01 red until full scoped proof.
5. Select the next red row; leave green features and federation cursors alone unless a reproducible bug requires a root fix.
6. Finish large parity items, including POCO mapping and migration tooling, at their documented priority after demo/current priority acceptance.
7. Perform a final requirement-by-requirement audit before claiming full EF/NHibernate or COSMIC completion.

The 12 investor-demo rows are green. They are a scoped milestone, not full ORM parity.
Source APIs remain capability-aware and provider-scoped; unsupported forms fail explicitly. No client materialization, silent retry or fallback may replace a missing native implementation.
[Full roadmap/history](Archive/CQL_COMPLETION_ROADMAP_history_2026-10-27.md) retains the earlier acceptance criteria.
