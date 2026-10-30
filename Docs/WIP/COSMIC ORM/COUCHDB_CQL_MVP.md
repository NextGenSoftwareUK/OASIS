# CouchDB CQL MVP audit

Updated30October2026. Bounded CQL/native record/restart verified; paired demo red.

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

## Retained native environment — 30 October 2026

No suitable Windows/WSL/rootful/rootless cache resource found. Reused existing
rootful Podman and downloaded official Apache CouchDB3.5.1 Linuxamd64 once:
`docker.io/apache/couchdb@sha256:701bf53dd95b8742cd4120fb8f5aea8f14c0d8a77cfa807e86caaa537f822ee1`.
Image ID df2aa2a1f3b5c7b5b8ecd51132941c204a0f1da91219c3280a83feb6260c7e11,
size281,148,524B. Retain/reuse, no per-run image or source recreation.

`Create-CosmicCouchDbEnvironment.py` created one authenticated single-node source:
`/var/tmp/oasis-couchdb-cql`, container oasis-couchdb-cql ID
1379f2e07bac0974b29a2801d508d1007f039b47239955f9254bc5c3f801a4b0.
Only loopback15984 published;2CPU/512MiB/256PIDs, bounded Erlang schedulers.
Private credentials file0600 owned by existing user1000; config/data private and
owned by container5984. No secrets in reports, arguments or committed files.
Original setup intent/container acknowledgement/environment manifest retained;
creator refuses an existing environment instead of resetting it.

Authenticated native first/restart checks pass version3.5.1 and identical server
UUID3297b77519b62c5593f1c85449e8ea4b. Anonymous database listing401. Only _replicator
and _users system databases; no business holons. Both owned starts stopped with
exit0; no running gateway. Retained source/config/credentials total43,245B at setup.
This is environment admission, not native record/CQL/replication proof.

Next: reuse this container/source and existing Linux.NET10/portableVSTest to verify
journalled original holon saves, native3/filter2/hash/sequence stability and actual
process restart, then Mongo-paired C#/HTTP/browser5/4. Never recreate credentials
or source if missing/unknown acknowledgement. [Official image/setup](https://docs.couchdb.org/en/stable/install/docker.html).

Environment evidence `TestResults/CouchDbCqlCurrent/native-environment-summary.log`, SHA256 `5dc64980357b1bbbecc7773ea66c545adc0938a055efebefa4b09b9479427c62`.

## Original native records and process restart verified

`CouchDbNativeCqlTests` runs on retained CouchDB3.5.1 through the shipping provider.
Initial1/1 and process-restart1/1 pass. The original actor/threeGUIDs were journalled
before writes; each validated native revision acknowledged separately. Baseline
hashes persisted before completion; unresolved pending refuses replay/reseed.
Same records load Espresso/Latte/Tea; explicit ID-membership selects Espresso/Latte,
paging returns1. Native catalogue equals exactly the three original business IDs;
design-index documents excluded only in fixture verification (not a CQL scan).
Native payload hashes and acknowledged revisions match after restart; queries leave
both hashes and CouchDB update_seq unchanged. Plaintext provider JSON scope only.

One reused rootful container/source, Linux.NET10.0.12 and portableVSTest18.11.0;
test child runs as existing user1000, credentials remain private/out of logs.
No new runtime downloads. Original identity.json/three write acks/native-baseline
retained under `/var/tmp/oasis-couchdb-cql`; total231,082B at verification. Pending
absent; owned container stopped exit0 after both phases. No destructive cleanup.

Run native tests after bounded build under one lease:
`wsl -d Ubuntu -u root -- python3 /mnt/c/Source/OASIS-cosmic/Scripts/TestHosts/run-cosmic-couchdb-linux.py`.
Runner verifies exact container/image/loopback mount bindings before reuse; it never
adopts a running service. Next Mongo-paired C#/HTTP/browser5/4 and hub preset before
inventory proof enrollment. Full conditional-write/graph/history/parity stays red.

Evidence `TestResults/CouchDbCqlCurrent/native-first.trx`, SHA256 `9fa5dba03a1c66953ae54517ef3f93cc52e9aad2f7508fac970ebd1161343299`.

Evidence `TestResults/CouchDbCqlCurrent/native-restart.trx`, SHA256 `3e53c686bff74424b58098e38f76246779cf72682c88d8407f43a7d26a5522b3`.

## Mongo-paired C#, HTTP and browser MVP verified — 30 October 2026

This section supersedes the pending paired step above. The same three original
CouchDB records survived both process restart and paired use unchanged. C# and
HTTP return five physical rows (CouchDB3 + Mongo2), or four for the two-ID subset
(CouchDB2 + Mongo2), preserving provider provenance. Browser confirmed both counts
and the explicit retained-source shutdown. Screenshot capture was unavailable;
accessibility results verified the actual visible responses. No snapshot claim.

Run `Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider CouchDB -Federated`;
add `-VerifyOnly` for bounded verification. Browser host is loopback5132. Example:

```csharp
// Select a bounded catalogue of trusted holon identities for each provider.
var selected = source.Where(row => selectedIds.Contains(row.Id));

// Execute the prepared Mongo + CouchDB plan through the shared manager.
// The result retains each physical copy and its provider provenance.
var result = await manager.ExecuteFederationQueryAsync(plan, adapters);
```

The linked integration test `CouchDbMongoCqlNativeTests.cs` exercises the real
manager/adapters and HTTP host, rejects forged actors/unknown presets, and returns
503 without rows when CouchDB is inactive. Native-postpair rechecks original
hashes/revisions/update_seq. Both bounded and interactive runs passed all five
one-test phases (native-first, native-restart, mongo-init, mongo-federation,
native-postpair). Hub routing:8single/16plans,41paired/82plans,68paired and3archive
rejections; listeners unchanged. Exact disposable Mongo fixture was removed;
original CouchDB source231,082B retained, both owned services stopped, runner exit0.
No new downloads. Full CAS/graph/history/replication parity remains open.

Current evidence: `TestResults/CouchDbCqlCurrent/browser-run.log`, containing all
five report hashes; paired TRX SHA256
`e8e1aebb008eeb12a2c1787f9c29f05f17771a493955989d07e790920a249cc2`.
Native-postpair TRX SHA256
`7f2cde173acb9d0b42d46b7375590899891faf8bef95d5eb37f6916c9d705be2`.
Raw reports remain regenerable in TestResults, outside documentation.
