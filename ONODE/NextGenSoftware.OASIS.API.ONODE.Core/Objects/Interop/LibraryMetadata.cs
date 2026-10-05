using System;
using System.Collections.Generic;
using System.Linq;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Interop;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Objects.Interop
{
    /// <summary>
    /// Implementation of library metadata
    /// </summary>
    public class LibraryMetadata : ILibraryMetadata
    {
        public string LibraryName { get; set; } = string.Empty;
        public string Version { get; set; } = "1.0.0";
        public string Description { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string Framework { get; set; } = string.Empty;
        public IEnumerable<string> AvailableFunctions { get; set; } = new List<string>();
        public IEnumerable<IFunctionSignature> FunctionSignatures { get; set; } = new List<IFunctionSignature>();
        public Dictionary<string, object> CustomProperties { get; set; } = new Dictionary<string, object>();

        public LibraryMetadata()
        {
        }

        public LibraryMetadata(string libraryName)
        {
            LibraryName = libraryName;
        }

        /// <summary>
        /// Gets function signature by name
        /// </summary>
        public IFunctionSignature GetFunctionSignature(string functionName)
        {
            return FunctionSignatures?.FirstOrDefault(f => 
                f.FunctionName.Equals(functionName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Adds a function signature
        /// </summary>
        public void AddFunctionSignature(IFunctionSignature signature)
        {
            if (FunctionSignatures == null)
                FunctionSignatures = new List<IFunctionSignature>();

            var list = FunctionSignatures.ToList();
            list.Add(signature);
            FunctionSignatures = list;
        }
    }

    /// <summary>
    /// Implementation of loaded library
    /// </summary>
    public class LoadedLibrary : ILoadedLibrary
    {
        public string LibraryId { get; set; }
        public string LibraryPath { get; set; }
        public string LibraryName { get; set; }
        public InteropProviderType ProviderType { get; set; }
        public DateTime LoadedAt { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
    }
}

