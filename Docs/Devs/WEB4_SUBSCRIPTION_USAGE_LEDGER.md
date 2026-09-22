# WEB4 subscription usage ledger

## Invariant and ownership

WEB4 is the only authority for subscription entitlement, quota policy, usage reservations, settlement, aggregates, and billing-facing history for WEB5 through WEB10. Higher webs measure their own provider usage and report it to WEB4. They do not infer entitlement from JWT claims, accept a billing avatar from callers, or maintain a parallel quota counter.

Every billable operation follows one sequence:

1. Authenticate the user with a WEB4 bearer token.
2. Generate a UUID operation ID and call `POST /api/subscription/usage/authorize` before provider work.
3. WEB4 resolves the authenticated avatar, subscription, and current karma; atomically checks limits and creates a reservation.
4. The higher web performs the operation and measures tokens, units, provider, model, and cost.
5. It awaits `POST /api/subscription/usage/settle` before publishing a successful terminal response.
6. Reads come from `GET /api/subscription/usage/current` and `GET /api/subscription/usage/events`; caller-supplied plan or karma is not part of either contract.

## Policy matrix

| Plan | Monthly requests | Daily calls | Daily tokens | Monthly AI budget |
|---|---:|---:|---:|---:|
| Free | 1,000 | 20 | 50,000 | $1 |
| Bronze | 10,000 | 100 | 250,000 | $10 |
| Silver | 100,000 | 500 | 1,000,000 | $50 |
| Gold | 1,000,000 | 2,000 | 5,000,000 | $250 |
| Enterprise | Unlimited | Unlimited | Unlimited | Unlimited |

Karma scales only the daily-call limit: below 500 = 1x, 500 = 1.5x, 1,000 = 2x, 5,000 = 3x, 20,000 = 5x, and 100,000 = 10x. WEB4 loads karma from authoritative avatar state at authorization/read time. Enterprise remains unlimited for every karma value.

Zero means unlimited for daily calls, daily tokens, and monthly budget. `-1` means unlimited for monthly requests.

## Idempotency and concurrency

`OperationId` is globally unique. The MongoDB event collection has a unique index on it. Authorization and its aggregate increment commit in one transaction; settlement and its aggregate changes commit in one transaction. An identical retry returns the existing result. Reuse by another avatar or with a changed authorization/settlement payload returns `409 OPERATION_ID_CONFLICT`.

The aggregate key is `{avatarId}:{yyyy-MM}`. Monthly counts and costs roll over with the UTC month. Daily calls and tokens reset when the UTC date changes. Reserved cost contributes to budget checks until settlement replaces it with final cost.

MongoDB must support transactions. Use a replica set or sharded cluster. WEB4 fails startup when neither these environment variables nor matching MongoDBOASIS DNA settings are available:

- `SUBSCRIPTION_MONGODB_CONNECTION_STRING`
- `SUBSCRIPTION_MONGODB_DATABASE`

Collections and indexes are created by `MongoSubscriptionUsageRepository`:

- `subscription_usage_events`, unique `ux_operation_id`, plus `ix_user_authorized`
- `subscription_usage_aggregates`

## Authentication rules

- The bearer token selects the billing principal.
- A request body, query parameter, or `AvatarId` header cannot select the billing principal.
- The former global WEB6 API key cannot authorize public billable work because it has no user-bound subscription.
- `ConsumingService` is restricted to WEB5, WEB6, WEB7, WEB8, WEB9, or WEB10.
- Operation IDs must be UUIDs. Reservation and settlement numbers cannot be negative.
- Settlement outcome must be `succeeded`, `failed`, or `cancelled`.

## WEB6 integration

`SubscriptionMiddleware` reserves ordinary REST work. `MeteredEndpointAttribute` synchronously settles unit-priced actions. Completion, OpenAI-compatible, streaming, and WebSocket paths settle token-accurate measurements themselves. Streaming settlement completes before the terminal SSE event. Each WebSocket message receives its own reservation and settlement; the connection handshake is a separate zero-unit event.

The REST `/v1/usage`, GraphQL usage field, gRPC telemetry usage call, and MCP `web6_get_usage` proxy WEB4. None accepts plan, karma, or avatar as quota inputs.

WEB6 retains its provider/model catalogue only to measure or estimate provider cost. The disabled methods in `UsageMeteringManager` and disabled `RateLimitHeaderMiddleware` are retained temporarily with explicit supersession comments; they are not compiled into the active quota path.

## Failure semantics

- If WEB4 authorization is unavailable, WEB6 returns `503 SUBSCRIPTION_AUTHORITY_UNAVAILABLE` and does no provider work.
- A policy denial returns WEB4's status and stable code, normally 429.
- A provider failure settles the reservation as failed.
- A settlement error is surfaced; it is not swallowed or moved to a background task.
- A terminal streaming success is emitted only after settlement succeeds.

## Verification matrix

Automated coverage includes:

- every plan and unknown-plan normalization;
- all karma thresholds and Enterprise at multiple karma levels;
- just below, exactly at, and above monthly request, daily call, daily token, and budget boundaries;
- unlimited sentinel behavior;
- reserved plus settled budget arithmetic;
- negative reservation and settlement inputs;
- valid and invalid consuming services and operation IDs;
- authenticated identity propagation without caller plan/karma/avatar;
- authorize, settle, and current-usage client serialization;
- repeated authorization and settlement, changed-payload conflict, wrong-owner conflict, and concurrent operation IDs in the repository integration suite;
- successful, failed, cancelled, cache-hit, REST, streaming, WebSocket, GraphQL, gRPC, and MCP paths in the WEB6 integration suite;
- a source guard that rejects reintroduction of active WEB6 local-quota calls, stale JWT plan/karma decisions, caller-selected billing identity, or background usage writes.

Run the focused unit tests:

```powershell
dotnet test ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests/NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests.csproj --filter "FullyQualifiedName~Subscription" -m:1
dotnet test WEB6/NextGenSoftware.OASIS.Web6.Core.UnitTests/NextGenSoftware.OASIS.Web6.Core.UnitTests.csproj -m:1
```

Run the cutover guard:

```powershell
pwsh -File Scripts/validate_web6_web4_metering_cutover.ps1
```

Run the live transactional/idempotency/concurrency matrix against a deployed test stack:

```powershell
pwsh -File Scripts/test_web4_subscription_usage_live.ps1 -BaseUrl https://your-web4-test -BearerToken $env:WEB4_TEST_BEARER -SecondBearerToken $env:WEB4_SECOND_TEST_BEARER
```

The second bearer is optional and enables the wrong-owner operation-ID case. The live matrix requires a transaction-capable MongoDB deployment and test subscriptions. Provider end-to-end tests additionally require provider credentials; they must never use production billing credentials.
