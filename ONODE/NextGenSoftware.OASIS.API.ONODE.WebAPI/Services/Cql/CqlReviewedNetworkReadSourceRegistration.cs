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
using NextGenSoftware.OASIS.API.Providers.GOMapOASIS;
using NextGenSoftware.OASIS.API.Providers.TRONOASIS;
using NextGenSoftware.OASIS.API.Providers.TheSandboxOASIS;
using NextGenSoftware.OASIS.API.Providers.PolygonIDOASIS;
using NextGenSoftware.OASIS.API.Providers.SEEDSOASIS;
using NextGenSoftware.OASIS.API.Providers.IntelOpenVINOOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static class CqlReviewedNetworkReadSourceRegistration
{
    public static void AddReviewedNetworkReadSources(this IServiceCollection services, IConfiguration configuration, bool sandboxEnabled = false)
    {
        var tron = configuration["CQL_TRON_PUBLIC_HEADER_QUERY_SOURCE_JSON"];
        var tronKey = configuration["COSMIC_TRON_PUBLIC_READER_KEY"];
        if (tron != null || tronKey != null)
        {
            if (!sandboxEnabled || string.IsNullOrWhiteSpace(tron) || string.IsNullOrWhiteSpace(tronKey))
                throw new InvalidOperationException("TRON public header queries require sandbox mode, an explicit catalogue and a separate dedicated reader key.");
            services.AddSingleton<CqlReadSourceProfile>(_ => new CqlReadSourceProfile(
                TronPublicHeaderMetadataSource.FromAuthenticatedJson(tron, tronKey)));
        }

        var ronin = configuration["CQL_RONIN_PUBLIC_SOURCE_JSON"];
        if (ronin != null)
        {
            if (!sandboxEnabled || string.IsNullOrWhiteSpace(ronin))
                throw new InvalidOperationException("Ronin public account queries require sandbox mode and an explicit catalogue.");
            services.AddSingleton<CqlReadSourceProfile>(_ => new CqlReadSourceProfile(
                Providers.RoninOASIS.RoninPublicAccountMetadataSource.FromJson(ronin)));
        }

        var immutable = configuration["CQL_IMMUTABLE_ZKEVM_PUBLIC_SOURCE_JSON"];
        if (immutable != null)
        {
            if (!sandboxEnabled || string.IsNullOrWhiteSpace(immutable))
                throw new InvalidOperationException("Immutable zkEVM public account queries require sandbox mode and an explicit catalogue.");
            services.AddSingleton<CqlReadSourceProfile>(_ => new CqlReadSourceProfile(
                Providers.ImmutableZkEVMOASIS.ImmutableZkEVMPublicAccountMetadataSource.FromJson(immutable)));
        }

        var flow = configuration["CQL_FLOW_PUBLIC_SOURCE_JSON"];
        if (flow != null)
        {
            if (!sandboxEnabled || string.IsNullOrWhiteSpace(flow))
                throw new InvalidOperationException("Flow sealed header queries require sandbox mode and an explicit catalogue.");
            services.AddSingleton<CqlReadSourceProfile>(_ => new CqlReadSourceProfile(
                Providers.FlowOASIS.FlowSealedHeaderQuerySource.FromJson(flow)));
        }

        var eas = configuration["CQL_EAS_PUBLIC_SOURCE_JSON"];
        if (eas != null)
        {
            if (!sandboxEnabled || string.IsNullOrWhiteSpace(eas))
                throw new InvalidOperationException("EAS attestation metadata queries require sandbox mode and an explicit catalogue.");
            services.AddSingleton<CqlReadSourceProfile>(_ => new CqlReadSourceProfile(
                Providers.EASOASIS.EasAttestationQuerySource.FromJson(eas)));
        }

        var sign = configuration["CQL_SIGN_PUBLIC_SOURCE_JSON"];
        if (sign != null)
        {
            if (!sandboxEnabled || string.IsNullOrWhiteSpace(sign))
                throw new InvalidOperationException("Sign attestation metadata queries require sandbox mode and an explicit catalogue.");
            services.AddSingleton<CqlReadSourceProfile>(_ => new CqlReadSourceProfile(
                Providers.SignProtocolOASIS.SignAttestationQuerySource.FromJson(sign)));
        }

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
        var goMapOwner = configuration["CQL_GOMAP_ROUTE_QUERY_SOURCE_OWNER"];
        if (goMapOwner != null)
        {
            var originText = configuration["CQL_GOMAP_ROUTE_QUERY_SOURCE_ORIGIN"];
            if (!Uri.TryCreate(originText, UriKind.Absolute, out var origin) || origin.Scheme != Uri.UriSchemeHttps ||
                origin.UserInfo.Length != 0 || origin.AbsolutePath != "/" || origin.Query.Length != 0 ||
                origin.Fragment.Length != 0 || origin.IdnHost.TrimEnd('.').ToLowerInvariant() is
                    "router.project-osrm.org" or "routing.openstreetmap.de")
                throw new InvalidOperationException("Hosted GOMap CQL requires a dedicated HTTPS routing origin.");

            Register(services, goMapOwner, "Fixed public GOMap route",
                owner => GOMapRouteQuerySource.ForHostedTeachingCatalogue(owner, origin));
        }
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
