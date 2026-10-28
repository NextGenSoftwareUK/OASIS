# Protocol multi-record CQL checkpoint

28 October 2026. Scope: ActivityPubOASIS against Mastodon 4.3+ native batch API.

## C# example

Explicitly enroll an instance that supports the native batch endpoint, then activate it:

```csharp
using var provider = new ActivityPubOASIS(instanceUrl, "")
{
    EnableCqlStatusKeySets = true
};

var activation = await provider.ActivateProviderAsync();
// Check activation.IsError before proceeding.

// Existing native status IDs, not OASIS projection GUIDs.
var statusIds = new[] { firstStatusId, secondStatusId, thirdStatusId };

var query = CosmicQuery<Holon>
    .Create(CosmicQueryScope.Named(ProviderType.ActivityPubOASIS).Result)
    .Where(row => statusIds.Contains(
        CosmicQueryMetadata.Value<string>(row, ActivityPubOASIS.CqlStatusKey)))
    .Result;

var plan = CosmicQueryPlanner.Plan(
    query,
    ProviderType.ActivityPubOASIS,
    new[] { new CosmicQueryTarget(provider.QueryProviderType, provider.QueryCapabilities) });

// Check plan.IsError; unsupported shapes are rejected before native execution.
var result = await provider.ExecuteCosmicQueryAsync(plan.Result);
// Check result.IsError before using result.Result.
```

`Contains` becomes native indexed ID membership in one GET, not timeline enumeration
or point-read fan-out. The profile admits 1â€“40 unique decimal IDs, optional Skip/Take,
and read-access enforcement. Public/unlisted statuses only. Native missing IDs may be
absent from a successful array; HTTP failures never become empty successful results.
The full bounded response validates before access/window publication. Duplicate,
unrequested, malformed and non-public records fail the whole query. Maximum body:
1 MiB. Results retain canonical native IDs, URIs, content and provider provenance.
Captured key order defines this profile's deterministic page order; arbitrary OrderBy,
graphs, content predicates and projections are not admitted.

Default enrollment remains the existing point-only profile. This is an explicit
backend capability choice, never an error-driven endpoint fallback.

[Mastodon API contract](https://docs.joinmastodon.org/methods/statuses/#index):
GET /api/v1/statuses, id[] membership, added in 4.3; public access for public records.
This does not establish equivalent query support on every ActivityPub implementation.

## Verification and remaining demo work

- Core native key-set tests:8/8; CQL regression suite:2190/2190.
- ActivityPub controlled suite:321/321, including10 new batch cases.
- Live acceptance:1/1, independently discovers3 existing public statuses on
  mastodon.social, compares native URI/content, and asserts exactly one batch request.
- Proving tests: `CosmicNativeKeySetTests.cs`, `ActivityPubCqlKeySetTests.cs`,
  `PublicMastodonKeySetTests.cs` under the existing Core/provider test projects.
- Raw logs/TRX: `C:/Source/OASIS-cosmic/TestResults/DemoBreadthCurrent/`.
  Hashes and resource lifecycle are recorded in ENVIRONMENT_AND_CLEANUP_LEDGER.md.
- No account/token/posting, native fixture writes, new runtime or dependency restore.
- Still open: interactive teaching page/HTTP multi-record demonstration, actual
  Mongo-paired federated preset, broader content/tag filters, IPFS and SOLID collections,
  hosted free Azure and Solana browser verification. These are not marked green here.

## IPFS encrypted CID-set implementation

Set `EnableCqlCidKeySets = true` on the configured IPFS provider before planning.
Use the same Contains expression above with `IPFSOASIS.CqlRecordCidKey` and 1–8
valid CIDs. One authenticated immutable private catalogue is read; only selected
holon entries are decrypted. Missing catalogue entries are absent, access/deletion
filters precede paging, and every selected envelope validates before any page is
published. No general content predicate, Pod query or plaintext index is implied.
The native read bound is one catalogue plus at most8 record envelopes, each at most
4 MiB. Result-budget excess fails the whole page. Default remains point enrollment.

Controlled verification:17 point/membership cases and195 selected controlled provider
tests pass, including selection from3 encrypted records, access-before-Skip, corrupt
selected envelope even with Take(0), and budget failure. Actual Kubo collection
acceptance and Mongo federation remain unverified; do not mark those gates green.
An initial overbroad regression filter also selected8 historical native inspection/
cleanup tests; they failed on missing environment prerequisites before native work.
The corrected controlled selection passes; that first run is retained as evidence.
