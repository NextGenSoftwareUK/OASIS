# Elasticsearch CQL MVP continuation

5 November2026. Active, not yet accepted as a new local profile.

The existing REST provider stores an indexed projection and a canonical Holon
JSON string in `dataJson` under `oasis-holons`. Its bounded CQL adapter now uses
the shared authorized identity compiler and realtime native document reads.
It checks the selected index/document/canonical identity and verifies indexed
deletion, parent and type against the canonical Holon before exposing rows.
Missing index, inconsistent missing responses, malformed records and budget
failures are errors without partial rows. Query-only activation verifies an
explicit live existing record without provisioning indices. General CRUD,
historical reads and full parity are not certified by this work.

Controlled tests pass32/32, no skipped cases (`controlled-v3.trx`). Original
compile failure in `controlled.log` was two obsolete metadata test assumptions;
test contracts were corrected, not production APIs invented. `controlled-v2`
passes31; v3 adds deactivation clearing CQL admission and reruns after that code
change. Two runner controls prevent prior-run shutdown logs certifying a later
run and reject truncated log lineage. Native fixture build passes0errors/5warnings;
rebuild its outputs after the subsequent deactivation change before native use.
No acceptance/count increment until native query, manager/HTTP and browser complete.

Official Windows8.19.24 archive is520,583,121bytes; vendor SHA512 verified:
`a88a6aec0f02c18c24ead95461b564b8318dc4ea1cf354ce05c9e4a822430e84f8f92f7017259b4c6110d35b902925f402a6b264dbbdd9dbc86f8d34917290b9`.
One cache under `LocalState/COSMIC/RecoveryLaptopElasticsearch81924`.
Shared JRE21 passes version/help but actual server fails before readiness:
`jdk.jlink` is required by `org.elasticsearch.entitlement`. The original failure
is retained. Its archive was removed prematurely after version qualification;
the identical official archive was restored and its complete bundled JDK27
extracted. No entitlement bypass or mixed runtime modules. The archive has now
been removed after actual native startup/shutdown qualification.

Non-JDK runtime887files/514571804bytes, manifest SHA256
`a62eac761035e14e3327308430105c9aa7a803a2ecb6cf9862cae972aaa3d568`.
Bundled JDK479files/340254021bytes, manifest SHA256
`89bbce38a7fecebd6294288887796e0816a3b990f3c029c3412f48ea4ebd6519`.
Two real daemon preflights pass, version8.19.24/native product header/owned
cluster verified; exact native PID receives Ctrl+C, node stopped/closed logs,
exit130, both19400/19500listeners closed. No business source/index initialized.
Second preflight uses dedicated `ES_TMPDIR` under the cache; initial default
temp path `C:/Users/David/AppData/Local/Temp/elasticsearch` remains untouched.
Single loopback teaching node has explicit local-only settings; hosted security
and credentials are not certified. Heap128/512MiB, two processors.

Native capacity receipt `preflight-998dad0768f147da90ad584d9723dddd-capacity.json`
reports total456665329664bytes, available42763169792, high watermark90%, required
45666532967. After archive cleanup C free43283451904, still about2.38GB short.
Native watermarks stay unchanged. Source/bootstrap, restart query, manager/Mongo/
HTTP/browser and WEB4 card remain open; counts100/60/28 unchanged.

Continue: reclaim agent-owned disposable space through permitted scoped cleanup
while preserving all source cohorts and others' outputs, then rebuild native
fixture under the lease and explicitly admit one journalled source. Do not
repeat unit acceptance or create source IDs until capacity is available.

Primary references: [Windows distribution](https://www.elastic.co/guide/en/elasticsearch/reference/8.19/zip-windows.html),
[minimum Java runtime17 in the exact release source](https://github.com/elastic/elasticsearch/blob/v8.19.24/build-tools-internal/src/main/resources/minimumRuntimeVersion).
See the [recovery cleanup ledger](RECOVERY_LAPTOP_CLEANUP_LEDGER.md).
