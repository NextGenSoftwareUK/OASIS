using System;
using System.Threading.Tasks;
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
        /// <summary>
        /// Create and register default interop providers
        /// </summary>
        public static async Task<OASISResult<LibraryInteropManager>> CreateDefaultManagerAsync()
        {
            var result = new OASISResult<LibraryInteropManager>();

            try
            {
                var manager = new LibraryInteropManager();

                // Register native P/Invoke provider (always available, best performance)
                var nativeProvider = new NativePInvokeProvider();
                var nativeResult = await manager.RegisterProviderAsync(nativeProvider);
                if (nativeResult.IsError)
                {
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(nativeResult, result);
                    return result;
                }

                // Register Python provider (if available)
                try
                {
                    var pythonProvider = new PythonInteropProvider();
                    await manager.RegisterProviderAsync(pythonProvider);
                }
                catch
                {
                    // Python.NET not available, skip
                }

                // Register JavaScript provider (if available)
                try
                {
                    var jsProvider = new JavaScriptInteropProvider();
                    await manager.RegisterProviderAsync(jsProvider);
                }
                catch
                {
                    // ClearScript not available, skip
                }

                // Register WebAssembly provider (if available)
                try
                {
                    var wasmProvider = new WebAssemblyInteropProvider();
                    await manager.RegisterProviderAsync(wasmProvider);
                }
                catch
                {
                    // Wasmtime not available, skip
                }

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
                _ => throw new NotSupportedException($"Provider type {providerType} is not yet implemented.")
            };
        }
    }
}

