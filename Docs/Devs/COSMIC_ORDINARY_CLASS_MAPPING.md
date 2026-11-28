# Ordinary classes in COSMIC ORM

Qualification: native MongoDB, SQLite and Neo4j acceptance plus actual WEB4 build passed46104535369 on Core0623246c. Included in this qualified Development batch; running deployment acceptance remains pending. Exact evidence belongs in Docs/WIP/COSMIC ORM/GPT_M01_LEDGER_2026-11-28.md.

Business classes need no Holon base, IHolon interface, OASIS attributes or generic new() constraint. An immutable CosmicPocoModel<T> describes stored properties. CosmicPocoContext<T> binds that model to one actor and one shipping provider. Internal canonical Holon envelopes own native identities, audit and accepted revisions; business values use declared metadata fields. Existing native CQL and transaction owners enforce queries, permissions and acknowledged writes.

## Mapping and queries

```csharp
public sealed class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public int Quantity { get; set; }
    public string Display => Name + " / " + Quantity;
}

var mapping = new CosmicPocoModelBuilder<Product>("shop_products_v1");
mapping.Ignore(p => p.Display);
mapping.Property(p => p.Name).IsRequired().HasMaxLength(120)
    .HasMetadataKey("product_name");
var model = mapping.Build();
if (model.IsError) throw new InvalidOperationException(model.Message, model.Exception);

var access = CosmicQueryReadAccess.OwnerOrPublic(actorId);
if (access.IsError) throw new InvalidOperationException(access.Message, access.Exception);
var opened = CosmicPocoContext<Product>.Create(manager, shippingProvider, access.Result, model.Result);
if (opened.IsError) throw new InvalidOperationException(opened.Message, opened.Exception);
using var context = opened.Result;

var product = new Product { Name = "Sample", Quantity = 12 };
var added = context.Add(product);
if (added.IsError) throw new InvalidOperationException(added.Message, added.Exception);
var saved = await context.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()));
if (saved.IsError) throw new InvalidOperationException(saved.Message, saved.Exception);

var source = context.Linq();
if (source.IsError) throw new InvalidOperationException(source.Message, source.Exception);
var rows = await source.Result.Where(p => p.Quantity >= 10)
    .OrderBy(p => p.Name).Select(p => new { p.Name, p.Quantity }).ToCosmicListAsync();
```

Supply the existing HolonManager and an ICosmicQueryStorageProvider implemented by the shipping provider. Context creation and query construction perform no reads. Linq(maximumResults) bounds execution. Every sequence stage passes through native query admission; predicates are never compiled or evaluated through local Enumerable execution. Native filters, ordering, projections and asynchronous scalar/element terminals retain their existing provider capability checks.

Choose a stable contract string per application mapping. Every context read adds the active-envelope and contract predicate alongside the caller policy, excluding soft-deleted records and other mapped contracts. Build captures an immutable model; later builder changes do not change stored field paths or constructor binding. Changing paths or types needs an explicit migration decision.

## Constructor-only classes and shadow identity

```csharp
public sealed record Account(Guid Id, string Name, int Balance);
var built = new CosmicPocoModelBuilder<Account>("accounts_v1")
    .MaterializeUsing(v => new Account(v.Id, v.Get<string>("Name"), v.Get<int>("Balance")))
    .Build();
```

The factory receives decoded values and must preserve the native key. Assign read-only keys before Add. Replace(tracked, replacement) updates immutable objects through the same tracked native envelope and accepted revision; it cannot change a key or silently refresh a stale revision.

A class without a Guid Id uses a shadow native identity, available through Identity(entity) while tracked. HasKey explicitly selects another Guid property. Non-Guid business Id properties remain ordinary mapped values. Generated writable Guid keys publish only after native acknowledgement.

## Tracking and outcomes

Tracking resolves one native identity to one business instance without discarding local edits. NoTracking creates fresh objects; NoTrackingWithIdentityResolution resolves duplicates within one read. Every read still executes the current native access policy; a tracked instance does not authorize later reads.

SaveChangesAsync captures getters before native dispatch and retains native revision conflict checks. Remove queues native soft/hard deletion; removing an uncommitted Add only detaches it. A caller key change during dispatch, or a setter failure after commit, returns an explicit committed-publication error and blocks the context. Do not replay that write: reconcile in a new authorized context using original operation evidence. Do not perform concurrent operations or dispose during an active native call.

Supported fields use existing native scalar metadata domains and required, length and precision rules. Unsupported members require explicit Ignore or a separately implemented mapping feature; nothing is silently dropped. Relationship/value-object collections, inheritance, XML and provider-specific capabilities retain their own checklist scope. Project grouped/joined business records to scalar or DTO values before materializing shapes containing mapped entities.

SQL and AI engines remain backend-owned at POST /api/data/query-cql/sql and /ask. This .NET API introduces no browser engine or new HTTP endpoint.

## NHibernate XML ordinary-class increment

ImportNHibernateXml(xml, explicitClassName) imports one allowed ordinary-class mapping using the shared bounded, DTD-prohibited reader. Build must succeed before the destination mapping changes. Omitted properties stay unmapped; previously built models remain immutable. Native3/3 and WEB4 acceptance46105933887 covers scalar aliases/constraints, generated canonical Guid, assigned scalar and composite logical keys, code-supplied constructor factories and mutable=false model policy. Assigned/composite identities use FindByPrimaryKeyAsync with values in declaration order. For a factory with an assigned logical key, read the business key by its mapped property name; CosmicPocoValues.Id remains the private storage GUID.

Only assigned and guid generator policies are currently enrolled for this ordinary-class importer. Relationship, inheritance/table/schema/catalog, custom user types and remaining generator XML are still open under full M04 and return explicit unsupported errors. This native-qualified increment does not establish full NHibernate XML parity. SQL/AI engine changes still belong in backend endpoints.
