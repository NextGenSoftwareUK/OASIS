# Sharing mutation correctness before CQL policy enrollment

25 October 2026. This work fixes the existing WEB4 sharing mutation boundary.
It does **not** add shared-recipient or tenant predicates to CQL. CQL still admits
its declared owner/public rule; native recipient policy enrollment remains open.

## One conditional mutation path

`HolonAccess.ShareAsync` and `RevokeAsync` share one load/check/change/save owner.
The host supplies an authenticated `IAvatar`; these methods do not authenticate
an arbitrary avatar object from a client. The existing creator or trusted Wizard
check applies equally to grants and revocation.

The helper loads only the requested root, rejects warning/error/mismatched lookup
results, changes its recipient set and requests COSMIC conditional persistence.
Conditions use the provider revision accepted by that load, not logical VersionId.
Only the root is saved. Unsupported conditional providers return an explicit error;
no ordinary-write retry or provider substitution is allowed. Conflict and unknown
outcome error codes survive the boundary, with their original exceptions.

`RecordShares` writes sorted unique recipient IDs and rebuilds derived index flags,
removing flags for revoked recipients. An index flag is a lookup hint and cannot
grant read access on its own. The single-recipient REST action returns real errors;
its test-data setting no longer converts a failed grant into apparent success.

## Explicit intent example

```csharp
// Obtain actor from the host's authenticated identity, never from a request DTO.
// Retain operationId if this logical write has an uncertain outcome.
var options = new CosmicWriteOptions(operationId);

var granted = await HolonAccess.ShareAsync(
    manager,
    holonId,
    recipientIds,
    authenticatedAvatar,
    options);

// A revocation is a separate logical intent with its own stable operation ID.
var revoked = await HolonAccess.RevokeAsync(
    manager,
    holonId,
    recipientIds,
    authenticatedAvatar,
    new CosmicWriteOptions(revocationOperationId));
```

The original four-argument ShareAsync signature remains available and uses the
same conditional mutation owner. Callers requiring explicit intent/revision
control use the overload above, optionally with ExpectedRevisions. A stale grant
must fail after a newer revocation; the helper never reloads and silently retries.
REST revocation and a public stable-intent transport contract are not added here.

## Evidence and remaining gates

Six original controlled tests fail before the fix and pass afterward. The expanded
full Core suite passes3,166tests, zero skips, including12new cases. WEB4 compilation
passes. Real MongoDB/SQLite/Neo4j grant/revoke/stale-grant acceptance passes on all three
stores in the joint gate, alongside138SDK/138signedHTTP CQL queries (345settled
operations/337attempts). Final Core3,166and shippingWEB4 compilation pass again
after preserving the four-argument helper signature. Six producer copies and
source snapshots match; owned services/fixtures are cleaned up. Reports:
`TestResults/CosmicCqlDemo/sharing-native.log`, `sharing-write-final.log`,
`sharing-evidence.json`, and `TestResults/CoreCurrent/sharing-write-final.trx`.

Remaining policy work includes one generic Core canonical grant owner, explicit
legacy-list migration, malformed/oversized grant rejection, atomic issuer authority
across remote mutation paths, authoritative tenant membership and consistent native
root/traversal/join/aggregate/output checks. Existing permissive recipient parsing
is not adequate evidence for enrolling a stronger CQL access profile.
