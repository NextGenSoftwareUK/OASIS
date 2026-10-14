# Paid overages: configuration and launch checks

Development uses the existing Stripe **test** account, MongoDB connection from OASIS DNA, and WEB4 subscription authority. Production promotion remains a separate release.

## Agreed prices and defaults

Customer prices are loaded from [`ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Configuration/subscription-overage-prices.json`](../../ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Configuration/subscription-overage-prices.json). The build copies this file to `Configuration/subscription-overage-prices.json`. No additional Railway Stripe or pricing variable is required.

| Plan | USD per extra successful request |
|---|---:|
| Bronze | 0.001 |
| Silver | 0.0005 |
| Gold | 0.0002 |
| Enterprise | 0.0001 |

An extra request is a request authorised after the plan's monthly request allowance has been consumed. Failed or cancelled extra requests do not receive this request fee. Enterprise's current unlimited allowance produces no request overages. The rate remains available for a future bounded enterprise agreement.

WEB6 AI overages use the immutable model catalogue cost, **without an AI margin**, only for measured costs beyond the plan's included monthly USD usage budget. A failed provider operation can still have a verified vendor cost. Requests and provider costs are separate accounting components; do not mistake the provider budget for an additional monthly subscription price.

Paid overages default to **disabled**. Free subscriptions cannot opt in. An active paid subscriber must have a default payment method, accept the current pricing version and SHA-256 fingerprint, and choose a monthly USD cap in whole cents. The portal initially offers USD 50 as an editable cap; it does not enable or charge overages without confirmation.

When opted in, the included daily call/token and monthly usage gates give way to the agreed spending cap. Request-size limits, provider-call bounds, ownership checks and replay protection still apply. The ledger conservatively reserves full potential provider cost plus any extra request fee before execution, and releases unused exposure after settlement. Actual billed AI usage remains the amount above the included budget. Provider overruns beyond authorised exposure are the platform's cost.

Disabling overages affects future reservations. It cannot erase previously authorised charges. Failed USD invoice payments enforce included quotas until Stripe confirms payment; an unrelated GBP subscription payment does not clear that debt.

## Billing and storage

1. GBP monthly subscriptions continue using the existing Stripe price IDs and customer portal.
2. USD overages use standalone Stripe invoices for the same verified customer, with invoice items attached to that specific invoice. They cannot become pending items on a GBP subscription invoice.
3. WEB4 prepares immutable statements and allocates each operation once. The statement, operation bindings and audit commit in one Mongo transaction.
4. The worker invoices closed UTC calendar months. **Pay incurred overages now** invoices already settled usage in the current month. Repeated billing request UUIDs return the same statement.
5. Each collection attempt has a durable lease and a stable Stripe idempotency key. Invoice metadata permits recovery after Stripe's idempotency cache expires. A previously attached invoice is reused.
6. Round the cumulative monthly customer charge to USD cents, then subtract amounts already stated. Do not round every request or every pay-now batch independently.
7. Stripe may carry amounts below its minimum payment size to the customer's USD invoice balance. Invoice subtotal is the billed amount; `amountPaid` is actual cash collected. The portal and administrator screens distinguish both.
8. Signed standalone invoice webhooks fetch canonical Stripe state and verify customer, avatar, month, ledger scope and amount before recording payment. Event and invoice replays cannot duplicate an order.
9. Audited cost corrections append adjustments. A negative adjustment produces a USD invoice credit; original usage and invoice records remain intact. Positive corrections cannot exceed the approved cap. Credits apply to future USD invoices; this is distinct from a card refund.

Collections are in the existing ledger database: `subscription_overage_statements` and `subscription_overage_adjustments`. They reuse the existing Mongo instance. WEB6 pricing versions remain in the existing Holon settings system.

## API and portal checks

1. `GET /api/subscription/overages/prices`: confirm currency USD, zero AI margin, the reviewed rates, version and fingerprint.
2. Log in with a paid test subscriber. Open **Subscription → Usage**, review the terms, set a cap and enable paid overages. An unconfirmed checkbox or missing price fingerprint must prevent activation.
3. `POST /api/subscription/toggle-pay-as-you-go`: submit `enabled`, `pricingVersion`, `pricingSha256`, and `monthlyLimitUsd`. Confirm terms and consent are audited.
4. Run a supported API operation. Verify measured supplier cost, reserved exposure and settlement. Replaying the same operation must not execute or charge twice.
5. `GET /api/subscription/overages/statements`: inspect only the current subscriber's statements. For incurred excess, `POST /api/subscription/overages/pay-now` takes a `billingRequestId` UUID and optional UTC `month`.
6. Inspect **Manage billing**, **Orders** and **USD overage statements**. Confirm standalone currency and actual paid amount; never add GBP and USD as though they were one currency.
7. In administrator login, open **USD Overages** and inspect state, invoice ID, payment failure and collection errors. Ordinary subscribers must receive 403 from `/api/subscription/overages/admin/statements`.
8. Enable `SUBSCRIPTION_USAGE_ENABLED=true` on WEB4 after deployment verification. The existing flag starts expiry, reconciliation and overage billing workers together.
9. Verify a restart preserves statements, consent, immutable pricing and invoice bindings. Reconciliation compares customer overages with their USD invoice subtotals, separately from provider evidence and GBP revenue. An open month remains pending.

## Verification evidence

- 25 pricing and transactional ledger cases passed against the existing Mongo instance using disposable, isolated databases. They cover concurrent caps, expiry, opt-out, price changes, failed payments, replay, cumulative rounding and audited corrections. Those databases were removed afterward.
- The real Stripe sandbox gateway collected USD 0.60 once: `in_1UQJ4uD2t0mRpwzcVjAKBRj8`.
- It issued a USD 0.10 credit: `in_1UQJ54D2t0mRpwzcKdFCkkjc`. The owned test credit balance was restored to its initial state after verification.
- This gateway test used controlled usage fixtures in an isolated ledger scope; it does not claim a real vendor cost of USD 0.70. Actual hosted WEB6 provider measurement was verified separately with an immutable catalogue and a successful settlement.
- Hosted monthly checkout, Bronze-to-Silver upgrade, one GBP 19.94 proration invoice, period-end cancellation, portal end-date display and renewal restoration all passed for the same Stripe subscription.
- Hosted WEB6 streaming also passed with operation `6185c1c7-1233-f388-6d1a-c1ce00dd3254`: HTTP 200 SSE, 205 prompt tokens, 5 completion tokens, USD 0.00005925, and a succeeded/settled ledger event under `web6-model-catalogue-2026-10-14-launch-v3`. An identical retry returned 409.
- A real signup verification email arrived in a controlled inbox. Its development link verified the account through hosted WEB4, normal OPORTAL source login passed, and an invalid verification token displayed an error. The portal page now rejects application errors inside HTTP 200 responses.
- The owned sandbox subscription's future renewal was disabled after testing. It remains active until its current period ends; the immutable test history is retained.

The protected Vercel development deployment remains a separate hosting acceptance check until the connector can access the OASIS team. Portal browser checks above served the reviewed source locally against deployed APIs. Three unresolved operations on the owned historical test subscriber retain their reservations pending authoritative supplier evidence; they are not silently zeroed or billed as guessed usage. Missing invoices for the still-open UTC month are pending supplier evidence, not a completed reconciliation.

See [the development acceptance checklist](SUBSCRIPTION_DEVELOPMENT_ACCEPTANCE.md) for the completed checks and remaining steps.
