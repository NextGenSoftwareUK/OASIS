# Private bootstrap authority

## Approved design and cost boundary

Owner approved the recommended Mongo-only private bootstrap authority. JWT identity validation and quota admission must not initiate paid native reads before reservation, or charge users whose request is denied. No paid-chain fallback is allowed when this authority is missing, stale, conflicting or unavailable. This is not a public mirror or a replacement for encrypted provider records.

## Current implementation — foundation, NOT enabled

`ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Services/Subscription/BootstrapAuthority.cs` defines a small record and Mongo repository using primary reads, majority read concern and majority acknowledged writes. Fields: canonical avatar ID, revision, ready/enabled status, required User/Wizard/System role, token version, karma version/value and UTC verification expiry. No email, username, password, refresh tokens, wallet keys or full avatar payload.

Validation requires exact identity/token version, positive revision/karma version, fresh UTC expiry, ready state and enabled status. Missing/invalid/expired/updating records fail closed; disabled/revoked identities are unauthorized. Negative karma remains representable; quota conversion must retain existing nonnegative/int-max normalization.

Mutation protocol:

1. Compare-and-swap a ready record at the expected revision to unavailable, incrementing revision. If unsuccessful, do not start the canonical mutation.
2. Apply the canonical security/karma mutation with existing persistence owner and native accounting.
3. Publish the independently verified resulting projection against the exact unavailable revision. Token/karma versions must not decrease.
4. Crash, uncertain result or publication conflict leaves the record unavailable. Recovery must inspect the original operation, not replay an uncertain mutation or automatically re-enable authority.

No upsert or implicit provisioning exists. First trusted provisioning/reviewed reconciliation needs an explicit implementation. Expiry is not a substitute for immediate invalidation. No watcher/change stream/eventual background mirror alone can satisfy revocation correctness.

Publication additionally requires an increased karma version for a changed karma value and an increased token version for changed enabled status. Equal versions may only preserve those values. A version is not merely a nondecreasing decorative counter.

Role is also security state: changes require a higher token version, rejecting old tokens. The record uses nullable existing Core AvatarType and validates that it is defined; **Wizard is enum zero**, so a nonnullable default would dangerously treat a missing persisted role as Wizard. Missing and unknown roles are rejected. This does not trust caller-supplied/JWT role claims or enable privileged endpoints before writer integration.

## Source findings and required integration

WEB4 `Middleware/JwtMiddleware.cs` currently calls `Program.AvatarManager.LoadAvatarAsync` before setting request identity/avatar. `Controllers/SubscriptionController.cs:GetCurrentKarmaAsync` loads AvatarDetail before quota admission; `LocalUsageReservationAuthority` delegates to the same karma callback. These remain unchanged until writer safety is implemented.

Core `AvatarManager-Save.cs` has separate sync/async V2 early returns; changing only a common trailing block would miss them. Security changes and direct delete/provider paths also need coverage. `AvatarManager-Karma.cs` delegates V2 updates to DurableKarmaManager; authoritative karma publication must occur at durable persistence, not from an in-memory cache or client request. Token issuance currently needs an explicit version contract; do not assume legacy tokens carry a revocation version. Middleware consumers currently expect a full IAvatar in request items, so a minimal record must not be represented as a complete avatar.

Further trace: `KarmaManager.cs` persists ordinary Add/Remove/Reset totals through HolonManager `SaveSettingsAsync`/`SaveSettings` for the `karma` settings section (lines156/235/335 at this audit), while other avatar karma entry points save AvatarDetail projections. Current quota reads AvatarDetail. Integration must explicitly reconcile these actual canonical paths, not assume all karma updates flow through SaveAvatarDetail or mirror a cached total. `AvatarManager-SaveUtils.cs:GenerateJWTToken` emits id/sub/email/name but no token-version claim; Authenticate async/sync, refresh and session callers all need the issuance contract. `AvatarManager-Delete.cs` has username/email/id overloads and sync/async branches with DeletedDate semantics; revocation cannot be implemented solely in the general SaveAvatar method.

Remaining gates:

- Trace every canonical security/delete/karma mutation and establish one shared invalidation/publication owner, including sync/async and cross-service callers.
- Explicit trusted first provisioning and crash reconciliation, without unbounded lease renewal from stale source data.
- Token issuance/version validation and endpoint compatibility without pre-reservation native hydration.
- Real retained Mongo concurrency/restart/majority tests for invalidation/publication; wrong revision, duplicate publication, out-of-order version, outage and crash tests.
- Wire WEB4 JWT and quota consumers to the same validated record; other hosts must not bypass the authority.
- Actual HTTP denial/revocation/quota tests proving zero paid-chain calls, followed by admitted native execution and existing fee recovery.
- All release/pin/hosted gates remain required. Nothing here enables sponsored spending or Railway configuration.

## Evidence

2026-10-17 authorization compatibility trace: custom `Helpers/AuthorizeAttribute.cs` casts request Avatar to concrete Avatar and checks AvatarType; `Middleware/OASISAuthHandler.cs` additionally requires IAvatar; `Controllers/OASISControllerBase.cs` exposes Avatar/AvatarId via request Items. A small authority record must not be installed as a fabricated complete Avatar. Added required version-protected role and tested actual Mongo missing/null and unknown numeric persisted roles, same-version role-change rejection, successful versioned role change and old-token rejection. Retained authority/ledger suite **92/92passed25s/0skip/full runnerexit0**, SHA256 `49F476EAA0431C8D6CB64569A3FC26CB8071FE4FEB1D86200FB7E768CB758A25`; pure validation **13/13passed83ms**, SHA256 `24AA5F473038B87B950B8B64EFFF4606FB3F87310180F232E807E4D158252CE7`; WEB4 diagnostic build0errors255warnings. Same environment, no download/restore/native operation/deployment; owned Mongo stopped/log65298bytes; free C84,796,207,104bytes. Required next integration remains minimal authenticated identity vs post-reservation full-avatar hydration, canonical writers, trusted provisioning and token issuance. No host enablement/release claim.

2026-10-17 response-time expiry: repository now owns an injected TimeProvider (system UTC by default); ReadAsync no longer accepts a caller timestamp and checks expiry AFTER the actual Mongo response/cancellation. PublishAsync likewise obtains its validation time internally. Real Mongo CommandSucceededEvent advances the test clock at find completion to exactly the record expiry, proving refusal instead of admission using a pre-query timestamp. Existing retained authority/ledger suite **89/89passed24s/0skip/full runnerexit0**, `authority-ledger-current.trx` SHA256 `8F1C4430EEFF6EA15315FFE5A546DEDAC094A1725679F1E6C04C5F769D08FAA0`; WEB4 build0errors255warnings; pure validation **11/11passed112ms**, report SHA256 `5B1F04D491F274DD3DFF66855A93367BA178F17142D9DCB9D44D5A3348110A87`. Same environment/no restore/download/native spend; owned Mongo terminal/stopped; free C84,817,526,784bytes. Still not host/writer integration or release certification.

2026-10-17 retained Mongo replica verification via `pwsh -NoProfile -File Scripts/TestHosts/Run-ReusableUsageRecoveryTests.ps1 -AuthorityLedger`: final **88 passed/0 failed/0 skipped**,24s, full runner exit0. Linked production authority code into the existing protocol test project, not a reimplemented fake. Tested 16 concurrent invalidations/exactly1winner, wrong revision, duplicate publication, version rollback, persisted unavailable state through a new client/repository, and disabled identity rejection. Initial86/86pass report SHA256 `1D555F71D1B15F3A574E170680F6E5CBE0244B93F860B7814DF85EE631D9040D`; two added same-version/value-change regressions failed before fix (86pass/2fail/88total), SHA256 `4DEEE705FB235748563C804BF24D6AD20417738CB999CCD4ED53D28BE6C35066`. Tightened Mongo CAS filter; final `authority-ledger-current.trx` SHA256 `E23AA1FE4BF6D55A936F112700F45777BF869272409514E0B488F23E040E14B1`. This proves real single-node majority Mongo persistence/concurrency/reconnect, NOT a Mongo process crash/cluster election, complete writer coverage or hosted auth integration.

Reused fixed `usage_bootstrap_authority_test` database on isolated27029 replica; each test removes only its own inserted avatar ID, never drops shared native evidence databases. Owned Mongo stopped after every runner, log bounded65,395bytes; no listener27029 remained. No download/restore/new installation/native operation; final free C84,857,729,024bytes. Pending Hedera/TRON records untouched.

2026-10-17: WEB4 diagnostic build succeeded, 0 errors/255 warnings. Focused BootstrapAuthorityTests: **11 passed, 0 failed, 0 skipped**, 82 ms, process exit 0. These test pure validation only; they do NOT prove Mongo persistence/concurrency or host integration. Report `TestResults/SubscriptionUsageCurrent/bootstrap-authority-current.trx`, SHA256 `8D8BFA4D63E11BB9170967E6ABBDAE8588199FCEC294CFC78BBA07C873A88DE2`.

Existing checkout/runtime/dependencies reused. No restore/download/new environment, native transaction or deployment. Free C after checks: 84,921,081,856 bytes. Unrelated dirty subscription changes preserved; build is diagnostic, not immutable release certification.
