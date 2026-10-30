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
- Holon save ownership/receipt and batch continuation root fixes now have controls
  below; avatar and wider graph/history save behavior remain open.
- Save response id/rev/ok validation and staged native identity pass controlled HTTP
  proofs below; live CouchDB acceptance remains open.
- Existing Mango searches truncate at1000; full-store metadata filtering isn't CQL.
- Child depth/recursive/version/type params are not fully implemented; defer large
  parity depth but keep red acceptance explicit.
- Native environment/cache/source not yet inventoried; no CouchDB downloaded or run.

Next inventory/reuse free native
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

## Holon save ownership and native receipt controls

Eight new tests initially8failed. Full fixed41passed/0failed/0skipped. Missing holon
IDs are generated locally and staged into detached wire JSON, together with canonical
provider storage key; caller identity/key remain unchanged until successful receipt.
Runtime concrete fields serialize into the document without mutating caller state.
Native ok=true/id matching staged key/nonempty rev are required. Invalid/failed
receipts preserve caller state and require outcome reconciliation before retry;
no silent retry introduced. Result IsSaved/SavedCount/native revision metadata
publish only after acknowledgement; manager owns clean-state flags.

Batch honors continueOnError (stop or continue), exposes partial acknowledged count
and errors instead of dropping failed writes. Tests cover delayed publication,
rejection, wrong identity/empty revision/false ok/malformed receipt, failed/continued
batches, canonical staged Id/provider key and revision receipt. Shared Upsert receipt
validation also applies to its existing callers; avatar caller staging remains open.
This does not implement cross-client CAS: fresh _rev fetch still differs from an
expected caller token. Keep atomic conditional-write parity red.

Native image inventory: no CouchDB image found in current rootful WSL Podman cache;
other suitable runtimes still need inspection before download. No service started.

Evidence `TestResults/CouchDbCqlCurrent/save-fixed.trx`, SHA256 `d846d541714a721b42b9016b69c33a0761fcab49392b6b8950a6cf22d7d55f7b`.
