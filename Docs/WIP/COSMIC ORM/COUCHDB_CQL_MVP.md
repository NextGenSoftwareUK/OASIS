# CouchDB CQL MVP audit

Updated30October2026. HTTP boundary controls verified; CQL/native/paired demo red.

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

- No CQL adapter/capability admission. Add bounded native identity selection using
  shared compiler; reject unsupported shapes before I/O, no full-store managed scan.
- Current upsert fetches current _rev rather than asserting caller's observed token;
  cross-client optimistic concurrency and atomic conditional writes not certified.
- Save mutates a missing caller ID before acknowledgement and does not publish saved
  counts/receipt; batch ignores individual errors and continueOnError semantics.
- Save response body id/rev/ok validation and native record identity need proofs.
- Existing Mango searches truncate at1000; full-store metadata filtering isn't CQL.
- Child depth/recursive/version/type params are not fully implemented; defer large
  parity depth but keep red acceptance explicit.
- Native environment/cache/source not yet inventoried; no CouchDB downloaded or run.

Next bounded CQL adapter/HTTP execution controls, then inventory/reuse free native
CouchDB and journal source IDs before native3/filter2/restart/Mongo-paired5/4 proof.
No public hosting, production concurrency, replication or complete parity claim.
Arweave's separate automatic-review gate remains documented; do not bypass it.
