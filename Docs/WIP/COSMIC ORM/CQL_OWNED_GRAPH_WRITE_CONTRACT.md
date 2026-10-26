# V2 owned graph write boundary

25 October 2026. Generic Core SDK implementation; REST/GraphQL/gRPC enrollment
and prepared accounting integration remain open. Existing ordinary Save APIs keep
their trusted-application contract. CQL recipient/tenant policy is not enrolled.

## Invariant

An authenticated creator may mutate only selected stored records they own. A
forged CreatedByAvatarId on a child payload cannot authorize that child. Ownership
comes from the exact write authority; read failover or load balancing cannot
substitute a replica. The write binds caller-observed revisions, never fresh tokens
from the authorization lookup. All selected mutations commit through existing
atomic COSMIC conditional writes, or none do.

`HolonManager.SaveOwnedHolonAsync` uses the existing detached HolonSavePlan graph
selection and publication owner. SaveChildren, recursive and maxChildDepth select
the same nodes as persistence. A caller-selected node limit, at most10,000, rejects
oversized selected graphs before storage access. Source leases are released on
admission failure, with no caller IDs or acknowledged baselines published.

For each existing selected identity, the facade reads only that root from the
required provider, validates its returned identity and persisted creator, then
builds exact conditional preconditions. Explicit CosmicWriteOptions revisions take
precedence; otherwise frozen input ProviderMetaData tokens are used. An existing
record with no caller token is rejected. New generated IDs are create-only;
authoritative absence for a supplied ID also produces a create-only condition.
An outage, warning, wrong identity or not-found with a transport exception is not
absence. No-op sources still undergo authorization before returning no-write.

## Example

```csharp
// actor is selected by the authenticated host, never a client creator field.
// The holon came from a load and carries provider revision tokens.
holon.Name = "edited name";

var result = await manager.SaveOwnedHolonAsync(
    holon,
    actor,
    new CosmicWriteOptions(operationId),
    saveChildren: true,
    recursive: true,
    maxChildDepth: 4,
    providerType: ProviderType.MongoDBOASIS,
    maximumHolons: 1000);
```

A stale revision returns COSMIC_CONCURRENCY_CONFLICT. Foreign ownership returns
COSMIC_WRITE_FORBIDDEN. Missing existing-record revisions return
COSMIC_EXPECTED_REVISION_REQUIRED. Lookup failure returns
COSMIC_WRITE_OWNERSHIP_LOOKUP_FAILED. The facade rejects legacy HyperDrive mode;
no legacy fallback runs. It grants no Wizard or tenant-role bypass.

## Accounting and transport integration still required

Authorization lookups are real physical reads, with the actor bound on each
storage request. The SDK facade does not invent a prepared admission or price.
A public host must admit/budget those reads and the conditional write under its
existing physical-attempt/receipt owner before enrolling this path. It must also
bind actor/operation/provider identity and serialize failures without exposing
inaccessible data. Ordinary REST root-only ownership checks and GraphQL/gRPC save
paths still need migration; do not treat this SDK addition as their completion.
The sharing helper now pins its initial V2 read and subsequent conditional
save to one authority through the generic Core observation API.

## Verification

Dual Core targets and full3,228Core tests pass, zero skips. Eleven new controlled
cases include forged foreign children, no-op authorization, stale input versus
fresh lookup, explicit revisions, bounds/lease release, no-failover ownership,
authoritative absence, transport uncertainty and legacy rejection.

The first native run stopped on a test-only Boolean conversion of a SQLite
JsonElement. Its exact manifest, zero HTTP attempts, five Mongo/five SQLite
committed receipts and nine owned records/store were reviewed before cleanup.
The corrected final gate passes all three stores: forged foreign-child writes
are denied without changing stored records, valid owned writes succeed and stale
caller tokens conflict. The joint138SDK/138signedHTTP walkthrough also passes,
with345settled CQL operations/337CQL attempts. Those accounting numbers describe
the admitted CQL walkthrough, not the separate direct-SDK authorization checks.
Source snapshots and six binary identities match; owned services/fixtures are
cleaned. Core1740b696e0963698938f11e518cce597c299124d is published. See
TestResults/CosmicCqlDemo/owned-save-evidence.json for paths and hashes.
