# OASIS2 Security Audit Report
**Scope:** Full codebase — ONODE, STAR ODK, WEB6–WEB10, MongoDB provider, Core, all blockchain providers
**Date:** 2026-07-04 (updated 2026-07-05)
**Auditor:** Claude Sonnet 4.6 via Claude Code
**Status: ALL FIXABLE ISSUES RESOLVED ✓**

---

## Finding 1: Privilege Escalation — Any User Can Self-Promote to Wizard (Admin) — Severity: HIGH ✓ FIXED

**File:** `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Controllers/AvatarController.cs:1298, 1358, 1414`

**Fix applied:** Added `Avatar.AvatarType.Value == AvatarType.Wizard` guard before allowing `AvatarType` to be updated. Regular users can no longer elevate themselves.

---

## Finding 2: IDOR — Any Authenticated User Can Modify Any User's Karma — Severity: HIGH ✓ FIXED

**File:** `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Controllers/KarmaController.cs:314, 357`

**Fix applied:** Ownership check added to `AddKarmaToAvatar` and `RemoveKarmaFromAvatar` — returns 401 if `avatarId != Avatar.Id && Avatar.AvatarType.Value != AvatarType.Wizard`.

---

## Finding 3: Karma Endpoints Unauthenticated — Severity: HIGH ✓ FIXED

**File:** `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Controllers/KarmaController.cs:96, 164`

**Fix applied:** `[Authorize]` added to both `get-karma-for-avatar` and `get-karma-akashic-records-for-avatar`. Note: karma is intentionally public for accountability — the auth requirement is to prevent anonymous scraping, not to restrict access between users.

---

## Finding 4: JWT Token Logged in Plaintext to Console — Severity: HIGH ✓ FIXED

**File:** `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Middleware/JwtMiddleware.cs:40`

**Fix applied:** `Console.WriteLine` line removed entirely.

---

## Finding 5: Wildcard CORS — Severity: HIGH ✓ FIXED

**Files:** `ONODE/Startup.cs`, `STAR ODK/Program.cs`, `WEB6–WEB10/Program.cs`

**Fix applied:** All 7 APIs replaced `AllowAnyOrigin()` / `SetIsOriginAllowed(origin => true)` with explicit `WithOrigins("https://oasisomniverse.one", "https://app.oasisomniverse.one", "https://oasisweb4.one", "https://oasisweb5.one", "http://localhost:3000", "http://localhost:5173")`.

---

## Finding 6: Developer Exception Page Enabled in All Environments — Severity: MEDIUM ✓ FIXED

**File:** `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Startup.cs:300`

**Fix applied:** Wrapped `app.UseDeveloperExceptionPage()` in `if (env.IsDevelopment())`. STAR/WEB6-10 exception handlers were already properly gated behind `enableGenericExceptionHandling` (defaults to `true` in production).

---

## Finding 7: Missing Ownership Check on Avatar Detail by Email/Username — Severity: MEDIUM ✓ FIXED

**File:** `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Controllers/AvatarController.cs:783, 813`

**Fix applied:** Ownership check added to `GetAvatarDetailByEmail` and `GetAvatarDetailByUsername` — returns 401 if caller is not the owner and not a Wizard.

---

## Finding 8: Avatar Search Endpoint Unauthenticated — Severity: MEDIUM ✓ FIXED

**File:** `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Controllers/AvatarController.cs:1125`

**Fix applied:** `[Authorize]` added to both `search` and `search/{providerType}/{setGlobally}` endpoints.

---

## Finding 9: JWT Validation Missing Issuer and Audience — Severity: MEDIUM ✓ FIXED

**Files:** `ONODE/Middleware/JwtMiddleware.cs`, `ONODE/Services/AvatarService.cs`, `STAR/Middleware/JwtMiddleware.cs`, `OASIS Architecture/.../AvatarManager-Private.cs`

**Fix applied:** `ValidateIssuer = true, ValidIssuer = "OASIS", ValidateAudience = true, ValidAudience = "OASIS"` added to all token validation parameters. `Issuer = "OASIS", Audience = "OASIS"` added to all `SecurityTokenDescriptor` instances (token generation). Note: all existing tokens were invalidated — users must re-login.

---

## Finding 10: WEB6–WEB10 — All AI/Advanced Endpoints Completely Unauthenticated — Severity: HIGH ✓ FIXED

**Files:** All controllers in WEB6, WEB7, WEB8, WEB9, WEB10

**Fix applied:**
- Created `JwtMiddleware.cs` and `AuthorizeAttribute.cs` in WEB6, WEB7, WEB8, WEB9, WEB10
- `[Authorize]` added to `Web6ControllerBase`, `Web7ControllerBase`, `Web8ControllerBase` (inherited by all controllers)
- `[Authorize]` added directly to `SingularityController` (WEB9) and `SourceController` (WEB10)
- `app.UseMiddleware<JwtMiddleware>()` added to all 5 Program.cs files

---

## Finding 11: STAR WebAPI — JWT Pre-load Middleware Bypasses Signature Verification — Severity: MEDIUM ✓ FIXED

**File:** `STAR ODK/NextGenSoftware.OASIS.STAR.WebAPI/Program.cs` and `Middleware/JwtMiddleware.cs`

**Fix applied:**
- Pre-load middleware now only sets `context.Items["AvatarId"]` as a hint (no longer sets `context.Items["Avatar"]` from unverified JWT)
- `JwtMiddleware.AttachAccountToContext` changed to return `bool`; `Invoke` now `return`s immediately on auth failure, halting the pipeline
- Issuer/audience validation added

---

## Finding 12: MongoDB Search — Regex Injection via Unescaped User Input — Severity: MEDIUM ✓ FIXED

**File:** `Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS/Repositories/SearchRepository.cs:79, 87, 123`

**Fix applied:** `Regex.Escape()` applied to `SearchQuery` before embedding in `BsonRegularExpression` patterns on all three fields (FirstName, LastName, Email).

---

## Finding 13: MongoDB Production Credentials + JWT SecretKey in Git History — Severity: HIGH ✓ PARTIALLY FIXED (manual action required)

**File:** `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/OASIS_DNA.json`

**Fix applied (by David):** Credentials rotated. `OASIS_DNA.json` already excluded from git tracking.

**Remaining action (manual):** Run `git filter-repo` or BFG Repo Cleaner to scrub the old credentials from git history, then force-push — eliminates any possibility of recovery from history.

---

## NuGet Vulnerability Fixes (2026-07-05)

All packages that had a published safe version were updated. Every API builds clean with zero vulnerable direct/transitive packages under our control.

| Package | Old Version | New Version | CVE | Severity |
|---------|-------------|-------------|-----|----------|
| AutoMapper | 11.0.1 | 16.2.0 | GHSA-rvv3-g6hj-g44x | HIGH |
| MailKit | 2.11.1 | 4.17.0 | GHSA-9j88-vvj5-vhgr | MODERATE |
| MimeKit | (transitive) | 4.17.0 | GHSA-g7hc-96xr-gvvx | MODERATE |
| SharpCompress | 0.30.1 / 0.39.0 | 0.49.1 | GHSA-6c8g-7p36-r338 | MODERATE |
| Snappier | 1.0.0 | 1.3.1 | GHSA-pggp-6c3x-2xmx | HIGH |
| EF Core Sqlite | 8.0.2 | 10.0.9 | (pulls safer transitive deps) | — |
| PeterO.Cbor | 3.1.0 | 4.5.5 | GHSA-cxw4-9qv9-vx5h | HIGH |
| SharpZipLib | 1.2.0 | 1.4.2 | GHSA-m22m-h4rf-pwq3 | HIGH |
| System.IdentityModel.Tokens.Jwt | 7.0.3 | 8.19.1 | GHSA-59j7-ghrg-fj52 | MODERATE |
| Nethereum (7 providers) | 4.20–4.21.4 | 4.25.0 | (removes conflicting .Logging.Abstractions < 9.0.0 constraint) | — |
| Microsoft.Extensions.Caching.Memory | 5.0.0 / 8.0.0 | (via EF Core 10.0.9) | GHSA-qj66-m88j-hmgj | HIGH |

**One genuinely unfixable:**

| Package | Version | CVE | Reason |
|---------|---------|-----|--------|
| SQLitePCLRaw.lib.e_sqlite3 | 2.1.11 | GHSA-2m69-gcr7-jv3q | No fixed version published by maintainers as of 2026-07-05 |

**Remaining Dependabot alerts (~499):** All trace back to deep transitive dependencies inside third-party blockchain SDK packages (Solana, EOSIO, Cardano, Ethereum/Nethereum, etc.). These are not under OASIS control and will resolve when the upstream SDK maintainers update their deps.

---

## Summary Table

| # | Severity | Project | Issue | Status |
|---|----------|---------|-------|--------|
| 1 | **HIGH** | ONODE `AvatarController.cs:1298` | Any user can self-promote to Wizard via update endpoint | ✓ FIXED |
| 2 | **HIGH** | ONODE `KarmaController.cs:314,357` | Any authenticated user can modify any user's karma | ✓ FIXED |
| 3 | **HIGH** | ONODE `KarmaController.cs:96,164` | Karma read endpoints unauthenticated | ✓ FIXED |
| 4 | **HIGH** | ONODE `JwtMiddleware.cs:40` | Full JWT bearer token logged to stdout | ✓ FIXED |
| 5 | **HIGH** | ONODE/STAR/WEB6-10 | Wildcard CORS on all 7 APIs | ✓ FIXED |
| 6 | **MEDIUM** | ONODE `Startup.cs:300` | Developer exception page in production | ✓ FIXED |
| 7 | **MEDIUM** | ONODE `AvatarController.cs:783,813` | Missing ownership check on avatar detail by email/username | ✓ FIXED |
| 8 | **MEDIUM** | ONODE `AvatarController.cs:1125` | Avatar search unauthenticated | ✓ FIXED |
| 9 | **MEDIUM** | ONODE/STAR `JwtMiddleware.cs` | No issuer/audience validation on JWT tokens | ✓ FIXED |
| 10 | **HIGH** | WEB6–WEB10 all controllers | All AI/advanced endpoints completely unauthenticated | ✓ FIXED |
| 11 | **MEDIUM** | STAR `Program.cs:321` | JWT pre-load middleware bypasses signature verification | ✓ FIXED |
| 12 | **MEDIUM** | MongoDB `SearchRepository.cs:79` | Regex injection via unescaped user input | ✓ FIXED |
| 13 | **HIGH** | `OASIS_DNA.json` | MongoDB credentials + JWT SecretKey in git history | ✓ CREDENTIALS ROTATED (git history scrub pending) |
| N/A | **HIGH** | Multiple NuGet packages | AutoMapper, MailKit, SharpCompress, Snappier, PeterO.Cbor, SharpZipLib, SQLitePCLRaw, JWT | ✓ ALL UPDATED (except SQLitePCLRaw — no fix available) |

---

## Audit Coverage

**Completed:**
- [x] `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI` — Controllers, Middleware, Startup, Models, Services
- [x] `STAR ODK/NextGenSoftware.OASIS.STAR.WebAPI` — JwtMiddleware, Program.cs, CORS, auth pipeline
- [x] `WEB6` through `WEB10` — Auth coverage, CORS, JWT middleware, exception handling
- [x] `Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS` — Query patterns, SearchRepository
- [x] `Providers/Storage/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS` — NuGet deps
- [x] `Providers/Network/NextGenSoftware.OASIS.API.Providers.IPFSOASIS` — NuGet deps
- [x] `Providers/Blockchain/` (Arbitrum, Avalanche, Base, BNB, Fantom, Optimism, Web3Core) — Nethereum version conflict
- [x] `OASIS Architecture/NextGenSoftware.OASIS.API.Core` — NuGet deps, JWT token generation
- [x] `OASIS_DNA.json` — Hardcoded secrets / git history check
- [x] Full NuGet vulnerability scan across OASISBootLoader transitive dep tree

**Not audited (out of scope for this pass):**
- [ ] `ONODE/NextGenSoftware.OASIS.API.ONODE.OPORTAL`
- [ ] Provider implementations (Holochain, EOSIO, Solana, Telos, Cardano) — no public HTTP surface
- [ ] WEB6–WEB10 Core manager logic — business logic bugs beyond auth
