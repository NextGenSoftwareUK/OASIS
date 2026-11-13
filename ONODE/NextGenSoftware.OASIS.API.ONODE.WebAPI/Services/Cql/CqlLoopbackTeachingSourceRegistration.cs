using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Providers.GUNOASIS;
using NextGenSoftware.OASIS.API.Providers.OrbitDBOASIS;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static class CqlLoopbackTeachingSourceRegistration
{
    public static void AddLoopbackTeachingSources(this IServiceCollection services, IConfiguration configuration)
    {
        Add(services, configuration, "CQL_ORBITDB_QUERY_", "OrbitDB", (site, reader, probe) => new OrbitDbQuerySource(site, reader, probe));
        Add(services, configuration, "CQL_GUN_QUERY_", "GUN", (site, reader, probe) => new GunQuerySource(site, reader, probe));
    }

    private static void Add(IServiceCollection services, IConfiguration configuration, string prefix, string name,
        Func<Uri, string, Guid, LoopbackPublicHolonQuerySource> create)
    {
        var address = configuration[prefix + "SIDECAR"];
        var token = configuration[prefix + "READER_TOKEN"];
        var probe = configuration[prefix + "PROBE_ID"];
        if (address == null && token == null && probe == null) return;
        if (address is not { Length: <= 24 } || !Uri.TryCreate(address, UriKind.Absolute, out var sidecar) ||
            !Guid.TryParseExact(probe, "D", out var id) || id.ToString("D") != probe ||
            !LoopbackPublicHolonQuerySource.IsValidConfiguration(sidecar, token, id))
            throw new InvalidOperationException($"{name} CQL requires a reviewed loopback source, reader token and canonical probe.");

        services.AddSingleton<CqlReadSourceProfile>(_ => new CqlReadSourceProfile(create(sidecar, token, id)));
    }
}
