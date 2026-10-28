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

## All three protocol collection launchers

Hub `-MultiRecord` now supports IPFS, SOLID and ActivityPub. IPFS/SOLID native
acceptance also runs the actual shared teaching HTTP host before cleanup:3 selected
rows and2-row subset, canonical IDs and foreign key/actor rejection all pass.
`-VerifyOnly` is bounded; interactive mode waits for explicit shutdown before cleanup.
The completed-fixture archiver selects keyset journal/DNA/TRX/lifecycle exactly,
checks phase/pending/listeners, and moves with hash manifests; identities untouched.

IPFS native/HTTP1/1 and SOLID native/HTTP1/1 pass; completed journals retained,
pending absent. Kubo stopped; SOLID exits143 without OOM. Routing:8profiles,
16default lifecycle plans plus6protocol collection plans and unsupported-mode rejection.
IPFS/SOLID browser clicks and Mongo-paired presets remain unverified.

## Mongo/protocol federation SDK foundation

`NativeKeySetWalkthrough.RunFederatedAsync` explicitly selects Mongo and one enrolled
IPFS/SOLID/Mastodon adapter, captures the same native-key IN predicate for both, binds
read access and executes through HolonManager's existing federation planner/executor.
No new federation engine or managed filtering scan. Complete-source bounds80total/
40per provider/2attempts/30seconds; PreserveCopies retains native provenance.

321 ActivityPub unit tests pass, including actual native batch adapter plus controlled
mock-Mongo dispatch exactly once,3physical rows and both provider identities. This is
not live Mongo proof. Exact next step: create a journaled synthetic Mongo fixture in
the retained27029 environment carrying the matching native key, verify both real
adapters, clean only that exact fixture, then expose launcher presets. Repeat for
other providers; no arbitrary cross-provider parity or repeatable-snapshot claim.

## Live Mongo + Mastodon SDK federation

`Run-ReusableProtocolMongoTests.ps1` reuses Mongo8.3.11/27029 and the existing public
Mastodon collection. Fresh acceptance1/1 and retained-fixture reconciliation1/1 pass:
5 physical rows, Mongo2/Mastodon3; an unrelated third Mongo document is excluded by
the native membership predicate. Exact source IDs/names/owners are independently read.
No local substitute supplies the Mastodon rows and no mocked Mongo adapter is used.

The initial run reached the correct federation result but failed a fixture cleanup
assertion: activation creates Avatar/AvatarDetail/CosmicHolonOutbox alongside Holon.
The journal was preserved. Explicit reconciliation verified all3 bootstrap collections
empty and Holon count3, re-ran federation without replaying saves, then removed only
the exact previously absent owned DB. A fresh lifecycle passes with this contract.
No query engine fallback or weakened admission was introduced.

No pending fixture remains; Mongo27029 and teaching5088 listeners absent. Evidence:
TestResults/DemoBreadthCurrent/native-mongo-mastodon{,-reconcile}.trx and summary JSON.
Package graph updated using existing local NuGet cache only, no remote package source.
Other provider pairings, federated browser controls and hosted Azure remain open.

## Live Mongo + IPFS/SOLID SDK pairings

The same journaled NativeProtocolMongoAcceptance implementation now verifies all
three protocol pairings; no duplicated fixture cleanup or query engine. Native IPFS1/1
and SOLID1/1 tests pass: Mongo2 + protocol3 rows, unmatched Mongo document excluded,
exact source names/IDs/owners/provenance retained. Owner-bound read policy permits
the private protocol fixture; Mongo supplies no substitute protocol rows.

Per-provider fixed Mongo databases and pending journals are used, then removed only
after independent owner/collection checks. Both native protocol fixtures also pass
HTTP all-three/subset/admission checks before exact cleanup. Pending journals absent;
all owned native listeners stopped, SOLID final exited143/no OOM. Existing ActivityPub
consumer compiles against the extracted helper. No new runtime/dependency download;
IPFS/SOLID graph restore uses local NuGet cache only. Federated browser controls,
other provider pairings and richer non-key queries remain open.
