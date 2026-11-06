using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Providers.DenoDeployOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static class CqlDenoKvReadSourceRegistration
{
    public static void AddDenoKvReadSource(this IServiceCollection services, IConfiguration configuration)
    {
        const string prefix = "CQL_DENO_KV_QUERY_";
        var database = configuration[prefix + "DATABASE_ID"];
        var address = configuration[prefix + "DATA_ENDPOINT"];
        var token = configuration[prefix + "READER_TOKEN"];
        var probe = configuration[prefix + "PROBE_ID"];
        if (database == null && address == null && token == null && probe == null) return;
        if (address is not { Length: <= 1024 } || !Uri.TryCreate(address, UriKind.Absolute, out var endpoint) ||
            endpoint.AbsoluteUri != address || !Guid.TryParseExact(database, "D", out var databaseId) || databaseId.ToString("D") != database ||
            !Guid.TryParseExact(probe, "D", out var id) || id.ToString("D") != probe ||
            !DenoDeployQuerySource.IsValidConfiguration(databaseId, endpoint, token, id))
            throw new InvalidOperationException("Deno KV CQL requires a reviewed database, canonical HTTPS data endpoint, reader token and canonical probe.");

        services.AddSingleton<CqlReadSourceProfile>(_ => new CqlReadSourceProfile(new DenoDeployQuerySource(databaseId, endpoint, token, id)));
    }
}
