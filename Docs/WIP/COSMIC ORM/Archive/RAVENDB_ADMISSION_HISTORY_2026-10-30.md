> Superseded by [current RavenDB MVP guide](../RAVENDB_CQL_MVP.md); retained admission history.

# RavenDB CQL MVP — controls only

Updated30October2026. Native/restart/paired/browser acceptance remains red.

## Implemented scope

Pinned RavenDB.Client7.0.1 provider builds. A new provider-specific adapter binds
shared bounded identity algebra to exact RavenDB authority: complete trusted
catalogue at most100GUIDs, identity equality/membership, Boolean composition and
paging. One native SDK multi-load of OasisHolons/{guid} uses a fresh NoTracking,
NoCaching session; no index/table scan or managed content predicate. Unexpected
native keys, mismatched envelope/decoded identity and read authority reject.
Missing/deleted rows skip, children excluded, paging applies after selection.

Decoded JSON4MiB/result budgets and cancellation publish no partial rows. SDK
buffering precedes decoded-byte accounting; this is not a wire-allocation bound.
Independent provider reads have no distributed snapshot guarantee. Arbitrary
content/sorting/graph/deeper LINQ semantics are not admitted. Plaintext envelope
scope; native SDK conflict resolution/revisions are not yet certified.

## Root fixes

Activation previously initialized only a local SDK store without verifying the
configured database, then returned success without setting active state. It now
checks native database statistics and publishes activation only after success;
failed reactivation clears stale state, deactivation clears it. Failed SDK store
initialization disposes the candidate; only a successfully initialized store is
published under a lock. In-flight deactivation/session races remain unverified.

SaveHolon previously assigned caller ID/provider keys before network acknowledgement.
It now stages concrete runtime JSON/ID/copied keys, saves the tracked native
envelope, and requires its canonical ID/nonempty SDK change vector before caller
identity/key and result IsSaved/SavedCount publish. RavenDB.ChangeVector is returned
in result metadata. Existing document updates use the session-tracked instance.
Batch errors/continuation/counts now reflect acknowledgements. Manager owns clean
state/revision publication. This does not implement expected-revision CAS; identical
native writes may remain no-ops and broader history/replication is unverified.

## Verified tests and exact evidence

19controls pass:5existing metadata,10CQL/lifecycle,4failed-save/batch tests.
The provider unit project contains RavenDbCqlTests and RavenDbSaveOwnershipTests.
They verify authority/catalogue capture, unsupported shapes, inactive/pre-cancel,
invalid budgets/missing plan, deactivation/failed activation and failed new/existing
saves preserving original caller IDs/keys. Batch stop/continue checks exact attempts.
Invalid endpoint tests intentionally fail SDK setup without a live service; they
do not prove successful database authentication or malformed native receipts.

Evidence `TestResults/RavenDbCqlCurrent/controls.trx`,19passed/0failed/0skipped,
SHA256 `3be3ba661beccce28aa58b4724c393cc7c263c961a3dc2d55f23914a7e3259b1`.
Provider/test bounded builds pass, existing package-pruning warnings remain.
No native environment or business records created in this step. Shared NuGet cache
and .NET10reused, pinned SDK restored once; see resource ledger for sizes.

## Exact next step / remaining gates

Inspect existing RavenDB server/runtime/image caches, then set up one retained
native source if absent. Use the implemented caller-owned client certificate constructor, with normal
HTTPS certificate validation. Verify original3/filter2/page1, native change-vector
and payload stability across actual process restart, then Mongo-paired C#/HTTP/
browser5/4 and hub enrollment. Never mark inventory green from control tests.

Full CAS, graph/maxChildDepth, history/content/metadata query parity, central
registration and HyperDrive replication remain red. Avatar caller-state ownership
is not fixed/certified here. Legacy HyperDrive remains documentation-only.

[Official loading guide](https://ravendb.net/docs/article-page/7.0/csharp/client-api/session/loading-entities)
/ [pinned SDK](https://www.nuget.org/packages/RavenDB.Client/7.0.1)
/ [current status](CURRENT_STATUS.md) / [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).

## Client-certificate configuration verified — 30 October 2026

Native setup audit found no reusable Windows command, WSL runtime or rootful
Podman image. No server image/environment has been downloaded/created yet.
Official secured deployments use X.509 client certificates; the old provider
constructor could not pass them to its DocumentStore. A new three-argument
overload now takes a caller-owned X509Certificate2. The original two-argument
constructor is retained and delegates, preserving existing callers/signature.

```csharp
// Load credentials from your secure configuration; never log the PFX/password.
// The caller owns certificate lifetime and keeps it alive for provider operations.
using var clientCertificate = X509CertificateLoader.LoadPkcs12FromFile(
    clientPfxPath,
    clientPfxPassword);

var provider = new RavenDBOASIS(
    new[] { "https://your-ravendb-host" },
    "oasis",
    clientCertificate);

var activated = await provider.ActivateProviderAsync();
// Check activated.IsError before preparing native CQL plans.
```

Certificate configuration requires a private key and explicit HTTPS URLs. The
provider sets DocumentStore.Certificate and DisposeCertificate=false, keeping
caller credentials alive across deactivation/store recreation. It installs no
server-certificate validation override. Successful live TLS/authentication remains
a separate pending gate; these tests do not claim a verified handshake.

23controls now pass (the earlier19result is historical): previous19plus4certificate
cases for HTTP/invalid endpoints, public-only certificates and store recreation/
caller credential lifetime. Ephemeral test keys live in memory and are disposed;
no PFX/private keys or credentials committed or persisted by the unit tests.
Current evidence TestResults/RavenDbCqlCurrent/controls.trx,23passed/0failed/0skipped,
SHA256 `771bfd6fa38329198d6c53f9e51cfa198a24c0cd6908a4d0ec9135aeb82b90d0`.

Next: one retained secured native server, actual TLS/read/write/restart proof,
then Mongo-paired browser preset. Free-only/local scope, no paid subscription.
[Official certificate setup](https://ravendb.net/docs/article-page/7.0/All/start/containers/requirements/security).

## Secured native environment and empty-database restart verified

Official server7.2.7(build72051) image is pinned to
`sha256:92bd726e9d0031f48c8203c9be660c9f9b361ff11b6b352a17ca4a861e366471`,
image ID933a130ff10d66a6a28b8829c5f6f7af886162fc2425b4012a9f41649072816e,
711,973,673B cached once. Container oasis-ravendb-cql, loopback18003->8080,
2CPU/2GiB/256PIDs, retains source/config/credentials at
`/var/tmp/oasis-ravendb-cql`. No hosted account/product license key; local default
AGPL mode. Native server bootstrap certificate is also the isolated fixture admin.
No public access, OS trust-store change or certificate-validation bypass.

Initial generated CA lacked required key usage; strict OpenSSL/Python validation
caught it. Original CA key/dates/serial and leaf/private material were preserved;
CA certificate was completed with critical signing usage. Original invalid public
certificate retained as ca-initial-invalid.pem, correction receipt retained.
The initial receipt writer's chmod call failed after certificate correction; the
writer was fixed to fchmod, then its empty receipt completed after strict-chain/
same-key validation. Setup readiness now fails immediately on permanent TLS errors,
with a60second deadline. No keys/certificates regenerated or source reset.

A mistaken database-list endpoint was corrected from admin record lookup to SDK's
documented names-only endpoint. The original database PUT returned500but native
read-back showed oasis had been created. Its pending operation was preserved;
read-only reconciliation verified the exact original singleton database definition
(index3, memberA, replication1, empty settings) and0documents/etag0across restart.
A distinct NativeReadBackAfterHttp500ack records reconciliation without repeating
the request. The500response cause itself is not yet diagnosed; do not claim an
ordinary successful create response or general provisioning reliability.

Strict TLS/client-certificate reads pass; anonymous native administration rejected.
Database IDQ/LSgIUB6EKT6D8SF5o+fA, server IDcb84da73-34a3-4ee0-8c9f-1ee175c5be08,
topology01cde4ae-a3c2-4a05-b451-470f4a5ea184remain stable. Environment/database
pending resolved; owned container stopped0after both final phases. Source84,498,920B
retained, including private CA/leaf keys/PFX/config and original database.

Evidence TestResults/RavenDbCqlCurrent/native-environment-readback.log,
SHA256 `f3ac2f9085a40cd200cb52a5d91167fe4d50a1b5f1eab484226ae24c4388893a`.
This is native TLS/empty-state persistence proof, not CQL/SDK record acceptance.

Exact next step: build the existing integration project and add original journalled
3record SDK saves/native CQL3/filter2/page1/hash/change-vector checks and actual
process restart. Reuse this source/certificate/image only. Set SSL_CERT_FILE to
its ca.pem only for the owned Linux.NETtest child; load client.pfx via the provider
constructor. Investigate the provisioning500if SDK/native routing reproduces it,
preserving all original identity and any unresolved write outcomes. Then paired
C#/HTTP/browser5/4 and hub enrollment; inventory remains red until verified.
