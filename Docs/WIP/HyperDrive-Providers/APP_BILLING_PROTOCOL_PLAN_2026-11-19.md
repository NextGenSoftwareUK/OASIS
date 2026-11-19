# App billing completion and acceptance — 19 November 2026

The complete app billing path remains required. Price/statement reads are the first implementation stage, not the end state or payment acceptance. Work remains on the existing HyperDrive/OPORTAL branches; shared Atlas, paused workers, protected deliveries and deployment holds remain unchanged.

## Evidence and existing implementation

Development18675d6 has the reviewed app catalogue app-overages-2026-11-13-v1: USD, zero AI margin, Bronze0.001/Silver0.0005/Gold0.0002/Enterprise0.0001 per extra request. The actual JSON is integrated byte-for-byte; do not invent prices or ask David to choose them again. The earlier AppOveragePrices comment saying no prices ship is stale. The new immutable catalogue uses the existing PaidOveragePricing validation/canonical fingerprint and rejects missing/malformed/duplicate terms rather than inventing a disabled/default catalogue.

The task source already has the real StripePaidOverageGateway SDK/client, standalone-USD immutable statements, durable collection leases/idempotency/recovery and signed webhook infrastructure for personal overages. These are dependencies to extend, not a reason to implement a second collector. Existing hosted-payment/Wizard actions stay in ACTIONS-FOR-DAVID.md; do not duplicate or re-ask them.

## Required financial invariants before enabling app writes

- Consent binds authenticated current app ownership, approved price version/fingerprint, currency, rate, spending cap and the verified payer identity. Ownership and local billing state must be rechecked inside the write transaction after remote Stripe readiness verification.
- Each billable reservation freezes its agreed app terms and payer. Later price/cap changes, opt-out, app-plan changes or owner changes must not recalculate or erase prior liability. Legacy settings without the new bound evidence must not be relabelled as newly accepted consent by a read.
- App request exposure remains distinct from personal request/AI exposure. Trace the existing per-user floor and consent semantics before assigning a fee to either account; do not silently change or double-count the charging contract.
- App caps remain exact under concurrent admissions, including all permitted pool shard layouts. Preserve daily calls/tokens/budget/per-user restrictions; app request overages lift only their specified pool request limit.
- Unstarted expiry releases the exact reserved app exposure; unknown execution retains it pending evidence. Settlement, replay and corrections apply audited deltas once. Reconciliation must include every added monetary projection, not ignore new fields.
- Statements allocate immutable operation charges once by app, payer and UTC month. Round cumulatively to USD cents. Rates/caps at statement time must not reprice earlier operations. A deterministic identity and transaction prevent duplicate allocation/collection after restarts or retries.
- Extend the existing collector, canonical Stripe validation, signed webhook and payment-failure fence to the correct app/payer identity. An app-prefixed ledger subject must not be treated as an ordinary avatar ID. Preserve personal overage behavior and paid orders.
- New preparation work must honor the existing application-maintenance pause; do not start a second independent polling worker or enable shared-cluster workers to test it.

The older Development AppBilling/Owner code needs review rather than blind import: it recalculates closed months from current app settings/rates, has broad silent catches, and depends on fields/admission/worker changes absent from this task source. The current SetAppPaygAsync is settings-only and is not proof of charging. Current personal RecordInvoiceAsync targets Subscriptions by statement.UserId; the app identity/payer mapping must be made explicit before app collection is accepted.

## Complete verification scope

Use owned native MongoDB transactions and the shipping SDK/API/browser. Cover changed prices/caps/owners, revoked authorization, disabled nodes, expired/recovery reservations, concurrency at caps on multiple shards, settled/failed outcomes, immutable statement allocation, cumulative rounding, credits, restart/replay, payment failure/unblocking, and zero mutation on rejected input. Retain the exact unchanged personal behavior through relevant new regressions rather than repeating every accepted suite. Actual Stripe sandbox collection/webhook evidence and applicable hosted owner actions remain separate from seeded read fixtures and pure maths. No mocks or generated success in shipping paths.

## Current read stage

The real owner-authorized billing GET, immutable published terms, existing native statements, estimates and API/UI error/session handling passed exact qualification44468037293 on root5e77fc6b/Portalf10f0cad (one new native browser case, three actors, zero failures/skips). This does not accept consent writes, quota admission, financial settlement, invoicing or payment. Source publication and isolated qualification must be recorded separately from running deployments. App UI promotion must be coordinated with the matching API source.


## Consent and opt-out implementation in progress

The current branch now implements explicit immutable financial intent, strict JSON binding, real awaited Stripe readiness, transactional owner/payer/version checks, schema1 bound terms/payer snapshots, protected-ID redaction and opt-out history preservation. This is source implementation, not positive external readiness or charging acceptance. The forthcoming native case proves opt-out/refusals only without any Stripe call or gateway mock. Per-operation frozen charge terms, exact cap exposure/admission, expiry/settlement/correction/reconciliation, statement allocation and actual collection remain mandatory next work. No worker/source/deployed DNA setting is enabled by this stage.
