using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.ValKeyOASIS;

public partial class ValKeyOASIS
{
    // Reuse the encrypted transport executor, with a distinct storage authority.
    protected override ProviderType CqlAuthority => Core.Enums.ProviderType.ValKeyOASIS;

    protected override OASISResult<IReadOnlyList<Guid>> CompileCqlSelection<T>(CosmicQuery<T> query)
        => ValKeyCqlCompiler.Compile(query, QueryProviderType);
}

/// <summary>Bounded identity selection for Valkey; native admission is a separate gate.</summary>
public static class ValKeyCqlCompiler
{
    public static OASISResult<IReadOnlyList<Guid>> Compile<T>(CosmicQuery<T> query, ProviderType authority)
        where T : IHolon
        => BoundedIdentityCqlCompiler.Compile(query, authority, ProviderType.ValKeyOASIS);
}
