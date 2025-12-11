## Driver Signal Integration Plan

### 1. Objective & Scope
- Guarantee every driver action (accept, reject, start, pause, resume, complete, cancel) and telemetry update reaches `ride-scheduler-be` within 3 seconds on a stable network.
- Support two ingestion channels:
  - Telegram bot handlers (inline buttons, chat commands).
  - PathPulse Scout webhooks (nav + telemetry stream).
- Provide resilience through retries, dead-letter capture, and offline buffering.
- **Note:** PathPulse webhook ingestion needs to be demonstrable for Monday's stakeholder call.

### 2. Sequence Flows
#### 2.1 Booking Lifecycle (Telegram happy path)
1. Rider submits booking → backend assigns car/driver → Telegram bot sends driver prompt with inline buttons.
2. Driver taps **Accept**:
   - Bot handler logs action (`driver_signal`), calls `TimoRidesApiService.notifyDriverAction()` with payload `{ action: "accept", bookingId, driverId, source: "telegram", chatId }`.
   - Backend endpoint `/api/driver-actions` invokes `rideService.acceptBooking()`; confirmation returned to bot.
3. Backend emits updated booking status → rider app refreshes.
4. Driver taps **Start Trip** when onsite; bot calls `notifyDriverAction(action: "start")`.
5. During trip, driver shares location (bot prompts every 30s or PathPulse streaming). Bot uses `updateDriverLocation` with latest lat/lng/speed/bearing.
6. Driver taps **Complete** → bot calls `notifyDriverAction(action: "complete", meta: { paymentStatus })`. Backend runs `rideService.completeRide()`, triggers receipts.

```
Telegram Driver Flow
┌────────┐     1.booking request     ┌───────────┐
│ Rider  │ ────────────────────────▶ │ Backend   │
└────────┘                           │ ride-sched│
                      notify prompt  └─────┬─────┘
                                           │
                                           │2.accept button
                                           ▼
                               ┌────────────────────┐
                               │ Telegram Bot       │
                               │ (inline handler)   │
                               └───────┬────────────┘
                                       │ 2a. notifyDriverAction(action=accept)
                                       ▼
                               ┌────────────────────┐
                               │ TimoRidesApiService│
                               └───────┬────────────┘
                                       │ HTTP POST /api/driver-actions
                                       ▼
┌────────┐   completion events   ┌───────────┐
│ Rider  │ ◀──────────────────── │ Backend   │
└────────┘                       │ rideService
                                 └───────────┘
                                  ▲        │
                                  │5.location updates
                      4.start btn │        │6.complete btn
┌────────┐  button presses        │        │
│ Driver │ ───────────────────────┘        │
└────────┘                                  │
     ▲                                      │ receipts/webhooks
     └────── status updates/ack messages ───┘
```

#### 2.2 PathPulse Telemetry Flow
1. PathPulse sends HTTPS POST to `/api/driver-webhooks/pathpulse` with signature header.
2. `driverWebhookController` validates signature & schema, enqueues payload to `driver_webhook_queue`.
3. Worker dequeues, calls:
   - `driverService.updateDriverLocation(driverId, coords)` for `telemetry` events.
   - `rideService` transitions for `action` events (`accept`, `start`, etc.).
4. Success → ack logged; failure → retry policy (3 attempts, exponential backoff) then dead-letter with cause.

```
PathPulse Signal Flow
┌──────────────┐   signed POST    ┌────────────────┐
│ PathPulse    │ ───────────────▶ │ /driver-webhooks│
│ Scout Cloud  │                  │ controller      │
└──────┬───────┘                  └──────┬─────────┘
       │ validate signature             │ enqueue payload
       ▼                                ▼
┌──────────────┐                ┌────────────────────┐
│ Mongo Queue  │◀──────────────▶│ Queue Worker       │
│ driver_web.. │ status updates  │ (agenda/cron)     │
└──────┬───────┘                └──────┬────────────┘
       │                               │
       │dequeue payload                │telemetry → driverService.updateDriverLocation
       │                               │actions  → rideService.transition
       ▼                               ▼
                               ┌───────────────┐
                               │ Backend DB    │
                               │ Driver/Booking│
                               └───────────────┘
                                      │
                                      │success ack/log
                                      ▼
                               ┌──────────────┐
                               │ Monitoring   │
                               │ metrics/logs │
                               └──────────────┘
```

#### 2.3 Mixed Control (Telegram action + PathPulse telemetry)
- Booking accept/start/complete via Telegram; PathPulse continues location streaming.
- `syncTripState` endpoint reconciles state drifts (e.g., PathPulse marks `pause`; backend compares timeline, applies `rideService` updates).

### 3. Message Contracts
#### 3.1 Telegram Bot → Backend (`notifyDriverAction`)
```json
{
  "driverId": "64b2...",
  "bookingId": "65aa...",
  "action": "accept|reject|start|pause|resume|complete|cancel",
  "source": "telegram",
  "chatId": "52342342",
  "meta": {
    "reason": "optional text",
    "paymentStatus": "paid|pending|failed",
    "coordinates": { "lat": -26.1, "lng": 28.0 }
  },
  "traceId": "uuid",
  "timestamp": "ISO8601"
}
```

#### 3.2 Telegram Location Pings (`updateDriverLocation`)
```json
{
  "driverId": "64b2...",
  "source": "telegram",
  "chatId": "52342342",
  "location": {
    "latitude": -26.115,
    "longitude": 28.021,
    "speed": 11.2,
    "bearing": 88.5,
    "accuracy": 4.0
  },
  "timestamp": "ISO8601"
}
```

#### 3.3 PathPulse Webhook Payload
```json
{
  "eventType": "telemetry|action",
  "driverExternalId": "pp-9032",
  "bookingExternalId": "pp-ride-443",
  "timestamp": "ISO8601",
  "signature": "HMAC256",
  "telemetry": {
    "latitude": -26.11,
    "longitude": 28.02,
    "speed": 15.2,
    "bearing": 120,
    "accuracy": 3,
    "battery": 0.76
  },
  "action": {
    "type": "accept|start|pause|resume|complete|cancel",
    "reason": "optional",
    "meta": {
      "odometer": 10023.4,
      "routeId": "scout-55"
    }
  }
}
```

#### 3.4 Backend Responses
- All driver-signal endpoints respond with `{ success: true, booking, driver, traceId }` or `{ success: false, errorCode, message }`.

### 4. Implementation Plan
#### 4.1 Backend (`ride-scheduler-be`)
- **Routes**
  - `POST /api/driver-actions` → new controller calling `rideService` transitions and writing audit logs.
  - `POST /api/driver-webhooks/pathpulse` → validates signature, enqueues payload, immediate 202 response.
  - `POST /api/driver-webhooks/retry/:id` → manual replay of dead-letter entries.
- **Services**
  - Extend `driverService` with `recordActionAudit(driverId, bookingId, action, source, payload)`.
  - Add `driverWebhookQueueService` (Mongo collection `driver_webhook_queue`) with status fields: `pending`, `processing`, `completed`, `dead-letter`.
- **Workers**
  - Lightweight agenda/cron job processing queue every second; exponential retry (0s, 5s, 20s). After 3 failures, move to DLQ.
- **Security**
  - PathPulse requests signed via shared secret header `X-PathPulse-Signature`. Validate HMAC SHA256 over body + timestamp (header `X-PathPulse-Timestamp`).
  - Telegram-origin calls authenticated with bot token when hitting backend (server-side, no user auth).

#### 4.2 Telegram Integration (`TelegramBotService`)
- Inject new `TimoRidesApiService` methods:
  - `Task NotifyDriverActionAsync(DriverActionPayload payload)`
  - `Task UpdateDriverLocationAsync(DriverLocationPayload payload)`
  - `Task SyncTripStateAsync(string bookingId)`
- Inline button handlers call these methods and persist audit log (chatId, action, payload JSON) in existing storage (e.g., LiteDB or Cosmos stub).
- Implement optimistic UI: send immediate acknowledgement message, update later on success/failure.

#### 4.3 PathPulse Connector
- Create `TimoRides/MockTimoRidesApiService.cs` parity to simulate PathPulse events for local testing.
- Provide example webhook cURL + Postman collection.

### 5. Offline & Buffering Strategy
- **Telegram Side**
  - Maintain per-driver queue (max 50 events) in bot memory/disk. Each entry: payload, retries, expiry (10 minutes).
  - When backend unreachable (HTTP 5xx / timeout), mark event `queued` and start background retry (2s → 10s → 30s).
  - On reconnect, replay queued events in chronological order; if queue full, drop oldest with `offline_discard` log.
- **PathPulse Queue**
  - Mongo `driver_webhook_queue` stores raw payload, signature, status, attempts, error.
  - TTL index (48h) cleans processed documents.
  - Admin endpoint exposes queue depth for monitoring; CLI script `npm run replay-driver-webhooks`.

### 6. Monitoring & Observability
- **Structured Logs**
  - Logger name `driver_signal`. Fields: `source`, `action`, `bookingId`, `driverId`, `traceId`, `lat`, `lng`, `status`.
  - Log levels: `info` for success, `warn` for retries, `error` for failures/dead letters.
- **Metrics**
  - Counters: `driver_action_total{source,action}`, `driver_action_failed_total`, `driver_location_updates_total`, `driver_location_stale_total`.
  - Gauges: `driver_signal_latency_ms`, `driver_queue_depth`, `pathpulse_dlq_size`.
  - Export via existing Prometheus middleware or stub JSON endpoint `/metrics/driver-signal`.
- **Dashboard Draft**
  - Panels: acceptance latency, live driver map, queue depth, DLQ alerts, offline buffer size, 5m error rate.

### 7. Test Plan
- **Manual Matrix**
  1. Telegram-only accept/start/complete (backend up) → verify booking timeline updates <3s.
  2. Telegram accept while backend down → action queued, replay once backend restarts.
  3. PathPulse telemetry stream (5 payloads) → verify driver location doc updates each time.
  4. Mixed scenario: Telegram action + concurrent PathPulse location → ensure no conflicts.
  5. Offline buffer overflow (simulate 60 queued events) → confirm oldest drop logged.
  6. Signature tampering test → webhook rejected with 401.
- **Automation**
  - Integration tests using supertest: POST `/api/driver-actions` sequences.
  - Jest/Mocha tests for queue worker (retry logic, DLQ move).
  - Postman collection with scripts verifying responses.
  - cURL snippets documented for quick manual replay.
  - Tooling:
    - `scripts/driverSignalTest.sh` exercises driver action/location endpoints and PathPulse signature flow.
    - `tests/driver-signal.postman_collection.json` bundles the four primary requests (action, location, webhook, metrics) with pre-request HMAC signing.

### 8. Deliverables Checklist
- [ ] Sequence diagrams (Mermaid) embedded after implementation.
- [x] Message contracts documented.
- [x] Implementation roadmap captured.
- [x] Offline buffering rules defined.
- [x] Monitoring + metrics outlined.
- [x] Test matrix drafted.

