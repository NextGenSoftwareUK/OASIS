using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Interop;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects.Interop;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Providers.Interop
{
    /// <summary>
    /// PowerShell interop provider
    /// Extracts function signatures from PowerShell source code
    /// </summary>
    public class PowerShellInteropProvider : ILibraryInteropProvider
    {
        private readonly Dictionary<string, PowerShellLibraryInfo> _loadedScripts;
        private readonly object _lockObject = new object();
        private bool _initialized = false;
        private bool _powershellAvailable = false;
        private string _powershellPath = null;

        private class PowerShellLibraryInfo
        {
            public string ScriptPath { get; set; }
            public string ScriptContent { get; set; }
        }

        public InteropProviderType ProviderType => InteropProviderType.PowerShell;

        public string[] SupportedExtensions => new[]
        {
            ".ps1", ".psm1", ".psd1"
        };

        public PowerShellInteropProvider()
        {
            _loadedScripts = new Dictionary<string, PowerShellLibraryInfo>();
        }

        public Task<OASISResult<bool>> InitializeAsync()
        {
            var result = new OASISResult<bool>();

            try
            {
                _powershellAvailable = TryDetectPowerShell();
                _initialized = true;
                result.Result = true;
                
                if (_powershellAvailable)
                {
                    result.Message = $"PowerShell interop provider initialized with PowerShell runtime ({_powershellPath}). Both signature extraction and code execution are available.";
                }
                else
                {
                    result.Message = "PowerShell interop provider initialized. Function signatures will be extracted from source code. PowerShell runtime not found - code execution disabled. Install PowerShell for full support.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error initializing PowerShell provider: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        private bool TryDetectPowerShell()
        {
            var commonPaths = new[]
            {
                "pwsh",
                "powershell",
                @"C:\Program Files\PowerShell\*\pwsh.exe",
                @"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe"
            };

            foreach (var path in commonPaths)
            {
                try
                {
                    if (path.Contains("*"))
                    {
                        var dir = Path.GetDirectoryName(path);
                        var pattern = Path.GetFileName(path);
                        if (Directory.Exists(dir))
                        {
                            var matches = Directory.GetFiles(dir, pattern);
                            if (matches.Length > 0)
                            {
                                _powershellPath = matches[0];
                                return true;
                            }
                        }
                        continue;
                    }

                    var startInfo = new ProcessStartInfo
                    {
                        FileName = path,
                        Arguments = "-Command \"$PSVersionTable.PSVersion\"",
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
                                _powershellPath = path;
                                return true;
                            }
                        }
                    }
                }
                catch
                {
                    continue;
                }
            }

            return false;
        }

        public Task<OASISResult<ILoadedLibrary>> LoadLibraryAsync(string libraryPath, Dictionary<string, object> options = null)
        {
            var result = new OASISResult<ILoadedLibrary>();

            try
            {
                if (!File.Exists(libraryPath))
                {
                    OASISErrorHandling.HandleError(ref result, "PowerShell file not found.");
                    return Task.FromResult(result);
                }

                var scriptContent = File.ReadAllText(libraryPath);
                var libraryId = Guid.NewGuid().ToString();
                var libraryName = Path.GetFileName(libraryPath);

                lock (_lockObject)
                {
                    _loadedScripts[libraryId] = new PowerShellLibraryInfo
                    {
                        ScriptPath = libraryPath,
                        ScriptContent = scriptContent
                    };
                }

                result.Result = new Objects.Interop.LoadedLibrary
                {
                    LibraryId = libraryId,
                    LibraryPath = libraryPath,
                    LibraryName = libraryName,
                    ProviderType = ProviderType,
                    LoadedAt = DateTime.UtcNow,
                    Metadata = options ?? new Dictionary<string, object>()
                };

                result.Message = "PowerShell library loaded. Function signatures will be extracted from source code.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading PowerShell library: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        public Task<OASISResult<bool>> UnloadLibraryAsync(string libraryId)
        {
            var result = new OASISResult<bool>();

            try
            {
                lock (_lockObject)
                {
                    if (!_loadedScripts.ContainsKey(libraryId))
                    {
                        OASISErrorHandling.HandleError(ref result, "Library not found.");
                        return Task.FromResult(result);
                    }

                    _loadedScripts.Remove(libraryId);
                }

                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error unloading PowerShell library: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        public Task<OASISResult<T>> InvokeAsync<T>(string libraryId, string functionName, params object[] parameters)
        {
            var result = new OASISResult<T>();

            try
            {
                lock (_lockObject)
                {
                    if (!_loadedScripts.TryGetValue(libraryId, out var scriptInfo))
                    {
                        OASISErrorHandling.HandleError(ref result, "Library not loaded.");
                        return Task.FromResult(result);
                    }

                    if (!_powershellAvailable)
                    {
                        OASISErrorHandling.HandleError(ref result, 
                            "PowerShell runtime is not available. Cannot execute PowerShell code. " +
                            "Install PowerShell (https://aka.ms/powershell) for code execution. " +
                            "Signature extraction works without runtime, but code execution requires it.");
                        return Task.FromResult(result);
                    }

                    return ExecutePowerShellFunctionAsync<T>(scriptInfo, functionName, parameters);
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error invoking PowerShell function: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        private Task<OASISResult<T>> ExecutePowerShellFunctionAsync<T>(PowerShellLibraryInfo scriptInfo, string functionName, object[] parameters)
        {
            var result = new OASISResult<T>();

            try
            {
                var scriptDir = Path.GetDirectoryName(scriptInfo.ScriptPath);
                var scriptFile = Path.GetFileName(scriptInfo.ScriptPath);

                var scriptBuilder = new StringBuilder();
                scriptBuilder.AppendLine($". \"{scriptFile}\"");
                scriptBuilder.AppendLine($"$result = {functionName} {BuildPowerShellParameters(parameters)}");
                scriptBuilder.AppendLine("$result | ConvertTo-Json -Depth 10");

                var tempScript = Path.GetTempFileName() + ".ps1";
                File.WriteAllText(tempScript, scriptBuilder.ToString());

                try
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = _powershellPath,
                        Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{tempScript}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WorkingDirectory = scriptDir
                    };

                    using (var process = Process.Start(startInfo))
                    {
                        if (process != null)
                        {
                            var output = process.StandardOutput.ReadToEnd();
                            var error = process.StandardError.ReadToEnd();
                            process.WaitForExit(30000);

                            if (process.ExitCode != 0)
                            {
                                OASISErrorHandling.HandleError(ref result, $"PowerShell execution error: {error}");
                                return Task.FromResult(result);
                            }

                            result.Result = ParsePowerShellOutput<T>(output);
                            result.Message = $"PowerShell function '{functionName}' executed successfully.";
                        }
                    }
                }
                finally
                {
                    try { File.Delete(tempScript); } catch { }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error executing PowerShell function '{functionName}': {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        private string BuildPowerShellParameters(object[] parameters)
        {
            if (parameters == null || parameters.Length == 0)
                return string.Empty;

            var paramStrings = new List<string>();
            foreach (var param in parameters)
            {
                if (param == null)
                    paramStrings.Add("$null");
                else if (param is string)
                    paramStrings.Add($"\"{param.ToString().Replace("\"", "`\"")}\"");
                else if (param is bool)
                    paramStrings.Add((bool)param ? "$true" : "$false");
                else if (param is int || param is long || param is double || param is float || param is decimal)
                    paramStrings.Add(param.ToString());
                else
                    paramStrings.Add($"('{JsonSerializer.Serialize(param).Replace("'", "''")}' | ConvertFrom-Json)");
            }

            return string.Join(", ", paramStrings);
        }

        private T ParsePowerShellOutput<T>(string output)
        {
            if (string.IsNullOrWhiteSpace(output))
                return default(T);

            var trimmed = output.Trim();
            try
            {
                if (trimmed.StartsWith("{") || trimmed.StartsWith("["))
                    return JsonSerializer.Deserialize<T>(trimmed);
                return (T)Convert.ChangeType(trimmed, typeof(T));
            }
            catch
            {
                return default(T);
            }
        }

        public Task<OASISResult<object>> InvokeAsync(string libraryId, string functionName, params object[] parameters)
        {
            return InvokeAsync<object>(libraryId, functionName, parameters);
        }

        public Task<OASISResult<IEnumerable<string>>> GetAvailableFunctionsAsync(string libraryId)
        {
            var result = new OASISResult<IEnumerable<string>>();

            try
            {
                lock (_lockObject)
                {
                    if (!_loadedScripts.TryGetValue(libraryId, out var scriptInfo))
                    {
                        OASISErrorHandling.HandleError(ref result, "Library not loaded.");
                        return Task.FromResult(result);
                    }

                    var signatures = ParsePowerShellSignatures(scriptInfo.ScriptContent);
                    var functionNames = signatures.Select(s => s.FunctionName).ToList();

                    result.Result = functionNames;
                    result.Message = functionNames.Count > 0
                        ? $"Found {functionNames.Count} functions in PowerShell library."
                        : "No functions found in PowerShell library.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting PowerShell functions: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        public bool IsLibraryLoaded(string libraryId)
        {
            lock (_lockObject)
            {
                return _loadedScripts.ContainsKey(libraryId);
            }
        }

        public Task<OASISResult<ILibraryMetadata>> GetLibraryMetadataAsync(string libraryId)
        {
            var result = new OASISResult<ILibraryMetadata>();

            try
            {
                lock (_lockObject)
                {
                    if (!_loadedScripts.TryGetValue(libraryId, out var scriptContent))
                    {
                        OASISErrorHandling.HandleError(ref result, "Library not loaded.");
                        return Task.FromResult(result);
                    }

                    var metadata = new LibraryMetadata
                    {
                        LibraryName = "PowerShell Library",
                        Language = "PowerShell",
                        Framework = "PowerShell",
                        CustomProperties = new Dictionary<string, object>()
                    };

                    result.Result = metadata;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting PowerShell metadata: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        public Task<OASISResult<IEnumerable<IFunctionSignature>>> GetFunctionSignaturesAsync(string libraryId)
        {
            var result = new OASISResult<IEnumerable<IFunctionSignature>>();

            try
            {
                lock (_lockObject)
                {
                    if (!_loadedScripts.TryGetValue(libraryId, out var scriptInfo))
                    {
                        OASISErrorHandling.HandleError(ref result, "Library not loaded.");
                        return Task.FromResult(result);
                    }

                    var signatures = ParsePowerShellSignatures(scriptInfo.ScriptContent);

                    result.Result = signatures;
                    result.Message = signatures.Count > 0
                        ? $"Found {signatures.Count} function signatures in PowerShell library."
                        : "No function signatures found in PowerShell library.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting PowerShell function signatures: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        public Task<OASISResult<bool>> DisposeAsync()
        {
            var result = new OASISResult<bool>();

            try
            {
                lock (_lockObject)
                {
                    _loadedScripts.Clear();
                }

                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error disposing PowerShell provider: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        private List<IFunctionSignature> ParsePowerShellSignatures(string scriptContent)
        {
            var signatures = new List<IFunctionSignature>();

            if (string.IsNullOrWhiteSpace(scriptContent))
                return signatures;

            try
            {
                // Parse PowerShell function definitions: function Function-Name { param([Type]$Param1, [Type]$Param2) ... }
                // or: function Function-Name($Param1, $Param2) { ... }
                var functionPattern = @"function\s+([\w-]+)\s*(?:\(([^)]*)\))?\s*\{";
                var functionMatches = System.Text.RegularExpressions.Regex.Matches(
                    scriptContent,
                    functionPattern,
                    System.Text.RegularExpressions.RegexOptions.Multiline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                foreach (System.Text.RegularExpressions.Match match in functionMatches)
                {
                    var functionName = match.Groups[1].Value;
                    var paramsStr = match.Groups[2].Success ? match.Groups[2].Value : "";

                    // Also check for param() block inside function
                    var paramBlockMatch = System.Text.RegularExpressions.Regex.Match(
                        scriptContent.Substring(match.Index),
                        @"param\s*\(([^)]+)\)",
                        System.Text.RegularExpressions.RegexOptions.Multiline);

                    if (paramBlockMatch.Success)
                    {
                        paramsStr = paramBlockMatch.Groups[1].Value;
                    }

                    var parameters = ParsePowerShellParameters(paramsStr);

                    signatures.Add(new Objects.Interop.FunctionSignature
                    {
                        FunctionName = functionName,
                        ReturnType = "object", // PowerShell is dynamically typed
                        Parameters = parameters
                    });
                }
            }
            catch
            {
                // Parsing failed
            }

            return signatures;
        }

        private List<IParameterInfo> ParsePowerShellParameters(string paramsStr)
        {
            var parameters = new List<IParameterInfo>();

            if (string.IsNullOrWhiteSpace(paramsStr))
                return parameters;

            var paramParts = paramsStr.Split(',')
                .Select(p => p.Trim())
                .Where(p => !string.IsNullOrEmpty(p))
                .ToList();

            foreach (var paramPart in paramParts)
            {
                // PowerShell parameter: [Type]$name or $name or [Type]$name = default
                var paramMatch = System.Text.RegularExpressions.Regex.Match(
                    paramPart,
                    @"(?:\[([^\]]+)\])?\$(\w+)(?:\s*=\s*(.+))?$");

                if (paramMatch.Success)
                {
                    var type = paramMatch.Groups[1].Success ? paramMatch.Groups[1].Value.Trim() : "object";
                    var name = paramMatch.Groups[2].Value;
                    var hasDefault = paramMatch.Groups[3].Success;

                    parameters.Add(new Objects.Interop.ParameterInfo
                    {
                        Name = name,
                        Type = MapPowerShellTypeToCSharp(type),
                        IsOptional = hasDefault,
                        DefaultValue = hasDefault ? paramMatch.Groups[3].Value.Trim() : null
                    });
                }
                else
                {
                    // Simple $name
                    var simpleMatch = System.Text.RegularExpressions.Regex.Match(paramPart, @"\$(\w+)");
                    if (simpleMatch.Success)
                    {
                        parameters.Add(new Objects.Interop.ParameterInfo
                        {
                            Name = simpleMatch.Groups[1].Value,
                            Type = "object"
                        });
                    }
                }
            }

            return parameters;
        }

        private string MapPowerShellTypeToCSharp(string psType)
        {
            if (string.IsNullOrWhiteSpace(psType))
                return "object";

            return psType.Trim() switch
            {
                "string" => "string",
                "int" => "int",
                "long" => "long",
                "double" => "double",
                "bool" or "boolean" => "bool",
                "DateTime" => "DateTime",
                "array" => "object[]",
                "object" => "object",
                _ => "object"
            };
        }
    }
}

