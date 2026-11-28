# Composite primary keys in COSMIC ordinary-class models

Status: implemented on Corea30df79c; native three-store acceptance is pending. Full verification is tracked in Docs/WIP/COSMIC ORM/GPT_M09_LEDGER_2026-11-28.md.

A composite primary key is the business identity. OASIS keeps its global GUID as an internal storage identity, so existing audit, revision, replication and provider contracts retain their native GUID binding. The complete typed tuple is claimed in the existing native key registry inside the same transaction as the row. No tuple is reduced to a hash or concatenated with an ambiguous delimiter.

## Declare and find a key

```csharp
public sealed class Inventory
{
    public string Warehouse { get; set; } = "";
    public int ItemNumber { get; set; }
    public int Quantity { get; set; }
}

var configured = new CosmicPocoModelBuilder<Inventory>("inventory_v1")
    .HasCompositeKey(p => p.Warehouse, p => p.ItemNumber).Build();
// Open CosmicPocoContext<Inventory> with this immutable model and the normal actor/provider.
var result = await context.FindByPrimaryKeyAsync(new object[] { "north", 42 });
```

Check OASISResult errors after Build, context creation and every native operation, as shown in COSMIC_ORDINARY_CLASS_MAPPING.md. Supply key components in declaration order and their exact CLR types. Identity(entity) exposes the internal GUID while an object is tracked; callers do not need a GUID property on the business class. HasCompositeKey replaces the conventional Guid Id key strategy; any remaining business Id property is an ordinary mapped value.

## Identity semantics

Components are required and immutable from registration. Updating other fields preserves the same logical key and accepted native revision. A second object with the same logical key is refused in one context. Across contexts the native transaction enforces uniqueness; a conflict rolls back the entire batch. A successful deletion releases the claim, allowing a new record to use that logical identity.

Key lookup resolves the native claim and then executes the normal actor-bound, active/contract-filtered native read. A key does not authorize access to its holder. Uniqueness is scoped to the application contract and physical store. Include the tenant identifier as a component, or choose a distinct tenant contract, when business identities belong to separate tenant partitions.

Typed JSON framing distinguishes component boundaries and CLR domains. Decimal scale does not change numeric identity; finite floating zeros have one identity. DateTime components require explicit UTC values, and DateTimeOffset components use their UTC instant. The native contract admits 2–16 components and at most 1024 characters in the canonical encoded key. Unsupported domains and missing/null components are refused before dispatch.

Changing component order, CLR type, storage mapping or contract is a model migration, not an automatic key rewrite. Existing rows acquire no claim merely by declaring a new model. No automatic backfill or user-data migration occurs.

MongoDB, SQLite and Neo4j execute the real shipping atomic key-claim and authorized read paths. Other providers must enroll the native atomic claim/lookup capabilities before creating a context for this model; unsupported providers return an explicit capability error.

This is a .NET model API. Backend SQL and AI engines remain the source of truth for browser queries; no browser engine is added.
