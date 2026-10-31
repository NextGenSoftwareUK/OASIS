using System;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

/// <summary>An explicitly configured query-only representation. Its lifetime and
/// activation are independent of the canonical wallet/storage provider registry.</summary>
public sealed class CqlReadSourceProfile : IDisposable
{
    public IOASISStorageProvider Storage { get; }

    public CqlReadSourceProfile(IOASISStorageProvider storage)
    {
        if (storage?.ProviderType == null || storage is not ICosmicQueryStorageProvider query
            || query.QueryProviderType != storage.ProviderType.Value || query.QueryCapabilities == null
            || storage is not ICosmicReadOnlyQueryActivation)
            throw new ArgumentException("An explicit read-only query adapter is required.", nameof(storage));
        Storage = storage;
    }

    public void Dispose() => (Storage as IDisposable)?.Dispose();
}
