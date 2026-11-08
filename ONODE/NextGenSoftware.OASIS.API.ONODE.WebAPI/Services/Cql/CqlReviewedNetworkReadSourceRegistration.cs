using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.DecentralandOASIS;
using NextGenSoftware.OASIS.API.Providers.DiscordOASIS;
using NextGenSoftware.OASIS.API.Providers.LivepeerOASIS;
using NextGenSoftware.OASIS.API.Providers.PinataOASIS;
using NextGenSoftware.OASIS.API.Providers.PushProtocolOASIS;
using NextGenSoftware.OASIS.API.Providers.LayerZeroOASIS;
using NextGenSoftware.OASIS.API.Providers.ReclaimProtocolOASIS;
using NextGenSoftware.OASIS.API.Providers.WorldIDOASIS;
using NextGenSoftware.OASIS.API.Providers.WakuOASIS;
using NextGenSoftware.OASIS.API.Providers.TheGraphOASIS;
using NextGenSoftware.OASIS.API.Providers.SubqueryOASIS;
using NextGenSoftware.OASIS.API.Providers.SelfProtocolOASIS;
using NextGenSoftware.OASIS.API.Providers.HolonymOASIS;
using NextGenSoftware.OASIS.API.Providers.CivicOASIS;
using NextGenSoftware.OASIS.API.Providers.MapLibreOASIS;
using NextGenSoftware.OASIS.API.Providers.TheSandboxOASIS;
using NextGenSoftware.OASIS.API.Providers.PolygonIDOASIS;
using NextGenSoftware.OASIS.API.Providers.SEEDSOASIS;
using NextGenSoftware.OASIS.API.Providers.IntelOpenVINOOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static class CqlReviewedNetworkReadSourceRegistration
{
    public static void AddReviewedNetworkReadSources(this IServiceCollection services, IConfiguration configuration, bool sandboxEnabled = false)
    {
        var waku = configuration["CQL_WAKU_STORE_QUERY_SOURCE_JSON"];
        if (sandboxEnabled && waku != null)
            services.AddSingleton<CqlReadSourceProfile>(_ =>
                new CqlReadSourceProfile(WakuStoreQueryConfiguration.CreateSource(waku)));

        Register(services, configuration["CQL_DISCORD_PUBLIC_GUILD_QUERY_SOURCE_OWNER"], "Discord",
            DiscordPublicGuildQuerySource.ForReviewedTeachingCatalogue);
        Register(services, configuration["CQL_LIVEPEER_ORCHESTRATOR_QUERY_SOURCE_OWNER"], "Livepeer",
            LivepeerOrchestratorQuerySource.ForReviewedTeachingCatalogue);
        Register(services, configuration["CQL_PINATA_PUBLIC_BLOCK_QUERY_SOURCE_OWNER"], "Pinata",
            owner => new PinataPublicBlockQuerySource(owner));
        Register(services, configuration["CQL_PUSH_PUBLIC_CHANNEL_QUERY_SOURCE_OWNER"], "Push Protocol",
            owner => new PushProtocolPublicChannelQuerySource(owner));
        Register(services, configuration["CQL_LAYERZERO_V1_ENDPOINT_QUERY_SOURCE_OWNER"], "LayerZero v1",
            owner => new LayerZeroV1EndpointQuerySource(owner));
        Register(services, configuration["CQL_WORLD_ID_PROTOCOL_QUERY_SOURCE_OWNER"], "World ID v4",
            owner => new WorldIdProtocolQuerySource(owner));
        Register(services, configuration["CQL_RECLAIM_VERIFIER_QUERY_SOURCE_OWNER"], "Reclaim",
            owner => new ReclaimVerifierQuerySource(owner));
        Register(services, configuration["CQL_THE_GRAPH_STUDIO_QUERY_SOURCE_OWNER"], "The Graph Studio",
            owner => new TheGraphStudioStatusQuerySource(owner));
        Register(services, configuration["CQL_OPENVINO_MODEL_QUERY_SOURCE_OWNER"], "Owned OpenVINO model version",
            OpenVinoVersionQuerySource.ForReviewedTeachingCatalogue);
        Register(services, configuration["CQL_SEEDS_SETTINGS_QUERY_SOURCE_OWNER"], "SEEDS public setting",
            SeedsSettingsQuerySource.ForReviewedTeachingCatalogue);
        Register(services, configuration["CQL_POLYGON_ID_SCHEMA_QUERY_SOURCE_OWNER"], "Polygon ID public schema",
            PolygonIdSchemaQuerySource.ForReviewedTeachingCatalogue);
        Register(services, configuration["CQL_SANDBOX_CONTRACT_QUERY_SOURCE_OWNER"], "Sandbox public contract",
            SandboxContractQuerySource.ForReviewedTeachingCatalogue);
        Register(services, configuration["CQL_MAPLIBRE_STYLE_QUERY_SOURCE_OWNER"], "MapLibre demo style",
            MapLibreStyleQuerySource.ForReviewedTeachingCatalogue);
        Register(services, configuration["CQL_CIVIC_NETWORK_QUERY_SOURCE_OWNER"], "Civic public network",
            CivicNetworkQuerySource.ForReviewedTeachingCatalogue);
        Register(services, configuration["CQL_HOLONYM_ATTESTATION_QUERY_SOURCE_OWNER"], "Human ID attestation index",
            HolonymAttestationQuerySource.ForReviewedTeachingCatalogue);
        Register(services, configuration["CQL_SELF_PROTOCOL_REGISTRY_QUERY_SOURCE_OWNER"], "Self Protocol registry",
            SelfProtocolRegistryQuerySource.ForReviewedTeachingCatalogue);
        Register(services, configuration["CQL_SUBQUERY_DICTIONARY_QUERY_SOURCE_OWNER"], "SubQuery dictionary",
            SubqueryDictionaryBlockQuerySource.ForReviewedTeachingCatalogue);
        Register(services, configuration["CQL_DECENTRALAND_SCENE_QUERY_SOURCE_OWNER"], "Decentraland",
            owner => new DecentralandSceneQuerySource(owner));
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
