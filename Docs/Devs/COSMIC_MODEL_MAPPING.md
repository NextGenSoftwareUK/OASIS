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

Fluent scalar configuration, application constraints, naming conventions, selected child-role graphs, per-model read-only policies and owned plain value objects are described below with their representative native evidence. Converters, general inheritance mapping, schema migrations and unrestricted POCO mapping remain open roadmap work.
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

The scalar contract is also integrated with selected child-role graphs. The conventions/graph qualifier45115667791 passed six native journeys across MongoDB, SQLite and Neo4j, including graph identity, private-child exclusion and descendant validation. Unrestricted POCOs, XML mappings, annotation override precedence, inheritance conventions, converters and the full fluent ORM feature family remain open. Other query shapes still require their independently admitted native contracts. Follow the source/native/live qualification status in [the COSMIC status](../WIP/COSMIC%20ORM/CURRENT_STATUS.md); source presence alone is not parity or deployment acceptance.


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

Root and child identities, parent links, committed revisions and stored role observations must remain unchanged. Native visibility and graph budgets still apply; omitted private members are not materialized and their complete stored relationship observation is retained. The conventions/graph qualifier45115667791 accepted the selected graph integration on all three native providers. It does not add unrestricted POCO relationships or make session writes implicitly save a graph.

Model constraints cover every node selected by an owned graph save after authorization, not only the roots. This does not cause a session root-only save to select children implicitly.

## Explicit models across joins

Each admitted join source retains its own immutable model, including when both sources use the same CLR contract with different metadata keys. Join scalar output capture and LINQ correlation keys now use those models; typed whole-entity results reuse the source query materializer. GroupJoin carrier scalar admission uses the left model, while matched right entities use their own source model.

The representative qualifier covers scalar and typed Join, matched/unmatched GroupJoin and correlated SelectMany. The independent-model join qualifier45115044812 accepted these representative paths on MongoDB, SQLite and Neo4j. Unrestricted computed joined expressions and all post-join composition remain open.

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

## Current release boundary

Development revision7700707e includes the annotation-only, read-only and owned-value batch, with Core7186c806 pinned in both the gitlink and deployment manifest. Native qualifiers45118868505,45119523112 and45124692924 passed the three-store journeys; the exact image45125287105 and package45126351817 gates passed. All seven original service deployments subsequently succeeded. Before/after startup reads and an authorized HyperDrive status check confirmed exact WEB4 source7700707e and enabled load balancing, failover and replication. The temporary session was revoked and credentials removed. This adds deployment/settings acceptance; new business-query/browser acceptance is separate. The full EF/NHibernate roadmap remains incomplete.

## Ordered primitive and value-object collections

```csharp
builder.Entity<OrderHolon>().OwnsMany(row => row.Tags).HasMetadataKey("tags");
builder.Entity<OrderHolon>().OwnsMany(row => row.Lines)
    .IsRequired().HasMetadataKey("lines");
```

The declared property is a List<T> or one-dimensional T[]; elements are admitted scalar values or the same plain value-object shapes as OwnsOne. Collections are parent-owned values stored in canonical metadata arrays. Saving the parent persists list replacement, mutable nested changes, ordering and duplicate values atomically under its existing revision and permissions. A null collection and an empty collection remain distinct. IsRequired refuses null before dispatch, while an empty collection remains a valid non-null value.

Collection elements do not acquire separate Holon identities or independent write lifecycles. This batch does not add native element predicates, indexes, array joins or whole-collection query projections; those need their native array-query contracts. The corrected collection qualifier45130169196 passed all three MongoDB/SQLite/Neo4j journeys and the actual WEB4 build, with exact owned cleanup. Deployment acceptance is separate. Full M15 and general converter/comparer mapping remain open.

## Model-wide application query filters

```csharp
var minimumScore = 20;
builder.Entity<ScoreHolon>().HasQueryFilter(row => row.Score >= minimumScore);
var filteredModel = builder.Build();
// For an explicit application-filter bypass, create a separately bound model:
var unfilteredModel = filteredModel.Result.WithoutQueryFilters();
```

Build freezes captured literals and binds the predicate through the model's metadata aliases. Every query bound to that model's exact declared entity receives the application predicate before filtering/order/paging. Editing a closure or builder later does not alter an earlier model. To use different tenant/filter values, build an independent model snapshot; no global per-type or per-avatar mutable cache is installed.

WithoutQueryFilters removes application filters only. It preserves the same mapping, validation and read-only policies, and cannot remove mandatory authenticated caller visibility. Bind the returned model to its own query/session; existing model-affinity checks still forbid replacing the model on a captured query. This is an application selection policy, not an authorization boundary or a write constraint. Named selective filter bypass and inherited/polymorphic filter contracts remain separate scope. Representative native qualifier45129574750 passed all three MongoDB/SQLite/Neo4j journeys and the actual WEB4 build, with owned fixtures removed. Deployment acceptance is separate; full Q62 remains open.

Collection snapshots replace constructor-initialized custom/plain property values instead of appending their defaults. Explicit serialization overrides remain respected outside the plain owned contract; authoritative Holon fields retain their established policy. The first collection qualification exposed this shared issue; the corrected three-provider qualifier45130169196 verifies the replacement behavior alongside collection persistence.

## NHibernate XML mapping import

```csharp
var admittedTypes = new Dictionary<string, Type> { ["Customer"] = typeof(CustomerHolon) };
var imported = builder.ImportNHibernateXml(xml, admittedTypes);
// Check imported.IsError, then call Build and check its result.
```

The hibernate-mapping root uses urn:nhibernate-mapping-2.2. A class name resolves only through the explicit allowlist to a concrete public IHolon contract with a public parameterless constructor; the importer never loads a type named by the XML. Import into unconfigured contracts, then add ordinary fluent configuration as needed. Import validates a separate complete builder before changing the destination, so unsupported or conflicting input leaves it unchanged.

The current contract admits class name/mutable, scalar property name/column/type/not-null/length/precision/scale, an alternative single column child, and canonical Guid Id with an optional assigned generator. Property and column configuration cannot be split ambiguously. Scalar type names must agree with the CLR property; canonical identity/security fields cannot be renamed. Constraints and mutable=false use the existing application validation and read-only policy owners.

XML is bounded to1,048,576 characters and64 explicit allowed contracts. DTDs/external resources are disabled. Tables/schema generation, arbitrary NHibernate types/assemblies, POCO adapters, generators beyond assigned Guid, inheritance, relationships, collections, formulas, lazy proxies and unsupported attributes/elements return errors. This is the supported metadata-model import contract, not a claim of complete NHibernate XML compatibility. Native qualification and deployment status remain in the COSMIC status.

The XML syntax reference is the [official NHibernate mapping guide](https://nhibernate.info/doc/nhibernate-reference/mapping.html). This importer deliberately translates mutable=false to COSMIC explicit read-only persistence refusal, including inserts/deletes, rather than NHibernate immutable tracking behavior. That semantic difference is part of the current admission contract and prevents a full compatibility claim. Existing COSMIC UTC and numeric-domain rules continue to govern values.

The XML qualifier45132975102 passed three MongoDB/SQLite/Neo4j journeys, one XML-admission control and the actual WEB4 build, with exact owned fixture cleanup. This accepts the declared import subset and explicit COSMIC policies; deployment and broader NHibernate compatibility remain separate.

## Declared metadata properties without CLR fields

```csharp
builder.Entity<StockHolon>().ShadowProperty<int>("Stock")
    .IsRequired().HasMetadataKey("stock_quantity");
builder.Entity<StockHolon>().ShadowProperty<string>("Label").HasMaxLength(40);
var built = builder.Build();
// Use an open session bound to built.Result and a tracked entity:
var field = session.ShadowProperty<int>(entity, "Stock");
var staged = field.Result.SetCurrentValue(10);
var query = session.Query<StockHolon>().Result
    .Where(row => CosmicShadow.Value<int>(row, "Stock") >= 10);
// Check each OASISResult before using Result.
```

A shadow declaration records its exact CLR scalar domain, logical name, literal metadata key and application constraints in the immutable model. The existing metadata dictionary remains the sole persisted value; no companion holon or duplicate CLR field is created. Logical-name markers work inside model-bound native filters, ordering and scalar/DTO projections. Use a literal logical name and the exact declared generic domain. Undeclared/domain-mismatched markers remain outside the admitted query grammar.

Typed entries operate only on this session's tracked roots. GetCurrentValue, SetCurrentValue and RemoveCurrentValue recheck session/entity lifetime; absence is distinct from explicit null. Missing reads return COSMIC_SHADOW_VALUE_MISSING instead of inventing a CLR default. A present null is invalid for a non-nullable value domain. IsRequired refuses absence/null at save; other configured constraints use the existing authorized private-snapshot validation owner. Staging a value is not a persistence acknowledgement.

Build freezes keys/types/rules, rejects storage collisions with CLR/other shadow fields and preserves declarations when creating a filter-bypass model. Existing caller authorization still controls persistence and reads. This batch does not implement arbitrary custom CLR indexer access, notification proxies or unrestricted converted domains; full M16 remains open. Native qualification and deployment acceptance are separate.

Development3b81b749/Core13ca deployment and runtime settings are accepted for collections, application query filters and the XML subset. Isolated native behavior proofs retain their exact scopes; this does not infer hosted model CRUD/browser/real-WEB6 or full parity. New shadow-property code remains a later unqualified feature batch.

The fluent shadow qualifier45137770577 passed three native provider journeys and the actual WEB4 build, with exact owned cleanup. Deployment and full M16 remain separate.

## XML declarations for the metadata dictionary

```xml
<hibernate-mapping xmlns="urn:nhibernate-mapping-2.2">
  <class name="StockContract">
    <dynamic-component name="MetaData">
      <property name="Stock" type="Int32" column="stock_quantity" not-null="true"/>
      <property name="Label" type="String" column="stock_label" length="40"/>
    </dynamic-component>
  </class>
</hibernate-mapping>
```

Import uses the same explicit CLR contract allowlist. A single dynamic-component named MetaData declares scalar shadow fields over the existing canonical dictionary, without new CLR fields or companion records. Type is mandatory for each dictionary entry. Column aliases and application constraints share the ordinary XML parser and model admission owners. The resulting model works with the same typed session entries and CosmicShadow.Value markers.

An optional XML value-type field has a nullable CLR domain; not-null=true uses its non-nullable scalar domain and enforces presence. Reference-domain null handling follows the declared Required rule. Unsupported component targets, missing/unknown types, duplicate components/fields, reserved/colliding keys or non-scalar ordinary property declarations fail the complete import before destination mutation. Nested relationships/components and arbitrary CLR dictionaries/indexers remain separate contracts.

The syntax reference is [NHibernate dynamic components](https://nhibernate.info/doc/nhibernate-reference/components.html#components-dynamic). COSMIC's admitted subset maps the canonical metadata dictionary and retains the documented read-only/identity/query differences. The XML dictionary qualifier45140169734 passed three native journeys, one admission control and the actual WEB4 build, with exact owned cleanup. Combined image/package and deployment acceptance remain separate.


### Named application filters (native-qualified; deployment pending)
Entity<T>().HasQueryFilter("active", row => row.IsActive) adds or replaces
a case-sensitive named filter. Distinct names and the optional unnamed filter
are combined with AND before source paging. Build freezes captured literals.
model.WithoutQueryFilters("active") creates a separate immutable model removing
only that active named filter. Chaining selective bypass preserves prior removals.
Unknown, duplicate or invalid selections are refused. Names contain 1-128 letters,
digits, underscores, dots or hyphens; at most 64 names per entity are admitted.
WithoutQueryFilters() retains its existing explicit all-application-filter bypass.
Neither bypass removes mandatory caller visibility, mapping, validation or read-only
policy. These application filters are opt-in model rules, not provider-manager
collection filters. Exact-contract matching remains; polymorphic filters are open.


### Shadow entry comparison values and explicit flags (native-qualified; deployment pending)
A session ShadowProperty<TValue> entry supports GetOriginalValue(), GetValues(),
SetOriginalValue(value), RemoveOriginalValue(), and SetModified(bool).
These delegate to the existing canonical change tracker; there is no second baseline.
GetValues returns detached original/current JSON, separate existence flags, value
modification and explicit modification status. Missing metadata remains distinct
from JSON null; typed missing reads return COSMIC_SHADOW_VALUE_MISSING.
SetOriginalValue changes local comparison state only, never native revision evidence.
SetModified(false) restores current from that comparison original and clears its flag.
Normal acknowledged saves refresh originals and consume the captured modification
intent. All operations recheck session availability and membership; detached or
foreign-session entries cannot read or change these values. Required constraints
remain enforced at save. Arbitrary CLR indexers and full M16 parity remain open.


### Query tracking modes (qualification pending)
CosmicSession.Linq<T>(CosmicQueryTrackingMode mode) and
ReadAsync(query, CosmicQueryTrackingMode mode) admit Tracking, NoTracking and
NoTrackingWithIdentityResolution. Existing Boolean tracking APIs keep their meaning.
AsCosmicTracking(), AsCosmicNoTracking() and
AsCosmicNoTrackingWithIdentityResolution() select the mode on session LINQ queries.
Tracking uses the session identity map and retains pending local changes.
NoTracking returns native detached rows. NoTrackingWithIdentityResolution coalesces
repeated root identities within one result; those roots remain detached, and a fresh
map is used for each execution. Scalar/DTO projections are not root tracking entries.
Nested projected entity graphs are not recursively rewritten by this root contract.
Stateless LINQ supports both untracked modes and continues to refuse session tracking.
All modes use the same actor/authority-bound native reads and session operation/lifetime
gates; identity resolution never substitutes for a new access check. Undefined modes
are refused before execution. Full graph-wide ORM tracking parity remains open.
