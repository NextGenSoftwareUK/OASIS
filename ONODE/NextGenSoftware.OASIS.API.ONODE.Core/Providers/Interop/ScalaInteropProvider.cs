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
    /// Scala interop provider
    /// Extracts function signatures from Scala source code
    /// Scala compiles to JVM bytecode, so can also use Java provider for compiled libraries
    /// </summary>
    public class ScalaInteropProvider : ILibraryInteropProvider
    {
        private readonly Dictionary<string, ScalaLibraryInfo> _loadedScripts;
        private readonly object _lockObject = new object();
        private bool _initialized = false;
        private bool _scalaAvailable = false;
        private bool _javaAvailable = false;
        private string _scalaPath = null;
        private string _javaPath = null;

        private class ScalaLibraryInfo
        {
            public string ScriptPath { get; set; }
            public string ScriptContent { get; set; }
        }

        public InteropProviderType ProviderType => InteropProviderType.Scala;

        public string[] SupportedExtensions => new[]
        {
            ".scala", ".sc"
        };

        public ScalaInteropProvider()
        {
            _loadedScripts = new Dictionary<string, ScalaLibraryInfo>();
        }

        public Task<OASISResult<bool>> InitializeAsync()
        {
            var result = new OASISResult<bool>();

            try
            {
                _scalaAvailable = TryDetectScala();
                _javaAvailable = TryDetectJava();
                _initialized = true;
                result.Result = true;
                
                if (_scalaAvailable && _javaAvailable)
                {
                    result.Message = $"Scala interop provider initialized with Scala compiler ({_scalaPath}) and Java runtime ({_javaPath}). Both signature extraction and code execution are available.";
                }
                else
                {
                    result.Message = "Scala interop provider initialized. Function signatures will be extracted from source code. Scala compiler or Java runtime not found - code execution disabled. Install Scala and Java for full support.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error initializing Scala provider: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        private bool TryDetectScala()
        {
            var commonPaths = new[]
            {
                "scala",
                "scalac",
                @"/usr/bin/scala",
                @"/usr/local/bin/scala"
            };

            foreach (var path in commonPaths)
            {
                try
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = path,
                        Arguments = "-version",
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
                                _scalaPath = path;
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

        private bool TryDetectJava()
        {
            var commonPaths = new[]
            {
                "java",
                @"C:\Program Files\Java\*\bin\java.exe"
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
                                _javaPath = matches[0];
                                return true;
                            }
                        }
                        continue;
                    }

                    var startInfo = new ProcessStartInfo
                    {
                        FileName = path,
                        Arguments = "-version",
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
                                _javaPath = path;
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
                    OASISErrorHandling.HandleError(ref result, "Scala file not found.");
                    return Task.FromResult(result);
                }

                var scriptContent = File.ReadAllText(libraryPath);
                var libraryId = Guid.NewGuid().ToString();
                var libraryName = Path.GetFileName(libraryPath);

                lock (_lockObject)
                {
                    _loadedScripts[libraryId] = new ScalaLibraryInfo
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

                result.Message = "Scala library loaded. Function signatures will be extracted from source code.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading Scala library: {ex.Message}", ex);
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
                OASISErrorHandling.HandleError(ref result, $"Error unloading Scala library: {ex.Message}", ex);
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

                    if (!_scalaAvailable || !_javaAvailable)
                    {
                        OASISErrorHandling.HandleError(ref result, 
                            "Scala compiler or Java runtime is not available. Cannot execute Scala code. " +
                            "Install Scala (https://www.scala-lang.org/) and Java JDK for code execution. " +
                            "Signature extraction works without runtime, but code execution requires it.");
                        return Task.FromResult(result);
                    }

                    return ExecuteScalaFunctionAsync<T>(scriptInfo, functionName, parameters);
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error invoking Scala function: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        private Task<OASISResult<T>> ExecuteScalaFunctionAsync<T>(ScalaLibraryInfo scriptInfo, string functionName, object[] parameters)
        {
            var result = new OASISResult<T>();

            try
            {
                var scriptDir = Path.GetDirectoryName(scriptInfo.ScriptPath);
                var scriptFile = Path.GetFileName(scriptInfo.ScriptPath);
                var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempDir);

                try
                {
                    // Copy original script to temp directory
                    var scriptFileName = Path.GetFileName(scriptFile);
                    var scriptBaseName = Path.GetFileNameWithoutExtension(scriptFile);
                    File.Copy(scriptInfo.ScriptPath, Path.Combine(tempDir, scriptFileName), true);

                    // Create wrapper Scala object that calls the function
                    var wrapperScript = Path.Combine(tempDir, "Wrapper.scala");
                    var wrapperContent = new StringBuilder();
                    wrapperContent.AppendLine("import scala.util.parsing.json.JSON");
                    wrapperContent.AppendLine($"import {scriptBaseName}._");
                    wrapperContent.AppendLine();
                    wrapperContent.AppendLine("object Wrapper {");
                    wrapperContent.AppendLine("  def main(args: Array[String]): Unit = {");
                    wrapperContent.AppendLine($"    val result = {functionName}({BuildScalaParameters(parameters)})");
                    wrapperContent.AppendLine("    println(JSON.stringify(result))");
                    wrapperContent.AppendLine("  }");
                    wrapperContent.AppendLine("}");

                    File.WriteAllText(wrapperScript, wrapperContent.ToString());

                    // Compile Scala to class files
                    var scalacPath = _scalaPath == "scala" ? "scalac" : _scalaPath.Replace("scala", "scalac");
                    var compileInfo = new ProcessStartInfo
                    {
                        FileName = scalacPath,
                        Arguments = $"-d \"{tempDir}\" \"{wrapperScript}\" \"{Path.Combine(tempDir, scriptFile)}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WorkingDirectory = tempDir
                    };

                    using (var compileProcess = Process.Start(compileInfo))
                    {
                        if (compileProcess != null)
                        {
                            compileProcess.WaitForExit(60000);
                            if (compileProcess.ExitCode != 0)
                            {
                                var error = compileProcess.StandardError.ReadToEnd();
                                OASISErrorHandling.HandleError(ref result, $"Scala compilation error: {error}");
                                return Task.FromResult(result);
                            }
                        }
                    }

                    // Execute compiled Scala class
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = _javaPath,
                        Arguments = $"-cp \"{tempDir}\" Wrapper",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WorkingDirectory = tempDir
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
                                OASISErrorHandling.HandleError(ref result, $"Scala execution error: {error}");
                                return Task.FromResult(result);
                            }

                            result.Result = ParseScalaOutput<T>(output);
                            result.Message = $"Scala function '{functionName}' executed successfully.";
                        }
                    }
                }
                finally
                {
                    try
                    {
                        if (Directory.Exists(tempDir))
                        {
                            Directory.Delete(tempDir, true);
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error executing Scala function '{functionName}': {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        private string BuildScalaParameters(object[] parameters)
        {
            if (parameters == null || parameters.Length == 0)
                return string.Empty;

            var paramStrings = new List<string>();
            foreach (var param in parameters)
            {
                if (param == null)
                    paramStrings.Add("null");
                else if (param is string)
                    paramStrings.Add($"\"{param.ToString().Replace("\"", "\\\"")}\"");
                else if (param is bool)
                    paramStrings.Add((bool)param ? "true" : "false");
                else if (param is int || param is long || param is double || param is float || param is decimal)
                    paramStrings.Add(param.ToString());
                else
                    paramStrings.Add($"JSON.parseFull(\"{JsonSerializer.Serialize(param).Replace("\"", "\\\"")}\").get");
            }

            return string.Join(", ", paramStrings);
        }

        private T ParseScalaOutput<T>(string output)
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

                    var signatures = ParseScalaSignatures(scriptInfo.ScriptContent);
                    var functionNames = signatures.Select(s => s.FunctionName).ToList();

                    result.Result = functionNames;
                    result.Message = functionNames.Count > 0
                        ? $"Found {functionNames.Count} functions in Scala library."
                        : "No functions found in Scala library.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting Scala functions: {ex.Message}", ex);
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
                        LibraryName = "Scala Library",
                        Language = "Scala",
                        Framework = "JVM",
                        CustomProperties = new Dictionary<string, object>()
                    };

                    result.Result = metadata;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting Scala metadata: {ex.Message}", ex);
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

                    var signatures = ParseScalaSignatures(scriptInfo.ScriptContent);

                    result.Result = signatures;
                    result.Message = signatures.Count > 0
                        ? $"Found {signatures.Count} function signatures in Scala library."
                        : "No function signatures found in Scala library.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting Scala function signatures: {ex.Message}", ex);
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
                OASISErrorHandling.HandleError(ref result, $"Error disposing Scala provider: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        private List<IFunctionSignature> ParseScalaSignatures(string scriptContent)
        {
            var signatures = new List<IFunctionSignature>();

            if (string.IsNullOrWhiteSpace(scriptContent))
                return signatures;

            try
            {
                // Parse Scala function definitions: def functionName(param1: Type1, param2: Type2 = default): ReturnType
                var functionPattern = @"def\s+(\w+)\s*\(([^)]*)\)\s*(?::\s*([^=]+))?";
                var functionMatches = System.Text.RegularExpressions.Regex.Matches(
                    scriptContent,
                    functionPattern,
                    System.Text.RegularExpressions.RegexOptions.Multiline);

                foreach (System.Text.RegularExpressions.Match match in functionMatches)
                {
                    var functionName = match.Groups[1].Value;
                    var paramsStr = match.Groups[2].Value;
                    var returnType = match.Groups[3].Success ? match.Groups[3].Value.Trim() : "Unit";

                    var parameters = ParseScalaParameters(paramsStr);

                    signatures.Add(new Objects.Interop.FunctionSignature
                    {
                        FunctionName = functionName,
                        ReturnType = MapScalaTypeToCSharp(returnType),
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

        private List<IParameterInfo> ParseScalaParameters(string paramsStr)
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
                // Scala parameter: name: Type or name: Type = default
                var paramMatch = System.Text.RegularExpressions.Regex.Match(
                    paramPart,
                    @"^(\w+)\s*:\s*([^=]+)(?:\s*=\s*(.+))?$");

                if (paramMatch.Success)
                {
                    var name = paramMatch.Groups[1].Value;
                    var type = paramMatch.Groups[2].Value.Trim();
                    var hasDefault = paramMatch.Groups[3].Success;

                    parameters.Add(new Objects.Interop.ParameterInfo
                    {
                        Name = name,
                        Type = MapScalaTypeToCSharp(type),
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

        private string MapScalaTypeToCSharp(string scalaType)
        {
            if (string.IsNullOrWhiteSpace(scalaType))
                return "object";

            return scalaType.Trim() switch
            {
                "String" => "string",
                "Int" => "int",
                "Long" => "long",
                "Double" => "double",
                "Float" => "float",
                "Boolean" => "bool",
                "Byte" => "byte",
                "Short" => "short",
                "Char" => "char",
                "Unit" => "void",
                "Any" => "object",
                _ => scalaType.Contains("Array") || scalaType.Contains("List") ? "object[]" : "object"
            };
        }
    }
}

