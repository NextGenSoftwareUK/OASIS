# COSMIC model mapping

## Explicit metadata storage keys

A custom CLR property can declare the metadata key used for persistence and CQL:

```csharp
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.Core.Holons;

public sealed class Reading : Holon
{
    [CustomOASISProperty(MetadataKey = "measured_value")]
    public int ReadingValue { get; set; }
}
```

The write pipeline stores this value under `MetaData["measured_value"]`.
It does not add `ReadingValue` as a second metadata key. Typed hydration and
native predicate/projection capture use the same storage-name owner.

An open, actor-bound COSMIC session can query the CLR property:

```csharp
var query = session.Query<Reading>().Result
    .Where(row => row.ReadingValue > 20).Result;
var rows = await session.ReadAsync(query, cancellationToken: cancellationToken);
// Check rows.IsError before consuming rows.Result.
```

Changing a tracked `ReadingValue` and saving through `SaveChangesAsync`
updates that same metadata key. Source predicates and projections are
translated to the shipping provider; query capture does not execute property
getters to discover their values.

A null `MetadataKey` preserves the existing CLR property name. Keys are literal,
bounded to 128 characters, and must contain valid Unicode without NUL.
Explicit aliases cannot shadow authoritative Holon fields or collide within
the declared model. Native scalar paths require a public getter/setter and an
admitted scalar domain. `StoreAsJsonString` retains its existing codec behavior;
JSON-string properties need their separately admitted query shape.

Choose a stable key before creating records. A later key change requires an
explicit model/data migration; this feature does not rename or backfill records.

## Delivery evidence and remaining scope

[The isolated native qualifier](../WIP/COSMIC%20ORM/MODEL_METADATA_ALIAS_NATIVE_ACCEPTANCE_2026-11-22.json)
passed one create/read/query/update journey on each real MongoDB, SQLite and
Neo4j provider. Its representative mapped value is Int32. It also verifies
collision/reserved-key refusal before getters, and owned-fixture cleanup.

Intrinsic annotation names are cached with weak type keys. The cache contains
immutable property/name information and no avatar, application, connection or
permission state.

Full fluent model configuration, converters, complex/owned types, inheritance,
schema migrations, and unrestricted POCO mapping remain separate roadmap work.
Full/virtual/hybrid source policies govern where data resides; this attribute
defines a CLR property's Holon metadata key and does not introduce a new import
or virtual-source lifecycle.

## Typed identity groups

Mapped scalar properties also work as grouping keys. Group members are hydrated through the same typed materialization pipeline as flat queries, preserving metadata aliases and session identity observation. The three native provider journeys verify this integration after a tracked mapped-field update. This does not claim full fluent-model or unrestricted grouping parity.

## Typed join results and grouped matches

Mapped scalar keys can participate in native inner joins and GroupJoin. Selecting the right entity from an ordinary inner join uses its admitted right source; parent ancestry is required only when the source is an actual child-navigation join. GroupJoin can return the typed left model, complete typed matches, mapped scalar carrier fields and Count/LongCount/Any summaries. An unmatched group preserves its left model and an empty matching collection.

All native pair proofs and budgets are validated before application models are hydrated. Manager-bound entity joins use the same typed hydration/decryption/selection owner as flat and grouped reads. Repeated identities within an admitted source are materialized once for that result. Direct CosmicJoinExecutor calls retain their canonical detached-output contract; use HolonManager or a bound COSMIC query for application models.

[The mapped-join native journey](../WIP/COSMIC%20ORM/MAPPED_JOIN_NATIVE_ACCEPTANCE_2026-11-23.json) passed on MongoDB, SQLite and Neo4j after an update to the mapped value. It covers an ordinary typed right-entity join, matched typed groups and unmatched groups without additional fixture writes. This is representative functional evidence, not unrestricted join composition, session tracking of nested carriers or full EF/NHibernate parity.
