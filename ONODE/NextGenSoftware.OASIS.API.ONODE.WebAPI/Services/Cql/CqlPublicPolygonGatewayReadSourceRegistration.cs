using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Providers.QuickNodeOASIS;
using NextGenSoftware.OASIS.API.Providers.TenderlyOASIS;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static class CqlPublicPolygonGatewayReadSourceRegistration
{
    private static readonly string[] TeachingAddresses =
    {
        "0x0000000000000000000000000000000000000000",
        "0x000000000000000000000000000000000000dead",
        "0x0000000000000000000000000000000000001010"
    };

    public static void AddPublicPolygonGatewayReadSources(this IServiceCollection services, IConfiguration configuration)
    {
        Register(services, configuration["CQL_TENDERLY_POLYGON_QUERY_SOURCE_OWNER"], "Tenderly",
            owner => new TenderlyPublicAccountMetadataSource(TeachingAddresses, owner));
        Register(services, configuration["CQL_QUICKNODE_POLYGON_QUERY_SOURCE_OWNER"], "QuickNode",
            owner => new QuickNodePublicAccountMetadataSource(TeachingAddresses, owner));
    }

    private static void Register(IServiceCollection services, string ownerText, string gateway,
        Func<Guid, EvmPublicAccountMetadataSource> create)
    {
        if (ownerText == null) return;
        if (!Guid.TryParseExact(ownerText, "D", out var owner) || owner == Guid.Empty || owner.ToString("D") != ownerText)
            throw new InvalidOperationException($"{gateway} Polygon CQL requires a canonical teaching source owner.");

        services.AddSingleton<CqlReadSourceProfile>(_ => new CqlReadSourceProfile(create(owner)));
    }
}
