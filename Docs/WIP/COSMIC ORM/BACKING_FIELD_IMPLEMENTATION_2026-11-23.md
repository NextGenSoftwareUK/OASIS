# Explicit backing-field mapping candidate

23 November 2026. Unqualified source; native/WEB4, image/package and live gates remain open.

The invariant is one explicit storage slot across save, hydration, canonical snapshots, rollback/reload and acknowledgement publication. A computed public getter must not change persisted field values or constraints.

CosmicFieldAttribute names one exact mutable instance field declared by its custom Holon property's owner. A cached HolonPropertyAccess implements the JSON value-provider and shared get/set contract. Canonical names, navigation, static/literal/readonly fields, mismatched field types, duplicate field targets and snapshot exclusions are rejected. This is an explicit application-code annotation, not a process-wide model override.

The existing metadata owner reads that slot and completes exact decoding before assigning it. Persistent publication/rollback/reload use the same accessor. Fluent aliases and constraints admit a field-backed writable slot; annotated scalar paths retain native query translation. Ordinary properties keep their existing reflection behavior.

The native fixture exercises a getter-only property whose getter adds DISPLAY: while the storage field contains Alpha. Save/hydration/snapshot/publication/constraints must bypass the getter, while a native projection reads the stored Alpha. It also exercises constructor-default replacement with null, immutable fluent aliases, field updates/rollback and foreign caller exclusion. Each real MongoDB/SQLite/Neo4j journey expects8 feature groups,2 owned cleanup identities,4 acknowledged Holon writes and10 timed operations.

Readonly fields, constructor-bound objects, general fluent HasField/access-mode declarations, component/graph field policies and full M17 certification remain open. No deployment acceptance is inferred.
