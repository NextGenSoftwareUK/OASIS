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
    /// Perl interop provider
    /// Extracts function signatures from Perl source code
    /// </summary>
    public class PerlInteropProvider : ILibraryInteropProvider
    {
        private readonly Dictionary<string, PerlLibraryInfo> _loadedScripts;
        private readonly object _lockObject = new object();
        private bool _initialized = false;
        private bool _perlAvailable = false;
        private string _perlPath = null;

        private class PerlLibraryInfo
        {
            public string ScriptPath { get; set; }
            public string ScriptContent { get; set; }
        }

        public InteropProviderType ProviderType => InteropProviderType.Perl;

        public string[] SupportedExtensions => new[]
        {
            ".pl", ".pm", ".perl"
        };

        public PerlInteropProvider()
        {
            _loadedScripts = new Dictionary<string, PerlLibraryInfo>();
        }

        public Task<OASISResult<bool>> InitializeAsync()
        {
            var result = new OASISResult<bool>();

            try
            {
                _perlAvailable = TryDetectPerl();
                _initialized = true;
                result.Result = true;
                
                if (_perlAvailable)
                {
                    result.Message = $"Perl interop provider initialized with Perl runtime ({_perlPath}). Both signature extraction and code execution are available.";
                }
                else
                {
                    result.Message = "Perl interop provider initialized. Function signatures will be extracted from source code. Perl runtime not found - code execution disabled. Install Perl for full support.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error initializing Perl provider: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        private bool TryDetectPerl()
        {
            var commonPaths = new[]
            {
                "perl",
                @"C:\Perl\bin\perl.exe",
                @"C:\Strawberry\perl\bin\perl.exe",
                @"/usr/bin/perl",
                @"/usr/local/bin/perl",
                @"/opt/homebrew/bin/perl"
            };

            foreach (var path in commonPaths)
            {
                try
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = path,
                        Arguments = "-v",
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
                                _perlPath = path;
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
                    OASISErrorHandling.HandleError(ref result, "Perl file not found.");
                    return Task.FromResult(result);
                }

                var scriptContent = File.ReadAllText(libraryPath);
                var libraryId = Guid.NewGuid().ToString();
                var libraryName = Path.GetFileName(libraryPath);

                lock (_lockObject)
                {
                    _loadedScripts[libraryId] = new PerlLibraryInfo
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

                result.Message = "Perl library loaded. Function signatures will be extracted from source code.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading Perl library: {ex.Message}", ex);
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
                OASISErrorHandling.HandleError(ref result, $"Error unloading Perl library: {ex.Message}", ex);
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

                    if (!_perlAvailable)
                    {
                        OASISErrorHandling.HandleError(ref result, 
                            "Perl runtime is not available. Cannot execute Perl code. " +
                            "Install Perl (https://www.perl.org/) for code execution. " +
                            "Signature extraction works without runtime, but code execution requires it.");
                        return Task.FromResult(result);
                    }

                    return ExecutePerlFunctionAsync<T>(scriptInfo, functionName, parameters);
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error invoking Perl function: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        private Task<OASISResult<T>> ExecutePerlFunctionAsync<T>(PerlLibraryInfo scriptInfo, string functionName, object[] parameters)
        {
            var result = new OASISResult<T>();

            try
            {
                var tempScript = Path.GetTempFileName() + ".pl";
                var scriptDir = Path.GetDirectoryName(scriptInfo.ScriptPath);
                var scriptFile = Path.GetFileName(scriptInfo.ScriptPath);

                var scriptBuilder = new StringBuilder();
                scriptBuilder.AppendLine("use JSON;");
                scriptBuilder.AppendLine($"require './{scriptFile}';");
                scriptBuilder.AppendLine($"my $result = {functionName}({BuildPerlParameters(parameters)});");
                scriptBuilder.AppendLine("print encode_json($result);");

                File.WriteAllText(tempScript, scriptBuilder.ToString());

                try
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = _perlPath,
                        Arguments = $"\"{tempScript}\"",
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
                                OASISErrorHandling.HandleError(ref result, $"Perl execution error: {error}");
                                return Task.FromResult(result);
                            }

                            result.Result = ParsePerlOutput<T>(output);
                            result.Message = $"Perl function '{functionName}' executed successfully.";
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
                OASISErrorHandling.HandleError(ref result, $"Error executing Perl function '{functionName}': {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        private string BuildPerlParameters(object[] parameters)
        {
            if (parameters == null || parameters.Length == 0)
                return string.Empty;

            var paramStrings = new List<string>();
            foreach (var param in parameters)
            {
                if (param == null)
                    paramStrings.Add("undef");
                else if (param is string)
                    paramStrings.Add($"\"{param.ToString().Replace("\"", "\\\"")}\"");
                else if (param is bool)
                    paramStrings.Add((bool)param ? "1" : "0");
                else if (param is int || param is long || param is double || param is float || param is decimal)
                    paramStrings.Add(param.ToString());
                else
                    paramStrings.Add($"decode_json('{JsonSerializer.Serialize(param).Replace("'", "\\'")}')");
            }

            return string.Join(", ", paramStrings);
        }

        private T ParsePerlOutput<T>(string output)
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

                    var signatures = ParsePerlSignatures(scriptInfo.ScriptContent);
                    var functionNames = signatures.Select(s => s.FunctionName).ToList();

                    result.Result = functionNames;
                    result.Message = functionNames.Count > 0
                        ? $"Found {functionNames.Count} subroutines in Perl library."
                        : "No subroutines found in Perl library.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting Perl functions: {ex.Message}", ex);
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
                        LibraryName = "Perl Library",
                        Language = "Perl",
                        Framework = "Perl",
                        CustomProperties = new Dictionary<string, object>()
                    };

                    result.Result = metadata;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting Perl metadata: {ex.Message}", ex);
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

                    var signatures = ParsePerlSignatures(scriptInfo.ScriptContent);

                    result.Result = signatures;
                    result.Message = signatures.Count > 0
                        ? $"Found {signatures.Count} subroutine signatures in Perl library."
                        : "No subroutine signatures found in Perl library.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting Perl function signatures: {ex.Message}", ex);
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
                OASISErrorHandling.HandleError(ref result, $"Error disposing Perl provider: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        private List<IFunctionSignature> ParsePerlSignatures(string scriptContent)
        {
            var signatures = new List<IFunctionSignature>();

            if (string.IsNullOrWhiteSpace(scriptContent))
                return signatures;

            try
            {
                // Parse Perl subroutine definitions: sub subroutine_name { ... } or sub subroutine_name ($param1, $param2) { ... }
                var subPattern = @"sub\s+(\w+)\s*(?:\(([^)]*)\))?\s*\{";
                var subMatches = System.Text.RegularExpressions.Regex.Matches(
                    scriptContent,
                    subPattern,
                    System.Text.RegularExpressions.RegexOptions.Multiline);

                foreach (System.Text.RegularExpressions.Match match in subMatches)
                {
                    var subName = match.Groups[1].Value;
                    var paramsStr = match.Groups[2].Success ? match.Groups[2].Value : "";

                    var parameters = ParsePerlParameters(paramsStr);

                    signatures.Add(new Objects.Interop.FunctionSignature
                    {
                        FunctionName = subName,
                        ReturnType = "object", // Perl is dynamically typed
                        Parameters = parameters,
                        Documentation = ExtractPerlDocComment(scriptContent, match.Index)
                    });
                }
            }
            catch
            {
                // Parsing failed
            }

            return signatures;
        }

        private List<IParameterInfo> ParsePerlParameters(string paramsStr)
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
                // Perl parameter: $name or $name = default or \%hash or @array
                var paramMatch = System.Text.RegularExpressions.Regex.Match(
                    paramPart,
                    @"^([\$@%])(\w+)(?:\s*=\s*(.+))?$");

                if (paramMatch.Success)
                {
                    var sigil = paramMatch.Groups[1].Value;
                    var name = paramMatch.Groups[2].Value;
                    var hasDefault = paramMatch.Groups[3].Success;

                    var type = sigil switch
                    {
                        "$" => "object",
                        "@" => "object[]",
                        "%" => "Dictionary<string, object>",
                        _ => "object"
                    };

                    parameters.Add(new Objects.Interop.ParameterInfo
                    {
                        Name = name,
                        Type = type,
                        IsOptional = hasDefault,
                        DefaultValue = hasDefault ? paramMatch.Groups[3].Value.Trim() : null
                    });
                }
                else
                {
                    parameters.Add(new Objects.Interop.ParameterInfo
                    {
                        Name = paramPart,
                        Type = "object"
                    });
                }
            }

            return parameters;
        }

        private string ExtractPerlDocComment(string scriptContent, int subIndex)
        {
            try
            {
                var beforeSub = scriptContent.Substring(0, subIndex);
                var lines = beforeSub.Split('\n');
                
                // Look for POD comments or # comments
                for (int i = lines.Length - 1; i >= 0 && i >= lines.Length - 5; i--)
                {
                    var line = lines[i].Trim();
                    if (line.StartsWith("#"))
                    {
                        return line.Substring(1).Trim();
                    }
                }
            }
            catch
            {
                // Extraction failed
            }

            return string.Empty;
        }
    }
}

