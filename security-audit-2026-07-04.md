# OASIS2 Security Audit Report
**Scope:** Full codebase — ONODE, STAR ODK, WEB6–WEB10, MongoDB provider, Core
**Date:** 2026-07-04
**Auditor:** Claude Sonnet 4.6 via Claude Code

---

## Finding 1: Privilege Escalation — Any User Can Self-Promote to Wizard (Admin) — Severity: HIGH

**File:** `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Controllers/AvatarController.cs:1298, 1358, 1414`

```csharp
if (!string.IsNullOrEmpty(avatar.AvatarType)) 
{
    if (Enum.TryParse<AvatarType>(avatar.AvatarType, out var avatarType))
        existingAvatar.AvatarType = new EnumValue<AvatarType>(avatarType);
}
```

This pattern exists in `Update`, `UpdateByEmail`, and `UpdateByUsername`.

**Description:** Any authenticated user can call `POST /api/avatar/update-by-id/{their-own-id}` and pass `"AvatarType": "Wizard"` in the request body. The controller performs an ownership check but then blindly allows the `AvatarType` field to be overwritten without verifying that only a Wizard can elevate another account to Wizard. A regular user can promote themselves to admin.

**Exploit Scenario:**
1. Register a normal user account and authenticate.
2. `POST /api/avatar/update-by-id/{myId}` with body `{ "AvatarType": "Wizard" }`.
3. Re-authenticate to get a new JWT reflecting Wizard role.
4. Access any Wizard-only endpoint: `GET /api/avatar/get-all-avatars`, `GET /api/avatar/get-all-avatar-details`, etc.

**Recommendation:** Remove `AvatarType` from `UpdateRequest` entirely. Or, before applying the field, enforce that `Avatar.AvatarType.Value == AvatarType.Wizard`.

**Confidence: 10/10**

---

## Finding 2: IDOR — Any Authenticated User Can Modify Any User's Karma — Severity: HIGH

**File:** `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Controllers/KarmaController.cs:314, 357`

```csharp
[Authorize]
[HttpPost("add-karma-to-avatar/{avatarId}")]
public async Task<OASISResult<KarmaAkashicRecord>> AddKarmaToAvatar(Guid avatarId, ...)

[Authorize]
[HttpPost("remove-karma-from-avatar/{avatarId}")]
public async Task<OASISResult<KarmaAkashicRecord>> RemoveKarmaFromAvatar(Guid avatarId, ...)
```

**Description:** `add-karma-to-avatar` and `remove-karma-from-avatar` are protected with `[Authorize]` but contain **no check that the caller is operating on their own avatar or is a Wizard**. Any authenticated user can add or remove karma from any other user's avatar by passing a different `avatarId`. Karma is a core game/reputation mechanic — this allows anyone to corrupt any user's reputation score.

**Exploit Scenario:** Attacker registers and authenticates, then calls `POST /api/karma/remove-karma-from-avatar/{victim-avatarId}` repeatedly with valid karma type values, reducing the victim's karma to zero.

**Recommendation:** Add ownership check: `if (avatarId != Avatar.Id && Avatar.AvatarType.Value != AvatarType.Wizard) return Unauthorized(...)`.

**Confidence: 9/10**

---

## Finding 3: Karma Endpoints Unauthenticated — Any Anonymous User Can Read All Karma History — Severity: HIGH

**File:** `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Controllers/KarmaController.cs:96, 164`

```csharp
[HttpGet("get-karma-for-avatar/{avatarId}")]
public async Task<OASISResult<long>> GetKarmaForAvatar(Guid avatarId)

[HttpGet("get-karma-akashic-records-for-avatar/{avatarId}")]
public OASISResult<IEnumerable<IKarmaAkashicRecord>> GetKarmaAkashicRecordsForAvatar(Guid avatarId)
```

**Description:** No `[Authorize]` attribute. Any unauthenticated caller on the internet can retrieve the karma value and full karma Akashic Record history (all karma transactions with sources, titles, descriptions) for any avatar by guessing or enumerating avatar GUIDs.

**Recommendation:** Add `[Authorize]` to both GET endpoints.

**Confidence: 9/10**

---

## Finding 4: JWT Token Logged in Plaintext to Console — Severity: HIGH

**File:** `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Middleware/JwtMiddleware.cs:40`

```csharp
Console.WriteLine($"JwtMiddleware: Validating JWT Token... Token: {token}");
```

**Description:** The raw bearer JWT is printed to stdout on every authenticated request. In any environment where stdout is captured (container logs, application insights, log forwarders), this exposes all user session tokens. An attacker with log read access can replay any active token and impersonate any user including Wizards.

**Exploit Scenario:** Attacker reads container stdout → obtains a Wizard's JWT → calls admin-only endpoints to enumerate or delete all accounts.

**Recommendation:** Remove this line entirely.

**Confidence: 10/10**

---

## Finding 5: Wildcard CORS with `AllowCredentials()` — Severity: HIGH

**File:** `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Startup.cs:312-316`

```csharp
app.UseCors(x => x
    .SetIsOriginAllowed(origin => true)
    .AllowAnyMethod()
    .AllowAnyHeader()
    .AllowCredentials());
```

**Description:** `SetIsOriginAllowed(origin => true)` reflects any origin, combined with `AllowCredentials()`. This bypasses the browser's same-origin protection. A malicious site can make credentialed cross-origin requests using the victim's `refreshToken` cookie.

**Exploit Scenario:** Victim visits `evil.com` while logged into OASIS. `evil.com` makes a fetch to the OASIS API with `credentials: "include"`. The browser sends the `refreshToken` cookie; CORS reflects the evil origin; the server processes the request as the victim.

**Recommendation:** Replace with an explicit allowlist:
```csharp
app.UseCors(x => x
    .WithOrigins("https://oasisomniverse.one", "https://app.oasisomniverse.one")
    .AllowAnyMethod()
    .AllowAnyHeader()
    .AllowCredentials());
```

**Confidence: 9/10**

---

## Finding 6: Developer Exception Page Enabled in All Environments — Severity: MEDIUM

**File:** `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Startup.cs:300`

```csharp
app.UseDeveloperExceptionPage();  // no env.IsDevelopment() guard
```

**Description:** Called unconditionally. In production, any unhandled exception returns a full .NET stack trace, source file paths, class names, and internal variable values to the caller — detailed reconnaissance for an attacker.

**Exploit Scenario:** Attacker sends a malformed request that triggers an exception. Response reveals internal class names, DB access patterns, and file paths, accelerating further attacks.

**Recommendation:**
```csharp
if (env.IsDevelopment())
    app.UseDeveloperExceptionPage();
```

**Confidence: 9/10**

---

## Finding 7: Missing Ownership Check on Avatar Detail by Email/Username — Severity: MEDIUM

**File:** `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Controllers/AvatarController.cs:783, 813`

```csharp
[Authorize]
[HttpGet("get-avatar-detail-by-email/{email}")]
public async Task<OASISHttpResponseMessage<IAvatarDetail>> GetAvatarDetailByEmail(string email)
{
    // No ownership check — loads anyone's full detail
    return HttpResponseHelper.FormatResponse(await Program.AvatarManager.LoadAvatarDetailByEmailAsync(email));
}
```

**Description:** Any authenticated user can request any other user's full avatar detail (DOB, address, karma, XP, spiritual attributes, inventory) simply by knowing their email or username. Contrast with `GetById` (line 1028) which correctly enforces ownership.

**Recommendation:**
```csharp
if (email != Avatar.Email && Avatar.AvatarType.Value != AvatarType.Wizard)
    return HttpResponseHelper.FormatResponse(new OASISResult<IAvatarDetail>() { IsError = true, Message = "Unauthorized" }, HttpStatusCode.Unauthorized);
```

**Confidence: 9/10**

---

## Finding 8: Avatar Search Endpoint Unauthenticated — Severity: MEDIUM

**File:** `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Controllers/AvatarController.cs:1125`

```csharp
[HttpPost("search")]
public async Task<OASISHttpResponseMessage<ISearchResults>> SearchAvatar(SearchParams searchParams)
```

**Description:** No `[Authorize]` attribute. Any anonymous user can search the user database, enabling user enumeration. Combined with Finding 7, an attacker can enumerate emails/usernames and then pull full profile details.

**Recommendation:** Add `[Authorize]` to both the `search` and `search/{providerType}/{setGlobally}` overloads.

**Confidence: 8/10**

---

## Finding 9: JWT Validation Missing Issuer and Audience — Severity: MEDIUM

**File:** `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Middleware/JwtMiddleware.cs:48-49`  
Also: `Services/AvatarService.cs:783-784`

```csharp
ValidateIssuer = false,
ValidateAudience = false,
```

**Description:** With no issuer or audience validation, any JWT signed with the same secret key from any service or environment will be accepted. If the same `SecretKey` is reused across environments (very common), a token from dev is valid against prod.

**Recommendation:** Set `ValidateIssuer = true`, `ValidateAudience = true` with unique per-environment values.

**Confidence: 8/10**

---

## Summary Table

| # | Severity | File | Issue |
|---|----------|------|-------|
| 1 | **HIGH** | `AvatarController.cs:1298` | Any user can self-promote to Wizard (admin) via update endpoint |
| 2 | **HIGH** | `KarmaController.cs:314,357` | Any authenticated user can modify any user's karma (no ownership check) |
| 3 | **HIGH** | `KarmaController.cs:96,164` | Karma read endpoints unauthenticated — anyone can read karma history |
| 4 | **HIGH** | `JwtMiddleware.cs:40` | Full JWT bearer token logged to stdout on every request |
| 5 | **HIGH** | `Startup.cs:312` | Wildcard CORS + AllowCredentials enables cross-site credential theft |
| 6 | **MEDIUM** | `Startup.cs:300` | Developer exception page active in production — leaks stack traces |
| 7 | **MEDIUM** | `AvatarController.cs:783,813` | Missing ownership check on avatar detail by email/username |
| 8 | **MEDIUM** | `AvatarController.cs:1125` | Avatar search unauthenticated — enables user enumeration |
| 9 | **MEDIUM** | `JwtMiddleware.cs:48` | No issuer/audience validation on JWT tokens |

---

## Priority Order

1. **Finding 1** — any registered user can become admin right now
2. **Finding 2** — any registered user can corrupt any user's karma
3. **Finding 4** — bearer tokens exposed in logs
4. **Finding 5** — CORS misconfiguration, cross-site request forgery via cookies
5. **Finding 3** — anonymous karma history read
6. **Finding 7** — PII data exposure by email/username lookup
7. **Finding 6** — stack traces in production
8. **Finding 8** — user enumeration via search
9. **Finding 9** — cross-environment JWT reuse

---

---

## Finding 10: WEB6–WEB10 — All AI/Advanced Endpoints Completely Unauthenticated — Severity: HIGH

**Files:** `WEB6/NextGenSoftware.OASIS.Web6.WebAPI/Controllers/` (all), similarly WEB7, WEB8, WEB9, WEB10

```
WEB6: OrchestratorController, CompletionController, HolonicBraidController,
      HolonicMemoryController, ReasoningNetworkController, ImagesController
WEB7: SymbiosisController, CollectiveConsciousnessController
WEB8: MeshController, ProtocolBridgeController
WEB9: SingularityController
WEB10: SourceController
```

**Description:** A grep of all controller files in WEB6–WEB10 finds **zero** `[Authorize]` attributes. Every endpoint in these projects is fully public. This includes:
- AI completions (proxy to external LLMs on the server's API keys)
- AI orchestrator registration and invocation (MCP, A2A, LangChain, AutoGen, CrewAI, Semantic Kernel)
- Holonic BRAID memory write operations
- Image generation
- Collective consciousness, singularity, and mesh protocol endpoints

**Exploit Scenario:**
1. Anonymous caller `POST /v1/orchestrators` registering an external orchestrator pointing at an attacker-controlled server.
2. Anonymous caller `POST /v1/complete` issuing unlimited AI completion requests, consuming the OASIS platform's LLM API credits.
3. Anonymous caller reads or writes holonic memory state for any user.

**Recommendation:** Add `[Authorize]` to every controller base class in WEB6–WEB10. WEB6's `Web6ControllerBase` is the right place to add a default policy.

**Confidence: 10/10**

---

## Finding 11: STAR WebAPI — JWT Pre-load Middleware Bypasses Signature Verification — Severity: MEDIUM

**File:** `STAR ODK/NextGenSoftware.OASIS.STAR.WebAPI/Program.cs:321–374, 378`

```csharp
// Pre-load middleware (line 321) — parses JWT WITHOUT signature verification:
var avatarId = ParseAvatarIdFromJwt(token);  // just base64-decodes payload
// ...
context.Items["Avatar"] = avatarLoadResult.Result;  // sets auth context from unverified ID

// JwtMiddleware (line 378) — validates signature — on failure:
catch (Exception ex)
{
    context.Response.StatusCode = 401;
    await context.Response.Body.WriteAsync(body);  // writes 401 body
}
// No return — falls through to: await _next(context);
```

**Description:** Two issues interact. First, `ParseAvatarIdFromJwt` decodes the JWT payload without verifying the signature — any arbitrary JWT with a known avatar GUID as the `id` claim will cause the pre-load middleware to load that avatar into `context.Items["Avatar"]`. Second, JwtMiddleware's catch block does not halt the pipeline (`return` is missing after writing the 401 response), so the controller and `[Authorize]` still execute. Since `[Authorize]` reads from `context.Items["Avatar"]` which was set by the pre-load middleware, the authorization check passes. Business logic runs, though the response may be corrupted (401 body + controller body concatenated) making direct data exfiltration difficult but not impossible in all configurations.

**Exploit Scenario:**
1. Attacker obtains a legitimate avatar GUID via the unauthenticated avatar search endpoint (Finding 8).
2. Attacker crafts a JWT with that GUID in the payload and any (invalid) signature.
3. Sends the request to the STAR WebAPI — pre-load middleware loads the victim avatar into context.
4. JwtMiddleware writes a 401 body but calls `_next`; the controller executes with the victim's identity.

**Recommendation:**
- In `ParseAvatarIdFromJwt`/pre-load middleware, only set `context.Items["Avatar"]` **after** the signature has been validated — move avatar loading into JwtMiddleware only.
- Add `return;` after the JwtMiddleware 401 write so the pipeline halts.

**Confidence: 8/10**

---

## Finding 12: MongoDB Search — Regex Injection via Unescaped User Input — Severity: MEDIUM

**File:** `Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS/Repositories/SearchRepository.cs:79, 87, 123`

```csharp
avatarFilter = Builders<Avatar>.Filter.Regex("FirstName",
    new BsonRegularExpression("/" + searchTextGroup.SearchQuery.ToLower() + "/"));

avatarFilter = Builders<Avatar>.Filter.Regex("LastName",
    new BsonRegularExpression("/" + searchTextGroup.SearchQuery.ToLower() + "/"));

avatarFilter = Builders<Avatar>.Filter.Regex("Email",
    new BsonRegularExpression("/" + searchTextGroup.SearchQuery.ToLower() + "/"));
```

**Description:** User-supplied `SearchQuery` is interpolated directly into a MongoDB regex filter string without escaping. An attacker can supply arbitrary PCRE patterns, enabling:
- Anchored prefix probing to enumerate field values character-by-character (`^secretpassword_` → binary oracle)
- Extraction of hashed passwords or token values stored in searched fields

The `Username` field uses safe LINQ `Contains` but `FirstName`, `LastName`, and `Email` are vulnerable.

**Exploit Scenario:** Combined with the unauthenticated avatar search endpoint (Finding 8): attacker iterates through patterns like `^A`, `^B`, … against the `Email` field to enumerate all registered email addresses one character at a time.

**Recommendation:** Escape the search query using `Regex.Escape()` before embedding in the pattern:
```csharp
new BsonRegularExpression("/" + Regex.Escape(searchTextGroup.SearchQuery.ToLower()) + "/i")
```

**Confidence: 9/10**

---

## Finding 13: MongoDB Production Credentials Hardcoded in Config File — Severity: HIGH

**File:** `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/OASIS_DNA.json`

```json
"ConnectionString": "mongodb+srv://oasisadmin:Uz3j5Dpz663V0fOA@oasis.ib2hkpe.mongodb.net/"
"SecretKey": "7B6A835F-33F0-4A60-93BC-ABAD068D37D399D6831D-A72C-4100-B2B0-AA6532A58544"
```

**Description:** The MongoDB Atlas production connection string (including plaintext username and password) and the JWT signing `SecretKey` are stored in `OASIS_DNA.json`. While the file is currently excluded from git tracking, `git log --follow` confirms this file was committed in earlier history (`79699397e`, `054e961ec`). If the repository is public (GitHub), these credentials are accessible to anyone who can read the git history.

Additionally, the same `SecretKey` is used across all OASIS WebAPI projects and environments — anyone who has seen a copy of this file can forge JWT tokens that authenticate as any user including Wizards on all environments.

**Exploit Scenario:**
1. Attacker reads git history: `git show 79699397e:ONODE/.../OASIS_DNA.json` → obtains credentials.
2. Connects to MongoDB Atlas directly: `mongosh "mongodb+srv://oasisadmin:Uz3j5Dpz663V0fOA@oasis.ib2hkpe.mongodb.net/"` → full DB access, all user data.
3. Uses `SecretKey` to forge a Wizard JWT → admin access to all API endpoints.

**Recommendation:**
1. **Immediately rotate** the MongoDB Atlas credentials and JWT SecretKey.
2. Add `OASIS_DNA.json` to `.gitignore` and verify it's excluded from all future commits.
3. Run `git filter-repo` or BFG Repo Cleaner to scrub the credential from git history, then force-push.
4. Use environment variables or a secrets manager (Azure Key Vault, AWS Secrets Manager) for credentials at runtime.

**Confidence: 10/10**

---

## Summary Table

| # | Severity | Project | Issue |
|---|----------|---------|-------|
| 1 | **HIGH** | ONODE `AvatarController.cs:1298` | Any user can self-promote to Wizard (admin) via update endpoint |
| 2 | **HIGH** | ONODE `KarmaController.cs:314,357` | Any authenticated user can modify any user's karma (no ownership check) |
| 3 | **HIGH** | ONODE `KarmaController.cs:96,164` | Karma read endpoints unauthenticated — anyone can read karma history |
| 4 | **HIGH** | ONODE `JwtMiddleware.cs:40` | Full JWT bearer token logged to stdout on every request |
| 5 | **HIGH** | ONODE `Startup.cs:312` | Wildcard CORS + AllowCredentials enables cross-site credential theft |
| 6 | **MEDIUM** | ONODE `Startup.cs:300` | Developer exception page active in production — leaks stack traces |
| 7 | **MEDIUM** | ONODE `AvatarController.cs:783,813` | Missing ownership check on avatar detail by email/username |
| 8 | **MEDIUM** | ONODE `AvatarController.cs:1125` | Avatar search unauthenticated — enables user enumeration |
| 9 | **MEDIUM** | ONODE `JwtMiddleware.cs:48` | No issuer/audience validation on JWT tokens |
| 10 | **HIGH** | WEB6–WEB10 all controllers | All AI/advanced endpoints completely unauthenticated |
| 11 | **MEDIUM** | STAR `Program.cs:321` | JWT pre-load middleware bypasses signature verification; pipeline not halted on 401 |
| 12 | **MEDIUM** | MongoDB `SearchRepository.cs:79` | Regex injection via unescaped user input in avatar search queries |
| 13 | **HIGH** | `OASIS_DNA.json` | MongoDB production credentials + JWT SecretKey in git history |

---

## Priority Order

1. **Finding 13** — rotate credentials immediately; git history exposure of DB password and JWT signing key
2. **Finding 1** — any registered user can become admin right now
3. **Finding 10** — all WEB6–WEB10 AI endpoints public; free LLM abuse and data access
4. **Finding 2** — any registered user can corrupt any user's karma
5. **Finding 4** — bearer tokens exposed in logs
6. **Finding 5** — CORS misconfiguration, cross-site request forgery via cookies
7. **Finding 3** — anonymous karma history read
8. **Finding 11** — STAR WebAPI JWT pre-load auth bypass
9. **Finding 12** — MongoDB regex injection
10. **Finding 7** — PII data exposure by email/username lookup
11. **Finding 6** — stack traces in production
12. **Finding 8** — user enumeration via search
13. **Finding 9** — cross-environment JWT reuse

---

## Audit Coverage

**Completed:**
- [x] `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI` — Controllers, Middleware, Startup, Models, Services
- [x] `STAR ODK/NextGenSoftware.OASIS.STAR.WebAPI` — JwtMiddleware, Program.cs, CORS, AvatarController (proxy to WEB4)
- [x] `WEB6` through `WEB10` — Auth coverage check across all controllers
- [x] `Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS` — Query patterns, SearchRepository
- [x] `OASIS_DNA.json` — Hardcoded secrets / git history check

**Not audited (out of scope for this pass):**
- [ ] `ONODE/NextGenSoftware.OASIS.API.ONODE.OPORTAL`
- [ ] Provider implementations (Holochain, EOSIO, Solana, Telos) — lower risk (no public HTTP surface)
- [ ] WEB6–WEB10 Core manager logic — business logic bugs beyond auth
