# ArangoDB bounded CQL MVP

The provider now has a bounded identity query path over the native AQL
cursor API. It uses the existing `oasis/holons` documents and binds
canonical GUID keys in a `DOCUMENT` lookup. It strips embedded children
before returning the records. Missing documents are excluded by the
native lookup, and paging follows identity and visibility checks. A
native error, incomplete cursor, foreign key or inconsistent payload
rejects the whole result. Budgets are 100 keys, 15 seconds, 8 MiB wire
and 4 MiB decoded payload. General graph and historical queries are open.

The public profile takes a caller-owned HTTP client and an existing
probe. It requires the probe and selected live records to remain public.
The test fixture covers the exact AQL request, paging, deleted and missing
records, private records and malformed cursor outcomes. The provider
build is pending the shared lease.

The opt-in hosted fixture is prepared for the pending WEB4 registration.
It will use `COSMIC_ARANGODB_HOSTED_CQL_SOURCE` and protected origin,
restricted reader and probe settings. Run native qualification with
an approved ArangoDB source. ArangoDB 3.12 requires Linux or the official
Linux container on this Windows host. There is no supported native
Windows package for 3.12.
[Native lookup contract](https://docs.arango.ai/arangodb/stable/aql/functions/miscellaneous/#document).
[Native platform support](https://docs.arango.ai/arangodb/stable/operations/installation/).

All 13 provider tests passed at Core `0492cec40a87a7ab84ac2ec7718e017ae4463871`.
The cursor tests verified 3/2, paging, deletion and absence, private record
rejection and strict failure on a foreign key, bad payload or incomplete
cursor. WEB4 registration and native server execution remain open.

WEB4 now reads protected `CQL_ARANGODB_QUERY_` settings with suffixes ORIGIN,
READER_USERNAME, READER_PASSWORD and PROBE_ID. The connection is pinned
to the configured HTTPS cursor endpoint in database `oasis`. The
transport checks the exact AQL, runtime limit and canonical key set before
sending a request. The protected settings preparer requires the first
reviewed live ID as the probe. Eight settings tests passed.

The native fixture is enabled with
`/p:EnableArangoDbHostedCqlNativeTests=true` and selected with
`TestCategory=ArangoDbHostedCqlNative`. The fixture build passed and all 11 WEB4 admission tests passed at Core
`c7743e6d9ee0432d180311e4a85a23dbd4980b93`. An approved TLS endpoint and restricted reader for
`oasis/holons` are required for the native run.

The original task branch records native restart and Mongo-paired browser
proof at `d36cc175c` and `ed5d9c6bb`. Those results cover the original source
and local demo. This recovery build covers the new WEB4 admission path.

Admission TRX SHA256
`a98217a443336f380162b69701b3b0130defedf71c64b7eea1c87262477c03ed`.
The fixture was compiled here. The approved hosted ArangoDB source and
live Development verification are still pending.
