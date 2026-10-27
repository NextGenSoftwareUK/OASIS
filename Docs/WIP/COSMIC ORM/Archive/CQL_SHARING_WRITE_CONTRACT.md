> Superseded by [current status](../CURRENT_STATUS.md); retained historical and technical reference.

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

## Reusing the generic representation contract

```csharp
// Uses the generic Core component; no provider-specific parser is needed.
var grants = HolonReadGrants.Read(loadedHolon);

if (grants.IsError)
{
    // Deny recipient-based access. Do not use a partial list or rewrite the data.
    return grants;
}

// For an explicit migration of known legacy text, the trusted host can call
// Read(loadedHolon, allowLegacyText: true), then Encode(grants.Result).
// Persist that canonical value only after owner/issuer checks and with CAS.
```

Missing grant metadata means no recipients. Present null, malformed or unsupported
metadata is an error. The creator/public rule remains independent of recipient
parsing. Valid parser output alone never authenticates a caller or authorizes a
write; raw storage credentials and client tenant/role labels are not grant proofs.

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

The generic Core representation owner is now `HolonReadGrants`: bounded whole-set
validation and canonical encoding, reused by WEB4. Default reads admit array/typed
GUID representations; legacy single-GUID/comma text needs `allowLegacyText: true`.
Malformed JSON never enters the legacy grammar. Incoming GUIDs must be nonempty
D-format text, and all entries must validate. Lists are bounded to1,024 inputs
and text to40,960characters; canonical encoding sorts/deduplicates IDs. Returned
recipient snapshots are immutable. Arbitrary object stringification is rejected.

WEB4 explicitly admits valid legacy text; its next authorized conditional sharing
mutation writes canonical JSON. Malformed existing metadata fails grant/revocation
without saving. Repair requires a separate explicit owner-controlled conditional
mutation. The native fixture verifies that sequence on all three stores.

Remaining policy work includes a maintained migration/operator surface, atomic issuer authority
across remote mutation paths, authoritative tenant membership and consistent native
root/traversal/join/aggregate/output checks. Correct parsing alone is not issuer authorization and does not enroll a stronger
CQL access profile.

Latest generic parser acceptance:10originalREDcases nowPASS; finalfullCore3,217
PASS0skip, bothCoreTFMs/WEB4compilePASS. All3native malformedgrant/repair checks
and138SDK/138HTTP345settled337attemptsPASS. Core906f64d3source is published;
parentdependency pin/gitlink move together. Exactreports/hashes and final additional
Corecopies: TestResults/CosmicCqlDemo/grant-parser-evidence.json.

## Same-authority observation and caller snapshot — 26 October 2026 UTC

V2 sharing now loads through generic Core `LoadHolonForWriteAsync`. Its tracked
`HolonAuthorityRead` records the selected Authority, ActorId and mutable Record.
It is an optimistic observation, not a lock, permission grant or credential.
The helper passes that exact authority into its conditional save. A changed
default provider cannot redirect the mutation, and an unavailable write authority
cannot borrow a replica's recipient list. Core owned-graph authorization reads
reuse the same private pinned-route owner.

The helper captures the authenticated caller ID and trusted Wizard status before
its first await, then uses those values for checks and the write actor. Mutating
the caller object while storage is awaited cannot substitute another identity or
elevate that request. Existing creator/trusted-Wizard policy is unchanged; this
work does not establish a tenant authority or enroll CQL shared-recipient access.

```csharp
var observed = await manager.LoadHolonForWriteAsync(holonId, actor, provider);

// Trusted host: authenticate actor and authorize this record before editing it.
// Check observed.IsError before reading observed.Result.
var record = observed.Result.Record;
record.Name = "edited";

var saved = await manager.SaveHolonAsync(
    record,
    actor,
    new CosmicWriteOptions(operationId),
    saveChildren: false,
    providerType: observed.Result.Authority);
```

Four original sharing regressions fail before the fix and pass afterward. Three
child-scope controls and two authority-holder controls also pass; fullCore3,264,
both Core target builds and WEB4 compilation pass. The final native checkpoint passes sharing3/owned-write3 plus138SDK/138signedHTTP
examples on MongoDB/SQLite/Neo4j,345settled CQL operations/337CQL attempts. Source
snapshots and six binary identities match; owned services/fixtures are cleaned.
Exact reports/hashes: TestResults/CosmicCqlDemo/sharing-authority-evidence.json.
Legacy HyperDrive routing is unchanged; the new Core observation API rejects it.
