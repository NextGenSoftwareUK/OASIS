# Azure Blob shared storage baseline

Current 2026-10-27: **native25/52 partial;109controlled pass**. See the acknowledgement repair checkpoint below for current gates. This is not a provider completion claim.

## Initial checkpoint (superseded by the repair checkpoint below)

Initial 2026-10-27. **Partial: native2/52**,95 controlled tests passed, no skips.

## Binding and observed gap

The opt-in `AzureStorageConformanceTests` links the canonical `CoreStorageConformance.cs` into the existing MSTest host. It calls the shipping `CloudProviderFactory.CreateAzureStorage` using explicit DNA container settings, real `Azure.Storage.Blobs` clients and the built-in loopback emulator account. No hosted credentials, billable requests or fake service are involved.

Both schedules passed activation and stopped at `avatar-save`: the provider successfully wrote its avatar blob but omitted `SavedCount`. Review also finds omitted acknowledgement counts in other record reads/writes. Next: repair those fields at the successful operation boundary, extend controlled assertions, then reconcile/preserve the two exact synthetic avatar fixtures before another native run. Never discard unknown rows or relabel the95 controlled passes as native acceptance.

The fixture refuses previous reports and pre-existing blob payloads in six fixed conformance containers. Successful runs must prove every container empty through an independent SDK listing. This run failed before cleanup; the12,472-byte persistent store remains intact.

## Runtime observation

Reused `C:/Users/david/.cache/oasis-runtimes/azurite-3.37.0`, Node24.19.0 and existing .NET/NuGet outputs. No download or second environment was created. The scripted hidden startup exceeded its15-second readiness deadline without output; it failed closed and stopped only its owned process. A direct CLI help diagnostic completed, then the same retained runtime/arguments started and bound `127.0.0.1:10000`. The native probe used that verified process and frozen source-verified binaries. That process was stopped after the failed run; no listener is intentionally retained.

The hidden-startup timing difference is not yet explained. `Run-AzureBlobStorageConformance.ps1` is a bounded, fail-closed runner; its complete automatic startup/native path is not proven green. Do not mask the startup issue with an alternative provider or unbounded retries.

## Retained evidence

Under `C:/Source/OASIS/TestResults/HyperDrive-Providers/NativeEvidence/`:

- `azure-blob-controls-current.trx.gz`:95/95 controlled cases.
- `azure-blob-native-current.trx.gz`: two failed native schedules, aggregate2/52.
- `azure-blob-initial-identities.json`: binary/PDB and compressed/raw TRX hashes plus preserved store paths/sizes/hashes. SHA256 `abc03e6698952920d662512f56cdb0b7fec890eabbb77597aab62c7dac8e81fd`.
- `conformance-azurestorageoasis-current.json`: hash/path registered in `CONFORMANCE_RESULTS.json`.

Source verification matched13 Azure provider and15 test-consumer documents, Core794 and Shared56. Tested Core SHA256 `1C57C304779C12D88119197798C53A38F586D39FF40E790A078BCC89811727FE`; Shared `B67E41F406C1ED3347CD6235C0B4F14F74CC5D50766AD5E309D2778AE6F5411C`. No unrelated Core changes are included in this publication.

Fixed workspace `TestResults/AzureCurrent/{conformance-data,environment.lock,azurite.out.log,azurite.err.log,conformance-owned.pid}` retains runtime state and ownership metadata. The PID is historical, not permission to stop a future reused PID. Revalidate process identity every time. Original Azure routing-file guard remains in the runner. Shared old routing outcomes are not reconstructed or claimed resolved.

Compressed reports were decompressed/verified before deleting owned raw TRX files. Retain the persistent store and reports until exact-fixture reconciliation is reviewed; do not rerun the suite over them.

## Acknowledgement repair checkpoint (27 October)

Six successful-write count controls genuinely failed before repair; the six failed-write controls passed. Save/read, filtered collection/export and partial graph counts are now assigned at their successful boundaries. Two parent-query count regressions also failed before their fix. Final **109 controlled cases pass**, no skips.

The original two avatar fixtures were fully preserved, identity-checked from the authoritative downloaded bytes, then removed with their original ETags; exact reconciliation case passed. The same retained hidden runtime startup was rechecked and opened its owned loopback listener in1.4426s; this narrows the earlier startup concern without claiming its original timing cause or a complete automatic runner pass.

Native rerun advanced to **25/52**: async12 checks with an interrupted atomic checkpoint (`report.json.next` exists, original report last intent holon-update); sync13 checks stopped at parent-query collection count. The parent fix was compiled and verified by controls afterward; it has not yet received a new native pass. Both native schedules remain failed. Preserve the current graph fixtures and both async checkpoint files before any further run; do not erase `.next` or assume the interrupted operation was absent. The owned emulator was stopped with its12KB original store plus current added fixtures retained (re-measure before cleanup).

New local proof receipts under the same NativeEvidence path:

- `azure-blob-count-fix-identities.json`: SHA256 `0ebe4033bf0fb304e3781d1ab889e9bdc70875a252635218bce106b941e3d79f`; includes initial fixture reconciliation, red/control/native results and unchanged report/store inventories.
- `azure-blob-count-final-controls-identities.json`: SHA256 `6a327a388b12febfef64286eabd186a12ae8a10823e2386219dc20b958b7506a`; final109controls and two parent red regressions. Its binary cohort is separate from the earlier partial native run.
- `azure-blob-initial-fixture-reconciliation.json` plus two saved avatar payloads: original ETag/hash receipts; no arbitrary namespace sweep or user record removal.

Exact next action: diagnose/preserve the interrupted report publication, then reconcile only the fixed IDs in the two retained current reports and their downloaded ETag-bound native snapshots. All applicable baseline and hosted acceptance gates remain open.
