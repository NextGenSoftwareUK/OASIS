## Driver Signal Testing Guide

Use this checklist whenever you need to validate the driver signal pipeline (Telegram + PathPulse → backend → metrics) in a fresh session.

### 1. Prerequisites
- `Database_Url`, `SERVICE_DRIVER_ACTION_TOKEN`, and `PATHPULSE_WEBHOOK_SECRET` configured in `TimoRides/ride-scheduler-be/config/.env`.
- A driver/booking pair in Mongo you can safely reuse (the seed script creates one).
- Node.js environment ready (same as existing backend setup).
- `jq`, `curl`, and `openssl` installed if you plan to use the shell helper.
- Postman desktop app (optional, for collection).

### 2. Start Services
1. **Backend API**
   ```bash
   cd /Volumes/Storage/OASIS_CLEAN/TimoRides/ride-scheduler-be
   npm install   # first run only
   npm run start
   ```
2. **Driver Signal Worker**
   ```bash
   # New terminal
   cd /Volumes/Storage/OASIS_CLEAN/TimoRides/ride-scheduler-be
   npm run driver-signal-worker
   ```
   The worker drains `driver_webhook_queue`; keep it running while testing PathPulse payloads.

### 3. Shell Helper (preferred quick test)
```
cd /Volumes/Storage/OASIS_CLEAN/TimoRides/ride-scheduler-be
chmod +x scripts/driverSignalTest.sh

BASE_URL=http://localhost:4205/api \
SERVICE_TOKEN=<service token> \
DRIVER_ID=<mongo driver _id> \
BOOKING_ID=<mongo booking _id> \
PATHPULSE_SECRET=<pathpulse secret> \
./scripts/driverSignalTest.sh action accept
```
- `action start`, `action complete`, `location -26.11 28.02`, `pathpulse telemetry`, etc.
- `... pathpulse action` enqueues an action event; confirm worker logs processing.
- `... metrics` fetches the live `/api/metrics/driver-signal` snapshot.

### 4. Postman Collection
1. Import `TimoRides/tests/driver-signal.postman_collection.json`.
2. Configure collection variables:
   - `baseUrl`: `http://localhost:4205/api`
   - `serviceToken`, `driverId`, `bookingId`, `pathpulseSecret`.
3. Run the four requests in order: Driver Action → Driver Location → PathPulse Webhook → Driver Signal Metrics.
   - The PathPulse request auto-generates the `x-pathpulse-signature`.

### 5. Expected Outcomes
- **Driver Actions** return `success: true`, updated booking payload, and the worker logs the action.
- **Driver Location** responses include the driver/car snapshot with newly persisted coordinates.
- **PathPulse Webhook** immediately returns `202`; within a few seconds the worker logs completion and metrics increment.
- **/metrics/driver-signal** shows updated counters (actions, locations, queue processed/failed, last latency).

### 6. Troubleshooting
- If the webhook request fails with `401`, confirm `PATHPULSE_WEBHOOK_SECRET` matches, and the signature timestamps are near real-time.
- If queue items stall, check the worker terminal for errors; rerun with `DEBUG=driver-signal-worker npm run driver-signal-worker` if necessary.
- Use Mongo to inspect `driver_webhook_queue` (look for `dead-letter` status) when diagnosing retries.

Keep this guide alongside the `TimoRides_SignalPlan.md` so all agents have a consistent test flow.***

