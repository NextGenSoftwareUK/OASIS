using System;
using Npgsql;
using NextGenSoftware.OASIS.API.Providers.PostgreSQLOASIS;

namespace NextGenSoftware.OASIS.API.Providers.NeonOASIS;

/// <summary>Thin Neon adapter over the canonical Npgsql PostgreSQL storage family.
/// Hosted Neon acceptance and schema compatibility require their own native run.</summary>
public class NeonOASIS : PostgreSqlFamilyProviderBase
{
    public NeonOASIS(string connectionString, bool usePooler = true)
        : base(PrepareConnectionString(connectionString, usePooler),
            NextGenSoftware.OASIS.API.Core.Enums.ProviderType.NeonOASIS, nameof(NeonOASIS)) { }

    private static string PrepareConnectionString(string connectionString, bool usePooler)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        var config = new NpgsqlConnectionStringBuilder(connectionString);
        if (string.IsNullOrWhiteSpace(config.Host))
            throw new ArgumentException("An explicit PostgreSQL host is required.", nameof(connectionString));

        var endpoints = config.Host.Split(',', StringSplitOptions.TrimEntries);
        foreach (var endpoint in endpoints)
        {
            var portSeparator = endpoint.IndexOf(':');
            var hostname = portSeparator < 0 ? endpoint : endpoint[..portSeparator];
            if (!hostname.TrimEnd('.').EndsWith(".neon.tech", StringComparison.OrdinalIgnoreCase))
                continue;

            // Npgsql applies this mode to every host in the connection string.
            config.SslMode = SslMode.VerifyFull;
            if (endpoints.Length == 1 && usePooler)
            {
                var dot = hostname.IndexOf('.');
                if (!hostname[..dot].EndsWith("-pooler", StringComparison.OrdinalIgnoreCase))
                    config.Host = endpoint.Insert(dot, "-pooler");
            }
        }

        return config.ConnectionString;
    }
}
