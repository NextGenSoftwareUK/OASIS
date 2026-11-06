using System;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.PostgreSQLOASIS;
using NextGenSoftware.OASIS.Common;
using Npgsql;

namespace NextGenSoftware.OASIS.API.Providers.CockroachDBOASIS;

/// <summary>Thin PostgreSQL-wire adapter; native product conformance is tracked separately.</summary>
public class CockroachDBOASIS : PostgreSqlFamilyProviderBase
{
    protected override NextGenSoftware.OASIS.API.Core.Enums.ProviderType NativeBoundedQueryAuthority
        => NextGenSoftware.OASIS.API.Core.Enums.ProviderType.CockroachDBOASIS;

    public CockroachDBOASIS(string connectionString)
        : base(connectionString,NextGenSoftware.OASIS.API.Core.Enums.ProviderType.CockroachDBOASIS,nameof(CockroachDBOASIS)) { }
}

/// <summary>Read-only CQL activation for a dedicated CockroachDB teaching source.</summary>
public sealed class CockroachDbQuerySource : CockroachDBOASIS, ICosmicReadOnlyQueryActivation
{
    private readonly Guid _probe;

    public CockroachDbQuerySource(string connectionString, Guid probe)
        : base(RequireVerifiedConnection(connectionString))
    {
        if (probe == Guid.Empty) throw new ArgumentException("An existing teaching probe is required.", nameof(probe));
        _probe = probe;
    }

    protected override bool RequiresPublicQueryRecords => true;

    public override OASISResult<bool> ActivateProvider() =>
        ActivateProviderAsync().GetAwaiter().GetResult();

    public override Task<OASISResult<bool>> ActivateProviderAsync() =>
        ActivateQuerySourceAsync(default);

    public Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken) =>
        VerifyPublicQueryProbeAsync(_probe, cancellationToken);

    private static string RequireVerifiedConnection(string connectionString)
    {
        var settings = new NpgsqlConnectionStringBuilder(connectionString);
        if (settings.SslMode != SslMode.VerifyFull || string.IsNullOrWhiteSpace(settings.Host) ||
            settings.Host.Contains(',') || string.IsNullOrWhiteSpace(settings.Database) ||
            string.IsNullOrWhiteSpace(settings.Username))
            throw new ArgumentException("A single certificate-verified CockroachDB host, database and reader are required.", nameof(connectionString));
        return settings.ConnectionString;
    }
}
