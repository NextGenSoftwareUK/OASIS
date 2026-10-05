## TimoRides FIAT Wallet & Paystack Integration Plan

### 1. Objectives
- Launch in South Africa with FIAT-first experience while keeping hooks for future OASIS/crypto rail.
- Support rider payments via Paystack checkout (card, EFT, QR) plus M‑Pesa C2B for East African users.
- Automate driver payouts via Paystack Transfers (bank) and M‑Pesa B2C (opt-in), keeping Mongo wallet as shadow ledger.
- Ensure every provider event is auditable, idempotent, and observable.

### 2. High-Level Architecture
```
           +----------------+        charge.success / verify        +----------------------+
           |   Rider Apps   | ------------------------------------> |  Paystack Checkout   |
           +----------------+                                      +----------------------+
                    |                                                         |
                    | webhook (charge.success)                                |
                    v                                                         |
        +------------------------+  credit wallet  +---------------------+    |
        |  Timo API / Webhooks   | <--------------- | paymentsService     | <--+
        +------------------------+                 +---------------------+
                    | payout request                           |
                    v                                          | transfer.create
        +------------------------+                             v
        | paymentRequestModel    | ------------------> +----------------------+
        +------------------------+                     |  Paystack Transfers  |
                   ^                                   +----------------------+
                   | driver bank deposit success/failed          |
                   +---------------------------------------------+
```

### 3. Sequence: Rider Funding via Paystack Checkout
```
1. Rider -> Mobile/Web: POST /api/bookings/:id/pay (client tokenized card or EFT).
2. Client SDK hits Paystack Checkout; Paystack processes payment.
3. Paystack sends `charge.success` webhook (with `reference`, `metadata.bookingId`).
4. /webhooks/paystack (Timo) verifies signature, fetches booking, calls paymentsService.fundWalletFromPaystack().
5. paymentsService marks booking.payment as paid, mirrors amount into rider wallet, logs audit event.
6. Response 200 to Paystack; booking moves to “paid/pending driver”.
```

### 4. Sequence: Rider Funding via M‑Pesa C2B (Daraja)
```
  Rider (USSD/App)         Safaricom M-Pesa       Timo Webhook (mpesaGateway)    paymentsService
        |                         |                            |                        |
1. Paybill payment -->           |                            |                        |
        |----- C2B callback ---->|                            |                        |
        |                         |---- POST /webhooks/mpesa -->|                        |
        |                         |                            |-- validate shortCode -->|
        |                         |                            |-- create booking credit->|
        |                         |                            |                        |
        |<---- SMS confirmation --|                            |                        |
```

### 5. Sequence: Driver Payout via Paystack Transfers
```
Driver App          Timo API            paymentsService          Paystack API          Driver Bank
    |                   |                        |                     |                     |
1. requestPayment ----> |                        |                     |                     |
    |                   |-- validate KYC/bank -->|                     |                     |
    |                   |                        |-- ensure recipient->|                     |
    |                   |                        |   (transferrecipient)|                     |
    |                   |                        |-- initiate transfer->|                     |
    |                   |                        |                     |-- debit balance --->|
    |                   |<-- 202 Accepted -------|                     |                     |
    |                   |                        |<-- webhook success --|                     |
    |                   |<-- status update ------|                     |                     |
    |                   |                        |                     |<-- funds credited --|
```

### 6. Sequence: Driver Payout via M‑Pesa B2C
```
Driver App          Timo API        paymentsService     MpesaGateway        Safaricom B2C
    |                   |                  |                |                    |
1. requestPayment ----> |                  |                |                    |
    |                   |-- choose mpesa ->|                |                    |
    |                   |                  |-- get token -->|                    |
    |                   |                  |-- send B2C ---->------------------->|
    |                   |                  |                |<-- result codes ----|
    |                   |                  |<-- update paymentRequest -----------|
```

### 7. Key Components & Ownership
- `services/gateways/paystackGateway.js` – REST client for `/transaction/verify`, `/transferrecipient`, `/transfer`, signature validation helper.
- `services/gateways/mpesaGateway.js` – OAuth token cache + STK Push/C2B/B2C wrappers.
- `services/paymentsService.js` – Orchestrates wallet updates, audit logging, and idempotency keyed by `providerReference`.
- `routes/webhooks.js` – Exposes `/webhooks/paystack`, `/webhooks/mpesa` using dedicated middleware to verify headers and store raw payload.
- `userController.requestPayment` – Validates payout method, enqueues payout via `paymentsService`.
- `paymentRequestModel` – Extend with `provider`, `providerReference`, `recipientCode`, `statusHistory`, `failureReason`.

### 8. Configuration Matrix
| Provider  | Required ENV Keys | Notes |
|-----------|-------------------|-------|
| Paystack  | `PAYSTACK_SECRET_KEY`, `PAYSTACK_BASE_URL`, `PAYSTACK_WEBHOOK_SECRET`, `PAYSTACK_TRANSFER_SOURCE`, `PAYSTACK_DEFAULT_CURRENCY=ZAR` | Use live vs. test URLs by NODE_ENV; store webhook raw body for replays. |
| M‑Pesa    | `MPESA_CONSUMER_KEY`, `MPESA_CONSUMER_SECRET`, `MPESA_PASS_KEY`, `MPESA_SHORT_CODE`, `MPESA_INITIATOR_NAME`, `MPESA_INITIATOR_PASSWORD`, `MPESA_ENV=sandbox|production` | Sandbox uses HTTPS with base64 passwords; rotate initiator creds regularly. |

#### 8.1 Test Credential Snapshot (Current)
```
PAYSTACK_SECRET_KEY=sk_test_c80ec29cce4d86ffc92cd2e24e8a8faa8135f246
PAYSTACK_PUBLIC_KEY=pk_test_81814a8cb081d5782c80a98461e7e550463f7fed
PAYSTACK_BASE_URL=https://api.paystack.co
PAYSTACK_DEFAULT_CURRENCY=ZAR
PAYSTACK_TRANSFER_SOURCE=balance
PAYSTACK_WEBHOOK_SECRET=<set after webhook verification>
```
> Store these in `ride-scheduler-be/config/.env` (or your env manager) and never commit real keys.
c xcxm n#### 8.2 Webhook Setup (Test)
1. Start the backend locally and expose it via ngrok: `ngrok http 4205`. Copy the HTTPS URL (e.g., `https://abc123.ngrok.io`).
2. In Paystack Dashboard → `Settings` → `API Keys & Webhooks`, set **Test Webhook URL** to `https://abc123.ngrok.io/webhooks/paystack` and click **Save** (Test Callback URL can stay `https://example.com` for now).
3. In `ride-scheduler-be`, add route `POST /webhooks/paystack` that:
   - Captures raw request body (needed for signature).
   - Validates `x-paystack-signature` using `PAYSTACK_WEBHOOK_SECRET`.
   - Parses event types (`charge.success`, `transfer.success`, `transfer.failed`) and dispatches to `paymentsService`.
4. After the first webhook request, Paystack shows the signing secret; copy it into `PAYSTACK_WEBHOOK_SECRET` and restart the server.

Testing tip: trigger a manual event by running a test transaction in the dashboard or via `https://paystack.com/pay` checkout; confirm the webhook logs and booking/payment records update.

### 9. Observability & Audit
- Structured log `payment.event` with `{ provider, bookingId, paymentRequestId, traceId }`.
- `webhookEventLog` collection storing headers, payload checksum, processing result.
- `/api/health` additions:
  - `providers.paystack.lastChargeSuccess`
  - `providers.paystack.pendingTransfers`
  - `providers.mpesa.oauthExpiresAt`
  - `payments.pendingPayouts`

### 10. Open Questions / Follow-Ups
1. Do we need instant payouts (requires Paystack treasury prefunding) or next-day is fine?
2. Driver KYC source (in-house vs. Paystack Identity vs. third-party like Smile Identity).
3. Do we allow mixed payout destinations (bank + M‑Pesa) per driver or enforce single method?
4. Reconciliation cadence: hourly vs. daily settlement reports from Paystack / Safaricom.
5. Failover plan if Paystack webhook delivery lags (poll `/transaction/verify` cron?).

