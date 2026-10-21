# MongoDB durable preparation implementation checkpoint

Subsequent numeric prerequisite: the current selected suite now passes 175/175 controlled and 12/12 native cases, including all ten preparation/conditional cases below. [Exact numeric persistence proof and separate artifact cohort](MONGO_AUDIT_PRECISION.md#json-numeric-storage-follow-up--20-october-2026). The original ten-case report is retained as TestResults/SubscriptionUsageCurrent/mongo-cosmic-conditional-before-json-numbers-907B347CEC56.trx; the earlier preparation evidence below retains its original scope.

20 October 2026. The final MongoDB unit consumer passes 153/153 tests and the reusable native conditional suite passes 10/10, with zero skips. An independent retained-fixture reconciliation also passes 1/1. These prove the selected cases below, not full fault or production acceptance.

## Native ownership

MongoDB implements Core's existing IHolonWritePreparationStorageProvider contract. Per-record encrypted envelopes remain opaque native BSON strings in CosmicWritePreparation; there is no provider encryption or alternate logical-intent comparison. Core retains all authentication, typed snapshot binding, graph/condition comparison and caller publication ownership.

Existing winners use a primary, majority read and retain their original ordered records. First claims run in a snapshot transaction with majority/journaled commit. First claims and conditional transactions with no existing receipt write the same persistent CosmicOperationFence document before mutation. Positive receipt replay remains read-only. A missing-row snapshot read alone would not serialize a competing receipt insert; the shared native write fence supplies that conflict point.

A prior receipt without its original preparation rejects reconciliation-required. Existing preparation plus receipt remains recoverable. The native `_id` constraint owns uniqueness; the fence generation is coordination state, not a holon revision or user version. All participating conditional writers must use this owner: a mixed deployment with older unfenced writers is not certified. Legacy Hyperdrive routing is unchanged.

Pure native pre-commit contention can return COSMIC_PREPARATION_CONCURRENT_CLAIM. The caller retries the same operation after the winner completes. There is no automatic transaction callback retry, new operation ID, cross-provider substitute or sequential write path. Commit/abort uncertainty exposes operation and available native session/transaction evidence through the reconciliation marker; no transaction identity is invented. A known original winner survives later cleanup failure as a warning.

## Admission and storage

Preparation has the shared record/count/text bounds and a native 16 MiB BSON document limit checked before storage. Stored format/type/record identities are validated before publication. Other actors receive no returned preparation. Public calls reject missing candidates, wrong authority, cancellation and an unactivated repository explicitly through OASISResult. A transaction-capable MongoDB replica set is required; no standalone downgrade is implemented.

## Verified cases

- Seven controlled public admission cases, without activating a database.
- Two native sync/async cases in the existing reusable replica-set conditional suite: fresh manager/provider graph replay, empty-ID allocation, key rotation, separate root/child contexts, exact private metadata and clean tracking.
- Independent BSON comparisons for holons, original receipt, preparation and committed fence; private data stays encrypted while configured metadata remains queryable.
- Changed private intent, another actor and denied historical preparation key reject without changing records.
- A directly committed pre-journal receipt cannot acquire a fabricated original preparation.
- The existing eight native conditional cases remain included to catch transaction/receipt regressions.

Two additional controlled tests verify that runtime encryption policies are excluded from Core BSON records, including embedded children. The original native module-initialization failure exposed an eager dictionary convention resolving serializers before runtime members were removed. Dictionary representation now runs after that removal; runtime policies are not made persistent.

Evidence: TestResults/CosmicMongoCurrent/mongo-preparation-final-unit-full.trx (153/153), TestResults/SubscriptionUsageCurrent/mongo-cosmic-conditional-current.trx (10/10), and mongo-cosmic-conditional-reconciliation.trx in that same directory (1/1). Reconciliation independently checked the original uncertain replica commit, receipt checksums, physical rows, replica heads and committed operation fences before removing only its two owned fixture databases. Receipt replay performs no data or fence writes. Earlier failed reports remain retained.

The existing installed MongoDB runtime, fixed SubscriptionUsageCurrent data directory, environment lease and fixture are reused. Failed fixtures are retained for independent reconciliation; they are never automatically dropped to unblock a rerun. Real process/fault schedules, broader concurrent claim schedules, retention/operations acceptance and Neo4j preparation support remain open.

## Build coordination prerequisite

Cross-session messages allowed simultaneous Shared output replacement and consumer copies. Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1 now owns exclusive file-based producer/consumer exclusion; AGENTS.md requires it for shared builds/runners. The wrapper fails before executing a competing body and releases on exceptions. The cross-process contract test records busy child exit 1, free child exit 0 and exception release true in TestResults/ProviderBuildCurrent/lease-contract.json. This is build ownership, not a provider request retry mechanism.
