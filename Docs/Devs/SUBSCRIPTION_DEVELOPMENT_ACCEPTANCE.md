# Subscription development acceptance — 14 October 2026

The implementation is deployed to Railway **development** and OPORTAL **dev**. Stripe is in **test mode**. This is not a production release. The deployed OPORTAL browser checks passed on 14 October after its owner disabled Vercel Authentication. The three historical test operations below still require supplier evidence; their recovery is deferred at the user's request.

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
| Deployed portal | The public development site passed normal login, plan display, usage/overage consent and cap, no-excess pay-now, cancellation display, Stripe billing navigation, AI chat, forged-return rejection, unavailable-price handling, invalid-email-token handling and delegated administrator views/catalogue in Edge. A new sandbox GBP 9 checkout returned to the deployed portal, its signed webhook activated Bronze, and the invoice appeared in Orders. Portal dev merge: `ef50389487665f3757f11f0289167d86342c5b3e`. |
| Pricing persistence | 123-model active catalogue `web6-model-catalogue-2026-10-14-launch-v3` stored through Holon settings; previous immutable version retained; repeated publication rejected. |
| Authorization | Missing overage consent returned 400. Ordinary subscriber admin access returned 403. Temporary operator delegation was removed on WEB4 and WEB6, redeployed, and both former operator requests returned 403. Existing stored Wizard administrators remain supported. |
| Test cleanup | Isolated Mongo test databases removed, sandbox credit balance restored, owned ephemeral WEB7 session ended, and future renewal disabled on the owned sandbox subscription. Immutable primary test ledger history retained. |

## Remaining steps, in order

1. **Resolve historical test supplier evidence when the user is available.** The owned subscriber `73db840c-351b-49e5-aa3a-f0f288005758` has three held operations: `0f43aa73-9db9-9642-daff-497a07a26fb2`, `6b9e339e-e1b8-b0c0-a3be-425f9873b3f8`, and `4e7a716f-6d5b-8b47-df49-65f0a29a2b62`. Obtain terminal supplier evidence using durable attempt correlations and follow the recovery procedure. Audited cost corrections apply after a measured terminal settlement exists; they cannot manufacture missing provider evidence. These arose during tests before the fixes; do not invent zero cost, repeat provider work, or delete immutable events. Supplier invoices for an open calendar month are normally pending until close.
2. **Record that recovery evidence here.** Only then mark development acceptance complete. Production promotion remains the separately agreed release step.

### Deployed browser evidence

- Checkout `cs_test_a1qKVtX8Q3frzpDOl1RrE2C01M0mG7CV6kZ2912PUKqVrlx1hok5NyRSMy`, subscription `sub_1UQNldD2t0mRpwzcmt9CojGB`, invoice `in_1UQNldD2t0mRpwzcsf8vYlQu`: paid GBP 9, Bronze active, return confirmation and invoice display passed on the actual development site. Future renewal was disabled after testing.
- AI chat operation `19137994-20ba-000b-dd02-29c30bdffdd5`: the deployed chat supplied an operation key and displayed the successful WEB6 reply.
- Unconfirmed overage consent was rejected; reviewed version `oasis-paid-overages-2026-10-14-v1` and USD 50 cap persisted; pay-now with no excess created no invoice; ordinary subscriber admin access returned 403. Overages were disabled after the test.
- Stripe billing management displayed Silver GBP 29, its period-end cancellation, the test payment method and GBP/USD invoice history.
- Admin overview, invoices and active catalogue loaded using the supported temporary delegated operator. Delegation was removed afterward and both WEB4 and WEB6 returned 403 to that operator. This was not a real Wizard-account login test.
- An invalid verification token reached development WEB4 and displayed an error even though its HTTP response was 200. A forged checkout-return URL did not activate a plan, and a simulated unavailable catalogue offered no purchase controls.

### Earlier access investigation and current supplier lookup requirements

Before the owner disabled Vercel Authentication, the additional verification attempt on 14 October confirmed GitHub's successful deployment of portal revision `ef50389487665f3757f11f0289167d86342c5b3e` to `https://oportal-41yb33pvq-oasis-team.vercel.app`. Visiting that exact preview redirected to Vercel authentication, and the connected protected-fetch tool returned 403. That browser access blocker is now resolved for the public development domain.

Read-only inspection confirmed all three identified outboxes still have one persisted OpenServ attempt, no provider receipt ID, no measured receipts and no terminal settlement. The existing inference key was also tested against OpenServ's official console `/usage-logs` read endpoint and received 401. Inference credentials therefore did not establish console usage access in this check. Supplier console records are required; no new supplier work was executed and no held accounting event was edited.

All three calls used OpenServ model `gpt-5.4-nano`. Supplier lookup details:

| UTC date/time | Operation ID | Client request correlation |
|---|---|---|
| 2026-10-14 00:51:07.220 | `0f43aa73-9db9-9642-daff-497a07a26fb2` | `0f43aa73-9db9-9642-daff-497a07a26fb2:1` |
| 2026-10-14 04:38:52.803 | `6b9e339e-e1b8-b0c0-a3be-425f9873b3f8` | `6b9e339e-e1b8-b0c0-a3be-425f9873b3f8:1` |
| 2026-10-14 04:53:00.711 | `4e7a716f-6d5b-8b47-df49-65f0a29a2b62` | `4e7a716f-6d5b-8b47-df49-65f0a29a2b62:1` |

Required supplier evidence: request/receipt ID, terminal outcome, actual prompt and completion token counts, charged amount/currency, and a traceable console record/export reference. An absence of local measurements is not a zero-cost supplier receipt.

## Prices and configuration

- [Step-by-step Railway runbook](SUBSCRIPTION_RAILWAY_CONFIGURATION.md): existing DNA, keys, service credentials and worker activation.
- [Paid overage runbook](SUBSCRIPTION_PAID_OVERAGES.md): agreed USD rates, consent defaults, caps and standalone invoices. GBP monthly plans remain unchanged.
- [Request-price JSON](../../ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Configuration/subscription-overage-prices.json).
- [WEB6 model-price JSON](https://github.com/NextGenSoftwareUK/OASIS-WEB6/blob/Development/NextGenSoftware.OASIS.Web6.Core/Configuration/web6-model-catalogue.json): bootstrap file at `C:\Source\OASIS\WEB6\NextGenSoftware.OASIS.Web6.Core\Configuration\web6-model-catalogue.json`; the published Holon settings version is the active runtime catalogue.
- [Usage operations runbook](WEB4_SUBSCRIPTION_USAGE_OPERATIONS.md): recovery and immutable accounting procedures.

Direct WEB4 business routes remain outside the WEB5–WEB10 consumer metering adapters; WEB4 is their subscription and accounting authority. Unsupported provider measurement paths reject execution before supplier work. These are the documented coverage boundaries, not evidence that every provider and every WEB4 endpoint was exercised.
