# SurrealDB CQL MVP — native/restart verified

Updated 30 October 2026. Native/restart/Mongo-paired/browser scoped MVP verified.

## Implemented scope

The existing shipping provider builds against pinned SurrealDb.Net1.0.0.
A provider-specific CQL adapter now binds the shared bounded identity compiler to
SurrealDB authority. It admits GUID equality/membership, Boolean combinations and
paging over a complete server-authorized catalogue of at most100 identities.
Each selected key uses native SDK Select with typed oasis_holons RecordId; no
full-table managed filter or query-string interpolation. Arbitrary content,
sorting, graphs and broader LINQ semantics are rejected before network access.

Read admission validates decoded ID/read authority, skips deleted records,
excludes children, and applies paging after selected reads. Complete decoded
JSON budget4MiB and result limits fail without partial rows. SDK buffering occurs
before decoded-byte accounting; this is not a wire-allocation bound. Cancellation
flows through authentication, namespace selection and record reads. Independent
reads have no distributed/database snapshot guarantee. Plaintext envelope scope.

Activation now marks the provider active only after configured sign-in/use;
failed reactivation clears stale state, deactivation clears it. A failed client
setup disposes its owned SDK client and propagates the actual error.

## Verified controls

15 unit tests pass (5existing provider controls +10CQL/lifecycle tests):
`Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.SurrealDBOASIS.UnitTests/SurrealDbCqlTests.cs`.
They cover exact authority, immutable captured catalogue, unsupported shapes,
pre-cancellation, inactive state, invalid budgets, missing plan and lifecycle
state errors. They do not prove native round trips or successful authentication.

Evidence `TestResults/SurrealDbCqlCurrent/controls.trx`,15passed/0failed/0skipped,
SHA256 `be5eb0c7513924f6d42a61d69dfaf9733b8e1bba90a2b21b7c4fcc77be92e49c`.
Provider/test builds pass. Existing package-pruning warnings remain unrelated.
An initial build failed because the nonrecursive test restore did not create the
provider assets; explicitly restoring that provider resolved it, no bypass.

## Native authenticated server and save ownership verified

The official pinned server3.3.5 uses one retained authenticated loopback18001
SurrealKV store. Initial table absence was a real native admission error; activation
now defines the three required schemaless tables and checks every DDL result.
Namespace/database provisioning is an explicit environment responsibility.

Native testing then reproduced anonymous access rejection before SignIn: SDK1.0.0
Connect issues RETURN TRUE during its handshake. The provider now configures root
authentication and namespace/database on the SDK options before Connect. Anonymous
access stays disabled; no alternate unauthenticated path. Failed client setup is
still disposed and errors propagated. These initial failures occurred before seed
journal/business writes; no original IDs or records were replayed or reset.

SaveHolon stages runtime-typed JSON, identity and copied provider keys. It validates
the returned native record and decoded identity before publishing caller ID/key or
IsSaved/SavedCount. Manager clean-state ownership is preserved. Batch failure
honours continueOnError and reports actual errors/saves. This is acknowledgement
ownership, not cross-client CAS. Avatar ownership remains unverified.

`SurrealDbNativeCqlTests.cs` passes1initial +1actual process-restart, preserving
original actor/three IDs and per-write acknowledgements. CQL returns native3,
ID-membership2 and page1. Native full-record hash before/after reads and restart
matches; fixture-only table enumeration verifies exactly3business records. All
19unit controls pass, including rejected new/existing saves preserving caller
identity/keys and exact batch-stop/continue counts. Malformed native save receipts
are guarded in implementation but not independently fault-injected yet.

Evidence under `TestResults/SurrealDbCqlCurrent/`:

| Report | Passed | SHA256 |
|---|---:|---|
| controls.trx |19|7beac4c080bdeeb405c82a96eeddebbe97e2da90ee0a5f02a35396e63ccfb969|
| native-first.trx |1|cc2b304bec9e312333ddd2114d8fd5603bb99543b3d8e860d35b54355f7b5210|
| native-restart.trx |1|f2b48d974d18d9367f10192ebf864bbdf1f96d1be8ea6acc47f8f2689e92378d|

Earlier15control hash above is historical, superseded by19control result here.
One retained original source `/var/tmp/oasis-surrealdb-cql`,12,288B including
private credentials/identity/write acknowledgements/baseline. Seed pending absent,
server stopped exit0 after both phases. Runtime352,368,104B retained/reusable;
verified105,509,109B installer archive removed. Resource details in ledger.

## Exact next step / remaining gaps

Reuse original source/runtime for Mongo-paired C#/HTTP/browser5/4 and hub preset.
Do not enroll inventory green until that acceptance passes. No new seed/store.
Run existing native fixture after a bounded build under one provider lease:
`wsl -d Ubuntu -- python3 /mnt/c/Source/OASIS-cosmic/Scripts/TestHosts/run-cosmic-surrealdb-linux.py`.

Full CAS/graph/history/metadata querying and broader parity remain unverified.
Avatar writes have unverified caller ownership. No HyperDrive replication proof
from this work. SDK buffering still precedes the decoded-byte bound.

SDK reference: [official .NET repository](https://github.com/surrealdb/surrealdb.net),
[pinned package](https://www.nuget.org/packages/SurrealDb.Net/1.0.0),
[pinned HTTP handshake source](https://github.com/surrealdb/surrealdb.net/blob/v1.0.0/SurrealDb.Net/Internals/SurrealDbEngine.Http.cs),
[official server3.3.5](https://github.com/surrealdb/surrealdb/releases/tag/v3.3.5).
[Current status](CURRENT_STATUS.md) / [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).

## Mongo-paired C#/HTTP/browser acceptance complete

This supersedes the pending paired step above. Original native SurrealDB3.3.5
records and Mongo copies are queried through shared HolonManager V2. All selects
return5physical rows (SurrealDB3 + Mongo2); two-ID subset returns4 (2+2), with
provider provenance and excluded unrelated Mongo data. Browser5133 verified both
visible JSON responses and retained-source shutdown. Screenshot capture failed;
no screenshot proof is claimed. This is identity-set selection, not arbitrary
property predicates, a distributed snapshot or a replication acceptance test.

```powershell
# Interactive C# / HTTP / browser teaching preset; original source retained.
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider SurrealDB -Federated

# Bounded native/restart/C#/HTTP/post-pair verification, then automatic cleanup.
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider SurrealDB -Federated -VerifyOnly
```

The linked `SurrealDbMongoCqlNativeTests.cs` exercises the actual shared C# example
and HTTP host, rejects forged actor/unknown preset400, and checks inactive native
provider503without partial rows. Both bounded and interactive runs pass5one-test
phases. Native post-pair hashes prove original records unchanged. Exact disposable
Mongo fixture verified/dropped; pending/ready cleared, owned services stopped0.
Retained state15,408B at final measurement; physical engine housekeeping can change
store size while original native logical record hashes remain stable.

Hub routing:8single/16plans,42paired/84plans,70paired rejections,3archive rejections;
listeners unchanged. No native table scan in CQL, no new environment downloads.
Current raw evidence under TestResults/SurrealDbCqlCurrent (regenerable):

| Report | Passed | SHA256 |
|---|---:|---|
| mongo-federation-bounded.trx |1|f7853bdca817f23bdfe230ec7177e476abfd650e53fbe6838e13d1a65a9ef697|
| mongo-federation.trx |1|97caa065f84fedcb3ffb64dd8d4a5452ba8250ab598a9fe73bc067c1279e1b31|
| native-postpair.trx |1|6b401e13f1b6db38352e82347524f54f2f408969baf21224723fbfc87a40d369|

The final browser-run.log summary contains all five phase hashes. Earlier native
hashes above refer to the earlier original-admission run, not current refreshed
TRX files. Full CAS/graph/history/registration/replication parity remains open.

## Recovery checkout reconciliation

Original native, restart and browser proof belongs to 3973cec5b on
origin/work/cosmic-cql and the original Linux source. This task branch
contains the example labels but lacks the original tested runner, native
fixtures and acknowledged-write implementation. The recovery Windows build
and offline tests do not establish the earlier runtime proof on this checkout.
Recover the verified implementation and runner from that branch, keeping the
current pre-read catalogue filter and delete error propagation. Reuse original
Linux state if available. Hosted Development registration remains open.

The native and Mongo-paired fixtures, shared Mongo environment test helper and
original Linux runner are restored. The Windows integration build passed.
The restored runner targets the original pinned Linux runtime and retained
source. It has not been executed on this laptop. The demo hub route has not
been re-enrolled on this branch.

Recovery laptop inventory confirmed that WSL has no installed distributions.
The launcher now rejects missing Ubuntu before writing outputs or starting a
build. The original Linux runtime and data therefore require recovery before
the original-source verification can run on this laptop.

### Development read source gate

The recovered provider activation calls DEFINE TABLE for the storage schema
and its SDK connection currently uses AuthLevel Root. WEB4 query enrollment
requires an independent read-only activation path and credentials scoped to
the demo namespace/database. Verify the configured database by reading an
explicit teaching record and checking its native and decoded IDs. An empty
query result alone cannot establish that the dataset is present. Keep
ordinary storage activation for provisioning, then verify the read-only
path against the native source before enrolling it in WEB4.

The database-scoped query profile and dedicated reader fixture are now in the
recovery branch. Run the fixture against an existing demo database with a
database-scoped reader authorized for the teaching record. Set the
SURREALDB_CQL_READER_ environment settings ENDPOINT, USERNAME, PASSWORD,
NAMESPACE, DATABASE and RECORD_ID privately. Use the normal provider build
lease and filter the integration test to SurrealDbQueryActivationNativeTests.
Require one pass with zero skips before treating native activation as proven.
The fixture only reads the admitted record. The unconfigured run skipped
as expected, and native execution remains pending.

### Windows recovery continuation, 5 November2026
The unavailable Linux source remains separate. User explicitly authorizes
recreation of needed environments; official pinned3.3.5 Windows amd64 runtime
is being admitted once under LocalState/COSMIC/RecoveryLaptopSurrealDB335.
Vendor release asset322004480B SHA256
`fb859638bd875d658d483897d8d0c3eca6215e94a2946ff75693456b45e3bbbc`.
Direct binary download has durable intent; never overwrite/restart partial
outcome automatically. Runtime qualification receipt will precede execution.
No source records created yet. Next: explicit create-only Windows teaching
admission using new IDs, native/restart preservation, dedicated DB reader,
then separate Windows Mongo/HTTP/browser fixture on unused UI5140. Existing
Linux fixture/receipts and its URI27039 remain unchanged. Reuse admitted recovery
Mongo27029. Hold one build lease for builds/consumer runs; no nested leases.
No count increment: SurrealDB's prior accepted profile is already in baseline.
Official source: https://github.com/surrealdb/surrealdb/releases/tag/v3.3.5

Windows executable qualification passed, exact official SHA/version3.3.5.
Initial-af840a34af6b4b658e530e950697108d created/acknowledged three new synthetic
records, captured identity/baseline, then failed CQL3-versus0. The invariant
was broken by raw StringRecordId reads while writes use RecordId.From with the
SDK's typed string identity. CQL query activation and execution now use the
same typed construction. No records replayed or IDs replaced.
Retained-b41b88d4411642a69427367b0d996274 passes1/1, native3/subset2/page1 and
original full-native hash unchanged after actual process restart; TRX SHA256
`387d2bee0af1de59124b8dc9da070896db094abd112c97d3915cee654bee2bd0`.
Key-fix build0errors/4warnings. Four runner admission controls pass. Exact owned
node Ctrl+C/exit0 and no18001listener; native shutdown emits a vendor SurrealKV
warning about no runtime available, retained unchanged in server.log. Logical
restart preservation is proven; crash durability is not certified.

New reproducible entry point Run-CosmicSurrealDbWindowsNative.ps1 -Mode Retained
holds one lease and preserves every unique run report. Initialize mode refuses
an existing source. Remaining: database VIEWER reader activation (official
DEFINE USER ON DATABASE scope), separate Windows Mongo fixture/browser5140,
then correct existing SurrealDB catalogue launcher; no new profile count.
Legacy avatar/detail/ordinary-load raw-string keys are outside this reproduced
CQL correction and remain documented full-provider depth work.

Recovery branch provider controls pass11/11, zero skips after the key correction;
key-controls/controls.trx and build/test log retained in the single Windows cache.

### Restricted reader and Windows browser accepted, 5 November2026
Database VIEWER created once in reader-admission-56924a49fd4e4c6187406bc93b7759df;
its reader.ack.json remains authoritative, no user recreation. Initial SDK
reader failure retained; diagnostic reader-e4eb2ec5eb64403c9b9266d5be9dde8d
revealed CBOR error during Version because native authentication defaulted to
Root. SDK1.0.0 HTTP Basic transport emits no surreal-auth-ns/db headers.
Supported IHttpClientFactory now supplies the native database authentication
scope, with redirects/cookies disabled and shared transport; no privileged
fallback, transport substitution or anonymous access. Query activation/native
reader-bf33ca4c6cbe48c88ed71aaea4e53371 passes1/1, zero skips, TRX SHA256
`80445e75fad11462410fd25e9c77986b0e0561d4fb22dd00be27a9e71ef6f651`.
Six runner controls pass, including no repeated/pending reader provisioning.
Official contract: https://surrealdb.com/docs/learn/security/best-practices/troubleshooting
SDK source: https://github.com/surrealdb/surrealdb.net/blob/39f4910dc05372c3172d6db804f8eb89c83f5e7a/SurrealDb.Net/Internals/SurrealDbEngine.Http.cs

Separate Windows consumer uses restricted reader, same retained three records,
admitted Mongo27029 and UI5140. First attempt48268c50ef7f4262975af98c2301eb84
failed before Mongo saves because testhost current directory is its output
folder; fixed explicit repository/source admission, preserving original failure.
Interactive-0d97207b677544d48e1dd766eb3bd986 passes manager/LINQ/HTTP1/1,
zero skips, TRX SHA256
`07dc95d5b6d4c95b5cd9ff635f091a0226a6cf2fa7a603691ccb73b9fde089e9`.
Actual browser full5/subset4, both providers/MovingProviderReads verified.
Native baseline hash before/after matches; exact three-record Mongo fixture
verified/dropped, pending/ready removed. Both owned services Ctrl+C stopped,
SurrealDB0, Mongo12/storageClosed/shutdownComplete;18001/27029/5140 closed.
Latest consumer build0errors/8warnings. New reusable guide and Windows launcher
are prepared for WEB4 local catalogue publication. Historical Linux proof is
preserved separately. Counts101local/60hostedHTTP/28hostedbrowser unchanged.

Final provider controls after the HTTP scope fix pass11/11, zero skips;
scoped-auth-controls/controls.trx and log retained.

### Live local catalogue publication
Websitef90b9a459063bc9a1b45b8a07014647af6730792/Vercel success publishes card46.
Live220953-byte HTML SHA256
`eb92c88a5353315d77126c54c289222ff1df7ad54b72685f2b0ab55e869a61c5`
is Git-blob exact. Guide and Windows fixture links match scoped parent
3fb4991d9fc496209a3592882ee1139a33ea46c1. Actual public card and expanded
launcher browser verified; target link values ellipsized in AX, covered by
exact HTML plus actual native5140 browser proof.113client tests pass.
publication.json retained. No new profile count or hosted enrollment claim.
Combined terminal website edit was automatic-review rejected without specific
reason; no command ran. A focused apply_patch completed the edit and independent
tests/commit/publication checks passed. No owner action required.

## WEB4 hosted enrollment preparation

The read profile uses CQL_SURREALDB_QUERY_ORIGIN, USERNAME, PASSWORD,
NAMESPACE, DATABASE and PROBE_ID with the same prefix. It requires an
existing owned HTTPS origin and an account with the database VIEWER role.
The native SDK applies explicit database authentication headers through the
WEB4 guarded HTTPS transport.

SurrealDbHostedCqlNative uses the shipping WEB4 registration. Set
COSMIC_SURREALDB_HOSTED_CQL_SOURCE to a descriptor containing Actor, three
LiveIds, one DeletedId and one MissingId. Use a probe from the live IDs and
keep credentials in the process environment. The fixture checks public
ownership, full and subset reads, paging, excluded records and unchanged
read projections. Nine focused WEB4 tests passed at Core `315ca55`, including
database headers on the actual SDK transport, configuration and four preset
plans. TRX SHA-256
`cd9268a128933e43e9cd72265e0c8bfaddff0016cbe2f5c4bd45761850aafb2e`.
The HTTPS fixture compiled with 147 warnings and zero errors. Native
HTTPS execution remains pending an owned source and VIEWER credentials.
The retained local source is preserved.

11 November UTC: standard SurrealDB CI ran eight cases. Three fixture-dependent tests failed at admission because the job lacked the owned Mongo root, Mongo replica connection, and retained SurrealDB source. Five generic storage cases skipped, so the job provides no generic storage acceptance. The CI fixture work must qualify both the generic path and the native restart/federation path. [SurrealDB Development job](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43028699870/job/130549847896)

11 November UTC: the standard SurrealDB matrix started a service but supplied no constructor environment settings. Candidate 4825f82b2c wires the local endpoint and synthetic credentials into the test factory. Diagnostic 2a0fd6ea48 runs the five generic cases. Require actual passes with zero skips before integrating. Native restart/federation and hosted CQL remain open.

11 November UTC: published isolated SurrealKV restart CI at `01c0c85118`. The workflow uses the existing checksum-pinned SurrealDB runtime installer and restart runner, with CI's .NET SDK and a disposable report directory. It exercises the same store before and after shutdown. MongoDB federation and hosted CQL remain open.

11 November UTC: the configured generic SurrealDB job exited green with all five tests skipped. The fixture setup had suppressed activation errors. Configured runs now report activation failures directly, while unconfigured runs still skip. Diagnostic ba80622d8d checks activation against the same CI service. [First configured job](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43037408709)

11 November UTC: the initial SurrealKV restart job stopped before downloading the runtime because the installer expected its parent directory to exist. The corrected CI workflow provisions that directory before invoking the unchanged checksum-pinned installer. Diagnostic 182d68ed9e is published. [Initial run](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43037652650)

11 November UTC: the strict SurrealDB diagnostic exposed unsuccessful SDK responses during activation for all five generic tests. The existing native restart test explicitly provisions the namespace and database before activation. Diagnostic `0d772e7bb8` applies that setup to the blank CI container and requires native SQL acknowledgement. [Activation run](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43037918392)

11 November UTC: the corrected SurrealKV runner reached native tests, then looked for the first TRX in the retained local report directory rather than the CI results directory. Diagnostic e2e9286cd7 uses the selected results directory for both creation and verification. [Earlier run](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43038124700)

11 November UTC: explicit namespace and database provisioning unblocked all five SurrealDB generic tests from activation. Two passed and three failed with the SDK CBOR decoder expecting a map. The failures occur at GUID avatar load, GUID holon load and avatar soft-delete, which share the single-record `Select<T>(StringRecordId)` path. Username and collection reads passed. Determine whether the pinned native runtime reproduces the point-read response before changing the shipping adapter. [Schema diagnostic](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43038350413)

11 November UTC: pinned SurrealDB 3.3.5 passed the native SurrealKV restart CI. A separate generic diagnostic `b1102fe57c` runs the same five tests and schema setup against the official v3.3.5 image to check whether the point-read failures reproduce on the qualified version. [Native restart CI](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43038591878)

11 November UTC: Development `2df4de082f` contains the verified OpenSearch restart/federation and SurrealDB restart workflows and fixture scripts. Both are manually dispatched. SurrealDB generic storage and federation remain open, as does hosted HTTPS CQL for both providers. [OpenSearch CI](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43037028447) · [SurrealDB restart CI](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43038591878)

11 November UTC: the pinned SurrealDB 3.3.5 generic run still failed three GUID operations, now with record-not-found errors. Writes and the verified CQL path construct typed IDs from separate table and GUID values. The failing point operations used interpolated record strings. Candidate `8f9deb611f` aligns all five point operations with the existing typed-key path. Diagnostic `b8a9e4620a` is published for verification. [Pinned generic CI](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43038986917)

11 November UTC: all five generic SurrealDB storage tests passed with zero skips on 3.3.5 after typed GUID record IDs replaced interpolated keys. The run directly verifies avatar and holon point loads and avatar soft-delete. Avatar-detail point reads and hard deletion changed to the same typed-key representation and still need direct checks. The fix is on the COSMIC branch pending Development integration. [SurrealDB typed-ID CI](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43039509964)

11 November UTC: added native SurrealDB tests for GUID avatar-detail round-trip and avatar hard deletion with absence by GUID and username. Diagnostic `9936be0782` runs those cases with the previous five on 3.3.5. Development integration awaits the result.

11 November UTC: the expanded seven-case generic SurrealDB diagnostic is active. The earlier 3.3.5 baseline and typed-ID companion runs have both completed, and their temporary tags were retired. [Expanded CI](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43040174105)

11 November UTC: the expanded SurrealDB suite passed six cases including hard deletion. Avatar-detail point read reached its native row and failed decoding the stored `IAvatarChakras` field. Candidate `314e936c05` uses the shared stored-record deserializer for all three avatar-detail read paths. The stored representation is preserved. Diagnostic `14aff5293f` is pending. [Expanded CI](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43040174105)
