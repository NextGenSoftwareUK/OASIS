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
