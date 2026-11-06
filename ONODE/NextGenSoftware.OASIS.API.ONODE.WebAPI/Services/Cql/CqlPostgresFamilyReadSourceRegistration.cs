using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.CockroachDBOASIS;
using NextGenSoftware.OASIS.API.Providers.NeonOASIS;
using NextGenSoftware.OASIS.API.Providers.TimescaleDBOASIS;
using Npgsql;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static class CqlPostgresFamilyReadSourceRegistration
{
    public static void AddCockroachDbReadSource(this IServiceCollection services, IConfiguration configuration) =>
        AddReadSource(services, configuration, "COCKROACHDB", "oasis", 26257,
            new[] { ".cockroachlabs.cloud" }, (connection, id) => new CockroachDbQuerySource(connection, id));

    public static void AddTimescaleDbReadSource(this IServiceCollection services, IConfiguration configuration) =>
        AddReadSource(services, configuration, "TIMESCALEDB", "tsdb", null,
            new[] { ".tsdb.cloud.timescale.com", ".tsdb.cloud.tigerdata.com" },
            (connection, id) => new TimescaleDbQuerySource(connection, id));

    public static void AddNeonReadSource(this IServiceCollection services, IConfiguration configuration) =>
        AddReadSource(services, configuration, "NEON", "oasis", 5432,
            new[] { ".neon.tech" }, (connection, id) => new NeonQuerySource(connection, id),
            NeonQuerySource.IsDirectEndpoint);

    private static void AddReadSource(IServiceCollection services, IConfiguration configuration,
        string product, string database, int? fixedPort, string[] domains, Func<string, Guid, IOASISStorageProvider> create,
        Func<string, bool> admitHost = null)
    {
        var prefix = $"CQL_{product}_QUERY_";
        var host = configuration[prefix + "HOST"];
        var configuredPort = fixedPort.HasValue ? null : configuration[prefix + "PORT"];
        var password = configuration[prefix + "READER_PASSWORD"];
        var probe = configuration[prefix + "PROBE_ID"];
        if (host == null && configuredPort == null && password == null && probe == null) return;
        var port = fixedPort.GetValueOrDefault();
        if (host is not { Length: > 0 and <= 253 } ||
            !domains.Any(domain => host.EndsWith(domain, StringComparison.Ordinal)) ||
            (admitHost != null && !admitHost(host)) ||
            host.Split('.').Any(label => label.Length is < 1 or > 63 ||
                label[0] == '-' || label[^1] == '-' ||
                label.Any(character => !(char.IsAsciiLetterLower(character) || char.IsAsciiDigit(character) || character == '-'))) ||
            (!fixedPort.HasValue && (configuredPort is not { Length: > 0 and <= 5 } ||
                !configuredPort.All(char.IsAsciiDigit) || !int.TryParse(configuredPort, out port) ||
                port is < 1 or > 65535 || configuredPort != port.ToString(System.Globalization.CultureInfo.InvariantCulture))) ||
            string.IsNullOrWhiteSpace(password) || password.Length > 1024 || password.Any(char.IsControl) ||
            !Guid.TryParseExact(probe, "D", out var id) || id == Guid.Empty || probe != id.ToString("D"))
            throw new InvalidOperationException($"{product} CQL requires a managed service address, restricted reader password and canonical probe.");

        var settings = new NpgsqlConnectionStringBuilder
        {
            Host = host,
            Port = port,
            Database = database,
            Username = "cosmic_reader",
            Password = password,
            SslMode = SslMode.VerifyFull,
            Timeout = 8,
            CommandTimeout = 15,
            SearchPath = "public"
        };
        services.AddSingleton<CqlReadSourceProfile>(_ =>
            new CqlReadSourceProfile(create(settings.ConnectionString, id)));
    }
}
