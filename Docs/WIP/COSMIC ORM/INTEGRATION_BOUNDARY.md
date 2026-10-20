# COSMIC source integration boundary

20 October 2026. Read-only audit of the current shared checkout. No Git/index changes, commit, push or dependency-pin update performed by this audit.

## Evidence and dependency closure

Core HEAD inspected: `6b0b104` (`Reject archived versions in default avatar credential selectors`). Tracking, child-collection/manifest/resolution implementations and related interfaces remain local untracked files. Manager/model/serialization changes are also present. A manifest-only committed Core dependency epoch therefore cannot be named from the current evidence.

Root committed `HolonGraphWritePlan` contains the original positional `PlannedHolonWrite(Source, Snapshot)` record. The working planner additionally exposes selected-child state, captured canonical intent, allocated child IDs and relationship manifests. The working source directly references new Core collection/manifest APIs. This distinction was verified with `git show HEAD:...`, working diffs and dependency searches.

The working planner fields have these concrete consumers:

| Field | Consumers |
| --- | --- |
| `CapturedIntentJson` | `CommittedHolonReplicaPayload` |
| `SelectedChildIds` | `ConditionalHolonPlan`, `CommittedHolonReplicaPayload`, `CommittedHolonReplicaPlan`, `CommittedHolonReplicaTarget` |
| `RelationshipManifest` | Newly captured planning data; provider persistence integration remains open |

Core `HolonChildCollectionState` also uses the tracking reference comparer; tracking relies on canonical runtime serialization. Manager preparation/publication and model lifecycle entry points use that tracking contract. Publishing only a new public manifest class would not satisfy this dependency closure.

## Integration gate

Define coherent source batches and include their actual dependency closure, tests and scoped documentation. Check ownership of every modified file against concurrent contributors before staging. Do not infer ownership from a filename or sweep the entire dirty Core/submodule into a release.

Verify the exact committed source batch, including required untracked sources, rather than treating current dirty-checkout test results as evidence for a different committed snapshot. Reuse the existing environment and build with bounded workers. Native evidence must identify the copied assemblies actually used, because producer output can advance afterward.

Core and STAR submodule publication requires a reviewed committed dependency state. Update the parent gitlink and deployment manifest together only for that verified state, then run the existing manifest validator. Preserve foreign staged manifest/script changes. No published pin may imply manifest persistence, CQL, deltas or other unfinished roadmap capabilities.

## Current scope

Core1,128, Shared782, SQLite711 and STAR8 are separately dated/hashed checkpoints in the working documentation, not a single clean-source release certificate. The provider session independently owns its committed provider/Shared-base batches and tracks their evidence under `Docs/WIP/HyperDrive-Providers`. Its committed snapshot must be assessed independently from this session's local COSMIC graph/tracking changes.

The full roadmap remains active. Integration is an outstanding acceptance gate, not an external blocker to continued implementation.

## Prepared Core integration inventory — 20 October 2026

Fetched origin/Development for Core and parent. Core HEAD remains6b0b104 and is0ahead/0behind; parent is1ahead/0behind at inspection. No merge/reset/index/commit/push performed. Core index is empty. The provider-session owner confirms it owns no pending Core production edits. The following55 working paths form the candidate COSMIC dependency closure; each remains subject to final diff review and exact committed-source verification before publishing or advancing pins.

```text
 M Helpers/Mapper.cs
 M Holons/CosmicConcurrency.cs
 M Holons/Holon.Part1.cs
 M Holons/Holon.Part3.cs
 M Holons/HolonBase.cs
 M Holons/SemanticHolon.cs
 M Interfaces/Providers/IConditionalHolonStorageProvider.cs
 M Managers/HolonManager/HolonManager-Delete.cs
 M Managers/HolonManager/HolonManager-Load.cs
 M Managers/HolonManager/HolonManager-LoadAll.cs
 M Managers/HolonManager/HolonManager-LoadByMetaData.cs
 M Managers/HolonManager/HolonManager-LoadForParent.cs
 M Managers/HolonManager/HolonManager-LoadHolonsByMetaData.cs
 M Managers/HolonManager/HolonManager-Private-Load.ById.cs
 M Managers/HolonManager/HolonManager-Private-Load.ByMeta.cs
 M Managers/HolonManager/HolonManager-Private-Load.cs
 M Managers/HolonManager/HolonManager-Private-LoadAll.cs
 M Managers/HolonManager/HolonManager-Private-LoadForParent.cs
 M Managers/HolonManager/HolonManager-Private-Save.cs
 M Managers/HolonManager/HolonManager-Private.Helpers.cs
 M Managers/HolonManager/HolonManager-Private.Replication.cs
 M Managers/HolonManager/HolonManager-Private.Restore.cs
 M Managers/HolonManager/HolonManager-Private.cs
 M Managers/HolonManager/HolonManager-Save.cs
 M Managers/HolonManager/HolonManager-Search.cs
 M Managers/HolonManager/HolonManager.cs
 M "Managers/OASIS HyperDrive/OASISHyperDrive.cs"
 M "Managers/OASIS HyperDrive/StorageOperationRequest.cs"
 M "Managers/OASIS HyperDrive/Synchronization/OASISProviderFanOutHandler.cs"
 M Serialization/StoredRecordJson.cs
?? Holons/Holon.ChildCollections.cs
?? Holons/Holon.ChildRoleBindings.cs
?? Holons/Holon.RelationshipState.cs
?? Holons/HolonChangeTracking.cs
?? Holons/HolonChildCollections.cs
?? Holons/HolonChildRoleBinding.cs
?? Holons/HolonChildRoleDelta.cs
?? Holons/HolonChildRoleHydration.cs
?? Holons/HolonChildRoleManifest.cs
?? Holons/HolonChildRoleObservation.cs
?? Holons/HolonChildRoleResolution.cs
?? Holons/HolonValueDelta.cs
?? Interfaces/ICosmicChildCollections.cs
?? Interfaces/ICosmicChildRoleBindings.cs
?? Interfaces/Providers/IAtomicHolonReplicationStorageProvider.cs
?? Interfaces/Providers/ICommittedHolonReplicaStorageProvider.cs
?? Interfaces/Providers/IHolonReplicationOutbox.cs
?? Interfaces/Providers/IHolonReplicationOutboxSource.cs
?? Interfaces/Providers/IHolonReplicationRecoveryOutbox.cs
?? Managers/HolonManager/HolonManager-Delete.V2.cs
?? Managers/HolonManager/HolonManager-RoleHydration.cs
?? Managers/HolonManager/HolonManager-Tracking.cs
?? Managers/HolonManager/HolonManager-Transfer.cs
?? Managers/HolonManager/HolonSavePlan.cs
?? Serialization/HolonChildRoleManifestJson.cs
```

Current dirty-source gates pass: Core1210, Shared784, SQLite803, STAR14, zero skipped. Core both targets build. Typed load batch is point-route scoped; broader collection/partial/native/provider gates remain open. Next integration sequence: review and commit the scoped Core production closure, rebuild/test that immutable epoch, then pair parent tests/Shared/provider consumers and dependency manifest/gitlink changes in coherent batches. Never advance a parent deployment pin to imply unfinished roadmap capabilities.

Integration review retained depth gate: `Serialization/ImportGraphSnapshot.Create` and ordinary committed replication acknowledgement traversal still use recursion. The verified heap-based tracking/save/preparation and selected-provider hydration depths must not be generalized to unlimited-depth import/ordinary replication. This is a remaining correctness task; no published full-depth guarantee is inferred from the scoped Core closure.

Core source publication completed: `2775936cd0e9fbc16550f260c349b7a02505f554` fast-forward pushed from6b0b104 to Core origin/Development, remote SHA verified. Post-commit Core both-target build succeeds; Core1210/1210 and SQLite803/803 pass, zero skipped (`core-2775936-unit-verified.trx`, `core-2775936-sqlite-verified.trx`). Copied Core SHA256 `8C6503E2ADAC8BA29E742FEBEDA10C51F064575A988A2FCE15F56D08DDC17ABC`; owned SQLite database/wal/shm absent after terminal completion. Core source/index clean. Parent test/Shared/provider/STAR closure and manifest/gitlink admission remain unfinished. No application deployment, Atlas request or foreign-process termination occurred.

Companion STAR source closure published: `2eba5ebfa9d74f1d71f55cc854a48e56a61948d2` fast-forward pushed to STAR origin/Development after a scoped five-file commit (three model owners and two test files). Remote SHA verified. Three concurrent README changes remained untouched and unstaged. Post-commit STAR producer/test-consumer builds pass and14/14 tests pass (`star-2eba5eb-unit-verified.trx`), zero skipped. STAR producer/consumer SHA256 `2C6766699C42ACA2B9B625BC598828FC078EB65634E1A124D03C9BB00A1DF967`; copied Core `43B9E23C9D83337E6A2E6767681EA335916E9A5D388C8BEEEAA9011F78119464`. Core checkout is clean at2775936; concurrent builds produce separately recorded consumer epochs, so this STAR gate is not conflated with the earlier8C650 Core/SQLite gate. Core AssemblyInformationalVersion currently records2.0.1+2775936. No parent gitlink/manifest or application deployment was changed by this source publication.

Next source closure is the parent Core regression tests, Shared planning/replication contracts and selected provider consumers, plus the matching Core/STAR gitlinks and dependency manifest in a coherent verified batch. Existing worker/provider-session commits must remain intact; dependency-safe deployment admission is still open. The dev hosted-sync pause and local-only Mongo development policy remain preserved.

Parent source-closure candidate verified:50 scoped paths contain21 Core regression files,15 Shared production contracts/planners,10 Shared test files, this integration note, two submodule gitlinks and the two-value dependency manifest update. Core2775936 and STAR2eba5eb are published. Fresh source/consumer builds pass; Core1210/1210 and Shared784/784 pass, zero skipped (`parent-closure-core-verified.trx`, `parent-closure-shared-verified.trx`). Core test copied SHA256 `EFE3AE5ECE5A4BC28DFB6FF3B75CE96BA9F97D19B8A969C671C0341179357349`; Shared test copied assembly `5A2FE9FA91691D589916C1D763E3F646857262FF4005BBDBEC4E3E43130D7C70`. These are separately recorded build epochs.

The structural manifest validator passes and staged gitlinks match both updated values. The strict --require-gitlinks check reads HEAD rather than the index, so its expected pre-commit mismatch is not a source failure; it must pass immediately after the coherent commit. Selected provider production/history/replication implementations remain a subsequent scoped source batch; this commit publishes optional shared contracts and planning infrastructure, not complete provider enrollment or full roadmap certification. Concurrent provider-session commits and release-gates documentation are excluded from staging and preserved. Hosted-worker settings remain owned by the worker session; no configuration change or Atlas query is part of this batch.
