using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OASIS.Omniverse.UnityHost.Config;
using OASIS.Omniverse.UnityHost.Core;
using OASIS.Omniverse.UnityHost.Native;
using UnityEngine;

namespace OASIS.Omniverse.UnityHost.Runtime
{
    [Serializable]
    public class HostRuntimeHealthSnapshot
    {
        public int totalSessions;
        public string activeGameId;
        public ulong availablePhysicalMemoryMb;
        public int restarts;
        public int recoveredWindowHandles;
        public string lastMaintenanceMessage;
        public string lastMaintenanceUtc;
    }

    public class GameProcessHostService
    {
        private class HostedSession
        {
            public HostedGameDefinition Definition;
            public Process Process;
            public IntPtr WindowHandle;
            public DateTime LastUsedUtc;
            public bool IsVisible;
            public int RecoveryCount;
        }

        private readonly OmniverseHostConfig _config;
        private readonly GlobalSettingsService _globalSettingsService;
        private readonly Dictionary<string, HostedSession> _sessions = new Dictionary<string, HostedSession>(StringComparer.OrdinalIgnoreCase);
        private DateTime _lastMaintenanceUtc = DateTime.MinValue;
        private string _activeGameId = string.Empty;
        private string _lastMaintenanceMessage = "Not started";
        private ulong _lastAvailableMemoryMb;
        private int _restartCount;
        private int _recoveredHandleCount;

        public GameProcessHostService(OmniverseHostConfig config, GlobalSettingsService globalSettingsService)
        {
            _config = config;
            _globalSettingsService = globalSettingsService;
        }

        public async Task<OASISResult<bool>> PreloadAllAsync()
        {
            foreach (var game in _config.games)
            {
                var preloadResult = await PreloadGameAsync(game.gameId);
                if (preloadResult.IsError)
                {
                    return preloadResult;
                }
            }

            return OASISResult<bool>.Success(true, "All games preloaded.");
        }

        public async Task<OASISResult<bool>> PreloadGameAsync(string gameId)
        {
            var game = _config.games.FirstOrDefault(g => g.gameId.Equals(gameId, StringComparison.OrdinalIgnoreCase));
            if (game == null)
            {
                return OASISResult<bool>.Error($"Game id '{gameId}' not found in config.");
            }

            if (_sessions.ContainsKey(gameId) && _sessions[gameId].Process is { HasExited: false })
            {
                var existing = _sessions[gameId];
                if (existing.WindowHandle == IntPtr.Zero)
                {
                    var recovered = await RecoverWindowHandleAsync(existing);
                    if (recovered.IsError)
                    {
                        _lastMaintenanceMessage = recovered.Message;
                    }
                    else
                    {
                        _recoveredHandleCount++;
                    }
                }

                return OASISResult<bool>.Success(true, $"{game.displayName} already preloaded.");
            }

            var exePath = Path.GetFullPath(Path.Combine(Application.dataPath, game.executableRelativePath));
            var workDir = Path.GetFullPath(Path.Combine(Application.dataPath, game.workingDirectoryRelativePath));
            if (!File.Exists(exePath))
            {
                return OASISResult<bool>.Error($"Executable not found: {exePath}");
            }

            if (!Directory.Exists(workDir))
            {
                return OASISResult<bool>.Error($"Working directory not found: {workDir}");
            }

            var globalArgsResult = _globalSettingsService.BuildLaunchArgumentsForGame(game.gameId);
            if (globalArgsResult.IsError)
            {
                return OASISResult<bool>.Error(globalArgsResult.Message);
            }

            var args = string.Join(" ", new[] { game.baseArguments, game.defaultLevelArgument, globalArgsResult.Result }.Where(s => !string.IsNullOrWhiteSpace(s))).Trim();
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = args,
                    WorkingDirectory = workDir,
                    UseShellExecute = true,
                    CreateNoWindow = false,
                    WindowStyle = ProcessWindowStyle.Minimized
                },
                EnableRaisingEvents = true
            };

            if (!process.Start())
            {
                return OASISResult<bool>.Error($"Failed to start process for {game.displayName}.");
            }

            var windowResult = await ResolveMainWindowAsync(process, game.displayName);
            if (windowResult.IsError)
            {
                return OASISResult<bool>.Error(windowResult.Message);
            }

            var session = new HostedSession
            {
                Definition = game,
                Process = process,
                WindowHandle = windowResult.Result,
                LastUsedUtc = DateTime.UtcNow,
                IsVisible = false
            };

            _sessions[gameId] = session;
            HideWindow(session);
            return OASISResult<bool>.Success(true, $"{game.displayName} preloaded and hidden.");
        }

        public async Task<OASISResult<bool>> ActivateGameAsync(string gameId)
        {
            var preloadResult = await PreloadGameAsync(gameId);
            if (preloadResult.IsError)
            {
                return preloadResult;
            }

            foreach (var pair in _sessions)
            {
                if (pair.Key.Equals(gameId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                HideWindow(pair.Value);
            }

            var target = _sessions[gameId];
            if (target.WindowHandle == IntPtr.Zero)
            {
                var recoveredWindow = await RecoverWindowHandleAsync(target);
                if (recoveredWindow.IsError)
                {
                        RuntimeDiagnosticsLog.Write("HOST", recoveredWindow.Message);
                    return OASISResult<bool>.Error(recoveredWindow.Message);
                }

                _recoveredHandleCount++;
                    RuntimeDiagnosticsLog.Write("HOST", $"Recovered window handle for '{target.Definition.displayName}'.");
            }

            EmbedIntoUnityWindow(target.WindowHandle);
            ShowWindow(target);
            target.LastUsedUtc = DateTime.UtcNow;
            _activeGameId = gameId;

            return OASISResult<bool>.Success(true, $"Activated game: {target.Definition.displayName}");
        }

        public OASISResult<bool> HideAllGames()
        {
            foreach (var session in _sessions.Values)
            {
                HideWindow(session);
            }

            _activeGameId = string.Empty;
            return OASISResult<bool>.Success(true, "All game windows hidden.");
        }

        public async Task<OASISResult<bool>> RebuildSessionsForUpdatedSettingsAsync()
        {
            foreach (var session in _sessions.Values.ToList())
            {
                KillSession(session);
            }

            _sessions.Clear();
            _activeGameId = string.Empty;
            return await PreloadAllAsync();
        }

        public OASISResult<bool> TickMaintenance()
        {
            if ((DateTime.UtcNow - _lastMaintenanceUtc).TotalSeconds < Mathf.Max(5, _config.maintenancePollSeconds))
            {
                return OASISResult<bool>.Success(true);
            }

            _lastMaintenanceUtc = DateTime.UtcNow;
            CleanupExitedProcesses();
            RecoverMissingWindowHandles();
            EnsureActiveSessionAlive();

            var availableMb = Win32Interop.GetAvailablePhysicalMemoryMb();
            _lastAvailableMemoryMb = availableMb;
            if (availableMb == 0)
            {
                _lastMaintenanceMessage = "Memory probe unavailable on this platform.";
                RuntimeDiagnosticsLog.Write("HOST", _lastMaintenanceMessage);
                return OASISResult<bool>.Success(true, "Memory probe unavailable on this platform.");
            }

            if (availableMb >= (ulong)Math.Max(128, _config.lowMemoryAvailableMbThreshold))
            {
                _lastMaintenanceMessage = $"Memory healthy ({availableMb} MB available).";
                return OASISResult<bool>.Success(true, $"Memory healthy ({availableMb} MB available).");
            }

            var staleCutoff = DateTime.UtcNow.AddMinutes(-Math.Max(1, _config.staleGameMinutes));
            var removedAny = false;
            foreach (var kv in _sessions
                         .Where(x => !x.Key.Equals(_activeGameId, StringComparison.OrdinalIgnoreCase) && x.Value.LastUsedUtc <= staleCutoff)
                         .OrderBy(x => x.Value.LastUsedUtc)
                         .ToList())
            {
                KillSession(kv.Value);
                _sessions.Remove(kv.Key);
                removedAny = true;

                availableMb = Win32Interop.GetAvailablePhysicalMemoryMb();
                if (availableMb >= (ulong)Math.Max(128, _config.lowMemoryAvailableMbThreshold))
                {
                    break;
                }
            }

            if (!removedAny && availableMb < (ulong)Math.Max(128, _config.lowMemoryAvailableMbThreshold * 0.7f))
            {
                // Emergency pass: unload least-recent non-active game if all sessions are "fresh".
                var emergencyVictim = _sessions
                    .Where(x => !x.Key.Equals(_activeGameId, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(x => x.Value.LastUsedUtc)
                    .FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(emergencyVictim.Key))
                {
                    KillSession(emergencyVictim.Value);
                    _sessions.Remove(emergencyVictim.Key);
                    RuntimeDiagnosticsLog.Write("HOST", $"Emergency unloaded '{emergencyVictim.Key}' due to low memory pressure.");
                    availableMb = Win32Interop.GetAvailablePhysicalMemoryMb();
                }
            }

            _lastMaintenanceMessage = $"Maintenance complete. Available memory: {availableMb} MB";
            return OASISResult<bool>.Success(true, $"Maintenance complete. Available memory: {availableMb} MB");
        }

        public HostRuntimeHealthSnapshot GetHealthSnapshot()
        {
            return new HostRuntimeHealthSnapshot
            {
                totalSessions = _sessions.Count,
                activeGameId = string.IsNullOrWhiteSpace(_activeGameId) ? "(none)" : _activeGameId,
                availablePhysicalMemoryMb = _lastAvailableMemoryMb,
                restarts = _restartCount,
                recoveredWindowHandles = _recoveredHandleCount,
                lastMaintenanceMessage = _lastMaintenanceMessage,
                lastMaintenanceUtc = _lastMaintenanceUtc == DateTime.MinValue ? "never" : _lastMaintenanceUtc.ToString("u")
            };
        }

        private static async Task<OASISResult<IntPtr>> ResolveMainWindowAsync(Process process, string displayName)
        {
            var timeout = DateTime.UtcNow.AddSeconds(20);
            while (DateTime.UtcNow < timeout)
            {
                if (process.HasExited)
                {
                    return OASISResult<IntPtr>.Error($"{displayName} exited during preload.");
                }

                process.Refresh();
                if (process.MainWindowHandle != IntPtr.Zero)
                {
                    return OASISResult<IntPtr>.Success(process.MainWindowHandle);
                }

                await Task.Delay(100);
            }

            return OASISResult<IntPtr>.Error($"Timed out waiting for {displayName} main window handle.");
        }

        private static void EmbedIntoUnityWindow(IntPtr gameWindow)
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            var unityWindow = Win32Interop.GetUnityWindowHandle();
            if (unityWindow == IntPtr.Zero)
            {
                return;
            }

            var style = Win32Interop.GetWindowLong(gameWindow, Win32Interop.GWL_STYLE);
            style &= ~Win32Interop.WS_CAPTION;
            style &= ~Win32Interop.WS_THICKFRAME;
            style &= ~Win32Interop.WS_MINIMIZE;
            style &= ~Win32Interop.WS_MAXIMIZE;
            style &= ~Win32Interop.WS_SYSMENU;
            style |= Win32Interop.WS_CHILD;
            style |= Win32Interop.WS_VISIBLE;

            Win32Interop.SetParent(gameWindow, unityWindow);
            Win32Interop.SetWindowLong(gameWindow, Win32Interop.GWL_STYLE, style);
            Win32Interop.MoveWindow(gameWindow, 0, 0, Screen.width, Screen.height, true);
#endif
        }

        private static void HideWindow(HostedSession session)
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            if (session.WindowHandle != IntPtr.Zero)
            {
                Win32Interop.ShowWindow(session.WindowHandle, Win32Interop.SW_HIDE);
                session.IsVisible = false;
            }
#endif
        }

        private static void ShowWindow(HostedSession session)
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            if (session.WindowHandle != IntPtr.Zero)
            {
                Win32Interop.ShowWindow(session.WindowHandle, Win32Interop.SW_SHOW);
                session.IsVisible = true;
            }
#endif
        }

        private void CleanupExitedProcesses()
        {
            var exited = _sessions.Where(x => x.Value.Process.HasExited).Select(x => x.Key).ToList();
            foreach (var key in exited)
            {
                _sessions.Remove(key);
            }
        }

        private void EnsureActiveSessionAlive()
        {
            if (string.IsNullOrWhiteSpace(_activeGameId))
            {
                return;
            }

            if (!_sessions.TryGetValue(_activeGameId, out var active))
            {
                return;
            }

            if (active.Process == null || active.Process.HasExited)
            {
                _lastMaintenanceMessage = $"Active session '{_activeGameId}' exited. Restarting...";
                _ = RestartSessionAsync(_activeGameId);
            }
        }

        private async Task RestartSessionAsync(string gameId)
        {
            try
            {
                _sessions.Remove(gameId);
                var preload = await PreloadGameAsync(gameId);
                if (!preload.IsError)
                {
                    _restartCount++;
                    RuntimeDiagnosticsLog.Write("HOST", $"Restarted active session '{gameId}' after unexpected exit.");
                    await ActivateGameAsync(gameId);
                }
                else
                {
                    RuntimeDiagnosticsLog.Write("HOST", $"Failed to restart active session '{gameId}': {preload.Message}");
                }
            }
            catch (Exception ex)
            {
                _lastMaintenanceMessage = $"Failed to restart active session '{gameId}': {ex.Message}";
                RuntimeDiagnosticsLog.Write("HOST", _lastMaintenanceMessage);
            }
        }

        private void RecoverMissingWindowHandles()
        {
            foreach (var session in _sessions.Values)
            {
                if (session.Process == null || session.Process.HasExited || session.WindowHandle != IntPtr.Zero)
                {
                    continue;
                }

                _ = RecoverWindowHandleAsync(session);
            }
        }

        private static async Task<OASISResult<bool>> RecoverWindowHandleAsync(HostedSession session)
        {
            if (session?.Process == null || session.Process.HasExited)
            {
                return OASISResult<bool>.Error("Process exited before window handle recovery.");
            }

            var timeout = DateTime.UtcNow.AddSeconds(6);
            while (DateTime.UtcNow < timeout)
            {
                session.Process.Refresh();
                if (session.Process.MainWindowHandle != IntPtr.Zero)
                {
                    session.WindowHandle = session.Process.MainWindowHandle;
                    session.RecoveryCount++;
                    return OASISResult<bool>.Success(true);
                }

                await Task.Delay(100);
            }

            return OASISResult<bool>.Error($"Failed to recover window handle for '{session.Definition?.displayName ?? "Unknown"}'.");
        }

        private static void KillSession(HostedSession session)
        {
            try
            {
                if (!session.Process.HasExited)
                {
                    session.Process.Kill();
                    session.Process.WaitForExit(3000);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to kill session '{session.Definition.displayName}': {ex.Message}");
            }
        }
    }
}

