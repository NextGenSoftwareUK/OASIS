# Private settlement outbox administration

This command requeues one immutable settlement after an operator repairs a permanent delivery failure, such as a rotated service credential. It runs inside trusted infrastructure with Mongo access; it exposes no HTTP endpoint and never executes a provider or edits usage, costs, receipt data, ownership or fingerprints.

The command compiles the same `OasisJwtValidation` and `SubscriptionServiceIdentity.RequireAdministrator` sources used by WEB4. It validates the signature, HS256 algorithm, exact configured issuer/audience, expiry/not-before, matching canonical `sub`/`id` avatar claims, `oasis.subscription.admin=true` and the administrator allowlist. The audit actor is derived solely from this validated identity. An actor cannot be supplied as a CLI argument.

Set these values through the operator host's protected secret environment:

| Environment setting | Required value |
|---|---|
| `SUBSCRIPTION_ADMIN_BEARER_TOKEN` | Current administrator JWT, without the `Bearer ` prefix. |
| `SUBSCRIPTION_ADMIN_AVATAR_IDS` | The same comma-separated administrator avatar allowlist configured on WEB4. |
| `OASIS_DNA_PATH` | Absolute path to the protected, deployed WEB4 OASIS DNA containing the actual `OASIS.Security.SecretKey` and optional `OASIS.Security.Oidc.Issuer`. The documented absent issuer value is `OASIS`, exactly as WEB4 validates it. |
| `SUBSCRIPTION_OUTBOX_MONGO_CONNECTION_STRING` | The target consuming service's outbox connection, using TLS outside local testing. |
| `SUBSCRIPTION_OUTBOX_DATABASE` | The target consuming service's outbox database. |

Use a dedicated operator Mongo role scoped to `find` and `update` on that database's `subscription_usage_outbox_v1` collection. Do not expose the signing key, bearer, or database credentials to an untrusted shell, untrusted user, logs or command-line arguments. Read the actual deployed DNA; substituting another signing key would no longer validate the deployment's administrators.

After inspecting the deadletter and verifying the repair, generate an action UUID and keep it with the change ticket:

```powershell
dotnet run --project Scripts/SubscriptionLedger.OutboxAdmin -- requeue 'OPERATION-UUID' 'ACTION-UUID' 'Service credential rotation verified; change ticket 123'
```

The command prints one JSON result. Exit `0` acknowledges the requeue action, `2` is invalid arguments/configuration, `3` is failed administrator authentication/authorization, `4` is a protocol/state conflict, and `5` is an unacknowledged timeout/storage failure. For exit `5`, retry the same operation UUID, action UUID and reason: identical administrative actions are idempotent even if the first process lost its acknowledgement. Do not generate a second action to compensate for an unknown first result.

Only a deadletter with an existing terminal settlement can transition to pending. The same atomic update appends the administrator, reason, action UUID, previous error, timestamp and immutable settlement fingerprint to `AdministrativeActions`. Changed actor/reason under an existing action UUID conflicts. Active or settled operations cannot be requeued. A successful CLI result means the original settlement awaits delivery; use WEB4's owned usage events and service metrics to verify its final acknowledgement.

Publish with `dotnet publish Scripts/SubscriptionLedger.OutboxAdmin -c Release`. The framework-dependent output requires the .NET 10 ASP.NET Core runtime because it uses the shared WEB4 execution SDK. Packaging and credentials belong on private operator infrastructure, separate from publicly served API content.

Tests: `dotnet test Scripts/SubscriptionLedger.OutboxAdmin.Tests`. JWT/security tests run locally; the real Mongo requeue integration test requires `SUBSCRIPTION_TEST_MONGODB_URI` pointing to a disposable replica set and fails explicitly when absent.
