using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.DecentralandOASIS;
using NextGenSoftware.OASIS.API.Providers.PushProtocolOASIS;
using NextGenSoftware.OASIS.API.Providers.LayerZeroOASIS;
using NextGenSoftware.OASIS.API.Providers.ReclaimProtocolOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static class CqlReviewedNetworkReadSourceRegistration
{
    public static void AddReviewedNetworkReadSources(this IServiceCollection services, IConfiguration configuration)
    {
        Register(services, configuration["CQL_DECENTRALAND_SCENE_QUERY_SOURCE_OWNER"], "Decentraland",
            owner => new DecentralandSceneQuerySource(owner));
        Register(services, configuration["CQL_PUSH_PUBLIC_CHANNEL_QUERY_SOURCE_OWNER"], "Push Protocol",
            owner => new PushProtocolPublicChannelQuerySource(owner));
        Register(services, configuration["CQL_LAYERZERO_V1_ENDPOINT_QUERY_SOURCE_OWNER"], "LayerZero v1",
            owner => new LayerZeroV1EndpointQuerySource(owner));
        Register(services, configuration["CQL_RECLAIM_VERIFIER_QUERY_SOURCE_OWNER"], "Reclaim",
            owner => new ReclaimVerifierQuerySource(owner));
    }

    private static void Register(IServiceCollection services, string ownerText, string provider,
        Func<Guid, IOASISStorageProvider> create)
    {
        if (ownerText == null) return;
        if (!Guid.TryParseExact(ownerText, "D", out var owner) || owner == Guid.Empty || owner.ToString("D") != ownerText)
            throw new InvalidOperationException($"{provider} CQL requires a canonical teaching source owner.");

        services.AddSingleton<CqlReadSourceProfile>(_ => new CqlReadSourceProfile(create(owner)));
    }
}
