# RavenDB bounded CQL demo scope

Started 5 November 2026. Native and hosted acceptance remain open; no coverage
count increase. Existing provider uses RavenDB.Client 7.0.1 and OasisHolons/{id}
envelopes containing camel-case canonical Holon JSON. Preserve that layout.

The new query path uses the shared bounded identity compiler and exactly one
explicit RavenDB authority. It intersects IDs with the server-owned read
catalogue before SDK requests, loads the selected documents in a fresh
nontracking/noncached async session, validates native envelope and payload
identity/deletion/read authority, strips children, then applies paging in
admitted-key order. No index scan, provisioning, writes or alternate storage
path is used. Historical queries and unsupported predicates are rejected.

The leased provider/shared build passes with zero errors (four warnings). The
first build rejected tracking/cache flags on Advanced; SDK SessionOptions owns
those settings, and the corrected build passes. No native execution is claimed.
Next verify controlled
admission and malformed/foreign envelope handling, then qualify a real retained
or explicitly owned RavenDB service with native 3/2/absent/deleted reads and
source preservation. Follow with manager/LINQ, local HTTP/Mongo pairing,
private isolation and replicas before any Development registration. Do not
claim RavenDB completeness from compilation or the existing SDK dependency.

The SDK materializes its response before per-document (1 MiB) and aggregate
payload (4 MiB) validation; these are accepted-payload bounds, not transport
stream allocation guarantees. Native rows are moving reads, not a distributed
snapshot. Wider content predicates, conditional writes/history, graphs,
replication and full ORM parity require their own evidence.

Controlled query gate: 13/13 tests pass, zero skipped, including eight new CQL
cases and five existing metadata checks. The CQL cases capture mutable key
arrays, intersect the authority catalogue, reject wrong provider binding and
unsupported name/metadata/order/graph/unbounded shapes, and verify pre-cancel,
inactive-store and invalid-plan/budget errors without a network connection.
The shared compiler remains the single owner of bounded identity algebra.
Native envelope/payload behavior, real-service availability and manager/HTTP
execution still require separate tests. No server or teaching records created.

## Native retained-source fixture

RavenDBCqlNativeTests compiles against the real SDK; execution remains open.
No retained RavenDB runtime/source was found in this laptop's COSMIC ledger or
runtime directories. Official installation entry point: https://ravendb.net/download
and https://ravendb.net/docs/article-page/7.0/csharp/start/getting-started .
No runtime download, license selection, server startup or native write occurred.

The test requires COSMIC_RAVENDB_CQL_SOURCE pointing to a metadata-only JSON
file with Endpoint (HTTP loopback origin), Database, Actor, LiveIds (three GUIDs),
DeletedId and MissingId. All five IDs must be distinct and nonempty. Select only
an explicitly owned, already prepared teaching database; there is no provisioning
or seed path in this test. It uses the official SDK to independently prove the
deleted control actually exists and the missing control is absent, then checks
CQL full/subset/empty/paging results in canonical ID order. Native envelopes
and projected rows must remain identical before and after the read sequence.

Run only after reviewing the exact source and server identity:
`dotnet test <RavenDB integration project> --no-build --no-restore --filter TestCategory=RavenDBCqlNative`
A missing configuration fails rather than skipping or selecting a database.
Latest leased fixture build passed with zero errors/two warnings. This is a
compiled native verification path, not native query acceptance. Malformed
payload/foreign identity controls, manager/LINQ and Mongo/HTTP remain open.

Runtime acquisition: official RavenDB.Embedded 7.0.1 package cached once under
LocalState/COSMIC/RecoveryLaptopRavenDB701; NuGet repository signature verifies.
The archive is 149,728,697 bytes (371,595,943 bytes expanded). No server has
been extracted or started. Package metadata requires license acceptance;
LICENSE.txt is available alongside the archive for owner review before use.
Keep that decision separate from preparing the native bootstrap and tests.

## One-shot owned bootstrap

RavenDBCqlBootstrapTests now compiles with the same integration project. Its
separate RavenDBCqlBootstrap category creates exactly one uniquely named
cosmic-cql-ravendb-<32hex> database, then stores three live and one deleted
canonical envelopes with create-only change-vector conditions. MissingId stays
absent. It flushes source hash, original identities and mutation bounds to an
exclusive intent before the first mutation. Any existing intent stops another
attempt, even after acknowledgement loss. Successful SaveChanges alone permits
an exclusive ACK. Native errors leave the original intent for reconciliation.
The read-only test never calls bootstrap. No mutation category was executed.

Two offline bootstrap controls pass (zero skipped): existing intent cannot be
overwritten, and unowned database/remote endpoint/duplicate IDs reject before
mutation. Initial compilation caught assignment to derived IsDeleted; bootstrap
now sets its owning DeletedDate field. The original failure remains recorded.
Metadata-only source.json is prepared once under RecoveryLaptopRavenDB701,
434 bytes, with one unique database/actor/five IDs and loopback port18087.
No server start, database creation, seed, license acceptance or bootstrap intent
has occurred. Pending owner license review is in ACTIONS-FOR-DAVID.md.

## Controlled SDK payload boundary

The unit suite now passes 19/19 tests, zero failures/skips. Six additional
cases execute the actual CQL reader through DispatchProxy implementations of
IDocumentStore/IAsyncDocumentSession. They permit only nontracking/noncached
OpenAsyncSession, bounded LoadAsync and disposal; any other SDK operation fails.
Five malformed second rows prove that a valid first row is never published as
partial success: envelope ID mismatch, payload ID mismatch, inconsistent deleted
state, missing JSON and over-1-MiB payload. Another case checks exact canonical
requested keys and applies paging after deleted/missing exclusions. These are
controlled boundary tests, not native RavenDB or HTTP proof. No production seam
or alternate read path was added. Runtime license review remains pending.

## Explicit read-only activation

RavenDB now implements ICosmicReadOnlyQueryActivation. Its query constructor
requires an explicit nonempty teaching-record GUID. Activation uses a fresh
nontracking/noncached SDK session to load only that existing ID, validates the
same bounded envelope decoder used by queries, and rejects missing/deleted or
mismatched data. No database statistics privilege, index, provisioning or write
operation is needed. Unconfigured or pre-cancelled activation makes no SDK read.

CQL requires both the active flag and initialized store. Failed activation
clears readiness. The store lifecycle now assigns its field only after SDK
initialization succeeds, disposing a failed candidate and preserving the error;
regular activation/deactivation publish the corresponding active state. These
fixes prevent a failed client initialization from leaving a readable-looking
store handle. Four new controls plus the prior suite pass 23/23, zero skipped:
valid/failed probe transitions, missing/pre-cancelled probe, deactivation and
failed SDK initialization. Actual native activation is not yet verified.

## Original laptop TLS and browser evidence recovered

The original task branch records native TLS, source restart and Mongo-paired
HTTP/browser acceptance at `f40b053a9`. Its archived native evidence belongs
to the original local server and retained database. The recovery checkout
carries forward the caller-owned certificate support from `8f3d528d4` and
keeps its current payload and activation controls. The copied certificate
tests are queued against the current Core pin.
[Original evidence](https://github.com/NextGenSoftwareUK/OASIS/commit/f40b053a9).

This machine's native license gate remains open. A hosted teaching source,
restricted client certificate, WEB4 admission and live visitor evidence are
separate gates.

The next hosted profile uses one certificate-authenticated HTTPS origin
and one database. It disables SDK topology discovery and topology caching,
requires a public probe, and rejects teaching rows that become private.
The certificate suite also checks that caller edits to the original URL
array cannot change the validated HTTPS destination.

All 28 provider tests passed at Core 439098484a74d8c84990fc1bbeb2b1228d5320d9.
Provider TRX SHA256 cd24ef3a24cc13060b953ccbd6f11e2c8d809458c155b885a52d892ad767c2cd.

### Development WEB4 admission candidate

The source uses RavenDB.Client 7.0.1 against one canonical RavenDB Cloud
HTTPS origin and the `oasis` database. Place the dedicated database
Read Only client certificate at `/data/cql-secrets/ravendb/reader.pfx` on
the existing WEB4 persistent volume. The service verifies the exact PFX
SHA256 and loads its key into process memory. The service owns disposal
of the client certificate after the source closes.

Protected settings use the `CQL_RAVENDB_QUERY_` prefix with `ORIGIN`,
`CERTIFICATE_SHA256`, optional `CERTIFICATE_PASSWORD` and `PROBE_ID`.
The reviewed catalogue binds the origin, PFX digest and first live UUID
before protected settings can be prepared. The PFX and password belong in
protected deployment state.
[RavenDB client connection](https://docs.ravendb.net/guides/connecting-c-application-to-ravendb-cloud) ·
[Read Only authorization](https://docs.ravendb.net/7.2/studio/server/certificates/read-only-access-level).

All 12 protected-settings tests passed. All eight WEB4 admission tests passed at Core
`fec1c1953b341c8439a08cff0a66779c92acadd3`. The approved RavenDB source,
restricted certificate, volume placement and live visitor proof remain open.
Admission TRX SHA256 1a5dc50b1a3723a5d0622cb967f53590d6390ef83568f3ad31c5e5e6fbee835b.
