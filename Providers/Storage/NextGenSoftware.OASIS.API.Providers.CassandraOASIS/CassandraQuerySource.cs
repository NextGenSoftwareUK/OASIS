using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Cassandra;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.CassandraOASIS;

/// <summary>Dedicated, read-only Apache Cassandra source using an approved Astra secure connection bundle.</summary>
public sealed class CassandraQuerySource : CassandraOASIS, ICosmicReadOnlyQueryActivation, IDisposable
{
    private readonly string _bundlePath;
    private readonly string _bundleSha256;
    private readonly string _readerToken;
    private readonly Guid _probe;
    private readonly SemaphoreSlim _lifecycle = new(1);

    public CassandraQuerySource(string bundlePath, string bundleSha256, string readerToken, Guid probe)
        : base(Array.Empty<string>())
    {
        if (!Path.IsPathFullyQualified(bundlePath)) throw new ArgumentException("Use an absolute bundle path.", nameof(bundlePath));
        if (bundleSha256 is not { Length: 64 } || bundleSha256.Any(c => !char.IsAsciiHexDigitLower(c)))
            throw new ArgumentException("Use the approved lowercase bundle SHA256.", nameof(bundleSha256));
        if (readerToken is not { Length: > 9 and <= 4096 } || !readerToken.StartsWith("AstraCS:", StringComparison.Ordinal) ||
            readerToken.Any(c => char.IsControl(c) || char.IsWhiteSpace(c)))
            throw new ArgumentException("Use an Astra application token with the restricted reader role.", nameof(readerToken));
        if (probe == Guid.Empty) throw new ArgumentException("An existing teaching probe is required.", nameof(probe));
        _bundlePath = bundlePath;
        _bundleSha256 = bundleSha256;
        _readerToken = readerToken;
        _probe = probe;
    }

    protected override bool RequiresPublicQueryRecords => true;

    public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().GetAwaiter().GetResult();

    public override Task<OASISResult<bool>> ActivateProviderAsync() => ActivateQuerySourceAsync(default);

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken)
    {
        try { await _lifecycle.WaitAsync(cancellationToken).ConfigureAwait(false); }
        catch (OperationCanceledException cause)
        {
            return new() { IsError = true, ErrorCount = 1, ErrorCode = "CQL_QUERY_CANCELLED", Exception = cause };
        }
        try { return await ActivatePublicQuerySourceAsync(BuildCluster, _probe, cancellationToken).ConfigureAwait(false); }
        finally { _lifecycle.Release(); }
    }

    public override async Task<OASISResult<bool>> DeActivateProviderAsync()
    {
        await _lifecycle.WaitAsync().ConfigureAwait(false);
        try { return await base.DeActivateProviderAsync().ConfigureAwait(false); }
        finally { _lifecycle.Release(); }
    }

    public void Dispose() => DeActivateProviderAsync().GetAwaiter().GetResult();

    private ICluster BuildCluster()
    {
        using var bundle = new FileStream(_bundlePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (bundle.Length is <= 0 or > 1024 * 1024 ||
            Convert.ToHexStringLower(SHA256.HashData(bundle)) != _bundleSha256)
            throw new InvalidDataException("The mounted Cassandra secure connection bundle differs from the approved source.");

        return Cluster.Builder()
            .WithCloudSecureConnectionBundle(_bundlePath)
            .WithCredentials("token", _readerToken)
            .WithSocketOptions(new SocketOptions().SetConnectTimeoutMillis(8000).SetReadTimeoutMillis(15000))
            .WithRetryPolicy(FallthroughRetryPolicy.Instance)
            .Build();
    }
}
