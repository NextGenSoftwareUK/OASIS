> Superseded by [current status](../CURRENT_STATUS.md); retained historical and technical reference.

# Provider-key deletion acceptance contract

## Actual commit acknowledgement loss - native verified

Run39009 injects acknowledgement loss only after the real native hard-delete commit succeeds, once per provider. MongoDB delegates the actual client/session then withholds the commit response; Neo4j reuses the real-driver commit proxy; SQLite uses the existing EF transaction-committed interception mechanism against the same owned database file. Each reports exactly one commit, COSMIC_WRITE_RECONCILIATION_REQUIRED, its reconciliation marker and original operation metadata, and no caller deletion publication. The healthy handles are restored, providers reconnect, and read-only receipt recovery verifies the committed operation without another mutation call. Current records and redacted receipt equality are checked.

This covers three actual acknowledgement-loss cases within the existing15 original conditional writes; the signed cohort remains453 settled operations/496 physical attempts. Sources were unchanged; seven producer/consumer pairs matched under lease; root/Core Git commits were held as build inputs. Owned services stopped and exact fixture/SQLite files are absent. Evidence: ../Evidence/Verification/recovery-ack-loss-*.

The test exposed a source-level diagnostic gap in Mongo's atomic facade: it manually copied errors but omitted metadata. The transaction owner now attaches COSMIC.OperationId and the facade forwards error diagnostics through the canonical OASISResult helper. All571 modern MongoDB unit tests pass, zero skips; evidence: ../Evidence/Verification/ackloss-mongo-full.trx.gz. The missing root unit assets were restored from the existing local package cache only, without downloads.

This is actual database commit/acknowledgement fault injection and reconnection recovery through trusted SDKs. OS-process crash/restart and authenticated hosted recovery remain open. Existing protected preparation retains Save-specific intent, but there is no read-only operation/actor lookup plus standalone reconstruction for hosted save/delete recovery. Do not substitute claim/re-execution or current holon state for that missing workflow.

## Routed recovery SDK - verified

Core `1832d0b` exposes HolonManager.ReadConditionalHolonWriteOutcomeAsync(original, authenticatedActor). It verifies actor before I/O, captures a detached original graph, pins the original authority, rejects unsupported capabilities and verifies returned operation/actor/authority/kind. HyperDrive dispatches a dedicated read operation with no conditional mutation enrollment, replication or failover. The ordinary tariff catalogue has an explicit recovery-read entry and a bounded18-row three-provider matrix.

Seven focused controls pass; the shared full Core3433/3433 report includes all seven (inspected XML, zero skips). Reuse [shared Core regression evidence](../../HyperDrive-Providers/NativeEvidence/Polkadot/shared-nft-core-current.trx.gz) and its source/binary record in stored-nft-lock-current.json. Real-Mongo usage402/402 passes: query tariffs do not authorize recovery reads, while an exact recovery-read tariff persists one bound attempt/receipt and its0.001USD charge.

Native79939 passes routed absent reads and direct-provider versus HolonManager committed-outcome equality for create/soft-delete/hard-delete receipts on all three providers after reconnection. The existing full cohort remains453 settlements/496 attempts. Sources stayed unchanged; seven producer/consumer pairs matched under lease; owned services stopped and exact fixture/SQLite files are absent. Evidence: ../Evidence/Verification/routed-recovery-* plus recovery-routing-focused.trx.gz.

This is trusted SDK routing and pricing-policy acceptance. An authenticated recovery HTTP endpoint, durable original hosted intent, actual lost native acknowledgement injection and OS-process restart remain open. The original provider request must be retained exactly; a current holon read or new CosmicWriteOptions is not a substitute for that original intent.

## Read-only recovery SDK - native verified

Core `b3d4df4` defines IConditionalHolonWriteRecoveryStorageProvider. MongoDB, SQLite and Neo4j implement it by reading durable receipts and reusing existing exact-intent validators. Status is bound to the original request's actor, operation, authority, selected graph and fingerprint. Results contain no stored graph or private field values. Absent is not proof of rejection or permission to replay; invalid/unavailable evidence returns an error.

Native run65539 passes all three stores: three original creates plus soft/hard deletes per provider (15 actual original writes), absent lookups before execution, committed receipt lookup after provider deactivation/reactivation, repeated equal status reads, foreign-actor rejection, summary redaction and retained/current deletion-state checks. Reads never re-execute an original. All IDs and original requests were journalled before writes. This is provider connection/lifecycle restart evidence, not OS process restart or a lost native commit acknowledgement fault.

The existing full native CQL/REST/GraphQL/gRPC cohort remains green:453 settled operations/496 physical attempts. Direct SDK recovery writes do not add HTTP usage settlements. Source snapshots stayed unchanged and seven producer/consumer pairs matched; owned services stopped and exact fixture/SQLite files are absent. Evidence: ../Evidence/Verification/conditional-recovery-*.

Regression gates: Core3420/0skip, Neo4j559/0skip (including seven new recovery cases), SQLite90/0skip. SQLite root test assets were restored from the existing cache without downloads. Core builds both targets; matching unit Core dependency identities are preserved. SQLite uses a separate ReadOnly nonpooled file connection; ephemeral private-memory stores explicitly reject durable recovery. MongoDB observes the primary with majority read concern. Neo4j uses MATCH only without claims/locks.

Generic routing/pricing, authenticated host integration, actual commit-acknowledgement fault injection, OS restart and durable recovery of original hosted intent remain open. Do not expose this trusted SDK surface as an unauthenticated status endpoint. Historical draft notes below are superseded by this checkpoint.

## Current verified gate - 26 October 2026

Signed REST logical-ID/provider-key deletion now passes on MongoDB, SQLite and Neo4j. Per provider, nine new cases verify missing logical identity/operation/revision before provider execution, key-to-ID mismatch, foreign creator denial, stale logical/key revisions, acknowledged logical soft deletion and acknowledged native-key hard deletion with committed delta. Persisted deletion state/actor, physical absence, original revision preservation after rejection, and unchanged unselected-child state/revision are checked.

The full joined cohort passes:63 signed REST requests (27 new deletes),48 GraphQL/gRPC requests,138 SDK queries and138 signed CQL walkthrough requests;453 settled operations and496 physical attempts. Settlement breakdown:345 CQL,60 REST and48 GraphQL/gRPC. Three REST requests exercise replay/changed-body behavior and do not add settlements. Every new delete checks exact attempt/receipt counts, outcome, actor, provider and tariff.

Run30646 exited0 with one native matrix test passed, zero skips. Sources stayed unchanged; seven producer/consumer identities matched under the build lease. Owned Mongo/Neo4j services stopped; exact manifest and SQLite fixture were checked absent. Required evidence is under ../Evidence/Verification/signed-rest-delete-*.

Remaining acceptance includes authenticated Wizard cases, unavailable-primary/replica and concurrent provider-scope changes, unknown-outcome/recovery across transport, full startup/schema enrollment and the wider ORM roadmap. Earlier source-only notes below are historical and superseded by this runtime gate.

Status: implementation in progress, 2026-10-26. Core SDK gate is verified at `5e483cb`; HTTP/native acceptance below is still pending. This supplements the existing roadmap rather than replacing its remaining scope.

## HTTP intent

Preserve the existing DELETE `/api/data/delete-holon-by-providerkey/{providerKey}` route and its legacy engine behavior. V2 must require canonical nonempty GUID headers `X-COSMIC-Holon-Id` and `X-COSMIC-Operation-Id`, plus canonical `X-COSMIC-Expected-Revision`. A revision equal to Guid.Empty is an actual token, not a create-only condition. Native keys must be nonblank and at most 4096 characters. Keep the existing two-argument action callable; any new provider selector must preserve that signature through delegation or a distinct HTTP entry point.

Validate intent before configuration, manager acquisition or provider I/O. Capture the authenticated actor and Wizard role before awaiting. Resolve the request provider once and scope configuration as on logical-ID deletion. Never use a caller-supplied role or owner. Ordinary deletion calls `DeleteOwnedHolonByProviderKeyAsync`; the authenticated Wizard policy still requires a pinned key observation, exact logical ID agreement and conditional delete with the original revision. Preserve provider failure codes/counts/metadata and committed deltas. Return the shared error-to-HTTP status mapping. No refresh of a stale revision, fallback authority or unconditional retry.

## Ordinary provider tariffs

The HTTP factory already rejects unpriced ordinary operations independently of route classification (`60adf1c`). Add explicit `LoadHolonByProviderKey` and `DeleteHolon` operation catalogue entries alongside existing operations. Bound the tariff matrix to three providers times five operations. Never alias delete to save or implicitly charge zero.

| Selected exact deployment route | Required ordinary operation tariffs |
| --- | --- |
| POST canonical save routes and existing save aliases | LoadHolon, SaveHolon, SaveHolons |
| DELETE logical-ID routes | LoadHolon, DeleteHolon |
| DELETE provider-key route | LoadHolonByProviderKey, DeleteHolon |
| POST gRPC SaveHolon | LoadHolon, SaveHolon, SaveHolons |
| POST gRPC DeleteHolon | LoadHolon, DeleteHolon |
| POST GraphQL mutation endpoint | LoadHolon, SaveHolon, SaveHolons, DeleteHolon |

Classification validates already enrolled exact routes; it never enrolls a prefix. Startup validates relevant operation rows against the selected route budget/catalogue. Runtime admission remains fail-closed for other selected routes. Existing save-only deployment profiles must not require unrelated key-read/delete tariffs.

## Required verification

- Missing/malformed/empty logical ID, missing operation/revision, invalid key and unenrolled alias: no manager/provider access.
- Key resolving to another ID: reject even when the actor owns both records; cover Wizard too.
- Stored foreign owner: forbidden; payload actor/role cannot override authentication.
- Stale original revision, changed default, unavailable primary with available replica: no refreshed condition or authority substitution.
- Soft and hard acknowledgement, failed/unknown outcome evidence, native committed delta and correct error/status publication.
- Complete 15-row tariff matrix accepted; duplicate/unknown/excess rows rejected; missing delete/key price rejected; existing save profile remains valid.
- Shipping MVC route coverage followed by signed real-provider MongoDB/SQLite/Neo4j matrix. Prejournal exact fixture IDs, freeze source/binary identities, verify attempt/receipt/settlement counts and teardown. A failed or unknown write requires exact reviewed reconciliation, never blind replay.

## Shared build handoff

Core/Shared/WEB4 sources and outputs were released to the provider session after gate 98572 (3412 Core cases, both target builds). Do not mutate or rebuild those shared resources until that consumer slot is explicitly returned. Prepare the HTTP contract and acceptance matrix here while it runs. This handoff is scheduling, not a product acceptance blocker.

## Source checkpoint

The V2 route, DTO/key validation, common conditional boundary, response-policy-preserving projection and five-operation tariff catalogue are now written. Ten tariff configuration controls and six MVC route controls are added. Diff whitespace checks pass. These source changes have not been compiled or tested yet: the peer released source mutation but retains frozen producer DLLs for its ONODE consumer gate. Native acceptance remains pending.

## HTTP and tariff verification

The shipping WEB4 build passes. Full WEB4 suite: 271 passed, zero failures, one unrelated Stripe sandbox test skipped because its explicit fixture credentials are absent. All six new MVC key-delete cases pass: malformed/missing/empty logical IDs reject before storage, key-to-ID mismatch returns400, foreign stored creator returns403, and an authorized stale hard delete returns409 while preserving the original revision/actor/operation. No unconditional delete call occurs. The first run exposed a null-payload response mapper exception; the alias now copies diagnostics without mapping absent inner records. Red and green reports are retained in ../Evidence/Verification/key-delete-web4-{red,green}.trx.gz.

The key route preserves its existing callable signature and supports optional `providerType` query scope through common intent validation. Its result projection preserves captured request routing policy. Legacy engine behavior remains unchanged. The new DTO ProviderKey also lets the canonical conditional-delete boundary share the same identity checks.

Usage suite: 400 passed, zero skips. Ten new configuration controls cover exact delete/key-read pricing, save-only compatibility and complete/duplicate/unknown tariff matrices. Evidence: ../Evidence/Verification/key-delete-usage-full.trx.gz. Signed native delete acceptance is still pending and must not be inferred from these mock-provider MVC cases.

## Signed GraphQL/gRPC native delete gate - 2026-10-26

Run98189 passed across MongoDB, SQLite and Neo4j. The focused shipping GraphQL schema now includes the actual DeleteHolon resolver. Per provider, four GraphQL soft-delete and four gRPC hard-delete requests cover missing intent, foreign-owner denial, stale revision rejection and acknowledged deletion. GraphQL soft deletion is verified through persisted deletion state/actor; gRPC hard deletion returns its committed delta and the physical record is absent. Every request checks durable attempt/receipt counts, settled outcome and exact operation tariff. No write retries were introduced.

Combined native cohort: 48 signed GraphQL/gRPC requests (24 existing saves plus24 deletes);36 signed REST save requests;138 SDK and138 signed CQL walkthrough cases;426 settled operations and466 physical attempts (345 CQL,33 REST and48 transport settlements). Source snapshots stayed unchanged and seven producer/consumer binary identities matched while the build lease was held. The native test passed with zero skips. Owned Mongo/Neo4j services stopped; exact manifest and SQLite fixture are absent after verified cleanup. Required evidence is preserved under ../Evidence/Verification/signed-delete-*.

This certifies the stated GraphQL/gRPC delete cases only. Signed REST logical-ID/provider-key deletion, Wizard/provider-scope concurrency, unavailable-primary behavior and unknown-outcome transport recovery remain open. It is not full startup/GraphQL schema or full roadmap certification. Existing SDK/MVC tests remain separate evidence cohorts.

## REST source extension pending verification

Seven cases per provider are prepared in the existing signed REST fixture: missing logical ID, native key resolving to another intended ID, foreign stored creator, stale logical revision, stale key revision, acknowledged logical soft deletion, and acknowledged key hard deletion with committed delta. The host enrolls only the exact observed key path and canonical logical path, using explicit read/delete tariffs. Existing journalled records are reused; no new fixture identities or databases are introduced. Expected counts are updated to57 REST requests and54 settlements. These tests have not been compiled/run yet while the peer owns the producer freeze; the earlier signed GraphQL/gRPC acceptance remains the verified result.

REST pending matrix now has nine cases per provider (27 total), adding missing operation and missing original revision with zero provider attempts. It also verifies that root-only deletion preserves the unselected child's deletion state and provider revision. Expected joined totals are63 REST requests/60 REST settlements,453 total settlements and496 physical attempts. These remain unverified source expectations until the native gate executes.

Next prepared controls (not yet verified): ten hosted Wizard/actor-role snapshot cases and four Core failed-authority/available-replica cases. The latter explicitly enable failover and supply a replica with a creator-owned copy; deletion must preserve the original authority error, make no replica calls and submit no conditional mutation.

## Hosted role and failed-authority controls - verified

Ten hosted delete cases now pass for logical/key and soft/hard deletion. The authenticated actor and Wizard role are captured before the authority read: later mutation of the avatar object cannot escalate an ordinary user or revoke the captured Wizard policy. Wizard access to a foreign creator still retains the original actor, operation, provider, original revision and delete choice; key-to-ID mismatch rejects before mutation even for a Wizard. These controlled-provider tests validate the shared hosted boundary, not external identity provisioning or signed native Wizard enrollment.

Four additional Core controls explicitly enable failover and offer an available replica containing an actor-owned copy. When the selected authority fails, logical/key soft/hard deletion makes no replica calls and no conditional mutation, preserving the original error code, count and diagnostic metadata.

Full Core:3416 passed, zero skips. Full WEB4:281 passed, zero failures, one unrelated Stripe sandbox credential skip. Compressed reports are ../Evidence/Verification/delete-authority-{core,web4}-full.trx.gz. No production code change was necessary for these invariants. Initial WEB4 fixture compilation selected a same-name local Avatar model; the test now explicitly uses the Core identity type. Native cross-provider scope concurrency, unknown outcomes, full startup/schema and wider roadmap gates remain open.

## Concurrent hosted provider scope - verified

Four deterministic barrier-controlled cases cover logical/key soft/hard deletion. A SQLite authority read remains paused while a MongoDB request completes; the shared registry publishes MongoDB as the new default and the first transport DTO changes identity, key, provider, operation, revision and delete choice. On release, each conditional write retains its original provider, actor, logical ID, operation, expected revision and soft/hard operation. Both provider calls deliberately reject at their conditional boundary, preventing unrelated fixture persistence. All waits are bounded and the suspended request is released/observed in finally.

Focused4/4 passed. Full WEB4285 passed, zero failures, one unrelated Stripe sandbox credential skip. Evidence: ../Evidence/Verification/delete-concurrency-focused.trx.gz and delete-concurrency-web4-full.trx.gz. No production source change was needed. These controlled-provider concurrent hosted cases do not certify simultaneous native network outages, signed Wizard enrollment or unknown-outcome recovery. Those and full startup/schema/wider roadmap gates remain open.

## Uncertain provider delete acknowledgement - controlled SDK gate

Four logical/key soft/hard controls pass when the conditional provider declares COSMIC_WRITE_RECONCILIATION_REQUIRED with the original operation metadata and INativeMutationReconciliationRequired transaction identity. The result preserves the same exception, error count/code, original transaction and operation; the provider is called exactly once, and the observed caller record retains its prior version/revision and undeleted state. The manager makes no unconditional retry or synthesized success publication.

Focused4/4 and fullCore3420/3420 pass, zero skips. Evidence: ../Evidence/Verification/delete-unknown-focused.trx.gz and delete-unknown-core-full.trx.gz. No production correction was needed. This proves SDK propagation and save-state ownership for a provider-declared uncertain result. It does not prove durable host recovery or an actual lost database acknowledgement: transport/usage recovery and native fault-injection/reconciliation remain required gates.

## Recovery owner audit - remaining implementation gap

Current source separates service-attempt accounting from mutation outcome. PreparedStorageExecution records a configured ordinary-operation receipt when the invocation returns; the HTTP ledger can therefore know the service charge even when the returned COSMIC_WRITE_RECONCILIATION_REQUIRED result declares database uncertainty. A known attempt charge is not evidence that the mutation failed or succeeded. Do not suppress known billing evidence or invent a provider completion receipt when invocation itself throws.

The shared IConditionalHolonStorageProvider exposes execution only, and IConditionalHolonDeltaStorageProvider is a capability marker. Neither currently exposes a read-only status/receipt lookup for the original operation. The three providers implement durable conditional-write receipts internally, but hosts have no generic status interface through which to reconcile an uncertain delete without calling the mutation again. This is an open recovery API gap; SDK error propagation tests do not close it.

Required implementation: one shared read-only operation-status capability, with actor/authority/operation binding and provider-specific durable receipt verification. Distinguish verified committed acknowledgement, authoritative terminal rejection if evidence exists, absent receipt, and unavailable/uncertain evidence. An absent receipt alone is not proof of non-execution. A status query must not submit, retry, create a new operation, refresh expected revisions or change provider authority. Preserve the original operation/transaction and return OASISResult errors. Expose an authenticated host boundary only after receipt authorization and capability checks are verified. Test acknowledgement loss after actual commit and process restart against MongoDB, SQLite and Neo4j; retain exact original fixtures until read-only reconciliation proves their state.

Source owners inspected: Core Interfaces/Providers/IConditionalHolonStorageProvider.cs and IConditionalHolonDeltaStorageProvider.cs; Services/Subscriptions/PreparedStorageExecution.cs; Managers/HolonManager/HolonManager-Tracking.cs; provider conditional implementations. This finding requires implementation and native acceptance, not merely an endpoint returning current holon state.

## Recovery capability source draft

Core now has an uncommitted IConditionalHolonWriteRecoveryStorageProvider contract requiring the original ConditionalHolonWriteRequest. Its immutable redacted result distinguishes Absent from Committed and carries the original actor/authority/operation binding; unavailable or invalid evidence must be returned as an OASISResult error. This permits reuse of exact receipt fingerprint/topology validation. Both Core target builds pass after explicitly qualifying System.Enum for the portable target. No provider advertises this capability yet, and no status host endpoint is implemented or certified. The preceding audit describes the published capability gap; this source draft does not close it.

Receipt execution lookups cannot be called as status reads: Neo4j's execution reader uses MERGE and claim locks, SQLite's write path reserves an IMMEDIATE writer transaction, and Mongo preparation touches operation fencing. Recovery must use read-only lookup and reuse the existing pure receipt decoder/validator. It must never execute the original mutation merely to retrieve a receipt.

Neo4j provider source now implements the draft recovery capability with MATCH-only lookup, duplicate receipt rejection and the existing ConditionalHolonReceipt decoder. Preparation-only state returns Absent (not rejection/retry permission); a valid completed envelope returns Committed plus redacted delta summary. Seven controlled-provider tests are prepared. This source is not yet compiled or advertised in a published provider build; MongoDB/SQLite, routing/host integration and native restart/fault acceptance remain open. Compilation waits for the peer's bounded Core/Shared consumer freeze to return.

## Recovery provider source/build checkpoint

Neo4j provider/consumer builds pass; all seven new recovery controls and full559 unit tests pass with zero skips. The test host's missing generated assets were restored from the existing local package cache only, without downloads. Evidence: ../Evidence/Verification/neo-recovery-{focused,full}.trx.gz.

MongoDB recovery now builds with primary/majority reads of the existing CosmicWriteReceipt collection and the shared pure receipt validator. SQLite recovery also builds: a separate nonpooled ReadOnly file connection runs SELECT only and invokes the existing SQLite receipt fingerprint/envelope/graph/delta validator. It does not save, detach context entities, reserve an IMMEDIATE writer transaction, create a missing database, or observe uncommitted writer-context receipts. Private in-memory authorities explicitly reject durable recovery rather than fabricate an absent result.

These implementations and the additive Core contract remain uncommitted. MongoDB/SQLite unit/native validation, Neo4j actual receipt/restart/fault validation, generic routing/admission, and authenticated host integration remain pending. A compilation result is not native recovery certification.

## Generic recovery routing - focused controls verified

HolonManager.ReadConditionalHolonWriteOutcomeAsync(original, authenticatedActor) now captures a detached original graph, verifies actor and explicit original authority, and dispatches a dedicated HyperDrive read operation with required provider/replication disabled. Unsupported provider capabilities reject before execution. A returned status must match actor, authority, original operation ID and operation kind. The recovery payload is not enrolled as a conditional mutation. The ordinary tariff catalogue adds an exact read-status operation and is bounded to18 entries across the three target stores.

Seven focused controls pass for save/soft/hard detached original capture, actor/legacy rejection, unsupported capability and mismatched returned binding. No mutation method is called. This uncommitted routing batch still needs portable/full regression, real receipt lookup through the manager and configured HTTP admission/pricing coverage; host recovery is not yet delivered. Provider-level native checks above remain the published evidence cohort.
