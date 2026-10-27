# Azure Blob shared storage baseline

Updated 2026-10-27. **Partial: native2/52**,95 controlled tests passed, no skips. This is not a provider completion claim.

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
