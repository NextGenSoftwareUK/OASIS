# ScyllaDB CQL MVP — native/restart/Mongo-paired demo verified

Updated 30 October 2026. Scoped native/C#/HTTP/browser acceptance passes; full parity remains open.

## Implemented and verified

Explicit ScyllaDBOASIS query authority uses shared bounded identity algebra:
complete trusted catalogue at most100GUIDs, identity equality/membership/Boolean
composition and paging. Prepared native partition-key IN, no scan/ALLOW FILTERING.
Restore stable catalogue order before paging; reject identity/authority mismatch,
duplicates, deleted rows,4MiB decoded payload/result budgets. No partial error rows.
Driver I/O has surrounding cancellation checks; active I/O cancellation and wire
byte bounds remain open. Arbitrary metadata/content/graph predicates reject.

SaveHolon stages payload/ID/copied keys and publishes caller identity and save
counts after native acknowledgement only. Failure preserves caller state; batch
stop/continue/count ownership fixed, HolonManager owns tracking acceptance.
Activation state and canonical keyspace identifier boundaries fixed. Native
activation exposed SimpleStrategy/tablet incompatibility: keyspace now uses
NetworkTopologyStrategy, asserted against actual native schema by integration test.

23unit controls passed; native initial1/restart1 passed on original source:
three records/filter two/missing zero/page one/budget rejection. JSON hashes and
native write timestamps match before/after queries and real restart. Source-loss,
orphan baseline and uncertain-write guards remain strict. Proving files:
- UnitTests/ScyllaDbCqlTests.cs and ScyllaDbSaveOwnershipTests.cs (provider test tree).
- IntegrationTests/ScyllaDbCqlNativeTests.cs, including native replication strategy.

Raw evidence TestResults/ScyllaDbCqlCurrent (not committed); controls SHA256
`8e3be116816f3e701d186f95445c6e3c813c2d826fec17da607c880ad065c223`.
Native TRX SHA256:
- native-first: `a8b92cc974c8f6d6b9e34be812a99e2751347fe0aed4ea1071368a15542b7cdb`
- native-restart: `92bacbd407b230da4690b2895bd888fad23b1751858e937bfb042293f9f608d0`

## Retained original environment

Official image pinned `sha256:2a3cf99b55d793a41e19b4d8afa4084744bc05517f4b4a211483c8d2abe4ef3a`,
image ae6d996ec593faef2a0cbdce559de45f8fbaa397309b23c4f461d73047d40bcf,
577,655,941B. Actual binary2026.3.4-0.20261018.daef9dad4a01; advertised Cassandra
wire release3.0.8 does not identify the product. Host75239ba2-9e5a-462b-97bd-1753918374d9,
cluster COSMIC CQL ScyllaDB stable across restarts, original actor/3IDs/receipts kept.
Source `/var/tmp/oasis-scylladb-cql`11,101,785B logical, original container
5fef2462d2a99bb90dd92eb6cb71a82d0928a6df8c571b16de50e92fa8eac539,
loopback19043only,1CPU/3GiBcap/1Gnative target/developer-mode. Native transport
local plaintext; no TLS/hosted/performance/production claim.

PasswordAuthenticator/CassandraAuthorizer enabled throughout. This release has
no default superuser: original random oasis_test role created once over documented
private maintenance socket after empty LIST ROLES/oasis schema readback. Original
identity acknowledged before writes. Credentials/keys stay in private source files.
Unused initial bootstrap config retained as historical setup, never used to create
or reset a default role. Network password authentication passes.

Vendor wrapper loses string argument boundaries. Local test-container correction
writes cluster_name through supported scalar YAML rather than a lossy argument;
original wrapper/failed quoting attempt/hashes retained. Vendor binary/image unchanged.
The2GiBcap left insufficient memory after native reservation; same container cap
corrected3GiB. No duplicate environment/runtime or source replacement. Original
prewrite intent from rejected SimpleStrategy continued only after absent keyspace/
manifest/baseline/write receipts readback; same IDs preserved and intent hash acked.
Original failed native setup history stays separate; no unknown write replay.

Both native phases stopped own node0/noOOM/noError; runner72046terminal0,
pending cleared and original source retained. All resources in the ledger.

## Next and commands

Run `Scripts/TestHosts/Run-CosmicScyllaDbLinuxDemo.ps1` for bounded native/restart.
Setup refuses existing source. Recovery switches are explicit narrow readback
workflows; never delete pending intents, reset/reseed or replace baselines.

Mongo-paired C#/HTTP/browser5/4 and hub routing pass; original native rows/write
timestamps checked before/after, exact disposable Mongo fixture verified/dropped.
IsVersionControlEnabled currently has no save/history behaviour. Avatar ownership,
graph/children/CAS/history/replication/migrations/metadata depth/full parity remain
open. No Scylla acceptance inferred from Cassandra's separate proof.
[Official Docker guide](https://docs.scylladb.com/manual/stable/getting-started/install-scylla/run-in-docker.html),
[superuser bootstrap](https://docs.scylladb.com/manual/stable/operating-scylla/security/create-superuser.html).
[Current status](CURRENT_STATUS.md) / [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).


## Mongo-paired demo

```powershell
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider ScyllaDB -Federated
# Add -VerifyOnly for bounded native/restart/C#/HTTP checks.
```

Browser http://127.0.0.1:5139 shows5physical rows (native3+Mongo2), then4for the
2-ID subset; provider copies/provenance retained, Tea excluded. Forged/unknown
HTTP presets reject; native inactive produces503/no partial rows. No distributed
snapshot/replication/full-parity claim. Shared code lives in Examples/CosmicCqlDemo/
IdentityFederationWalkthrough.cs and IdentityFederationApiHost.cs; integration
fixture ScyllaDbMongoCqlNativeTests.cs exercises C# and HTTP.

Both bounded and interactive runners passed5phases/one test each. Browser5/4
verified, screenshot TestResults/ScyllaDbCqlCurrent/browser.jpg49,809B. Tab closed
at turn boundary; original live runner stopped through the same HTTP demo endpoint.
Final native JSON/writetime preserved, original source11,108,511B logical retained;
exact cosmic_cql_mongo_scylladb_demo dropped, pending/ready cleared, own19043/Mongo27039/
demo5139stopped0, original runner67700terminal0. Hub8single/16plans,48paired/96plans,
82paired and3archive rejections pass, listeners unchanged.

Earlier bounded pair recorded21m54s elapsed with unknown cause. Repeat timestamps
show native+Mongo activation, C# and HTTP including deactivate/reactivate complete
in1.36s, browser ready1.45s after start; later wait was interactive. No performance
SLA inferred and no further green hardening without a reproduced bug.
Current interactive TRX hashes under TestResults/ScyllaDbCqlCurrent:
- native-first: `bcf6a4f9d3e717c3695ab4e88dbc65af1a4c03e5c15138b993d52f3c2bebd524`
- native-restart: `0f23b38d9dfc3db875b47976ffdf9bb98fe676b413552fa6ae99c9a81ad7dcc7`
- mongo-init: `b3323ed1e422f09b51370a60d53339152da4c2cf595e006e99a1719f4e4bf54c`
- mongo-federation: `78cb01ee65a6de03b1b039eb65f9349534cab5a730bbffb8ad9b70f38ae18761`
- native-postpair: `4676bd242a2bbe6bd1e2ca3447a6903658a815aa61d993e75d5a456bde08bfad`

Next red provider: Elasticsearch; continue breadth before deeper parity.
