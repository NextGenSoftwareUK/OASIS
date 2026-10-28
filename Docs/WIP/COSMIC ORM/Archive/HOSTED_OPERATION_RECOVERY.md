> Superseded by [current status](../CURRENT_STATUS.md); retained historical and technical reference.

# Authenticated hosted operation recovery

26 October 2026. Hosted protection enrollment, signed native HTTP recovery, protected public-manager hard-delete acknowledgement-loss and actual writer-process interruption/fresh-process recovery are verified on MongoDB, SQLite and Neo4j. The full ORM/CQL roadmap remains open.

## Request

`POST /api/data/recover-conditional-operation` accepts only an original operation ID and an explicit named provider. MongoDB, SQLite and Neo4j are the current provider focus.

```http
POST /api/data/recover-conditional-operation
Authorization: Bearer <existing WEB4 token>
Idempotency-Key: <key for this recovery observation>
Content-Type: application/json

{
  "OperationId": "<original mutation operation UUID>",
  "ProviderType": "MongoDBOASIS"
}
```

The body retains the original mutation identity. Billing/idempotency identity belongs to the recovery observation: a later fresh observation should use a new observation key. Reusing the original mutation identity does not execute that mutation.

The authenticated actor comes from the host's existing avatar context. A request cannot supply another actor, a key, an encryption context or a runtime contract. Wizard status does not override original-operation ownership. Default, All, None and numeric provider selectors are rejected.

## Host authorization

Register an application implementation of `ICosmicOperationRecoveryPolicy` through dependency injection. Its `Authorize(authenticatedActor, authority)` returns an `OASISResult<CosmicOperationRecoveryGrant>` or an explicit denial. The immutable grant binds that actor and authority, supplies authorized per-record protection contexts, and admits an explicit bounded catalogue of concrete holon types.

Use the application's authorized `IHolonMetadataKeyResolver`; do not derive keys from request data or generate replacement keys during recovery. The grant supports mixed record contexts through its per-record callback. Runtime admission compares exact type and assembly names against the host's catalogue and does not dynamically load request-selected types. A mismatched grant is rejected before manager acquisition.

An enrolled recovery route requires this service at startup. Readiness checks service composition without authorizing a real actor, resolving a key, activating a provider or making a native call. An unenrolled host does not acquire a default key or policy.

Readiness now resolves an enrolled policy in a short disposable DI scope. Scoped policies are disposed after composition validation, while application singletons remain owned by the root container. This supports `AddScoped<ICosmicOperationRecoveryPolicy, ApplicationRecoveryPolicy>()` under scope validation and prevents a scoped policy being retained until host shutdown when validation is disabled. Unenrolled or cancelled checks construct no policy. Request authorization still occurs in the request's own scope.

Registering this service now also enrolls V2 hosted writes through the shared single-save, batch-save and delete boundaries, including REST aliases/provider-key deletion and the GraphQL/gRPC wrapper. The grant is applied to a fresh request-owned manager, never the shared manager. Each selected record is admitted against the same runtime catalogue and authorized key context before a preparation claim or mutation. New IDs are assigned by the existing graph planner before the callback; unselected descendants never reach it. A denied/null record policy cannot fall back to global protection or an unprotected write.

Core exposes `PreparationPolicyResolver` with an immutable `HolonPreparationPolicyContext` containing the assigned ID and actual runtime type, without handing the callback a mutable entity. Request contexts retain the callback observation; later parent changes cannot replace it. The callback is excluded from JSON serialization. Existing explicit-policy SDK behavior is preserved when no application service is configured. Legacy paths are unchanged.

Preparation protects the private original-intent journal. It does not change the primary record's metadata encryption/query-visible keys or create replacement keys. Original operations without a preparation remain unavailable to stored-only recovery; absence still does not authorize replay.

## Pricing and execution

Enroll the exact POST path in `PREPARED_STORAGE_ADMISSION_JSON`. Require both ordinary attempt tariffs, for each permitted provider:

- `ReadHolonWritePreparation`
- `ReadConditionalHolonWriteOutcome`

The route must reserve exactly two provider attempts, with prices/catalogue within its existing budget. Query tariffs do not authorize these reads. Missing prices or a one-attempt reservation fail startup configuration. A missing/mismatched runtime admission fails before policy or storage work.

Execution retrieves the original protected preparation at its pinned authority, authenticates/reconstructs it under the host grant, and reads the original receipt. An authentication/type failure can stop after the first read; completed physical attempts remain accounted for. There is no first-winner claim, mutation, replication, provider substitution, condition refresh or replay in this path.

The response contains the existing redacted `ConditionalHolonWriteOutcome`. Missing original intent returns `COSMIC_PREPARATION_NOT_FOUND`; it is not proof of mutation rejection and never authorizes another write. Foreign preparation ownership is forbidden. Unavailable or unverifiable evidence remains an explicit error.

## Evidence and remaining acceptance

Both Core targets and the shipping WEB4 build pass. Full Core: **3,501 passed / zero skips or failures**. Full WEB4: **313 passed / one existing Stripe sandbox test skipped / zero failed**. The preceding pricing suite has **408 passing real-Mongo cases**, plus its separate replica initialization control. New controls verify assigned IDs, depth exclusions, per-record contexts, denial without fallback, shared-manager isolation, and all write shapes.

[Core report](../Evidence/Verification/hosted-protection-core.trx.gz), [WEB4 report](../Evidence/Verification/hosted-protection-web4.trx.gz), [preceding usage report](../Evidence/Verification/hosted-recovery-usage.trx.gz).

The signed native cohort now passes **78 REST requests**, **48 GraphQL/gRPC requests**, the unchanged **138 SDK / 138 signed CQL queries**, and **465 settlements / 517 attempts**. All three providers recover original save, soft-delete and hard-delete receipts over the shipping HTTP endpoint. Nine successful observations use two charged reads each; three foreign observations are forbidden after one charged lookup; three anonymous requests start no usage operation. The **12 recovery settlements** are counted separately from the original 60 REST-write, 48 transport and 345 CQL settlements. Current soft/hard state remains unchanged after recovery. Existing GraphQL/gRPC cases pass with the protection service configured.

Sources stayed unchanged, seven producer/consumer pairs matched, owned services stopped and the exact fixture/SQLite files were removed. [Native TRX](../Evidence/Verification/hosted-protection-native.trx.gz), [native result](../Evidence/Verification/hosted-protection-result.json), [source identities](../Evidence/Verification/hosted-protection-source.json.gz), [binary identities](../Evidence/Verification/hosted-protection-binaries.json). The bounded demo runner verifies exact counts; interactive mode also permits additional presenter queries, with four direct guard controls covering both modes.

The protected public-manager hard-delete fault gate now also passes on all three providers. A test-only capability proxy forwards preparation claims unchanged and injects acknowledgement loss only at conditional mutation execution. The native transaction commits once, the public manager reports reconciliation required without changing the caller snapshot, and the durable preparation contains an authenticated envelope. After provider reactivation, stored-only recovery reconstructs the original hard-delete operation and finds its committed receipt without replay. This replaces the earlier opaque-preparation hard-delete fixture; opaque save envelopes remain storage-conformance controls.

The complete native cohort again passed with the same 465 settlements / 517 attempts, unchanged sources and seven matching producer/consumer pairs. Owned services stopped and the exact fixture/SQLite files were removed. [Fault-gate TRX](../Evidence/Verification/protected-hard-delete-native.trx.gz), [result](../Evidence/Verification/protected-hard-delete-result.json), [source identities](../Evidence/Verification/protected-hard-delete-source.json.gz), [binary identities](../Evidence/Verification/protected-hard-delete-binaries.json).

Actual process interruption now has a separate gate. For each authority, the parent journals an additional original operation and launches the same frozen test assembly in a writer test host. The public manager commits its protected preparation normally. At the native hard-delete commit callback, the writer flushes the exact operation identity and terminates its own process before returning an acknowledgement. A distinct fresh test host then activates the same authority, authenticates/reconstructs the stored preparation, reads the original committed receipt and verifies physical absence. The reader never executes or claims the mutation. No new environment, restore, database copy or operation identity is introduced.

The parent requires the writer's nonzero termination, a matching post-commit marker, exactly one commit, a passing fresh reader and distinct writer/reader process IDs. Child phases are bounded to 90 seconds; timeout stops only that owned process tree and preserves intent. Unfinished phase files prevent replay. Successful recovery removes the three owned phase JSON files; bounded logs/TRX and compact original-operation/PID attestations remain. The full native gate again passed with 465 settlements / 517 attempts, three process-recovery providers, unchanged sources, seven matching binary pairs, stopped child/services and removed database fixtures.

[Process gate TRX](../Evidence/Verification/process-restart-native.trx.gz), [original operations and PIDs](../Evidence/Verification/process-restart-originals.json), [result](../Evidence/Verification/process-restart-result.json), [source identities](../Evidence/Verification/process-restart-source.json.gz), [binary identities](../Evidence/Verification/process-restart-binaries.json).

Remaining: operator retention/reconciliation and broader tenant/query/federation acceptance. A separate injected signed-HTTP failure scenario and explicit per-transport envelope inspection are not claimed by this public-manager process gate. Database-server crash/restart, interruption before commit and network partitions are separate failure windows requiring their own acceptance evidence.

## Recovery startup policy lifetime checkpoint

26 October 2026. Two shipping-assembly regressions reproduced the old root-container lookup: scope validation rejected a scoped registration, while disabled validation retained its disposable instance. Both now pass after readiness borrowed a scope. Positive controls verify that singleton disposal remains with the application container and that unenrolled/cancelled checks never construct a policy.

Shipping build and the full WEB4 suite pass: **320 passed / one existing Stripe sandbox skip**. The shipping producer and unit consumer hashes match. [Original failing controls](../Evidence/Verification/recovery-policy-lifetime-red.trx.gz), [full passing report](../Evidence/Verification/recovery-policy-lifetime-web4.trx.gz), [source/binary identities](../Evidence/Verification/recovery-policy-lifetime-identities.json). No provider or database test was needed for this DI ownership change; earlier Core and native matrices retain their separately recorded scope.
