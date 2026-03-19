using System.IO;

namespace NextGenSoftware.Utilities
{
    /// <summary>
    /// Helpers for path handling, especially paths that may come from config (e.g. JSON with forward slashes).
    /// </summary>
    public static class PathHelper
    {
        /// <summary>
        /// Normalizes a path from config (JSON often uses forward slashes). Ensures correct directory separator for current OS so paths work on Windows and Unix.
        /// </summary>
        /// <param name="path">Path string from config, or null/empty.</param>
        /// <returns>Path with OS-specific directory separators; null/empty input returned as-is (empty string for null).</returns>
        public static string NormalizePathFromConfig(string path)
        {
            if (string.IsNullOrEmpty(path)) return path ?? "";
            return path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        }
    }
}
