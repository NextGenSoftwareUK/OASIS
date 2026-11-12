using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS;

public sealed partial class EthereumOASIS_Web3Core : ICosmicReadOnlyQueryActivation
{
    private static readonly HttpClient PublicAccountHttp = new(NativeConnectionGuard.CreateGuardedHttpHandler())
    {
        Timeout = TimeSpan.FromSeconds(15)
    };

    private sealed record PublicQuery(EvmPublicAccountMetadataSource.IPublicAccountQuery Accounts,
        CosmicQueryCapabilities Capabilities);

    private PublicQuery publicQuery;
    private volatile bool publicQueryVerified;

    public override CosmicQueryCapabilities QueryCapabilities => publicQuery?.Capabilities ?? base.QueryCapabilities;

    public void ConfigurePublicAccountQuery(string json)
    {
        if (publicQuery != null)
            return;
        var accounts = EthereumPublicAccountMetadataSource.CreateQuery(json, PublicAccountHttp);
        var capabilities = base.QueryCapabilities.WithStringMetadataLookup(
            EthereumPublicAccountMetadataSource.AddressMetadata, 42, 8);
        Interlocked.CompareExchange(ref publicQuery, new PublicQuery(accounts, capabilities), null);
    }

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken)
    {
        publicQueryVerified = false;
        var query = publicQuery;
        if (query == null)
            return new() { IsError = true, ErrorCount = 1, ErrorCode = "CQL_CAPABILITY_UNSUPPORTED",
                Message = "Configure the reviewed Ethereum public account catalogue first." };

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated)
            {
                var activation = await ActivateProviderAsync().ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                if (activation.IsError || !activation.Result)
                    return activation;
            }
            await query.Accounts.VerifyAsync(cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            publicQueryVerified = true;
            return new(true);
        }
        catch (Exception error)
        {
            return new() { IsError = true, ErrorCount = 1, ErrorCode = error is OperationCanceledException
                ? "CQL_QUERY_CANCELLED" : "CQL_SOURCE_UNAVAILABLE",
                Message = "Public Ethereum account query activation failed.", Exception = error };
        }
    }

    public override Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default)
    {
        var query = publicQuery;
        return query?.Capabilities.UsesStringMetadataLookup(plan?.Query) == true
            ? query.Accounts.QueryAsync(plan, maximumResults, IsProviderActivated && publicQueryVerified, cancellationToken)
            : base.ExecuteCosmicQueryAsync(plan, maximumResults, cancellationToken);
    }
}
