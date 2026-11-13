using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.GarnetOASIS;

public partial class GarnetOASIS
{
    // Reuse the encrypted transport executor, with a distinct storage authority.
    protected override ProviderType CqlAuthority => Core.Enums.ProviderType.GarnetOASIS;

    protected override OASISResult<IReadOnlyList<Guid>> CompileCqlSelection<T>(CosmicQuery<T> query)
        => GarnetCqlCompiler.Compile(query, QueryProviderType);
}

/// <summary>Bounded identity selection for Garnet; native admission is a separate gate.</summary>
public static class GarnetCqlCompiler
{
    public static OASISResult<IReadOnlyList<Guid>> Compile<T>(CosmicQuery<T> query, ProviderType authority)
        where T : IHolon
        => BoundedIdentityCqlCompiler.Compile(query, authority, ProviderType.GarnetOASIS);
}
