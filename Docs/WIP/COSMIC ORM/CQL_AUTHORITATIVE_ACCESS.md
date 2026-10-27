# CQL authoritative record access

26 October 2026. Generic Core contract for a bounded, trusted authorization catalogue. Hosted recipient/tenant discovery and enrollment remain separate roadmap work.

`CosmicQueryReadAccess.OwnerOrPublic(actor)` retains its existing behavior. `AuthoritativeRecords(actor, authority, recordIds)` instead restricts every source row to the complete supplied identity set. Public records and the actor's own records outside that set remain inaccessible. This allows a trusted application to express a bounded tenant partition or authorized recipient catalogue without trusting metadata indexes, client tenant IDs or role labels.

```csharp
// Obtain these identities from the application's trusted authorization service.
// Do not substitute request IDs or metadata search hits for authorization.
var access = CosmicQueryReadAccess.AuthoritativeRecords(
    authenticatedActor,
    ProviderType.MongoDBOASIS,
    authorizedRecordIds);

if (access.IsError)
    return access;

// Bind policy before ordering/paging. Existing provider planning and manager
// execution apply the mandatory membership predicate in native storage.
var query = CosmicQuery<Holon>
    .Create(CosmicQueryScope.Named(ProviderType.MongoDBOASIS).Result)
    .WithReadAccess(access.Result)
    .Where(holon => holon.IsActive)
    .OrderBy(holon => holon.Name)
    .Take(20);
```

The factory snapshots at most 128 unique nonempty IDs, matching the existing membership-expression bound. Empty sets are valid and deny all rows. Duplicate/empty IDs, excessive or throwing enumeration, missing actor and nonconcrete provider selectors fail as a whole. No partial list is accepted or truncated. The catalogue is private and immutable; subsequent caller collection changes have no effect.

The catalogue belongs to one concrete storage authority. Planning rejects applying it to another provider, including another replica holding the same logical IDs, or implicitly federating it. A query cannot replace its policy with another policy for the same actor. Equivalent catalogues compare as sets; owner/public policy is distinct. Both join sources must have identical policies. Business filters can narrow the result normally.

Native predicates precede source ordering, paging, grouping and joins. Graph expansion carries the same policy to every selected child. Core and provider detached-source decoders independently check the same identity rule, including join and selected-result composition proofs. A provider returning an unapproved public row fails publication; it cannot be silently filtered after paging.

This contract does not discover recipients, read a client tenant claim, create storage credentials or authorize its own input. Trusted SDK code must obtain the complete actor/authority-bound catalogue from an application authorization authority. WEB4 now supports [hosted policy enrollment](CQL_HOSTED_READ_POLICY.md), verified with signed native HTTP on all three providers. Unenrolled hosts retain owner/public policy. Larger catalogues need an explicit scalable authorization contract; batching independent pages is not equivalent.

Verification: both Core targets build and 3,514 Core tests pass, including 13 new controls for immutable capture, whole-set rejection, scope binding, policy replacement, join mismatch and detached publication. The native cohort passes all three providers with authorized private rows before paging, selected-result composition, self-joins and child expansion restricted by the same catalogue. The full signed HTTP and actual process-recovery regression also passes: 465 settlements / 517 attempts, unchanged sources and seven matching binary pairs. Owned services stopped and exact fixtures were removed.

[Core TRX](Verification/authoritative-access-core.trx.gz), [native TRX](Verification/authoritative-access-native.trx.gz), [result](Verification/authoritative-access-result.json), [source identities](Verification/authoritative-access-source.json.gz), [binary identities](Verification/authoritative-access-binaries.json).

The first native run stopped at a fixture-only captured-array-index expression before any conditional write or usage initialization. Before cleanup, exact-hash reconciliation verified all 27 original seeded records, owners, public flags and relationships, no conditional receipts and no usage database. The corrected fixture captures its graph ID as a scalar; no production expression fallback was added. [Reconciliation TRX](Verification/authoritative-access-reconciliation.trx.gz), [original seed intent](Verification/authoritative-access-seed-original.json.gz), [observations](Verification/authoritative-access-seed-reconciliation.json.gz).
