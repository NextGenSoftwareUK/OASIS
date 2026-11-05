# Solr bounded CQL scope

5 November 2026. Controlled provider/CQL tests: 16 passed, zero failures or
skips. This is an adapter milestone, not native or hosted acceptance. Coverage
counts remain 97 local profiles, 60 hosted HTTP proofs and 28 hosted browser flows.

The CQL path uses the shared bounded identity compiler and exact reads through
`GET /solr/oasis_holons/get?id={canonical-guid}&wt=json&omitHeader=false`.
Read-only activation checks the existing collection using
`GET /solr/oasis_holons/schema/uniquekey?wt=json`; successful Solr status and
`uniqueKey=id` are required. Neither operation creates collections. The normal
provider activation/provisioning path is not used by CQL activation.

Each successful response must contain a successful responseHeader and doc.
Only HTTP success with explicit `doc:null` means an absent record; HTTP 404,
authentication, collection errors, malformed responses, wrong identities and
payload failures return explicit OASISResult errors without partial rows.
Canonical Holon identity/read authority, deletion filtering, ordering and paging
are enforced. The transport caps each response at 1 MiB, the query aggregate
at 4 MiB, and the overall query at 15 seconds. Child graphs are excluded.
Default HTTP redirects are disabled; an injected client must obey the same
transport policy. Historical/version-control reads remain explicitly unsupported.

The adapter consumes stored camel-case Holon JSON fields, matching the existing
provider serialization contract. Native Solr schema compatibility, including
single-valued fields and nested Holon data, has NOT been certified. The existing
CRUD collection provisioning/save implementation is not certified by these tests.
Do not advertise complete provider conformance or import support from this work.

## Native acceptance prepared, not executed

`SolrCqlNativeTests.cs` requires explicit COSMIC_SOLR_CQL_SOURCE metadata:
Endpoint (HTTP loopback origin), Actor, LiveIds (three), DeletedId and MissingId.
COSMIC_SOLR_CQL_USERNAME and COSMIC_SOLR_CQL_PASSWORD are optional together,
provided only through the environment. No credentials belong in source metadata.
It verifies the existing deleted/missing controls, 3/2/0 reads, paging and hashes
of the four existing native document payloads before/after. It never provisions,
seeds, repairs or deletes a source. A real source and retained fixture evidence
are still needed before incrementing coverage or adding a public demo card.

No new runtime/download was installed. Java is absent from PATH; the retained
QuestDB 10.1.0 runtime is Java 25.0.2 but is a custom module image (14 modules,
including io.questdb), not an admitted general Solr runtime. No assumption that
its version alone proves Solr compatibility. Solr runtime qualification and
manager/Mongo/HTTP/browser acceptance remain open.

Protocol references: [RealTime Get](https://solr.apache.org/guide/solr/9_1/configuration-guide/realtime-get.html),
[Schema API](https://solr.apache.org/guide/solr/latest/indexing-guide/schema-api.html),
[Java requirements](https://solr.apache.org/guide/solr/latest/deployment-guide/system-requirements.html).
