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

                    // Get available functions
                    var functionsResult = await manager.GetAvailableFunctionsAsync(libraryId);
                    if (functionsResult.IsError)
                    {
                        OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(functionsResult, result);
                        return result;
                    }

                    var functions = functionsResult.Result?.ToList() ?? new List<string>();

                    // Generate proxy class
                    var proxyCode = GenerateProxyClassCode(libraryName, libraryId, libraryPath, providerType, functions, namespaceName);
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

                    var functionsResult = await _interopManager.GetAvailableFunctionsAsync(libraryId);
                    if (functionsResult.IsError)
                    {
                        OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(functionsResult, result);
                        return result;
                    }

                    var functions = functionsResult.Result?.ToList() ?? new List<string>();
                    var proxyCode = GenerateProxyClassCode(libraryName, libraryId, libraryPath, providerType, functions, namespaceName);
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
            List<string> functions,
            string namespaceName)
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
            sb.AppendLine("        private readonly LibraryInteropManager _interopManager;");
            sb.AppendLine($"        private readonly string _libraryId = \"{libraryId}\";");
            sb.AppendLine();
            sb.AppendLine($"        public {className}Proxy(LibraryInteropManager interopManager)");
            sb.AppendLine("        {");
            sb.AppendLine("            _interopManager = interopManager ?? throw new ArgumentNullException(nameof(interopManager));");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Generate method stubs for each function
            foreach (var function in functions)
            {
                var methodName = SanitizeMethodName(function);
                sb.AppendLine("        /// <summary>");
                sb.AppendLine($"        /// Invokes {function} from the library");
                sb.AppendLine("        /// </summary>");
                sb.AppendLine($"        public async Task<OASISResult<object>> {methodName}Async(params object[] parameters)");
                sb.AppendLine("        {");
                sb.AppendLine($"            return await _interopManager.InvokeAsync(_libraryId, \"{function}\", parameters);");
                sb.AppendLine("        }");
                sb.AppendLine();
                sb.AppendLine("        /// <summary>");
                sb.AppendLine($"        /// Invokes {function} from the library with typed return");
                sb.AppendLine("        /// </summary>");
                sb.AppendLine($"        public async Task<OASISResult<T>> {methodName}Async<T>(params object[] parameters)");
                sb.AppendLine("        {");
                sb.AppendLine($"            return await _interopManager.InvokeAsync<T>(_libraryId, \"{function}\", parameters);");
                sb.AppendLine("        }");
                sb.AppendLine();
            }

            // If no functions found, add a generic invoke method
            if (functions.Count == 0)
            {
                sb.AppendLine("        /// <summary>");
                sb.AppendLine("        /// Generic invoke method - use when function names are known");
                sb.AppendLine("        /// </summary>");
                sb.AppendLine("        public async Task<OASISResult<object>> InvokeAsync(string functionName, params object[] parameters)");
                sb.AppendLine("        {");
                sb.AppendLine("            return await _interopManager.InvokeAsync(_libraryId, functionName, parameters);");
                sb.AppendLine("        }");
                sb.AppendLine();
                sb.AppendLine("        public async Task<OASISResult<T>> InvokeAsync<T>(string functionName, params object[] parameters)");
                sb.AppendLine("        {");
                sb.AppendLine("            return await _interopManager.InvokeAsync<T>(_libraryId, functionName, parameters);");
                sb.AppendLine("        }");
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
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

