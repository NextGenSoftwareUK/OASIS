# RavenDB CQL MVP

Updated30October2026. Native/restart/Mongo-paired/browser scoped MVP verified.
Full EF/NHibernate, graph/history/CAS and replication parity remain open.

## Demo commands

```powershell
# C# / HTTP / browser teaching preset over original secured RavenDB + Mongo.
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider RavenDB -Federated

# Bounded native/restart/C#/HTTP/post-pair checks and owned cleanup.
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider RavenDB -Federated -VerifyOnly
```

Browser5135shows5physical rows (RavenDB3 + Mongo2) or4for a two-ID subset (2+2).
Each physical copy retains its provider provenance. Unrelated Mongo data is excluded.
The shared C# walkthrough and HTTP host run through HolonManager V2 with explicit
provider scope and separate trusted record catalogues. Shutdown preserves original
RavenDB records, verifies/drops only the owned temporary Mongo fixture and stops
both owned services. This demo does not exercise HyperDrive replication.

## Query and save scope

CQL binds shared bounded identity algebra to RavenDB authority: complete trusted
catalogue at most100GUIDs, identity equality/membership, Boolean composition and
paging. Native SDK multi-load uses fresh NoTracking/NoCaching session; no table
scan or managed content filter. Native/decoded identities and read authority are
validated, missing/deleted records skipped, children excluded. Decoded JSON4MiB
and result budgets fail without partial rows; SDK buffering precedes byte checks.
Independent provider reads have no distributed snapshot guarantee. Plaintext JSON
envelopes; arbitrary property predicates, sorting and graph queries reject.

Activation verifies configured native DB statistics before publishing active state.
Failed store initialization disposes its candidate; successful stores publish under
a lock. Deactivation clears active state; in-flight deactivation races unverified.
SaveHolon stages runtime-typed JSON/ID/copied keys, waits for native save and a
nonempty change vector, then publishes caller ID/key and IsSaved/SavedCount.
RavenDB.ChangeVector is returned in result metadata. Manager owns clean state.
Batch stop/continue/error/saved counts are explicit. This is acknowledgement
ownership, not expected-revision CAS; native no-op updates may not write a revision.
Avatar caller ownership and broader provider correctness remain uncertified.

## Certificate authentication

Original two-argument constructor remains compatible. The three-argument overload
takes a caller-owned X509Certificate2 with private key and requires HTTPS URLs.
Store disposal leaves caller credentials alive. No validation override is installed.

```csharp
// Load credentials from private configuration; keep them alive during provider use.
using var certificate = X509CertificateLoader.LoadPkcs12FromFile(
    clientPfxPath,
    clientPfxPassword);

var provider = new RavenDBOASIS(
    new[] { "https://your-ravendb-host" },
    "oasis",
    certificate);

var activated = await provider.ActivateProviderAsync();
// Check activated.IsError before executing prepared CQL plans.
```

Native fixture uses retained private CA/client PFX, process-only SSL_CERT_FILE and
normal strict TLS validation. No OS trust-store modification or certificate bypass.

## Verified acceptance / evidence

23unit controls pass (metadata/compiler/lifecycle/save failures/certificate boundary).
Original SDK native1+actual process-restart1pass: same actor/three IDs, native3,
ID-membership2, page1; original complete payload hashes/native change vectors match.
Each save has a durable revision acknowledgement; no reseed/reset/replay occurred.
`RavenDbNativeCqlTests.cs` and `RavenDbMongoCqlNativeTests.cs` are in the provider's
IntegrationTests project. The latter verifies C#/HTTP5/4, forged actor/unknown preset
400and inactive-provider503without partial rows. Browser verified both visible
responses/provenance and retained-source shutdown. Screenshot capture unavailable;
no screenshot proof claimed. Bounded and interactive5phases each1pass.

Evidence in TestResults/RavenDbCqlCurrent (raw reports remain outside Docs):

| Report | Passed | SHA256 |
|---|---:|---|
| controls.trx |23|771bfd6fa38329198d6c53f9e51cfa198a24c0cd6908a4d0ec9135aeb82b90d0|
| mongo-federation-bounded.trx |1|8f9aa666861cb7d96adeeb3a6a0456d6c13630ee7b94853802d9cdc59b2e2956|
| mongo-federation.trx |1|43f947bf722f0a73f0d5ed457c5df8f6b5258b0f2911dfad067050f04116da01|
| native-postpair.trx |1|139171d19589b5090c6f993748248b1a08cb589786c698a73c1dfd51a5ce4a4a|

Final browser-run.log contains all five phase hashes. Original native-run.log
retains initial admission hashes; refreshed TRX files refer to latest run.
Hub routing:8single/16plans,44paired/88plans,74paired and3archive rejections;
listeners unchanged. Last source101,211,410B; native engine housekeeping changes
physical size while original logical hashes remain fixed. Owned services stopped0,
Mongo fixture removed, pending/ready cleared. Runtime/image/keys/source retained.

## Hosting and unresolved gates

One retained local server7.2.7(image digest
sha256:92bd726e9d0031f48c8203c9be660c9f9b361ff11b6b352a17ca4a861e366471),
SDK7.0.1, original `/var/tmp/oasis-ravendb-cql`, TLSloopback18003,2CPU/2GiB.
No hosted account or paid license. Original setup CA usage correction and native
DBcreate500/read-back acknowledgement are documented in
[admission history](Archive/RAVENDB_ADMISSION_HISTORY_2026-10-30.md). Original
creation response500cause remains undiagnosed; no general provisioning reliability
claim. It did not recur in the verified SDK saves/CQL/paired operations.

Full CAS, graph/maxChildDepth, metadata/content depth, history, migration/cutover,
central registration, replica semantics and HyperDrive replication remain red.
Legacy HyperDrive is documentation-only. Stop deepening this green demo scope
unless a reproducible bug is found; continue breadth with CockroachDB.

[Current status](CURRENT_STATUS.md) / [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md)
/ [official SDK](https://www.nuget.org/packages/RavenDB.Client/7.0.1).
