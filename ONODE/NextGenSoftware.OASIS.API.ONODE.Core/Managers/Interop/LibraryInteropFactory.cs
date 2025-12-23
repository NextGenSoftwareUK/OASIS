using System;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Interop;
using NextGenSoftware.OASIS.API.ONODE.Core.Providers.Interop;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers.Interop
{
    /// <summary>
    /// Factory for creating and configuring interop providers
    /// Simplifies setup and registration of interop providers
    /// </summary>
    public static class LibraryInteropFactory
    {
        private static LibraryInteropManager _defaultManager;
        private static readonly SemaphoreSlim _initSemaphore = new SemaphoreSlim(1, 1);
        private static bool _isInitialized = false;

        /// <summary>
        /// Gets or creates the default shared interop manager instance
        /// Uses singleton pattern for efficiency - all proxies share the same manager
        /// </summary>
        public static async Task<OASISResult<LibraryInteropManager>> CreateDefaultManagerAsync()
        {
            var result = new OASISResult<LibraryInteropManager>();

            try
            {
                // Initialize only once (thread-safe with semaphore)
                if (!_isInitialized)
                {
                    await _initSemaphore.WaitAsync();
                    try
                    {
                        if (!_isInitialized)
                        {
                            _defaultManager = new LibraryInteropManager();

                            // Register native P/Invoke provider (always available, best performance)
                            var nativeProvider = new NativePInvokeProvider();
                            var nativeResult = await _defaultManager.RegisterProviderAsync(nativeProvider);
                            if (nativeResult.IsError)
                            {
                                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(nativeResult, result);
                                return result;
                            }

                            // Register Python provider (if available)
                            try
                            {
                                var pythonProvider = new PythonInteropProvider();
                                await _defaultManager.RegisterProviderAsync(pythonProvider);
                            }
                            catch
                            {
                                // Python.NET not available, skip
                            }

                            // Register JavaScript provider (if available)
                            try
                            {
                                var jsProvider = new JavaScriptInteropProvider();
                                await _defaultManager.RegisterProviderAsync(jsProvider);
                            }
                            catch
                            {
                                // ClearScript not available, skip
                            }

                            // Register WebAssembly provider (if available)
                            try
                            {
                                var wasmProvider = new WebAssemblyInteropProvider();
                                await _defaultManager.RegisterProviderAsync(wasmProvider);
                            }
                            catch
                            {
                                // Wasmtime not available, skip
                            }

                            // Register Java provider (if available)
                            try
                            {
                                var javaProvider = new JavaInteropProvider();
                                await _defaultManager.RegisterProviderAsync(javaProvider);
                            }
                            catch
                            {
                                // JNI not available, skip
                            }

                            // Register REST API provider (always available)
                            try
                            {
                                var restProvider = new RestApiInteropProvider();
                                await _defaultManager.RegisterProviderAsync(restProvider);
                            }
                            catch
                            {
                                // HTTP client issues, skip
                            }

                            // Register Go provider
                            try
                            {
                                var goProvider = new GoInteropProvider();
                                await _defaultManager.RegisterProviderAsync(goProvider);
                            }
                            catch
                            {
                                // Skip if issues
                            }

                            // Register .NET provider
                            try
                            {
                                var dotNetProvider = new DotNetInteropProvider();
                                await _defaultManager.RegisterProviderAsync(dotNetProvider);
                            }
                            catch
                            {
                                // Skip if issues
                            }

                            // Register gRPC provider
                            try
                            {
                                var grpcProvider = new GrpcInteropProvider();
                                await _defaultManager.RegisterProviderAsync(grpcProvider);
                            }
                            catch
                            {
                                // Skip if issues
                            }

                            // Register additional language providers
                            try
                            {
                                var typeScriptProvider = new TypeScriptInteropProvider();
                                await _defaultManager.RegisterProviderAsync(typeScriptProvider);
                            }
                            catch { }

                            try
                            {
                                var rubyProvider = new RubyInteropProvider();
                                await _defaultManager.RegisterProviderAsync(rubyProvider);
                            }
                            catch { }

                            try
                            {
                                var phpProvider = new PhpInteropProvider();
                                await _defaultManager.RegisterProviderAsync(phpProvider);
                            }
                            catch { }

                            try
                            {
                                var luaProvider = new LuaInteropProvider();
                                await _defaultManager.RegisterProviderAsync(luaProvider);
                            }
                            catch { }

                            try
                            {
                                var perlProvider = new PerlInteropProvider();
                                await _defaultManager.RegisterProviderAsync(perlProvider);
                            }
                            catch { }

                            try
                            {
                                var kotlinProvider = new KotlinInteropProvider();
                                await _defaultManager.RegisterProviderAsync(kotlinProvider);
                            }
                            catch { }

                            try
                            {
                                var scalaProvider = new ScalaInteropProvider();
                                await _defaultManager.RegisterProviderAsync(scalaProvider);
                            }
                            catch { }

                            try
                            {
                                var groovyProvider = new GroovyInteropProvider();
                                await _defaultManager.RegisterProviderAsync(groovyProvider);
                            }
                            catch { }

                            try
                            {
                                var clojureProvider = new ClojureInteropProvider();
                                await _defaultManager.RegisterProviderAsync(clojureProvider);
                            }
                            catch { }

                            try
                            {
                                var dartProvider = new DartInteropProvider();
                                await _defaultManager.RegisterProviderAsync(dartProvider);
                            }
                            catch { }

                            try
                            {
                                var rProvider = new RInteropProvider();
                                await _defaultManager.RegisterProviderAsync(rProvider);
                            }
                            catch { }

                            try
                            {
                                var juliaProvider = new JuliaInteropProvider();
                                await _defaultManager.RegisterProviderAsync(juliaProvider);
                            }
                            catch { }

                            try
                            {
                                var shellProvider = new ShellScriptInteropProvider();
                                await _defaultManager.RegisterProviderAsync(shellProvider);
                            }
                            catch { }

                            try
                            {
                                var psProvider = new PowerShellInteropProvider();
                                await _defaultManager.RegisterProviderAsync(psProvider);
                            }
                            catch { }

                            _isInitialized = true;
                        }
                    }
                    finally
                    {
                        _initSemaphore.Release();
                    }
                }

                var manager = _defaultManager;

                result.Result = manager;
                result.Message = "Library interop manager created with default providers.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error creating interop manager: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Create a provider for a specific type
        /// </summary>
        public static ILibraryInteropProvider CreateProvider(InteropProviderType providerType)
        {
            return providerType switch
            {
                InteropProviderType.NativePInvoke => new NativePInvokeProvider(),
                InteropProviderType.Python => new PythonInteropProvider(),
                InteropProviderType.JavaScript => new JavaScriptInteropProvider(),
                InteropProviderType.WebAssembly => new WebAssemblyInteropProvider(),
                InteropProviderType.Java => new JavaInteropProvider(),
                InteropProviderType.Go => new GoInteropProvider(),
                InteropProviderType.DotNet => new DotNetInteropProvider(),
                InteropProviderType.RestApi => new RestApiInteropProvider(),
                InteropProviderType.Grpc => new GrpcInteropProvider(),
                _ => throw new NotSupportedException($"Provider type {providerType} is not yet implemented.")
            };
        }
    }
}

