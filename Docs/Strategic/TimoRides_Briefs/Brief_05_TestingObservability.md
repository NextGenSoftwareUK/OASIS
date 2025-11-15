# Brief 05 – Testing & Observability

## Objective
Establish minimum viable automated tests and operational visibility for TimoRides backend + Android + driver bot so regressions or outages are caught before demos.

## Current State
- No automated tests run as part of builds.
- No centralized logging/metrics dashboard; reliance on console output.
- Failure scenarios (Mongo down, webhook failure, auth expiry) undocumented.

## Deliverables
1. **Test coverage**
   - Backend: implement Jest or Mocha tests covering `rideService` transitions, payment recording, driver location updates, webhook authentication.
   - Android: extend existing instrumentation (from Brief 03) and add unit tests for repositories/session handling.
   - Driver bot: add .NET unit tests (xUnit/NUnit) for `TimoRidesApiService`, state manager, and Telegram command handlers (using Telegram.Bot test helpers).
   - Bundle test commands into scripts (`npm test`, `./gradlew test`, `dotnet test`) and ensure they run locally.
2. **Smoke test automation**
   - Create a bash or Node script `scripts/smoke.sh` orchestrating: start backend (if not running), seed DB, simulate booking via HTTP, poll for status transitions, and assert success.
   - Provide equivalent Postman or k6 collection for load-lite testing.
3. **Observability stack**
   - Configure winston/pino for backend JSON logs; ship to file or Loki-compatible endpoint.
   - For .NET, use Serilog with sinks to console + rolling file.
   - Define log fields: request_id, booking_id, driver_id, component, severity.
   - If full stack logging not feasible, document manual steps for reading logs with `npm run dev` and `dotnet run`.
4. **Metrics & alerts**
   - Add lightweight metrics exporter (Prometheus or custom `/health/metrics`) reporting counts like pending bookings, failed payments, driver signal latency.
   - Provide dashboard mock (Grafana screenshot or textual description) focusing on Monday demo KPIs.
5. **Runbooks**
   - Draft `Docs/Operational/Runbook_TimoRides.md` describing how to detect/respond to:
     - Mongo outage
     - Telegram bot disconnect
     - Failed PathPulse webhook
     - Payment ledger mismatch
   - Include log locations, commands, escalation steps, and rollback strategy.

## Acceptance Criteria
- `npm test`, `dotnet test`, and `./gradlew test` all succeed locally.
- Smoke script exits 0 after creating + completing a booking.
- At least one dashboard or structured log sample attached to deliverable.
- Runbook enables on-call responder to triage top 4 incidents within 10 minutes.

## References
- Context: `Docs/Strategic/TimoRides_Context.md`
- Files: `ride-scheduler-be/tests/*` (create if absent), Android `app/src/test`, driver bot `ONODE/.../Tests` (create).***

