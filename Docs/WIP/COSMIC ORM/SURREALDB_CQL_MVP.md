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
