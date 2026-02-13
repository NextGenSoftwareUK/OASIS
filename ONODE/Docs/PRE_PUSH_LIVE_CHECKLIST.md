# Pre-push to live – Docker & API impact checklist

Summary of changes that affect the Docker image and existing API behaviour, and how to avoid breaking production.

---

## 1. Docker image (`docker/Dockerfile`)

| Change | Impact |
|--------|--------|
| **Health check** | `curl /swagger/index.html` → `curl /api/health`. ECS/load balancers that rely on the health check now hit `/api/health`. Same endpoint still returns 200 when healthy. **Safe** – prefer explicit health endpoint. |
| **Labels** | Added OCI image labels (version, description). **Safe** – metadata only. |
| **Build** | Added `rm -rf .../obj .../bin` for Libraries to avoid “file already exists”. **Safe** – build fix. |
| **Comments** | OASIS_DNA.json source comment updated. **No behaviour change.** |

**Static assets (minting video):**  
`wwwroot/` is not in `.dockerignore`. The SDK Web project includes `wwwroot` in `dotnet publish` by default, so `wwwroot/minting/witness-the-jpeg-miracle.mp4` will be in the image and served at `/minting/witness-the-jpeg-miracle.mp4` in production when `OasisApiBaseUrl` is set to the live API URL. No Dockerfile change needed.

---

## 2. `.dockerignore`

- **A2A/** → **SERV/**: ignore list now excludes `SERV/` instead of `A2A/`. Build context may include `A2A/` if present. Unlikely to affect ONODE WebAPI build.
- **Removed** `NextGenSoftware.OASIS.API.Native.Integrated.EndPoint/` from ignore. That folder is now in the build context. Only matters if it exists and has heavy or conflicting content.

---

## 3. API behaviour changes (could affect existing clients)

| Area | Change | Risk |
|------|--------|------|
| **SubscriptionMiddleware** | **Commented out** in `Startup.cs`: `// app.UseMiddleware<SubscriptionMiddleware>();` | **High** – subscription/credits are no longer enforced. Existing clients that relied on 402/blocking will no longer get it. If you still want subscriptions in production, **re-enable** this line (and ensure Wizard bypass in the middleware is correct). |
| **JwtMiddleware** | POST `/api/avatar/authenticate` skips JWT validation so login never returns 401 from middleware. | **Low** – fixes login when no token in header. Does not change other routes. |
| **SubscriptionMiddleware (code)** | Wizard avatars bypass subscription/credits; `/api/avatar/authenticate` added to allowed paths. | Only applies when middleware is enabled. |

---

## 4. Additive / safe API changes

- **HealthController**: New `GET /api/health/mongo` – MongoDB connectivity check. Existing `GET /api/health` unchanged.
- **NftController**: New endpoints only:  
  `load-all-nfts-for_avatar-from-all-providers/{avatarId}`  
  `load-all-nfts-for-mint-wallet-address-from-all-providers/{mintWalletAddress}`  
  Both require `[Authorize]`. No changes to existing NFT routes.
- **Startup**:  
  - `OASISHttpStatusResultFilter` and `HttpResponseMessageSchemaFilter` – response shape and Swagger schema only.  
  - Swagger BasePath support (e.g. when behind `/api`) – config-driven, no breaking change if not used.  
  - Postman/links in Swagger UI: `oasisweb4.one` → `api.oasisweb4.com` – link only.
- **Telegram mint flow**: New options (`MintingGifUrl`, `MintingGifUrls`, `OasisApiBaseUrl`), fallback GIF, built-in video path. All optional; no change to existing API routes.

---

## 5. What to do before pushing to live

1. **Subscription / credits**  
   - If production should still enforce subscriptions or credits: **uncomment** `app.UseMiddleware<SubscriptionMiddleware>();` in `Startup.cs` before deploy.  
   - If you intend to leave it off, confirm no clients or SLAs depend on 402 or subscription checks.

2. **Config for production**  
   - Set **TelegramNftMint** in production config (or env/secrets):  
     `OasisApiBaseUrl` = `https://api.oasisweb4.com` (or your live API base URL) so the minting animation uses the built-in video.  
   - Optional: set `MintingGifUrl` or `MintingGifUrls` for custom branding.

3. **Health checks**  
   - If any external system (ECS, ALB, k8s, monitoring) was using the old health URL, update it to `GET /api/health` (or keep using `/api/health` if already in use).

4. **Build and smoke test**  
   - From repo root:  
     `docker build -f docker/Dockerfile .`  
   - Run the container and hit:  
     - `GET /api/health`  
     - `GET /api/health/mongo` (optional)  
     - A known-good auth + NFT or avatar endpoint.  
   - Confirm `GET https://<your-live-host>/minting/witness-the-jpeg-miracle.mp4` returns the video (if you use the built-in minting animation).

---

## 6. One-line summary

**Docker:** Health check and labels only; minting video will be in the image via `wwwroot`.  
**API:** Additive and optional except **SubscriptionMiddleware is disabled** – re-enable in `Startup.cs` if you still want subscription/credits enforcement in production.
