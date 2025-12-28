using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers.Interop
{
    /// <summary>
    /// Helper class for executing scripts via external processes
    /// Used by scripting language providers (Ruby, PHP, Lua, Perl, etc.)
    /// </summary>
    public static class ProcessExecutionHelper
    {
        /// <summary>
        /// Executes a script function via external process
        /// </summary>
        public static async Task<OASISResult<T>> ExecuteScriptFunctionAsync<T>(
            string runtimePath,
            string scriptPath,
            string functionName,
            object[] parameters,
            string[] runtimeArgs = null,
            int timeoutMs = 30000)
        {
            var result = new OASISResult<T>();

            try
            {
                if (string.IsNullOrEmpty(runtimePath) || !File.Exists(runtimePath))
                {
                    OASISErrorHandling.HandleError(ref result, 
                        $"Runtime not found at '{runtimePath}'. Please install the required runtime.");
                    return result;
                }

                if (!File.Exists(scriptPath))
                {
                    OASISErrorHandling.HandleError(ref result, $"Script file not found: {scriptPath}");
                    return result;
                }

                // Create wrapper script that calls the function
                var wrapperScript = CreateWrapperScript(scriptPath, functionName, parameters);
                var tempScriptPath = Path.GetTempFileName() + Path.GetExtension(scriptPath);

                try
                {
                    File.WriteAllText(tempScriptPath, wrapperScript);

                    var startInfo = new ProcessStartInfo
                    {
                        FileName = runtimePath,
                        Arguments = BuildArguments(runtimeArgs, tempScriptPath),
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WorkingDirectory = Path.GetDirectoryName(scriptPath)
                    };

                    using (var process = Process.Start(startInfo))
                    {
                        if (process == null)
                        {
                            OASISErrorHandling.HandleError(ref result, "Failed to start process.");
                            return result;
                        }

                        var outputTask = process.StandardOutput.ReadToEndAsync();
                        var errorTask = process.StandardError.ReadToEndAsync();

                        var completed = await Task.WhenAny(
                            Task.Run(() => process.WaitForExit(timeoutMs)),
                            Task.Delay(timeoutMs + 1000)
                        );

                        if (!process.HasExited)
                        {
                            process.Kill();
                            OASISErrorHandling.HandleError(ref result, $"Process timed out after {timeoutMs}ms");
                            return result;
                        }

                        var output = await outputTask;
                        var error = await errorTask;

                        if (process.ExitCode != 0)
                        {
                            OASISErrorHandling.HandleError(ref result, $"Execution error: {error}");
                            return result;
                        }

                        // Parse output
                        result.Result = ParseOutput<T>(output);
                        result.Message = $"Function '{functionName}' executed successfully.";
                    }
                }
                finally
                {
                    try { File.Delete(tempScriptPath); } catch { }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error executing script function: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Detects if a runtime is available in PATH or common locations
        /// </summary>
        public static string DetectRuntime(string runtimeName, string[] commonPaths = null)
        {
            // Try PATH first
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = runtimeName,
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(startInfo))
                {
                    if (process != null)
                    {
                        process.WaitForExit(2000);
                        if (process.ExitCode == 0)
                        {
                            return runtimeName;
                        }
                    }
                }
            }
            catch
            {
                // Not in PATH
            }

            // Try common paths
            if (commonPaths != null)
            {
                foreach (var path in commonPaths)
                {
                    if (File.Exists(path))
                    {
                        return path;
                    }
                }
            }

            return null;
        }

        private static string CreateWrapperScript(string scriptPath, string functionName, object[] parameters)
        {
            // This is a template - each language provider will override with language-specific implementation
            var scriptDir = Path.GetDirectoryName(scriptPath);
            var scriptFile = Path.GetFileName(scriptPath);
            
            var sb = new StringBuilder();
            sb.AppendLine($"# Load script: {scriptFile}");
            sb.AppendLine($"# Call function: {functionName}");
            sb.AppendLine($"# Parameters: {JsonSerializer.Serialize(parameters)}");
            
            return sb.ToString();
        }

        private static string BuildArguments(string[] runtimeArgs, string scriptPath)
        {
            var args = new List<string>();
            if (runtimeArgs != null)
            {
                args.AddRange(runtimeArgs);
            }
            args.Add($"\"{scriptPath}\"");
            return string.Join(" ", args);
        }

        private static T ParseOutput<T>(string output)
        {
            if (string.IsNullOrWhiteSpace(output))
                return default(T);

            var trimmed = output.Trim();

            try
            {
                // Try JSON first
                if (trimmed.StartsWith("{") || trimmed.StartsWith("["))
                {
                    return JsonSerializer.Deserialize<T>(trimmed);
                }

                // Try direct conversion
                return (T)Convert.ChangeType(trimmed, typeof(T));
            }
            catch
            {
                // Fallback to string conversion
                try
                {
                    return (T)Convert.ChangeType(trimmed, typeof(T));
                }
                catch
                {
                    return default(T);
                }
            }
        }
    }
}

