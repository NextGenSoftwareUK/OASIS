# OASIS HyperDrive Client — Design Specification

**Version:** 1.1  
**Date:** 2026-07-17  
**Status:** Implemented (Phase 1 + Phase 2 complete)  
**Implementation Reference:** [OASIS-HyperDrive-Client-Implementation.md](./OASIS-HyperDrive-Client-Implementation.md)  
**Repository:** `OASIS-HyperDrive-Client` (separate repo)

---

## Table of Contents

1. [Overview](#1-overview)
2. [Goals & Non-Goals](#2-goals--non-goals)
3. [Technology Stack](#3-technology-stack)
4. [Architecture](#4-architecture)
5. [System Tray Icon & States](#5-system-tray-icon--states)
6. [File Browser Window](#6-file-browser-window)
7. [Content Types & Display](#7-content-types--display)
8. [Provider Filter](#8-provider-filter)
9. [File & Holon Operations](#9-file--holon-operations)
10. [Send to Avatar Feature](#10-send-to-avatar-feature)
11. [Metadata Viewer](#11-metadata-viewer)
12. [Context Menu](#12-context-menu)
13. [Settings & Configuration](#13-settings--configuration)
14. [HyperDrive Status Dashboard](#14-hyperdrive-status-dashboard)
15. [Notifications](#15-notifications)
16. [Authentication & Session Management](#16-authentication--session-management)
17. [API Integration](#17-api-integration)
18. [Cross-Platform Considerations](#18-cross-platform-considerations)
19. [Project Structure](#19-project-structure)
20. [Data Models](#20-data-models)
21. [Error Handling & Resilience](#21-error-handling--resilience)
22. [Security](#22-security)
23. [Build & Distribution](#23-build--distribution)
24. [Roadmap & Phasing](#24-roadmap--phasing)

---

## 1. Overview

The **OASIS HyperDrive Client** is a cross-platform desktop system-tray application that gives users a native file-explorer experience over the OASIS HyperDrive — the decentralised, multi-provider storage layer built into the OASIS Architecture.

Like OneDrive, Google Drive, or Dropbox, the client sits quietly in the system tray and lights up when something needs attention. Double-clicking it opens a purpose-built file browser that surfaces the user's **holons, files, NFTs, GeoNFTs**, and other OASIS digital assets stored across all enabled providers. Because OASIS HyperDrive provides **auto-failover, auto-load-balancing, and auto-replication** between providers, the client abstracts all that complexity away — users see a single unified view of their data, with the option to inspect per-provider details when they want to.

The client communicates exclusively with the **WEB4 OASIS API** (`NextGenSoftware.OASIS.API.ONODE.WebAPI`) — specifically the **Data API** (`api/data/*`) and the **HyperDrive API** (`api/hyperDrive/*`).

See also: [HyperDrive API Reference](./API%20Documentation/WEB4%20OASIS%20API/HyperDrive-API.md) | [Data API Reference](./API%20Documentation/WEB4%20OASIS%20API/Data-API.md) | [OASIS HyperDrive Whitepaper](../OASIS_HYPERDRIVE_WHITEPAPER.md)

---

## 2. Goals & Non-Goals

### Goals

- Cross-platform system-tray app (Windows, macOS, Linux)
- Neon-glowing "O" tray icon with clear colour-coded states
- File browser showing holons, files, NFTs, GeoNFTs
- Provider filter (All / per-provider)
- Full CRUD on holons and files: create, rename, edit metadata, delete (soft and hard), download
- Send holon/file to another avatar
- View rich metadata for any item
- Real-time HyperDrive health status (active providers, failover events, replication state)
- Notifications for important events (failover triggered, quota warning, error)
- Secure local session (JWT stored in OS credential store)
- Auto-start on login (opt-in)

### Non-Goals

- Replacing the full ONODE Manager dashboard — see [ONODE Manager Architecture](./API%20Documentation/ONODE-Manager-Architecture.md)
- Offline/sync mode (reads and writes go live to the API; local caching is a future phase)
- Local file system sync (like OneDrive folder sync) — Phase 3
- Mobile client

---

## 3. Technology Stack

### Primary Recommendation: Avalonia UI (same as ONODE Manager)

**Avalonia UI** is the recommended framework, consistent with the ONODE Manager decision. The key reasons remain:

| Criterion | Avalonia | Electron | MAUI |
|---|---|---|---|
| **Cross-platform** | Windows / macOS / Linux native | Windows / macOS / Linux | Windows / macOS only (no Linux) |
| **Language** | C# / .NET | JS/TS | C# / .NET |
| **System tray** | `Avalonia.Controls.TrayIcon` (built-in) | `Tray` module (built-in) | Limited / workarounds |
| **Native look & feel** | Avalonia Fluent theme + custom | Chromium shell | Platform-native controls |
| **Binary size** | ~30–50 MB self-contained | ~120–200 MB | ~50–80 MB |
| **Custom neon effects** | SkiaSharp shaders | CSS | Limited |
| **Consistency with ONODE Manager** | Same codebase patterns, shared libs | None | None |
| **Maturity** | Stable 12.x | Very mature | Newer, gaps |

**Verdict: Avalonia 12.1.0** — same as ONODE Manager. Shared `OasisApiClient` and model libraries reduce duplication between the two apps.

### Full Stack (as implemented)

| Layer | Technology | Version |
|---|---|---|
| UI Framework | Avalonia UI | 12.1.0 |
| Language | C# 12, .NET 10 | — |
| MVVM | ReactiveUI | 23.2.28 |
| Avalonia ReactiveUI bridge | Avalonia.ReactiveUI | 11.3.8 ¹ |
| HTTP Client | `HttpClient` + `System.Text.Json` + Polly | — |
| Auth token storage | `FileCredentialStore` (base64, `%APPDATA%`) ² | — |
| Tray icon | `Avalonia.Controls.TrayIcon` (built-in) | — |
| Icon rendering | SkiaSharp (neon-O rendered per `TrayState` at runtime) | — |
| Notifications | `Avalonia.Controls.Notifications.WindowNotificationManager` | — |
| Logging | Serilog (file + console sinks) | — |
| DI Container | `Microsoft.Extensions.DependencyInjection` | 10.0.10 |
| Background services | `Microsoft.Extensions.Hosting` | 10.0.10 |
| Config | `AppSettings` class + JSON (`settings.json`) | — |
| Packaging | `dotnet publish` → self-contained single-file; Velopack planned | — |

> ¹ `Avalonia.ReactiveUI` uses independent versioning. `11.3.8` is the correct package for Avalonia `12.1.0`.  
> ² OS Keychain integration (Windows Credential Manager / macOS Keychain / Linux libsecret) is planned for Phase 3.

---

## 4. Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    OASIS HyperDrive Client                      │
│                                                                 │
│  ┌─────────────┐   ┌──────────────────┐   ┌─────────────────┐  │
│  │  Tray Icon  │   │  File Browser    │   │  Settings /     │  │
│  │  (always    │   │  Window          │   │  Dashboard      │  │
│  │   running)  │   │  (on dbl-click)  │   │  Window         │  │
│  └──────┬──────┘   └────────┬─────────┘   └────────┬────────┘  │
│         │                   │                       │           │
│  ┌──────▼───────────────────▼───────────────────────▼────────┐  │
│  │                    ViewModels (ReactiveUI)                 │  │
│  │  TrayIconViewModel  FileBrowserViewModel  SettingsViewModel│  │
│  └──────────────────────────┬────────────────────────────────┘  │
│                             │                                   │
│  ┌──────────────────────────▼────────────────────────────────┐  │
│  │                     Services Layer                        │  │
│  │  HyperDriveService  DataService  AvatarService            │  │
│  │  AuthService        NotificationService                   │  │
│  └──────────────────────────┬────────────────────────────────┘  │
│                             │                                   │
│  ┌──────────────────────────▼────────────────────────────────┐  │
│  │               OasisApiClient (shared lib)                 │  │
│  │  Wraps HttpClient, handles JWT injection, OASISResult<T>  │  │
│  └──────────────────────────┬────────────────────────────────┘  │
└─────────────────────────────┼───────────────────────────────────┘
                              │ HTTPS
                              ▼
             ┌────────────────────────────────┐
             │   WEB4 OASIS API               │
             │   api/data/*                   │
             │   api/hyperDrive/*             │
             │   api/avatar/*                 │
             └────────────────────────────────┘
```

### Key Design Decisions

- **Single-process**: tray icon and all windows run in one process. The tray icon boots first; the browser window is created lazily on first open.
- **Background polling**: a lightweight `HyperDriveMonitorService` polls `GET api/hyperDrive/dashboard` every 30 seconds to update tray icon state.
- **Shared library**: `OasisHyperDriveClient.Core` (models, API client, services) is a separate project referenced by both this client and potentially the ONODE Manager, avoiding duplication.
- **Reactive state**: `TrayIconState` is an `IObservable<TrayState>` that all icon and notification components subscribe to.

---

## 5. System Tray Icon & States

### The "O" Icon

The tray icon is a stylised capital **O** letter rendered with a neon glow effect using SkiaSharp. At 16×16 and 32×32 (for HiDPI) it reads as a clean circle-like symbol; at tooltip hover size it is clearly the OASIS "O".

The glow colour and inner fill communicate system health at a glance.

### Colour States

| State | Colour | Glow | Meaning |
|---|---|---|---|
| `Disabled` | **Grey** `#808080` | None / faint | Client not connected / ONODE unreachable |
| `Connecting` | **Blue** `#4488FF` | Pulsing | Authenticating or initial connection |
| `Healthy` | **Cyan** `#00FFEE` | Steady glow | All providers healthy, HyperDrive running |
| `Degraded` | **Yellow** `#FFD700` | Slow pulse | Warning — at least one provider degraded, quota approaching, or non-critical alert |
| `Error` | **Red** `#FF3333` | Fast pulse | Error — failover triggered, provider down, quota exceeded |
| `Syncing` | **Purple** `#CC44FF` | Animated sweep | Active replication in progress |
| `Busy` | **Orange** `#FF8800` | Steady | Upload/download in progress |

The pulse animation is a sinusoidal opacity oscillation on the outer glow ring. State changes animate smoothly with a 300 ms cross-fade.

### Tray Right-Click Menu

```
OASIS HyperDrive Client
─────────────────────────────
▶ Open HyperDrive Browser
─────────────────────────────
  Status: Healthy (5/5 providers)
─────────────────────────────
  View Dashboard
  Settings
─────────────────────────────
  Sign Out
  Quit
```

---

## 6. File Browser Window

### Layout

```
┌──────────────────────────────────────────────────────────────────────────┐
│  OASIS HyperDrive                                        [—] [□] [✕]      │
├──────────────────────────────────────────────────────────────────────────┤
│  [Upload] [Download] [Rename] [Delete] [Send] [Metadata] [Refresh]       │
│  ┌───────────────┐  Filter: [All Providers ▼]  [Search...]               │
│  │ All Files     │  ┌──────────────────────────────────────────────────┐ │
│  │ Holons        │  │ Name       │ Type    │ Provider  │ Size │ Modified│ │
│  │ NFTs          │  ├────────────┼─────────┼───────────┼──────┼────────┤ │
│  │ GeoNFTs       │  │ report.pdf │ File    │ Holochain │ 2 MB │ 2h ago  │ │
│  │ Keys          │  │ profile    │ Holon   │ IPFS      │  —   │ 5d ago  │ │
│  │ Avatar        │  │ CryptoArt  │ NFT     │ Ethereum  │  —   │ 1w ago  │ │
│  │               │  │ Hyde Park  │ GeoNFT  │ Solana    │  —   │ 3d ago  │ │
│  └───────────────┘  └──────────────────────────────────────────────────┘ │
│  47 items  | Holochain  IPFS  Ethereum                                    │
└──────────────────────────────────────────────────────────────────────────┘
```

### Navigation

- **Left sidebar**: content-type navigation — switches the main list filter.
- **Provider filter**: shows items from a specific provider only.
- **Search**: real-time filter on loaded items.

---

## 7. Content Types & Display

| Type | Icon | API Source | Notes |
|---|---|---|---|
| **File** | (type-specific) | `api/data/load-file` | Blob stored in holon |
| **Holon** | (generic) | `api/data/load-holon` | Generic OASIS data object |
| **NFT** | (image) | `api/nft/*` | Shows token ID, chain |
| **GeoNFT** | (map pin) | `api/oland/*` | Map pin thumbnail |
| **Avatar** | (avatar) | `api/avatar/*` | Current user's avatar data |
| **Keys** | (key) | `api/keys/*` | Public key listings |

The sidebar filter switches which `HolonType` is passed to `api/data/load-all-holons`. The "All Files" view loads everything.

---

## 8. Provider Filter

A **Provider Filter** dropdown sits in the toolbar, populated dynamically from `GET api/hyperDrive/config` → `EnabledProviders`. When a specific provider is selected, requests pass `Provider = "<ProviderType>"` to the Data API endpoints.

---

## 9. File & Holon Operations

### Toolbar Operations

| Action | Endpoint |
|---|---|
| Upload file | `POST api/data/save-file` |
| Download | `POST api/data/load-file` |
| Rename | `POST api/data/save-holon` (update name field) |
| Delete | `DELETE api/data/delete-holon` |
| Send to Avatar | holon save with updated owner |
| View Metadata | local display of loaded holon data |

### Delete Dialog

Deleting prompts the user with a choice:

```
Delete "report.pdf"?
  ○ Soft delete (can be recovered later)
  ● Permanent delete (cannot be undone)
  [ Cancel ]  [ Delete ]
```

Maps to `SoftDelete: true/false` on `api/data/delete-holon`.

### Upload

1. Read file bytes via native file picker (`Avalonia.Platform.Storage`).
2. Call `POST api/data/save-file` with `Data`, `FileName`, `FileExtension`, `MimeType`, `Provider`, and the current avatar's `AvatarId`.
3. Refresh the list on success.

---

## 10. Send to Avatar Feature

Users can send any holon or file to another avatar's HyperDrive.

1. Right-click item → **"Send to Avatar..."**
2. A dialog opens with an avatar search field (calls `GET api/avatar/search?searchQuery=...`).
3. User selects a recipient and clicks Send.
4. The holon is saved with the updated owner via `api/data/save-holon`.

---

## 11. Metadata Viewer

Right-click any item → **"View Metadata"**. A modal shows the full holon structure including ID, name, type, MIME, size, created/modified dates, provider keys, replication status, and version chain.

**Replication Status** is derived from `ProviderUniqueStorageKey` — if a provider key exists, it's replicated there.

---

## 12. Context Menu

Right-clicking any item in the file browser shows:

```
Download
Send to Avatar...
──────────────
Rename
──────────────
View Metadata
──────────────
Delete...
```

---

## 13. Settings & Configuration

Accessed via tray right-click → **Settings**.

### General

- **ONODE API URL**: base URL for the WEB4 API (e.g. `https://api.oasis.ac`)
- **Auto-start on login**: checkbox (creates OS autostart entry)
- **Theme**: Light / Dark / System
- **Default Provider**: which provider to prefer when uploading
- **Dashboard refresh interval**: slider 10s–300s

### Notifications

- Failover triggered: on/off
- Provider down: on/off
- Replication complete: on/off
- File sent to you: on/off
- Upload complete: on/off

### Account

- Signed in as: `avatar username`
- **Sign Out** button

---

## 14. HyperDrive Status Dashboard

Accessible from tray menu → **View Dashboard**. A secondary window showing live HyperDrive metrics from `GET api/hyperDrive/dashboard`.

Data sources:
- `GET api/hyperDrive/dashboard` — headline metrics and alerts
- `GET api/hyperDrive/metrics` — per-provider performance
- `GET api/hyperDrive/config` — provider list

---

## 15. Notifications

The client uses OS-native toast notifications via `Avalonia.Controls.Notifications` (`WindowNotificationManager`).

| Trigger | Severity | Example |
|---|---|---|
| Provider goes offline | Error | "Ethereum provider offline. HyperDrive has failed over to Solana." |
| Failover triggered | Warning | "Auto-failover: Polkadot → IPFS due to high error rate." |
| Replication complete | Info | "Your file 'report.pdf' has been replicated to 3 providers." |
| File received from avatar | Info | "@alice sent you a holon: 'project-brief'" |
| Upload complete | Info | "'photo.jpg' uploaded to OASIS HyperDrive." |
| Error on operation | Error | "Failed to delete 'old-notes'." |

---

## 16. Authentication & Session Management

### Login Flow

1. On first launch, a **Login Window** appears (Avalonia dialog).
2. User enters OASIS avatar credentials (email + password).
3. Client calls `POST api/avatar/authenticate` → receives JWT.
4. JWT stored in `%APPDATA%/OasisHyperDriveClient/.session` (base64); OS Keychain planned for Phase 3.
5. On subsequent launches, client reads stored token and validates via `GET api/avatar/get-logged-in-avatar`.

### Startup Note

Because the app uses `ShutdownMode.OnExplicitShutdown` (tray-only; no main window), the login window uses `Show()` + `TaskCompletionSource<bool>` instead of `ShowDialog()` (which requires a parent window).

---

## 17. API Integration

### Key Endpoint Mapping

| UI Action | HTTP | Endpoint |
|---|---|---|
| Load file list | POST | `api/data/load-all-holons` |
| Load specific holon | POST | `api/data/load-holon` |
| Load file bytes | POST | `api/data/load-file` |
| Upload file | POST | `api/data/save-file` |
| Save / rename holon | POST | `api/data/save-holon` |
| Delete item | DELETE | `api/data/delete-holon` |
| Get provider health | GET | `api/hyperDrive/dashboard` |
| Get provider list | GET | `api/hyperDrive/config` |
| Get metrics | GET | `api/hyperDrive/metrics` |
| Avatar search | GET | `api/avatar/search` |
| Authenticate | POST | `api/avatar/authenticate` |

See [HyperDrive API Reference](./API%20Documentation/WEB4%20OASIS%20API/HyperDrive-API.md) and [Data API Reference](./API%20Documentation/WEB4%20OASIS%20API/Data-API.md) for full endpoint documentation.

---

## 18. Cross-Platform Considerations

### System Tray

- **Windows**: `Avalonia.Controls.TrayIcon` renders in the taskbar notification area.
- **macOS**: `Avalonia.Controls.TrayIcon` renders in the macOS menu bar.
- **Linux (X11/Wayland)**: `Avalonia.Controls.TrayIcon` uses `StatusNotifierItem` (SNI) for KDE/GNOME.

### Auto-start on Login

| Platform | Mechanism |
|---|---|
| Windows | Registry `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` |
| macOS | LaunchAgent plist in `~/Library/LaunchAgents/` |
| Linux | `.desktop` file in `~/.config/autostart/` |

Managed via a platform-abstracted `IAutoStartService`. Each platform has its own implementation registered at startup via `OperatingSystem.IsWindows()` / `IsMacOS()` guards.

---

## 19. Project Structure

```
OASIS-HyperDrive-Client/
├── OasisHyperDriveClient.slnx
├── build-win.ps1
├── build-linux.sh
├── build-mac.sh
├── src/
│   ├── OasisHyperDriveClient.Core/      (.NET 10 class library)
│   │   ├── Api/     — OasisApiClient, DataService, HyperDriveService, AvatarService
│   │   ├── Auth/    — AuthService, ICredentialStore, FileCredentialStore
│   │   ├── Models/  — Holon, Avatar, TrayState, HyperDriveDashboard, OASISResult
│   │   └── Services/ — AppSettings, HyperDriveMonitorService, IAutoStartService, INotificationService
│   └── OasisHyperDriveClient/           (.NET 10 Avalonia WinExe)
│       ├── App.axaml / App.axaml.cs     — DI, tray setup, startup
│       ├── Services/  — TrayIconRenderer, AvaloniaNotificationService, *AutoStartService
│       ├── ViewModels/ — TrayIconViewModel, FileBrowserViewModel, LoginViewModel, ...
│       └── Views/     — FileBrowserWindow, LoginWindow, MetadataWindow, DashboardWindow, ...
└── tests/
    └── OasisHyperDriveClient.Tests/     (.NET 10 xUnit)
        ├── HyperDriveMonitorServiceTests.cs
        ├── HolonViewModelTests.cs
        └── AppSettingsTests.cs
```

---

## 20. Data Models

### TrayState

```csharp
public enum TrayState
{
    Disabled,
    Connecting,
    Healthy,
    Degraded,
    Error,
    Syncing,
    Busy
}
```

### HolonViewModel (display model)

```csharp
public class HolonViewModel
{
    public Guid Id { get; init; }
    public string Name { get; init; }
    public string HolonType { get; init; }
    public string? Provider { get; init; }
    public long? SizeBytes { get; init; }
    public DateTime? Modified { get; init; }
    public DateTime? Created { get; init; }
    public string DisplayIcon { get; init; }
    public string SizeDisplay { get; init; }
    public static HolonViewModel FromHolon(Holon h);
}
```

---

## 21. Error Handling & Resilience

### API Errors

Every API call returns `OASISResult<T>`. The client checks `IsError` before processing. On error:
- Log via Serilog to `%APPDATA%/OasisHyperDriveClient/logs/`
- Show status text error in the file browser
- If connectivity error, transition tray to `Disabled` state

### Transient Failures

HTTP calls use Polly retry policies (3 retries with exponential backoff). Circuit breaker is planned for Phase 3.

### HyperDrive Auto-Failover

The OASIS HyperDrive handles provider-level failover server-side. The client surfaces this by reading `Alerts` from the dashboard poll and showing OS notifications.

---

## 22. Security

- **JWT**: stored in local file (base64); OS Keychain planned for Phase 3
- **API base URL**: validated on save in settings
- **No telemetry** without explicit opt-in
- **File content streamed**, not held in memory longer than needed
- **Soft delete default** — permanent delete requires confirmation dialog

---

## 23. Build & Distribution

### Build Targets

```powershell
# Windows x64
.\build-win.ps1 -Version 1.0.0

# Linux x64
bash build-linux.sh 1.0.0

# macOS (x64 + arm64)
bash build-mac.sh 1.0.0
```

All builds use `dotnet publish --self-contained true -p:PublishSingleFile=true`.

### Installers (Phase 3)

**Velopack** (the Squirrel successor) is planned for Phase 3:
- Windows: `.exe` NSIS installer + auto-update
- macOS: `.dmg` with drag-to-Applications
- Linux: `.AppImage`, `.deb`, `.rpm`

---

## 24. Roadmap & Phasing

### Phase 1 — MVP (core tray + browser) — **Complete**

- [x] Avalonia project scaffold with system tray
- [x] Neon-O icon with colour states — rendered via SkiaSharp at runtime
- [x] Login window and JWT auth
- [x] File browser with list view (DataGrid with sidebar nav)
- [x] Load holons via `api/data/load-all-holons`
- [x] Provider filter dropdown
- [x] Basic operations: download, delete (soft + hard), rename
- [x] Metadata viewer
- [x] 30-second dashboard poll → tray state updates
- [x] Windows + macOS + Linux builds

### Phase 2 — Full Operations — **Complete**

- [x] Upload files (file picker + `api/data/save-file`)
- [x] Download files (`api/data/load-file` + save file picker)
- [x] Send to Avatar dialog + avatar search
- [x] Right-click context menu
- [x] Toolbar commands fully wired
- [x] OS toast notifications (`WindowNotificationManager`)
- [x] HyperDrive status dashboard window
- [x] Settings window (API URL, provider, auto-start, refresh rate, notification prefs)
- [x] Auto-start on login (Windows / macOS / Linux)
- [x] Dynamic provider status bar
- [x] Unit tests (17 passing — xUnit + NSubstitute)
- [x] Cross-platform build scripts

### Phase 3 — Advanced

- [ ] Local caching layer with background sync
- [ ] Offline read access to cached holons
- [ ] Batch operations (multi-select)
- [ ] Grid / icon view for NFTs and GeoNFTs
- [ ] Version history viewer (traverse `PreviousVersionId` chain)
- [ ] Context menu "View on Provider" links (Holochain explorer, IPFS gateway, etc.)
- [ ] Drag-and-drop upload
- [ ] OS Keychain credential store
- [ ] Polly circuit breaker
- [ ] Velopack installers + auto-update
- [ ] Certificate pinning for enterprise deployments
- [ ] AI recommendations display

---

*This document is the authoritative design specification for the OASIS HyperDrive Client.*
