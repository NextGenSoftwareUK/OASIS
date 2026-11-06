using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.DruidOASIS;
using NextGenSoftware.OASIS.API.Providers.PinotOASIS;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static class CqlProtectedSqlReadSourceRegistration
{
    public static void AddDruidReadSource(this IServiceCollection services, IConfiguration configuration) =>
        Add(services, configuration, "DRUID", (origin, id, username, password) => new DruidPublicQuerySource(origin, id, username, password));

    public static void AddPinotReadSource(this IServiceCollection services, IConfiguration configuration) =>
        Add(services, configuration, "PINOT", (origin, id, username, password) => new PinotPublicQuerySource(origin, id, username, password));

    private static void Add(IServiceCollection services, IConfiguration configuration, string provider,
        Func<Uri, Guid, string, string, IOASISStorageProvider> create)
    {
        var prefix = "CQL_" + provider + "_QUERY_";
        var address = configuration[prefix + "ORIGIN"];
        var username = configuration[prefix + "READER_USERNAME"];
        var password = configuration[prefix + "READER_PASSWORD"];
        var probe = configuration[prefix + "PROBE_ID"];
        if (address == null && username == null && password == null && probe == null) return;
        if (address is not { Length: <= 261 } || !Uri.TryCreate(address, UriKind.Absolute, out var origin) ||
            !Guid.TryParseExact(probe, "D", out var id) || id.ToString("D") != probe ||
            !HostedBasicSqlQueryReader.IsValid(origin, id, username, password))
            throw new InvalidOperationException($"{provider} CQL requires a reviewed HTTPS origin, restricted reader and canonical public probe.");

        services.AddSingleton<CqlReadSourceProfile>(_ =>
            new CqlReadSourceProfile(create(origin, id, username, password)));
    }
}
