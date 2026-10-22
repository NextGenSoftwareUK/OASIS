# CQL v1 developer and operator notes

The WEB4 v1 transport runs the existing generic HolonManager/Core query pipeline against one active MongoDB, SQLite or Neo4j provider. [Request language and pricing configuration](CQL_V1_HTTP_CONTRACT.md); [local C# and Swagger demonstrations](CQL_DEMO_RUNBOOK.md).

## Calling from C#

Use an existing WEB4 bearer token and retain the same idempotency key for the logical request. The server supplies identity, visibility, capabilities and prices.

```csharp
using System.Net.Http.Headers;
using System.Net.Http.Json;

// Obtain this token through the application's existing WEB4 login flow.
client.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("Bearer", bearerToken);

var query = new
{
    provider = "MongoDBOASIS",

    filters = new object[]
    {
        new
        {
            metadataPath = new[] { "Category" },
            scalar = "String",
            @operator = "Equal",
            value = "coffee"
        },
        new
        {
            metadataPath = new[] { "Rating" },
            scalar = "Integer",
            @operator = "GreaterThanOrEqual",
            value = 7
        }
    },

    orderBy = "Name",
    skip = 0,
    take = 10
};

// Store this alongside the user's intent before sending. A transport timeout
// does not prove that the query or its usage settlement failed to execute.
var idempotencyKey = Guid.NewGuid().ToString("D");

using var request = new HttpRequestMessage(HttpMethod.Post, "/api/data/query-cql")
{
    Content = JsonContent.Create(query)
};

request.Headers.Add("Idempotency-Key", idempotencyKey);

using var response = await client.SendAsync(request, cancellationToken);

var operationId = response.Headers.TryGetValues("X-OASIS-Operation-Id", out var ids)
    ? ids.Single()
    : null;

// Keep operationId and the response before surfacing an error. Do not generate
// a fresh key automatically to repeat a request whose outcome is uncertain.
var resultJson = await response.Content.ReadAsStringAsync(cancellationToken);
response.EnsureSuccessStatusCode();
```

Metadata key spelling/case must match persisted keys. Integers use exact Int64 semantics; numeric strings are not coerced into integers, and invalid GUID values do not satisfy typed GUID inequality. Guid literals use D format and normalize GUID case. String Contains/StartsWith/EndsWith use ordinal semantics. Exists includes an explicitly null member; IsNull matches an explicitly null member; a missing path satisfies neither. Queryable metadata must remain available to the native projection under the configured encryption/reserved-key policy; CQL does not decrypt an entire provider dataset to search it.

## Enabling the route

1. Run WEB4 with HyperDrive v2 and an already registered, activated supported provider. An omitted provider uses the active server default. The request cannot activate a provider, select an arbitrary instance, initiate failover or override capabilities.
2. Configure the existing durable usage ledger/local reservation authority and prepared-storage route. Add explicit query tariffs and an approved catalogue/version using the [configuration example](CQL_V1_HTTP_CONTRACT.md). An explicit test/example rate is not an approved production commercial price. Native fee/quote configuration is needed only when the deployment actually includes its native profile.
3. Keep ordinary query attempts limited to one per request, reserve sufficient service cost and set the CQL route's native fee budget to zero. Validate catalogue/rate changes at startup. Missing admission/price/provider readiness fails closed.
4. Retain authenticated HTTPS, existing subscription/reconciliation authorization and monitored outbox persistence. No secret, signer, bearer token or connection credential belongs in request JSON or evidence reports.

Owner-or-globally-public visibility is enforced natively before the page. Wizard status and SHARED_WITH metadata cannot bypass it. Shared-recipient authorization and tenant-specific isolation are not supplied by this v1 contract. Deployments needing those policies must implement their authoritative predicates before admitting those workloads. This route does not change the behavior or security of older data endpoints.

## Failures and operations

The five-second deadline is cooperative: it passes cancellation through Core to the adapter. It is not a promise that every driver/server interrupts at precisely five seconds. Existing native provider timeouts and bounded-result checks remain active. The boundary rejects excessive request/response sizes without publishing a partial page. A provider error envelope is a completed service-query attempt under the configured tariff; it is not an inferred vendor invoice. Interrupted attempts or failed receipt persistence retain an uncertain outcome for reconciliation.

Clients retain the idempotency key and X-OASIS-Operation-Id. A duplicate receives a conflict/terminal result and does not silently re-execute the query or replay a response body. After a timeout/unknown result, use the existing authorized operation-status/reconciliation workflow; only independently established evidence may complete an uncertain operation. Never clear the outbox or issue a new key simply to hide uncertainty. Administrative reconciliation routes live under `/api/subscription/usage/admin/reconciliation` and retain their existing service/admin authorization.

Operational monitoring should distinguish rejected authentication, pre-dispatch validation, provider errors, cancellation, response limits and unsettled usage. A missing receipt is not zero cost or proof of no execution. Keep the shared provider build lease and owned fixture journals when verifying changes. Production rollout, approved prices and application-specific access policy remain operator responsibilities; test acceptance does not deploy or activate them.

## Repeatable acceptance

From C:/Source/OASIS, run the existing producer lease around `Scripts/TestHosts/Run-ReusableUsageRecoveryTests.ps1 -CqlV1Web4`. This reuses the fixed Mongo/Neo environment and checks an exact 65-case report, including the HTTP matrix over all three native engines. It needs the already installed/retained runtimes and stopped owned services; it does not install them. Shared build outputs are serialized; do not nest the lease or bypass an occupied environment lock.

The HTTP fixture uses the shipping OasisJwtValidation, controller, compiler, endpoint and durable admission/usage middleware, real native providers and a real Mongo execution outbox. Avatar-directory resolution and reservation/settlement authority are controlled test components. This is not a production account or a boot of the entire production application.

Successful runs stop owned services and remove only verified fixture resources. Failures preserve their manifest/data and must be inspected before another run. `-CqlV1Web4Reconcile` is intentionally locked to one reviewed historical manifest; it is not a general cleanup command for arbitrary future failures. Resource ownership, sizes and retention are tracked in [the ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
