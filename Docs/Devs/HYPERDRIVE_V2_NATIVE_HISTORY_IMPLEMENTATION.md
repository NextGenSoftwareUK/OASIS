# Native provider history implementation and acceptance contract

Evidence audit: 2026-10-16, parent `2c3dff3af`. This is an implementation specification and open release gate, not a claim that history works.

## Existing semantics and evidence

- `IOASISDBStorageProvider.IsVersionControlEnabled` describes retaining a new copy linked to the preceding version instead of simply overwriting it. See `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Interfaces/Providers/IOASISDBStorageProvider.cs`.
- Holon version identity includes `Version`, `VersionId`, `PreviousVersionId` and `PreviousVersionProviderUniqueStorageKey`. Logical record identity must remain distinct from version identity and native storage keys.
- Shared `KeyValueStorageProviderBase.StampVersionAsync` assigns initial version at least1, increments the previous version, records its VersionId and optionally archives the previous canonical payload. `ReadVersionAsync` treats0 as latest and validates historical ID/version. Reuse those semantics; do not copy its separate archive/current writes as an atomic protocol.
- DynamoDBOASIS declares `IsVersionControlEnabled` but source search finds no use of it in saves/reads. Setting true does not establish history. Its positive-version guards are deliberate explicit incompleteness, not history support.
- Google and Azure latest-only guards likewise do not implement historical persistence or reads. Their current canonical JSON includes version fields but storing fields is not retaining historical records.
- SQLite repository version predicates select matching rows. Predicate coverage alone is not proof that successive versions coexist; audit its save keys/schema and retention separately before claiming historical support there.
- Follow-up SQLite source audit: `SQLLiteDBOASIS.Avatar.cs` declares the version-control flag without consumers. `HolonRepository.AddOrUpdateHolon` finds/replaces the existing row by logical Id, so its version predicates are not an implemented archive. History remains open. The accompanying detached graph identity fix does not add history.
- WEB5 STARNETHolonID/version semantics are distinct from WEB4 logical holon IDs. Do not replace STARNETHolonID lookup with direct WEB4 ID lookup or conflate the version domains.

## Required invariants

1. Zero selects latest; negative versions fail locally. Positive versions select exactly that version or structured not-found, never silently latest.
2. Retained history is immutable canonical data, keyed unambiguously by record kind, logical ID and version. Validate embedded ID/version/VersionId and linkage against the key.
3. Current-record lists must not accidentally enumerate archive records as additional live avatars/details/holons.
4. A successful update must not advance current while losing the previous archived version. Concurrent updates must not overwrite the same version or fork linkage silently.
5. Publish generated version identifiers/linkage to caller objects only after native acknowledgement. Unknown outcomes retain original cause and durable operation identity/evidence, with no blind mutation replay.
6. Profile/detail lifecycle operations must preserve their existing consistency guarantees while recording history; do not regress Dynamo/Firestore paired transactions into independent writes. Azure cross-blob changes remain explicitly non-atomic until an implemented recoverable protocol proves otherwise.
7. Existing identity ambiguity, canonical projection integrity, tombstone, ownership, graph depth, metadata matching and partial-acknowledgement rules still apply to version-aware reads/writes.
8. Version-aware username/email and collection/parent/metadata/export reads need specified semantics and actual implementation. Do not implement ID history alone and remove guards everywhere.
9. Soft deletion is a retained state transition. Hard-delete/history retention policy must be explicit; never retain private deleted data by accidental archive leakage or silently destroy history under an unrelated delete contract.
10. Replication must preserve committed version identity/linkage, not independently increment a replica into a different version. Distinguish source-authoritative replication from a new user mutation.

## Native implementation sequence

| Store | Native path to implement | Main gate |
| --- | --- | --- |
| DynamoDB/AWS | Dedicated archive table/keyspace plus conditional native transaction for immutable previous version and current update; observed canonical payload/version CAS | Same-version competing writers; transaction cancellation; native commit followed by lost acknowledgement; profile/detail lifecycle integration |
| Google Firestore | Dedicated history documents with transactionally read current and conditional archive/current writes | Concurrent transaction retries, generated version publication, paired lifecycle preservation, uncertain commit reconciliation |
| Azure Blob | Dedicated history namespace/container, immutable conditional archive creation and current ETag CAS, with explicit recovery/partial evidence | Archive acknowledged/current conflict or uncertainty; no false atomic success across blobs |
| MongoDB/SQLite | Audit/reuse existing native historical storage and transactions before adding any alternate scheme | Actual multiple-version retention/readback and V2 replication, not only version predicates |

Use the existing reusable native runtimes and fixed evidence directories. No new clone, runtime or per-run environment is required for these stores.

## Acceptance matrix (all currently unproven as a complete native-history feature)

SQLite negative-version boundary is now verified separately (2026-10-16): disposed-context matrices prove local argument failure before database access; latest/current-version regression remains green. This does not satisfy the retention/linkage/transactional history matrix below.

Shared key/value avatar/detail save snapshots now preserve caller version metadata on failed saves. An acknowledged archive followed by failed current write returns exact acknowledged-key reconciliation evidence; unit tests inspect both archive and unchanged current payload. Archive/current writes remain separate/non-atomic, first-write uncertainty remains backend-owned, and this is not native transactional-history or durable-recovery completion. No hard-deletion/retention policy changed.

- Avatar, avatar detail, holon: create1 → update2 → update3, then independent native archive/current readback and provider loads0/1/2/3/missing.
- Verify VersionId uniqueness and PreviousVersionId linkage, full inventory/metadata/derived payload preservation, no caller mutation on failed acknowledgement.
- Sync/async ID/key/username/email/collection/parent/metadata/export routes; changed selectors; historical tombstones and hard-deletion policy.
- Two real competing updates and native conditional/transaction rejection; same version reused with different payload; corrupt archive ID/version/linkage.
- Real commit then controlled SDK acknowledgement loss, durable restart reconciliation, physical transport-loss evidence separately labeled.
- V2 source→replica and reverse source ordering with exact same committed version/linkage; normal failover/load balancing and terminal integrity failure; no replica-local version increment.
- Hosted HTTP routes and deploy pins/builds after native acceptance. Emulator success is not live IAM/quota/network certification.

Latest-only guards stay explicit until their corresponding historical behavior passes. This document does not authorize claiming a provider complete or merging before the full release gates pass.
