using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using Npgsql;

namespace NextGenSoftware.OASIS.API.Providers.NileOASIS;

/// <summary>Bounded public teaching reads from one Nile shared-table database.</summary>
public sealed class NileQuerySource : NileOASIS, ICosmicReadOnlyQueryActivation
{
    private readonly Guid probeId;

    public NileQuerySource(string connectionString, Guid probeId)
        : base(RequireNileReader(connectionString))
    {
        if (probeId == Guid.Empty)
            throw new ArgumentException("An existing teaching probe is required.", nameof(probeId));
        this.probeId = probeId;
    }

    protected override ProviderType NativeBoundedQueryAuthority => Core.Enums.ProviderType.NileOASIS;
    protected override bool RequiresPublicQueryRecords => true;

    public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().GetAwaiter().GetResult();
    public override Task<OASISResult<bool>> ActivateProviderAsync() => ActivateQuerySourceAsync(default);
    public Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken) =>
        VerifyPublicQueryProbeAsync(probeId, cancellationToken);

    private static string RequireNileReader(string connectionString)
    {
        var settings = new NpgsqlConnectionStringBuilder(RequireVerifiedQueryConnection(connectionString));
        if (!IsManagedHost(settings.Host) || settings.Port != 5432 || string.IsNullOrEmpty(settings.Password))
            throw new ArgumentException("Use one managed Nile database address and dedicated reader.", nameof(connectionString));

        return new NpgsqlConnectionStringBuilder
        {
            Host = settings.Host,
            Port = settings.Port,
            Database = settings.Database,
            Username = settings.Username,
            Password = settings.Password,
            SslMode = SslMode.VerifyFull,
            SearchPath = "public",
            Timeout = 8,
            CommandTimeout = 15
        }.ConnectionString;
    }

    public static bool IsManagedHost(string? host)
    {
        const string primary = "db.thenile.dev";
        if (host == primary) return true;
        if (host == null || !host.EndsWith("." + primary, StringComparison.Ordinal)) return false;
        var region = host[..^(primary.Length + 1)];
        return region.Length is >= 1 and <= 63 && region[0] != '-' && region[^1] != '-' &&
            region.All(character => char.IsAsciiLetterLower(character) || char.IsAsciiDigit(character) || character == '-');
    }
}
