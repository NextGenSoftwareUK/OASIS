using System;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Providers.PostgreSQLOASIS;

namespace NextGenSoftware.OASIS.API.Providers.TimescaleDBOASIS;

/// <summary>Thin PostgreSQL-wire adapter; native product conformance is tracked separately.</summary>
public class TimescaleDBOASIS : PostgreSqlFamilyProviderBase
{
    protected override NextGenSoftware.OASIS.API.Core.Enums.ProviderType NativeBoundedQueryAuthority
        => NextGenSoftware.OASIS.API.Core.Enums.ProviderType.TimescaleDBOASIS;

    public TimescaleDBOASIS(string connectionString)
        : base(connectionString,NextGenSoftware.OASIS.API.Core.Enums.ProviderType.TimescaleDBOASIS,nameof(TimescaleDBOASIS)) { }
}

/// <summary>Read-only CQL activation for a dedicated TimescaleDB teaching source.</summary>
public sealed class TimescaleDbQuerySource : TimescaleDBOASIS, ICosmicReadOnlyQueryActivation
{
    private readonly Guid _probe;

    public TimescaleDbQuerySource(string connectionString, Guid probe)
        : base(RequireVerifiedQueryConnection(connectionString))
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
}
