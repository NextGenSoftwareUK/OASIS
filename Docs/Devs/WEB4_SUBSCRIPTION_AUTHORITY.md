# WEB4 subscription authority for WEB5-WEB10

WEB4 is the single source of truth for OASIS subscription plans and monthly API-request usage. WEB5, WEB6, WEB7, WEB8, WEB9, and WEB10 do not maintain subscription records or infer entitlements from JWT plan claims.

## Customer subscription flow

1. The customer chooses a paid plan in OPortal.
2. OPortal creates a Stripe checkout session through WEB4.
3. Stripe sends a signed webhook to WEB4.
4. WEB4 verifies the Stripe signature and writes the authoritative subscription record for the avatar.
5. Subsequent WEB5-WEB10 requests are authorized against that record.

Enterprise remains a contact-sales plan. Do not grant it by editing JWT claims, environment allowlists, client code, or the legacy settings endpoint. Administrative provisioning must write the same authoritative WEB4 subscription record through an authenticated administrative workflow.

## Request authorization contract

Each authenticated WEB5-WEB10 request calls:

```http
POST {WEB4_API_BASE_URL}/api/subscription/authorize-request
Authorization: Bearer <avatar JWT>
Content-Type: application/json

{"consumingService":"WEB6"}
```

WEB4 resolves the avatar from the bearer token, loads its subscription, checks status and expiry, checks the monthly plan limit, and records one request. The response includes `allowed`, `statusCode`, `code`, `message`, `planId`, `currentUsage`, `limit`, and `remaining`. A limit and remaining value of `-1` means unlimited Enterprise usage.

Consumers return the decision status when access is denied. If WEB4 cannot be reached or returns an invalid transport response, consumers return `503 SUBSCRIPTION_AUTHORITY_UNAVAILABLE`; they do not silently permit the request and do not consult a local entitlement store.

Unauthenticated requests continue to the normal authentication middleware so each API preserves its existing public-route and authentication behavior. Swagger, health, favicon, and OpenAPI routes bypass subscription accounting.

## Configuration

Set `WEB4_API_BASE_URL` on WEB6-WEB10. WEB5 also accepts its existing `WEB4_OASIS_API_BASE_URL` name during the environment-variable naming migration. The production default is `https://api.web4.oasisomniverse.one`.

WEB6 retains its AI token and provider-cost metering. That data measures model usage and cost; it does not determine the user's OASIS subscription entitlement or monthly cross-service request allowance.

## Verification

Run:

```powershell
python Scripts/validate_web4_subscription_authority.py
python Scripts/validate_railway_dependency_manifest.py --require-gitlinks
```

The first check ensures every WEB5-WEB10 middleware uses the shared WEB4 authorization client and that WEB5's former local subscription service is absent. The dependency check ensures Railway builds use the exact API Core, STAR ODK, and WEB6 commits recorded in `Docker/oasis-dependency-versions.env`.
