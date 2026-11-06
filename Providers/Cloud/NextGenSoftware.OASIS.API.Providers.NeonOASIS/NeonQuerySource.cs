using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using Npgsql;

namespace NextGenSoftware.OASIS.API.Providers.NeonOASIS;

/// <summary>Bounded public teaching reads on one dedicated Neon endpoint.</summary>
public sealed class NeonQuerySource : NeonOASIS, ICosmicReadOnlyQueryActivation
{
    private readonly Guid _probe;

    public NeonQuerySource(string connectionString, Guid probe)
        : base(RequireNeonEndpoint(connectionString), usePooler: false)
    {
        if (probe == Guid.Empty) throw new ArgumentException("An existing teaching probe is required.", nameof(probe));
        _probe = probe;
    }

    protected override ProviderType NativeBoundedQueryAuthority => Core.Enums.ProviderType.NeonOASIS;
    protected override bool RequiresPublicQueryRecords => true;

    public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().GetAwaiter().GetResult();

    public override Task<OASISResult<bool>> ActivateProviderAsync() => ActivateQuerySourceAsync(default);

    public Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken) =>
        VerifyPublicQueryProbeAsync(_probe, cancellationToken);

    private static string RequireNeonEndpoint(string connectionString)
    {
        var verified = RequireVerifiedQueryConnection(connectionString);
        if (!IsDirectEndpoint(new NpgsqlConnectionStringBuilder(verified).Host))
            throw new ArgumentException("Use one direct Neon compute hostname.", nameof(connectionString));
        return verified;
    }

    public static bool IsDirectEndpoint(string? host) =>
        host is { Length: > 0 and <= 253 } &&
        host.StartsWith("ep-", StringComparison.Ordinal) &&
        host.EndsWith(".neon.tech", StringComparison.Ordinal) &&
        !host[..host.IndexOf('.')].EndsWith("-pooler", StringComparison.Ordinal) &&
        host.Split('.').All(label => label.Length is >= 1 and <= 63 &&
            label[0] != '-' && label[^1] != '-' &&
            label.All(c => char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c == '-'));
}
