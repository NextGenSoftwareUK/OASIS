using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Interop;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers.Interop;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers.Interop
{
    /// <summary>
    /// Generates C# proxy classes for libraries to enable IntelliSense support
    /// Creates strongly-typed wrappers around interop libraries
    /// </summary>
    public class LibraryProxyGenerator
    {
        private readonly LibraryInteropManager _interopManager;

        public LibraryProxyGenerator(LibraryInteropManager interopManager = null)
        {
            _interopManager = interopManager;
        }

        /// <summary>
        /// Generates a C# proxy class for a library
        /// </summary>
        public async Task<OASISResult<string>> GenerateProxyClassAsync(
            string libraryId,
            string libraryName,
            string libraryPath,
            InteropProviderType providerType,
            string namespaceName = "OASIS.LibraryProxies")
        {
            var result = new OASISResult<string>();

            try
            {
                if (_interopManager == null)
                {
                    var managerResult = await LibraryInteropFactory.CreateDefaultManagerAsync();
                    if (managerResult.IsError || managerResult.Result == null)
                    {
                        OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(managerResult, result);
                        return result;
                    }
                    var manager = managerResult.Result;
                    
                    // Load library if not already loaded
                    if (!manager.IsLibraryLoaded(libraryId))
                    {
                        var loadResult = await manager.LoadLibraryAsync(libraryPath, providerType);
                        if (loadResult.IsError || loadResult.Result == null)
                        {
                            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadResult, result);
                            return result;
                        }
                    }

                    // Get function signatures (preferred) or function names (fallback)
                    var signaturesResult = await manager.GetFunctionSignaturesAsync(libraryId);
                    List<IFunctionSignature> signatures = null;
                    List<string> functionNames = null;

                    if (!signaturesResult.IsError && signaturesResult.Result != null && signaturesResult.Result.Any())
                    {
                        signatures = signaturesResult.Result.ToList();
                    }
                    else
                    {
                        // Fallback to function names if signatures not available
                        var functionsResult = await manager.GetAvailableFunctionsAsync(libraryId);
                        if (functionsResult.IsError)
                        {
                            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(functionsResult, result);
                            return result;
                        }
                        functionNames = functionsResult.Result?.ToList() ?? new List<string>();
                    }

                    // Generate proxy class
                    var proxyCode = GenerateProxyClassCode(libraryName, libraryId, libraryPath, providerType, signatures, functionNames, namespaceName);
                    result.Result = proxyCode;
                }
                else
                {
                    // Use provided manager
                    if (!_interopManager.IsLibraryLoaded(libraryId))
                    {
                        var loadResult = await _interopManager.LoadLibraryAsync(libraryPath, providerType);
                        if (loadResult.IsError || loadResult.Result == null)
                        {
                            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadResult, result);
                            return result;
                        }
                    }

                    // Get function signatures (preferred) or function names (fallback)
                    var signaturesResult = await _interopManager.GetFunctionSignaturesAsync(libraryId);
                    List<IFunctionSignature> signatures = null;
                    List<string> functionNames = null;

                    if (!signaturesResult.IsError && signaturesResult.Result != null && signaturesResult.Result.Any())
                    {
                        signatures = signaturesResult.Result.ToList();
                    }
                    else
                    {
                        // Fallback to function names if signatures not available
                        var functionsResult = await _interopManager.GetAvailableFunctionsAsync(libraryId);
                        if (functionsResult.IsError)
                        {
                            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(functionsResult, result);
                            return result;
                        }
                        functionNames = functionsResult.Result?.ToList() ?? new List<string>();
                    }

                    var proxyCode = GenerateProxyClassCode(libraryName, libraryId, libraryPath, providerType, signatures, functionNames, namespaceName);
                    result.Result = proxyCode;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error generating proxy class: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Generates proxy class code
        /// </summary>
        private string GenerateProxyClassCode(
            string libraryName,
            string libraryId,
            string libraryPath,
            InteropProviderType providerType,
            List<IFunctionSignature> signatures = null,
            List<string> functionNames = null,
            string namespaceName = "OASIS.LibraryProxies")
        {
            var sb = new StringBuilder();
            var className = SanitizeClassName(libraryName);

            sb.AppendLine("using System;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine("using NextGenSoftware.OASIS.API.ONODE.Core.Managers.Interop;");
            sb.AppendLine("using NextGenSoftware.OASIS.API.ONODE.Core.Enums;");
            sb.AppendLine("using NextGenSoftware.OASIS.Common;");
            sb.AppendLine();
            sb.AppendLine($"namespace {namespaceName}");
            sb.AppendLine("{");
            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// Auto-generated proxy class for library: {libraryName}");
            sb.AppendLine($"    /// Provides IntelliSense support for library functions");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    public class {className}Proxy");
            sb.AppendLine("    {");
            sb.AppendLine("        private LibraryInteropManager _interopManager;");
            sb.AppendLine($"        private string _libraryId = \"{libraryId}\";");
            sb.AppendLine($"        private readonly string _libraryPath = @\"{libraryPath.Replace("\\", "\\\\")}\";");
            sb.AppendLine($"        private readonly InteropProviderType _providerType = InteropProviderType.{providerType};");
            sb.AppendLine("        private bool _isInitialized = false;");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Default constructor - automatically loads the library");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        public {className}Proxy()");
            sb.AppendLine("        {");
            sb.AppendLine("            // Library will be loaded on first use (lazy initialization)");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Advanced constructor - inject custom interop manager and library ID");
            sb.AppendLine("        /// Only use this for: unit testing (mock manager), custom provider setup, or sharing a pre-loaded library");
            sb.AppendLine("        /// For normal usage, use the default constructor instead");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        public {className}Proxy(LibraryInteropManager interopManager, string libraryId = null)");
            sb.AppendLine("        {");
            sb.AppendLine("            _interopManager = interopManager ?? throw new ArgumentNullException(nameof(interopManager));");
            sb.AppendLine("            if (!string.IsNullOrEmpty(libraryId))");
            sb.AppendLine("                _libraryId = libraryId;");
            sb.AppendLine("            _isInitialized = true;");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Ensures the library is loaded and interop manager is initialized");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        private async Task EnsureInitializedAsync()");
            sb.AppendLine("        {");
            sb.AppendLine("            if (_isInitialized)");
            sb.AppendLine("                return;");
            sb.AppendLine();
            sb.AppendLine("            var managerResult = await LibraryInteropFactory.CreateDefaultManagerAsync();");
            sb.AppendLine("            if (managerResult.IsError || managerResult.Result == null)");
            sb.AppendLine("                throw new InvalidOperationException($\"Failed to initialize interop manager: {managerResult.Message}\");");
            sb.AppendLine();
            sb.AppendLine("            _interopManager = managerResult.Result;");
            sb.AppendLine();
            // Load library if not already loaded
            sb.AppendLine("            if (!_interopManager.IsLibraryLoaded(_libraryId))");
            sb.AppendLine("            {");
            sb.AppendLine("                var loadResult = await _interopManager.LoadLibraryAsync(_libraryPath, _providerType);");
            sb.AppendLine("                if (loadResult.IsError || loadResult.Result == null)");
            sb.AppendLine("                    throw new InvalidOperationException($\"Failed to load library {_libraryPath}: {loadResult.Message}\");");
            sb.AppendLine();
            sb.AppendLine("                _libraryId = loadResult.Result.LibraryId;");
            sb.AppendLine("            }");
            sb.AppendLine();
            sb.AppendLine("            _isInitialized = true;");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Generate method stubs for each function
            if (signatures != null && signatures.Any())
            {
                // Generate strongly-typed methods from signatures
                foreach (var signature in signatures)
                {
                    GenerateTypedMethod(sb, signature);
                }
            }
            else if (functionNames != null && functionNames.Any())
            {
                // Fallback: Generate generic methods from function names
                foreach (var function in functionNames)
                {
                    GenerateGenericMethod(sb, function);
                }
            }

            // If no functions found, add a generic invoke method
            if ((signatures == null || !signatures.Any()) && (functionNames == null || functionNames.Count == 0))
            {
                sb.AppendLine("        /// <summary>");
                sb.AppendLine("        /// Generic invoke method - use when function names are known");
                sb.AppendLine("        /// </summary>");
                sb.AppendLine("        public async Task<OASISResult<object>> InvokeAsync(string functionName, params object[] parameters)");
                sb.AppendLine("        {");
                sb.AppendLine("            await EnsureInitializedAsync();");
                sb.AppendLine("            return await _interopManager.InvokeAsync(_libraryId, functionName, parameters);");
                sb.AppendLine("        }");
                sb.AppendLine();
                sb.AppendLine("        public async Task<OASISResult<T>> InvokeAsync<T>(string functionName, params object[] parameters)");
                sb.AppendLine("        {");
                sb.AppendLine("            await EnsureInitializedAsync();");
                sb.AppendLine("            return await _interopManager.InvokeAsync<T>(_libraryId, functionName, parameters);");
                sb.AppendLine("        }");
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        /// <summary>
        /// Generates a strongly-typed method from a function signature
        /// </summary>
        private void GenerateTypedMethod(StringBuilder sb, IFunctionSignature signature)
        {
            var methodName = SanitizeMethodName(signature.FunctionName);
            var returnType = signature.ReturnType ?? "object";
            var wrappedAsyncReturnType = $"Task<OASISResult<{returnType}>>";

            // Build parameter list
            var parameters = new List<string>();
            var parameterNames = new List<string>();
            foreach (var param in signature.Parameters ?? new List<IParameterInfo>())
            {
                var paramName = SanitizeParameterName(param.Name ?? $"param{parameters.Count}");
                var paramType = param.Type ?? "object";
                var paramStr = $"{paramType} {paramName}";
                
                if (param.IsOptional && param.DefaultValue != null)
                {
                    var defaultValue = GetDefaultValueString(param.DefaultValue, param.Type);
                    paramStr += $" = {defaultValue}";
                }
                else if (param.IsOptional)
                {
                    paramStr += " = null";
                }
                
                parameters.Add(paramStr);
                parameterNames.Add(paramName);
            }

            var paramList = string.Join(", ", parameters);
            var paramInvokeList = string.Join(", ", parameterNames);

            // XML documentation
            if (!string.IsNullOrEmpty(signature.Documentation))
            {
                sb.AppendLine("        /// <summary>");
                sb.AppendLine($"        /// {signature.Documentation}");
                sb.AppendLine("        /// </summary>");
                foreach (var param in signature.Parameters ?? new List<IParameterInfo>())
                {
                    if (!string.IsNullOrEmpty(param.Documentation))
                    {
                        sb.AppendLine($"        /// <param name=\"{SanitizeParameterName(param.Name ?? "param")}\">{param.Documentation}</param>");
                    }
                }
            }
            else
            {
                sb.AppendLine("        /// <summary>");
                sb.AppendLine($"        /// Invokes {signature.FunctionName} from the library");
                sb.AppendLine("        /// </summary>");
            }

            // Method signature
            sb.AppendLine($"        public async {wrappedAsyncReturnType} {methodName}Async({paramList})");
            sb.AppendLine("        {");
            sb.AppendLine("            await EnsureInitializedAsync();");
            
            // Build parameter array for invocation
            if (parameterNames.Any())
            {
                sb.AppendLine($"            var parameters = new object[] {{ {paramInvokeList} }};");
                sb.AppendLine($"            return await _interopManager.InvokeAsync<{returnType}>(_libraryId, \"{signature.FunctionName}\", parameters);");
            }
            else
            {
                sb.AppendLine($"            return await _interopManager.InvokeAsync<{returnType}>(_libraryId, \"{signature.FunctionName}\");");
            }
            
            sb.AppendLine("        }");
            sb.AppendLine();
        }

        /// <summary>
        /// Generates a generic method from a function name (fallback when signatures not available)
        /// </summary>
        private void GenerateGenericMethod(StringBuilder sb, string function)
        {
            var methodName = SanitizeMethodName(function);
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Invokes {function} from the library");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        public async Task<OASISResult<object>> {methodName}Async(params object[] parameters)");
            sb.AppendLine("        {");
            sb.AppendLine("            await EnsureInitializedAsync();");
            sb.AppendLine($"            return await _interopManager.InvokeAsync(_libraryId, \"{function}\", parameters);");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Invokes {function} from the library with typed return");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        public async Task<OASISResult<T>> {methodName}Async<T>(params object[] parameters)");
            sb.AppendLine("        {");
            sb.AppendLine("            await EnsureInitializedAsync();");
            sb.AppendLine($"            return await _interopManager.InvokeAsync<T>(_libraryId, \"{function}\", parameters);");
            sb.AppendLine("        }");
            sb.AppendLine();
        }

        /// <summary>
        /// Gets default value string for C# code generation
        /// </summary>
        private string GetDefaultValueString(object defaultValue, string type)
        {
            if (defaultValue == null)
                return "null";

            if (type == "string" || type == "String")
                return $"\"{defaultValue}\"";

            if (type == "bool" || type == "Boolean")
                return defaultValue.ToString().ToLowerInvariant();

            if (type == "int" || type == "Int32" || type == "long" || type == "Int64" || 
                type == "float" || type == "Single" || type == "double" || type == "Double" ||
                type == "decimal" || type == "Decimal")
                return defaultValue.ToString();

            return defaultValue.ToString();
        }

        /// <summary>
        /// Sanitizes a parameter name for C# code
        /// </summary>
        private string SanitizeParameterName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "param";

            // Remove invalid characters and ensure it starts with a letter
            var sanitized = System.Text.RegularExpressions.Regex.Replace(name, @"[^a-zA-Z0-9_]", "");
            if (string.IsNullOrEmpty(sanitized) || char.IsDigit(sanitized[0]))
                sanitized = "param" + sanitized;

            return sanitized;
        }

        /// <summary>
        /// Saves proxy class to file in OAPP Source folder
        /// </summary>
        public async Task<OASISResult<bool>> SaveProxyClassToOAPPAsync(
            string oappSourcePath,
            string libraryId,
            string libraryName,
            string libraryPath,
            InteropProviderType providerType)
        {
            var result = new OASISResult<bool>();

            try
            {
                // Create Source/LibraryProxies folder if it doesn't exist
                var proxiesFolder = Path.Combine(oappSourcePath, "Source", "LibraryProxies");
                if (!Directory.Exists(proxiesFolder))
                {
                    Directory.CreateDirectory(proxiesFolder);
                }

                // Generate proxy class
                var proxyResult = await GenerateProxyClassAsync(libraryId, libraryName, libraryPath, providerType);
                if (proxyResult.IsError || string.IsNullOrEmpty(proxyResult.Result))
                {
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(proxyResult, result);
                    return result;
                }

                // Save to file
                var className = SanitizeClassName(libraryName);
                var fileName = $"{className}Proxy.cs";
                var filePath = Path.Combine(proxiesFolder, fileName);

                await File.WriteAllTextAsync(filePath, proxyResult.Result);

                result.Result = true;
                result.Message = $"Proxy class saved to {filePath}";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving proxy class: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Updates OAPP Program.cs with library references and example stubs
        /// </summary>
        public async Task<OASISResult<bool>> UpdateOAPPProgramCsAsync(
            string oappSourcePath,
            List<(string LibraryName, string LibraryId, InteropProviderType ProviderType)> libraries)
        {
            var result = new OASISResult<bool>();

            try
            {
                var programCsPath = Path.Combine(oappSourcePath, "Source", "Program.cs");
                
                if (!File.Exists(programCsPath))
                {
                    // Create Program.cs if it doesn't exist
                    var programCsContent = GenerateProgramCsContent(libraries);
                    await File.WriteAllTextAsync(programCsPath, programCsContent);
                }
                else
                {
                    // Update existing Program.cs
                    var existingContent = await File.ReadAllTextAsync(programCsPath);
                    var updatedContent = UpdateProgramCsWithLibraries(existingContent, libraries);
                    await File.WriteAllTextAsync(programCsPath, updatedContent);
                }

                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error updating Program.cs: {ex.Message}", ex);
            }

            return result;
        }

        private string GenerateProgramCsContent(List<(string LibraryName, string LibraryId, InteropProviderType ProviderType)> libraries)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine("using NextGenSoftware.OASIS.API.ONODE.Core.Managers.Interop;");
            sb.AppendLine("using OASIS.LibraryProxies;");
            sb.AppendLine("using NextGenSoftware.OASIS.Common;");
            sb.AppendLine();
            sb.AppendLine("namespace OASIS.OAPP");
            sb.AppendLine("{");
            sb.AppendLine("    class Program");
            sb.AppendLine("    {");
            sb.AppendLine("        static async Task Main(string[] args)");
            sb.AppendLine("        {");
            sb.AppendLine("            // Initialize interop manager");
            sb.AppendLine("            var interopManagerResult = await LibraryInteropFactory.CreateDefaultManagerAsync();");
            sb.AppendLine("            if (interopManagerResult.IsError || interopManagerResult.Result == null)");
            sb.AppendLine("            {");
            sb.AppendLine("                Console.WriteLine($\"Error initializing interop manager: {interopManagerResult.Message}\");");
            sb.AppendLine("                return;");
            sb.AppendLine("            }");
            sb.AppendLine("            var interopManager = interopManagerResult.Result;");
            sb.AppendLine();

            // Add library proxies
            foreach (var (libraryName, libraryId, _) in libraries)
            {
                var className = SanitizeClassName(libraryName);
                sb.AppendLine($"            // Initialize {libraryName} library proxy");
                sb.AppendLine($"            var {className.ToLower()}Proxy = new {className}Proxy(interopManager);");
                sb.AppendLine();
            }

            sb.AppendLine("            // Example library calls:");
            sb.AppendLine();
            
            foreach (var (libraryName, libraryId, _) in libraries)
            {
                var className = SanitizeClassName(libraryName);
                var varName = className.ToLower();
                sb.AppendLine($"            // Example: Call {libraryName} library");
                sb.AppendLine($"            // var result = await {varName}Proxy.InvokeAsync(\"functionName\", param1, param2);");
                sb.AppendLine($"            // if (!result.IsError && result.Result != null)");
                sb.AppendLine($"            //     Console.WriteLine($\"Result: {{result.Result}}\");");
                sb.AppendLine();
            }

            sb.AppendLine("            Console.WriteLine(\"OAPP started successfully!\");");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private string UpdateProgramCsWithLibraries(string existingContent, List<(string LibraryName, string LibraryId, InteropProviderType ProviderType)> libraries)
        {
            // Simple implementation - in practice, you'd want a proper C# parser
            // For now, append library initialization code before the Main method ends
            var sb = new StringBuilder(existingContent);
            
            // Find the end of Main method and insert library code
            var mainEndIndex = existingContent.LastIndexOf("}");
            if (mainEndIndex > 0)
            {
                sb.Insert(mainEndIndex, "\n            // Library proxies initialized here\n");
                foreach (var (libraryName, libraryId, _) in libraries)
                {
                    var className = SanitizeClassName(libraryName);
                    sb.Insert(mainEndIndex, $"            var {className.ToLower()}Proxy = new {className}Proxy(interopManager);\n");
                }
            }

            return sb.ToString();
        }

        private string SanitizeClassName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "Library";

            // Remove invalid characters and make PascalCase
            var sanitized = new string(name.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
            if (sanitized.Length == 0)
                return "Library";

            // Ensure starts with letter
            if (!char.IsLetter(sanitized[0]))
                sanitized = "Lib" + sanitized;

            // Make PascalCase
            return char.ToUpper(sanitized[0]) + sanitized.Substring(1).ToLower();
        }

        private string SanitizeMethodName(string name)
        {
            return SanitizeClassName(name);
        }
    }
}

