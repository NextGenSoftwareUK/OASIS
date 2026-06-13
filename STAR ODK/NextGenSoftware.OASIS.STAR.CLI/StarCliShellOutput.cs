using System;
using System.Text.Json;

namespace NextGenSoftware.OASIS.STAR.CLI
{
    /// <summary>
    /// Structured stdout/stderr helpers for <c>--json</c> and exit codes in shell mode.
    /// </summary>
    internal static class StarCliShellOutput
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        public static void WriteSuccess(bool jsonMode, string message = null, object data = null)
        {
            if (jsonMode)
            {
                Console.Out.WriteLine(JsonSerializer.Serialize(new { success = true, message, data }, JsonOptions));
            }
            else if (!string.IsNullOrEmpty(message))
            {
                Console.WriteLine(message);
            }
        }

        public static void WriteError(bool jsonMode, int exitCode, string error, string detail = null)
        {
            Environment.ExitCode = exitCode;
            if (jsonMode)
            {
                Console.Out.WriteLine(JsonSerializer.Serialize(new { success = false, exitCode, error = error ?? "Error", detail }, JsonOptions));
            }
            else
            {
                Console.Error.WriteLine(error);
                if (!string.IsNullOrEmpty(detail))
                    Console.Error.WriteLine(detail);
            }
        }
    }
}
