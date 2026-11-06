using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.DiscordOASIS;
using NextGenSoftware.OASIS.API.Providers.LivepeerOASIS;
using NextGenSoftware.OASIS.API.Providers.PinataOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static class CqlReviewedNetworkReadSourceRegistration
{
    public static void AddReviewedNetworkReadSources(this IServiceCollection services, IConfiguration configuration)
    {
        Register(services, configuration["CQL_DISCORD_PUBLIC_GUILD_QUERY_SOURCE_OWNER"], "Discord",
            DiscordPublicGuildQuerySource.ForReviewedTeachingCatalogue);
        Register(services, configuration["CQL_LIVEPEER_ORCHESTRATOR_QUERY_SOURCE_OWNER"], "Livepeer",
            LivepeerOrchestratorQuerySource.ForReviewedTeachingCatalogue);
        Register(services, configuration["CQL_PINATA_PUBLIC_BLOCK_QUERY_SOURCE_OWNER"], "Pinata",
            owner => new PinataPublicBlockQuerySource(owner));
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
