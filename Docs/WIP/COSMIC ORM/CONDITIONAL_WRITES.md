# Conditional writes and cross-client concurrency

Updated 19 October 2026. Implementation is local and in progress. SQLite and MongoDB now have local native implementations; remaining provider certification, fault injection and durable replication remain open.

20 October primary-receipt follow-on: new SQLite acknowledgements use an integrity-checked format2 envelope, the existing Shared graph codec, operation/context binding and exact saved-intent validation. Historical format1 receipts require explicit reconciliation before replay; the historical checkpoint below describing format1 is superseded for new writes. See [receipt contract, rollout gate and verification](SQLITE_RECEIPT_INTEGRITY.md).

The subsequent [committed-delta checkpoint](COMMITTED_DELTAS.md) extends new primary envelopes to format3, persists the delta in the same transaction and returns a redacted descriptor summary on Save/replay. Valid format2 acknowledgements remain readable with explicit missing-delta evidence; format1 still requires reconciliation.

## Provider-local revisions and logical history

Concurrency tokens are now separate from logical `VersionId`/`PreviousVersionId`. SQLite owns a physical `ProviderRevisionId`, exposed through `CosmicConcurrency.GetProviderRevisionId(holon, authority)` using reserved provider metadata. Ordinary copies and replication preserve logical version identity while each SQLite write issues a fresh local token. Logical version IDs cannot serve as expected concurrency tokens.

## Contract

Use `manager.SaveHolonAsync(holon, actorId, new CosmicWriteOptions(operationId))`, or the corresponding synchronous/batch overload. Keep the operation ID stable for an exact retry. A new logical edit needs a new operation ID. Existing overloads retain unconditional write semantics.

Tracked records use their private accepted provider revision for the selected authority, never the mutable public revision or `Original` view. A new record expects absence. An untracked record with an allocated ID requires `ExpectedRevisions`: a null value means create only; `Guid.Empty` means an existing legacy revision. `ConditionalHolonWrite.Authority` binds the command to that provider. Every selected child needs a condition. Conditions outside the selected graph are rejected. Generated identities are stable for the same operation ID and selected graph traversal, and remain detached until acknowledgement publication.

`IConditionalHolonStorageProvider` is an explicit capability. Hyperdrive rejects an unsupported authority before request-driven activation or mutation. It preserves the selected authority, disables predictive provider substitution, and does not fail over or replicate the conditional command. A conflict at one authority cannot safely be retried against a lagging replica. Durable replication must later use acknowledged content and independent target preconditions.

No-op saves retain the existing behavior: no I/O, revision or receipt. A successful save publishes only acknowledged values; a conflict retains pending local changes and the accepted baseline. The exception `CosmicConcurrencyException` contains the selected identity, expected revision and actual revision/version for each conflict. Check `IsError` and `ErrorCode` before using result payloads.

## Native SQLite invariant

One `BEGIN IMMEDIATE` transaction owns receipt lookup, authoritative revision reads, comparison of all selected records, the graph write and receipt insertion. Competing contexts cannot both validate the same revision and commit. SQLite's native writer reservation supplies the cross-client exclusion; there is no process-local lock pretending to provide it. See [Microsoft's transaction documentation](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/transactions).

Conditional saves assign version 1 for creation and persisted version + 1 for updates, preserve creation audit fields, and issue a new revision token. Unrelated pending EF context changes are rejected. Selected cached EF entities are detached before the authoritative read. Failed writes roll back records and receipt together and detach selected context state; an uncertain commit/rollback produces a reconciliation-required exception.

The durable `CosmicWriteReceipt` table and local token column are maintained for migrated and EnsureCreated databases. EF migration `20261019191942_AddHolonProviderRevision` preserves existing logical lineage and initializes old rows with the known legacy token `Guid.Empty`. Downgrade uses a transactional SQL guard that refuses populated rows or receipts without leaving the EF migration lock held. Receipt content includes an explicit graph envelope and the exact committed acknowledgement. Replay returns that acknowledgement even if later operations changed the records. Reusing an operation ID with different content or actor is rejected. The provider computes the request fingerprint itself; manager-assigned timestamps and revision hints are excluded. Canonical object key ordering makes metadata dictionary ordering irrelevant.

## Current boundaries and remaining acceptance

- Conditional save and soft/hard deletion are implemented in SQLite. Manager deletion overloads accept a tracked holon or explicit expected revision and delete that record, without implicitly deleting descendants. Deletions always check the authority even when the local object is clean. Soft deletion persists a new revision; hard deletion returns a versioned tombstone acknowledgement retained in the receipt. Stale deletion fails without modifying the newer record.
- Normal SQLite saves and soft deletes also rotate their local revision token. The persistence layer owns that token; callers cannot recycle an earlier token. Ordinary replication preserves primary logical version IDs; destination-local tokens remain independently owned.
- Atomicity covers the selected graph in one SQLite database. It does not provide a transaction across independent providers or chains.
- Receipt replay requires the same prepared intent and conditions. Changing a graph's membership/order or encryption ciphertext can change that intent; do not reuse an operation ID for modified requests. Durable encryption preparation and API-level retry-intent binding remain to be implemented.
- Actor identity participates in receipt binding and audit; this capability does not itself authenticate the actor or implement tenant authorization.
- Receipt retention/compaction, administrative reconciliation API, cancellation during native SQLite I/O and delta/history linkage remain roadmap work.
- Native SQL issued outside the provider must follow the revision protocol. Arbitrary external SQL cannot be made safe by client-side tracking.

## Verification

The current cases cover private-baseline conditions, stable new graph identities, untracked/extra-condition rejection, unsupported authority rejection before request-driven activation, independent clients, restart receipt replay, operation-ID content mismatch, raw provider intervening writes, failed child rollback and exact retry, manager publication across legacy/V2 and sync/async, simultaneous native writers, and whole-graph rejection on a stale child. Full-suite results are recorded in [implementation status](IMPLEMENTATION_STATUS.md) and [verification](VERIFICATION.md) after each verified batch.

## MongoDB token prerequisite — completed before the conditional adapter

Ordinary MongoDB holon save/update/import/graph writes now own a physical `ProviderRevisionId`. Every write issues a new token independently of submitted provider metadata or logical `VersionId`. The public DTO observation is authority-scoped through `CosmicConcurrency`; a row predating the field maps to known legacy token `Guid.Empty`. No collection rebuild is required. Native avatar collections do not acquire holon conditional-write guarantees from this change.

Provider metadata is copied during DTO/native mapping so revision preparation cannot modify the caller's submitted observation. Graph transaction preparation now resolves all selected native identities, immutable creation audit values and local tokens before building the acknowledged graph or serializing ancestor embeddings. The acknowledgement therefore represents prepared native records rather than submitted child snapshots.

Native assertions verify single-root intervening token changes, unchanged logical version identity, all selected child acknowledgement tokens against their persisted physical fields, and retained unselected descendants. Unit mapping verifies that physical state overrides a spoofed observation without mutating its input map and that missing physical tokens map to legacy zero. Latest suites: 52 selected native transaction cases and 72 MongoDB unit cases pass.

MongoDB now implements `IConditionalHolonStorageProvider` through the transaction owner described below. The previous unsupported-capability boundary is superseded for this provider; other unsupported authorities must still reject the guarantee. Arbitrary external MongoDB mutations must follow the provider token protocol.

## Native MongoDB conditional transaction

`ExecuteConditionalHolonWrite[Async]` admits an immutable shared conditional plan before repository access. Wrong authority, missing selected-node conditions and pre-cancelled calls fail before activation/I/O. Native verification uses the retained replica set; conditional operations require MongoDB native transaction support and never downgrade to ordinary writes on unsupported topology or failure.

The existing graph import transaction is the single commit owner. It looks up `CosmicWriteReceipt` by operation ID first, validates its fingerprint and exact acknowledgement, and returns it before checking current records. Otherwise it reads every selected physical token under a snapshot, rejects all stale/missing/create-only conflicts, prepares provider-owned logical revisions/audit/local tokens, constructs the selected acknowledgement, serializes all records, preserves excluded descendants, and writes records plus the receipt in one majority/journaled transaction. Soft and hard deletion use the current persisted content for tombstone acknowledgements. Hard deletion removes only selected records; it does not implicitly delete descendants.

Receipt replay survives provider reconstruction and intervening ordinary writes. Reusing an ID for changed content or actor returns `COSMIC_OPERATION_ID_REUSED`; malformed/corrupt acknowledgement data returns `COSMIC_RECEIPT_INVALID` before mutation. The receipt checksum detects corruption; it is not a signature or an authorization mechanism.

Expected-token mismatches return `COSMIC_CONCURRENCY_CONFLICT` with expected/actual token and logical revision data. A native write conflict or duplicate-key race before commit returns `COSMIC_CONCURRENT_WRITE`; reload or retry the exact stable intent explicitly as appropriate. There is no automatic transaction replay. Exact concurrent replay may return a receipt immediately, or a losing caller can explicitly retry after a known aborted attempt. Other known failures return `COSMIC_CONDITIONAL_WRITE_FAILED`; pre-commit cancellation has `COSMIC_CONDITIONAL_WRITE_CANCELLED`. Once commit dispatch is entered, failures carry native session/transaction reconciliation evidence and `COSMIC_WRITE_RECONCILIATION_REQUIRED`, rather than permission to submit a new mutation.

Native tests cover sync/async paths, graph creation, stale-child whole-graph rejection, source isolation, logical revision ownership, provider reconstruction, receipt replay after intervening writes, changed-intent/actor rejection, competing clients, create-only and same-operation races, forced receipt insertion failure with full record rollback, exact retry, receipt corruption, legacy missing physical tokens, soft/hard deletion and V2 manager clean/pending state publication. The suite has **two conditional theory cases**, each exercising this scenario sequence; the full selected native suite has **54 cases**. MongoDB unit suite: **75 cases**.

Remaining gates: repeatable process-crash/drop-response recovery harnesses, original transaction reconciliation tooling, receipt retention/compaction, endpoint/tenant policy and authorization, and durable target replication/outbox coupling. No cross-database/provider atomicity or universal exactly-once delivery is claimed. Cancellation is accepted by the async native adapter; broader manager cancellation surfaces remain roadmap work.

## Native post-commit acknowledgement fault

The owned MongoDB runner now enables test commands for the conditional/full native modes. The fault is scoped to the synthetic client's `appName`; it injects native write-concern error 64 on `commitTransaction` after the server executes the commit. Command monitoring verifies three inserts for the two-node graph plus receipt, no abort after uncertain commit dispatch, and no caller identity/version publication on the error. The returned error retains the original session/transaction and native cause.

The transaction owner now explicitly refuses to issue `abortTransaction` once commit dispatch has begun. Before dispatch, failed preparation or mutation still aborts normally with cancellation-independent cleanup.

The test resolves the receipt through an independent bounded majority read with `afterClusterTime` set to the original native commit reply's `operationTime`. An unqualified immediate majority read can legitimately see an earlier majority snapshot after a failed acknowledgement; no sleep or retry substitutes for the causal barrier. Only after the receipt and persisted logical/local revisions have been independently verified does the test replay the exact stable intent to publish the acknowledged graph. Monitoring proves this issues no additional inserts or commits.

Both sync/async scenario sequences pass in the full 54-case native suite; MongoDB unit suite remains 75 passing cases. An initial missing-receipt assertion retained the fixture. After the runner force-terminated and restarted its owned process, independent majority inspection verified six synthetic rows and 14 valid receipts, including the original uncertain commit, before removing only that owned database. This provides concrete forced-restart durability evidence for that retained fixture. A dedicated repeatable crash/drop-response recovery harness and supported operator-facing reconciliation API remain open; post-commit write-concern failure is not represented as every transport-failure mode.

## Neo4j conditional writes — locally implemented and verified

Neo4j now implements `IConditionalHolonStorageProvider` for synchronous/asynchronous save, soft delete and hard delete. The existing native graph transaction owns the selected graph, edges, optional history and durable original acknowledgement receipt. Receipt lookup precedes revision checks; an exact retry returns the original result even after intervening writes. Changed intent/actor and corrupt receipts fail explicitly.

Activation inspects actual native constraint shapes, provisions missing guarantees, and verifies uniqueness of `Holon.Id`, `CosmicWriteReceipt.OperationId`, and `OasisRecordVersion (Kind, RecordId, Version)`. Existing equivalent constraints avoid unnecessary DDL. Rejection leaves the provider inactive with the native cause; existing duplicate data is never removed implicitly. Runtime credentials require schema inspection, and provisioning missing constraints requires DDL permission. A native low-privilege permission matrix remains unverified.

The transaction claims the operation receipt and acquires native node locks in sorted holon-ID order, including create-only identities. It compares every selected physical revision before preparing mutations. Native physical revisions are independent of logical versions; missing legacy physical tokens mean known zero. Conditional logical versions are provider-owned (previous plus one), including when history is disabled. Creation audit comes from persisted content; caller version hints cannot choose the committed revision. Stale children reject the whole graph. Hard deletion removes selected nodes and their history while retaining unselected descendants.

`Neo4jOASIS.Conditional.cs` supplies admission, receipt binding, revision comparison and error classification; `Neo4jOASIS.HolonSnapshot.cs` remains the shared native mutation owner. Validated committed acknowledgements survive subsequent cleanup failures as success with a warning. Definite rollback, concurrency conflict and uncertain commit remain distinct. Once commit is dispatched, the provider does not abort or automatically replay the transaction. Uncertain outcomes expose `Neo4jConditionalReconciliationException` and the durable operation ID. The installed driver exposes no native transaction ID, so that field remains empty. Cancellation is cooperative between commands and before commit; active native query cancellation is not claimed.

Verification: **117 unit and 134 native cases pass**, zero failures/skips. Four conditional theory cases cover sync/async with history on/off, cross-client update/create races, exact-operation races, replay after provider reconstruction/intervening writes, stale children, version exhaustion rollback, receipt corruption, legacy-token upgrade, delete replay, and V2 caller publication/pending conflicts. Three native schema races cover all identity guarantees.

Each conditional theory also delegates a real SDK commit and then injects an acknowledgement exception at the application boundary. Independent native inspection verifies the committed receipt and graph before retry; callers remain unpublished until exact receipt replay, which performs no second commit. This proves post-commit application acknowledgement recovery, not an actual transport drop or process crash. Two unit cases separately verify receipt-read cleanup warnings without another cleanup attempt.

Remaining gates include actual transport/process-crash recovery, operator reconciliation, receipt retention and authorization/tenant boundaries, explicit replica-versus-edit history intent, graph unlink/reparent consistency, and additional providers. Receipt checksums are integrity checks, not authentication. Durable cross-provider outbox/delivery guarantees remain open. Legacy Hyperdrive issues are documentation-only.

## Neo4j conditional relationship ownership

Selected child records now own their incoming native parent edge even when saved as roots. Parent reassignment/unlink, cycle validation, snapshot/token/history and receipt share the existing native transaction; an invalid cycle leaves no receipt claim. Unselected descendants and omitted children are preserved. Parent snapshot tokens do not serve as aggregate membership tokens. See [child certification and remaining relationship gates](CHILD_HOLON_CERTIFICATION.md#neo4j-selected-child-parent-assignment).

## Shared Neo4j holon transaction outcomes

The native graph mutation owner now uses common transaction state for ordinary and conditional writes. Before commit dispatch, a failure awaits explicit rollback; after dispatch it never aborts or automatically replays. Confirmed rollback is a definite failure, failed rollback/commit acknowledgement requires reconciliation, and a validated committed acknowledgement survives later disposal errors with a warning. Disposal preserves all original and cleanup causes rather than masking one with another. Single-holon save wrappers propagate the warning and saved count.

This does not add receipts to ordinary saves, extend the guarantee to Avatar/AvatarDetail-specific writers, or certify real network/process interruption. Conditional receipt-first replay and its authority/content binding remain unchanged. See the [implementation checkpoint](IMPLEMENTATION_STATUS.md#neo4j-ordinary-holon-transaction-outcomes).

Latest shared Neo4j owner verification: **117 unit and 158 native cases pass**, zero failures/skips. See [current evidence](VERIFICATION.md#neo4j-ordinary-transaction-outcome-checkpoint--19-october-2026). Earlier suite counts above identify earlier checkpoints.

## Explicit replica intent shares the native owner

The canonical Neo4j receiver now applies committed replicas through a distinct Core capability and the same atomic graph/receipt owner. It preserves committed logical/audit identity, checks durable source/local lineage and retains a replica head through hard deletion. The existing edit condition/fingerprint semantics remain unchanged; edit-command replication is still rejected. See [replica contract and current 163-case native evidence](COMMITTED_REPLICAS.md). Atomic outbox enrollment and other receiver implementations remain open.

## SQLite persisted extension and replay lifecycle

SQLite now retains additional common-holon persistent fields/provider metadata outside its relational columns. Two new conditional sync/async cases verify exact extension acknowledgement replay after a later write and changed-intent rejection. Replayed committed nodes no longer inherit the constructor's new-record flag. The receipt's persistent format is retained. Full SQLite verification now passes 597 native cases; receiver/outbox parity remains open. See [mapping contract](SQLITE_PERSISTENT_VALUES.md).

## SQLite shared atomic owner and explicit replica intent

SQLite now applies committed replicas through the same IMMEDIATE graph transaction owner as conditional edits. Existing edit request/hash/receipt format is retained; intent is captured before I/O and exact replay normalizes committed lifecycle state. Captured child topology, complete pre-commit acknowledgement validation, native rollback, commit dispatch and cleanup ownership prevent later caller/cleanup changes from altering the durable result. Replica receipts use the shared content-bound codec and durable per-record heads. See [receiver semantics and evidence](SQLITE_COMMITTED_REPLICAS.md). Direct conditional command replication remains unsupported until independent target application and atomic source delivery enrollment are integrated.


## SQLite exact intent fingerprints — 20 October 2026

Precision audit found that SQLite still reparsed captured intent through the default double reader and omitted every provider's keys/revisions. The conditional owner now fingerprints the already captured canonical Holon snapshot and uses the shared exact-number hash owner, without recapturing live inputs. Only this authority's assigned current key/revision and persistence-assigned audit/version fields are excluded. Foreign provider keys/revisions and previous-version keys remain persisted write intent. Equivalent numeric scale and JsonElement/CLR representations normalize without double conversion; strings remain strings.

The corrected hash has explicit fingerprint domain3 (plain intent) or4 (submitted relationship intent), distinct from historical rounded hashes. New SQLite primary receipts retain acknowledgement format1 and add a fingerprintFormat marker. An unmarked/obsolete receipt returns COSMIC_RECEIPT_FINGERPRINT_RECONCILIATION_REQUIRED before another mutation; there is no rounded-hash fallback or automatic second write. Operators must reconcile the original committed operation before retrying with a new intent. This compatibility gate remains an operational rollout requirement before deployment.

Nine native regressions cover changed tiny decimals, nested values, foreign keys/revisions/prior keys, equivalent numeric/local-observation controls and historical receipt refusal without mutation. The original gate fails6/passes3, establishing the defects and controls. Final native acceptance is recorded separately.

Final fingerprint acceptance: focused9/9 passes2s and fullSQLite963/963 passes122s, zero skips, with fixture absent after teardown. Shared exact-hash API dependency83d6523cc has its own856/856 report on matching recorded Core/Shared hashes. No Core source/pin change or deployment is included in this SQLite caller batch.

## Encrypted preparation replay defect — 20 October 2026

Historical red checkpoint: the subsequent [shared V2 recovery implementation](WRITE_PREPARATION.md#current-v2-save-flow) now passes the original four-case regression and selected expanded native cases. The original failure evidence and acceptance requirements below remain useful; they do not describe the latest SQLite retry behavior.

Native regression `CosmicEncryptedConditionalReplayTests.EncryptedConditionalCreateReplaysOriginalReceiptAcrossFreshManagers` reconstructs both provider and manager, submits an identical new-record input with the same actor and operation ID, and requires the original ID, logical version, physical revision and decrypted metadata acknowledgement. Both synchronous and asynchronous unencrypted controls pass. Both authenticated-encryption cases fail with `COSMIC_OPERATION_ID_REUSED`. Focused evidence: `TestResults/SQLiteCurrent/encrypted-replay-red.trx`, four executed, two passed, two failed, zero skipped; build succeeds. Core source revision: `25a15a846c2759af64a5d20e11a73dd92857fd85`.

The manager generates a fresh AES-GCM nonce during each preparation. SQLite correctly binds its receipt to the actual prepared ciphertext, so repeated logical plaintext is different submitted content. Existing provider receipt replay is therefore insufficient to promise encrypted logical-operation replay across fresh clients. This is an open correctness defect, not a passing acceptance gate. The new regression intentionally remains red until the preparation owner is implemented.

Required invariant: a logical operation has one durable, authorized prepared request, bound to its authority, actor, operation kind, selected graph, conditions, relationship intent and encryption contexts. A retry with exactly the same logical intent reuses the original prepared bytes; changed intent is rejected before mutation. Independent clients racing to prepare must converge on one winner. Losing preparations must never be dispatched. A crash after preparation persistence but before mutation must be recoverable.

Implementation must retain random nonces and full provider request fingerprinting. Removing ciphertext from fingerprints, deriving deterministic GCM nonces, keeping a process-local cache, or accepting operation IDs without comparing logical intent would break the invariant. Private intent must not be persisted as plaintext or as an unkeyed digest that exposes low-entropy values. Preparation protection requires an explicit, domain-separated authenticated format and authorized key resolution; children with separate contexts must retain those boundaries. Preparation lookup must pass the normal V2 authorization/routing boundary rather than bypassing it through manager-owned direct database access.

Remaining acceptance cases: same-intent concurrent clients; changed actor/conditions/private or clear metadata/graph; key rotation with retained original key; missing or denied historical keys; tampered preparation; failure before and after durable claim, dispatch and commit; original receipt after intervening writes; sync/async single and batch graphs; supported providers and explicit capability refusal. Existing ordinary encrypted-save and direct immutable-wire receipt tests do not prove these cases.

The subsequent [durable preparation increment](WRITE_PREPARATION.md) adds shared protection and an immutable provider-neutral claim contract plus native SQLite first-winner persistence. Initial eight native claim cases pass, including independent clients and provider reconstruction. The manager/V2 logical-intent and prepared-graph recovery path is still required before these encrypted retry failures can pass.
