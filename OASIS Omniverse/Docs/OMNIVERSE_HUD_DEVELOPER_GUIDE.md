# OASIS Omniverse HUD — Developer Guide

This document covers the architecture, class relationships, data flow, and extension points for the OASIS Omniverse shared HUD overlay. For the player-facing feature reference, see [OMNIVERSE_HUD_USER_GUIDE.md](OMNIVERSE_HUD_USER_GUIDE.md).

---

## Where the HUD sits in the stack

```
┌──────────────────────────────────────────────────────────────┐
│  UNITY HUB (OASIS Omniverse / OASIS Omniverse.unity)        │
│                                                              │
│  ┌─────────────────────────────────────────────────────┐    │
│  │  LAYER 4 — OASIS HUD OVERLAY (sortingOrder 9999)    │    │
│  │  SharedHudOverlay.cs  — Control Center (I key)      │    │
│  │  QuestTrackerWidget.cs — always-visible mini-HUD    │    │
│  └─────────────────────────────────────────────────────┘    │
│                  ↕ C# method calls                           │
│  ┌─────────────────────────────────────────────────────┐    │
│  │  LAYER 3 — OMNIVERSE KERNEL                         │    │
│  │  OmniverseKernel.cs  — bootstrap, portal dispatch,  │    │
│  │    global settings, quest tracker layout             │    │
│  └─────────────────────────────────────────────────────┘    │
│                  ↕ async HTTP / circuit breaker              │
│  ┌─────────────────────────────────────────────────────┐    │
│  │  LAYER 2 — API GATEWAY                              │    │
│  │  Web4Web5GatewayClient.cs — WEB4 OASIS + WEB5 STAR │    │
│  └─────────────────────────────────────────────────────┘    │
│                  ↕ Win32 process embed / IPC                 │
│  ┌─────────────────────────────────────────────────────┐    │
│  │  LAYER 1 — HOSTED OGAME PROCESSES                   │    │
│  │  ODOOM  OQuake  ODOOM3  ODuke3D  OWolf3D  …         │    │
│  └─────────────────────────────────────────────────────┘    │
└──────────────────────────────────────────────────────────────┘
```

The HUD is a Unity `MonoBehaviour` (`SharedHudOverlay.cs`) on a persistent `GameObject`. It builds its entire UI in C# at runtime (no Unity prefabs or UXML) on a dedicated `Canvas` with `renderMode = ScreenSpaceOverlay` and `sortingOrder = 9999`, which guarantees it renders on top of everything including embedded OGame windows.

---

## Key classes

| Class | File | Role |
|-------|------|------|
| `SharedHudOverlay` | `Assets/Scripts/UI/SharedHudOverlay.cs` | All HUD UI: Control Center panel, toasts, status strip, return-to-hub dialog |
| `QuestTrackerWidget` | `Assets/Scripts/UI/QuestTrackerWidget.cs` | Always-visible mini quest list; auto-refreshes every 20 s |
| `OmniverseKernel` | `Assets/Scripts/OmniverseKernel.cs` | Singleton: bootstrap, portal dispatch, settings persistence, quest tracker layout |
| `Web4Web5GatewayClient` | `Assets/Scripts/API/Web4Web5GatewayClient.cs` | HTTP client with retry, backoff, circuit breaker, and response cache |
| `GlobalSettingsService` | `Assets/Scripts/Config/GlobalSettingsService.cs` | In-memory settings store; `CloneCurrentSettings`, `ResolveKeyBinding` |
| `DraggableResizablePanel` | `Assets/Scripts/UI/DraggableResizablePanel.cs` | Drag + resize behaviour added to the Control Center panel at build time |
| `RuntimeDiagnosticsLog` | `Assets/Scripts/Diagnostics/RuntimeDiagnosticsLog.cs` | Rolling in-memory log; `ReadRecentLines(n)` used by Diagnostics tab export |
| `OmniverseHostConfig` | `Assets/Scripts/Config/OmniverseHostConfig.cs` | Boot config: API URLs, list of configured OGames with display names and portal colours |

---

## Initialization

`OmniverseKernel` calls `SharedHudOverlay.Initialize(config, apiClient, settingsService, kernel)` during hub startup. Initialize does three things in order:

1. `SyncHotkeysFromSettings()` — resolves saved key strings from `GlobalSettingsService` into `KeyCode` values; sets `_toggleKey`, `_hideGameKey`, `_returnToHubKey`, `_returnToHubRequiresCtrl`
2. `BuildUi()` — constructs the entire UI hierarchy in code (see below)
3. `_panel.SetActive(false)` — hides the panel so `Update` can show it on first keypress

```csharp
public void Initialize(OmniverseHostConfig config, Web4Web5GatewayClient apiClient,
                       GlobalSettingsService settingsService, OmniverseKernel kernel)
```

### BuildUi hierarchy

```
SharedHudCanvas  (Canvas, CanvasScaler 1920×1080, GraphicRaycaster)
├── OmniverseControlCenter  (_panel)   ← DraggableResizablePanel, hidden until Toggle()
│   ├── Title  (Text)
│   ├── ReturnToHub_Button  (Button, hidden unless game active)
│   ├── Status  (Text, top-right status line)
│   ├── Tabs  (9 tab buttons)
│   ├── ListControls  (_listControlsRoot)
│   │   ├── Search row: label + InputField
│   │   ├── Sort row: Sort label + field Dropdown + direction Dropdown + Refresh + Prev + PageIndicator + Next
│   │   └── Preset row: Preset label + dropdown + name input + Save + Apply + Delete + Template dropdown + Template + Export + Import
│   ├── ContentRoot  (_contentRoot)    ← Text content for list tabs
│   ├── SettingsRoot  (_settingsRoot)  ← Settings-specific controls, hidden when not on Settings tab
│   └── TeleportRoot  (_teleportRoot)  ← Game cards grid, hidden when not on Teleport tab
├── OmniverseToastRoot  (RectTransform, anchored top-centre)
├── OmniverseStatusStrip  (RectTransform, anchored full-width top)
└── ConfirmReturnToHubDialog  (fullscreen overlay, hidden by default)
```

---

## Update loop

`Update()` runs every frame and handles four concerns:

```csharp
private void Update()
{
    // 1. Toggle key — I key (or configured key) opens/closes Control Center
    // 2. Hide game key — F1 (or configured) calls _kernel.HideHostedGames()
    // 3. Return to Hub key — Ctrl+H (or configured) calls ReturnToHub() if a game is active
    // 4. Confirmation dialog keyboard handling — Enter confirms, Escape cancels
    HandleLayoutHotkeys();  // 5. Ctrl+Alt+0/1/2/3/7/8/9
    TickToastQueue();       // 6. Expire old toasts
    TickToastAnimations();  // 7. Smooth Y-position lerp for toast stack
    TickStatusStrip();      // 8. Poll health snapshot every 0.6 s
}
```

### Win32 hotkey detection

On Windows builds, `IsHotkeyDown(KeyCode)` uses `Win32Interop.GetAsyncKeyState(vk) & 0x8000` instead of `Input.GetKey`. This means the hotkeys respond even when an OGame window has captured keyboard input — the same mechanism Steam uses for the Steam overlay.

```csharp
private static bool IsHotkeyDown(KeyCode keyCode)
{
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    var vk = ToVirtualKeyCode(keyCode);
    if (vk > 0) return (Win32Interop.GetAsyncKeyState(vk) & 0x8000) != 0;
#endif
    return Input.GetKey(keyCode);
}
```

`ToVirtualKeyCode` maps `KeyCode.A`–`Z`, `Alpha0`–`Alpha9`, and function keys `F1`–`F12` to Win32 virtual key codes. Any key not in that set falls back to Unity's `Input.GetKey`.

---

## Tab data flow

Opening a tab calls `ShowTabAsync(OmniverseTab)`, which:
1. Stores `_currentTab`, resets `_currentPage = 0`
2. Calls `ConfigureSortOptionsForTab` — updates the sort dropdown options and restores the active preset for the new tab
3. Toggles visibility of `_contentRoot`, `_settingsRoot`, `_teleportRoot`, `_listControlsRoot` based on the tab type
4. Calls `RefreshCurrentTabAsync()` for all tabs except Settings and Teleport

`RefreshCurrentTabAsync()` switches on `_currentTab`, calls the appropriate `_apiClient` method, and populates the relevant cache list:

| Tab | API call | Cache field |
|-----|----------|-------------|
| Inventory | `GetSharedInventoryAsync()` | `_inventoryCache` |
| Quests | `GetCrossGameQuestsAsync()` | `_questCache` |
| NFTs | `GetCrossGameNftsAsync()` | `_nftCache` |
| Avatar | `GetAvatarProfileAsync()` | `_avatarCache` |
| Karma | `GetKarmaOverviewAsync()` | `_karmaCache`, `_karmaTotal` |
| Friends | `GetClanMembersAsync()` | `_clanCache`, `_clanLoadError` |
| Diagnostics | *(no API call — reads from `_kernel.GetRuntimeHealthSnapshot()` at draw time)* | — |

After fetching, `RefreshCurrentTabAsync` always calls `RedrawListTab()` in the `finally` block (even on error) so the UI always shows something.

`RedrawListTab()` applies the current search query and sort to the appropriate cache, then calls the tab's draw method:

```
DrawInventory → WritePaged(filtered, formatter, emptyText)
DrawQuests    → WritePaged(filtered, formatter, emptyText)
DrawNfts      → WritePaged(filtered, formatter, emptyText)
DrawAvatar    → direct StringBuilder output (no paging)
DrawKarma     → WritePaged(filtered, formatter, emptyText)
DrawDiagnostics → direct StringBuilder from _kernel.GetRuntimeHealthSnapshot()
DrawFriends   → direct StringBuilder from _clanCache (own pagination logic)
```

`WritePaged<T>` handles page clamping, page indicator text, and slices the list to `PageSize` (10) items.

---

## Toast system

Toasts are fully self-contained — they do not use the same canvas panel as the Control Center.

### Lifecycle

1. `ShowToast(message, severity, duration)` is called from anywhere (tab data load errors, settings save confirmation, teleport feedback, Return to Hub result, preset operations, diagnostics export)
2. If `_activeToasts.Count >= maxVisible`, the oldest toast is dismissed immediately
3. A new `ToastEntry` is created: a child `GameObject` under `_toastRoot` with an `Image` (coloured background) and a `Text` (icon + message)
4. `AnimateToastIn` coroutine fades alpha 0→1 over 0.12 s
5. `TickToastQueue` (called every frame) checks `expireAtRealtime`; expired toasts are removed and `DismissToast` starts `AnimateToastOutAndDestroy` (fades + slides up 12px over 0.16 s, then `Destroy`)
6. `RelayoutToasts` recalculates `targetY` for each active toast: `-(i * (ToastHeight + ToastSpacing))`
7. `TickToastAnimations` lerps each toast's current Y toward its `targetY` using `1 - exp(-14 * dt)` (fast exponential smoothing)

### Toast severity colours

| Severity | Background | Foreground | Icon |
|----------|-----------|-----------|------|
| Success | `(0.05, 0.26, 0.36, 0.90)` deep blue | `(0.86, 0.97, 1.0)` pale cyan | `[+]` |
| Warning | `(0.40, 0.28, 0.06, 0.90)` amber-dark | `(1.0, 0.95, 0.78)` warm white | `[~]` |
| Error | `(0.40, 0.06, 0.06, 0.90)` dark red | `(1.0, 0.86, 0.86)` pink | `[!]` |

### Public API

```csharp
// Called from OmniverseKernel and any other Unity script
public void ShowToast(string message);                                        // Success, default duration
public void ShowToast(string message, ToastSeverity severity, float duration); // Full control (private overload)
```

---

## Status strip

`TickStatusStrip()` polls `_kernel.GetRuntimeHealthSnapshot()` every `StatusStripPollSeconds` (0.6 s) using `Time.unscaledTime` so it works even when the game is paused.

The health snapshot is a `RuntimeHealthSnapshot` struct provided by `OmniverseKernel` containing two sub-objects:
- `health.api` — `OmniverseApiHealth`: `circuitOpen`, `consecutiveFailures`, `lastError`, `lastResultFromCache`, `authExpired`, `lastLatencyMs`, `lastSuccessUtc`
- `health.host` — `OmniverseHostHealth`: `totalSessions`, `activeGameId`, `availablePhysicalMemoryMb`, `restarts`, `recoveredWindowHandles`, `lastMaintenanceMessage`, `lastMaintenanceUtc`

The strip's visibility is controlled by `settings.showStatusStrip` (toggled in Settings tab, applied via `ApplyAccessibilityTheme()`).

---

## Panel layout persistence

`DraggableResizablePanel` fires `OnLayoutCommitted` whenever the user finishes a drag or resize. `SharedHudOverlay` subscribes:

```csharp
dragResize.OnLayoutCommitted += panelRect =>
    _ = PersistPanelLayoutAsync(ControlCenterPanelId, panelRect);
```

`PersistPanelLayoutAsync` clones the current settings via `GlobalSettingsService.CloneCurrentSettings()`, upserts an `OmniversePanelLayout` record (by `panelId`), and calls `_kernel.SaveUiPreferencesAsync(settings)` which writes to the platform settings store.

On next open, `ApplySavedPanelLayout(rect, panelId)` reads the saved layout and applies it before the panel is shown.

Animated preset snaps use `AnimateRectLayoutCoroutine`: a `SmoothStep` lerp over `SnapAnimationDuration` (0.22 s) that moves both position and size simultaneously.

---

## Settings — save and apply

`SaveAndApplySettingsAsync()` reads all Settings UI controls into a new `OmniverseGlobalSettings` object, then calls:

```csharp
await _kernel.ApplyGlobalSettingsAndRebuildSessionsAsync(settings);
```

This is the key integration point — `OmniverseKernel` applies the new settings to all currently running OGame sessions immediately, not just on next launch. Volume changes, fullscreen, and graphics preset take effect while games are running.

After a successful apply:
- `SyncHotkeysFromSettings()` re-resolves all key bindings so hotkeys update instantly
- `ApplyAccessibilityTheme()` updates panel background alpha, status strip visibility, and content text colour

---

## Preset system internals

View presets are stored in `OmniverseGlobalSettings.viewPresets` (`List<OmniverseViewPreset>`) and `activeViewPresets` (`List<OmniverseActiveViewPreset>`), persisted through `GlobalSettingsService` and `OmniverseKernel.SaveUiPreferencesAsync`.

```csharp
class OmniverseViewPreset    { string name; string tab; string searchQuery; string sortField; bool sortAscending; }
class OmniverseActiveViewPreset { string tab; string presetName; }
```

When switching tabs, `ConfigureSortOptionsForTab` calls `ApplyActivePresetForCurrentTab` which looks up the active preset for the new tab and applies it without triggering recursive preset events (`_suppressPresetEvents` guard).

**Export/import format**

```json
{
  "schema": "oasis.omniverse.viewpresets",
  "schemaVersion": 1,
  "exportedAtUtc": "2026-08-03T12:00:00.000Z",
  "viewPresets": [ { "name": "...", "tab": "Quests", "sortField": "Priority", ... } ],
  "activeViewPresets": [ { "tab": "Quests", "presetName": "Critical Quests First" } ]
}
```

`ParsePresetImportPayload` handles three legacy formats in addition to the current schema: a bare JSON array, a bare object with `viewPresets` but no `schemaVersion`, and the full versioned object.

---

## Accessibility

`ApplyAccessibilityTheme()` applies two settings-driven visual adjustments:

| Setting | Effect |
|---------|--------|
| `uiFontScale` (0.8–1.5) | `_panel.transform.localScale = Vector3.one * scale` — scales all HUD elements uniformly |
| `uiHighContrast` | Panel background: `(0,0,0, 0.93)` vs `(0,0,0, 0.82)` — darker for better readability; content text: `Color.white` vs `(0.8, 0.92, 1)` |

`ApplyAccessibilityTheme()` is called during `BuildUi`, after every `SaveAndApplySettingsAsync`, and whenever the Settings tab is rendered.

---

## Diagnostics export — sanitizer

`CopyDiagnosticsToClipboardSanitized()` runs the JSON output through `SanitizeDiagnosticsJson` which applies five regex passes in order:

1. **GUIDs** — 8-4-4-4-12 hex pattern → `[REDACTED-GUID]`
2. **Tokens** — alphanumeric strings ≥40 chars that look hex-like → `[REDACTED-TOKEN]`
3. **Emails** — standard email pattern → `[REDACTED-EMAIL]`
4. **URLs** — keeps domain, redacts path and query string → `domain/[REDACTED-PATH]`
5. **JSON key patterns** — JSON keys containing `key`, `token`, or `password` with values ≥20 chars → `[REDACTED-KEY]`, `[REDACTED-TOKEN]`, `[REDACTED]`

---

## Extending the HUD

### Adding a new tab

1. Add a value to the `OmniverseTab` enum
2. Add a `CreateTabButton` call in `BuildUi` (9th slot is at `index * 0.165f`, so adjust positions if adding a 10th)
3. Add sort options to `_sortOptions`
4. Add a `case` to `RefreshCurrentTabAsync` with the API call and cache field
5. Add a `case` to `RedrawListTab` calling a new `DrawXxx(builder, query)` method
6. If the tab needs a custom root (like Settings/Teleport), build it and toggle it in `ShowTabAsync`

### Adding a toast from another script

Call `ShowToast(message)` on the `SharedHudOverlay` reference. `OmniverseKernel` holds this reference after initialization. You can also call it from any script that has a reference to `OmniverseKernel.Instance.HudOverlay` (or however your project exposes it).

### Adding a setting

1. Add the field to `OmniverseGlobalSettings`
2. Add a UI control in `BuildSettingsUi`
3. Read the control's value in `SaveAndApplySettingsAsync` and include it in the new settings object
4. Populate the control from `_settingsService.CurrentSettings` in `RenderSettings`
5. If the setting affects hotkeys, call `SyncHotkeysFromSettings` after apply; if it affects visuals, call `ApplyAccessibilityTheme`

### Adding a panel layout

To make a new panel (e.g. a second floating widget) use the same pattern as the Control Center:
1. Add a `DraggableResizablePanel` component
2. Subscribe to `OnLayoutCommitted` → `PersistPanelLayoutAsync(yourPanelId, rect)`
3. Call `ApplySavedPanelLayout(rect, yourPanelId)` after building the panel to restore saved position
4. Add snap-preset methods analogous to `ApplyControlCenterLayoutPresetAsync` and expose them from `OmniverseKernel` (as with `ApplyQuestTrackerLayoutPresetAsync`)

---

## Threading notes

`SharedHudOverlay` uses `async Task` methods (not `async void`) for all async operations so exceptions propagate correctly. All awaited calls (`_apiClient.*Async()`, `_kernel.SaveUiPreferencesAsync`) are awaited on the Unity main thread via Unity's `UnitySynchronizationContext`. No explicit `SwitchToMainThread` calls are needed.

The `_ = SomeAsync()` pattern is used intentionally for fire-and-forget operations (e.g. panel layout persistence on drag end, preset saves triggered by dropdown change) where the result is reflected in a subsequent `ShowToast` call rather than blocking the caller.

---

## Common issues

| Symptom | Likely cause | Fix |
|---------|-------------|-----|
| Control Center panel is blank on first open | Unity UI not yet initialized when `SetActive(true)` was called | `RefreshAfterActivation` coroutine handles this: waits one frame, then recursively activates children and calls `Canvas.ForceUpdateCanvases()`. The `Debug.Log` calls in that coroutine are leftover from the fix and can be removed once rendering is confirmed stable. |
| Hotkeys not working when game window is in focus | Win32 `GetAsyncKeyState` not resolving the key | Check `ToVirtualKeyCode` — add a `case` for any key not already mapped (A–Z, 0–9, and F1–F12 are covered; special keys need explicit cases) |
| Settings applied but volume not changing in game | `ApplyGlobalSettingsAndRebuildSessionsAsync` failing silently | Check `_settingsFeedbackText` — error is written there if the kernel call fails |
| Presets not persisting across sessions | `SaveUiPreferencesAsync` failing or settings service not connected | `_statusText` shows error on failed preset save; check that `GlobalSettingsService` and `OmniverseKernel` are both initialized before `SharedHudOverlay.Initialize` is called |
