using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Interop
{
    /// <summary>
    /// Represents a loaded library instance
    /// </summary>
    public interface ILoadedLibrary
    {
        string LibraryId { get; }
        string LibraryPath { get; }
        string LibraryName { get; }
        InteropProviderType ProviderType { get; }
        DateTime LoadedAt { get; }
        Dictionary<string, object> Metadata { get; }
    }
}

