# Subscription development acceptance — 14 October 2026

The implementation is deployed to Railway **development** and OPORTAL **dev**. Stripe is in **test mode**. This is not a production release. The protected deployed OPORTAL runtime and the historical test operations below still need acceptance evidence before claiming every check is complete.

## Completed checks

| Check | Evidence |
|---|---|
| Railway WEB4–WEB10 | All seven deployments successful at source `a49ee689425bc85d0b9450e71594dc3ec1b31ce9`; existing OASIS DNA and Mongo connection reused; all usage/worker flags enabled. |
| Exact deployed dependency graph | WEB6 pin `9d9565e94f86be4b260988b5efa458c4f5649c4b`. Subscription workflow [38514487085](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/38514487085) passed all protocol stages and published all seven APIs. The subsequent development workflow [38515698969](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/38515698969) also succeeded. |
| Automated coverage | 392 passing xUnit tests, 60 WEB6 protocol assertions and 46 recovery assertions. One gated external application test was skipped by CI; actual Stripe sandbox billing was exercised separately. |
| Normal signup | Registration, actual email delivery to a controlled inbox, delivered-token verification, and normal portal login passed. Invalid-token UI displays an error. |
| Monthly billing | Bronze GBP 9 checkout, signed webhook activation, Silver upgrade with GBP 19.94 proration, invoice display, period-end cancellation and renewal restoration passed for one subscription. |
| Paid overages | Reviewed USD request rates, zero AI margin, explicit version/fingerprint consent, spending cap, quota/payment-failure behavior, immutable statements, and correction accounting tested. Sandbox gateway collected USD 0.60 once and issued a USD 0.10 credit. |
| Provider usage | Supported business operations on WEB5–WEB10 settled; identical retries returned 409. Ordinary and streaming WEB6 requests recorded actual provider usage under immutable prices. Streaming operation `6185c1c7-1233-f388-6d1a-c1ce00dd3254` settled USD 0.00005925. |
| Portal source | Plans, usage, consent, cap, no-excess pay-now, orders, Stripe billing management, cancellation display, AI chat and administrator screens exercised in Edge against hosted APIs. Configuration tests, endpoint drift check, build and Vercel preview checks passed. Latest portal dev merge: `ef50389487665f3757f11f0289167d86342c5b3e`. |
| Pricing persistence | 123-model active catalogue `web6-model-catalogue-2026-10-14-launch-v3` stored through Holon settings; previous immutable version retained; repeated publication rejected. |
| Authorization | Missing overage consent returned 400. Ordinary subscriber admin access returned 403. Temporary operator delegation was removed on WEB4 and WEB6, redeployed, and both former operator requests returned 403. Existing stored Wizard administrators remain supported. |
| Test cleanup | Isolated Mongo test databases removed, sandbox credit balance restored, owned ephemeral WEB7 session ended, and future renewal disabled on the owned sandbox subscription. Immutable primary test ledger history retained. |

## Remaining steps, in order

1. **Restore Vercel deployment read access for `oasis-team`.** The connector can list OPORTAL but team deployment access returns 403. The development URL returns Vercel authentication to an unauthenticated client. Neither a login page nor a successful preview build proves the protected development portal serves the reviewed revision.
2. **Verify the actual protected portal deployment.** Confirm it serves OPORTAL dev revision `ef50389487665f3757f11f0289167d86342c5b3e` or a descendant containing it. Follow [OPORTAL's subscriber and administrator checklist](https://github.com/NextGenSoftwareUK/OPORTAL-JS/blob/dev/docs/SUBSCRIPTION_VERIFICATION.md). Check deployed email verification, sign-in, plan display, checkout confirmation, billing management, usage consent and an actual Wizard admin login. Use the existing Stripe test configuration.
3. **Resolve historical test supplier evidence.** The owned subscriber `73db840c-351b-49e5-aa3a-f0f288005758` has three held operations: `0f43aa73-9db9-9642-daff-497a07a26fb2`, `6b9e339e-e1b8-b0c0-a3be-425f9873b3f8`, and `4e7a716f-6d5b-8b47-df49-65f0a29a2b62`. Obtain terminal supplier evidence using durable attempt correlations and follow the recovery procedure. Audited cost corrections apply after a measured terminal settlement exists; they cannot manufacture missing provider evidence. These arose during tests before the fixes; do not invent zero cost, repeat provider work, or delete immutable events. Supplier invoices for an open calendar month are normally pending until close.
4. **Record the protected-host and recovery evidence here.** Only then mark development acceptance complete. Production promotion remains the separately agreed release step.

## Prices and configuration

- [Step-by-step Railway runbook](SUBSCRIPTION_RAILWAY_CONFIGURATION.md): existing DNA, keys, service credentials and worker activation.
- [Paid overage runbook](SUBSCRIPTION_PAID_OVERAGES.md): agreed USD rates, consent defaults, caps and standalone invoices. GBP monthly plans remain unchanged.
- [Request-price JSON](../../ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Configuration/subscription-overage-prices.json).
- [WEB6 model-price JSON](https://github.com/NextGenSoftwareUK/OASIS-WEB6/blob/Development/NextGenSoftware.OASIS.Web6.Core/Configuration/web6-model-catalogue.json): bootstrap file at `C:\Source\OASIS\WEB6\NextGenSoftware.OASIS.Web6.Core\Configuration\web6-model-catalogue.json`; the published Holon settings version is the active runtime catalogue.
- [Usage operations runbook](WEB4_SUBSCRIPTION_USAGE_OPERATIONS.md): recovery and immutable accounting procedures.

Direct WEB4 business routes remain outside the WEB5–WEB10 consumer metering adapters; WEB4 is their subscription and accounting authority. Unsupported provider measurement paths reject execution before supplier work. These are the documented coverage boundaries, not evidence that every provider and every WEB4 endpoint was exercised.
