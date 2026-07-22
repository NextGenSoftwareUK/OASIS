# OASIS HyperDrive Client — Architecture, Reference & Operations Manual

**Version:** 1.1  
**Date:** 2026-07-18  
**Status:** Phase 1 + Phase 2 complete · Phase 3 in progress  
**Repository:** `OASIS-HyperDrive-Client` (separate repo)

See also: [HyperDrive API Reference](./API%20Documentation/WEB4%20OASIS%20API/HyperDrive-API.md) | [Data API Reference](./API%20Documentation/WEB4%20OASIS%20API/Data-API.md) | [OASIS HyperDrive Whitepaper](../OASIS_HYPERDRIVE_WHITEPAPER.md)

---

## Table of Contents

1. [Overview](#1-overview)
2. [Goals & Non-Goals](#2-goals--non-goals)
3. [Technology Stack](#3-technology-stack)
4. [Architecture](#4-architecture)
5. [Tray Icon & States](#5-tray-icon--states)
6. [File Browser Window](#6-file-browser-window)
7. [Content Types & Display](#7-content-types--display)
8. [Provider Filter](#8-provider-filter)
9. [File & Holon Operations](#9-file--holon-operations)
10. [Send to Avatar](#10-send-to-avatar)
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
21. [Key Implementation Decisions](#21-key-implementation-decisions)
22. [Build & Run](#22-build--run)
23. [Testing](#23-testing)
24. [Error Handling & Resilience](#24-error-handling--resilience)
25. [Security](#25-security)
26. [Roadmap](#26-roadmap)

---

## 1. Overview

The **OASIS HyperDrive Client** is a cross-platform desktop system-tray application that gives users a native file-explorer experience over the OASIS HyperDrive — the decentralised, multi-provider storage layer built into the OASIS Architecture.

Like OneDrive, Google Drive, or Dropbox, the client sits quietly in the system tray and lights up when something needs attention. Double-clicking it opens a purpose-built file browser that surfaces the user's **holons, files, NFTs, GeoNFTs**, and other OASIS digital assets stored across all enabled providers. Because OASIS HyperDrive provides **auto-failover, auto-load-balancing, and auto-replication** between providers, the client abstracts all that complexity away — users see a single unified view of their data, with the option to inspect per-provider details when they want to.

The client communicates exclusively with the **WEB4 OASIS API** (`NextGenSoftware.OASIS.API.ONODE.WebAPI`) — specifically the **Data API** (`api/data/*`) and the **HyperDrive API** (`api/hyperDrive/*`).

---

## 2. Goals & Non-Goals

### Goals

- Cross-platform system-tray app (Windows, macOS, Linux)
- Neon-glowing "O" tray icon with clear colour-coded states
- File browser showing holons, files, NFTs, GeoNFTs
- Provider filter (All / per-provider)
- Full CRUD on holons and files: create, rename, edit metadata, delete (soft and hard), download
- Send holon/file to another avatar
- Rich metadata viewer for any item
- Real-time HyperDrive health dashboard (active providers, failover events, replication state)
- OS notifications for important events (failover, quota warning, error)
- Secure local session (JWT stored in OS credential store)
- Auto-start on login (opt-in)

### Non-Goals

- Replacing the full ONODE Manager dashboard — see [ONODE Manager Architecture](./API%20Documentation/ONODE-Manager-Architecture.md)
- Mobile client
- Local file system sync (like OneDrive folder sync) — Phase 3

---

## 3. Technology Stack

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

> ¹ `Avalonia.ReactiveUI` uses independent versioning — `11.3.8` is the correct package for Avalonia `12.1.0`. Using `12.x` will produce a NuGet resolution error.  
> ² OS Keychain integration (Windows Credential Manager / macOS Keychain / Linux libsecret) is Phase 3.

Avalonia UI was chosen over Electron and MAUI for: true cross-platform (including Linux), native C#/.NET, smaller binary (~30–50 MB vs ~120–200 MB), SkiaSharp shader support for neon effects, and consistency with the ONODE Manager (shared `OasisApiClient` and model libraries).

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
- **Background polling**: `HyperDriveMonitorService` polls `GET api/hyperDrive/dashboard` every 30 seconds to update tray icon state.
- **Shared library**: `OasisHyperDriveClient.Core` (models, API client, services) is a separate project that can be referenced by the ONODE Manager, avoiding duplication.
- **Reactive state**: `TrayIconState` is an `IObservable<TrayState>` that all icon and notification components subscribe to.

---

## 5. Tray Icon & States

The tray icon is a stylised capital **O** rendered with a neon glow using SkiaSharp. No static PNG assets are needed — the icon is re-rendered on every `TrayState` change.

### Colour States

| State | Colour | Glow | Meaning |
|---|---|---|---|
| `Disabled` | Grey `#808080` | None | Client not connected / ONODE unreachable |
| `Connecting` | Blue `#4488FF` | Pulsing | Authenticating or initial connection |
| `Healthy` | Cyan `#00FFEE` | Steady glow | All providers healthy, HyperDrive running |
| `Degraded` | Yellow `#FFD700` | Slow pulse | Warning — provider degraded, quota approaching |
| `Error` | Red `#FF3333` | Fast pulse | Error — failover triggered, provider down |
| `Syncing` | Purple `#CC44FF` | Animated sweep | Active replication in progress |
| `Busy` | Orange `#FF8800` | Steady | Upload/download in progress |

Pulse animation is a sinusoidal opacity oscillation on the outer glow ring. State changes animate with a 300 ms cross-fade.

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

- **Left sidebar**: content-type navigation — switches the main list filter
- **Provider filter**: shows items from a specific provider only
- **Search**: real-time filter on loaded items
- Window is hidden on close and re-opened from tray (not destroyed)

---

## 7. Content Types & Display

| Type | API Source | Notes |
|---|---|---|
| **File** | `api/data/load-file` | Blob stored in holon |
| **Holon** | `api/data/load-holon` | Generic OASIS data object |
| **NFT** | `api/nft/*` | Shows token ID, chain |
| **GeoNFT** | `api/oland/*` | Map pin thumbnail |
| **Avatar** | `api/avatar/*` | Current user's avatar data |
| **Keys** | `api/keys/*` | Public key listings |

The sidebar filter switches which `HolonType` is passed to `api/data/load-all-holons`.

---

## 8. Provider Filter

A **Provider Filter** dropdown sits in the toolbar, populated dynamically from `GET api/hyperDrive/config` → `EnabledProviders`. When a specific provider is selected, requests pass `Provider = "<ProviderType>"` to the Data API endpoints.

---

## 9. File & Holon Operations

| Action | Endpoint |
|---|---|
| Upload file | `POST api/data/save-file` |
| Download | `POST api/data/load-file` |
| Rename | `POST api/data/save-holon` (update name field) |
| Delete | `DELETE api/data/delete-holon` |
| Send to Avatar | holon save with updated owner |
| View Metadata | local display of loaded holon data |

### Delete Dialog

```
Delete "report.pdf"?
  ○ Soft delete (can be recovered later)
  ● Permanent delete (cannot be undone)
  [ Cancel ]  [ Delete ]
```

Maps to `SoftDelete: true/false` on `api/data/delete-holon`.

### Upload

1. Read file bytes via native file picker (`Avalonia.Platform.Storage`)
2. Call `POST api/data/save-file` with `Data`, `FileName`, `FileExtension`, `MimeType`, `Provider`, and the current avatar's `AvatarId`
3. Refresh the list on success

---

## 10. Send to Avatar

1. Right-click item → **"Send to Avatar..."**
2. Dialog opens with an avatar search field (calls `GET api/avatar/search?searchQuery=...`)
3. User selects a recipient and clicks Send
4. Holon is saved with the updated owner via `api/data/save-holon`

---

## 11. Metadata Viewer

Right-click any item → **"View Metadata"**. A modal shows the full holon structure: ID, name, type, MIME, size, created/modified dates, provider keys, replication status, and version chain.

**Replication Status** is derived from `ProviderUniqueStorageKey` — if a provider key exists, it's replicated there.

---

## 12. Context Menu

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
- **Auto-start on login**: checkbox
- **Theme**: Light / Dark / System
- **Default Provider**: preferred upload provider
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

Accessible from tray menu → **View Dashboard**. A secondary window showing live HyperDrive metrics.

| Data Source | Endpoint |
|---|---|
| Headline metrics + alerts | `GET api/hyperDrive/dashboard` |
| Per-provider performance | `GET api/hyperDrive/metrics` |
| Provider list | `GET api/hyperDrive/config` |

---

## 15. Notifications

OS-native toast notifications via `Avalonia.Controls.Notifications.WindowNotificationManager`.

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

1. On first launch a **Login Window** appears
2. User enters OASIS avatar credentials (email + password)
3. Client calls `POST api/avatar/authenticate` → receives JWT
4. JWT stored in `%APPDATA%/OasisHyperDriveClient/.session` (base64); OS Keychain planned for Phase 3
5. On subsequent launches, client reads stored token and validates via `GET api/avatar/get-logged-in-avatar`

### Startup Note

`ShutdownMode.OnExplicitShutdown` (tray-only; no main window) means `MainWindow` is always `null`. `ShowDialog(null)` throws. The login window uses `Show()` + `TaskCompletionSource<bool>`:

```csharp
var tcs = new TaskCompletionSource<bool>();
loginVm.LoginSucceeded += (_, _) => { tcs.TrySetResult(true); loginWin.Close(); };
loginWin.Closed += (_, _) => tcs.TrySetResult(false);
loginWin.Show();
var loggedIn = await tcs.Task;
```

---

## 17. API Integration

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

---

## 18. Cross-Platform Considerations

### System Tray

- **Windows**: taskbar notification area
- **macOS**: menu bar
- **Linux (X11/Wayland)**: `StatusNotifierItem` (SNI) for KDE/GNOME

### Auto-start on Login

| Platform | Mechanism |
|---|---|
| Windows | `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` |
| macOS | LaunchAgent plist in `~/Library/LaunchAgents/` |
| Linux | `.desktop` file in `~/.config/autostart/` |

Managed via platform-abstracted `IAutoStartService` with per-platform implementations registered via `OperatingSystem.IsWindows()` / `IsMacOS()` guards.

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
```

### NuGet Packages

| Package | Version |
|---|---|
| Avalonia | 12.1.0 |
| Avalonia.Desktop | 12.1.0 |
| Avalonia.Controls.DataGrid | 12.1.0 |
| Avalonia.Themes.Fluent | 12.1.0 |
| Avalonia.Fonts.Inter | 12.1.0 |
| Avalonia.ReactiveUI | 11.3.8 |
| ReactiveUI | 23.2.28 |
| SkiaSharp | latest stable |
| Microsoft.Extensions.Hosting | 10.0.10 |
| Serilog.Sinks.File + Console | latest |

---

## 20. Data Models

### TrayState

```csharp
public enum TrayState
{
    Disabled, Connecting, Healthy, Degraded, Error, Syncing, Busy
}
```

### HolonViewModel

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

### OASISResult\<T\>

```csharp
public class OASISResult<T>
{
    public bool IsError { get; set; }
    public bool IsWarning { get; set; }
    public string Message { get; set; }
    public string ErrorCode { get; set; }
    public T Result { get; set; }
}
```

---

## 21. Key Implementation Decisions

### Avalonia.ReactiveUI Version

`Avalonia.ReactiveUI` has its own version scheme. For Avalonia 12.1.0, the correct package is `Avalonia.ReactiveUI 11.3.8`. Using `12.x` produces a NuGet resolution error.

### WhenAnyValue Ambiguity

```csharp
// Wrong — ambiguous overload:
var canDo = this.WhenAnyValue(x => x.SelectedItem, x => x != null);

// Correct:
var canDo = this.WhenAnyValue(x => x.SelectedItem).Select(x => x is not null);
// Requires: using System.Reactive.Linq;
```

### DataGrid Package

`DataGrid` is not in the core Avalonia package. Requires:
1. `Avalonia.Controls.DataGrid` NuGet
2. `<StyleInclude Source="avares://Avalonia.Controls.DataGrid/Themes/Fluent.xaml" />` in `App.axaml`
3. `xmlns:dg="clr-namespace:Avalonia.Controls;assembly=Avalonia.Controls.DataGrid"` in views

### NativeMenu Compiled Bindings

`NativeMenu` commands use compiled bindings requiring `x:DataType` on the `<NativeMenu>` element. The ViewModel namespace `xmlns` must be on the root `<Application>` element.

### HolonViewModel.Name is init-only

After a rename, create a new `HolonViewModel` via `HolonViewModel.FromHolon(saved)` and replace the old item in `Items` by index. Mutating `Name` directly causes CS8852.

### Startup Flow (`App.axaml.cs`)

1. `OnFrameworkInitializationCompleted` → `ShutdownMode.OnExplicitShutdown`
2. `BuildServices()` — registers all DI services including platform-specific `IAutoStartService`
3. `SetupTrayIcon()` — creates `TrayIcon`, subscribes to `TrayIconViewModel.CurrentState` → `TrayIconRenderer.Render(state)` → live SkiaSharp PNG → `TrayIcon.Icon`
4. `StartAsync()` — tries `TryRestoreSessionAsync`; on failure shows `LoginWindow`
5. Starts `HyperDriveMonitorService` background polling

### FileBrowserWindow Event Wiring

```
vm.ViewMetadataRequested  → new MetadataWindow(item).ShowDialog(this)
vm.RenameRequested        → new RenameDialog(vm) + close on Confirmed
vm.DeleteRequested        → new DeleteConfirmDialog(vm) + close on Confirmed
vm.SendToAvatarRequested  → new SendToAvatarDialog(vm) + close on SendRequested
vm.UploadRequested        → StorageProvider.OpenFilePickerAsync → vm.UploadFilesAsync
vm.DownloadRequested      → StorageProvider.SaveFilePickerAsync → vm.DownloadAsync
```

### Notification Service

`AvaloniaNotificationService` must be attached to a `WindowNotificationManager` in `FileBrowserWindow.OnLoaded`:

```csharp
var manager = new WindowNotificationManager(TopLevel.GetTopLevel(this)!)
{
    Position = NotificationPosition.BottomRight,
    MaxItems = 3
};
_notifications.Attach(manager);
```

Calls from any thread are marshalled via `Dispatcher.UIThread.Post`.

---

## 22. Build & Run

### Development

```bash
cd C:\Source\OASIS-HyperDrive-Client
dotnet build

# Run the app
dotnet run --project src/OasisHyperDriveClient/OasisHyperDriveClient.csproj

# Override API URL for local ONODE
$env:OASIS_API_URL = "http://localhost:5000"
dotnet run --project src/OasisHyperDriveClient/OasisHyperDriveClient.csproj
```

### Release Builds

```powershell
# Windows x64
.\build-win.ps1 -Version 1.0.0
# → dist\win\OasisHyperDriveClient.exe

# Linux x64
bash build-linux.sh 1.0.0
# → dist/linux/OasisHyperDriveClient

# macOS (both architectures)
bash build-mac.sh 1.0.0
# → dist/mac-x64/  and  dist/mac-arm64/
```

All builds use `--self-contained true -p:PublishSingleFile=true`.

---

## 23. Testing

```bash
cd C:\Source\OASIS-HyperDrive-Client
dotnet test tests/OasisHyperDriveClient.Tests/
```

**17 tests across 3 files:**

| Test File | What It Tests |
|---|---|
| `HyperDriveMonitorServiceTests.cs` | `TrayState` logic — Healthy / Degraded / Error / Warning alert cases |
| `HolonViewModelTests.cs` | `DisplayIcon` emoji mapping, `SizeDisplay` formatting, `FromHolon` field mapping |
| `AppSettingsTests.cs` | Default values for `AppSettings` and `NotificationSettings` |

Tests use xUnit and NSubstitute.

---

## 24. Error Handling & Resilience

Every API call returns `OASISResult<T>`. The client checks `IsError` before processing. On error:
- Log via Serilog to `%APPDATA%/OasisHyperDriveClient/logs/`
- Show status text error in the file browser
- If connectivity error, transition tray to `Disabled` state

HTTP calls use Polly retry policies (3 retries, exponential backoff). Circuit breaker is Phase 3.

The OASIS HyperDrive handles provider-level failover server-side. The client surfaces this by reading `Alerts` from the dashboard poll and showing OS notifications.

---

## 25. Security

- **JWT**: stored in local file (base64); OS Keychain planned for Phase 3
- **API base URL**: validated on save in settings
- **No telemetry** without explicit opt-in
- **File content streamed**, not held in memory longer than needed
- **Soft delete default** — permanent delete requires confirmation dialog

---

## 26. Roadmap

### Phase 1 — MVP ✅ Complete

- [x] Avalonia project scaffold with system tray
- [x] Neon-O icon via SkiaSharp, colour states
- [x] Login window + JWT auth
- [x] File browser (DataGrid, sidebar, provider filter)
- [x] Load holons via `api/data/load-all-holons`
- [x] Download, delete (soft + hard), rename
- [x] Metadata viewer
- [x] 30-second dashboard poll → tray state updates
- [x] Windows + macOS + Linux builds

### Phase 2 — Full Operations ✅ Complete

- [x] Upload files (file picker + `api/data/save-file`)
- [x] Download files (`api/data/load-file` + save picker)
- [x] Send to Avatar dialog + avatar search
- [x] Right-click context menu
- [x] Toolbar commands fully wired
- [x] OS toast notifications
- [x] HyperDrive dashboard window
- [x] Settings window (all prefs, notification toggles)
- [x] Auto-start on login (Windows / macOS / Linux)
- [x] Dynamic provider status bar
- [x] Unit tests (17 passing)
- [x] Cross-platform build scripts

### Phase 3 — Advanced 🚧 In Progress

- [ ] Polly circuit breaker
- [ ] OS Keychain credential store (Windows Credential Manager / macOS Keychain / Linux libsecret)
- [ ] Grid / icon view for NFTs and GeoNFTs
- [ ] Version history viewer (traverse `PreviousVersionId` chain)
- [ ] Drag-and-drop upload
- [ ] "View on Provider" context menu (Holochain explorer, IPFS gateway, etc.)
- [ ] Breadcrumb navigation / Back / Forward
- [ ] Batch operations (multi-select)
- [ ] Local caching layer with background sync
- [ ] Offline read access to cached holons
- [ ] Velopack installers + auto-update
- [ ] Certificate pinning for enterprise deployments
- [ ] AI recommendations display (`HyperDriveService.GetRecommendationsAsync`)
- [ ] Avalonia headless UI tests
