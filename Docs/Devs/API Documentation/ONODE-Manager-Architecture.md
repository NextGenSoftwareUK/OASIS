# ONODE Manager — Architecture, Design & Operations Manual

> Complete reference for the OASIS Web4–Web10 node management system.  
> Covers architecture, components, APIs, deployment, and day-to-day operations.

---

## Table of Contents

1. [System Overview](#system-overview)
2. [Components](#components)
3. [Architecture Diagrams](#architecture-diagrams)
4. [ONODEService — Supervisor Daemon](#onodeservice--supervisor-daemon)
5. [ONODE Manager — Desktop Tray App](#onode-manager--desktop-tray-app)
6. [STAR CLI — onode Commands](#star-cli--onode-commands)
7. [OPORTAL-JS — Web Control Panel](#oportal-js--web-control-panel)
8. [Web4 API — Command Bridge](#web4-api--command-bridge)
9. [Real-Time Push (WebSocket)](#real-time-push-websocket)
10. [Metrics & History (SQLite)](#metrics--history-sqlite)
11. [Provider Management](#provider-management)
12. [Rate Limiting & Audit Log](#rate-limiting--audit-log)
13. [Auto-Update (Velopack)](#auto-update-velopack)
14. [Icon Generation (SkiaSharp)](#icon-generation-skiasharp)
15. [REST API Reference](#rest-api-reference)
16. [Data Transfer Objects](#data-transfer-objects)
17. [Installation & First Run](#installation--first-run)
18. [Releasing a New Version](#releasing-a-new-version)
19. [Troubleshooting](#troubleshooting)

---

## System Overview

The ONODE system lets users run OASIS Web4–Web10 node services locally and control them either:

- **Locally** — via the ONODE Manager desktop tray app or STAR CLI (direct loopback connection to ONODEService)
- **Remotely** — via OPORTAL (hosted on Vercel) using the CommandHolon bridge over the Web4 API

```
┌─────────────────────────────────────────────────────────┐
│                    User's Machine                        │
│                                                          │
│  ┌──────────────────┐    ┌───────────────────────────┐  │
│  │  ONODE Manager   │    │       ONODEService         │  │
│  │  (Avalonia tray) │◄──►│   (.NET 10 Worker Service) │  │
│  └──────────────────┘    │   127.0.0.1:8765           │  │
│                          │                             │  │
│  ┌──────────────────┐    │  ┌─────────┐ ┌──────────┐ │  │
│  │   STAR CLI       │◄──►│  │  Web4   │ │  Web5    │ │  │
│  └──────────────────┘    │  │  Web6   │ │  Web7    │ │  │
│                          │  │  Web8   │ │  Web9    │ │  │
│                          │  │  Web10  │ └──────────┘ │  │
│                          └───────────────────────────┘  │
│                                    │ pushes state every 5s
└────────────────────────────────────┼────────────────────┘
                                     ▼
                          ┌─────────────────────┐
                          │   Web4 OASIS API     │
                          │  (NextGen cloud)     │
                          │                      │
                          │  AvatarNodeStateHolon│
                          │  CommandHolon queue  │
                          │  WebSocket /ws/onode │
                          └─────────────────────┘
                                     ▲
                          ┌─────────────────────┐
                          │     OPORTAL-JS       │
                          │  (Vercel, Web panel) │
                          └─────────────────────┘
```

---

## Components

| Component | Technology | Location |
|---|---|---|
| **ONODEService** | .NET 10 Worker Service | `ONODE/NextGenSoftware.OASIS.ONODE.Service/` |
| **ONODE Manager** | Avalonia UI 11.3, .NET 10 | `ONODE/NextGenSoftware.OASIS.ONODE.Manager/` |
| **ONODE Client** | .NET 10 class library | `ONODE/NextGenSoftware.OASIS.ONODE.Client/` |
| **Web4 API (ONODE endpoints)** | ASP.NET Core | `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/` |
| **OPORTAL-JS** | Vanilla JS + CSS | `OPORTAL-JS/` (separate repo) |
| **STAR CLI** | .NET 10 console | `STAR ODK/NextGenSoftware.OASIS.STAR.CLI/` |

---

## Architecture Diagrams

### Local Control Flow

```
ONODE Manager / STAR CLI
        │
        │  HTTP REST  (127.0.0.1:8765)
        ▼
  ONODEService  ──manages──►  Web4 / Web5 / … / Web10 processes
        │
        │  polls OASISDNA.json for provider config
        │  records metrics → ~/.oasis/onode-metrics.db (every 30s)
        └──────────────────────────────────────────────────────────
```

### Remote Control Flow (CommandHolon pattern)

```
OPORTAL (Vercel)
    │
    │  POST /api/v1/onode/command/{nodeId}   (Web4 API)
    │  → stores CommandHolon in memory queue
    ▼
Web4 OASIS API
    ▲
    │  GET /api/v1/onode/command/next/{nodeId}   every 3 s
    │
ONODEService  (on user's machine)
    │  executes command
    │
    │  POST /api/v1/onode/state/{nodeId}   (push result + state)
    ▼
Web4 OASIS API  →  WebSocket broadcast  →  OPORTAL
```

**Worst-case round-trip: ~6 seconds** (3s poll + processing + push)

### State Push Flow

```
ONODEService
    │  every 5 s
    │  POST /api/v1/onode/state/{nodeId}   (AvatarNodeStateHolon)
    ▼
Web4 OASIS API
    ├──► stores in _nodeStates dict
    └──► ONODEWebSocketHub.BroadcastAsync(nodeId, state)
              │
              └──► all open WebSocket clients for that nodeId
```

---

## ONODEService — Supervisor Daemon

**Loopback REST API:** `http://127.0.0.1:8765`

### Process Management

Each Web service (Web4–Web10) is managed as a child process. The supervisor:

- Starts/stops/restarts on demand
- Monitors health via stdout heartbeat and exit codes
- Reports status: `Stopped | Starting | Running | Stopping | Restarting | Error`

### Supervisor API Endpoints

| Method | Path | Description |
|---|---|---|
| `GET` | `/supervisor/status` | All service statuses + aggregate metrics |
| `POST` | `/supervisor/start/{service}` | Start a service |
| `POST` | `/supervisor/stop/{service}` | Stop a service |
| `POST` | `/supervisor/restart/{service}` | Restart a service |
| `GET` | `/supervisor/metrics/{service}` | Per-service metrics |
| `GET` | `/supervisor/metrics/history` | 24h history from SQLite (see §10) |
| `GET` | `/supervisor/providers` | List all providers from OASISDNA.json |
| `PUT` | `/supervisor/providers/{type}/enable` | Enable a provider |
| `PUT` | `/supervisor/providers/{type}/disable` | Disable a provider |
| `PUT` | `/supervisor/providers/{type}/priority/{n}` | Set provider priority |

### State Push to Web4

Every 5 seconds ONODEService calls `POST /api/v1/onode/state/{nodeId}` with an `AvatarNodeStateHolon` containing:

```json
{
  "nodeId": "avatar-guid",
  "services": [
    {
      "name": "Web4",
      "status": "Running",
      "peers": 12,
      "bytesIn": 1048576,
      "bytesOut": 524288,
      "requestsPerSec": 4.2,
      "latencyMs": 18,
      "providers": [
        { "type": "Holochain", "enabled": true, "priority": 1 }
      ]
    }
  ],
  "lastSeen": "2026-07-17T10:30:00Z"
}
```

### Command Polling

Every 3 seconds ONODEService calls `GET /api/v1/onode/command/next/{nodeId}`. If a `CommandHolon` is waiting it executes it and calls `PUT /api/v1/onode/command/{commandId}/done` (or `/error`).

---

## ONODE Manager — Desktop Tray App

Cross-platform desktop tray app built with Avalonia UI 11.3 and .NET 10.

### Tabs

| Tab | Content |
|---|---|
| **Overview** | Per-service status cards with Start/Stop/Restart buttons |
| **Metrics** | Real-time LiveCharts2 line charts — Peers, Bandwidth (In/Out), Requests/s |
| **Network** | Per-service metrics table (Peers, Bytes In/Out, Req/s, Latency) |
| **Config** | OASISDNA.json path, service toggles, port config |
| **Audit** | Timestamped command audit log (loaded from Web4 API) |
| **Providers** | Enable/disable toggles and priority arrows per provider |

### Real-Time Updates

- Polls ONODEService every **3 seconds** via `SupervisorClient`
- Chart history: 60-point rolling buffer (~3 minutes of live data)
- 24h history loadable on demand via "Load 24h History" button (reads SQLite via supervisor API)

### Tray Icon States

| State | Icon | Animation |
|---|---|---|
| All running | Blue circle | Static |
| All stopped | Grey circle | Static |
| Any error | Red circle | Static |
| Starting / Stopping / Restarting | Yellow circle | 500ms blink (yellow ↔ grey) |

Icons are generated at first launch using SkiaSharp into `~/.oasis/icons/`. See [§14](#icon-generation-skiasharp).

### Toast Notifications

Borderless `ToastWindow` overlay appears bottom-right, auto-closes after 4 seconds. Triggered by `NotificationRequested` event on the ViewModel.

### Tray Menu

- Show / Hide window
- Start All / Stop All
- Check for Updates… (Velopack)
- Quit

---

## STAR CLI — onode Commands

```
onode status                        # show all service statuses
onode start <service>               # start Web4, Web5, … Web10, or "all"
onode stop <service>
onode restart <service>
onode providers list                # list providers from OASISDNA.json
onode providers enable <type>       # e.g. Holochain, Ethereum, IPFS
onode providers disable <type>
onode providers priority <type> <n>
```

All commands route through `SupervisorClient` → `http://127.0.0.1:8765`.

---

## OPORTAL-JS — Web Control Panel

Hosted on Vercel. Provides a browser UI for ONODE control and monitoring.

### Dual Mode

| Mode | When | Control path | State updates |
|---|---|---|---|
| **Local** | OPORTAL detects ONODEService on `127.0.0.1:8765` | Direct REST to supervisor | WebSocket from Web4 API (replaces polling) |
| **Remote** | No local service detected | CommandHolon via Web4 API | 5s polling of `AvatarNodeStateHolon` |

### ONODE Modal Tabs

- **Overview** — service status cards, Start/Stop/Restart buttons
- **Logs** — live log tail (auto-refresh)
- **Config** — OASISDNA config fields
- **Providers** — enable/disable + priority (dual-mode aware)

### WebSocket Client (Local Mode)

```javascript
// Opens ws://localhost:5000/ws/onode/{nodeId}
openWebSocket(nodeId)   // called after loadAllLocal() setup
closeWebSocket()        // called on modal close

// onopen: clears polling interval (WS replaces polling)
// onmessage: onWsStateUpdate(state) updates UI instantly
// onclose: restarts 5s polling interval as fallback
```

### ONET Modal — Connected Nodes

The ONET modal "Nodes" tab includes an **Active ONODEs (last 5 min)** section populated by `loadActiveONODEs()`, which calls `GET /api/v1/onode/active-nodes`. Each card shows: node ID, avatar name, age since last seen, peer count, and service statuses.

---

## Web4 API — Command Bridge

Controller: `ONODEController` at `/api/v1/onode/`

### Endpoints

| Method | Path | Description |
|---|---|---|
| `POST` | `/command/{nodeId}` | Queue a CommandHolon (rate-limited, audited) |
| `GET` | `/command/next/{nodeId}` | ONODEService polls this |
| `PUT` | `/command/{commandId}/done` | Mark command complete |
| `PUT` | `/command/{commandId}/error` | Mark command failed |
| `POST` | `/state/{nodeId}` | ONODEService pushes AvatarNodeStateHolon |
| `GET` | `/state/{nodeId}` | OPORTAL reads current state (remote mode) |
| `GET` | `/active-nodes` | All nodes with LastSeen < 5 minutes |
| `GET` | `/audit` | Audit log (`?nodeId=&limit=200`) |
| `PUT` | `/providers/{type}/enable` | Proxied to ONODEService |
| `PUT` | `/providers/{type}/disable` | Proxied to ONODEService |
| `PUT` | `/providers/{type}/priority/{n}` | Proxied to ONODEService |

---

## Real-Time Push (WebSocket)

In local mode, OPORTAL connects to `ws://localhost:5000/ws/onode/{nodeId}` (Web4 API).

### Server Side (`Startup.cs` inline middleware)

```
GET /ws/onode/{nodeId}   → WebSocket upgrade
                         → ONODEWebSocketHub.Register(nodeId, ws)
                         → receive loop (keeps connection alive)
```

### Hub (`ONODEWebSocketHub.cs`)

- Static `ConcurrentDictionary<string, ConcurrentBag<WebSocket>> _connections`
- `BroadcastAsync(nodeId, payload)` — serialises to JSON, sends to all open sockets, rebuilds bag removing dead connections
- Called automatically by `PushNodeState` in the controller when ONODEService posts state

### Why WebSocket is local-only

OPORTAL is hosted on Vercel (HTTPS). Browser security blocks WebSocket connections from `https://` pages to `ws://localhost`. In remote mode, state is updated via 5-second polling of `GET /state/{nodeId}` instead.

---

## Metrics & History (SQLite)

**Service:** `MetricsHistoryService` (ONODEService)  
**Database:** `~/.oasis/onode-metrics.db`

### Schema

```sql
CREATE TABLE metrics (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    ts          TEXT NOT NULL,           -- ISO 8601
    node_id     TEXT NOT NULL,
    service_id  TEXT NOT NULL,           -- "Web4", "_aggregate", etc.
    peers       INTEGER,
    bytes_in    INTEGER,
    bytes_out   INTEGER,
    req_per_sec REAL,
    latency_ms  REAL
);
```

- Records every **30 seconds**
- `service_id = "_aggregate"` row holds totals across all services
- Rows older than **24 hours** pruned on startup and every 6 hours

### Reading History

```
GET http://127.0.0.1:8765/supervisor/metrics/history?hours=24&serviceId=_aggregate
```

Returns `MetricsHistoryPointDto[]`. The ONODE Manager "Load 24h History" button calls this and loads the data into the LiveCharts2 series.

---

## Provider Management

Providers are OASIS storage/blockchain backends (Holochain, Ethereum, IPFS, Solana, etc.) configured in `OASISDNA.json`.

### Flow

```
OPORTAL / Manager / CLI
    │  toggle or priority change
    ▼
ONODEService supervisor API   (local)
  OR
Web4 API provider proxy       (remote, proxied to ONODEService)
    │
    ▼
ProviderService.UpdateOASISDNA()
    │  writes OASISDNA.json
    ▼
Running services pick up changes on next config reload
```

### Provider DTO

```json
{
  "type": "Holochain",
  "enabled": true,
  "priority": 1
}
```

---

## Rate Limiting & Audit Log

### Rate Limiting

- Applied to `POST /api/v1/onode/command/{nodeId}` in Web4 API
- **Max 10 commands per minute per nodeId**
- Implemented via `ConcurrentDictionary<string, (int count, DateTime window)>` — no external dependency
- Returns `HTTP 429 Too Many Requests` when exceeded

### Audit Log

- `ConcurrentQueue<AuditLogEntryDto>` capped at **1000 entries**
- Written on: command posted, command completed, command errored
- Read via `GET /api/v1/onode/audit?nodeId=<id>&limit=200`
- Displayed in ONODE Manager **Audit** tab

### AuditLogEntryDto

```json
{
  "timestamp": "2026-07-17T10:30:00Z",
  "nodeId": "avatar-guid",
  "commandType": "Start",
  "service": "Web4",
  "outcome": "Done",
  "message": ""
}
```

---

## Auto-Update (Velopack)

**Library:** Velopack 0.0.835  
**Update feed URL:** `https://releases.oasisomniverse.one/onode-manager/`

### Integration

`Program.cs` (ONODE Manager):
```csharp
VelopackApp.Build().Run();   // must be the first line
```

`TrayIconManager.cs` — "Check for Updates…" tray menu item:
```csharp
var mgr = new UpdateManager("https://releases.oasisomniverse.one/onode-manager/");
var update = await mgr.CheckForUpdatesAsync();
if (update != null) await mgr.DownloadUpdatesAsync(update);
mgr.ApplyUpdatesAndRestart(update);
```

### Hosting a Release Feed

Upload Velopack output files (`RELEASES`, `*.nupkg`, setup exe) to the feed URL. The GitHub Actions workflow produces these files — see [§18](#releasing-a-new-version).

---

## Icon Generation (SkiaSharp)

Icons are generated at first launch (no pre-built assets needed in the repo).

**Output directory:** `~/.oasis/icons/`  
**Files:** `onode-blue.png`, `onode-grey.png`, `onode-yellow.png`, `onode-red.png`

### Visual Style

Each icon is a 256×256 PNG:
- Dark navy `#0A0E1A` background
- Subtle outer glow in the icon colour
- Coloured ring stroke
- White "O" letter centred

| Key | Colour | Meaning |
|---|---|---|
| `blue` | `#00BFFF` | All services running |
| `grey` | `#808080` | All services stopped |
| `yellow` | `#FFA500` | Transitioning (blinks) |
| `red` | `#FF4444` | One or more services in error |

**Entry point:** `IconGenerator.EnsureIconsExist()` — called once from `Program.cs` before Avalonia initialises.

---

## REST API Reference

### SupervisorClient (`ONODE.Client`)

```csharp
// Status
Task<NodeStatusDto> GetStatusAsync(CancellationToken ct)

// Control
Task StartAsync(string service, CancellationToken ct)
Task StopAsync(string service, CancellationToken ct)
Task RestartAsync(string service, CancellationToken ct)

// Metrics
Task<ServiceMetricsDto[]> GetServiceMetricsAsync(CancellationToken ct)
Task<MetricsHistoryPointDto[]> GetMetricsHistoryAsync(int hours, string serviceId, CancellationToken ct)

// Providers
Task<ProviderDto[]> GetProvidersAsync(CancellationToken ct)
Task EnableProviderAsync(string type, CancellationToken ct)
Task DisableProviderAsync(string type, CancellationToken ct)
Task SetProviderPriorityAsync(string type, int priority, CancellationToken ct)
```

---

## Data Transfer Objects

### NodeStatusDto

```json
{
  "services": [
    {
      "name": "Web4",
      "status": "Running",
      "pid": 12345,
      "uptime": "01:23:45"
    }
  ]
}
```

### ServiceMetricsDto

```json
{
  "name": "Web4",
  "peers": 12,
  "bytesIn": 1048576,
  "bytesOut": 524288,
  "requestsPerSec": 4.2,
  "latencyMs": 18.0
}
```

### MetricsHistoryPointDto

```json
{
  "timestamp": "2026-07-17T09:00:00Z",
  "peers": 10,
  "bytesIn": 983040,
  "bytesOut": 491520,
  "requestsPerSec": 3.8,
  "latencyMs": 20.1
}
```

### AvatarNodeStateHolon

```json
{
  "nodeId": "avatar-guid",
  "lastSeen": "2026-07-17T10:30:00Z",
  "services": [ /* ServiceMetricsDto with providers[] */ ]
}
```

### CommandHolon

```json
{
  "commandId": "guid",
  "nodeId": "avatar-guid",
  "type": "Start",
  "service": "Web4",
  "payload": {},
  "createdAt": "2026-07-17T10:30:00Z"
}
```

---

## Installation & First Run

### ONODEService

```bash
# Install as a system service
onode service install

# Start
onode service start

# Verify
onode status
```

The service runs as a background daemon. Logs go to `~/.oasis/logs/onode-service.log`.

### ONODE Manager

**Windows:** Run `ONODEManagerSetup.exe` (Velopack installer)  
**macOS:** Open `ONODEManager.dmg`, drag to Applications  
**Linux:** `chmod +x ONODEManager.AppImage && ./ONODEManager.AppImage`

On first launch:
1. `VelopackApp.Build().Run()` initialises the update framework
2. `IconGenerator.EnsureIconsExist()` generates tray icons into `~/.oasis/icons/`
3. Avalonia UI starts, tray icon appears
4. Manager connects to ONODEService on `127.0.0.1:8765`

### STAR CLI

Included with the OASIS STAR ODK. No additional installation required.

```bash
star onode status
star onode start all
```

### OPORTAL

No installation — hosted at the OPORTAL Vercel URL. Automatically detects whether ONODEService is running locally.

---

## Releasing a New Version

The GitHub Actions workflow at [`.github/workflows/release-onode-manager.yml`](../../.github/workflows/release-onode-manager.yml) triggers on a version tag.

### Steps

```bash
# 1. Make sure master is clean and pushed
git status

# 2. Create and push a version tag
git tag onode-manager-v1.0.0
git push origin onode-manager-v1.0.0
```

### What the workflow does

1. **Three parallel jobs** — `build-windows`, `build-macos`, `build-linux`
2. Each job: checks out → installs .NET 10 → installs Velopack CLI (`vpk`) → `dotnet publish --self-contained` → `vpk pack`
3. **`release` job** (after all three complete): downloads all artifacts → creates a GitHub Release with installers attached

### Tag format

`onode-manager-v{MAJOR}.{MINOR}.{PATCH}` — e.g. `onode-manager-v1.0.0`

Only tags matching `onode-manager-v*` trigger the workflow. Regular pushes to `master` do **not** trigger it.

### Hosting the Velopack update feed

After the release is created, upload the Velopack `RELEASES` file and `*.nupkg` from the Windows build artifact to:

```
https://releases.oasisomniverse.one/onode-manager/
```

This is the URL the ONODE Manager checks when the user clicks "Check for Updates…".

---

## Troubleshooting

### Manager can't connect to ONODEService

- Verify ONODEService is running: `onode service status` or check Task Manager
- Check it's listening: `netstat -an | findstr 8765`
- Check firewall isn't blocking loopback on port 8765

### Remote control commands not executing

- ONODEService polls Web4 API every 3s — allow up to 6s for round-trip
- Check ONODEService can reach Web4 API (internet connectivity)
- Check rate limit hasn't been hit (429 response) — max 10 commands/minute per node

### Tray icon not appearing

- Icons generate into `~/.oasis/icons/` on first launch — check this directory exists and is writable
- Check `~/.oasis/logs/onode-manager.log` for SkiaSharp errors

### Charts show no history

- Click "Load 24h History" button in the Metrics tab (history is not loaded automatically on start)
- If the button returns no data, check `~/.oasis/onode-metrics.db` exists (created by ONODEService after first 30s of running)

### WebSocket not connecting in OPORTAL local mode

- WebSocket only works when OPORTAL is accessed via `http://` (not `https://`) — browser blocks `ws://localhost` from `https://` pages
- OPORTAL falls back to 5s polling automatically if WebSocket fails

### Auto-update fails

- The update feed URL must serve the Velopack `RELEASES` file and packages
- Code signing is required for production installs on Windows (UAC) and macOS (Gatekeeper)
- Check the Velopack docs for signing configuration at packaging time
