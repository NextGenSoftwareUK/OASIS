using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Providers.AppwriteOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static class CqlAppwriteReadSourceRegistration
{
    public static void AddAppwriteReadSource(this IServiceCollection services, IConfiguration configuration)
    {
        const string prefix = "CQL_APPWRITE_QUERY_";
        var address = configuration[prefix + "ENDPOINT"];
        var project = configuration[prefix + "PROJECT_ID"];
        var database = configuration[prefix + "DATABASE_ID"];
        var token = configuration[prefix + "READER_TOKEN"];
        var probe = configuration[prefix + "PROBE_ID"];
        if (address == null && project == null && database == null && token == null && probe == null) return;
        if (address is not { Length: <= 264 } || !Uri.TryCreate(address, UriKind.Absolute, out var endpoint) ||
            endpoint.AbsoluteUri != address || !endpoint.Host.Contains('.') || endpoint.AbsolutePath != "/v1" ||
            !Guid.TryParseExact(probe, "D", out var id) || id.ToString("D") != probe ||
            !AppwriteQuerySource.IsValidConfiguration(endpoint, project, database, token, id))
            throw new InvalidOperationException("Appwrite CQL requires a reviewed HTTPS endpoint, project, database, reader token and canonical probe.");

        services.AddSingleton<CqlReadSourceProfile>(_ => new CqlReadSourceProfile(new AppwriteQuerySource(endpoint, project, database, token, id)));
    }
}
