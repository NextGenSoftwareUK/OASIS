# Brief 01 – Driver Bot Build Fix & QA

## Objective
Restore a clean build/run for `NextGenSoftware.OASIS.API.ONODE.WebAPI` with the TimoRides-aware Telegram bot, then prove `/bookride`, `/track`, `/cancel`, `/myrides` flows work end-to-end against the running `ride-scheduler-be` backend.

## Current State
- `TelegramBotService.cs` now includes Timo ride handlers but still relies on legacy APIs (`DownloadFileAsync`, `SendTextMessageAsync`, etc.).
- DI registration in `Startup.cs` references `TimoRidesApiService`, `RideBookingStateManager`, and `GoogleMapsService`, but these classes are missing from the repo.
- `AchievementManager` constructor currently expects `(TelegramOASIS, AvatarManager, IOASISStorageProvider?, string?)`; DI instantiation passes fewer args.
- Build log shows three blocking errors: constructor mismatches (lines 291 & 302 in `Startup.cs`) and missing `DownloadFileAsync` (line 1342 in `TelegramBotService.cs`).

## Deliverables
1. **Code fixes**
   - Implement or restore `Services/TimoRidesApiService.cs`, `Services/RideBookingStateManager.cs`, `Services/GoogleMapsService.cs`. Each should encapsulate:
     - HTTP client to `ride-scheduler-be` with base URL from configuration.
     - In-memory ride state cache (per chat) for bookings initiated via Telegram.
     - Google Maps geocoding/distance helper with API key from config (support mocking if key absent).
   - Update `TelegramBotService` constructor to accept the new services and use them in `/bookride` workflow (call backend create booking, fetch drivers, etc.).
   - Replace deprecated Telegram.Bot calls with supported equivalents (if staying on v20, ensure package is actually restored; otherwise upgrade and adjust method names).
   - Fix `AchievementManager` instantiation by passing optional parameters or overloading constructor.
2. **Build verification**
   - `dotnet restore` + `dotnet build` must succeed from `/Volumes/Storage/OASIS_CLEAN/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI`.
3. **Runtime smoke test**
   - Run backend (`npm run dev` in `TimoRides/ride-scheduler-be`) and WebAPI simultaneously.
   - Using Telegram test bot token (config fallback), execute `/bookride`, complete prompts until booking hits backend, then `/track`, `/cancel`, `/myrides`.
   - Capture logs/screenshots plus sample booking documents showing Telegram-sourced rides.
4. **Documentation**
   - Create `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/DRIVER_BOT_TEST_PLAN.md` with setup steps, env vars, commands, and expected outputs.

## Acceptance Criteria
- Build passes with zero errors (warnings acceptable).
- Telegram interaction creates bookings visible via backend API or Mongo collection.
- Documentation allows another engineer to reproduce in under 30 minutes.
- All new services covered with unit tests or at least integration harness (e.g., mock backend).

## References
- Context: `Docs/Strategic/TimoRides_Context.md`
- Relevant files:
  - `ONODE/.../Services/TelegramBotService.cs`
  - `ONODE/.../Startup.cs`
  - `TimoRides/ride-scheduler-be` API docs (`README.md` / Swagger at `/api-docs`).***

