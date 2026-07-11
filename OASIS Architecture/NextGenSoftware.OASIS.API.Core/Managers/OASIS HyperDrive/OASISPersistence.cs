using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive
{
    /// <summary>
    /// Thin JSON-file persistence layer for OASIS runtime state.
    /// Used by OASISHyperDrive (quota counters) and ONETDiscovery (discovered peer cache).
    /// Files are written atomically via a temp file + rename to avoid corrupt state on crash.
    /// </summary>
    public static class OASISPersistence
    {
        private static readonly JsonSerializerOptions _json = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        /// <summary>
        /// Load state from <paramref name="dataDir"/>/<paramref name="fileName"/>.
        /// Returns default(T) when the file does not exist yet.
        /// </summary>
        public static async Task<T?> LoadAsync<T>(string dataDir, string fileName) where T : class
        {
            try
            {
                var path = Path.Combine(dataDir, fileName);
                if (!File.Exists(path))
                    return default;
                using var fs = File.OpenRead(path);
                return await JsonSerializer.DeserializeAsync<T>(fs, _json);
            }
            catch (Exception ex)
            {
                LoggingManager.Log($"OASISPersistence: failed to load {fileName}: {ex.Message}", Logging.LogType.Warning);
                return default;
            }
        }

        /// <summary>
        /// Save <paramref name="state"/> to <paramref name="dataDir"/>/<paramref name="fileName"/> atomically.
        /// Creates <paramref name="dataDir"/> if it does not exist.
        /// </summary>
        public static async Task SaveAsync<T>(string dataDir, string fileName, T state)
        {
            try
            {
                Directory.CreateDirectory(dataDir);
                var path = Path.Combine(dataDir, fileName);
                var tmp = path + ".tmp";
                using (var fs = File.Create(tmp))
                    await JsonSerializer.SerializeAsync(fs, state, _json);
                File.Move(tmp, path, overwrite: true);
            }
            catch (Exception ex)
            {
                LoggingManager.Log($"OASISPersistence: failed to save {fileName}: {ex.Message}", Logging.LogType.Warning);
            }
        }
    }
}
