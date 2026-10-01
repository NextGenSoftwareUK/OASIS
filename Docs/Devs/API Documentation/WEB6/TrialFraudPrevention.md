# WEB6 Trial Fraud Prevention — One Trial Per Person

## Goal
Ensure each real person can only ever claim one 14-day free trial, regardless of how many accounts, emails or wallets they create.

## Current State
The trial CTA links to OPORTAL. No trial-claim tracking or deduplication is implemented yet.

---

## What Needs Building

### 1. Trial Claim Record (Backend)
Store a `TrialClaim` record against the avatar's OASIS ID when a trial is activated.

```csharp
// Suggested fields — persist via HolonManager (OASIS Data API, not Redis)
TrialClaimId    Guid
AvatarId        Guid        // OASIS avatar primary key
ClaimedAt       DateTime
PlanTier        string      // "Bronze" | "Silver" | "Gold"
ExpiresAt       DateTime    // ClaimedAt + 14 days
IpAddress       string      // logged, not primary gate
```

- Save via `HolonManager.Instance.SaveHolonAsync` in the `"subscription-trial"` settings category.
- On any trial activation request, check whether a `TrialClaim` already exists for that `AvatarId` — if so, reject.

### 2. Sybil Resistance (What Stops Someone Making a New Avatar?)
A new OASIS avatar costs nothing. To make that meaningless, at least ONE of the following gates must be enforced before a trial is granted:

| Option | Strength | Effort |
|--------|----------|--------|
| **Phone number verification** (SMS OTP via Twilio/Vonage) | High — phone numbers are rate-limited by carriers | Low–Medium |
| **Payment method on file** (card tokenised, not charged) | Very high — real card = real person, deters mass abuse | Medium |
| **Email + manual review** (sales@oasisomniverse.one approves) | Medium — good enough for low volume | Very Low (now) |
| **Proof-of-humanity / Worldcoin** | Very high — biometric uniqueness | High |
| **Small refundable stake** (e.g. $1 hold) | High | Medium |

**Recommended for launch:** Email + manual approval (zero dev cost, viable at low user volume). Add phone OTP when volume grows.

### 3. OPORTAL Trial Flow
1. User clicks "START 14-DAY TRIAL →" on pricing.html → lands on OPORTAL trial page.
2. OPORTAL calls `GET /v1/trial/eligibility?avatarId={id}` — WEB6 returns `{ eligible: bool, reason?: string }`.
3. If eligible and Sybil gate passed → POST `/v1/trial/claim` → WEB6 saves `TrialClaim` holon, activates plan for 14 days.
4. On expiry, plan reverts to Free automatically (cron job or middleware check).

### 4. WEB6 Endpoints to Build

```
GET  /v1/trial/eligibility          # check if avatarId has ever claimed a trial
POST /v1/trial/claim                # record the claim and activate plan
GET  /v1/trial/status               # current trial days remaining for authenticated avatar
```

All three are authenticated (Bearer JWT). `eligibility` can also be called unauthenticated with `?avatarId=` for OPORTAL pre-check.

### 5. Rate Limiting / IP Logging
- Log IP at claim time (not a primary gate — VPNs defeat it — but useful for abuse investigations).
- Rate-limit `/v1/trial/claim` to 1 request per IP per hour as a basic bot deterrent.

---

## Recommended Launch Sequence

1. **Now (zero code):** Manual approval via sales@oasisomniverse.one — suitable for early access.
2. **Phase 1:** Build the three trial endpoints + `TrialClaim` holon storage. OPORTAL calls them.
3. **Phase 2:** Add phone OTP gate in OPORTAL before POST `/v1/trial/claim`.
4. **Phase 3:** Optionally add payment-method-on-file for stronger Sybil resistance.
