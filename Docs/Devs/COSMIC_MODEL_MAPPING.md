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


## Fluent scalar models

An immutable model can give a public scalar property its metadata key without an attribute:

```csharp
var builder = new CosmicModelBuilder();
builder.Entity<ScoreHolon>().Property(row => row.Score).HasMetadataKey("score_value");
var model = builder.Build();
if (model.IsError) throw new InvalidOperationException(model.Message);
using var session = CosmicSession.Create(manager, provider, access, model.Result).Result;
```

The declared entity currently derives from Holon and has a public parameterless constructor and public scalar getters/setters. Configure each property through its annotation or the fluent model. Existing annotated properties keep their annotation mapping; a fluent override of an annotated property is explicitly refused. Canonical holon identity, ownership and security fields cannot be renamed.

Build snapshots are independent: later builder edits do not affect an existing model. The same immutable model is bound before query expressions/windows and remains attached to session reads, source predicates/order/projections, scalar terminal captures, typed hydration and owned save preparation. Query/session model mismatch or model rebinding fails explicitly. There is no global per-type override or client-side query evaluation.

This initial slice covers flat scalar roots. Explicit model graph expansion is now source-integrated through selected child-role bindings and awaits its own native qualification. Unrestricted POCOs, XML mappings, annotation override precedence, inheritance conventions, converters and the full fluent ORM feature family remain open. Other query shapes still require their independently admitted native contracts. Follow the source/native/live qualification status in [the COSMIC status](../WIP/COSMIC%20ORM/CURRENT_STATUS.md); source presence alone is not parity or deployment acceptance.


## Fluent application constraints

Required values, maximum string/blob length and exact decimal precision/scale use the existing application validation rule owner:

```csharp
builder.Entity<InvoiceHolon>().Property(row => row.Caption)
    .IsRequired().HasMaxLength(80).HasMetadataKey("caption");
builder.Entity<InvoiceHolon>().Property(row => row.Amount)
    .HasPrecision(10, 2).HasMetadataKey("amount");
```

Unannotated custom properties need their metadata mapping before Build. Canonical or annotated properties can receive constraints without being renamed. Build freezes both aliases and rules; later builder edits leave existing sessions unchanged. Model constraints apply to the private authorized save snapshot after application callbacks, so replacing session callbacks cannot disable them. Failed validation dispatches no write. These are application constraints, not generated database columns, native schema constraints or a full validation/schema parity claim. Current verification and deployment scope remains in the COSMIC status.

## Automatic custom scalar naming conventions

Configure a whole flat model instead of writing an alias for every custom field:

```csharp
builder.Entity<Reading>()
    .UsePropertyNamingConvention(CosmicPropertyNamingConvention.SnakeCase);
```

PropertyName preserves CLR spelling; SnakeCase maps ReadingValue to reading_value and HTTPStatus to http_status. Explicit fluent aliases and inherited property annotations take precedence. Canonical Holon fields retain their names. Each Build resolves a separate immutable snapshot, so changing the builder convention does not change existing queries or sessions.

The convention applies to every public custom property in this flat scalar contract. A property outside the admitted scalar/public getter/setter contract, reserved key or collision makes Build return an error. Complex properties require their separately specified mapping support. Query, save and hydration use the same model map. Verification and deployment status remain in the COSMIC status; this does not establish full naming/model parity.

## Explicit models and selected child roles

An explicit model now travels with an admitted WithChildren query through typed child-role hydration. Configure each child CLR contract in the same model and register its role with HolonChildRoleBinding. Configured role members use the existing model metadata codec before role callbacks; a separate local metadata factory is unnecessary. Unconfigured roles retain their registered factory contract.

Root and child identities, parent links, committed revisions and stored role observations must remain unchanged. Native visibility and graph budgets still apply; omitted private members are not materialized and their complete stored relationship observation is retained. This source integration requires its new three-provider qualification before acceptance. It does not add unrestricted POCO relationships or make session writes implicitly save a graph.

Model constraints cover every node selected by an owned graph save after authorization, not only the roots. This does not cause a session root-only save to select children implicitly.

## Explicit models across joins

Each admitted join source retains its own immutable model, including when both sources use the same CLR contract with different metadata keys. Join scalar output capture and LINQ correlation keys now use those models; typed whole-entity results reuse the source query materializer. GroupJoin carrier scalar admission uses the left model, while matched right entities use their own source model.

The representative qualifier covers scalar and typed Join, matched/unmatched GroupJoin and correlated SelectMany. This is source-integrated pending its native qualification, and does not imply unrestricted computed joined expressions or all post-join composition.

## Declaring annotation-only entity contracts

Entity<T>() declares a contract in an explicit model even when its properties already have CustomOASISProperty annotations or use only canonical Holon fields:

```csharp
var builder = new CosmicModelBuilder();
builder.Entity<Reading>();
var model = builder.Build();
```

Explicit property mappings and naming conventions are optional for this declaration. Typed child-role materialization can use any contract declared in the same model, including an annotation-only contract. Build freezes the entity set; declaring additional types later does not change an existing model. The annotation-only qualifier45118868505 passed one native graph journey per MongoDB, SQLite and Neo4j store plus the WEB4 build. Its isolated fixtures were removed. Deployment acceptance is separate; consult current status before treating it as deployed.

## Per-model read-only entity contracts

```csharp
builder.Entity<Reading>().IsReadOnly();
var readModel = builder.Build();
```

Queries and typed hydration remain available. SaveChanges refuses inserts, updates and deletions of a configured read-only contract with COSMIC_MODEL_READ_ONLY_ENTITY. A policy on a base contract includes its runtime subtypes. Application edits and removal intents can still be staged; a prohibited persistence request returns an explicit error.

Policy belongs to the immutable model snapshot. Building another writable model for the same CLR type does not alter this model, and changing the builder later does not unlock existing sessions. Mandatory provider/caller permissions still apply. This is a persistence mapping policy; general immutable-object construction, keyless views and full M18 parity remain separate scope. The read-only qualifier45119523112 passed three native journeys and the WEB4 build; deployment status remains in the COSMIC status.

## Owned value objects

```csharp
builder.Entity<CustomerHolon>().OwnsOne(row => row.Address)
    .IsRequired().HasMetadataKey("mailing_address");
```

The complete value object is stored atomically in its parent's metadata. Its scalar CLR leaf chains bind to native metadata paths, so Address.City and Address.Region.Code can participate in admitted filters, ordering and scalar DTO projections. Root snapshot tracking detects nested value edits; saving the parent replaces the component value through the existing owned write pipeline. The parent's permissions govern the value.

This initial contract accepts acyclic plain public classes with public parameterless constructors and public non-indexed property getters/setters. Components can contain scalar leaves and recursively plain components. It rejects Holon/navigation/collection types, public fields and custom JSON/data-contract/type-converter mapping attributes. Shape limits are sixteen metadata segments, 256 scalar leaves and 1,024 shape nodes. It does not create separate component holon identities, schemas or custom serializers. Whole-component selected projections and broader owned/collection mappings require separate query contracts.

Capture freezes the alias and property chains without invoking getters. The final owned-value qualifier45124692924 passed all three representative native journeys and the WEB4 build, including null preservation. Deployment status remains separate in the COSMIC status.

Plain value properties and custom model fields retain explicit nulls through canonical snapshots and hydration, so constructor initializers cannot replace a submitted null. Authoritative record/audit field compaction retains its established policy; explicit serializer overrides remain respected outside the plain owned contract. The owned qualifier includes null-required-component refusal and a null nested string with a non-null constructor default.
