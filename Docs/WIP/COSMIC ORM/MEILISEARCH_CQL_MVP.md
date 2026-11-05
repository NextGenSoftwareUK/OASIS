# Meilisearch bounded CQL demo scope

5 November 2026. Provider/shared build and 16 controlled tests pass, zero
failed/skipped: eleven new HTTP/CQL cases and five existing metadata checks.
No native runtime, manager/LINQ, HTTP/Mongo, hosted or browser proof yet.
Coverage counts remain unchanged.

The existing provider stores camel-case canonical Holon JSON in oasis_holons,
with id as its primary key. CQL now uses the shared bounded identity compiler
and exact GET /indexes/oasis_holons/documents/{canonical-guid} reads, after
intersecting server-owned authority. It does not invoke search scans or the
legacy implicit index-creation path. Read-only activation verifies both uid
and primaryKey through the [official index API](https://www.meilisearch.com/docs/reference/api/indexes/get-index).
Default HTTP transport refuses redirects; an injected client remains configured
and owned by its caller. Bearer credentials use the existing provider contract.

The adapter bounds each streamed document to1MiB and all accepted native bodies
to4MiB, with one15-second query deadline. Native ID and decoded read authority
must match; duplicate document properties reject. Deleted records are excluded
before canonical-key paging. Unsupported predicates/history reject explicitly.
No partial rows are published after any failure. Only HTTP404 with the native
code document_not_found is absence. Missing-index, auth/authorization, server
errors and malformed error envelopes remain query failures.

Controlled tests use an HttpMessageHandler, not a native server. They verify
3/2 selection and paging with deleted/missing controls, activation without writes,
index/primary-key mismatch rejection, wrong/duplicate IDs, oversized second
records and unchanged error semantics after a valid first row. They prove no
search or mutation route is requested by these CQL paths.

Next qualify one reusable real Meilisearch service, canonical retained records,
missing/deleted controls and source preservation; follow with manager/LINQ and
HTTP/Mongo pairing, privacy and replicas. Only then prepare WEB4 source/tariff/
preset enrollment. Wider content/search, writes/conditional concurrency, graphs,
history, replication, migrations and full ORM parity remain separate gates.

## Recovery-laptop native runtime preparation

The official community Windows v1.56.1 asset is cached once under
LocalState/COSMIC/RecoveryLaptopMeili1561. Source release:
https://github.com/meilisearch/meilisearch/releases/tag/v1.56.1 .
Published size353,019,904bytes and SHA256
ac6d2506cafe862c93a6add533e125a19852339ec21303413120bde84aad3a48
match the downloaded executable. --version reports meilisearch1.56.1. This is
the community asset, not the enterprise executable. The release repository
separately identifies MIT and enterprise BUSL code; no enterprise activation or
binding license prompt was accepted. No service, native index or documents yet.

MeilisearchCqlNativeTests uses COSMIC_MEILISEARCH_CQL_SOURCE metadata plus
COSMIC_MEILISEARCH_CQL_KEY in the process environment. It reads one existing
loopback source, validates3live/1deleted/1missing control, tests3/2/0selection
and paging, and requires identical document hashes before/after. Missing
control must be HTTP404with document_not_found, not a missing index. All native
observation bodies are bounded, and no test write or provisioning occurs.
A364-byte source.json was created once with fixed actor/five IDs and port17700.
Its records do not exist yet. Native bootstrap, actual test execution, manager/
LINQ and Mongo/HTTP remain open. Runtime identity alone is not CQL acceptance.

The leased native fixture build passed with zero errors. It has not run against
a server; no native coverage count or source-mutation claim follows.
