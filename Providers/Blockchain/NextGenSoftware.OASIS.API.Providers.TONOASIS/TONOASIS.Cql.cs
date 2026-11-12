using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.TONOASIS;

public sealed partial class TONOASIS : ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation
{
    private static readonly HttpClient PublicHeaderHttp = new(NativeConnectionGuard.CreateGuardedHttpHandler())
    {
        Timeout = TimeSpan.FromSeconds(15)
    };
    private TonPublicHeaderMetadataSource.HeadersBackend publicHeaders;
    private readonly object publicQueryActivationGate = new();
    private int publicQueryActivationGeneration;
    private volatile bool publicQueryVerified;

    private static readonly CosmicQueryCapabilities PublicHeaderCapabilities =
        new CosmicQueryCapabilities(CosmicQueryFeatures.None, Array.Empty<CosmicQueryFieldCapability>())
            .WithStringMetadataLookup(TonPublicHeaderMetadataSource.AddressMetadata, 44, 8);

    public new CosmicQueryCapabilities QueryCapabilities => publicHeaders == null
        ? base.QueryCapabilities : PublicHeaderCapabilities;
    public new bool IsQuerySourceActivated => publicQueryVerified;

    public void ConfigurePublicHeaderQuery(string json)
    {
        if (publicHeaders != null)
            return;
        var configured = TonPublicHeaderMetadataSource.CreateHeaders(json, PublicHeaderHttp);
        Interlocked.CompareExchange(ref publicHeaders, configured, null);
    }

    private int ResetPublicQueryActivation()
    {
        lock (publicQueryActivationGate)
        {
            publicQueryVerified = false;
            return ++publicQueryActivationGeneration;
        }
    }

    public override OASISResult<bool> DeActivateProvider()
    {
        ResetPublicQueryActivation();
        return base.DeActivateProvider();
    }

    public new async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken)
    {
        var generation = ResetPublicQueryActivation();
        var headers = publicHeaders;
        if (headers == null)
            return new() { IsError = true, ErrorCount = 1, ErrorCode = "CQL_CAPABILITY_UNSUPPORTED",
                Message = "Configure the reviewed TON public header catalogue first." };

        try
        {
            await headers.VerifyAsync(cancellationToken).ConfigureAwait(false);
            lock (publicQueryActivationGate)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (generation != publicQueryActivationGeneration)
                    return new() { IsError = true, ErrorCount = 1, ErrorCode = "CQL_SOURCE_UNAVAILABLE",
                        Message = "The public account activation was superseded." };
                publicQueryVerified = true;
            }
            return new(true);
        }
        catch (Exception error)
        {
            return new() { IsError = true, ErrorCount = 1, ErrorCode = error is OperationCanceledException
                ? "CQL_QUERY_CANCELLED" : "CQL_SOURCE_UNAVAILABLE",
                Message = "Public TON header query activation failed.", Exception = error };
        }
    }

    public new Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
        => publicHeaders == null
            ? base.ExecuteCosmicQueryAsync(plan, maximumResults, cancellationToken)
            : TonPublicHeaderMetadataSource.ExecutePublicQueryAsync(publicHeaders, IsQuerySourceActivated,
                plan, maximumResults, cancellationToken);
}
