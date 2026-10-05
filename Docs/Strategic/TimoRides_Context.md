# TimoRides Delivery Context

## Mission Snapshot
- **Goal**: Demonstrate fully functioning ride bookings (request → driver accept → completion + payment record) by Monday using existing monorepo while splitting contributions across multiple agents.
- **Priority order**: 1) Driver channel reliability, 2) Rider experience sufficient for demo, 3) Backend hardening + payments, 4) Supporting infra (tests, release ops).
- **Repos & deployment**: Work inside `/Volumes/Storage/OASIS_CLEAN` monorepo; selectively push to `timo-org/ride-scheduler-be` and `timo-org/Timo-Android-App` via `push-to-timo.sh` (git subtree split). SSH passphrase for `id_ed25519` still pending.

## Architecture Overview
- **Backend (`TimoRides/ride-scheduler-be`)**
  - Node.js/Express, MongoDB (Atlas cluster `oasisweb4/TimoDev`), JWT auth, SendGrid/Twilio/Cloudinary integrations (Twilio currently stubbed).
  - Key additions: driver location update endpoints, `rideService` lifecycle orchestration (accept → start → complete), payment/timeline/cancellation subdocuments, booking expiry job, database seed script.
  - Outstanding: payment processing logic (currently stub), stronger validation/logging, integration test coverage.

- **Android Rider App (`TimoRides/Timo-Android-App`)**
  - Java, Retrofit + OkHttp + AuthInterceptor, secure session via `EncryptedSharedPreferences`.
  - Login flow wired; DTOs for login/booking/driver results ready; configurable `TIMORIDES_BASE_URL`.
  - Outstanding: hook booking UI to API, live ride status updates, driver tracking visuals, QA/instrumentation tests.

- **Driver Channel (Telegram + PathPulse Scout)**
  - `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI` hosts `TelegramBotService` integrating Timo ride commands.
  - Build currently failing due to DI mismatch (`AchievementManager`, `TelegramBotService` constructor) and missing helper services (`TimoRidesApiService`, `RideBookingStateManager`, `GoogleMapsService`). Telegram.Bot package upgrade removed legacy `DownloadFileAsync`.
  - Desired behavior: drivers can accept/cancel/start/complete rides via bot or PathPulse webhooks; location updates relayed to backend.

- **OASIS Platform Integration**
  - Shared identity (avatars, karma) and wallet services used for trust and payment flows.
  - Telegram provider (`NextGenSoftware.OASIS.API.Providers.TelegramOASIS`) handles linking Telegram IDs to avatars.

## Environment & Ops
- `.env` for backend: Mongo Atlas URI, generated JWT secrets, Twilio stubs, SendGrid placeholders, seed defaults.
- Scripts:
  - `npm run dev` for backend server.
  - `npm run seed` to populate admin/driver/car.
  - `npm run expire-pending` to clean stale bookings.
- Git helper: `TimoRides/push-to-timo.sh` automates subtree pushes but requires SSH key passphrase.

## Known Blockers
1. **Driver bot build failure** (missing services & outdated Telegram API calls).
2. **Driver signal plumbing** (no actual callbacks into backend yet).
3. **Rider booking UI** incomplete beyond login.
4. **Payments** stubbed; no wallet ledger updates.
5. **Testing/observability** absent (no automated smoke tests or dashboards).
6. **Release workflow**: CI/CD + credential handling unresolved.

## Coordination Guidelines
- Keep all file paths absolute in tooling.
- Never revert user’s in-progress changes.
- Preference for acting (not asking) on well-understood fixes.
- When editing, stay ASCII-only unless file already uses Unicode.

Use this context with the detailed briefs below to assign tasks to independent agents without loss of situational awareness.***

