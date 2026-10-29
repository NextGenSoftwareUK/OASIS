# Azure Blob CQL MVP

Updated 29 October 2026. **Controls and native/restart query verified; paired demo pending.**
This is separate from Azure Cosmos DB; hosted Cosmos remains pending by user instruction.

## Implemented profile

AzureStorageOASIS now implements the common CQL storage interface. Captured ID
equality/membership, AND/OR/constants and paging select at most100 canonical GUID
blob names intersected with a trusted server-owned catalogue. Load/save/query share
one canonical GUID-key builder; the stored name format is unchanged. Arbitrary
custom-key discovery, metadata/order predicates and child expansion are outside
this profile and must not be inferred from these controls.

The adapter uses the official Azure SDK to stream only selected blobs, then the
existing canonical/history reader for current records. Declared and actual bytes
are bounded to4MiB before publication; invalid UTF-8/identity/access or SDK errors
fail the entire page. Deleted records are hidden. Only explicit BlobNotFound means
absence; missing containers/permissions/transport failures remain errors.

Caller cancellation reaches the SDK stream and returns a caller-token cancellation
result rather than an SDK transport timeout. The provider's existing SDK-only
deadline classification remains intact. Reads can observe different moments;
no cross-blob or distributed snapshot is promised. This path reads canonical JSON;
application-level record encryption is not asserted for the Blob MVP.

## Evidence

Producer/consumer builds pass; **14/14 controls, zero skips**:

- Seven compiler/adapter cases cover immutable catalogue intersection, wrong
  authority, unsupported shapes/bounds, inactivity and pre-cancellation.
- Six official-SDK controlled HTTP cases cover selected canonical keys, record
  decode, absence, container failure, identity mismatch, deleted visibility and
  declared byte-budget rejection before body consumption (some cases cover more
  than one boundary).
- One live SDK request is cancelled after reaching its owned listener; no partial
  rows or provider-outage classification result.

Report: `TestResults/AzureBlobCqlCurrent/azureblob-cql-controls.trx`, SHA256
`304B38CFC3350D45E683064587E00BAE805804F4EC1012C2CF7D3C20DACB736A`.
Controlled listener ports/modes are recorded in TRX stdout and awaited/stopped per
case. These are signed SDK-pipeline controls, not native Azurite acceptance.

Native acceptance now additionally passes **1/1** on retained Azurite3.37.0:
three saved records filter to two; exact native blob contents and ETags remain
unchanged. Full graceful stop/start reuse passes **1/1, zero skips, 257ms**, with
an actual authorized missing-blob read returning an empty result. The actual
shipping CloudProviderFactory constructs the provider with version control enabled.

Native report: `TestResults/AzureBlobCqlCurrent/azureblob-cql-native.trx`, SHA256
`6F4F8E9EE48973624B4DAFB4F6B61FCDA101FF15F0185017E1425B888DEFB138`.
Original first-pass report is preserved as `azureblob-cql-native.initial.trx`.
Persistent source: `LocalState/COSMIC/AzureBlobCql/demo-seed.json`173bytes, SHA256
`9758C6A96B95BC00B2B3A4E8C8A800E61AED320ED943CBE0B8114DAB14C6E23A`.
The complete retained store/catalogue/lock measured15,284bytes. Port10000 is closed
after verified graceful IPC shutdown; setup journal is removed. No subscription,
runtime install or new dependency download was needed.

## Paired demo acceptance

`AzureBlobMongoCqlNativeTests.NativeAzureBlobMongoFederationRunsSdkAndHttpWithProviderProvenance`
passes 1/1, zero skips. SDK and HTTP return five/full and four/subset rows,
exclude unrelated Mongo identities, preserve provider copies and reject forged
actors, unknown presets and inactive Blob authority without partial rows.
The browser independently verified both counts and provenance at localhost5106.

Evidence under `TestResults/AzureBlobCqlCurrent/`:
- `azureblob-mongo-federation.trx`: 1/1; SHA256
  `05332369A4BA0CF715F09D68A60AB906751F920A16203C2A7BA33E524F868537`.
- `azureblob-mongo-browser.jpg`: verified four-row subset screenshot.
- `hub-routing.log`: 8 single/16 plans,17 paired/34 plans,20 paired rejections,
  3 archive rejections; listeners unchanged.

After requested shutdown the same owned runner completed with exit0. Exact owned
Mongo database `cosmic_cql_mongo_blob_demo` was validated/dropped; pending and ready
journals are gone; ports10000/27029/5106 have no listeners. Persistent Blob source
is retained. No hosted Azure or distributed snapshot guarantee is claimed.

```powershell
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider AzureBlob -Federated
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider AzureBlob -Federated -VerifyOnly
```

Continue other red provider MVPs. [Inventory](CQL_ALL_PROVIDER_MVP_INVENTORY.md) /
[current status](CURRENT_STATUS.md) / [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
