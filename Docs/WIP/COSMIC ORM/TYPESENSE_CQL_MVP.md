# Typesense bounded CQL demo scope

5 November 2026: provider build and 13 controlled tests pass, zero failed/skipped.
Eight tests cover the new CQL/HTTP boundary; five are existing metadata checks.
No native service, manager/LINQ, federation, hosted or browser acceptance yet.
Coverage totals remain unchanged.

The existing provider stores camel-case canonical Holon JSON as documents in
`oasis_holons`. The new CQL path uses the shared bounded identity compiler,
intersects server-owned read authority before requests and retrieves exact IDs
through the actual [Typesense document API](https://typesense.org/docs/29.0/api/documents.html#retrieve-a-document).
It uses GET /collections/oasis_holons/documents/{canonical-guid}; no search scan
or client-side business predicate. Responses are bounded while streaming to
1 MiB each/4 MiB total, with one aggregate 15-second deadline. Native ID and
payload authority must agree. Duplicate top-level properties reject. Deleted
rows are excluded before paging in canonical GUID order. Historical reads and
unsupported query shapes reject rather than switching to legacy load methods.

Query activation uses GET /collections/oasis_holons and verifies the existing
collection name; it never invokes the ordinary collection-creation path. Failed
activation clears readiness. The default HTTP transport disables redirects;
explicitly injected clients remain owned/configured by their caller. Credentials
use the provider's existing X-TYPESENSE-API-KEY contract. No new API/client package.

Controlled tests verify 3/2 selections, paging after deleted/missing exclusions,
read-only missing-collection activation, authentication/authorization/server-error
propagation and failure without partial rows after wrong IDs, duplicate IDs or
oversized second records. Only document HTTP 404 becomes an absent row; other
HTTP errors remain errors. This is controlled HttpMessageHandler evidence,
not native Typesense proof or a distributed snapshot guarantee.

Next: qualify one real reusable owned Typesense runtime and source, prove exact
native 3/2/absent/deleted selections and unchanged source, then manager/LINQ and
HTTP/Mongo pairing, privacy/replicas. Host enrollment must use restricted source
credentials and explicit tariff/preset admission after native qualification.
Wider content/search, native write correctness, conditional concurrency, children,
history, replication and full ORM parity remain separate acceptance gates.

## Native read-only fixture

TypesenseCqlNativeTests requires COSMIC_TYPESENSE_CQL_SOURCE (metadata JSON:
Endpoint, Actor, LiveIds[3], DeletedId, MissingId) and the source credential only
in COSMIC_TYPESENSE_CQL_KEY. It accepts one explicit HTTP loopback origin and
five distinct nonempty IDs. No source is selected implicitly. It activates the
existing collection, independently retrieves each control, proves the deleted
record exists and missing record is HTTP404, then checks CQL 3/2/0 and paging.
Four native document SHA256 observations must match before/after. All calls are
GET; no provisioning, save, cleanup mutation or sample substitution occurs.
Native observation bodies are bounded to1MiB with15-second whole-read deadlines.

Native execution remains open. No retained Typesense runtime is recorded on this
laptop. WSL reported no installed distributions; Docker is not on PATH. Official
[Windows installation instructions](https://typesense.org/docs/guide/install-typesense.html)
use WSL. No Linux environment, native binary or cloud account was installed.
Choose one reusable actual service before dispatching the explicitly filtered
TypesenseCqlNative category. Existing old integration tests are not its runner.

The leased native fixture build passed with zero errors. This is compilation
only; no native test was dispatched and no coverage count increased.
