# Mapped join materialization: next breadth batch

Source reviewed: Core 048b02d54870e793de8f18439ff8f00950ad3547.

## Invariant

Native providers transport complete canonical snapshots with identity, visibility and revision evidence. Application CLR models must be constructed only after validation and through shared typed hydration. A canonical Holon is not expected to already be an application subclass.

## Concrete remaining gap

Queries/CosmicJoinResults.cs validates selected snapshots in Detach by projecting to canonical Holon, then checks domain.IsInstanceOfType(copy). This rejects custom subclasses, including metadata-mapped models. Construct subsequently directly casts canonical snapshots to application types.

The new grouped entity path separates canonical validation from HolonManager.MaterializeCqlRows, shared with flat reads. Join execution still calls validation/construction independently through CosmicJoinExecutor. Removing the type check alone would leave invalid casts and omit strict metadata hydration/decryption.

Queries/CosmicLinq.GroupJoin.cs also excludes metadata-backed direct scalar carrier properties even when shared native path admission accepts them.

## Cohesive implementation scope

- Preserve canonical snapshot validation, complete native pair proof, per-source read access, identity consistency and payload bounds.
- Bring selected join entities through shared manager-owned typed hydration after complete validation and before application result construction.
- Keep left/right scopes distinct, preserve unmatched right absence, and materialize repeated identities consistently within each admitted source.
- Admit mapped scalar carrier fields through existing scalar path admission; do not evaluate client predicates or join keys.
- Cover ordinary typed entity join output and grouped typed output together with representative native journeys on MongoDB, SQLite and Neo4j.
- Preserve accepted canonical join cohorts; rerun only changed shared-owner regressions and the new mapped journey.

This is source review and implementation direction, not implementation or native acceptance. Full annotation/fluent-model parity remains open. No database, customer record, deployed configuration or runtime behavior changed during this review.
