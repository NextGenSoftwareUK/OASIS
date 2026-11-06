using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.CouchbaseOASIS;

/// <summary>Bind shared bounded-key algebra to this exact storage authority.</summary>
public static class CouchbaseCqlCompiler
{
    internal static string NativeIdentityStatement(string bucket) =>
        $"SELECT META(h).id AS `Key`, h.id AS `Id`, h.isDeleted AS `IsDeleted`, h.dataJson AS `DataJson` " +
        $"FROM `{bucket}`.`_default`.`holons` AS h USE KEYS $keys";

    public static CosmicQueryCapabilities Capabilities => BoundedIdentityCqlCompiler.Capabilities;

    public static OASISResult<IReadOnlyList<Guid>> Compile<T>(CosmicQuery<T> query, ProviderType authority) where T : IHolon
        => BoundedIdentityCqlCompiler.Compile(query, authority, ProviderType.CouchbaseOASIS);
}
