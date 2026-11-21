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

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS;

public partial class SolanaOASIS : ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation
{
    private static readonly HttpClient PublicAccountHttp = new(NativeConnectionGuard.CreateGuardedHttpHandler())
    {
        Timeout = TimeSpan.FromSeconds(15)
    };
    private SolanaPublicAccountMetadataSource.AccountsBackend _publicAccounts;
    private bool _publicAccountsVerified;

    public PType QueryProviderType => PType.SolanaOASIS;
    public CosmicQueryCapabilities QueryCapabilities => _publicAccounts == null
        ? null : SolanaPublicAccountMetadataSource.PublicAccountCapabilities;

    public void ConfigurePublicAccountQuery(string json)
    {
        if (_publicAccounts != null)
            throw new InvalidOperationException("The Solana public query catalogue is already configured.");
        _publicAccounts = SolanaPublicAccountMetadataSource.CreateAccounts(json, PublicAccountHttp);
    }

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken)
    {
        _publicAccountsVerified = false;
        if (_publicAccounts == null)
            return new() { IsError = true, ErrorCount = 1, ErrorCode = "CQL_CAPABILITY_UNSUPPORTED",
                Message = "Configure the reviewed Solana public account catalogue first." };

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated)
            {
                var activation = await ActivateProviderAsync();
                cancellationToken.ThrowIfCancellationRequested();
                if (activation.IsError || !activation.Result)
                    return activation;
            }

            await _publicAccounts.VerifyAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            _publicAccountsVerified = true;
            return new(true);
        }
        catch (Exception error)
        {
            return new() { IsError = true, ErrorCount = 1, ErrorCode = error is OperationCanceledException
                ? "CQL_QUERY_CANCELLED" : "CQL_SOURCE_UNAVAILABLE",
                Message = "Public Solana account query activation failed.", Exception = error };
        }
    }

    public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon =>
        SolanaPublicAccountMetadataSource.ExecutePublicAccountQueryAsync(_publicAccounts,
            IsProviderActivated && _publicAccountsVerified, plan, maximumResults, cancellationToken);
}
