# Azure Blob CQL MVP

Updated 29 October 2026. **Adapter controls verified; native and paired demo pending.**
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

## Exact next step

1. Reuse cached Azurite3.37.0 and Node. Existing reusable host helpers support
   graceful IPC shutdown; keep persistent source data/catalogue in LocalState and
   logs/TRX in worktree TestResults. No runtime install or Azure subscription needed.
2. Provision a journaled, task-owned three-record canonical source with distinct
   containers through the actual shipping factory. Prove native3/2 selection,
   absent key behavior and unchanged contents/ETags; retain it for restart reuse.
3. Add the Mongo-paired C#/HTTP/browser five/four-result preset and launcher route;
   validate/drop only its exact owned temporary Mongo database afterward.

Do not green the [MVP inventory](CQL_ALL_PROVIDER_MVP_INVENTORY.md) until those native
and paired proofs pass. Free-only provisioning and existing historical/provider
gates remain in force. [Resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md) /
[current status](CURRENT_STATUS.md).
