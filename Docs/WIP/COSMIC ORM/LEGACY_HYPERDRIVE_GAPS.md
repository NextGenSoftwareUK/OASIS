# Legacy Hyperdrive gaps — documentation only

Updated 19 October 2026. The user explicitly restricted Hyperdrive implementation to V2 and requested documentation only for legacy issues. This inventory is not an instruction to repair legacy code. Shared COSMIC ORM/provider fixes already completed remain recorded in implementation status.

## Observed legacy deletion issues

Source: `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/HolonManager/HolonManager-Delete.cs`, legacy branches in the Guid/provider-key, generic/non-generic and sync/async overloads.

- Hard-delete replication loops stop after the first successful replica, leaving additional configured targets unvisited.
- Replica results overwrite the primary acknowledgement; a later failure or success can replace evidence of the authoritative operation.
- Routing calls `SetAndActivateCurrentStorageProvider` for primary and replica attempts, mutating shared provider selection during an operation.
- The asynchronous Guid overload invokes synchronous provider deletion and activation in its legacy path.
- Legacy soft-delete preparation uses load/change/save and some branches call actor-less saves despite receiving an explicit actor. Audit and concurrency ownership are therefore unclear.
- Repeated implementations across overloads differ in error/publication behavior. Inner diagnostics can be promoted to an overall error after a committed primary operation without structured target outcomes.

These are source-inspected findings, not a comprehensive native certification of every legacy overload. Native deletion side effects, history retention and all-target convergence remain unproven for legacy.

## Other retained gaps

Legacy unqualified save routing changes shared provider selection; replication uncertainty and primary acknowledgement preservation need a separate audit before any future legacy reliability claim. Existing legacy paths do not establish a universal atomic outbox/durable eventual-delivery guarantee. Historical tests of selected legacy/shared paths must not be represented as full legacy certification.

## V2 implementation boundary

Further work targets the canonical V2 router, provider contracts, conditional writes, graph semantics, history, durable delivery, migration, placement, deltas and CQL. V2 deletion remains an implementation/certification requirement. No new legacy-only repairs or legacy acceptance gates will be added.

Typed metadata hydration audit: legacy MapMetaData can match metadata keys to canonical record/runtime properties (identity, audit, visibility and provider state) and silently ignore conversion failures. Legacy code is unchanged per scope. V2 canonical ownership repair and separate strict conversion work are tracked with CQL manager integration; no legacy correction is claimed.
