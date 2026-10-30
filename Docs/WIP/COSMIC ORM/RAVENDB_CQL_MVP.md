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
