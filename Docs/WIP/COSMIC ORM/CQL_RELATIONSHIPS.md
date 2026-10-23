
SQLite native bounded descendant implementation and canonical empty-parent validation are documented in [the traversal contract](CQL_DESCENDANT_CONTRACT.md). Direct-child and descendant membership checks now share the same observation owner; the other providers retain their existing native owners.

Neo quantified descendant execution now passes316unit/45native/joint109, including inline excluded-branch pruning, duplicate-edge budgets, exact metadata and cycle validation. See [descendant semantics/runtime](CQL_DESCENDANT_CONTRACT.md#neo4j-native-execution). Mongo descendant execution remains pending.
