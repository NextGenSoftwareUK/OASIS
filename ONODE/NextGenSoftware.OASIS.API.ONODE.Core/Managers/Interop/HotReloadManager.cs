using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers.Interop
{
    /// <summary>
    /// Manages hot reloading of libraries when files change
    /// </summary>
    public class HotReloadManager
    {
        private readonly Dictionary<string, FileSystemWatcher> _watchers;
        private readonly Dictionary<string, HotReloadInfo> _reloadInfo;
        private readonly object _lockObject = new object();

        public event EventHandler<LibraryReloadEventArgs> LibraryReloaded;

        public HotReloadManager()
        {
            _watchers = new Dictionary<string, FileSystemWatcher>();
            _reloadInfo = new Dictionary<string, HotReloadInfo>();
        }

        /// <summary>
        /// Enables hot reloading for a library
        /// </summary>
        public OASISResult<bool> EnableHotReload(string libraryId, string libraryPath, Func<string, Task<OASISResult<bool>>> reloadCallback)
        {
            var result = new OASISResult<bool>();

            try
            {
                if (string.IsNullOrWhiteSpace(libraryPath) || !File.Exists(libraryPath))
                {
                    OASISErrorHandling.HandleError(ref result, "Library file not found.");
                    return result;
                }

                lock (_lockObject)
                {
                    // Remove existing watcher if any
                    if (_watchers.TryGetValue(libraryId, out var existingWatcher))
                    {
                        existingWatcher.Dispose();
                        _watchers.Remove(libraryId);
                    }

                    // Create file watcher
                    var directory = Path.GetDirectoryName(libraryPath);
                    var fileName = Path.GetFileName(libraryPath);

                    var watcher = new FileSystemWatcher(directory, fileName)
                    {
                        NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                        EnableRaisingEvents = true
                    };

                    watcher.Changed += async (sender, e) =>
                    {
                        await OnFileChanged(libraryId, libraryPath, reloadCallback);
                    };

                    _watchers[libraryId] = watcher;
                    _reloadInfo[libraryId] = new HotReloadInfo
                    {
                        LibraryId = libraryId,
                        LibraryPath = libraryPath,
                        ReloadCallback = reloadCallback,
                        Enabled = true
                    };

                    result.Result = true;
                    result.Message = $"Hot reload enabled for library: {libraryId}";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error enabling hot reload: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Disables hot reloading for a library
        /// </summary>
        public OASISResult<bool> DisableHotReload(string libraryId)
        {
            var result = new OASISResult<bool>();

            try
            {
                lock (_lockObject)
                {
                    if (_watchers.TryGetValue(libraryId, out var watcher))
                    {
                        watcher.Dispose();
                        _watchers.Remove(libraryId);
                    }

                    if (_reloadInfo.TryGetValue(libraryId, out var info))
                    {
                        info.Enabled = false;
                        _reloadInfo.Remove(libraryId);
                    }

                    result.Result = true;
                    result.Message = $"Hot reload disabled for library: {libraryId}";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error disabling hot reload: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Checks if hot reload is enabled for a library
        /// </summary>
        public bool IsHotReloadEnabled(string libraryId)
        {
            lock (_lockObject)
            {
                return _watchers.ContainsKey(libraryId) &&
                       _reloadInfo.TryGetValue(libraryId, out var info) &&
                       info.Enabled;
            }
        }

        /// <summary>
        /// Manually triggers a reload for a library
        /// </summary>
        public async Task<OASISResult<bool>> ReloadLibraryAsync(string libraryId)
        {
            var result = new OASISResult<bool>();

            try
            {
                lock (_lockObject)
                {
                    if (!_reloadInfo.TryGetValue(libraryId, out var info))
                    {
                        OASISErrorHandling.HandleError(ref result, $"Hot reload not enabled for library: {libraryId}");
                        return result;
                    }

                    if (info.ReloadCallback == null)
                    {
                        OASISErrorHandling.HandleError(ref result, "Reload callback not set.");
                        return result;
                    }

                    // Call reload callback
                    var reloadResult = info.ReloadCallback(info.LibraryPath).Result;
                    if (reloadResult.IsError)
                    {
                        OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(reloadResult, result);
                        return result;
                    }

                    result.Result = true;
                    result.Message = $"Library {libraryId} reloaded successfully.";

                    // Raise event
                    LibraryReloaded?.Invoke(this, new LibraryReloadEventArgs
                    {
                        LibraryId = libraryId,
                        LibraryPath = info.LibraryPath,
                        ReloadedAt = DateTime.UtcNow
                    });
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error reloading library: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Handles file change events
        /// </summary>
        private async Task OnFileChanged(string libraryId, string libraryPath, Func<string, Task<OASISResult<bool>>> reloadCallback)
        {
            try
            {
                // Wait a bit to ensure file write is complete
                await Task.Delay(500);

                lock (_lockObject)
                {
                    if (!_reloadInfo.TryGetValue(libraryId, out var info) || !info.Enabled)
                        return;
                }

                // Call reload callback
                var reloadResult = await reloadCallback(libraryPath);
                if (!reloadResult.IsError)
                {
                    // Raise event
                    LibraryReloaded?.Invoke(this, new LibraryReloadEventArgs
                    {
                        LibraryId = libraryId,
                        LibraryPath = libraryPath,
                        ReloadedAt = DateTime.UtcNow
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling file change: {ex.Message}");
            }
        }

        /// <summary>
        /// Disposes all watchers
        /// </summary>
        public void Dispose()
        {
            lock (_lockObject)
            {
                foreach (var watcher in _watchers.Values)
                {
                    watcher?.Dispose();
                }
                _watchers.Clear();
                _reloadInfo.Clear();
            }
        }

        private class HotReloadInfo
        {
            public string LibraryId { get; set; }
            public string LibraryPath { get; set; }
            public Func<string, Task<OASISResult<bool>>> ReloadCallback { get; set; }
            public bool Enabled { get; set; }
        }
    }

    /// <summary>
    /// Event arguments for library reload events
    /// </summary>
    public class LibraryReloadEventArgs : EventArgs
    {
        public string LibraryId { get; set; }
        public string LibraryPath { get; set; }
        public DateTime ReloadedAt { get; set; }
    }
}


