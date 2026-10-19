# Shared canonical record-history codec

Updated2026-10-19. Shared extraction and current Cosmos regression gates pass. AWS/DynamoDB and Google Cloud explicit historical reads are STILL unsupported; this extraction is not their implementation or provider certification.

## Verified scoped result

Rebuilt shared427/427passed1s, Cosmos unit152/152passed903ms, retained official-SDK/Cosmos-emulator native42/42passed83seconds; all zero skips. Original runner terminal0 and pinned retained container independently confirmed exited (`77f8a39f5c39e0b7878c8d9cddd70ff08872695dec9b85d2055b38d5d58d5302`). No new image/container/volume/install/download/environment. Current Cosmos same-item writer/ETag/size/reconciliation behavior is unchanged; only pure canonical history logic moved behind its existing static adapter.

TRX SHA256: shared `656524F19827A84891851F3015B17CAF713B672CB7EB30F94EE139ECEE05CFD8`; Cosmos unit `998DA06A6B9A170C82507B41172AFE4E8D1525AB918C6AECAF31899AC345E571`; Cosmos native `5757B42B837BA1D5879DD3B7D8A0FFB93966A0BCEEB52E66D815A6BCD9DAA400`. Fixed compact reports retained under EmbeddedHistoryCurrent and CosmosDBCurrent. Dependency manifest/gitlink validator green, Development0ahead/0behind before integration. Broad cloud IAM/hosted/provider-version support claims remain excluded.

Consumer-output cleanup was deliberately NOT performed: inspection found existing synthetic routing backup `Providers/Cloud/TestProjects/NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.IntegrationTests/bin/Debug/net10.0/TestResults/CosmosDBRouting/delete-backup.db` (634880bytes). Native suite is terminal but that is not alone proof the retained backup is disposable; preserve it until exact fixture reconciliation. No copies/moves/deletes or alternate cleanup attempted. Free C:80,944,824,320bytes measured during native run. See WORKSPACE_CLEANUP_LEDGER.md; no reclaimed-space claim.

## Existing gap and reuse

Current DynamoDB/AWS rejects positive historical versions in ValidateRecordVersion; GoogleCloud/Firestore similarly rejects them. Existing Cosmos same-item history already performs detached canonical staging, strict archived-record identity/version validation, existing StoredRecordVersions stamping/linkage, and historical version selection. Extracted that exact logic into `EmbeddedRecordHistory` in the existing shared provider library; CosmosEmbeddedHistory remains its public static adapter with unchanged signatures and its existing native-field list. No new backend, environment, SDK or fallback. First shared build caught one accidentally static instance method; corrected before successful builds/tests.

Native metadata removal is configured per adapter, cloned at construction and augmented only with native `id` and the shared history property. Cosmos keeps exactly its prior id/etag/rid/self/attachments/ts exclusions; no Cosmos field names are implicitly stripped by generic consumers. Archived native fields are rejected, not silently published or deserialized as domain state. This list is a serialization boundary, NOT a public-data/privacy whitelist. Canonical payloads and histories can contain credentials/private metadata and require the same access control/encryption policy as current records. History retention means old credentials/private data also remain until a reviewed retention/deletion policy handles them; no public-chain storage permission is implied.

## Responsibilities

- Shared codec owns detached canonical JSON, schema1archive validation, exact ID/version matching, unique preceding versions, no silent trimming, version/previous-version IDs and selected-version reads. It reuses StoredRecordVersions rather than reimplementing stamps. Caller IDs/versions remain unpublished before native ACK.
- Providers own actual SDK calls, create-only/observed-revision conditions, response equivalence and reconciliation, transport retry rules, native item limits, caller publication, credential authorization, graph/public lookup integration and billing. Cosmos SDK persistence still uses its existing ETag code; no second writer added.
- Native field lists are explicit, not data-safety declarations. Providers must validate their actual item identity/projection in addition to selecting a version. Version selects roots only; provider-linked children retain independent versions as previously chosen by the user.
- AWS/DynamoDB and Firestore need reviewed native storage layout/capacity and atomic update rules before wiring this codec or choosing a separate native history-record layout. Do not blindly copy Cosmos2MiB assumptions into DynamoDB400KiB or Firestore limits. A capacity rejection must preserve the prior committed record/archive and must not trim history or look successful. Actual request/storage costs grow with retained data; failed conditions/read-before-write and archive reads can add SDK billing operations. No fee estimate or cloud-IAM proof is made here.

## Current evidence

Five new generic controlled rows pass as part of rebuilt shared427/427/zero skips1second: detached two-version staging/readback/linkage and retained private metadata; duplicate/foreign-ID/native-field archive rejection even on latest read; explicit immutable adapter field policy. Existing Cosmos unit152/152passes903ms/zero skips with rebuilt producer and consumer. Same retained Cosmos image/container/volume native suite is running; no native regression claim until terminal report. Missing current test assets regenerated cache-only from existing NuGet packages; no download/install/new runtime/database/container. Source/shared build succeeds. Current changes remain uncommitted until relevant native regression gate passes.

## Next provider implementation gates

1. Implement actual SDK conditional/CAS history writer, including create races, lost ACK, no unknown replay, size rejection and preservation when option disabled after archive creation.
2. Positive-version ID/key/list/selectors/metadata/search/parent/export reads must select stored history, never silently return latest; authentication must use current authoritative credentials, not historical login state.
3. Stamp/publish IDs/versions only after native ACK and integrate existing common graph projection, not another hierarchy implementation.
4. Real retained DynamoDB/Firestore native proof plus relevant AWS facade tests and actual cloud IAM/session credentials when available. Local service proof is not real cloud account/IAM/hosted/V2 completion.
