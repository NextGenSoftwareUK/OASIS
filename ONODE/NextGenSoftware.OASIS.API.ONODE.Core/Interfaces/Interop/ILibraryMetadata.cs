using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Interop
{
    /// <summary>
    /// Metadata about a library
    /// </summary>
    public interface ILibraryMetadata
    {
        string LibraryName { get; }
        string Version { get; }
        string Description { get; }
        string Author { get; }
        string Language { get; }
        string Framework { get; }
        IEnumerable<string> AvailableFunctions { get; }
        Dictionary<string, object> CustomProperties { get; }
    }
}

