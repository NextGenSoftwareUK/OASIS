# CouchDB CQL MVP audit

Updated30October2026. Bounded CQL/HTTP controls verified; native/paired demo red.

## Root fixes and evidence

Revision reads now treat only404 as absent.401/403/500 stop before PUT; existing
records without valid string _rev reject before writes. Hard-delete read errors no
longer report success. Index HTTP failures prevent activation. Revision/id JSON uses
native serialization, and deletion revision query values are escaped. Missing provider
name/description corrected. Existing constructor signature remains unchanged.

Original unit project could not compile because a stale test called nonexistent
GetProviderVersion (not a provider API). Removed that invalid test; no fake production
method added. Actual run11tests:8failed/3passed. Fixed full11passed/0failed/0skipped.
Seven HTTP cases cover denied/unavailable revision reads, malformed existing document,
actual404 creation, failed index activation and hard-delete denial. Four metadata
checks pass. HttpMessageHandler fixtures use no real CouchDB or socket.
Evidence `TestResults/CouchDbCqlCurrent/http-fixed.trx`, SHA256
`af7370825b55c183248ec2f84f7039819473626fe16a0ef49165075d6313d8e8`.

## Gaps and exact next step

- Bounded CQL adapter and HTTP execution now verified by controls below; native
  source/restart and paired demonstration remain unverified.
- Current upsert fetches current _rev rather than asserting caller's observed token;
  cross-client optimistic concurrency and atomic conditional writes not certified.
- Save mutates a missing caller ID before acknowledgement and does not publish saved
  counts/receipt; batch ignores individual errors and continueOnError semantics.
- Save response body id/rev/ok validation and native record identity need proofs.
- Existing Mango searches truncate at1000; full-store metadata filtering isn't CQL.
- Child depth/recursive/version/type params are not fully implemented; defer large
  parity depth but keep red acceptance explicit.
- Native environment/cache/source not yet inventoried; no CouchDB downloaded or run.

Next verify save acknowledgement ownership before fixture writes, then inventory/reuse free native
CouchDB and journal source IDs before native3/filter2/restart/Mongo-paired5/4 proof.
No public hosting, production concurrency, replication or complete parity claim.
Arweave's separate automatic-review gate remains documented; do not bypass it.

## Bounded native-key CQL execution controls

CouchDbCqlCompiler reuses the existing shared identity compiler, bound to CouchDB
read authority and at most100selected keys. Each becomes GET of the canonical
native document ID with conflicts=true. No _all_docs/_find scan or managed business
predicate. GUID equality/membership/AND/OR/paging admitted; unsupported graph,
business fields, metadata and ordering reject. Unverified IsVersionControlEnabled
mode rejects before HTTP rather than silently serving another revision policy.

HTTP streams enforce aggregate4MiB payload bound. Native _id/_rev and decoded
holon Id/read authority checked; explicit native conflicts reject as ambiguous,
404/deleted rows skip, children excluded. Paging after complete selection and whole
page result budgets enforced. In-flight HTTP cancellation and later read errors
publish no partial rows. Independent document reads; no database/federated snapshot.
Existing plaintext JSON codec; no encryption/production concurrency claim added.

Full33tests pass (18previous/admission +15execution). Execution controls prove
three-record catalogue selecting only two native reads, paging, tombstones, wrong
identity/revision, HTTP/malformed/later failure, native conflict,4MiB/result budgets,
real token propagation to an in-flight HTTP handler and version-mode denial.
No live CouchDB or sockets involved; native acceptance remains red.

Evidence `TestResults/CouchDbCqlCurrent/cql-execution.trx`, SHA256 `4846fbb763afafd336862776e35d32f113bda875a3d061d9fe8fd91a4bbdd950`.
