using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Interop;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects.Interop;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Providers.Interop
{
    /// <summary>
    /// TypeScript interop provider
    /// Extracts function signatures from TypeScript source code
    /// </summary>
    public class TypeScriptInteropProvider : ILibraryInteropProvider
    {
        private readonly Dictionary<string, string> _loadedScripts;
        private readonly object _lockObject = new object();

        public InteropProviderType ProviderType => InteropProviderType.JavaScript; // Reuse JavaScript type

        public string[] SupportedExtensions => new[]
        {
            ".ts", ".tsx"
        };

        public TypeScriptInteropProvider()
        {
            _loadedScripts = new Dictionary<string, string>();
        }

        public Task<OASISResult<bool>> InitializeAsync()
        {
            var result = new OASISResult<bool> { Result = true };
            return Task.FromResult(result);
        }

        public Task<OASISResult<ILoadedLibrary>> LoadLibraryAsync(string libraryPath, Dictionary<string, object> options = null)
        {
            var result = new OASISResult<ILoadedLibrary>();

            try
            {
                if (!File.Exists(libraryPath))
                {
                    OASISErrorHandling.HandleError(ref result, "TypeScript file not found.");
                    return Task.FromResult(result);
                }

                var scriptContent = File.ReadAllText(libraryPath);
                var libraryId = Guid.NewGuid().ToString();
                var libraryName = Path.GetFileName(libraryPath);

                lock (_lockObject)
                {
                    _loadedScripts[libraryId] = scriptContent;
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

                result.Message = "TypeScript library loaded. Function signatures will be extracted from source code.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading TypeScript library: {ex.Message}", ex);
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
                OASISErrorHandling.HandleError(ref result, $"Error unloading TypeScript library: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        public Task<OASISResult<T>> InvokeAsync<T>(string libraryId, string functionName, params object[] parameters)
        {
            var result = new OASISResult<T>();
            result.Message = "TypeScript function invocation. Requires TypeScript runtime for execution.";
            return Task.FromResult(result);
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
                    if (!_loadedScripts.TryGetValue(libraryId, out var scriptContent))
                    {
                        OASISErrorHandling.HandleError(ref result, "Library not loaded.");
                        return Task.FromResult(result);
                    }

                    var signatures = ParseTypeScriptSignatures(scriptContent);
                    var functionNames = signatures.Select(s => s.FunctionName).ToList();

                    result.Result = functionNames;
                    result.Message = functionNames.Count > 0
                        ? $"Found {functionNames.Count} functions in TypeScript library."
                        : "No functions found in TypeScript library.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting TypeScript functions: {ex.Message}", ex);
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
                        LibraryName = "TypeScript Library",
                        Language = "TypeScript",
                        Framework = "TypeScript",
                        CustomProperties = new Dictionary<string, object>()
                    };

                    result.Result = metadata;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting TypeScript metadata: {ex.Message}", ex);
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
                    if (!_loadedScripts.TryGetValue(libraryId, out var scriptContent))
                    {
                        OASISErrorHandling.HandleError(ref result, "Library not loaded.");
                        return Task.FromResult(result);
                    }

                    var signatures = ParseTypeScriptSignatures(scriptContent);

                    result.Result = signatures;
                    result.Message = signatures.Count > 0
                        ? $"Found {signatures.Count} function signatures in TypeScript library."
                        : "No function signatures found in TypeScript library.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting TypeScript function signatures: {ex.Message}", ex);
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
                OASISErrorHandling.HandleError(ref result, $"Error disposing TypeScript provider: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        private List<IFunctionSignature> ParseTypeScriptSignatures(string scriptContent)
        {
            var signatures = new List<IFunctionSignature>();

            if (string.IsNullOrWhiteSpace(scriptContent))
                return signatures;

            try
            {
                // Parse TypeScript function declarations with types
                // function name(param1: type1, param2: type2): returnType { ... }
                var functionPattern = @"function\s+(\w+)\s*\(([^)]*)\)\s*(?::\s*([^\{]+))?";
                var functionMatches = System.Text.RegularExpressions.Regex.Matches(
                    scriptContent,
                    functionPattern,
                    System.Text.RegularExpressions.RegexOptions.Multiline);

                foreach (System.Text.RegularExpressions.Match match in functionMatches)
                {
                    var functionName = match.Groups[1].Value;
                    var paramsStr = match.Groups[2].Value;
                    var returnType = match.Groups[3].Success ? match.Groups[3].Value.Trim() : "any";

                    var parameters = ParseTypeScriptParameters(paramsStr);

                    signatures.Add(new Objects.Interop.FunctionSignature
                    {
                        FunctionName = functionName,
                        ReturnType = MapTypeScriptTypeToCSharp(returnType),
                        Parameters = parameters,
                        Documentation = ExtractTSDocComment(scriptContent, match.Index)
                    });
                }

                // Parse arrow functions with types: const name = (param1: type1) => type2
                var arrowPattern = @"(?:const|let|var|export\s+(?:const|let|var)?)\s+(\w+)\s*[:=]\s*(?:\(([^)]*)\)|(\w+))\s*(?::\s*([^=]+))?\s*=>";
                var arrowMatches = System.Text.RegularExpressions.Regex.Matches(
                    scriptContent,
                    arrowPattern,
                    System.Text.RegularExpressions.RegexOptions.Multiline);

                foreach (System.Text.RegularExpressions.Match match in arrowMatches)
                {
                    var functionName = match.Groups[1].Value;
                    var paramsStr = match.Groups[2].Success ? match.Groups[2].Value : "";
                    var returnType = match.Groups[4].Success ? match.Groups[4].Value.Trim() : "any";

                    var parameters = ParseTypeScriptParameters(paramsStr);

                    signatures.Add(new Objects.Interop.FunctionSignature
                    {
                        FunctionName = functionName,
                        ReturnType = MapTypeScriptTypeToCSharp(returnType),
                        Parameters = parameters
                    });
                }

                // Parse class methods: methodName(param1: type1): returnType { ... }
                var methodPattern = @"(?:public|private|protected)?\s*(\w+)\s*\(([^)]*)\)\s*(?::\s*([^\{]+))?";
                var methodMatches = System.Text.RegularExpressions.Regex.Matches(
                    scriptContent,
                    methodPattern,
                    System.Text.RegularExpressions.RegexOptions.Multiline);

                foreach (System.Text.RegularExpressions.Match match in methodMatches)
                {
                    var methodName = match.Groups[1].Value;
                    if (methodName == "constructor" || methodName == "get" || methodName == "set")
                        continue;

                    var paramsStr = match.Groups[2].Value;
                    var returnType = match.Groups[3].Success ? match.Groups[3].Value.Trim() : "any";

                    var parameters = ParseTypeScriptParameters(paramsStr);

                    signatures.Add(new Objects.Interop.FunctionSignature
                    {
                        FunctionName = methodName,
                        ReturnType = MapTypeScriptTypeToCSharp(returnType),
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

        private List<IParameterInfo> ParseTypeScriptParameters(string paramsStr)
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
                // TypeScript parameter: name: type or name?: type or name: type = default
                var optionalMatch = System.Text.RegularExpressions.Regex.Match(paramPart, @"^(\w+)\??\s*:\s*([^=]+)(?:\s*=\s*(.+))?$");
                if (optionalMatch.Success)
                {
                    var name = optionalMatch.Groups[1].Value;
                    var type = optionalMatch.Groups[2].Value.Trim();
                    var hasDefault = optionalMatch.Groups[3].Success;

                    parameters.Add(new Objects.Interop.ParameterInfo
                    {
                        Name = name,
                        Type = MapTypeScriptTypeToCSharp(type),
                        IsOptional = paramPart.Contains("?") || hasDefault,
                        DefaultValue = hasDefault ? optionalMatch.Groups[3].Value.Trim() : null
                    });
                }
                else
                {
                    // Simple parameter name
                    parameters.Add(new Objects.Interop.ParameterInfo
                    {
                        Name = paramPart,
                        Type = "object"
                    });
                }
            }

            return parameters;
        }

        private string MapTypeScriptTypeToCSharp(string tsType)
        {
            if (string.IsNullOrWhiteSpace(tsType))
                return "object";

            return tsType.Trim() switch
            {
                "string" => "string",
                "number" => "double",
                "boolean" => "bool",
                "any" => "object",
                "void" => "void",
                "null" => "object",
                "undefined" => "object",
                _ => tsType.Contains("[]") ? tsType.Replace("[]", "[]") : "object"
            };
        }

        private string ExtractTSDocComment(string scriptContent, int functionIndex)
        {
            try
            {
                var beforeFunc = scriptContent.Substring(0, functionIndex);
                var docMatch = System.Text.RegularExpressions.Regex.Match(
                    beforeFunc,
                    @"/\*\*[\s\S]*?\*/",
                    System.Text.RegularExpressions.RegexOptions.RightToLeft);

                if (docMatch.Success)
                {
                    var doc = docMatch.Value;
                    var descMatch = System.Text.RegularExpressions.Regex.Match(doc, @"\*\s+(.+)");
                    if (descMatch.Success)
                    {
                        return descMatch.Groups[1].Value.Trim();
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

