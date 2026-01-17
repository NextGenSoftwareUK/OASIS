# Brief 03 – Android Rider Booking Flow

## Objective
Deliver a demo-ready Android rider experience that authenticates, discovers nearby drivers, creates bookings, tracks status, and displays completion + receipt data, all powered by `ride-scheduler-be`.

## Current State
- Networking stack (Retrofit, OkHttp logging, AuthInterceptor) and DTOs are in place.
- `LoginActivity` stores JWT + refresh token via `SessionManager` and routes to `HomepageActivity`.
- Booking UI components exist but are still wired to mock data.
- No background polling, no real-time driver markers, and no error handling for auth expiration.

## Deliverables
1. **API wiring**
   - Use `ApiService` endpoints (`/auth/login`, `/drivers/nearby`, `/bookings`) end-to-end.
   - Implement `BookingRepository` abstraction to encapsulate Retrofit calls and map responses to UI models.
2. **UI/UX upgrades**
   - Update rider home/booking screen to show:
     - Map with rider location + available drivers (using Google Maps SDK).
     - Pickup/dropoff selectors (allow manual entry + map pin).
     - Vehicle selection, fare estimate, ETA summary from backend response.
   - Add progress indicator + status panel for active rides (accepted, en route, completed).
3. **State management**
   - Persist current booking ID locally (SharedPreferences) and resume tracking after app relaunch.
   - Background worker (WorkManager) or coroutine polling every ~10s to fetch booking status until completion/cancel.
   - Handle token expiry via refresh flow; logout user on repeated failure.
4. **Error handling & telemetry**
   - Surface backend errors with user-friendly toasts/dialogs.
   - Log key events (booking created, driver assigned, ride completed) to Logcat and optional analytics hook.
5. **Testing**
   - Add instrumentation test (Espresso) covering login → booking creation with mocked API (use MockWebServer).
   - Provide manual QA checklist with emulator + physical device steps, including network toggle/offline scenario.
6. **Documentation**
   - Update `TimoRides/Timo-Android-App/README.md` with build instructions, `timoridesBaseUrl` override, and demo walkthrough.

## Acceptance Criteria
- Rider can launch app, log in, see nearby drivers (using seed data), create a booking, and watch status transitions until completion.
- UI recovers gracefully from app relaunch and network blips.
- At least one automated test validating booking repository logic.
- Artifacts (screen recordings or screenshots) included for demo deck.

## References
- Context: `Docs/Strategic/TimoRides_Context.md`
- Code paths: `app/src/main/java/com/itechnotion/nextgen/...`
- Backend responses documented in `ride-scheduler-be/README.md`.***

