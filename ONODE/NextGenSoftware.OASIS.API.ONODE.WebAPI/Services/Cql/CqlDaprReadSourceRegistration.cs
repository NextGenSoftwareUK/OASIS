using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Providers.DaprOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static class CqlDaprReadSourceRegistration
{
    public static void AddDaprReadSource(this IServiceCollection services, IConfiguration configuration)
    {
        const string prefix = "CQL_DAPR_QUERY_";
        var store = configuration[prefix + "STORE"];
        var address = configuration[prefix + "GRPC_ENDPOINT"];
        var token = configuration[prefix + "SIDECAR_TOKEN"];
        var probe = configuration[prefix + "PROBE_ID"];
        if (store == null && address == null && token == null && probe == null) return;
        if (!Uri.TryCreate(address, UriKind.Absolute, out var endpoint) || endpoint.AbsoluteUri != address ||
            !Guid.TryParseExact(probe, "D", out var id) || id.ToString("D") != probe ||
            !DaprQuerySource.IsValidConfiguration(store, endpoint, token, id))
            throw new InvalidOperationException("Dapr CQL requires a reviewed loopback sidecar, state component, token and canonical probe.");

        services.AddSingleton<CqlReadSourceProfile>(_ => new CqlReadSourceProfile(new DaprQuerySource(store, endpoint, token, id)));
    }
}
