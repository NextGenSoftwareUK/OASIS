# ActivityPub/Mastodon CQL native point profile

27 October 2026. One native read profile verified; full protocol demo remains open.

## What works

The ActivityPub adapter now implements the shared CQL row contract for an exact
Mastodon status key. It calls the configured instance's native status endpoint
once and reuses the canonical status projection owner. No timeline enumeration,
database substitute, hash reversal, automatic activation or retry occurs.

The native key is exposed as string metadata `MastodonStatusId`. Projected OASIS
GUIDs remain non-reversible. Results include `MastodonInstance`, original
`MastodonObjectUri` and the native provider key. Only explicitly public/unlisted
statuses can be published; mandatory read access can further exclude them.
This is Mastodon HTTP API support, not universal ActivityPub query support.

```csharp
// Host supplies its explicitly configured, activated adapter and V2 manager.
// nativeStatusId is an existing status's native key, not its OASIS GUID.
var scope = CosmicQueryScope.Named(ProviderType.ActivityPubOASIS).Result;
var access = CosmicQueryReadAccess.OwnerOrPublic(actorId).Result;

var source = CosmicQuery<Holon>.Create(scope)
    .WithReadAccess(access)
    .Where(h => CosmicQueryMetadata.Value<string>(h, "MastodonStatusId") == nativeStatusId)
    .Result;

var query = CosmicLinq.Create(manager, provider, source, maximumResults: 1).Result;
var result = await query.ToCosmicListAsync(cancellationToken);

// Check IsError before using Result in application code.
```

The checked, spaced and commented version is
`Examples/CosmicCqlDemo/MastodonPointWalkthrough.cs`. The integration test compiles
and invokes that actual source through HolonManager/LINQ; it is a callable
walkthrough, not a new switch in the SQLite console/Swagger host.

## Exact admission and failure semantics

- One selected provider and exactly one business predicate: equality on that
  single metadata key, with a nonempty printable ASCII key of at most128chars.
- Mandatory read policy is separately bound; it is not mistaken for an extra
  user predicate. Visibility is checked on the canonical point result.
- Skip/Take preserve single-point cardinality; Skip greater than0 or Take0
  returns an acknowledged empty result without fetching a status.
- Arbitrary metadata/canonical filters, OR, extra predicates, ordering, graphs,
  tail selection and indexed mode reject before native HTTP.
- HTTP404 means an empty source;401/403/5xx/redirect/malformed identity/ambiguous
  JSON are errors. No fallback or partial result is returned.
- Cancellation propagates into native HTTP/stream reads. Response body maximum
  is1MiB, including a stream without Content-Length. Duplicate source status or
  author fields reject in the canonical projection owner.

## Proving evidence

Final source builds: both Core targets, AP provider and unit/integration consumers.
All counts have zero skips. Raw reports remain under worktree TestResults.

| Gate | Result | Relative evidence path / SHA256 |
|---|---|---|
| Affected Core CQL | 1,964 pass, including15 new point controls | CoreCurrent/cql-point-publish-core.trx / F501F1A71300A35C5ACF2BA0489899EE39D2D4C956EDA7562F73536EB8169D2B |
| AP controls | 311 pass, including20 new CQL controls | ActivityPubCurrent/cql-point-publish-unit.trx / 9C089633A7BC19B5A58CF4EBE312CD6CCC8E6AA993B442BFFC782F3EF2F0252A |
| Native CQL + actual sample | 1 pass;5 allowlisted public GETs | ActivityPubCurrent/cql-point-publish-native.trx / 7E6BED9B1A7B1077DE7B638C8AC69054361445C121369C939857F98A42F81132 |

The native fixture independently discovers one existing public Mastodon account
status, fetches that exact status, and compares native key/URI/content with the
shared CQL result. It uses no token, account creation, posting, federation write,
test service or persistent fixture. No repeated green social baseline was run.
Two genuine failing controls first reproduced duplicate ID/visibility acceptance
in `cql-ambiguity-before.trx`; the canonical source-owner fix makes them pass.

To run the native gate after the bounded cached producer/consumer build, set
`OASIS_ACTIVITYPUB_PUBLIC_LIVE=1` for that process and select
`PublicMastodonCqlTests.NativeStatusPointThroughSharedLinqMatchesIndependentPublicSource`
in the existing ActivityPub integration project. Restore the previous environment
value afterward, preserve earlier reports and put new TRX only in TestResults.

## Remaining red gates

Production WEB4 endpoint/authorization/usage enrollment and the interactive demo
host are not enabled for this provider. Scalar/projection/timeline/search queries,
storage Save/deltas/versioning and complete common-demo coverage are not proved
by this point-read milestone. No cross-server snapshot or freshness guarantee is
claimed for a live social response. IPFS/SOLID retained configuration is still
unresolved; Solana recovery remains deferred at the user's request.
