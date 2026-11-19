# App-key admission evidence reconciliation — 19 November 2026

Status: valid-credential admission already accepted; incomplete-configuration root fix published in Core 13ba34168d21ec583ccb5057a80edc9bac9444ca. Configuration qualification accepted in run44595965065/job135814194706: actual API zero build errors, exactly one new case passed in5ms, strict artifact and process restoration verified. Seven public negative credential cases now accepted in run44598492673, actual API0errors and real Mongo no-write/zero-reservation checks; hosted/full portal acceptance remains open. OPORTAL remains incomplete.

## Retain existing acceptance without rerunning it

Run 44507676469, source 492e030c, frozen Portal 4fd7cfaa and SDK d7aa569 already covers a real app credential through the shipping `Web4SubscriptionAuthorizationClient`, authenticated WEB4 controller, service and native Mongo repository. `SubscriptionFreshAppUsageNativeTests.cs`, `PrepareFreshAppUsageThroughSdkAsync`, rotates a real app key and supplies it alongside app ID and service credentials. It verifies authorization/replay, start, an actual acknowledged native Mongo insert, settlement/replay and a stored original app charge of USD 0.001. Removing original consent evidence produces the explicit policy denial 402, not an outage. Subsequent browser/native assertions verify allocation, original payer and balanced projection. Financial readiness is declared input; zero-cent rounding is not genuine Stripe payment proof.

Run 44549547233 separately covers account creation, rotation and disabled-account credential rejection. Its seven credential observations call the actual repository through a nonce-guarded test endpoint. They do not establish those negative cases through the public usage authorization endpoint.

These results must not be described as missing valid app-key admission or repeated merely to consolidate evidence.

## Newly identified invariant violation

An optional app identity must be either absent or complete. A partially configured app identity must fail explicitly before sending authorization; it must never silently become avatar-only admission.

Current shipping Core `Services/Subscriptions/Web4SubscriptionAuthorizationClient.cs` resolves app ID/key from constructor values or their environment settings, then `AddAppIdentity` returns when **either** value is blank. Consequently an ID-only or key-only effective configuration omits both headers. The WEB4 controller cannot reject a malformed pair it never receives. The controller's `ResolveAppAsync` already rejects present invalid, partial or disabled credentials with `APP_CREDENTIAL_INVALID`; the transport loses the caller's app intent before that boundary.

This is a source finding, not a reproduced hosted incident. No credential values or environment listing were inspected. No production/deployed configuration was changed.

## Next scoped implementation and qualification

1. Fix effective app-pair validation at its single SDK configuration owner, preserving the specified no-app mode and existing complete-pair mode. Do not add a fallback or second admission route.
2. Add only new bounded configuration/admission cases: ID-only and key-only effective configuration must fail without sending a request; rotated-old, disabled, incorrect and partial HTTP credentials must refuse public usage admission without reserving usage or changing financial state. Review existing service-identity tests before selecting additional service cases.
3. Use the existing reusable isolated CI host and actual controllers/repository/backend, with no replacement business responses or shared Atlas calls. Retain accepted valid-path and account-reader evidence; do not rerun those cohorts unchanged.
4. Keep environment inputs isolated/restored, assert owned cleanup and publish scoped source plus evidence. Source review alone is not acceptance.

Core's physical checkout differs from the parent gitlink and belongs to concurrent work; it was read only. Review current remote Core task-branch state before a scoped publication; do not stage or commit the physical checkout wholesale. COSMIC current status was reviewed and its grouped DISTINCT work left untouched.

## Resource and publication record

Read-only source/evidence reconciliation and this compact report only. No local installation, build, database, archive, browser, worker or test process was started. No shared database, Stripe, Railway, DNA, protected delivery 535/543 or deployment action. No reclaimed bytes claimed. Existing cleanup evidence remains in `Docs/WIP/Cleanup/CLEANUP_LEDGER.md`.

## Negative public admission accepted

Run44598492673/job135823178540, source a4d3d0daabc9, passed the single new case in2s. Seven authenticated controller calls cover missing ID/key, incorrect key, absent/malformed app ID, revoked old key and disabled current key. Each requires exact APP_CREDENTIAL_INVALID401, no key disclosure and unchanged whole native state; zero reservations verified. Actual shipping transactional setup and Mongo, no replaced business responses. First run44596850959 failed before HTTP because the initial finite pooled plan required a disabled flag; retained as failure. Corrected setup uses supported enterprise/non-pooled account without altering production flags or assertions. Artifact14566859252/2636B strictly verified in RAM and disposed; owned database/provider/process cleanup and Mongo absence passed. Existing positive and configuration cases were not rerun.
