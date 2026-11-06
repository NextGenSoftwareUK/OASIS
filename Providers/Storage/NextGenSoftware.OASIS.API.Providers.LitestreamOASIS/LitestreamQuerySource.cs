using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.LitestreamOASIS;

/// <summary>Bounded public reads from a separately approved Litestream restore.</summary>
public sealed class LitestreamQuerySource : LitestreamOASIS, ICosmicReadOnlyQueryActivation
{
    private readonly string _path;
    private readonly string _sha256;
    private readonly Guid _probe;

    public LitestreamQuerySource(string path, string sha256, Guid probe) : base(path, readOnly: true)
    {
        if (!Path.IsPathFullyQualified(path)) throw new ArgumentException("Use an absolute restored database path.", nameof(path));
        if (sha256 is not { Length: 64 } || sha256.Any(c => !char.IsAsciiHexDigitLower(c)))
            throw new ArgumentException("Use the approved lowercase restore SHA256.", nameof(sha256));
        if (probe == Guid.Empty) throw new ArgumentException("An existing teaching probe is required.", nameof(probe));
        _path = path;
        _sha256 = sha256;
        _probe = probe;
    }

    protected override bool RequiresPublicQueryRecords => true;

    public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().GetAwaiter().GetResult();

    public override Task<OASISResult<bool>> ActivateProviderAsync() => ActivateQuerySourceAsync(default);

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken)
    {
        IsProviderActivated = false;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await ValidateQuerySourceAsync(cancellationToken).ConfigureAwait(false);
            await VerifyPublicReplicaProbeAsync(_probe, cancellationToken).ConfigureAwait(false);
            await ValidateQuerySourceAsync(cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            IsProviderActivated = true;
            return new(true);
        }
        catch (Exception cause)
        {
            return new() { IsError = true, ErrorCount = 1, Exception = cause,
                ErrorCode = cause is OperationCanceledException ? "CQL_QUERY_CANCELLED" : "CQL_QUERY_SOURCE_UNAVAILABLE",
                Message = "The Litestream teaching snapshot did not verify its probe." };
        }
    }

    protected override async Task ValidateQuerySourceAsync(CancellationToken cancellationToken)
    {
        if (File.Exists(_path + "-wal") || File.Exists(_path + "-shm") || File.Exists(_path + "-journal"))
            throw new InvalidDataException("The approved Litestream restore must be a standalone snapshot.");
        await using var stream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (stream.Length is <= 0 or > 16 * 1024 * 1024 ||
            Convert.ToHexStringLower(await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false)) != _sha256)
            throw new InvalidDataException("The mounted Litestream restore differs from the approved snapshot.");
    }
}
