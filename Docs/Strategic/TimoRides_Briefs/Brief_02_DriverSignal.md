# Brief 02 – Driver Signal Integration

## Objective
Ensure driver actions (accept, reject, start trip, pause, complete, cancel) and telemetry (location, speed, bearing) reliably reach `ride-scheduler-be` whether initiated by Telegram bot or PathPulse Scout webhooks, with fallbacks for intermittent connectivity.

## Current State
- Backend exposes driver-centric endpoints (`/api/drivers/:id/location`, booking lifecycle PATCH routes) but nothing is sending real driver data yet.
- Telegram bot prompts drivers, but logic currently stops at simulated driver lists; no outbound calls to backend.
- PathPulse Scout partner offers nav + telemetry, but integration points are only conceptual.
- Offline handling (Hyperdrive/HoloNET) unimplemented; no store-and-forward queue.

## Deliverables
1. **Signal Flow Design**
   - Produce sequence diagrams covering: booking request → driver notifications → accept/decline → live tracking → completion.
   - Document message contracts (JSON schemas) for both Telegram bot actions and PathPulse webhooks.
2. **Implementation**
   - Extend `TimoRidesApiService` with methods `notifyDriverAction`, `updateDriverLocation`, `syncTripState`.
   - In Telegram bot handlers, call these methods whenever drivers press inline buttons; persist minimal audit trail (chat ID, timestamp, payload).
   - Build a lightweight webhook receiver (e.g., `/api/driver-webhooks/pathpulse`) inside `ride-scheduler-be` that authenticates PathPulse callbacks and forwards data into `driverService.updateDriverLocation` and `rideService` transitions.
   - Add retry and dead-letter storage (e.g., Mongo collection `driver_webhook_queue`) for failed callbacks.
3. **Offline Strategy**
   - Define buffer rules: driver-side queue size, expiry, reconnection behavior. Stub implementation that logs queued events and replays when backend reachable.
4. **Monitoring**
   - Introduce structured logs (`driver_signal` category) and metrics counters (accept latency, location drift, failed updates).
   - Create dashboard draft (even if manual) summarizing driver signal health.
5. **Test Plan**
   - Script manual test matrix: Telegram-only, PathPulse-only, mixed, offline-recovery.
   - Provide Curl/Postman collections or automated tests hitting webhook endpoints.

## Acceptance Criteria
- Every driver action taken in Telegram or PathPulse updates the booking document within <3 seconds on a local network.
- Queue/retry mechanism guarantees delivery when backend temporarily unavailable (demonstrate by stopping backend mid-test).
- Documentation bundled as `Docs/Strategic/TimoRides_SignalPlan.md` with diagrams + test steps.

## References
- Context: `Docs/Strategic/TimoRides_Context.md`
- Backend services: `TimoRides/ride-scheduler-be/services/driverService.js`, `rideService.js`.
- Telegram integration: `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Services/TelegramBotService.cs`.***

