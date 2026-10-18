# Private bootstrap authority

## Approved design and cost boundary

Owner approved the recommended Mongo-only private bootstrap authority. JWT identity validation and quota admission must not initiate paid native reads before reservation, or charge users whose request is denied. No paid-chain fallback is allowed when this authority is missing, stale, conflicting or unavailable. This is not a public mirror or a replacement for encrypted provider records.

## Current implementation — foundation, NOT enabled

`ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Services/Subscription/BootstrapAuthority.cs` defines a small record and Mongo repository using primary reads, majority read concern and majority acknowledged writes. Fields: canonical avatar ID, revision, ready/enabled status, token version, karma version/value and UTC verification expiry. No email, username, password, refresh tokens, wallet keys or full avatar payload.

Validation requires exact identity/token version, positive revision/karma version, fresh UTC expiry, ready state and enabled status. Missing/invalid/expired/updating records fail closed; disabled/revoked identities are unauthorized. Negative karma remains representable; quota conversion must retain existing nonnegative/int-max normalization.

Mutation protocol:

1. Compare-and-swap a ready record at the expected revision to unavailable, incrementing revision. If unsuccessful, do not start the canonical mutation.
2. Apply the canonical security/karma mutation with existing persistence owner and native accounting.
3. Publish the independently verified resulting projection against the exact unavailable revision. Token/karma versions must not decrease.
4. Crash, uncertain result or publication conflict leaves the record unavailable. Recovery must inspect the original operation, not replay an uncertain mutation or automatically re-enable authority.

No upsert or implicit provisioning exists. First trusted provisioning/reviewed reconciliation needs an explicit implementation. Expiry is not a substitute for immediate invalidation. No watcher/change stream/eventual background mirror alone can satisfy revocation correctness.

## Source findings and required integration

WEB4 `Middleware/JwtMiddleware.cs` currently calls `Program.AvatarManager.LoadAvatarAsync` before setting request identity/avatar. `Controllers/SubscriptionController.cs:GetCurrentKarmaAsync` loads AvatarDetail before quota admission; `LocalUsageReservationAuthority` delegates to the same karma callback. These remain unchanged until writer safety is implemented.

Core `AvatarManager-Save.cs` has separate sync/async V2 early returns; changing only a common trailing block would miss them. Security changes and direct delete/provider paths also need coverage. `AvatarManager-Karma.cs` delegates V2 updates to DurableKarmaManager; authoritative karma publication must occur at durable persistence, not from an in-memory cache or client request. Token issuance currently needs an explicit version contract; do not assume legacy tokens carry a revocation version. Middleware consumers currently expect a full IAvatar in request items, so a minimal record must not be represented as a complete avatar.

Remaining gates:

- Trace every canonical security/delete/karma mutation and establish one shared invalidation/publication owner, including sync/async and cross-service callers.
- Explicit trusted first provisioning and crash reconciliation, without unbounded lease renewal from stale source data.
- Token issuance/version validation and endpoint compatibility without pre-reservation native hydration.
- Real retained Mongo concurrency/restart/majority tests for invalidation/publication; wrong revision, duplicate publication, out-of-order version, outage and crash tests.
- Wire WEB4 JWT and quota consumers to the same validated record; other hosts must not bypass the authority.
- Actual HTTP denial/revocation/quota tests proving zero paid-chain calls, followed by admitted native execution and existing fee recovery.
- All release/pin/hosted gates remain required. Nothing here enables sponsored spending or Railway configuration.

## Evidence

2026-10-17: WEB4 diagnostic build succeeded, 0 errors/255 warnings. Focused BootstrapAuthorityTests: **11 passed, 0 failed, 0 skipped**, 82 ms, process exit 0. These test pure validation only; they do NOT prove Mongo persistence/concurrency or host integration. Report `TestResults/SubscriptionUsageCurrent/bootstrap-authority-current.trx`, SHA256 `8D8BFA4D63E11BB9170967E6ABBDAE8588199FCEC294CFC78BBA07C873A88DE2`.

Existing checkout/runtime/dependencies reused. No restore/download/new environment, native transaction or deployment. Free C after checks: 84,921,081,856 bytes. Unrelated dirty subscription changes preserved; build is diagnostic, not immutable release certification.
