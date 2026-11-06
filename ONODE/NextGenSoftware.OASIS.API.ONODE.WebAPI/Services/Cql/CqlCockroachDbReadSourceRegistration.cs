using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Providers.CockroachDBOASIS;
using Npgsql;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static class CqlCockroachDbReadSourceRegistration
{
    public static void AddCockroachDbReadSource(this IServiceCollection services, IConfiguration configuration)
    {
        const string prefix = "CQL_COCKROACHDB_QUERY_";
        var host = configuration[prefix + "HOST"];
        var password = configuration[prefix + "READER_PASSWORD"];
        var probe = configuration[prefix + "PROBE_ID"];
        if (host == null && password == null && probe == null) return;
        if (host is not { Length: > 0 and <= 253 } ||
            !host.EndsWith(".cockroachlabs.cloud", StringComparison.Ordinal) ||
            host.Split('.').Any(label => label.Length is < 1 or > 63 ||
                label[0] == '-' || label[^1] == '-' ||
                label.Any(character => !(char.IsAsciiLetterLower(character) || char.IsAsciiDigit(character) || character == '-'))) ||
            string.IsNullOrWhiteSpace(password) || password.Length > 1024 || password.Any(char.IsControl) ||
            !Guid.TryParseExact(probe, "D", out var id) || id == Guid.Empty || probe != id.ToString("D"))
            throw new InvalidOperationException("CockroachDB CQL requires a CockroachDB Cloud host, restricted reader password and canonical probe.");

        var settings = new NpgsqlConnectionStringBuilder
        {
            Host = host,
            Port = 26257,
            Database = "oasis",
            Username = "cosmic_reader",
            Password = password,
            SslMode = SslMode.VerifyFull,
            Timeout = 8,
            CommandTimeout = 15,
            SearchPath = "public"
        };
        services.AddSingleton<CqlReadSourceProfile>(_ =>
            new CqlReadSourceProfile(new CockroachDbQuerySource(settings.ConnectionString, id)));
    }
}
