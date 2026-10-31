using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using System.Text.Json.Serialization;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS
{
    public partial class PolkadotOASIS
    {
        public OASISResult<ITransactionResponse> SendToken(ISendWeb3TokenRequest request)
        {
            return Task.Run(() => SendTokenAsync(request)).GetAwaiter().GetResult();
        }

        public Task<OASISResult<ITransactionResponse>> SendTokenAsync(ISendWeb3TokenRequest request)
            => !string.IsNullOrEmpty(request?.FromTokenAddress) ? SendOwnedAssetAsync(request) : SendOwnedNativeTransferAsync(request);

        public OASISResult<ITransactionResponse> MintToken(IMintWeb3TokenRequest request)
        {
            return Task.Run(() => MintTokenAsync(request)).GetAwaiter().GetResult();
        }

        public Task<OASISResult<ITransactionResponse>> MintTokenAsync(IMintWeb3TokenRequest request)
            => MintOwnedAssetAsync(request);

        public OASISResult<ITransactionResponse> BurnToken(IBurnWeb3TokenRequest request)
        {
            return Task.Run(() => BurnTokenAsync(request)).GetAwaiter().GetResult();
        }

        public Task<OASISResult<ITransactionResponse>> BurnTokenAsync(IBurnWeb3TokenRequest request)
            => BurnOwnedAssetAsync(request);

        public OASISResult<ITransactionResponse> LockToken(ILockWeb3TokenRequest request)
            =>LockTokenAsync(request).GetAwaiter().GetResult();

        public Task<OASISResult<ITransactionResponse>> LockTokenAsync(ILockWeb3TokenRequest request)
            =>ChangeOwnedTokenEscrowAsync(request,null);

        public OASISResult<ITransactionResponse> UnlockToken(IUnlockWeb3TokenRequest request)
            =>UnlockTokenAsync(request).GetAwaiter().GetResult();

        public Task<OASISResult<ITransactionResponse>> UnlockTokenAsync(IUnlockWeb3TokenRequest request)
            =>ChangeOwnedTokenEscrowAsync(null,request);

        public OASISResult<double> GetBalance(IGetWeb3WalletBalanceRequest request)
        {
            return GetBalanceAsync(request).GetAwaiter().GetResult();
        }

        public async Task<OASISResult<double>> GetBalanceAsync(IGetWeb3WalletBalanceRequest request)
        {
            var result = new OASISResult<double>();
            try
            {
                if (!_isActivated || _httpClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Polkadot provider is not activated");
                    return result;
                }

                if (request == null || string.IsNullOrWhiteSpace(request.WalletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "WalletAddress is required");
                    return result;
                }

                var address = request.WalletAddress;
                result.Result = await ReadNativeBalanceAsync(address);
                result.Message = "Native free balance read from finalized Polkadot runtime storage.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting balance: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IList<IWalletTransaction>> GetTransactions(IGetWeb3TransactionsRequest request)
        {
            return Task.Run(() => GetTransactionsAsync(request)).GetAwaiter().GetResult();
        }

        public Task<OASISResult<IList<IWalletTransaction>>> GetTransactionsAsync(IGetWeb3TransactionsRequest request)
            => ReadWalletHistoryAsync(request);

    }
}
