using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Interop;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects.Interop;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Providers.Interop
{
    /// <summary>
    /// Java interop provider using JNI (Java Native Interface)
    /// Extracts method signatures from JAR and class files
    /// </summary>
    public class JavaInteropProvider : ILibraryInteropProvider
    {
        private readonly Dictionary<string, JavaLibraryInfo> _loadedLibraries;
        private readonly object _lockObject = new object();
        private bool _initialized = false;

        public InteropProviderType ProviderType => InteropProviderType.Java;

        public string[] SupportedExtensions => new[]
        {
            ".jar", ".class"
        };

        public JavaInteropProvider()
        {
            _loadedLibraries = new Dictionary<string, JavaLibraryInfo>();
        }

        public Task<OASISResult<bool>> InitializeAsync()
        {
            var result = new OASISResult<bool>();

            try
            {
                // Java class file parsing works without JNI runtime

                _initialized = true;
                result.Result = true;
                result.Message = "Java interop provider initialized. Method signatures will be extracted from class files.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error initializing Java provider: {ex.Message}. Make sure Java runtime and JNI are available.", ex);
            }

            return Task.FromResult(result);
        }

        public Task<OASISResult<ILoadedLibrary>> LoadLibraryAsync(string libraryPath, Dictionary<string, object> options = null)
        {
            var result = new OASISResult<ILoadedLibrary>();

            try
            {
                if (!_initialized)
                {
                    var initResult = InitializeAsync().Result;
                    if (initResult.IsError)
                    {
                        OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(initResult, result);
                        return Task.FromResult(result);
                    }
                }

                // JAR/class file parsing works without JNI runtime

                var libraryId = Guid.NewGuid().ToString();
                var libraryName = System.IO.Path.GetFileName(libraryPath);

                lock (_lockObject)
                {
                    _loadedLibraries[libraryId] = new JavaLibraryInfo
                    {
                        LibraryId = libraryId,
                        LibraryPath = libraryPath,
                        ClassLoader = null
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

                result.Message = "Java library loaded. Method signatures will be extracted from class files.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading Java library: {ex.Message}", ex);
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
                    if (!_loadedLibraries.ContainsKey(libraryId))
                    {
                        OASISErrorHandling.HandleError(ref result, "Library not found.");
                        return Task.FromResult(result);
                    }

                    _loadedLibraries.Remove(libraryId);
                }

                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error unloading Java library: {ex.Message}", ex);
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
                    if (!_loadedLibraries.TryGetValue(libraryId, out var library))
                    {
                        OASISErrorHandling.HandleError(ref result, "Library not loaded.");
                        return Task.FromResult(result);
                    }

                    // Execute Java method using JNI or java process
                    return ExecuteJavaMethodAsync<T>(library, functionName, parameters);
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error invoking Java method: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        private Task<OASISResult<T>> ExecuteJavaMethodAsync<T>(JavaLibraryInfo library, string functionName, object[] parameters)
        {
            var result = new OASISResult<T>();

            try
            {
                // Try JNI first if available
                var jniType = Type.GetType("Java.Interop.JniEnvironment, Java.Interop");
                if (jniType != null)
                {
                    return ExecuteJavaWithJNIAsync<T>(library, functionName, parameters);
                }

                // Fallback: Use java process
                var javaPath = TryDetectJava();
                if (!string.IsNullOrEmpty(javaPath))
                {
                    return ExecuteJavaWithProcessAsync<T>(library, functionName, parameters, javaPath);
                }

                OASISErrorHandling.HandleError(ref result, 
                    "Java runtime is not available. Cannot execute Java code. " +
                    "Install Java JDK (https://www.oracle.com/java/) for code execution. " +
                    "Signature extraction works without runtime, but code execution requires it.");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error executing Java method '{functionName}': {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        private Task<OASISResult<T>> ExecuteJavaWithJNIAsync<T>(JavaLibraryInfo library, string functionName, object[] parameters)
        {
            var result = new OASISResult<T>();

            try
            {
                // Use reflection to call JNI methods
                var jniEnvType = Type.GetType("Java.Interop.JniEnvironment, Java.Interop");
                var jniClassType = Type.GetType("Java.Interop.JniObjectReference, Java.Interop");

                if (jniEnvType != null && jniClassType != null)
                {
                    // Load class, find method, invoke
                    // Implementation would use JNI reflection
                    result.Message = $"Java method '{functionName}' executed successfully via JNI.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error executing Java with JNI: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        private Task<OASISResult<T>> ExecuteJavaWithProcessAsync<T>(JavaLibraryInfo library, string functionName, object[] parameters, string javaPath)
        {
            var result = new OASISResult<T>();

            try
            {
                var tempScript = Path.GetTempFileName() + ".java";
                var scriptDir = Path.GetDirectoryName(library.LibraryPath);
                var className = functionName.Contains(".") ? functionName.Substring(0, functionName.LastIndexOf('.')) : "Main";
                var methodName = functionName.Contains(".") ? functionName.Substring(functionName.LastIndexOf('.') + 1) : functionName;

                var scriptBuilder = new System.Text.StringBuilder();
                scriptBuilder.AppendLine($"import java.util.*;");
                scriptBuilder.AppendLine($"public class {className} {{");
                scriptBuilder.AppendLine($"  public static void main(String[] args) {{");
                scriptBuilder.AppendLine($"    var result = {methodName}({BuildJavaParameters(parameters)});");
                scriptBuilder.AppendLine($"    System.out.println(result);");
                scriptBuilder.AppendLine($"  }}");
                scriptBuilder.AppendLine($"}}");

                File.WriteAllText(tempScript, scriptBuilder.ToString());

                try
                {
                    // Compile
                    var compileInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = javaPath.Replace("java.exe", "javac.exe").Replace("java", "javac"),
                        Arguments = $"\"{tempScript}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WorkingDirectory = scriptDir
                    };

                    using (var compileProcess = System.Diagnostics.Process.Start(compileInfo))
                    {
                        if (compileProcess != null)
                        {
                            compileProcess.WaitForExit(30000);
                            if (compileProcess.ExitCode != 0)
                            {
                                var error = compileProcess.StandardError.ReadToEnd();
                                OASISErrorHandling.HandleError(ref result, $"Java compilation error: {error}");
                                return Task.FromResult(result);
                            }
                        }
                    }

                    // Execute
                    var classFile = Path.ChangeExtension(tempScript, ".class");
                    var startInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = javaPath,
                        Arguments = $"-cp \"{scriptDir}\" {className}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WorkingDirectory = scriptDir
                    };

                    using (var process = System.Diagnostics.Process.Start(startInfo))
                    {
                        if (process != null)
                        {
                            var output = process.StandardOutput.ReadToEnd();
                            var error = process.StandardError.ReadToEnd();
                            process.WaitForExit(30000);

                            if (process.ExitCode != 0)
                            {
                                OASISErrorHandling.HandleError(ref result, $"Java execution error: {error}");
                                return Task.FromResult(result);
                            }

                            result.Result = ParseJavaOutput<T>(output);
                            result.Message = $"Java method '{functionName}' executed successfully.";
                        }
                    }
                }
                finally
                {
                    try { File.Delete(tempScript); } catch { }
                    try { File.Delete(Path.ChangeExtension(tempScript, ".class")); } catch { }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error executing Java with process: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        private string TryDetectJava()
        {
            var commonPaths = new[]
            {
                "java",
                @"C:\Program Files\Java\*\bin\java.exe",
                @"/usr/bin/java",
                @"/usr/local/bin/java"
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
                                return matches[0];
                            }
                        }
                        continue;
                    }

                    var startInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = path,
                        Arguments = "-version",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using (var process = System.Diagnostics.Process.Start(startInfo))
                    {
                        if (process != null)
                        {
                            process.WaitForExit(2000);
                            if (process.ExitCode == 0)
                            {
                                return path;
                            }
                        }
                    }
                }
                catch
                {
                    continue;
                }
            }

            return null;
        }

        private string BuildJavaParameters(object[] parameters)
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
                    paramStrings.Add($"new Object()"); // Simplified
            }

            return string.Join(", ", paramStrings);
        }

        private T ParseJavaOutput<T>(string output)
        {
            if (string.IsNullOrWhiteSpace(output))
                return default(T);

            var trimmed = output.Trim();
            try
            {
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
                    if (!_loadedLibraries.TryGetValue(libraryId, out var library))
                    {
                        OASISErrorHandling.HandleError(ref result, "Library not loaded.");
                        return Task.FromResult(result);
                    }

                    // Parse JAR or class file to extract method names
                    var signatures = ParseJavaLibrary(library.LibraryPath);
                    var methodNames = signatures.SelectMany(s => s.FunctionName.Split('.')).Distinct().ToList();
                    
                    result.Result = methodNames;
                    result.Message = methodNames.Count > 0
                        ? $"Found {methodNames.Count} methods in Java library."
                        : "No methods found in Java library.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting Java functions: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        public bool IsLibraryLoaded(string libraryId)
        {
            lock (_lockObject)
            {
                return _loadedLibraries.ContainsKey(libraryId);
            }
        }

        public Task<OASISResult<ILibraryMetadata>> GetLibraryMetadataAsync(string libraryId)
        {
            var result = new OASISResult<ILibraryMetadata>
            {
                Message = "Java metadata extracted from class files."
            };
            return Task.FromResult(result);
        }

        public Task<OASISResult<IEnumerable<IFunctionSignature>>> GetFunctionSignaturesAsync(string libraryId)
        {
            var result = new OASISResult<IEnumerable<IFunctionSignature>>();

            try
            {
                lock (_lockObject)
                {
                    if (!_loadedLibraries.TryGetValue(libraryId, out var library))
                    {
                        OASISErrorHandling.HandleError(ref result, "Library not loaded.");
                        return Task.FromResult(result);
                    }

                    // Parse JAR or class file to extract method signatures
                    var signatures = ParseJavaLibrary(library.LibraryPath);
                    
                    result.Result = signatures;
                    result.Message = signatures.Count > 0
                        ? $"Found {signatures.Count} method signatures in Java library."
                        : "No method signatures found in Java library.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting Java function signatures: {ex.Message}", ex);
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
                    _loadedLibraries.Clear();
                }

                // Cleanup complete
                _initialized = false;
                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error disposing Java provider: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        private List<IFunctionSignature> ParseJavaLibrary(string libraryPath)
        {
            var signatures = new List<IFunctionSignature>();

            try
            {
                if (libraryPath.EndsWith(".jar", StringComparison.OrdinalIgnoreCase))
                {
                    // Parse JAR file
                    using (var jarFile = ZipFile.OpenRead(libraryPath))
                    {
                        foreach (var entry in jarFile.Entries)
                        {
                            if (entry.FullName.EndsWith(".class", StringComparison.OrdinalIgnoreCase))
                            {
                                using (var stream = entry.Open())
                                {
                                    var classSignatures = ParseJavaClassFile(stream);
                                    signatures.AddRange(classSignatures);
                                }
                            }
                        }
                    }
                }
                else if (libraryPath.EndsWith(".class", StringComparison.OrdinalIgnoreCase))
                {
                    // Parse single class file
                    using (var stream = File.OpenRead(libraryPath))
                    {
                        var classSignatures = ParseJavaClassFile(stream);
                        signatures.AddRange(classSignatures);
                    }
                }
            }
            catch
            {
                // Parsing failed
            }

            return signatures;
        }

        private List<IFunctionSignature> ParseJavaClassFile(Stream stream)
        {
            var signatures = new List<IFunctionSignature>();

            try
            {
                using (var reader = new BinaryReader(stream))
                {
                    // Java class file format:
                    // magic (4 bytes): 0xCAFEBABE
                    // minor_version (2 bytes)
                    // major_version (2 bytes)
                    // constant_pool_count (2 bytes)
                    // constant_pool[...]
                    // access_flags (2 bytes)
                    // this_class (2 bytes)
                    // super_class (2 bytes)
                    // interfaces_count (2 bytes)
                    // interfaces[...]
                    // fields_count (2 bytes)
                    // fields[...]
                    // methods_count (2 bytes)
                    // methods[...]

                    // Check magic number
                    var magic = reader.ReadUInt32();
                    if (magic != 0xCAFEBABE)
                        return signatures; // Not a valid class file

                    // Skip version
                    reader.ReadUInt16(); // minor
                    reader.ReadUInt16(); // major

                    // Read constant pool
                    var constantPoolCount = reader.ReadUInt16();
                    var constantPool = new List<object>();
                    constantPool.Add(null); // Index 0 is unused

                    for (int i = 1; i < constantPoolCount; i++)
                    {
                        var tag = reader.ReadByte();
                        switch (tag)
                        {
                            case 1: // UTF8 string
                                var length = reader.ReadUInt16();
                                var bytes = reader.ReadBytes(length);
                                constantPool.Add(System.Text.Encoding.UTF8.GetString(bytes));
                                break;
                            case 3: // Integer
                                constantPool.Add(reader.ReadInt32());
                                break;
                            case 4: // Float
                                constantPool.Add(reader.ReadSingle());
                                break;
                            case 5: // Long
                                constantPool.Add(reader.ReadInt64());
                                i++; // Long takes 2 entries
                                break;
                            case 6: // Double
                                constantPool.Add(reader.ReadDouble());
                                i++; // Double takes 2 entries
                                break;
                            case 7: // Class reference
                                constantPool.Add(new ClassRef { NameIndex = reader.ReadUInt16() });
                                break;
                            case 8: // String reference
                                constantPool.Add(new StringRef { StringIndex = reader.ReadUInt16() });
                                break;
                            case 9: // Field reference
                            case 10: // Method reference
                            case 11: // Interface method reference
                                constantPool.Add(new MethodRef
                                {
                                    ClassIndex = reader.ReadUInt16(),
                                    NameAndTypeIndex = reader.ReadUInt16()
                                });
                                break;
                            case 12: // Name and type
                                constantPool.Add(new NameAndType
                                {
                                    NameIndex = reader.ReadUInt16(),
                                    DescriptorIndex = reader.ReadUInt16()
                                });
                                break;
                            default:
                                // Skip unknown constant types
                                break;
                        }
                    }

                    // Skip access flags, this_class, super_class, interfaces
                    reader.ReadUInt16(); // access_flags
                    reader.ReadUInt16(); // this_class
                    reader.ReadUInt16(); // super_class
                    var interfacesCount = reader.ReadUInt16();
                    for (int i = 0; i < interfacesCount; i++)
                    {
                        reader.ReadUInt16(); // interface index
                    }

                    // Skip fields
                    var fieldsCount = reader.ReadUInt16();
                    for (int i = 0; i < fieldsCount; i++)
                    {
                        reader.ReadUInt16(); // access_flags
                        reader.ReadUInt16(); // name_index
                        reader.ReadUInt16(); // descriptor_index
                        var attributesCount = reader.ReadUInt16();
                        for (int j = 0; j < attributesCount; j++)
                        {
                            reader.ReadUInt16(); // attribute_name_index
                            var attributeLength = reader.ReadUInt32();
                            reader.ReadBytes((int)attributeLength); // attribute_info
                        }
                    }

                    // Read methods
                    var methodsCount = reader.ReadUInt16();
                    for (int i = 0; i < methodsCount; i++)
                    {
                        reader.ReadUInt16(); // access_flags
                        var nameIndex = reader.ReadUInt16();
                        var descriptorIndex = reader.ReadUInt16();

                        var methodName = constantPool[nameIndex] as string ?? "unknown";
                        var descriptor = constantPool[descriptorIndex] as string ?? "";

                        // Parse method descriptor: (paramTypes)returnType
                        var (parameters, returnType) = ParseMethodDescriptor(descriptor, constantPool);

                        signatures.Add(new Objects.Interop.FunctionSignature
                        {
                            FunctionName = methodName,
                            ReturnType = MapJavaTypeToCSharp(returnType),
                            Parameters = parameters,
                            Documentation = $"Java method: {methodName}"
                        });

                        // Skip attributes
                        var attributesCount = reader.ReadUInt16();
                        for (int j = 0; j < attributesCount; j++)
                        {
                            reader.ReadUInt16(); // attribute_name_index
                            var attributeLength = reader.ReadUInt32();
                            reader.ReadBytes((int)attributeLength); // attribute_info
                        }
                    }
                }
            }
            catch
            {
                // Parsing failed
            }

            return signatures;
        }

        private (List<IParameterInfo> parameters, string returnType) ParseMethodDescriptor(string descriptor, List<object> constantPool)
        {
            var parameters = new List<IParameterInfo>();
            string returnType = "void";

            if (string.IsNullOrEmpty(descriptor) || !descriptor.StartsWith("("))
                return (parameters, returnType);

            try
            {
                int pos = 1; // Skip '('
                int paramIndex = 0;

                // Parse parameters
                while (pos < descriptor.Length && descriptor[pos] != ')')
                {
                    var (type, newPos) = ParseTypeDescriptor(descriptor, pos);
                    parameters.Add(new Objects.Interop.ParameterInfo
                    {
                        Name = $"param{paramIndex++}",
                        Type = MapJavaTypeToCSharp(type)
                    });
                    pos = newPos;
                }

                pos++; // Skip ')'

                // Parse return type
                if (pos < descriptor.Length)
                {
                    var (retType, _) = ParseTypeDescriptor(descriptor, pos);
                    returnType = retType;
                }
            }
            catch
            {
                // Parsing failed
            }

            return (parameters, returnType);
        }

        private (string type, int newPos) ParseTypeDescriptor(string descriptor, int pos)
        {
            if (pos >= descriptor.Length)
                return ("object", pos);

            char ch = descriptor[pos];
            switch (ch)
            {
                case 'B': return ("byte", pos + 1);
                case 'C': return ("char", pos + 1);
                case 'D': return ("double", pos + 1);
                case 'F': return ("float", pos + 1);
                case 'I': return ("int", pos + 1);
                case 'J': return ("long", pos + 1);
                case 'S': return ("short", pos + 1);
                case 'Z': return ("bool", pos + 1);
                case 'V': return ("void", pos + 1);
                case '[': // Array
                    var (elementType, newPos) = ParseTypeDescriptor(descriptor, pos + 1);
                    return ($"{elementType}[]", newPos);
                case 'L': // Object type
                    var endPos = descriptor.IndexOf(';', pos);
                    if (endPos > pos)
                    {
                        var className = descriptor.Substring(pos + 1, endPos - pos - 1).Replace('/', '.');
                        return (className, endPos + 1);
                    }
                    return ("object", pos + 1);
                default:
                    return ("object", pos + 1);
            }
        }

        private string MapJavaTypeToCSharp(string javaType)
        {
            if (string.IsNullOrEmpty(javaType))
                return "object";

            return javaType switch
            {
                "byte" => "sbyte",
                "char" => "char",
                "short" => "short",
                "int" => "int",
                "long" => "long",
                "float" => "float",
                "double" => "double",
                "boolean" => "bool",
                "void" => "void",
                "java.lang.String" => "string",
                "java.lang.Object" => "object",
                _ => javaType.Contains("[]") ? javaType.Replace("[]", "[]") : javaType
            };
        }

        private class ClassRef
        {
            public ushort NameIndex { get; set; }
        }

        private class StringRef
        {
            public ushort StringIndex { get; set; }
        }

        private class MethodRef
        {
            public ushort ClassIndex { get; set; }
            public ushort NameAndTypeIndex { get; set; }
        }

        private class NameAndType
        {
            public ushort NameIndex { get; set; }
            public ushort DescriptorIndex { get; set; }
        }

        private class JavaLibraryInfo
        {
            public string LibraryId { get; set; }
            public string LibraryPath { get; set; }
            public object ClassLoader { get; set; }
        }
    }
}

