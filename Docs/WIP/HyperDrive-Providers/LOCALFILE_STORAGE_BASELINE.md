# LocalFile current storage baseline

Updated 2026-10-27. Native `core-storage-v2`: **52/52** across both schedules. All22 selected test cases passed (two native schedules plus20 controlled cases), zero skips. This is a baseline, not every-interface, durability, security or HyperDrive routing certification.

## Root causes repaired

- Successful avatar/detail and batch saves omitted `SavedCount`.
- Successful avatar/detail point reads omitted `LoadedCount`; collections must report their actual selected count.
- Graph point/collection/parent selections and owner/global exports omitted acknowledgement/count fields.
- Permanent avatar deletion removed avatar/detail files but omitted `IsDeleted`; soft deletion now acknowledges only a saved successful result.

Counts are assigned only after the existing file operation succeeds. No fallback storage path, synthetic success, skipped contract or native mock was introduced. The provider continues to use the real .NET file API and existing graph helpers.

## Evidence and provenance

`Scripts/Providers/Run-CoreStorageConformance.ps1` reused existing cached outputs and the build lease. Source/PDB verification matched16 provider source documents and85 consumer documents. Captured dependencies: Core `1C57C304779C12D88119197798C53A38F586D39FF40E790A078BCC89811727FE` (794 source documents), Shared `FD64350A89BED98020694AF5E0E79C63D30A568D356E54EB958EB362F9ACA14D` (56). This identifies the tested cohort; it does not claim that unrelated dirty Core changes were published.

Raw evidence is retained locally under `C:/Source/OASIS/TestResults/HyperDrive-Providers/NativeEvidence/`:

| Evidence | Scope |
|---|---|
| `localfile-v2-acknowledgement-red.trx.gz` and receipt |2/52 native;20 controls passed; save acknowledgement failure reproduced before repair |
| `localfile-v2-delete-ack-red.trx.gz` and receipt |46/52 native; actual avatar/detail deletion completed, acknowledgement absent |
| `localfile-v2-final.trx.gz` | All22 selected cases passed; both native schedules completed |
| `localfile-v2-final-identities.json` | Binary/PDB hashes, report counts, output sizes and fixture inventories; SHA256 `4b675e85b98295266ddd91dc93022cd9ed55e5ad5ced49d0c2f0521e4244ecc3` |
| `conformance-localfile-current.json` | Current aggregate; authoritative hash/path in `CONFORMANCE_RESULTS.json` |

The final receipt hashes each binary and preserved fixture. Compressed TRX bytes were decompressed and verified before owned raw TRX removal. Both current `data` directories contain zero payload files. The v1 and v2 failure payloads/reports remain byte-preserved in the same fixed environment, totaling21,939 bytes including reports/receipts/locks. No unknown records were deleted or replayed.

`retire_localfile_v1_reports.py` admits only the exact registered pair at the two reviewed acknowledgement gates: one known synthetic avatar at save, or an independently empty fixture directory at delete. It preflights both schedules before any move, refuses existing destinations, preserves payload bytes and records hashes. Two controls prove check-only behavior, byte preservation and rejection of an unknown second-mode file before any move.

## Reuse and next step

No runtime download, SDK installation or additional service/environment was required. The existing LocalFile producer and shared consumer bin/obj outputs remain reusable; see the resource ledger. Do not rerun over current reports: inspect and preserve the verified current pair first. The suite removes only its owned successful fixtures.

Continue other family baselines. Generic HyperDrive routing, advanced recovery and broad LocalFile interface certification remain separate gates.
