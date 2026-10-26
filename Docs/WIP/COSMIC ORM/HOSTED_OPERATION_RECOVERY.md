# Authenticated hosted operation recovery

26 October 2026. Implemented boundary milestone; signed native HTTP certification and automatic hosted write enrollment remain open. Do not treat this checkpoint as full production CQL completion.

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

Registering a read authorization service alone does **not** enroll original writes. Original operations must already have a protected preparation created under an authorized compatible policy. Shared SDK save/delete enrollment and request-policy inheritance are implemented; automatic per-actor policy enrollment across hosted write routes is the next integration step.

## Pricing and execution

Enroll the exact POST path in `PREPARED_STORAGE_ADMISSION_JSON`. Require both ordinary attempt tariffs, for each permitted provider:

- `ReadHolonWritePreparation`
- `ReadConditionalHolonWriteOutcome`

The route must reserve exactly two provider attempts, with prices/catalogue within its existing budget. Query tariffs do not authorize these reads. Missing prices or a one-attempt reservation fail startup configuration. A missing/mismatched runtime admission fails before policy or storage work.

Execution retrieves the original protected preparation at its pinned authority, authenticates/reconstructs it under the host grant, and reads the original receipt. An authentication/type failure can stop after the first read; completed physical attempts remain accounted for. There is no first-winner claim, mutation, replication, provider substitution, condition refresh or replay in this path.

The response contains the existing redacted `ConditionalHolonWriteOutcome`. Missing original intent returns `COSMIC_PREPARATION_NOT_FOUND`; it is not proof of mutation rejection and never authorizes another write. Foreign preparation ownership is forbidden. Unavailable or unverifiable evidence remains an explicit error.

## Evidence and remaining acceptance

Both Core targets and the shipping WEB4 build pass. Full WEB4: **305 passed / one existing Stripe sandbox test skipped / zero failed**. Real-Mongo usage: **408 passed / zero skipped or failed**, plus the separate replica initialization control. Controls cover pre-manager actor/input/admission rejection, missing/denied/mismatched grants, bounded runtime catalogues, readiness without key access, and a priced stored-original recovery returning only the redacted outcome.

[WEB4 report](Verification/hosted-recovery-web4.trx.gz), [usage report](Verification/hosted-recovery-usage.trx.gz), [tested sources and matching producer/consumer identities](Verification/hosted-recovery-identities.json).

Remaining: automatic authorized protection enrollment on hosted saves/deletes, signed HTTP recovery against all three real providers with durable quota/settlement evidence, protected hard-delete acknowledgement-loss certification, and actual process interruption/restart. Earlier native SDK/scoped-manager recovery evidence remains valid for its recorded scope; it does not certify this new HTTP boundary.
