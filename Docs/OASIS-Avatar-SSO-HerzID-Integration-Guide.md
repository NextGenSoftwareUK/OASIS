# OASIS Avatar SSO & HerzID — Developer Integration Guide

**Audience:** Third-party developers integrating OASIS Avatar authentication, karma, or HerzID into their own application.  
**Base URL:** `https://api.oasisomniverse.one/api` (dev: `https://dev.api.web4.oasisomniverse.one/api`)

---

## Table of Contents

1. [Overview](#1-overview)
2. [Quick Start — 5 minutes to "Login with OASIS"](#2-quick-start)
3. [Authentication — three ways to integrate](#3-authentication)
4. [Avatar API reference](#4-avatar-api)
5. [Karma API reference](#5-karma-api)
6. [HerzID — what it is and how to add it](#6-herzid)
7. [HerzID API reference](#7-herzid-api)
8. [Voice biometrics](#8-voice-biometrics)
9. [SDK & code examples](#9-sdk--code-examples)
10. [Configuration checklist](#10-configuration-checklist)
11. [Error handling](#11-error-handling)
12. [Changelog](#12-changelog)

---

## 1. Overview

OASIS provides three things a third-party app can use:

| Feature | What it gives you |
|---------|-------------------|
| **Avatar SSO** | "Login with OASIS" — authenticate users against the OASIS Avatar system |
| **Karma** | A cross-app reputation score that follows the user across any OASIS-connected app |
| **HerzID** | A sovereign, cryptographically-sealed member identity for the Enlightened Nations / HerzWorld ecosystem |

These are independent layers. You can use any one without the others.

---

## 2. Quick Start

The fastest path: authenticate a user and get their identity in 5 minutes.

### Option A — Direct JWT (your own backend)

```http
POST /api/avatar/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "their-password"
}
```

```json
{
  "isSuccess": true,
  "result": {
    "id": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
    "username": "alice",
    "email": "user@example.com",
    "token": "eyJ...",
    "refreshToken": "..."
  }
}
```

Store `token` as a Bearer token. Pass it in the `Authorization` header on every subsequent request:

```
Authorization: Bearer eyJ...
```

### Option B — OIDC / "Login with OASIS" (recommended for web apps)

Redirect the user to:

```
GET /oauth/authorize
  ?client_id=YOUR_CLIENT_ID
  &redirect_uri=https://yourapp.com/auth/callback
  &response_type=code
  &scope=openid email profile herzid
  &state=RANDOM_CSRF_TOKEN
```

The user logs in on the OASIS side. OASIS redirects back to your `redirect_uri` with `?code=AUTH_CODE`.

Exchange the code:

```http
POST /oauth/token
Content-Type: application/x-www-form-urlencoded

grant_type=authorization_code
&code=AUTH_CODE
&client_id=YOUR_CLIENT_ID
&client_secret=YOUR_CLIENT_SECRET
&redirect_uri=https://yourapp.com/auth/callback
```

You receive an `access_token` and an `id_token` (a signed JWT). The `id_token` contains:

```json
{
  "sub": "avatar-uuid",
  "email": "user@example.com",
  "name": "Alice",
  "herzid": "052·0·000·000·001·✦",
  "herzid_clearance": 3
}
```

`herzid` and `herzid_clearance` are absent for users who haven't registered a HerzID — handle their absence gracefully.

> **OIDC discovery doc:** `GET /.well-known/openid-configuration` — plug this URL into any OIDC library (NextAuth, Passport.js, Auth0 Actions, etc.) and it configures itself automatically.

---

## 3. Authentication

### 3.1 Direct login (server-to-server or mobile)

| Endpoint | Method | Auth required |
|----------|--------|---------------|
| `/api/avatar/login` | POST | None |
| `/api/avatar/refresh` | POST | Refresh token |
| `/api/avatar/logout` | POST | Bearer |

**Login request:**
```json
{
  "email": "user@example.com",
  "password": "password"
}
```

**Login response:**
```json
{
  "isSuccess": true,
  "result": {
    "id": "uuid",
    "username": "alice",
    "email": "user@example.com",
    "token": "eyJ...",
    "refreshToken": "...",
    "tokenExpires": "2026-10-18T12:00:00Z"
  }
}
```

Tokens are short-lived (15 minutes by default). Use the refresh token endpoint to get a new one without re-authenticating:

```http
POST /api/avatar/refresh
Content-Type: application/json

{ "refreshToken": "..." }
```

### 3.2 OIDC (recommended for web)

OASIS is a fully standards-compliant OpenID Connect provider. Any OIDC library works.

**Discovery endpoint:** `GET /.well-known/openid-configuration`

**Scopes:**

| Scope | Claims included |
|-------|-----------------|
| `openid` | `sub` |
| `email` | `email` |
| `profile` | `name`, `username` |
| `herzid` | `herzid`, `herzid_clearance`, `herzid_country` |

**Token endpoint:** `POST /oauth/token`  
**UserInfo endpoint:** `GET /oauth/userinfo`  
**JWKS (public keys):** `GET /oauth/jwks`

**Example — NextAuth.js:**

```js
// pages/api/auth/[...nextauth].js
import NextAuth from 'next-auth';

export default NextAuth({
  providers: [{
    id: 'oasis',
    name: 'OASIS',
    type: 'oauth',
    wellKnown: 'https://api.oasisomniverse.one/.well-known/openid-configuration',
    clientId: process.env.OASIS_CLIENT_ID,
    clientSecret: process.env.OASIS_CLIENT_SECRET,
    authorization: { params: { scope: 'openid email profile herzid' } },
    idToken: true,
    profile(profile) {
      return {
        id: profile.sub,
        name: profile.name,
        email: profile.email,
        herzId: profile.herzid,
        herzClearance: profile.herzid_clearance,
      };
    },
  }],
});
```

**Example — Passport.js:**

```js
const { Strategy } = require('openid-client');
const { Issuer } = require('openid-client');

const oasisIssuer = await Issuer.discover('https://api.oasisomniverse.one');
const client = new oasisIssuer.Client({
  client_id: process.env.OASIS_CLIENT_ID,
  client_secret: process.env.OASIS_CLIENT_SECRET,
  redirect_uris: ['https://yourapp.com/auth/callback'],
  response_types: ['code'],
});

passport.use('oasis', new Strategy({ client }, (tokenSet, userinfo, done) => {
  return done(null, {
    id: userinfo.sub,
    email: userinfo.email,
    herzId: userinfo.herzid,
  });
}));
```

### 3.3 Registration

```http
POST /api/avatar/register
Content-Type: application/json

{
  "username": "alice",
  "email": "user@example.com",
  "password": "SecurePass1!",
  "firstName": "Alice",
  "lastName": "Smith",
  "acceptTerms": true
}
```

A verification email is sent. The avatar cannot log in until verified (unless `SendVerificationEmail` is disabled in the OASIS config).

---

## 4. Avatar API

All endpoints require `Authorization: Bearer <token>` unless marked **[public]**.

### GET /api/avatar  
Returns the authenticated user's avatar.

```json
{
  "isSuccess": true,
  "result": {
    "id": "uuid",
    "username": "alice",
    "email": "user@example.com",
    "firstName": "Alice",
    "lastName": "Smith",
    "bio": "...",
    "image": "https://...",
    "karmaScore": 1234,
    "level": 7,
    "createdDate": "2025-01-01T00:00:00Z",
    "lastLoginDate": "2026-10-17T09:00:00Z",
    "herzId": "052·0·000·000·001·✦",
    "herzClearanceLevel": 3
  }
}
```

### GET /api/avatar/{id}  [public]
Returns a public avatar profile by UUID.

### PATCH /api/avatar
Updates the authenticated user's profile.

```json
{
  "firstName": "Alice",
  "bio": "Building for the planet.",
  "image": "https://cdn.example.com/avatar.jpg"
}
```

---

## 5. Karma API

Karma is a cross-app reputation score. It persists across every OASIS-connected application.

### GET /api/karma/{avatarId}  [public]
Returns the karma score for any avatar.

```json
{
  "isSuccess": true,
  "result": {
    "karmaScore": 1234,
    "rank": 42,
    "level": 7,
    "nextLevelAt": 1500
  }
}
```

### POST /api/karma/add
Award karma to an avatar from your application. Requires your application's API key in the `X-Api-Key` header.

```json
{
  "avatarId": "uuid",
  "amount": 10,
  "reason": "Completed a community challenge",
  "karmaType": "karmaAkashicRecord",
  "karmaSourceType": "dApp"
}
```

### GET /api/karma/{avatarId}/history
Returns the karma history for an avatar (paginated). Requires auth.

---

## 6. HerzID

HerzID is a sovereign member identity for the Enlightened Nations / HerzWorld ecosystem. It is layered on top of OASIS Avatar SSO — every HerzID holder is also an OASIS Avatar.

### What a HerzID looks like

```
052 · 0 · 000 · 000 · 001 · ✦
 │                          │
 │   sequential number       └── QEA seal (cryptographic proof)
 └── 3-digit country code (ISO 3166-1 numeric)
```

The `✦` glyph is display-only. The actual stored seal is a single alphanumeric character (A–Z, 0–9) computed via HMAC-SHA256.

### Clearance tiers

| Level | Title | Notes |
|-------|-------|-------|
| 1 | Explorer | Before HerzID assigned |
| 2 | Wanderer | Newly registered (default) |
| 3 | Tribe Member | Community contributor |
| 4 | Contributor | Active builder |
| 5 | Ally | Trusted partner |
| 6 | Guardian | Ecosystem steward |
| 7 | Elder | Long-standing leader |
| 8 | Flame Keeper | Governance authority — can set others' clearance |
| 9 | Sovereign / Founder | Unlimited vouches, founding member |

### The vouching requirement

New members cannot self-register. An existing HerzID holder must vouch for them first. Each holder starts with **12 vouches** to gift; Founders have unlimited. Every HerzID traces back to a founder through a verifiable chain.

**Bootstrapping your integration:** You need at least one Founder HerzID before any of your users can register. Contact Enlightened Nations to register your founder account, or register directly via the OASIS API with founder-level credentials.

### Verifying a HerzID

Anyone can verify a HerzID — no authentication required:

```http
GET /api/herzid/verify/052%C2%B70%C2%B7000%C2%B7000%C2%B7001%C2%B7%E2%9C%A6
```

```json
{
  "valid": true,
  "herzId": "052·0·000·000·001·✦",
  "clearanceLevel": 2,
  "assignedDate": "2026-09-13T00:00:00Z",
  "message": "✦ QEA VERIFIED — genuine, aligned."
}
```

Use this to gate features in your app: check `valid === true` before granting access.

---

## 7. HerzID API

All endpoints require `Authorization: Bearer <token>` unless marked **[public]**.

Enable HerzID by setting `OASIS.Security.HerzId.Enabled = true` in your OASIS deployment config, or via the `OASIS_HERZID_ENABLED=true` environment variable. All endpoints return `404` when disabled.

### POST /api/herzid/vouch
Gift a vouch to a new member. Must be called before the member can register.

```json
{
  "newMemberAvatarId": "uuid-of-new-member"
}
```

```json
{
  "isSuccess": true,
  "result": {
    "success": true,
    "vouchesRemaining": 11,
    "message": "Vouch recorded."
  }
}
```

### POST /api/herzid/register
Register a HerzID for the authenticated avatar. The avatar must have been vouched for.

```json
{
  "countryCode": "052",
  "qeaProfile": "369-144-999",
  "voucherHerzId": "052·0·000·000·001·✦",
  "voiceprintId": "azure-speaker-profile-guid"
}
```

`voiceprintId` is optional — provide it if the user has enrolled a voice biometric.

```json
{
  "isSuccess": true,
  "result": {
    "herzId": "052·0·000·000·002·✦",
    "countryCode": "052",
    "sequentialNumber": 2,
    "clearanceLevel": 2,
    "assignedAt": "2026-10-17T12:00:00Z",
    "message": "HerzID assigned. You have 12 vouches to gift."
  }
}
```

### GET /api/herzid/profile
Returns the authenticated user's full HerzID profile.

```json
{
  "isSuccess": true,
  "result": {
    "herzId": "052·0·000·000·002·✦",
    "countryCode": "052",
    "globalRank": 2,
    "clearanceLevel": 2,
    "qeaTier": "QEA-2 (Wanderer)",
    "assignedDate": "2026-10-17T12:00:00Z",
    "vouchesRemaining": 12,
    "totalVouched": 0,
    "hasVoiceprint": false
  }
}
```

### GET /api/herzid/verify/{herzId}  [public]
Verify a HerzID's cryptographic seal. No authentication required.

```json
{
  "valid": true,
  "herzId": "052·0·000·000·002·✦",
  "clearanceLevel": 2,
  "assignedDate": "2026-10-17T00:00:00Z",
  "message": "✦ QEA VERIFIED — genuine, aligned."
}
```

### GET /api/herzid/vouch-chain/{herzId}  [public]
Returns the vouch chain from a HerzID up to the founding member (max 50 hops). Useful for displaying trust lineage.

```json
{
  "isSuccess": true,
  "result": [
    { "herzId": "052·0·000·000·002·✦", "clearanceLevel": 2, "isFounder": false, "vouchedBy": "052·0·000·000·001·✦" },
    { "herzId": "052·0·000·000·001·✦", "clearanceLevel": 9, "isFounder": true, "vouchedBy": null }
  ]
}
```

### GET /api/herzid/vouches-issued
Returns all members this avatar has vouched for. Requires auth.

### POST /api/herzid/set-clearance
Admin — requires clearance level 8 (Flame Keeper) or 9 (Founder).

```json
{
  "avatarId": "uuid",
  "clearanceLevel": 5
}
```

### POST /api/herzid/ghost-check/{herzId}
Admin — requires clearance level 8+. Returns a risk score to detect potential ghost accounts.

**Risk signals:**

| Signal | Weight |
|--------|--------|
| Registered < 24 hours ago | +1 |
| No voice biometric enrolled | +1 |
| Clearance still at level 1 | +1 |
| Voucher issued > 2× allocation in last 30 days | +3 |
| Voucher total exceeds allocation | +2 |

Score: 0 = Low, 1–2 = Moderate, 3–4 = High, 5+ = Critical.

---

## 8. Voice Biometrics

Optional. Requires Azure Cognitive Services Speaker Recognition to be configured on the OASIS deployment.

All endpoints require `Authorization: Bearer <token>`. Audio uploads are multipart form data (`audio` field), WAV/OGG/MP3, minimum 20 seconds.

| Endpoint | Description |
|----------|-------------|
| `GET /api/biometric/status` | Returns enrollment status for the authenticated avatar |
| `POST /api/biometric/voice/enroll` | Enrol a voice biometric — submit audio, returns a voiceprint profile id |
| `POST /api/biometric/voice/verify` | Verify a voice — returns `{ accepted: bool, score: float }` |
| `DELETE /api/biometric/voice` | Remove the voiceprint from Azure and the avatar |

The `score` returned by verify is 0–1; the minimum acceptable score is configurable (`Biometric.VoiceVerificationMinScore`, default `0.5`).

---

## 9. SDK & Code Examples

### JavaScript / TypeScript (plain fetch, no dependencies)

```ts
const OASIS_BASE = 'https://api.oasisomniverse.one/api';

async function oasisLogin(email: string, password: string) {
  const res = await fetch(`${OASIS_BASE}/avatar/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password }),
  });
  if (!res.ok) throw new Error(`OASIS login failed: ${res.status}`);
  const data = await res.json();
  if (!data.isSuccess) throw new Error(data.message);
  return data.result; // { id, username, email, token, refreshToken }
}

async function getAvatar(token: string) {
  const res = await fetch(`${OASIS_BASE}/avatar`, {
    headers: { Authorization: `Bearer ${token}` },
  });
  return (await res.json()).result;
}

async function getKarma(avatarId: string, token: string) {
  const res = await fetch(`${OASIS_BASE}/karma/${avatarId}`, {
    headers: { Authorization: `Bearer ${token}` },
  });
  return (await res.json()).result; // { karmaScore, rank, level }
}

async function verifyHerzId(herzId: string) {
  const res = await fetch(`${OASIS_BASE}/herzid/verify/${encodeURIComponent(herzId)}`);
  return (await res.json()).result; // { valid, clearanceLevel, ... }
}
```

### Official npm package

```
npm install @oasis/web4-api-client
```

```ts
import { OASISWeb4Client } from '@oasis/web4-api-client';

const client = new OASISWeb4Client({
  apiUrl: 'https://api.oasisomniverse.one/api',
});

const { result } = await client.login('user@example.com', 'password');
const avatar = await client.getAvatar(result.token);
const karma  = await client.getKarma(result.id, result.token);
```

### React hook (if using lightseed or a similar Firebase app)

The lightseed open-source starter ships `useOasisIdentity()` — a self-contained React hook that handles link, unlink, karma sync, and HerzID registration. See [`docs/oasis-herzid-integration.md`](../../lightseed.online/docs/oasis-herzid-integration.md) in the lightseed repo.

### C# / .NET

```csharp
using var http = new HttpClient { BaseAddress = new Uri("https://api.oasisomniverse.one/api/") };

// Login
var loginResp = await http.PostAsJsonAsync("avatar/login", new { email, password });
var login = await loginResp.Content.ReadFromJsonAsync<OasisResult<AuthResult>>();

http.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("Bearer", login.Result.Token);

// Get karma
var karmaResp = await http.GetFromJsonAsync<OasisResult<KarmaResult>>($"karma/{login.Result.Id}");

// Verify a HerzID
var verifyResp = await http.GetFromJsonAsync<OasisResult<HerzVerifyResult>>(
    $"herzid/verify/{Uri.EscapeDataString(herzId)}");
```

---

## 10. Configuration Checklist

If you are running your own OASIS deployment (Railway, Docker, self-hosted):

### Minimum — Avatar SSO only

```json
{
  "OASIS": {
    "Security": {
      "SecretKey": "YOUR_JWT_SIGNING_SECRET_MIN_32_CHARS",
      "JwtTokenExpirationMinutes": 15,
      "RefreshTokenExpirationDays": 7
    }
  }
}
```

Set via environment variable: `OASIS_SECURITY_SECRETKEY=...`

### Add OIDC ("Login with OASIS")

```json
{
  "OASIS": {
    "Security": {
      "Oidc": {
        "Enabled": true,
        "Issuer": "https://api.oasisomniverse.one",
        "ProviderName": "OASIS",
        "AccessTokenLifetimeSeconds": 900
      }
    }
  }
}
```

Register your `client_id` / `client_secret` via the OASIS admin console or the operator contact.

### Add HerzID

```json
{
  "OASIS": {
    "Security": {
      "HerzId": {
        "Enabled": true,
        "QeaPrivateSeed": "GENERATE_A_RANDOM_64_HEX_CHAR_SEED",
        "NewMemberVouches": 12,
        "SequentialDigits": 10,
        "QeaSealDisplayGlyph": "✦"
      }
    }
  }
}
```

**Environment variables (never commit secrets):**

| Variable | Purpose |
|----------|---------|
| `OASIS_SECURITY_SECRETKEY` | JWT signing secret |
| `OASIS_HERZID_QEA_SEED` | QEA seal HMAC private seed |
| `OASIS_WICCIAN_API_KEY` | Wiccian QEA Registry (optional — adds notarisation) |
| `OASIS_AZURE_SPEECH_ENDPOINT` | Azure Speaker Recognition endpoint (voice biometrics) |
| `OASIS_AZURE_SPEECH_KEY` | Azure Speaker Recognition key |

### Rate limiting

```json
{
  "OASIS": {
    "Security": {
      "RateLimiting": {
        "Enabled": true,
        "RequestsPerWindow": 100,
        "WindowSeconds": 60
      }
    }
  }
}
```

---

## 11. Error Handling

All OASIS API responses follow a consistent envelope:

```json
{
  "isSuccess": true | false,
  "message": "Human-readable description",
  "result": { ... } | null
}
```

Always check `isSuccess` before reading `result`. `message` is safe to display to the user.

**HTTP status codes:**

| Code | Meaning |
|------|---------|
| 200 | Success — check `isSuccess` in the body |
| 400 | Bad request — `message` explains what's wrong |
| 401 | Missing or expired Bearer token |
| 403 | Insufficient clearance level |
| 404 | Feature disabled (`HerzId.Enabled = false`) or resource not found |
| 429 | Rate limit exceeded |
| 500 | Server error — report to the OASIS team |

**HerzID-specific errors:**

| Scenario | message |
|----------|---------|
| User has no vouch yet | `"This avatar has not been vouched for."` |
| Voucher has no vouches left | `"Voucher has no remaining vouches."` |
| HerzID already registered | `"This avatar already has a HerzID."` |
| Invalid QEA seal on verify | `valid: false` in the result |

---

## 12. Changelog

| Date | What changed |
|------|-------------|
| 2026-10-17 | This guide published |
| 2026-09-20 | HerzID Phase 3 complete — OIDC, voice biometrics, distributed counter, ghost-check, vouch-chain endpoints |
| 2026-09-13 | HerzID Phase 2 — register, vouch, verify, profile, set-clearance endpoints |
| 2026-09-01 | OASIS Avatar SSO stable on Railway WEB4 |

---

## Questions & support

- **GitHub Issues:** [github.com/NextGenSoftwareUK/OASIS-API](https://github.com/NextGenSoftwareUK/OASIS-API)
- **OASIS Developer Discord:** [oasisomniverse.one/discord](https://oasisomniverse.one/discord)
- **HerzID / Enlightened Nations:** Contact Sarah Elahina via the Enlightened Nations portal
