using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Providers.DruidOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static class CqlDruidReadSourceRegistration
{
    public static void AddDruidReadSource(this IServiceCollection services, IConfiguration configuration)
    {
        const string prefix = "CQL_DRUID_QUERY_";
        var address = configuration[prefix + "ORIGIN"];
        var username = configuration[prefix + "READER_USERNAME"];
        var password = configuration[prefix + "READER_PASSWORD"];
        var probe = configuration[prefix + "PROBE_ID"];
        if (address == null && username == null && password == null && probe == null) return;
        if (address is not { Length: <= 261 } || !Uri.TryCreate(address, UriKind.Absolute, out var origin) ||
            !Guid.TryParseExact(probe, "D", out var id) || id.ToString("D") != probe ||
            !DruidPublicQuerySource.IsValidHostedConfiguration(origin, id, username, password))
            throw new InvalidOperationException("Druid CQL requires a reviewed HTTPS origin, restricted reader and canonical public probe.");

        services.AddSingleton<CqlReadSourceProfile>(_ =>
            new CqlReadSourceProfile(new DruidPublicQuerySource(origin, id, username, password)));
    }
}
