# Durable conditional-write preparation

20 October 2026. This is an implementation checkpoint. Shared manager/V2 preparation recovery and the native SQLite store are implemented. Selected native encrypted retry cases now pass; complete provider, failure, retention and authorization acceptance remains open. Historical storage-only evidence is retained below.

## Current V2 save flow

SQLite primary acknowledgement decoding now uses the [integrity-bound receipt envelope](SQLITE_RECEIPT_INTEGRITY.md), including pre-COMMIT self-validation and exact original replay. The preparation protocol and immutable encrypted winner are unchanged. Historical primary envelopes require explicit reconciliation; they are not rewritten by preparation recovery.

Provider updates: [MongoDB](MONGO_WRITE_PREPARATION.md) has 153/153 controlled, 10/10 native conditional and 1/1 independent reconciliation cases. [Neo4j](NEO4J_WRITE_PREPARATION.md) now has 174/174 controlled and 13/13 native conditional cases, including encrypted manager/V2 replay. These are bounded evidence sets; broader fault, retention and authorization acceptance remains open.

1. The detached save plan allocates stable operation-based identities. Core captures exact logical values before audit/encryption, retaining selected root/child order and per-record protection contexts.
2. Normal metadata preparation runs. Core protects each node's original prepared snapshot and logical intent in that node's own authenticated preparation envelope. The protected graph intent binds authority, actor, operation ID, ordered roots/edges, relationship roles, expected revisions, child/recursive/depth flags, replication enrollment and requested CLR snapshot contracts. Type identity uses FullName and assembly simple name, omitting assembly version. Stored names never select an instantiated type.
3. Hyperdrive checks the required preparation capability before provider activation/persistence. The claim runs inside the existing pinned V2 provider invocation and its configured admission owner; there is no direct database access from HolonManager. This does not implement the unfinished authenticated WEB4 tenant/policy boundary.
4. Core authenticates every winning envelope and compares the complete logical selection before constructing any recovered entity. Exact numeric comparison preserves tiny decimal differences; strings remain strings. Changed intent is rejected before holon mutation.
5. Core restores the original prepared snapshots into the currently requested admitted CLR types, then reconnects the captured topology. Original encryption nonces/ciphertext remain unchanged. Runtime resolvers, policies and key material are not persisted.
6. The existing provider conditional transaction/receipt owner receives the recovered request with its full fingerprint unchanged. Caller state and tracking publish only after the normal complete acknowledgement checks.

Unsupported preparation providers return COSMIC_PREPARATION_UNSUPPORTED. SQLite has the selected end-to-end encrypted recovery proof. The coordinated provider session has also implemented DynamoDB/AWS claims and eight native encrypted manager/V2 cases on the same Core D490 epoch; see its provider-specific evidence. Newly implemented conditional capabilities in other providers do not automatically grant encrypted preparation support.

## Runtime policy and rollout

The ordinary conditional-save API selects the shared recovery path; callers do not invoke the preparation store directly:

```csharp
manager.MetadataEncryptionPolicy = new HolonMetadataEncryptionPolicy(
    "application-context", "current-key-id", authorizedResolver,
    new[] { "AppRegion" });
var operationId = Guid.NewGuid(); // retain for retries of this logical operation
var saved = await manager.SaveHolonAsync(
    holon, avatarId, new CosmicWriteOptions(operationId),
    saveChildren: true, providerType: ProviderType.SQLLiteDBOASIS);
```

This assumes an activated/configured V2 manager and uses Security, Interfaces and Enums from Core. The application supplies authorizedResolver; no runtime resolver is accepted from stored data. AppRegion and reserved system metadata remain queryable in the holon store. The preparation journal encrypts its copy of both logical and prepared data; it does not change which metadata the actual holon keeps clear.

For an older explicitly configured metadata cipher, set `manager.PreparationEncryptionPolicy` to an authorized policy while retaining the existing metadata encryption settings. This protects the journal without silently changing the metadata format.

For each selected node, preparation protection selects its MetadataEncryptionOverride, otherwise the manager's explicit PreparationEncryptionPolicy, otherwise MetadataEncryptionPolicy. Every selected node requires an authorized context. A current write-key change can read an older winner through its retained stored key reference; denied/retired keys are errors. Changing configuration does not rewrite an original prepared operation.

Existing CBC/layered metadata formats can use a separate PreparationEncryptionPolicy. Their metadata cipher and reserved query-key behavior remain unchanged. An encrypted conditional save without any authorized preparation context fails before storage. Ordinary saves are unchanged.

A prior write receipt without an original protected preparation cannot establish the original logical plaintext. SQLite rejects that state with COSMIC_PREPARATION_RECONCILIATION_REQUIRED before inserting a new claim. Operators must reconcile the original operation; no receipt-derived guess, automatic backfill, new nonce reconstruction or silent second write is allowed. Retain original preparation and receipt/key lineage together until an explicit retention policy proves replay is no longer required.

## Recovery verification in progress

The original four-case native regression now passes4/4, including both encrypted retries. Expanded native23/23 and complete native1,034/1,034 pass with no exclusions/skips on the recorded 1FA3 Core epoch. Eleven new Core admission cases exposed one ordering defect (a valid root constructed before late-child authentication failure); the corrected all-selection owner passes within complete Core1,522/1,522. An additional six native cases pass, covering abandoned post-claim dispatch, real commit followed by injected application acknowledgement loss, and old metadata CBC with explicit preparation protection. These are application-boundary injections, not process termination or real network fault certification. Final complete Core1,524/1,524 and native SQLite1,041/1,041 pass with no exclusions/skips; the receipt-cutover, explicit mixed-context and typed-snapshot binding gates are included. Exact reports and artifact identities are in the integration boundary.

Open acceptance includes actual process/transport interruption, native claim commit/rollback/cleanup fault injection, history/replication/migration key-context integration, mixed-tenant contexts, paid native-fee provider preparation, operator reconciliation/retention and other provider implementations. The original broader roadmap remains active.

Final recovery acceptance: Core **1,524/1,524** and SQLite **1,041/1,041**, zero skips/exclusions, with both red ordering/type-contract cases corrected. Core source `a265d7a`; final reports are preparation-type-contract-final-core-full.trx and preparation-type-contract-final-native-full.trx. The coordinated [DynamoDB/AWS encrypted V2 proof](../HyperDrive-Providers/AWS_DIRECT_CONFIGURATION_AUDIT_2026-10-20.md#aws-end-to-end-encrypted-v2-replay--88-native-green-2026-10-20) adds eight native cases on the same Core epoch, with its own Shared artifact and explicit scope. Exact evidence is retained in [integration history](INTEGRATION_BOUNDARY.md#shared-v2-encrypted-conditional-save-recovery--20-october-2026-1322-local).

## Ownership invariant

One logical operation retains one winning protected preparation at one concrete provider authority. A claim is not a holon mutation or committed acknowledgement. After claiming, Core must authenticate every returned record in its authorized runtime context, compare the complete original logical intent and conditions, and dispatch only the winning prepared graph. Storage does not decrypt the candidate or decide whether two plaintext inputs mean the same thing.

The protected per-record payload must retain each node's own authorization boundary. The shared preparation cipher binds operation, actor, authority, record, context and key identities; metadata and preparation are separate authenticated domains. See [crypto evidence](ENCRYPTION_CONTEXT.md#protected-preparation-crypto-owner--20-october-2026).

## Shared provider contract

Core exposes `IHolonWritePreparationStorageProvider`, `HolonWritePreparation` and `ProtectedHolonPreparation`. Claim sync/async operations retain the first candidate and return its original ordered opaque records, including when a later same-actor candidate contains different bytes or record identities. Returning the winner does not accept changed logical intent: the subsequent Core authentication/comparison owner must reject it before mutation. A different actor receives an explicit error with no preparation contents.

The immutable candidate captures between one and 4,096 unique, nonempty record identities. Individual envelope text is limited to two Mi characters; the aggregate limit is sixteen Mi characters. Admission accepts only the preparation prefix and valid Unicode. It does not claim cryptographic authentication. Record references and ciphertext remain untrusted until Core authenticates them. Operation and actor identities must be nonempty, and the provider authority must be concrete.

These are explicit character/count limits, not native scan deadlines or deployment quotas. The SQLite escaped JSON transport has a separate bounded read admission. There is no process-local claim cache, nonce derivation, ciphertext fingerprint omission, cross-provider fallback or automatic retention cleanup.

## SQLite native owner

`CosmicWritePreparation` stores operation ID as its native primary key, actor ID, opaque record JSON and UTC claim time. An IMMEDIATE native transaction reserves the writer before `INSERT ... ON CONFLICT DO NOTHING` and the winner read. Independent clients converge on one row. The first prepared bytes remain stable across provider reconstruction; no holon row is written during a claim.

Actor and authority mismatches are errors without returned contents. Corrupt/duplicate/invalid stored record JSON rejects without replacing it. The claim owner separates pre-commit failure, uncertain commit or rollback, and a known acknowledgement followed by cleanup failure. An uncertain outcome carries `SqlitePreparationReconciliationException`, the original operation ID, and the shared native reconciliation marker; no native transaction ID is invented. These failure-state branches still require injected fault acceptance beyond the initial native conformance tests.

Both activation upgrade and EF migration create the table. Downgrade refuses to discard any retained claim. Retention, abandoned preparations, operational reconciliation and authenticated tenant admission are still required; do not delete claims to make a retry appear new.

## Initial storage-only evidence and then-remaining integration

Initial focused native claim cohort: eight executed, eight passed, zero skipped (`TestResults/SQLiteCurrent/preparation-store-native-focused.trx`). These storage tests intentionally use opaque random tokens, not authenticated Core envelopes, and do not certify encrypted logical replay. Three new Core contract tests cover detached ordering, immutable publication, duplicate/null/identity admission and count/payload limits. Full Core initially passes 1,511/1,511; final valid-Unicode admission evidence is recorded in the integration boundary after rebuilding.

Next work:

- Capture exact logical intent and prepared per-node snapshots in the protected payload, including ordered roots, graph membership/roles, write conditions, operation flags and replication enrollment.
- Route claims through V2's existing authorization, billing and pinned-authority execution owner. Unsupported providers must refuse the required capability before any persistence.
- Authenticate and compare the winning preparation before dispatch; do not trust returned selectors or provider data.
- Recover the original prepared graph without publishing caller/tracking state until the normal committed acknowledgement is verified.
- Verify same-intent races, altered intent, key rotation/denial, tampering, crash/uncertain outcomes, sync/async single/batch/child graphs and supported provider parity.

The [encrypted retry regression](CONDITIONAL_WRITES.md#encrypted-preparation-replay-defect--20-october-2026) remains two passing unencrypted controls and two failing encrypted cases. Having a claim table alone does not fix that regression.

Final acceptance: Core1,511/1,511 and SQLite1,011/1,011 pass, zero failures/skips, terminal0; native run129s. These reports are preparation-store-final-core-full.trx and preparation-store-final-native-full.trx in their existing Core/SQLite result folders. The separate final replay report preparation-store-final-replay-regression.trx executes all four cases, passes two and fails two, terminal1. XML counters confirm all stated totals/executed counts. Core source is locally committed as f2bd54c. Exact assembly hashes are retained in the integration boundary. The owned fixture DB is absent after all runs; no push/deployment.
