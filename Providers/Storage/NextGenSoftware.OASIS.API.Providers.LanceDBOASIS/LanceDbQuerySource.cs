using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.LanceDBOASIS;

/// <summary>Explicit teaching source on a provisioned LanceDB table.</summary>
public sealed class LanceDbQuerySource : LanceDBOASIS, ICosmicReadOnlyQueryActivation
{
    private readonly Guid _probe;

    public LanceDbQuerySource(HttpClient client, Uri origin, string apiKey, string database, Guid probe)
        : base(client, ValidateOrigin(origin), apiKey, database)
    {
        if (probe == Guid.Empty) throw new ArgumentException("An existing teaching probe is required.", nameof(probe));
        _probe = probe;
    }

    protected override bool RequiresPublicQueryRecords => true;

    public override Task<OASISResult<bool>> ActivateProviderAsync() => ActivateQuerySourceAsync(default);

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken)
    {
        IsProviderActivated = false;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await VerifyPublicQueryProbeAsync(_probe, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            IsProviderActivated = true;
            return new(true);
        }
        catch (Exception cause)
        {
            return new() { IsError = true, ErrorCount = 1, Exception = cause,
                ErrorCode = cause is OperationCanceledException ? "CQL_QUERY_CANCELLED" : "CQL_QUERY_SOURCE_UNAVAILABLE",
                Message = "The LanceDB teaching source did not verify its probe." };
        }
    }

    private static string ValidateOrigin(Uri origin)
    {
        if (origin is not { IsAbsoluteUri: true, Scheme: "https", AbsolutePath: "/", IsLoopback: false } ||
            origin.UserInfo.Length != 0 || origin.Query.Length != 0 || origin.Fragment.Length != 0)
            throw new ArgumentException("Use a credential-free HTTPS LanceDB origin.", nameof(origin));
        return origin.GetLeftPart(UriPartial.Authority);
    }
}
