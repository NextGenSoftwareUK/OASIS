# OASIS HyperDrive Client — Implementation Reference

**Version:** 1.0  
**Date:** 2026-07-17  
**Status:** Complete (Phase 1 + Phase 2)  
**Design Spec:** [OASIS-HyperDrive-Client-Design-Spec.md](./OASIS-HyperDrive-Client-Design-Spec.md)  
**Repository:** `OASIS-HyperDrive-Client` (separate repo)

---

## Table of Contents

1. [Implementation Status](#1-implementation-status)
2. [Solution Layout](#2-solution-layout)
3. [Core Library — OasisHyperDriveClient.Core](#3-core-library)
4. [UI Project — OasisHyperDriveClient](#4-ui-project)
5. [Key Implementation Decisions](#5-key-implementation-decisions)
6. [Build & Run](#6-build--run)
7. [Testing](#7-testing)
8. [Known Gaps / Phase 3 Work](#8-known-gaps--phase-3-work)

---

## 1. Implementation Status

### Phase 1 — Complete

| Feature | Status |
|---|---|
| Avalonia project scaffold, system tray | Done |
| Neon-O icon via SkiaSharp, colour states | Done |
| Login window + JWT auth | Done |
| File browser (DataGrid, sidebar, provider filter) | Done |
| Load holons via `api/data/load-all-holons` | Done |
| Provider filter dropdown | Done |
| Rename dialog | Done |
| Delete (soft + hard) with confirmation dialog | Done |
| Metadata viewer window | Done |
| 30-second dashboard poll → tray state updates | Done |
| Windows + macOS + Linux builds | Done |

### Phase 2 — Complete

| Feature | Status |
|---|---|
| Upload files (file picker + `api/data/save-file`) | Done |
| Download files (`api/data/load-file` + save picker) | Done |
| Send to Avatar dialog (avatar search + transfer) | Done |
| Right-click context menu on file list | Done |
| Toolbar commands fully wired | Done |
| OS toast notifications (`WindowNotificationManager`) | Done |
| HyperDrive dashboard window | Done |
| Settings window (all prefs, notification toggles) | Done |
| Auto-start on login (Windows / macOS / Linux) | Done |
| Dynamic provider status bar | Done |
| SkiaSharp tray icon renderer (per-state neon-O) | Done |
| Unit tests (17 passing) | Done |
| Cross-platform build scripts | Done |

---

## 2. Solution Layout

```
OASIS-HyperDrive-Client/
├── OasisHyperDriveClient.slnx
├── build-win.ps1
├── build-linux.sh
├── build-mac.sh
├── src/
│   ├── OasisHyperDriveClient.Core/      (.NET 10 class library)
│   └── OasisHyperDriveClient/           (.NET 10 Avalonia WinExe)
└── tests/
    └── OasisHyperDriveClient.Tests/     (.NET 10 xUnit)
```

**NuGet packages (UI project):**

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

> **Note:** `Avalonia.ReactiveUI` uses its own version numbering independent of Avalonia itself. `11.3.8` is the correct version for Avalonia `12.1.0`.

---

## 3. Core Library

### `OasisHyperDriveClient.Core`

All models, API clients, and services that could be shared with other apps (e.g. ONODE Manager).

#### Models

| File | Contents |
|---|---|
| `Models/Holon.cs` | `Holon` (API model) + `HolonViewModel` (display model with `DisplayIcon`, `SizeDisplay`, `FromHolon` factory) |
| `Models/Avatar.cs` | `AvatarInfo`, `AuthenticateRequest`, `AuthenticateResponse` |
| `Models/TrayState.cs` | `TrayState` enum (Disabled, Connecting, Healthy, Degraded, Error, Syncing, Busy) + `TrayStateInfo` |
| `Models/HyperDriveDashboard.cs` | `DashboardData`, `PerformanceMetrics`, `CostMetrics`, `DashboardAlert`, `ProviderPerformanceMetrics`, `HyperDriveConfig` |
| `Models/OASISResult.cs` | `OASISResult<T>` — generic API result with `IsError`, `IsWarning`, `Message`, `ErrorCode`, `Result` |

#### API Services

| File | Key Methods |
|---|---|
| `Api/OasisApiClient.cs` | `SetBearerToken`, `ClearToken`, `GetAsync<T>`, `PostAsync<T>`, `DeleteAsync<T>` |
| `Api/DataService.cs` | `LoadAllHolonsAsync`, `LoadHolonAsync`, `LoadFileAsync`, `SaveFileAsync`, `SaveHolonAsync`, `DeleteHolonAsync`, `LoadHolonsForParentAsync` |
| `Api/HyperDriveService.cs` | `GetDashboardAsync`, `GetMetricsAsync`, `GetConfigurationAsync`, `EnableIntelligentModeAsync`, `DisableIntelligentModeAsync` |
| `Api/AvatarService.cs` | `AuthenticateAsync`, `GetLoggedInAvatarAsync`, `SearchAvatarsAsync` |

#### Auth

| File | Purpose |
|---|---|
| `Auth/ICredentialStore.cs` | Interface: `SaveToken`, `LoadToken`, `ClearToken` |
| `Auth/FileCredentialStore.cs` | Base64-encodes JWT to `%APPDATA%/OasisHyperDriveClient/.session` |
| `Auth/AuthService.cs` | `TryRestoreSessionAsync` (reads stored JWT, validates via API), `LoginAsync` (authenticates + stores token), `Logout` |

#### Services

| File | Purpose |
|---|---|
| `Services/AppSettings.cs` | Settings POCO + static `Load()` / instance `Save()` to `%APPDATA%/.../settings.json` |
| `Services/HyperDriveMonitorService.cs` | `BackgroundService` — polls `api/hyperDrive/dashboard` every N seconds; fires `StateChanged` and `AlertReceived` events |
| `Services/IAutoStartService.cs` | Interface: `IsEnabled`, `Enable()`, `Disable()` |
| `Services/INotificationService.cs` | Interface: `Show(title, message, level)` |

---

## 4. UI Project

### Startup Flow (`App.axaml.cs`)

1. `OnFrameworkInitializationCompleted` → `ShutdownMode.OnExplicitShutdown` (tray-only; no main window)
2. `BuildServices()` — registers all DI services including platform-specific `IAutoStartService`
3. `SetupTrayIcon()` — creates `TrayIcon`, subscribes to `TrayIconViewModel.CurrentState` → calls `TrayIconRenderer.Render(state)` to produce a live SkiaSharp PNG and sets `TrayIcon.Icon`
4. `StartAsync()` — tries `TryRestoreSessionAsync`; on failure shows `LoginWindow` using `Show()` + `TaskCompletionSource<bool>` pattern (not `ShowDialog`, which requires a parent window that doesn't exist in tray-only mode)
5. Starts `HyperDriveMonitorService` background polling

### Platform-Specific DI Registration

```csharp
if (OperatingSystem.IsWindows())
    services.AddSingleton<IAutoStartService, WindowsAutoStartService>();
else if (OperatingSystem.IsMacOS())
    services.AddSingleton<IAutoStartService, MacAutoStartService>();
else
    services.AddSingleton<IAutoStartService, LinuxAutoStartService>();
```

### Tray Icon Rendering (`Services/TrayIconRenderer.cs`)

Uses SkiaSharp to paint the neon-O at runtime:
- Outer glow ring (blurred, semi-transparent) in the state colour
- Solid inner ring
- Inner dot for Connecting / Syncing / Busy states
- No glow for Disabled state

No static PNG assets are needed. The icon is re-rendered on every `TrayState` change.

### Windows & Dialogs

| View | ViewModel | Purpose |
|---|---|---|
| `LoginWindow` | `LoginViewModel` | Email/password login; `LoginSucceeded` event drives startup |
| `FileBrowserWindow` | `FileBrowserViewModel` | Main file browser; hidden on close (re-opened from tray) |
| `MetadataWindow` | `MetadataViewModel` | Read-only holon metadata viewer |
| `DashboardWindow` | `DashboardViewModel` | Live HyperDrive health dashboard |
| `SettingsWindow` | `SettingsViewModel` | All app settings; saves to `AppSettings` on confirm |
| `RenameDialog` | `RenameViewModel` | Single text field; fires `Confirmed(newName)` event |
| `DeleteConfirmDialog` | `DeleteConfirmViewModel` | Soft / hard delete radio buttons |
| `SendToAvatarDialog` | `SendToAvatarViewModel` | Avatar search list + send button |

### FileBrowserWindow — Event Wiring

`FileBrowserWindow.axaml.cs` subscribes to ViewModel events and handles dialogs:

```
vm.ViewMetadataRequested  → new MetadataWindow(item).ShowDialog(this)
vm.RenameRequested        → new RenameDialog(vm) + close on Confirmed
vm.DeleteRequested        → new DeleteConfirmDialog(vm) + close on Confirmed
vm.SendToAvatarRequested  → new SendToAvatarDialog(vm) + close on SendRequested
vm.UploadRequested        → StorageProvider.OpenFilePickerAsync → vm.UploadFilesAsync
vm.DownloadRequested      → StorageProvider.SaveFilePickerAsync → vm.DownloadAsync
```

### FileBrowserViewModel — Key Behaviours

- `SelectedContentType` setter and `SelectedProvider` setter both immediately call `LoadItemsAsync()`
- `ActiveProviderNames` collection is updated from `HyperDriveMonitorService.StateChanged`; bound to provider indicators in the status bar
- `SelectContentTypeCommand` accepts a `string` parameter (the content type name), wired from sidebar `Button.CommandParameter`
- Upload reads file bytes into memory, calls `DataService.SaveFileAsync` with the current avatar's `Id`
- Download calls `DataService.LoadFileAsync` then writes bytes to the chosen `IStorageFile`
- Rename replaces the list item with a fresh `HolonViewModel.FromHolon(saved)` (because `Name` is `init`-only)

### Notification Service (`Services/AvaloniaNotificationService.cs`)

`AvaloniaNotificationService` implements `INotificationService`. It must be attached to a `WindowNotificationManager` via `Attach(manager)` before use. This is done in `FileBrowserWindow.OnLoaded`:

```csharp
var manager = new WindowNotificationManager(TopLevel.GetTopLevel(this)!)
{
    Position = NotificationPosition.BottomRight,
    MaxItems = 3
};
_notifications.Attach(manager);
```

Calls from any thread are marshalled to the UI thread via `Dispatcher.UIThread.Post`.

### Auto-Start Implementations

| Platform | File | Mechanism |
|---|---|---|
| Windows | `WindowsAutoStartService.cs` | `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` via `Microsoft.Win32.Registry` |
| macOS | `MacAutoStartService.cs` | `~/Library/LaunchAgents/com.oasis.hyperdrive.plist` |
| Linux | `LinuxAutoStartService.cs` | `~/.config/autostart/oasis-hyperdrive.desktop` |

`WindowsAutoStartService` is annotated `[SupportedOSPlatform("windows")]` to suppress CA1416 cross-platform warnings.

---

## 5. Key Implementation Decisions

### Avalonia.ReactiveUI Version

`Avalonia.ReactiveUI` has its own version scheme. For Avalonia 12.1.0, the correct package is `Avalonia.ReactiveUI 11.3.8`. Using `12.x` will produce a NuGet resolution error.

### WhenAnyValue Ambiguity Fix

`WhenAnyValue` with an inline selector predicate is ambiguous between overloads. The fix is:

```csharp
// Wrong — ambiguous:
var canDo = this.WhenAnyValue(x => x.SelectedItem, x => x != null);

// Correct:
var canDo = this.WhenAnyValue(x => x.SelectedItem).Select(x => x is not null);
// Requires: using System.Reactive.Linq;
```

### No MainWindow in Tray-Only Apps

`ShutdownMode.OnExplicitShutdown` means `MainWindow` is always `null`. `ShowDialog(null)` throws. The login window uses:

```csharp
var tcs = new TaskCompletionSource<bool>();
loginVm.LoginSucceeded += (_, _) => { tcs.TrySetResult(true); loginWin.Close(); };
loginWin.Closed += (_, _) => tcs.TrySetResult(false);
loginWin.Show();
var loggedIn = await tcs.Task;
```

### DataGrid Package

`DataGrid` is not in the core Avalonia package. Requires:
1. `Avalonia.Controls.DataGrid` NuGet package
2. `<StyleInclude Source="avares://Avalonia.Controls.DataGrid/Themes/Fluent.xaml" />` in `App.axaml`
3. `xmlns:dg="clr-namespace:Avalonia.Controls;assembly=Avalonia.Controls.DataGrid"` in views

### NativeMenu Compiled Bindings

`NativeMenu` commands use compiled bindings, which require `x:DataType` on the `<NativeMenu>` element. The `xmlns` for the ViewModel namespace must be on the root `<Application>` element, not the `<NativeMenu>`.

### HolonViewModel.Name is init-only

After a rename, a new `HolonViewModel` is created via `HolonViewModel.FromHolon(saved)` and the old item in `Items` is replaced by index. Mutating `Name` directly causes CS8852.

---

## 6. Build & Run

### Development

```bash
# Build all projects
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

## 7. Testing

```bash
cd C:\Source\OASIS-HyperDrive-Client
dotnet test tests/OasisHyperDriveClient.Tests/
```

**17 tests across 3 files:**

| Test File | What It Tests |
|---|---|
| `HyperDriveMonitorServiceTests.cs` | `TrayState` logic — Healthy / Degraded / Error / Warning alert cases |
| `HolonViewModelTests.cs` | `DisplayIcon` emoji mapping, `SizeDisplay` formatting, `FromHolon` field mapping, default HolonType |
| `AppSettingsTests.cs` | Default values for `AppSettings` and `NotificationSettings` |

Tests use xUnit and NSubstitute. No UI tests (Avalonia headless mode is a Phase 3 addition).

---

## 8. Known Gaps / Phase 3 Work

The following items from the design spec are not yet implemented:

| Item | Notes |
|---|---|
| Polly HTTP retry / circuit breaker | `OasisApiClient` makes bare calls. Retry policy is wired at `AddHttpClient` level but the circuit breaker is not yet configured |
| OS Keychain credential store | `FileCredentialStore` (base64 file) is used. Windows Credential Manager / macOS Keychain / Linux libsecret implementations are planned |
| Grid / icon view for NFTs | List view only. Toggle buttons exist in the spec but are not wired |
| Version history viewer | `PreviousVersionId` chain is not traversed in the UI |
| Drag-and-drop upload | Upload is via file picker only; drop target on `FileBrowserWindow` is not implemented |
| "View on Provider" context menu item | Placeholder in spec; not implemented |
| Breadcrumb navigation / Back / Forward | Navigation history stack is not implemented; sidebar type switching works |
| Velopack installer + auto-update | `dotnet publish` single-file only; Velopack is planned for Phase 3 |
| Certificate pinning | Settings UI placeholder; not yet wired to `HttpClientHandler` |
| AI recommendations display | `HyperDriveService` would need a `GetRecommendationsAsync` method; endpoint not yet mapped |
