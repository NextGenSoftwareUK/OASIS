# Complete grouping collections: implementation handoff

Status: design only. Raw IQueryable GroupBy materialization still returns an
explicit unsupported result. Do not credit Q40 or full parity from this file.

## Required behavior

Materialize complete, bounded IGrouping<TKey,TElement> collections from real
MongoDBOASIS, SQLLiteDBOASIS and Neo4jOASIS sources. Preserve source predicates,
source ordering/windows, caller access, independent grouping/output budgets,
null/composite key equality, and element selector semantics. Excess inputs or
payload must produce an error, never a partial group.

## Implementation direction

Add an explicit group-member plan and capability contract on the shipping
providers, using native key/element projections and complete source-count proof.
The Core owner assembles immutable group objects from validated keyed member
rows. It must not compile key/element delegates, evaluate unsupported query
predicates over loaded entities, or fall back from aggregate grouping to a
generic load-all route.

Reuse the actual native source projection machinery and snapshot entity
hydration already used by joins. Do not implement a self-join over every pair
or invent a singleton right-hand source: neither is needed for grouping.

Inspected reuse points:

- MongoCqlTranslator.Join.cs: local Source<T> compiler inside CompileJoin.
- Mongo HolonRepository.Cql.Join.cs: local ReadSource<T> decoder (nativeCount,
  unique identities, access proof, typed keys/values/order), plus snapshot
  ReadSelectedEntities. The latter uses batched native ID reads under the same
  session as membership proof.
- SQLite HolonRepository.Cql.JoinSql.cs: local Source<T> SQL compiler; pair
  execution already owns a read transaction and native entity hydration.
- Neo4jOASIS.Cql.JoinCompiler.cs: local Source<T> compiler; the decoder and
  execution own a native read transaction and snapshot identity checks.

These helpers currently close over join selection details. Extract their
shared source responsibilities with explicit source/key/selected-column inputs;
keep join behavior unchanged. Do not copy a second, diverging normalizer or
use a fake join to reach otherwise inaccessible helpers.

Validate every native row and the complete input count before constructing
keys/elements or invoking DTO constructors/setters. Detach entity snapshots with
the existing stored-record/selection-observation owner and enforce caller access
and payload accounting. Scalar/anonymous elements should use native projected
cells and the existing detached shape materializer.

Keep individual input/member limits distinct from number-of-groups limits.
Where hydration requires more than one physical read, keep the existing native
snapshot/transaction owner for membership and payload. Empty sources, native
failures and source/payload limits must have explicit OASISResult outcomes.

## Representative qualification

One cohesive real-store journey per provider should cover complete populated
groups, an empty source, preceding source window, original member order,
scalar/anonymous elements and caller visibility. Retain accepted aggregate and
GroupJoin evidence unless a changed source/helper requires its representative
regression. Use only owned loopback databases in CI; no shared Atlas/PIPER data.
