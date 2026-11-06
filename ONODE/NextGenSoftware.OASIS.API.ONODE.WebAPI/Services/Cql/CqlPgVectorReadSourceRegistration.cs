using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Providers.PgVectorOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static class CqlPgVectorReadSourceRegistration
{
    public static void AddPgVectorReadSource(this IServiceCollection services, IConfiguration configuration)
    {
        const string prefix = "CQL_PGVECTOR_QUERY_";
        var host = configuration[prefix + "HOST"];
        var database = configuration[prefix + "DATABASE"];
        var user = configuration[prefix + "READER_USERNAME"];
        var password = configuration[prefix + "READER_PASSWORD"];
        var probe = configuration[prefix + "PROBE_ID"];
        if (host == null && database == null && user == null && password == null && probe == null) return;
        if (!Guid.TryParseExact(probe, "D", out var id) || id.ToString("D") != probe ||
            !PgVectorQuerySource.IsValidConfiguration(host, database, user, password, id))
            throw new InvalidOperationException("pgvector CQL requires a reviewed host, database, restricted reader and canonical probe.");

        services.AddSingleton<CqlReadSourceProfile>(_ =>
            new CqlReadSourceProfile(new PgVectorQuerySource(host, database, user, password, id)));
    }
}
