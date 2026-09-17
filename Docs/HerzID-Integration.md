# HerzID Integration Guide

## What this document covers

1. What OIDC (OpenID Connect) is — in plain English
2. What HerzID is and how it fits the HERZ ecosystem
3. How HerzID is built on top of the OASIS Avatar SSO system
4. Whether OIDC and HerzID can be used separately
5. API reference for all new endpoints

---

## 1. What is OIDC?

**The short version:** OIDC (OpenID Connect) is the standard that makes "Login with Google" work. It sits on top of OAuth 2.0 and adds one thing: *identity*. OAuth 2.0 only grants access to resources. OIDC also tells you *who the user is*.

**Why it matters:** Without OIDC, every app that wants to know "which OASIS user is this?" has to call OASIS directly and know OASIS's proprietary JWT format. With OIDC, any standards-compliant app — a React frontend, a mobile SDK, a third-party SaaS — can authenticate against OASIS without knowing anything OASIS-specific. They just follow the standard.

**How it works in five steps:**

```
1. User clicks "Login with OASIS" on your app
2. Your app redirects to GET /oauth/authorize?client_id=...&redirect_uri=...
3. OASIS validates the user, issues a short-lived signed auth code, redirects back
4. Your app exchanges the code via POST /oauth/token → gets access_token + id_token
5. Your app can call GET /oauth/userinfo with the access_token to get the user's identity
```

The `id_token` is a signed JWT that includes standard claims: `sub` (user ID), `email`, `name`, and in OASIS: `did`, `herzid`, `herzid_clearance`. Any OIDC library (auth0, nextauth, passport.js) works out of the box.

**OASIS as an identity provider:** The five standard endpoints added in Phase 1:

| Endpoint | Purpose |
|---|---|
| `GET /.well-known/openid-configuration` | Discovery document — tells clients where everything is |
| `GET /oauth/jwks` | Public keys for verifying tokens |
| `GET /oauth/userinfo` | Returns the logged-in user's identity claims |
| `GET /oauth/authorize` | Starts the login flow, issues auth code |
| `POST /oauth/token` | Exchanges code for tokens |

All gated by `OASIS.Security.Oidc.Enabled = true` in `OASIS_DNA.json`.

---

## 2. What is HerzID?

HerzID is the sovereign identity layer for the **HERZ mega-app ecosystem**. It is described in the HerzWorld spec as "White labeled Avatar SSO from OASIS."

Every HERZ member gets a 14-character identity string:

```
052 · 0 · 000 · 000 · 001 · ✦
│     │                     │
│     └── sequential number  └── QEA seal (verified)
└── country code (Mexico = 052)
```

**What makes it sovereign:** The QEA Seal (the ✦ character) is a cryptographic proof. It is an HMAC-SHA256 digest of the member's sequential number, country code, QEA profile, and join date — hashed with a private Wiccian root seed, then reduced to a single alphanumeric character (A-Z or 0-9). The seal cannot be forged without the seed. Anyone can call `GET /api/herzid/verify/{herzId}` to confirm a HerzID is genuine — the way you'd verify an SSL certificate.

**The QEA tier system:** Clearance levels 1-9 map to QEA (Quantum Energy Alignment) tiers:

| Level | Tier | Description |
|---|---|---|
| 1 | Explorer | Before HerzID assigned |
| 2 | Wanderer | HerzID assigned (default on registration) |
| 3 | Tribe Member | Community contributor |
| 4 | Contributor | Active builder |
| 5 | Ally | Trusted partner |
| 6 | Guardian | Ecosystem steward |
| 7 | Elder | Long-standing leader |
| 8 | Flame Keeper | Governance authority (can set others' clearance) |
| 9 | Sovereign / Founder | Unlimited vouches, founding member |

**Vouching — social accountability:** New members cannot self-register. They need to be vouched for by an existing HerzID holder. Every holder starts with 12 vouches to gift. Founders have unlimited vouches. This creates a web-of-trust graph: every HerzID traces back through a vouching chain to a founder.

**Voice biometrics:** The registration flow optionally accepts a `VoiceprintId` — an opaque profile GUID returned by Azure Cognitive Services Speaker Recognition after the member makes their voice declaration. OASIS stores this encrypted (via the 3-layer encryption stack already in place) in the Avatar's `HerzVoiceprintId` field.

---

## 3. How HerzID works with OASIS Avatar SSO

HerzID is not a separate identity system. It is a layer on top of the OASIS Avatar SSO system:

```
┌─────────────────────────────────────────┐
│               HERZ App                  │
│  (uses HerzID for member identity)      │
└────────────────┬────────────────────────┘
                 │ Login with OASIS (OIDC)
                 ▼
┌─────────────────────────────────────────┐
│         OASIS Avatar SSO (OIDC)         │
│  JWT includes herzid + herzid_clearance  │
│  when the avatar has a HerzID assigned  │
└────────────────┬────────────────────────┘
                 │ Stored on
                 ▼
┌─────────────────────────────────────────┐
│             OASIS Avatar Holon          │
│  Fields: HerzId, HerzCountryCode,       │
│  HerzSequentialNumber, HerzClearance,   │
│  HerzVoucherId, HerzVouchesRemaining,   │
│  HerzIdAssignedDate, HerzVoiceprintId,  │
│  HerzQeaProfile                         │
└─────────────────────────────────────────┘
```

**The flow for a HERZ user:**

1. User creates an OASIS Avatar (standard registration)
2. An existing HerzID holder vouches for them: `POST /api/herzid/vouch`
3. User registers their HerzID: `POST /api/herzid/register`
   - Atomic sequential number is claimed (thread-safe, persisted to OASIS storage)
   - QEA seal is computed locally via HMAC-SHA256 (optionally issued by Wiccian Registry)
   - HerzID string is assembled and saved to the Avatar
   - Voucher's remaining count is decremented
4. Future OASIS JWT tokens now include `herzid` and `herzid_clearance` claims automatically
5. The HERZ app reads these from the OIDC `userinfo` endpoint

**What this means in code:** When `AvatarManager.SaveAvatar()` builds the JWT, it already includes:
```json
{
  "sub": "uuid",
  "email": "user@example.com",
  "name": "Display Name",
  "herzid": "0520000000001A",
  "herzid_clearance": 2
}
```

No code changes needed in apps that consume these tokens — the claims appear automatically once a HerzID is assigned.

---

## 4. Can OIDC and HerzID be used separately?

**Yes, completely independently.**

| Scenario | OIDC | HerzID |
|---|---|---|
| Regular OASIS app (login, avatars, holons) | Optional | Not needed |
| Standard OIDC SSO (Login with OASIS) | ✓ Required | Not needed |
| HERZ ecosystem (sovereign member identity) | Optional | ✓ Required |
| Full HERZ + OIDC (recommended) | ✓ Required | ✓ Required |

**OIDC without HerzID:** Enable `Oidc.Enabled = true` in OASIS_DNA.json. Any app can now use OASIS as an SSO provider. The `herzid` claim will simply be absent or empty for users who haven't been assigned a HerzID.

**HerzID without OIDC:** The HerzID endpoints work entirely independently. Apps can call `POST /api/herzid/register` and `GET /api/herzid/verify/{herzId}` without enabling OIDC at all. The HerzID is stored on the Avatar and returned by the existing proprietary OASIS JWT.

**Both together (recommended for HERZ):** Enable both `Oidc.Enabled = true` and `HerzId.Enabled = true`. The OIDC flow carries the HerzID claims, making OASIS a sovereign identity provider that any standards-compliant app can integrate with.

---

## 5. API Reference

All HerzID endpoints require OASIS JWT authentication (`Authorization: Bearer <token>`) unless marked **[public]**.

Enable HerzID by setting `OASIS.Security.HerzId.Enabled = true` in `OASIS_DNA.json` or via environment variable. All endpoints return `404` when disabled.

### POST /api/herzid/register

Assigns a HerzID to the authenticated avatar. The avatar must have been vouched for first.

**Request body:**
```json
{
  "countryCode": "052",
  "qeaProfile": "369-144-999",
  "voucherHerzId": "052000000001A",
  "voiceprintId": "azure-profile-guid-optional"
}
```

**Response 200:**
```json
{
  "herzId": "052·0·000·000·001·✦",
  "countryCode": "052",
  "sequentialNumber": 1,
  "sealChar": "A",
  "displayGlyph": "✦",
  "clearanceLevel": 2,
  "assignedAt": "2026-09-13T12:00:00Z",
  "registryCertificateIssued": false,
  "message": "HerzID assigned successfully. You have 12 vouches to gift."
}
```

---

### POST /api/herzid/vouch

Gifts one vouch to a new member (prerequisite before they can register).

**Request body:**
```json
{
  "newMemberAvatarId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
}
```

**Response 200:**
```json
{
  "success": true,
  "vouchesRemaining": 11,
  "message": "Vouch recorded for avatar ..."
}
```

---

### GET /api/herzid/verify/{herzId}  [public]

Verifies a HerzID's QEA seal by recomputing it from the member's stored data. Does not require authentication.

**Response 200:**
```json
{
  "valid": true,
  "herzId": "052·0·000·000·001·✦",
  "clearanceLevel": 2,
  "qeaProfile": "369-144-999",
  "assignedDate": "2026-09-13T00:00:00Z",
  "message": "✦ QEA VERIFIED — genuine, aligned."
}
```

---

### GET /api/herzid/profile

Returns the authenticated user's full HerzID profile.

**Response 200:**
```json
{
  "herzId": "052·0·000·000·001·✦",
  "countryCode": "052",
  "sequentialNumber": 1,
  "globalRank": 1,
  "localRank": "Member #1 in country 052",
  "clearanceLevel": 2,
  "qeaTier": "QEA-2 (Wanderer)",
  "qeaProfile": "369-144-999",
  "assignedDate": "2026-09-13T12:00:00Z",
  "vouchesRemaining": 12,
  "hasVoiceprint": false
}
```

---

### POST /api/herzid/set-clearance

Admin endpoint — requires clearance level 8 (Flame Keeper) or 9 (Founder).

**Request body:**
```json
{
  "avatarId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "clearanceLevel": 5
}
```

**Response 200:**
```json
{
  "message": "Clearance level updated to 5 (QEA-5 (Ally))."
}
```

---

## 6. Configuration

All settings live under `OASIS.Security` in `OASIS_DNA.json`:

```json
{
  "HerzId": {
    "Enabled": false,
    "QeaPrivateSeed": "",
    "WiccianRegistryUrl": "",
    "WiccianApiKey": "",
    "AzureSpeakerRecognitionEndpoint": "",
    "AzureSpeakerRecognitionKey": "",
    "NewMemberVouches": 12,
    "SequentialDigits": 10,
    "QeaSealDisplayGlyph": "✦"
  }
}
```

**Production secret management via environment variables (never commit secrets):**

| Variable | Purpose |
|---|---|
| `OASIS_HERZID_QEA_SEED` | Private seed for QEA seal HMAC (highest priority) |
| `OASIS_WICCIAN_API_KEY` | Wiccian QEA Registry API key |
| `OASIS_AZURE_SPEECH_KEY` | Azure Speaker Recognition subscription key |

---

## 7. Implementation status

### Phase 2 — Done (commits c3040a4e9, 03198a87d)

| Item | File |
|---|---|
| All 5 HerzID endpoints (register, vouch, verify, profile, set-clearance) | `Controllers/HerzIdController.cs` |
| In-process atomic sequential counter (`Interlocked.Increment`, seeds from OASIS Holon) | `Services/HerzCounterService.cs` |
| Request/response models | `Models/HerzId/HerzIdModels.cs` |
| DI registration, `HerzIdSettings` in OASISDNA, `HerzId.Enabled` feature flag | Done |

### Phase 3 — Done

All remaining items have been implemented:

| Item | Notes |
|---|---|
| OIDC discovery endpoints | `OidcController.cs` — all 5 endpoints active |
| Avatar fields | All 9 `Herz*` fields on `IAvatar` / `Avatar`; `VoiceprintId` + `BiometricEnrolled` for general biometrics |
| Distributed sequential counter | `DistributedHerzCounterService` — optimistic read-increment-write with 10-retry back-off; safe for multi-pod Railway deployments |
| Wiccian Registry integration | `QeaSealService.TryIssueRegistryCertificateAsync` — fires when `WiccianRegistryUrl` is configured |
| Voice biometrics (HerzID) | `AzureVoiceBiometricService` — enroll/verify/delete via Azure Cognitive Services Speaker Recognition; stored as `HerzVoiceprintId` |
| Voice biometrics (general OASIS) | `BiometricController` — `POST /api/biometric/voice/enroll`, `POST /api/biometric/voice/verify`, `DELETE /api/biometric/voice`; stored as `VoiceprintId` on any Avatar; configurable via `OASIS.Security.Biometric` in OASISDNA |
| Vouching graph queries | `GET /api/herzid/vouch-chain/{herzId}` (upward to founder); `GET /api/herzid/vouches-issued` (downward) |
| Ghost-account detection | `POST /api/herzid/ghost-check/{herzId}` — risk score + signals; requires clearance 8+ |

---

## 8. General biometric authentication (all OASIS Avatars)

Any OASIS Avatar — with or without a HerzID — can enroll a voice biometric. This is independent of HerzID.

### Configuration (`OASIS.Security.Biometric` in `OASIS_DNA.json`)

```json
{
  "Biometric": {
    "Enabled": false,
    "VoiceEnabled": false,
    "RequireForLogin": false,
    "RequireForSensitiveOps": false,
    "AzureSpeakerRecognitionEndpoint": "",
    "AzureSpeakerRecognitionKey": "",
    "VoiceVerificationMinScore": 0.5
  }
}
```

| Variable | Purpose |
|---|---|
| `OASIS_AZURE_SPEECH_ENDPOINT` | Azure endpoint (takes priority over OASISDNA) |
| `OASIS_AZURE_SPEECH_KEY` | Azure subscription key (takes priority over OASISDNA) |

### Biometric API endpoints

| Endpoint | Description |
|---|---|
| `GET /api/biometric/status` | Returns enrollment status for the authenticated avatar |
| `POST /api/biometric/voice/enroll` | Enrol a voice biometric (multipart form `audio` field, WAV/OGG/MP3, ≥20 s) |
| `POST /api/biometric/voice/verify` | Verify voice against enrolled profile (multipart form `audio` field) |
| `DELETE /api/biometric/voice` | Remove voice biometric from Azure and Avatar |
| `POST /api/biometric/voice/verify/{avatarId}` | Admin: verify another avatar's voice (clearance 8+ required) |

### Vouching graph and ghost-account endpoints

| Endpoint | Auth | Description |
|---|---|---|
| `GET /api/herzid/vouch-chain/{herzId}` | Public | Walks the vouch chain upward to the founder (max 50 hops) |
| `GET /api/herzid/vouches-issued` | OASIS JWT | Returns all members this avatar has vouched for |
| `POST /api/herzid/ghost-check/{herzId}` | HerzID clearance 8+ | Returns risk score + signals for ghost-account detection |

**Ghost-check risk signals:**

| Signal | Risk |
|---|---|
| Registered within last 24 h | +1 |
| No voice biometric enrolled | +1 |
| Clearance still at level 1 | +1 |
| Voucher issued > 2× allocation in 30 days | +3 |
| Voucher total issues > allocation | +2 |

Score 0 = Low, 1–2 = Moderate, 3–4 = High, 5+ = Critical.

---

## 9. Technical notes

**Sequential number atomicity:** `HerzCounterService` uses `Interlocked.Increment` for in-process atomicity. It seeds from a dedicated OASIS Holon on startup and persists the high-water mark fire-and-forget after each increment. A crash between increment and persist causes a *gap* (not a duplicate) — acceptable for member numbers. For horizontal scaling, replace with a distributed SQL sequence.

**QEA seal algorithm:**
```
HMAC-SHA256(seed, "{seq}|{country}|{profile}|{joinDate:yyyy-MM-dd}")
→ sum all bytes → mod 36 → 0-9 or A-Z
```

The stored seal is the alphanumeric character. The `✦` glyph shown to users is a purely display-layer mapping configured in `OASISDNA.HerzId.QeaSealDisplayGlyph`.

**No Redis:** All persistence goes through the OASIS Data Provider layer (HolonManager). No external cache required.

**Backwards compatibility:** All new fields on IAvatar/Avatar default to null/0/empty. All endpoints return 404 when `HerzId.Enabled = false`. Existing OASIS deployments are completely unaffected.
