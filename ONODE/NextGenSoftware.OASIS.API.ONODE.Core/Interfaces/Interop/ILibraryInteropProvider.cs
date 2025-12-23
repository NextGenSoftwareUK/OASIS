using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Interop
{
    /// <summary>
    /// Unified interface for all interop library providers
    /// Enables any library from any language/framework to be used
    /// </summary>
    public interface ILibraryInteropProvider
    {
        /// <summary>
        /// Provider type (Native, Python, JavaScript, WASM, etc.)
        /// </summary>
        InteropProviderType ProviderType { get; }

        /// <summary>
        /// Supported library types/extensions
        /// </summary>
        string[] SupportedExtensions { get; }

        /// <summary>
        /// Initialize the provider
        /// </summary>
        Task<OASISResult<bool>> InitializeAsync();

        /// <summary>
        /// Load a library from file path
        /// </summary>
        Task<OASISResult<ILoadedLibrary>> LoadLibraryAsync(string libraryPath, Dictionary<string, object> options = null);

        /// <summary>
        /// Unload a library
        /// </summary>
        Task<OASISResult<bool>> UnloadLibraryAsync(string libraryId);

        /// <summary>
        /// Invoke a function/method in the library
        /// </summary>
        Task<OASISResult<T>> InvokeAsync<T>(string libraryId, string functionName, params object[] parameters);

        /// <summary>
        /// Invoke a function/method with dynamic return type
        /// </summary>
        Task<OASISResult<object>> InvokeAsync(string libraryId, string functionName, params object[] parameters);

        /// <summary>
        /// Get available functions/methods in the library
        /// </summary>
        Task<OASISResult<IEnumerable<string>>> GetAvailableFunctionsAsync(string libraryId);

        /// <summary>
        /// Check if library is loaded
        /// </summary>
        bool IsLibraryLoaded(string libraryId);

        /// <summary>
        /// Get library metadata
        /// </summary>
        Task<OASISResult<ILibraryMetadata>> GetLibraryMetadataAsync(string libraryId);

        /// <summary>
        /// Get function signatures for a library (parameter types, return types)
        /// Used for generating strongly-typed proxy methods
        /// </summary>
        Task<OASISResult<IEnumerable<IFunctionSignature>>> GetFunctionSignaturesAsync(string libraryId);

        /// <summary>
        /// Dispose/cleanup resources
        /// </summary>
        Task<OASISResult<bool>> DisposeAsync();
    }
}

