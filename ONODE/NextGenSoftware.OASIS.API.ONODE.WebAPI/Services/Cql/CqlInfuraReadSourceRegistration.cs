using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Providers.InfuraOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static class CqlInfuraReadSourceRegistration
{
    private static readonly string[] TeachingAddresses =
    {
        "0x0000000000000000000000000000000000000000",
        "0x000000000000000000000000000000000000dead",
        "0x00000000219ab540356cbb839cbe05303d7705fa"
    };

    public static void AddInfuraReadSource(this IServiceCollection services, IConfiguration configuration)
    {
        var ownerText = configuration["CQL_INFURA_QUERY_SOURCE_OWNER"];
        var key = configuration["CQL_INFURA_API_KEY"];
        var secret = configuration["CQL_INFURA_API_SECRET"];
        if (ownerText is null && key is null && secret is null)
            return;

        if (!Guid.TryParseExact(ownerText, "D", out var owner) || owner == Guid.Empty ||
            owner.ToString("D") != ownerText || string.IsNullOrEmpty(key) || string.IsNullOrEmpty(secret))
            throw new InvalidOperationException("Infura CQL requires a canonical source owner and private server credentials.");

        services.AddSingleton<CqlReadSourceProfile>(_ =>
            new CqlReadSourceProfile(InfuraPublicAccountMetadataSource.Create(key, secret, TeachingAddresses, owner)));
    }
}
