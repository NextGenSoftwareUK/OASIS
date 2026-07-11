using System;
using System.IO;
using UnityEngine;

namespace OASIS.Omniverse.UnityHost.Runtime
{
    public static class RuntimeDiagnosticsLog
    {
        private const long MaxLogBytes = 512 * 1024;
        private const int MaxLinesRead = 400;
        private static readonly object Sync = new object();

        public static void Write(string category, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            try
            {
                var dir = Path.Combine(Application.persistentDataPath, "Logs");
                Directory.CreateDirectory(dir);
                var path = Path.Combine(dir, "omniverse_runtime.log");
                RotateIfNeeded(path);
                var line = $"[{DateTime.UtcNow:u}] [{category}] {message}{Environment.NewLine}";
                lock (Sync)
                {
                    File.AppendAllText(path, line);
                }
            }
            catch
            {
                // Diagnostics logging is best-effort.
            }
        }

        private static void RotateIfNeeded(string path)
        {
            lock (Sync)
            {
                if (!File.Exists(path))
                {
                    return;
                }

                var info = new FileInfo(path);
                if (info.Length <= MaxLogBytes)
                {
                    return;
                }

                var archive = path + ".1";
                if (File.Exists(archive))
                {
                    File.Delete(archive);
                }

                File.Move(path, archive);
            }
        }

        public static string ReadRecentLines(int maxLines = 120)
        {
            try
            {
                var dir = Path.Combine(Application.persistentDataPath, "Logs");
                var path = Path.Combine(dir, "omniverse_runtime.log");
                if (!File.Exists(path))
                {
                    return "(no diagnostics log file yet)";
                }

                var lines = File.ReadAllLines(path);
                if (lines.Length == 0)
                {
                    return "(diagnostics log is empty)";
                }

                maxLines = Math.Max(1, Math.Min(maxLines, MaxLinesRead));
                var skip = Math.Max(0, lines.Length - maxLines);
                return string.Join(Environment.NewLine, lines, skip, lines.Length - skip);
            }
            catch (Exception ex)
            {
                return $"(failed to read diagnostics log: {ex.Message})";
            }
        }
    }
}

