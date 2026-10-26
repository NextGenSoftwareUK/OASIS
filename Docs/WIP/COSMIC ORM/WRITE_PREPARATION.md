# Durable conditional-write preparation

## Request-owned protection configuration

`HolonManager.CreateRequestContext` now retains the metadata and preparation protection policies observed when the context is created. Previously it constructed a new manager with neither policy, allowing scoped writes to silently lose configured protection. The new manager keeps the immutable policy references, including authorized resolver scope and query-visible key configuration; no key material is copied into request data. Resolver lifetime remains application-owned.

A trusted host may assign an actor-specific preparation policy to the returned request manager before any storage operation. Assigning that request's policy does not change the parent or another request manager. Later parent-policy changes do not rewrite an existing request's observation. This reuses the existing request-context owner rather than mutating the shared manager around an await.

Two inheritance regressions fail against the previous producer, while the concurrent per-request override control passes. With the factory fix, all three controls and full Core **3,497/3,497** pass without skips/failures; both target builds pass. The concurrency test pauses one preparation claim while another request completes and verifies both encrypted envelopes against their own authorized contexts. [Red evidence](Verification/request-protection-red.trx.gz), [frozen producer](Verification/request-protection-red-producer.json), [full corrected regression](Verification/request-protection-core-full.trx.gz).

Native verification passes inherited request-context protection through real encrypted manager soft deletes and stored-only recovery after reconnection on MongoDB, SQLite and Neo4j. Existing signed CQL/transport/delete and actual hard-delete acknowledgement-loss controls remain green: **453 settlements / 496 attempts**. Sources stayed unchanged, seven producer/consumer pairs matched, and owned fixtures/services were removed. [Native TRX](Verification/request-protection-native.trx.gz), [result](Verification/request-protection-result.json), [source identities](Verification/request-protection-source.json.gz), [binary identities](Verification/request-protection-binaries.json). This does not configure an unconfigured host or close authenticated recovery/process-restart acceptance.

## Protected conditional delete enrollment

Conditional `DeleteHolon` / `DeleteHolonAsync` now use the same preparation capture and first-winner recovery owner as conditional saves when an explicit preparation or metadata protection context is configured. Both soft and hard deletes seal their actual operation kind, original selected-root values, original revision condition and root-only traversal flags before mutation dispatch. An operation sealed as a save cannot be reused for a delete. Preparation lookup/reconstruction remains a read-only workflow and never dispatches the mutation again.

Deletes select only their root. Unselected children are not included in the preparation and are not published or changed by the delete. The request explicitly retains its actor and authority, requires that authority, and disables command replication. Missing preparation capability fails before the conditional mutation. An uncertain delete result retains the protected original without publishing deletion or a new revision into the caller.

The regression was reproduced against a frozen previous Core producer: **16 cases, 12 failures and four passing unprotected controls**. With the shared capture fix, full Core **3,494/3,494** passes without skips/failures; both targets build. [Red report](Verification/delete-preparation-red.trx.gz), [frozen producer identity](Verification/delete-preparation-red-producer.json), [full corrected regression](Verification/delete-preparation-core-full.trx.gz).

The native signed cohort passes on MongoDB, SQLite and Neo4j with actual encrypted manager soft-delete enrollment and stored-only status recovery after provider reconnection. The original untouched request still verifies the native receipt, and the retained preparation does not expose the fixture's private values. Existing hard-delete commit-acknowledgement-loss controls remain green; these still dispatch through the provider SDK with storage-conformance envelopes. Both protected soft/hard manager paths are covered by Core tests, but protected hard-delete native acknowledgement loss and OS-process restart need their own acceptance. Cohort remains **453 settlements / 496 attempts**, with frozen sources, seven matching binary pairs and successful owned teardown. [Native TRX](Verification/delete-preparation-native.trx.gz), [result](Verification/delete-preparation-result.json), [source snapshot](Verification/delete-preparation-source.json.gz), [binary identities](Verification/delete-preparation-binaries.json).

This respects the existing explicit protection policy. It does not allocate keys or introduce a plaintext journal for unprotected SDK calls. A deployed host still needs authorized protection policy enrollment and an authenticated recovery endpoint; OS-process interruption/restart remains a separate acceptance requirement.

## Routed stored-intent recovery SDK

Verified 26 October 2026: full Core **3,478/3,478**, zero skips/failures, both targets built; real-Mongo usage **404/404** plus the separate replica initialization control. The three-provider native gate passes routed/direct preparation equivalence before claim and after reconnection, with existing actual lost-acknowledgement recovery and **453 settlements / 496 attempts** unchanged. Sources stayed unchanged, seven producer/consumer pairs matched and owned services/fixtures were removed. [Core tests](Verification/preparation-routing-core-full.trx.gz), [usage tests](Verification/preparation-routing-usage.trx.gz), [native tests](Verification/preparation-routing-native.trx.gz), [native result](Verification/preparation-routing-result.json), [source identities](Verification/preparation-routing-source.json.gz), [binary identities](Verification/preparation-routing-binaries.json).

`HolonManager.ReadHolonWritePreparationAsync(operationId, actor, authority)` routes a lookup through HyperDrive V2, pins the original concrete authority, disables replication, and rejects any returned operation/actor/authority substitution. A missing preparation remains an observation of absence. Direct HyperDrive callers must provide the same immutable `HolonWritePreparationLookup` binding, authenticated actor, required provider and `AutoReplicate=false`; mutation enrollment is rejected before provider access. Both synchronous and asynchronous dispatch require the read capability.

`HolonManager.RecoverConditionalHolonWriteOutcomeAsync` combines lookup, authenticated original-intent reconstruction, and the existing pinned read-only receipt lookup. It requires host-owned protection-context and runtime-type admission callbacks. No request graph or retry candidate is needed, and no execution or first-winner claim is entered. Missing original intent, denied keys/types and changed returned bindings are explicit errors; none authorizes another mutation.

For a trusted SDK host with an authorized per-record policy dictionary, a built-in `Holon`-only type catalogue can be expressed as follows:

```csharp
var status = await holonManager.RecoverConditionalHolonWriteOutcomeAsync(
    operationId,
    authenticatedActorId,
    originalProvider,

    // This dictionary must come from trusted host authorization policy.
    // Missing records fail; never allocate a replacement key here.
    authorizePolicy: recordId => authorizedPolicyById[recordId],

    // Admit only the concrete contracts your application owns.
    // A typed application must explicitly admit its own holon types too.
    admitType: (typeName, assemblyName) =>
        typeName == typeof(Holon).FullName
        && assemblyName == typeof(Holon).Assembly.GetName().Name
            ? typeof(Holon)
            : null);

if (status.IsError)
{
    // Preserve the original operation ID and report the explicit error.
    // Do not infer failure, replay, or switch providers.
    Console.WriteLine(status.ErrorCode);
}
else
{
    Console.WriteLine(status.Result.ReceiptState);
}
```

Under priced host admission, `ReadHolonWritePreparation` and `ReadConditionalHolonWriteOutcome` each require their own ordinary storage-attempt tariff. A query price does not authorize either operation. The full three-provider ordinary tariff matrix now has 21 rows. A successful reconstructed status workflow can perform two separately accounted reads; an authentication failure after preparation retrieval never dispatches the receipt read.

This SDK surface is not an HTTP endpoint or a durable intent enrollment policy. Authenticated host exposure, writer enrollment under an explicit authorized protection policy, startup validation, and actual OS-process interruption/restart acceptance remain open.

## Read-only preparation retrieval

Verified on 26 October 2026: full Shared **1,242**, MongoDB **575**, SQLite **95** and Neo4j **566** unit tests pass, zero skips/failures. Both Core build targets pass. The native signed CQL cohort passes with unchanged **453 settlements / 496 attempts**, including fifteen preparation lookups after provider reconnection and foreign-actor rejection across all three stores. Existing actual commit-acknowledgement-loss receipt recovery also remains green. [Native result](Verification/preparation-read-native-result.json), [native TRX](Verification/preparation-read-native.trx.gz), [source identities](Verification/preparation-read-native-source.json.gz), [seven matched binary pairs](Verification/preparation-read-native-binaries.json). Unit reports are `Verification/preparation-read-unit-{0,1,2,3}.trx.gz` in Shared/MongoDB/SQLite/Neo4j order.

Native preparation cases use opaque storage-conformance envelopes. Core's separate protected reconstruction tests prove encryption/context/graph admission; the combination does not yet prove an authenticated hosted workflow or OS-process restart recovery.

The additive `IHolonWritePreparationRecoveryStorageProvider` capability reads an original preparation by operation ID and authenticated actor ID. It extends the existing claim capability without changing other providers' contracts. A successful result with a null preparation means none was observed; it does not prove rejection and never authorizes a replay. Returned envelopes remain untrusted until Core authenticates them using authorized protection contexts.

MongoDB reads the existing `CosmicWritePreparation` collection on the primary with majority read concern and reuses the claim owner's stored-record validator. Neo4j performs a bounded `MATCH` against the existing receipt and uses the same preparation ownership/schema validator and shared opaque decoder as its claim owner. SQLite uses a separate nonpooled, read-only file connection and a bounded `SELECT`; a missing file is an error and must not be created by lookup. SQLite claim and read paths now share the opaque transport decoder. No lookup opens a write transaction, claims an operation, mutates a fence, or invokes conditional execution.

Provider SDK callers must supply the actor established by their trusted authentication boundary. These methods do not provide an HTTP authentication boundary or decrypt private original intent. Read-only routing through HyperDrive and hosted recovery admission remain separate integration requirements.

## Stored-only reconstruction for status recovery — 26 October 2026

Core now has an internal `HolonConditionalPreparation.RecoverOriginalForStatus` decoder. It reconstructs the original conditional request from the existing protected preparation without requiring a retry candidate. Save, soft-delete and hard-delete kinds are explicit; the existing save preparation format remains unchanged.

The host supplies authorized per-record protection contexts and explicit runtime-type admission. Before constructing recovered entities, Core authenticates every envelope, checks operation/actor/authority binding, validates complete graph and condition coverage, admits every type, and checks prepared snapshot identities. Original revisions, graph flags, relationships and replication enrollment are retained. Recovery does not claim a preparation, execute a mutation, publish tracking state, or generate replacement ciphertext. The existing retry decoder still compares the entire original intent against the retry candidate.

Sixteen new controls cover all operation kinds, fresh key resolver and key rotation, exact integer/decimal values, denied contexts, substituted bindings, missing envelopes, malformed topology/conditions, inadmissible types and snapshot identity/type substitutions. Full Core: **3,449 executed, 3,449 passed, zero skips/failures**. Both `netstandard2.1` and `net10.0` builds pass. [Retained regression evidence](Verification/stored-preparation-core-full.trx.gz).

This internal decoder is not an authenticated hosted recovery endpoint. Remaining work includes read-only provider preparation lookup, durable hosted/delete intent enrollment with an authorized protection policy, host routing/admission, and actual process termination/restart tests. Existing claims must never be cleared or replayed to manufacture a status result.

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
