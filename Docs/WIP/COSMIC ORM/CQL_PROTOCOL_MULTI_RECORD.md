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

## SOLID encrypted resource-set implementation

Set `EnableCqlRecordKeySets = true` before planning on the configured SOLID provider.
Use Contains with `SOLIDOASIS.CqlRecordIdKey` and1–8 canonical lowercase nonempty
GUIDs. LDP has individual resources: this explicitly enrolled strategy makes one GET
per selected resource, never scans a container and never substitutes local results.
It is not a native server batch or SPARQL/content-filter claim. Each encrypted RDF
body is bounded to4 MiB, media/identity authenticated by the existing record reader.

All keys validate before I/O; source failures stop execution without partial results.
404 means an absent resource, while403/redirect/server errors fail. Access/deletion
filters precede Skip/Take; selected envelopes validate even before Take(0). The full
page must fit the requested result budget. Native resource URI/ETag and canonical
identity remain attached. Default enrollment retains the point-only contract.

393 SOLID unit tests pass, including9 new multi-record cases: subset selection,
paging, access exclusion, corrupt later envelope, budget, absence, denied source,
invalid key and cancellation. Live Pod resource-set acceptance is pending. Reuse
LocalState/COSMIC/SolidCurrent identities and the existing journaled native runner;
do not recreate identity or replay completed/unknown fixture journals.

## Native IPFS collection proof

`Run-ReusableIpfsTests.ps1 -CqlKeySet` under one provider-build lease now passes
one native acceptance case against retained offline Kubo0.43.1. Three encrypted
holons are saved under a before-write ownership journal, independently loaded, then
queried using all three CIDs and a two-CID subset. Canonical IDs and metadata match.
Exact catalogue membership and owners are verified before clearing only that owned
catalogue. Native reads confirm absence afterwards. Completed journal and immutable
blocks remain preserved; pending journal absent and daemon ports18547–18549 closed.

TRX: `C:/Source/OASIS-cosmic/TestResults/IPFSCurrent/ipfs-cql-keyset-native.trx`,
1 passed/0 failed/0 skipped. SHA256
`BB18B7E9B10898EBA5688AABBE2322E7BF16ED0BAEEBE611D74B65973463B470`.
Do not rerun over the completed cql-keyset journal/DNA; review and archive those exact
completed fixture records first. This proves native membership, not general content
filtering or a Mongo-federated/HTTP/browser demonstration.

## Native SOLID collection proof

`Run-ReusableSolidReadiness.ps1 -CqlKeySet -RequireCleanShutdown`, under one build
lease, passes one live acceptance case using the retained private CSS Pod and DPoP
identity. Three private encrypted records are journaled before saves, read through
native CQL as an all-three set and two-record subset, and independently compared
against canonical loads. An unrelated actor receives no rows. URI, native ETag and
identity provenance survive. Exact owners are verified before conditional deletion;
all three resources then return404. Pending journal absent; completed journal retained.

Factory callers explicitly opt in through `enableCqlRecordKeySets:true`; defaults
retain point enrollment. No authentication, identity, Pod or data replacement occurs.
Lifecycle testExit0/stopExit0, final `exited 143 false` (normal SIGTERM, no OOM).
TRX `C:/Source/OASIS-cosmic/TestResults/SolidCurrent/solid-cql-keyset-native.trx`:
1 passed/0 failed/0 skipped, SHA256
`8A76A73C9B30DE5B1D3EB3B1F159C4972FA05F3F9C73992B46B6352853F20F3B`.
Teaching pages, HTTP/federated presets and richer non-key filters remain open. Native
membership proof alone is not a claim of general LINQ/EF parity on protocol stores.

## Shared executable C# developer sample

`Examples/CosmicCqlDemo/NativeKeySetWalkthrough.cs` contains spaced/commented steps
for explicit scope, frozen native keys, caller access, deferred manager/LINQ execution
and safe provenance output. It is compiled into the ActivityPub controlled suite;
321 tests pass, including execution of the exact sample against the batch transport.
That validates the manager/LINQ path, not new browser or federation acceptance.
See the example README for invocation. Native source keys must be supplied from the
configured retained provider; the sample does not provision identities or scan data.

## Teaching HTTP host

`ProtocolPointApiHost.BuildKeySet` now serves the selectable multi-record example.
The server freezes allowed key/identity pairs and binds authoritative access. Browser
input only narrows that set; duplicate/foreign keys and unmapped actor fields fail400.
One live Mastodon acceptance test passes independent direct-source comparison plus
three-record HTTP query, invalid-key and actor-forgery rejection. No point fan-out.
The captured metadata key is a local string, so expression admission never invokes a
capability object's getter. Visual rendering and protocol launcher wiring are pending;
this is not yet a ready Mongo federation preset or production authentication claim.

## ActivityPub launcher and actual browser interaction

Hub `-Provider ActivityPub -MultiRecord` now builds/starts the retained read-only
collection host. Browser AX verification: three checked IDs yielded count3 and
matching canonical IDs; deselecting the third and rerunning yielded count2 with only
the first two native IDs. Screenshot API returned Unable to capture screenshot;
visual layout proof remains unverified. Explicit shutdown completed, native acceptance
1/1 passed, loopback5088 listener absent. Routing tests pass default8 profiles/16plans,
plus2 multi-record plans and rejection of an unenrolled SQLite MultiRecord mode.
No Mongo pairing, arbitrary filters, or IPFS/SOLID launcher completion is claimed.
