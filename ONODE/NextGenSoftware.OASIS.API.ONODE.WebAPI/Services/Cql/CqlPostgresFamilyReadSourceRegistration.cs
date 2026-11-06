using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.CockroachDBOASIS;
using NextGenSoftware.OASIS.API.Providers.NeonOASIS;
using NextGenSoftware.OASIS.API.Providers.NileOASIS;
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

    public static void AddNileReadSource(this IServiceCollection services, IConfiguration configuration)
    {
        const string prefix = "CQL_NILE_QUERY_";
        var host = configuration[prefix + "HOST"];
        var database = configuration[prefix + "DATABASE"];
        var username = configuration[prefix + "READER_USERNAME"];
        var password = configuration[prefix + "READER_PASSWORD"];
        var probe = configuration[prefix + "PROBE_ID"];
        if (host == null && database == null && username == null && password == null && probe == null) return;
        if (!NileQuerySource.IsManagedHost(host) || !IsNileIdentifier(database) || !IsNileIdentifier(username) ||
            !IsReaderPassword(password) || !TryParseProbe(probe, out var id))
            throw new InvalidOperationException("Nile CQL requires a managed host, database, restricted reader and canonical probe.");

        Register(services, host, 5432, database, username, password, id, (connection, key) => new NileQuerySource(connection, key));
    }

    private static bool IsNileIdentifier(string value) =>
        value is { Length: > 0 and <= 63 } && char.IsAsciiLetterOrDigit(value[0]) &&
        value.All(character => char.IsAsciiLetterLower(character) || char.IsAsciiDigit(character) || character is '-' or '_');

    private static bool IsReaderPassword(string password) =>
        !string.IsNullOrWhiteSpace(password) && password.Length <= 1024 && !password.Any(char.IsControl);

    private static bool TryParseProbe(string probe, out Guid id) =>
        Guid.TryParseExact(probe, "D", out id) && id != Guid.Empty && probe == id.ToString("D");

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
            !IsReaderPassword(password) || !TryParseProbe(probe, out var id))
            throw new InvalidOperationException($"{product} CQL requires a managed service address, restricted reader password and canonical probe.");

        Register(services, host, port, database, "cosmic_reader", password, id, create);
    }

    private static void Register(IServiceCollection services, string host, int port, string database, string username,
        string password, Guid id, Func<string, Guid, IOASISStorageProvider> create)
    {
        var settings = new NpgsqlConnectionStringBuilder
        {
            Host = host,
            Port = port,
            Database = database,
            Username = username,
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
