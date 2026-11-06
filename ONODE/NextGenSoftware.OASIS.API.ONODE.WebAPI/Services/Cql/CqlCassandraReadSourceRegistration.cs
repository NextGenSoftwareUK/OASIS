using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Providers.CassandraOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static class CqlCassandraReadSourceRegistration
{
    private const string BundlePath = "/data/cql-secrets/cassandra/secure-connect.zip";

    public static void AddCassandraReadSource(this IServiceCollection services, IConfiguration configuration)
    {
        const string prefix = "CQL_CASSANDRA_QUERY_";
        var hash = configuration[prefix + "BUNDLE_SHA256"];
        var token = configuration[prefix + "READER_TOKEN"];
        var probe = configuration[prefix + "PROBE_ID"];
        if (hash == null && token == null && probe == null) return;
        if (hash == null || token == null || !Guid.TryParseExact(probe, "D", out var id) ||
            id == Guid.Empty || probe != id.ToString("D"))
            throw new InvalidOperationException("Cassandra CQL requires an approved bundle, restricted token and canonical probe.");

        var source = new CassandraQuerySource(Path.GetFullPath(BundlePath), hash, token, id);
        services.AddSingleton<CqlReadSourceProfile>(_ => new CqlReadSourceProfile(source));
    }
}
