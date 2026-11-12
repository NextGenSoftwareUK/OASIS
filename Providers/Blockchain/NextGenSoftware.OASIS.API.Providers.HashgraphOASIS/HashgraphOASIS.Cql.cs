using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
using PType = NextGenSoftware.OASIS.API.Core.Enums.ProviderType;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS;

public partial class HashgraphOASIS : ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation
{
    private static readonly HttpClient PublicAccountHttp = new(NativeConnectionGuard.CreateGuardedHttpHandler())
    {
        Timeout = TimeSpan.FromSeconds(15)
    };
    private HederaPublicAccountMetadataSource.AccountsBackend publicAccounts;
    private readonly object publicQueryActivationGate = new();
    private int publicQueryActivationGeneration;
    private volatile bool publicQueryVerified;

    public PType QueryProviderType => PType.HashgraphOASIS;
    public CosmicQueryCapabilities QueryCapabilities => publicAccounts == null
        ? null : HederaPublicAccountMetadataSource.PublicCapabilities;
    public bool IsQuerySourceActivated => publicQueryVerified;

    public void ConfigurePublicAccountQuery(string json)
    {
        if (publicAccounts != null)
            return;
        var configured = HederaPublicAccountMetadataSource.CreateAccounts(json, PublicAccountHttp);
        Interlocked.CompareExchange(ref publicAccounts, configured, null);
    }

    private int ResetPublicQueryActivation()
    {
        lock (publicQueryActivationGate)
        {
            publicQueryVerified = false;
            return ++publicQueryActivationGeneration;
        }
    }

    public override Task<OASISResult<bool>> DeActivateProviderAsync()
    {
        ResetPublicQueryActivation();
        return base.DeActivateProviderAsync();
    }

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken)
    {
        var generation = ResetPublicQueryActivation();
        var accounts = publicAccounts;
        if (accounts == null)
            return new() { IsError = true, ErrorCount = 1, ErrorCode = "CQL_CAPABILITY_UNSUPPORTED",
                Message = "Configure the reviewed Hedera public account catalogue first." };

        try
        {
            await accounts.VerifyAsync(cancellationToken).ConfigureAwait(false);
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
                Message = "Public Hedera account query activation failed.", Exception = error };
        }
    }

    public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
        => HederaPublicAccountMetadataSource.ExecutePublicQueryAsync(publicAccounts, IsQuerySourceActivated,
            plan, maximumResults, cancellationToken);
}
