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
