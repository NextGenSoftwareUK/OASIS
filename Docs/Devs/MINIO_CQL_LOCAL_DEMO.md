# MinIO and MongoDB local CQL demo

This Windows teaching demo selects three retained MinIO Holons and two temporary
MongoDB copies through HolonManager and the shared C# CQL walkthrough. The full
query returns five physical rows; the two-ID subset returns four. Each row shows
its provider. MovingProviderReads is explicit; there is no distributed snapshot.

## Prepared environment

One official pinned MinIO RELEASE.2025-09-07T16-13-09Z Windows binary lives in
`LocalState/COSMIC/RecoveryLaptopMinIO20250907`. Its exact size, vendor SHA256,
version and source are recorded in runtime.json. It runs its own persistent
MinIO store on loopback19700; browser console is disabled. No Go, Java, Linux or
system service install is needed. This is a real MinIO server through AWSSDK.S3,
separate from the existing AWSS3OASIS profile or a generic compatible substitute.

The source uses a generated local teaching credential. Credentials stay on this
computer and are never forwarded to the browser. Hosted WEB4 bearer tokens and
cookies are never passed to MinIO. Hosted TLS and restricted credentials remain
separate gates. The owned bucket `cosmic-cql-minio-recovery` contains three live
canonical JSON objects and one deleted control. An embedded child is excluded
from the root query projection; its complete bytes still count toward limits.
One missing-key control stays absent. Objects use plaintext canonical JSON, not
application-level encryption. No MinIO save or reseed occurs during demos.

The original objects were conditionally created once. Source identity, credential
admission, per-object SHA256 and native ETags are checked against the original
receipts. Missing or altered state stops the launcher. Unknown writes must be
reconciled before proceeding; initialization cannot overwrite an existing source.

Mongo reuses the admitted recovery MongoDB8.2 runtime/store and replica set27029.
The fixed temporary database `cosmic_cql_mongo_minio_demo` must be absent before
its three journalled saves. Two copies are selected; the third proves exclusion.

## Build and start

From the matching task checkout, build only when source or outputs changed:

```powershell
& Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1 -Owner cosmic-minio-demo-build -Operation {
    dotnet build Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.MinIOOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.MinIOOASIS.IntegrationTests.csproj /m:2 /nr:false /p:UseSharedCompilation=false
    if ($LASTEXITCODE) { throw 'MinIO demo build failed.' }
}
```

Start the interactive demo:

```powershell
pwsh -File Scripts/TestHosts/Run-CosmicMinioMongoDemo.ps1 -Serve
```

The launcher holds one provider build lease, refuses occupied ports and unfinished
fixtures, and starts only its own processes. Wait for demo-ready.json in the cache,
then open [the local query page](http://127.0.0.1:5142/). Choose **Query all records**,
then **Query two-record subset**. Inspect counts, IDs and provider provenance.
Caller-supplied actors and unknown presets are rejected. Native failure returns
an explicit error without partial rows. Only NoSuchKey404 is an absent object;
missing buckets, permission failures and inconsistent responses are errors.

## Stop and retain

Use **Stop demo (keep original MinIO source)** and wait for the runner to finish.
It verifies and removes only its exact temporary Mongo fixture, checks absence,
then checks the original native hashes and ETags. Both owned processes receive
Ctrl+C through their exact hidden consoles. MinIO must acknowledge INTERRUPT
and exit0; Mongo must close storage and complete its native shutdown. Unrelated
console members and processes are refused. Closing the tab alone leaves it active.

Retain the original native store, credentials, intents and acknowledgements.
Native restart proof does not certify crash durability, full storage writes,
version history, encryption, replication, full EF/NHibernate parity or hosted
enrollment. Exact acceptance evidence and open gates are in
[the scope record](../WIP/COSMIC%20ORM/MINIO_CQL_MVP.md).
