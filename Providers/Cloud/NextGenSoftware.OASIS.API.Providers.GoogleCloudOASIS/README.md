# NextGen Software WEB4 OASIS API Google Cloud OASIS Provider

GoogleCloudOASIS uses the Google Cloud SDKs. Canonical OASIS avatar/detail/holon persistence uses Firestore; optional Cloud Storage and BigQuery clients are separate capabilities, not covered by Firestore-only evidence.

Username/email exports resolve the current avatar identity; `version` selects exported holon roots, not the avatar's independent history. Use the current username/email even for older holons. Linked children keep independent versions. Explicit versioned avatar lookups still support historical identity fields. Negative export versions fail before identity I/O.

## Current direct verification

Single-holon hard deletes also validate the SDK acknowledgement timestamp before publishing deletion success. Invalid acknowledgement retains reconciliation-required evidence even when independent inspection shows the original was deleted; do not replay it blindly. Existing server precondition conflicts remain known rejections. Transactional avatar deletion acknowledgement and durable recovery still require separate verification.

Avatar export roots are selected by CreatedByAvatarId, not merely ParentHolonId. Native regressions cover ID, username and email routes synchronously and asynchronously. Existing descendant projection is unchanged; this does not establish an authorization policy for graph descendants.

Use the retained local runner `Scripts/TestHosts/Run-ReusableGoogleTests.ps1`. It requires the already-installed Firestore emulator, reuses cached dependencies/fixed outputs, takes an exclusive environment lease and stops its owned Java process afterward. No implicit installation or production credentials. Test results are scoped to native Firestore SDK calls, not hosted cloud authorization, charges or complete provider/interface certification.

Trusted application subclasses are projected into canonical Core DTOs before saving, including selected recursive graph nodes through the shared planner. Stored JSON continues to use the restricted type binder; consumer-only CLR type names are not a persistence contract. Avatar-detail provider keys are stored and published only after acknowledgement. Arbitrary explicit-interface/proxy implementations and extra application-specific fields are not certified by these subclass tests.

Historical reads currently reject positive versions. General concurrency, acknowledgement/recovery, all fields/options/interfaces, Cloud Storage/BigQuery and hosted/API/release gates remain open. Activation can perform service-side health/setup operations; Firestore tests disable Cloud Storage/BigQuery and use the loopback emulator. Current evidence and limitations: `Docs/WIP/HyperDrive-Providers/HYPERDRIVE_V2_CURRENT_RELEASE_GATES.md`.

Single-record saves now require a nonnull SDK WriteResult with a nondefault native server UpdateTime before publishing caller IDs/keys. This validates the acknowledgement, not application CreatedDate metadata. Missing/uninitialized acknowledgements retain the attempted document path and reconciliation marker; no server transaction ID is invented. Inspect original state before retrying. The check adds no extra native requests and does not provide durable recovery or certify every mutation path.

Initial Release targeting OASIS API v2.3.1 (Current LIVE REST version): \
https://api.oasisplatform.world

Full documentation and source code can be found here: \
https://github.com/NextGenSoftwareUK/OASIS

Make sure you check out the Overview and Benefits Of Building On The WEB 4 OASIS API sections above! :) Thank you!


## Anonymous public Cloud Storage metadata CQL

`GoogleCloudPublicMetadataSource` is a separate read-only adapter over the existing CQL interfaces. Its public constructor always creates the pinned SDK's anonymous client. It reads object metadata, with SDK retries disabled and no billing-project setting; it never downloads imagery or creates/uploads/deletes cloud objects. Canonical Firestore persistence remains a separate capability.

Configure one to eight distinct printable-ASCII keys (maximum128characters each), a bounded general-purpose bucket name and a stable nonempty source-owner GUID. Retain that GUID with the catalogue: it participates in the deterministic native-to-holon ID. The current native sample uses three official public Landsat objects and needs no Google account, emulator or new runtime.

```csharp
// Read these values from the retained source configuration.
// Changing sourceOwner would change the mapped holon identities.
using var source = new GoogleCloudPublicMetadataSource(
    bucketName,
    sourceKeys,
    sourceOwner);

var activated = await source.ActivateQuerySourceAsync(cancellationToken);

if (activated.IsError)
    throw new InvalidOperationException(activated.Message, activated.Exception);

// Select several native objects by their metadata key.
// Use sourceKeys.Take(2).ToArray() to show the smaller subset.
var selectedKeys = sourceKeys;

var query = CosmicQuery<Holon>
    .Create(CosmicQueryScope.Named(ProviderType.GoogleCloudOASIS).Result)
    .Where(holon => selectedKeys.Contains(
        CosmicQueryMetadata.Value<string>(
            holon,
            GoogleCloudPublicMetadataSource.ObjectKeyMetadata)))
    .Result;

// The common planner checks the adapter's actual capabilities before I/O.
var planned = CosmicQueryPlanner.Plan(
    query,
    ProviderType.GoogleCloudOASIS,
    new[] { new CosmicQueryTarget(ProviderType.GoogleCloudOASIS, source.QueryCapabilities) });

if (planned.IsError)
    throw new InvalidOperationException(planned.Message);

var rows = await source.ExecuteCosmicQueryAsync(
    planned.Result,
    cancellationToken: cancellationToken);

if (rows.IsError)
    throw new InvalidOperationException(rows.Message, rows.Exception);
```

Supported source filters are `GCSObjectKey` equality and bounded membership; paging follows complete selection validation. General Name/Description filters, ordering, aggregate or arbitrary bucket-scan queries are rejected at planning. Output includes name/description and native bucket/key/ETag/generation/size/checksum metadata. Size, generation and metageneration are decimal strings to preserve native integer precision in WEB4/JavaScript. A failed/invalid selected response publishes no partial page.

Verification on31October2026:17controlled cases and1real anonymous SDK/CQL case (three records/two-record subset), no skips. This adapter is not yet registered in the hosted WEB4 sandbox, tariff or Mongo pairing. Do not describe it as hosted CQL/Firestore parity. Official [public-data access](https://docs.cloud.google.com/storage/docs/access-public-data) and [public datasets](https://docs.cloud.google.com/storage/docs/public-datasets).
