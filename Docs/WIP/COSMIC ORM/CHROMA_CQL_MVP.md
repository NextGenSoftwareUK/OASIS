# Chroma CQL demo — current integration

## Source recovery — 2 November 2026

Named provider/CQL/schema/tests/PowerShell/Python/setup source restored from
retained pin237403a75. Fifty controlled cases, producer/consumer compilation and
syntax pass. Tests inject HTTP responses; original runtime, tenant/database,
credentials and fixtures remain unopened/unverified. Setup source was parsed, not
executed. Native/browser acceptance below is historical, not a new run. Hosted
Chroma, history and semantic similarity search are not certified by this recovery.


31 October2026. **55 passed, zero failures/skips**:50 unit controls and
native-first/restart/Mongo identity/paired SDK-HTTP/native-postpair, one each.
Actual browser full query5 rows, filtered subset4, provider provenance retained.

```powershell
Scripts/TestHosts/Run-CosmicChromaLinuxDemo.ps1 -Federated -Serve
```

Run in C:/Source/OASIS-cosmic; open http://127.0.0.1:5133/.
Finish with the local stop button and wait for runner cleanup/exit.

Official retained Chroma1.5.9 image
a221f3391b5eabc5b0b293b65958a28f10d7015be67e4d8cc33a5e4e60e6f3ca.
Original three documents, collection IDs and schema verified across restart and
postpair; no reseed/business-save replay. Original schema preimage checked;
schema object property order canonicalized, array/document bytes remain exact.
Native source /var/tmp/oasis-chroma-cql remains311674B.

Native multi-ID get admits at most100 trusted catalogue IDs,15s deadline,
streamed4MiB response/1MiB document limits, matching UUID/document pairs,
access checks, stable paging and no partial rows. Activation only resolves
explicitly provisioned schema through read-only GET. Holon save stages caller
identity until native success; HolonManager owns tracking acceptance.
Fixed wrapper builds Mongo producer before federated consumer. Earlier successful
phase reports archived by hash; unresolved evidence stops before native start.

Exact owned Mongo fixture verified/dropped; pending/ready absent, nativeexit0,
noOOM/noError, Mongo stopped; ports18002/27039/5133 absent. Public CQL section39
main d8afd91/Production7454604193 success; expanded command/link verified.
Local catalogue only, not hosted Chroma connectivity. Fixture is loopback,
plaintext/no-auth; no semantic search/security/full parity certification.

Evidence under C:/Source/OASIS-cosmic/TestResults/ChromaCqlCurrent:

| File | Passed | SHA256 |
|---|---:|---|
|chroma-current-controls.trx|50|F9C57EFCA5809EDA5700B871B2C03DEA499AA0463B4330143B510D2D1427F469|
|native-first.trx|1|7E1140905705B7FA574D2BC6300B14FDB56B63A879C99419817C38D229391E9D|
|native-restart.trx|1|6DB788956AA8F48D2DADD781F291ECC25C038233AC1872797D7BFC4E0A7A6B5D|
|mongo-init.trx|1|A9A8EA9F564854B2DB4ED7B49EF85A1FACEA93814C0BC3289043B2132621AC80|
|mongo-federation.trx|1|B625A9C0154B3747D66FB972273525C130ACCC6D3F52E469F6B25AC21B4D071F|
|native-postpair.trx|1|0780692DFE2B7697F35FE0B4F2DB3DE61A505741E57E4719E0E5705FE132C5E8|

Browser proof: chroma-mongo-subset-current.png, chroma-catalogue-public.png.
Private completed test bins400files/95605789B were initially retained: automatic cleanup review
rejected removal without detailed reason; nothing deleted. Retained assets/cache
allow bounded leased rebuilds. Source, original receipts and schema retained.

General content predicates, central registration, generic read errors/ID bounds,
other write bounds, avatar ownership, children/history/CAS/unknown-write recovery,
replication/migrations/full HyperDrive and semantic/hosted security need separate
acceptance. Advertised demo failures remain immediate fixes, not deferred items.

Subsequent user-requested reviewed cleanup removed these private bins as part of63 verified directories/4751008965B. Source/evidence/cache/obj retained; see COMPLETED_TEST_OUTPUT_CLEANUP_2026-10-31.md.

## Development enrollment gate

The recovered provider exposes explicit read-only CQL activation. It verifies
the existing holon collection by name and canonical UUID using GET. The
WEB4 now has opt-in registration with a guarded HTTP client, explicit
HTTPS origin, tenant and database. Qualify the exact collection and three
live teaching IDs with credentials restricted to that source before
publishing the Development catalogue. Chroma's bounded reads use POST for the native collection
query API, so qualify read permissions by operation and path. Keep native
mutation endpoints outside the query credential's scope.

The adapter needs these two native operations under the exact configured
tenant and database:

- `GET /api/v2/tenants/{tenant}/databases/{database}/collections/oasis_holons`
  for collection activation.
- `POST /api/v2/tenants/{tenant}/databases/{database}/collections/{uuid}/get`
  for the bounded ID query. Use the collection UUID returned by activation.

Qualify the credential against both routes and confirm that collection
creation and document upsert/delete are denied on a disposable fixture.
The provider's injected HTTP client supports separate host-managed
authentication. Keep credential values out of source configuration and
acceptance reports.

The opt-in WEB4 registration uses `CQL_CHROMA_QUERY_ORIGIN`,
`CQL_CHROMA_QUERY_TENANT`, `CQL_CHROMA_QUERY_DATABASE` and the private
`CQL_CHROMA_QUERY_BEARER_TOKEN`. This contract targets a reviewed HTTPS
service that accepts a scoped bearer credential for the two read routes.
The WEB4 build and the exact Core `7b17894c` admission run passed. Chroma
and CouchDB passed 2/2 with zero skips, covering the zero-cost query-only
tariffs and four planned presets per provider. The parent gitlink and
Railway manifest match. The earlier loopback native/browser run establishes
the three-record demo shape. The original evidence path is absent on the
recovery laptop, and that run used plaintext without authentication. To
accept Development, qualify the reviewed HTTPS endpoint and scoped credential,
replay the same three IDs on that endpoint, then verify WEB4 discovery,
native and Mongo-paired query results, and the signed-in browser card.

## Recovery-laptop HTTPS read qualification tool, 5 November 2026

`Scripts/Sandbox/Probe-ChromaHostedReadiness.py` now qualifies the exact two
provider read routes without creating a collection or mutating documents.
Supply a metadata-only JSON source with exactly `origin`, `tenant`, `database`,
`collectionId` and `ids` (three distinct canonical UUIDs). The reviewed collection
must already contain the three public, active, undeleted canonical Holon documents.
The collection activation response must match both oasis_holons and the reviewed
UUID. Native reads request documents only, with bounded full/subset selections;
response order is not treated as stable. Each payload ID must match its native ID.

Keep the scoped bearer token only in the process environment
`CQL_CHROMA_QUERY_BEARER_TOKEN`. The tool rejects redirects, proxies, non-HTTPS
origins, URL credentials and local/private DNS results before credential dispatch.
It makes at most three requests, each with a 15-second timeout and 4 MiB response
bound. Receipts retain only the source hash, counts, outcome type and scope;
neither credentials nor document contents are written to the receipts.

```powershell
python -B Scripts/Sandbox/Probe-ChromaHostedReadiness.py `
  LocalState/COSMIC/ChromaHostedQualificationCurrent/source.json `
  LocalState/COSMIC/ChromaHostedQualificationCurrent
```

Exclusive intent/ACK/failure files prevent an implicit repeat after uncertainty.
Review the original outcome before any separately authorized qualification run.
Six controlled tests pass without network calls or new runtime dependencies.
No real hosted endpoint or credential has been supplied or qualified. The tool
explicitly does not certify denial of mutation routes, hosted CQL, federation,
replicas or browser acceptance. Qualify denied mutations only on a separately
owned disposable fixture, then retain that distinct authorization proof.
