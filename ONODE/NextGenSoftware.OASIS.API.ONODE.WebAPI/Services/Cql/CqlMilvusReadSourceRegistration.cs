using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Providers.MilvusOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static class CqlMilvusReadSourceRegistration
{
    public static void AddMilvusReadSource(this IServiceCollection services, IConfiguration configuration)
    {
        const string prefix = "CQL_MILVUS_QUERY_";
        var address = configuration[prefix + "ORIGIN"];
        var token = configuration[prefix + "READER_TOKEN"];
        var probe = configuration[prefix + "PROBE_ID"];
        if (address == null && token == null && probe == null) return;
        if (address is not { Length: <= 261 } || !Uri.TryCreate(address, UriKind.Absolute, out var origin) ||
            !Guid.TryParseExact(probe, "D", out var id) || id.ToString("D") != probe ||
            !MilvusQuerySource.IsValidConfiguration(origin, token, id))
            throw new InvalidOperationException("Milvus CQL requires a reviewed HTTPS origin, restricted reader and canonical public probe.");

        services.AddSingleton<CqlReadSourceProfile>(_ => new CqlReadSourceProfile(new MilvusQuerySource(origin, token, id)));
    }
}
