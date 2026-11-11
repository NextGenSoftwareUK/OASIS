# KeyDB + Mongo CQL demo

## Source recovery — 2 November 2026

Named CQL/provider/test/PowerShell/Python sources restored from retained pin
237403a75. Eight current controlled CQL cases, producer/consumer compilation
and syntax checks pass. Shared Mongo fixture support has one source under
Providers/Shared/TestSupport. Native services, credentials and original fixtures
were not opened/replayed; native/browser evidence below is historical. Hosted
KeyDB remains unverified.


Updated30 October2026. Current independent native/restart/C#/HTTP/browser scope
verified; full provider and ORM parity remain incomplete.

## Run

From C:/Source/OASIS-cosmic, PowerShell7 with the retained Ubuntu setup:

```powershell
Scripts/TestHosts/Run-CosmicKeyDbLinuxDemo.ps1 -Federated -Serve
```

Open http://127.0.0.1:5121/, run both buttons, stop locally and wait for cleanup.
All returns5 copies (KeyDB3+Mongo2); subset4 (2+2). Omit -Serve for bounded checks.
Start one paired demo at a time. Original source/keys reused; pending, changed or
missing original state refuses replay/replacement. No new runtime build/install.

## Independent scope

KeyDB binds its own CQL authority through the existing encrypted Redis transport
executor and canonical save keys. Mutable identity cannot grant another facade
these capabilities. Native version6.3.4, pinned executable hash/path, process ID and
socket ownership independently identify actual KeyDB; generic Redis fields alone
are not certification. No inherited Redis/Valkey green status.

At most100 authorized IDs support equality/membership/Boolean algebra, missing/
deleted exclusion and paging after exclusion. Native canonical-key reads without
keyspace scanning or managed content predicates. Result and4MiB post-decode budgets;
no pre-allocation bound or distributed snapshot claim.

## Current evidence

Raw outputs under C:/Source/OASIS-cosmic/TestResults/KeyDbCqlCurrent/:

| Report | Result | SHA256 |
|---|---|---|
| keydb-cql-current-controls.trx |8passed/0skips |7CA5305A22C53B60FC5E5D04A133B4D4BF4106F943F042994AD7A53D0A8A8CFF |
| keydb-cql-native.trx |1passed/0skips |AA7577C0BB6D140FB2E2D40354EE1EB73803D17E882A25F833DACCD61DD08C03 |
| keydb-cql-restart.trx |1passed/0skips |DBB19AEC13E2B86C269B5C9C14B7C409E90F150B0FFDF1C8B821BB47ADBD7FF8 |
| mongo-init.trx |1passed/0skips; retained replica identity |E91FFEE9C571D1D710025D77793E51783938E56A3BB7C871FB17A164609093A3 |
| keydb-mongo-federation.trx |1passed/0skips; SDK/HTTP5/4, rejection and cleanup |BAB6469B9347C22CFEDCA9AF6CC29F6E4DB7A390CB70F83442DF3B314884A733 |

Independent browser5/4 verified: keydb-mongo-subset-current.png. Original three live/
one deleted records and encrypted DUMP hashes unchanged through native reads,
actual restart and paired demo. Seed/configuration/key/ACK fingerprints unchanged;
seed SHA256DF9B9E649C444F0FEBB4D62415E60D76C40CD4BA2D1A81A21A29BD6ED820C015.
No Redis/shared executor changes or extra green Redis/Valkey hardening.

Exact temporary Mongo fixture verified/dropped, pending/active/ready absent,
runner exit0 and both daemon shutdowns clean;16390/27039/5121 absent. Successful
reports and previous verification receipt hash-archived; original source retained.

Public section27 verified: Web4Site main90943018de3dfbbcf2682cba2cf9b8bc5d62d317,
Production7453019513success, expanded command/fixed5121link and independent scope
visible. Screenshot keydb-catalogue-public.png SHA25697EF484500A9CF65620CF4F318F99774E654AAEC156E0276FA784472ADC1B80C.
Backend remains task-branch only; no Railway deployment. Hosted selector unchanged.

## Resources and remaining gates

Original runtime /var/tmp/oasis-runtimes/keydb-6.3.4 reused, daemonSHA256
309fe740e931ab61f4cdcc2ccb22359fca8241959ce1600224cb170dde821e29.
Native source /var/tmp/oasis-keydb-cql8226B includes471B archived verification.
Retain single data/appendonly.aof with0600keys/config. Loopback16390/DB0/AOFalways,
64MiB/one server thread. Existing LinuxMongo8.3.13/.NET10.0.12/portable test18.11.0/
NuGet reused; no download/install/rebuild/new environment.

General metadata/content/children/order queries, conditional writes/history,
active replication/cluster/failover, public hosting, central factory enrollment and
full HyperDrive/ORM parity remain separate red gates.

[Preserved history](Archive/KEYDB_CQL_MVP_PRESERVED_2026-10-30.md) ·
[Current status](CURRENT_STATUS.md) · [After-demo backlog](AFTER_DEMO_BACKLOG.md) ·
[Resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).

## WEB4 query profile preparation

The opt-in WEB4 source uses CQL_KEYDB_QUERY_ settings with suffixes
ADDRESS, PORT, TLS_HOST, USERNAME, PASSWORD, STORAGE_KEY and PROBE_ID.
Use the owned source's public TLS endpoint, existing encryption key and a
read-restricted ACL for the teaching keys. The probe must be the first ID
in the reviewed public catalogue. Keep credentials in protected settings.
The provider keeps its own encrypted key namespace and rejects a live
record whose public flag has been removed. The shared SDK profile permits
socket connections only to the configured address and port.

The shared native test category KeyDbHostedCqlNative uses the WEB4 registration with
COSMIC_KEYDB_HOSTED_CQL_SOURCE and the same protected settings. It
checks public 3/2, paging, missing/deleted exclusion and unchanged returned
records. The protected settings preparer validated the public catalogue
shape and probe binding. WEB4 admission and native fixture compilation are
running locally. The actual product still needs an approved TLS/ACL source
before hosted visitor acceptance.

The shared WEB4 admission run passed 13/13 against Core
`0561f11287d7913b235106cfc261484ae73db41c`. The native TLS fixture compiled
with zero errors. The admission tests covered distinct authority, settings
validation and rejecting an SDK connection to a different endpoint.

11 November UTC: KeyDB CI 43047936284 stopped compiling the pinned source because the runner lacked uuid/uuid.h. Candidate 076b4e1deb explicitly installs uuid-dev in CI. The native restart and federation tests did not execute in the first run. Standard CouchDB Development CI 43048587905 and Railway WEB4 deployment d0469a9d are queued.

11 November UTC: KeyDB run 43048779525 stopped installing uuid-dev because the Ubuntu Resolute mirrors failed DNS resolution. Candidate b2979f73ba pins Ubuntu 24.04 and makes package index failures explicit with bounded transport timeouts. Native build and tests remain pending.

11 November UTC: Ubuntu 24.04 CI 43049998861 also failed resolving Ubuntu package mirrors before compilation. Candidate 338a957c8c compiles the same pinned KeyDB source in a digest-pinned official Python Bookworm image with uuid-dev already present. The build container is disposable and the host runner verifies the resulting native executable and source. Results are pending.

11 November UTC: KeyDB CI 43050965468 built the pinned official KeyDB 6.3.4 source in the digest-pinned builder and passed all four strict one-test reports with zero skips: encrypted native CQL, restart, Mongo replica admission and SDK/HTTP federation. The runner matched the native process and verified the original fixture after restart. Standard matrix routing and hosted TLS acceptance remain open.

11 November UTC: KeyDB candidate ea54de967e routes generic storage tests to the standard authenticated-key matrix and calls the verified native restart/federation workflow separately. Combined diagnostic 0458ca161c is submitted on Development e5e3ca694c. Standard routing and hosted TLS remain unverified.

11 November UTC: both KeyDB jobs passed in combined CI 43051825070. Generic storage ran in the standard matrix fixture and encrypted native CQL, restart and MongoDB federation ran in the dedicated fixture. Verified wiring is published on the COSMIC branch. Development candidate ea54de967e awaits the next verified batch. Hosted TLS acceptance remains open.

11 November UTC: Development ea54de967e carries the verified four-file KeyDB standard CI routing. Generic storage and native restart/federation passed in combined CI 43051825070. Hosted TLS remains open.
