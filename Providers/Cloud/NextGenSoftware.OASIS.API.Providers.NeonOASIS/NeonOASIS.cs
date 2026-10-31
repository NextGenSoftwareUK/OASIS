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
        // Neon pooled connections use the -pooler hostname, not an invented port.
        // Preserve explicit non-Neon/proxy/multi-host configuration for the caller.
        var host = config.Host;
        if (string.IsNullOrWhiteSpace(host)) throw new ArgumentException("An explicit PostgreSQL host is required.", nameof(connectionString));
        if (!host.Contains(',') && host.EndsWith(".neon.tech", StringComparison.OrdinalIgnoreCase))
        {
            config.SslMode = SslMode.VerifyFull;
            var dot = host.IndexOf('.');
            if (usePooler && !host[..dot].EndsWith("-pooler", StringComparison.OrdinalIgnoreCase))
                config.Host = host[..dot] + "-pooler" + host[dot..];
        }
        return config.ConnectionString;
    }
}
