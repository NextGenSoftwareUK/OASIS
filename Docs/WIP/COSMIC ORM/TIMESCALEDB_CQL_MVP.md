# TimescaleDB CQL MVP — native and Mongo-paired demo verified

Updated 30 October 2026. Scoped native/restart/C#/HTTP/browser acceptance passes.

## Explicit product binding

TimescaleDBOASIS reuses PostgreSqlFamilyProviderBase/Npgsql CRUD and query execution.
It now explicitly binds CQL to TimescaleDB authority via the existing protected
native query-authority policy. Other unbound aliases remain unsupported; changing
an ordinary PostgreSQL provider enum cannot confer TimescaleDB query capability.
No duplicate CRUD, alternate execution path or PostgreSQL-server certification.

Product compiler binds shared bounded identity algebra: complete trusted catalogue
at most100GUIDs, ID equality/membership, Boolean composition and paging. Shared
parameterized native SELECT/materialization/budgets/cancellation are reused.
Unsupported content/sorting/graph shapes reject. Native TimescaleDB product/wire/
ordering/paging/save semantics were verified on the loaded Timescale extension.
Time-series aggregates/hypertables/compression are not implemented by this binding.

## Verified controls

12controls pass:4metadata,7compiler/inactive/pre-cancel and1ordinary-PostgreSQL
identity-tampering guard. UnitTests/TimescaleDbCqlTests.cs is the proving test.
Two stale metadata-test assumptions corrected: provider type is EnumValue.Value,
and nonexistentGetProviderVersion is not a production API. The stale version test
was removed rather than adding a fake version method. No backend used by controls.

Evidence TestResults/TimescaleDbCqlCurrent/controls.trx,12passed/0failed/0skipped,
SHA256 `87b743e5c87701c3b49e60a0244ef9e74eb1ec11f139279c2288c9c9d1667434`.
Bounded provider/test builds and cached restores pass, no new package downloads.

## One pinned official OSS runtime cached

Runtime/image inspection found no reusable TimescaleDB. One official
`timescale/timescaledb:2.31.0-pg17-oss` Linuxamd64 image was pinned/pulled:
`sha256:9192a05d435e360c861cfdb0f4b9f5ae11d33609713163465e46a64fcc577d49`.
Image ID9d6818ce427d460cb5f6750ea5c97ad7d121e0b76c02425c9020fb6a34034198,
expanded1,262,993,877B, compressed layers387,828,285B. Reuse existing rootful WSL
Podman/image; one retained secured native database/container and certificates.
No hosted signup, paid key/cloud instance or production capability claim.
[Official OSS image](https://hub.docker.com/r/timescale/timescaledb/tags)
/ [official release2.31.0](https://github.com/timescale/timescaledb/releases/tag/2.31.0).

## Native and paired acceptance

Original source `/var/tmp/oasis-timescaledb-cql` uses TimescaleDB extension 2.31.0
on PostgreSQL 17.11, TLS VerifyFull and SCRAM authentication, localhost15434.
Original system_identifier7702357589956755485 and database oasis oid16384 match
across actual container restarts. Original actor/three IDs/write acknowledgements,
JSON hashes and xmin snapshot remain unchanged; missing/orphan baselines refuse
reseeding. Private credentials and keys remain outside TestResults.

Native query proves three records, filtered two, missing zero, paging one and
budget rejection. Paired C#/HTTP and browser show five physical rows, then four
for the two-ID subset, with provider provenance and the unrelated Tea excluded.
This is bounded identity filtering over an explicit complete catalogue, not
arbitrary metadata predicates or time-series query support.

Both bounded and interactive runners passed all five phases (one test per phase).
Interactive teardown exited0: native15434, Mongo27039 and teaching5137 stopped;
exact temporary cosmic_cql_mongo_timescaledb_demo was verified/dropped, journals
cleared, original native source retained (70,090,733B logical).
Browser tab was closed at the prior turn boundary; final shutdown used the same
HTTP demo shutdown endpoint and the original live runner reached terminal success.
Screenshot: TestResults/TimescaleDbCqlCurrent/browser.jpg (50,297B).
Hub routing passes:8single/16plans,46paired/92plans,78paired and3archive rejections;
listener inventory unchanged. No HyperDrive replication certification is implied.

### Run the demo

```powershell
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider TimescaleDB -Federated
# Add -VerifyOnly for the bounded native/restart/paired checks.
```

The browser uses http://localhost:5137. Choose all records, then the subset;
stop through the demo control so the runner checks cleanup and source preservation.
The direct runner is Scripts/TestHosts/Run-CosmicTimescaleDbLinuxDemo.ps1.
No paid/hosted resource, reset, seed replay or alternative PostgreSQL backend.

### Retained proof hashes

Evidence is under TestResults/TimescaleDbCqlCurrent; raw output is not committed.
Interactive browser-run.log records five passes and stopped=true. Current TRX SHA256:
- native-first: `bb7fdd7d17de4a83d712780882c0f90c67be084e4e73c20085a544b8fb291c9b`
- native-restart: `d0c7903c79c050b7b78181b5db59c35fdd5c2f14efcd3e9d73084f6748630751`
- mongo-init: `67a44960cc0a5adc7ff239cd4685f8502b240e723d56f3a68bd3aa0f2aacdcbf`
- mongo-federation: `fce8501d93c06aaedf3f32f271b034971ccfadea070ffc428640c14b31c9d15a`
- native-postpair: `ebc511cb22a07c3595b51a25d0a15fb21e6dd80e55d134fe9aa78526fe39e962`

Full CAS, graph/history/metadata depth, Timescale-specific querying, replication/
migration and EF/NHibernate parity remain open. Continue breadth with Cassandra.
Legacy HyperDrive documentation only.
[Current status](CURRENT_STATUS.md) / [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
