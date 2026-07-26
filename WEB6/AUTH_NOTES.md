# Web6 Auth Debugging Notes — 2026-07-05

Full record of the auth investigation and fixes applied during the Leela chatbot integration.
Read this before touching auth in Web6, JwtMiddleware, or leela-chat.js.

---

## Architecture overview

```
Browser (SovereignTrust / Vercel)
  └── leela.js          reads JWT from localStorage (st_session.token)
        └── POST /api/leela-chat (Vercel serverless)
              └── leela-chat.js  injects X-Web6-Api-Key + forwards JWT
                    └── POST https://web6.railway.app/v1/complete  (Railway)
                          └── JwtMiddleware  validates JWT, sets Avatar/AvatarId
                                └── AuthorizeAttribute  accepts key OR JWT
                                      └── AIProviderManager  calls OpenAI/Anthropic/Gemini
```

All OASIS APIs (Web4 = ONODE, Web5 = STAR, Web6 = AI) share **one** `OASIS_DNA.json` on
Railway, one MongoDB, and one SecretKey. JWTs issued by Web4 log-in are valid on Web6.

---

## Errors fixed (in order)

### 1. "WEB6 returned an empty completion response" (generic fallback)

**Root cause A — PascalCase/camelCase mismatch**
`OASISResult<T>` serializes with PascalCase (`Message`, `IsError`, `Result`).
The `@oasisomniverse/web6-api` npm package's httpClient.js returns camelCase
(`isError`, `message`, `result`). The original leela-chat.js only checked lowercase,
so `r.message` was always `undefined` and a vague fallback message was thrown.

Fix: check both cases in leela-chat.js:
```js
const isError = r.isError ?? r.IsError ?? false;
const content = r.result?.content ?? r.result?.Content ?? r.Result?.content ?? r.Result?.Content;
const msg     = r.message || r.Message || r.detailedMessage || r.DetailedMessage;
```

**Root cause B — provider returning null content with no exception**
AIProviderManager returned `null` Content silently when the AI provider gave a response
with no `content` field. leela-chat.js received a success-shaped `OASISResult` with an
empty `content`, triggering the generic error.

Fix: explicit null checks in `AIProviderManager.cs` for OpenAI, Anthropic, and Gemini —
throw `InvalidOperationException` with `finish_reason` and refusal detail instead of
returning null.

---

### 2. 401 Unauthorized — JWT never forwarded

leela.js fetch had no `Authorization` header; leela-chat.js didn't forward the JWT.

Fix: leela.js reads `st_session.token` from localStorage and sends it as
`Authorization: Bearer <token>` to the Vercel function. leela-chat.js uses
`getBearerToken(req)` from `_oasis.js` to extract it and injects it via `fetchImpl`.

---

### 3. 401 — Avatar DB load failing even though JWT was valid

`JwtMiddleware` validated the JWT and extracted the avatar ID, then called
`AvatarManager.LoadAvatarAsync`. When that failed (DB not configured / provider error),
`Avatar` stayed `null`. The original `AuthorizeAttribute` only checked for a non-null
`Avatar`, so it rejected the request.

Fix: `JwtMiddleware` now sets `context.Items["AvatarId"]` from JWT claims **before**
attempting the DB load. `AuthorizeAttribute` accepts either `Avatar != null` OR
`AvatarId != Guid.Empty`. The DB load is best-effort inside an inner try/catch.

---

### 4. 401 — JwtMiddleware was never registered in Program.cs

The biggest bug: Web6 `Program.cs` never called `app.UseMiddleware<JwtMiddleware>()`.
The middleware class existed (copied from Web4) but was never wired into the pipeline.
Every JWT was silently ignored from day one. Web4 and Web5 both had the registration;
Web6 was missing it.

Fix: added `app.UseMiddleware<JwtMiddleware>()` to Program.cs, after `UseCors` and
before `UseAuthorization` / `MapControllers`.

---

### 5. MissingMethodException — GetValidLkgConfigurations

After adding the middleware, a `MissingMethodException: GetValidLkgConfigurations` was
thrown at JWT validation time.

Root cause: Web6 WebAPI inherited `System.IdentityModel.Tokens.Jwt 6.35.0` from OASIS
API Core (which pins it). A transitive dependency pulled in a newer
`Microsoft.IdentityModel.Tokens` that added `GetValidLkgConfigurations()` — a method
the 6.35.0 library tries to call but cannot resolve at runtime.

Fix: added an explicit package reference in `NextGenSoftware.OASIS.Web6.WebAPI.csproj`:
```xml
<PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="8.19.1" />
```
This matches ONODE (Web4) and resolves the conflict.

---

### 6. IDX10206 — Unable to validate audience (audience parameter is empty)

After fixing the NuGet version, `TokenValidationParameters` with `ValidateAudience = true`
(the default) threw `IDX10206` because Web4-issued JWTs contain no `aud` claim.
The older 6.35.0 library was lenient; 8.19.1 is strict.

Fix: `ValidateAudience = false` in JwtMiddleware.

**Security rationale:** audience validation protects against using a token issued for
service A on service B. All OASIS APIs share one secret key and no multi-audience
architecture, so there is no attack surface this check would protect against.
`ValidateIssuerSigningKey = true` is still enforced — the HMAC-SHA256 signature is
always verified, so a forged token is cryptographically rejected.

---

### 7. IDX10211 — Issuer validation failed (issuer parameter is null or whitespace)

Same root cause as #6. Web4 JWTs contain no `iss` claim; 8.19.1 rejects them by default.

Fix: `ValidateIssuer = false` in JwtMiddleware.

Same security rationale: no multi-issuer scenario exists in the shared-secret OASIS
architecture.

---

### 8. API key stopped working after re-enabling it

After fixing JWT validation, adding both `X-Web6-Api-Key` and `Authorization: Bearer`
headers to the same request caused the API key to stop working.

Root cause: The outer `catch` in JwtMiddleware was writing a `401` response directly
(`context.Response.WriteAsync`) before calling `await _next(context)`. This killed the
HTTP response before `AuthorizeAttribute` had a chance to check the API key.

Fix: changed the outer catch in JwtMiddleware to `catch { }` (silent skip). Auth
rejection is now the sole responsibility of `AuthorizeAttribute`, which checks the API
key first (see section below).

---

## Auth precedence (final state)

```
Request arrives at Web6
│
├─ JwtMiddleware (always runs, never writes 401)
│   ├─ Extract Bearer token from Authorization header
│   ├─ Validate HMAC-SHA256 signature against OASIS SecretKey
│   ├─ Set context.Items["AvatarId"] = id from claims
│   └─ Try load full Avatar from DB → set context.Items["Avatar"] (best-effort)
│
└─ AuthorizeAttribute (on protected controllers/actions)
    ├─ 1. Check X-Web6-Api-Key header against WEB6_API_KEY env var → ACCEPT if match
    ├─ 2. Check context.Items["Avatar"] != null → ACCEPT if set
    ├─ 3. Check context.Items["AvatarId"] != Guid.Empty → ACCEPT if set
    └─ 4. None matched → return 401
```

---

## Environment variables required

| Variable | Where | Purpose |
|---|---|---|
| `WEB6_API_KEY` | Railway + Vercel | Pre-shared key for Vercel → Web6 server-to-server auth |
| `OASIS_DNA` | Railway (shared) | Full OASIS DNA JSON including SecretKey, MongoDB URI, AI provider keys |

Generate a new API key: `node -e "console.log(require('crypto').randomBytes(20).toString('hex'))"`

---

## Files modified

| File | Change |
|---|---|
| `WEB6/NextGenSoftware.OASIS.Web6.WebAPI/Program.cs` | Added missing `app.UseMiddleware<JwtMiddleware>()` |
| `WEB6/NextGenSoftware.OASIS.Web6.WebAPI/NextGenSoftware.OASIS.Web6.WebAPI.csproj` | Pinned `System.IdentityModel.Tokens.Jwt` to 8.19.1 |
| `WEB6/NextGenSoftware.OASIS.Web6.WebAPI/Middleware/JwtMiddleware.cs` | `ValidateIssuer/Audience = false`; silent outer catch; AvatarId set before DB load; inner catch for DB failure |
| `WEB6/NextGenSoftware.OASIS.Web6.WebAPI/Attributes/AuthorizeAttribute.cs` | Added API key check (first); added AvatarId fallback check |
| `WEB6/NextGenSoftware.OASIS.Web6.Core/Managers/AIProviderManager.cs` | Null content guards for OpenAI, Anthropic, Gemini — explicit exception instead of silent null |
| `trust/api/leela-chat.js` | fetchImpl injects X-Web6-Api-Key + forwarded JWT; both-case OASISResult property checks; diagnostic logging |
| `trust/js/leela.js` | Reads JWT from localStorage and sends as Authorization header to /api/leela-chat |
