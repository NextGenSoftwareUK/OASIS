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
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NBitcoin;

namespace NextGenSoftware.OASIS.API.Providers.BitcoinOASIS
{
    public partial class BitcoinOASIS
    {
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool continueOnErrorRecursive = true, int version = 0)
        {
            return LoadHolonsForParentAsync(providerKey, holonType, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, continueOnErrorRecursive, version).Result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaData, string value, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool continueOnErrorRecursive = true, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaData, value, holonType, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, continueOnErrorRecursive, version).Result;
        }

        // NFT Provider interface methods
        public OASISResult<IWeb3NFTTransactionResponse> SendNFT(ISendWeb3NFTRequest request)
        {
            return SendNFTAsync(request).Result;
        }

        public Task<OASISResult<IWeb3NFTTransactionResponse>> SendNFTAsync(ISendWeb3NFTRequest request)
        {
            return SendOrdinalInscriptionAsync(request);
        }

        public OASISResult<IWeb3NFTTransactionResponse> MintNFT(IMintWeb3NFTRequest request)
        {
            return MintNFTAsync(request).Result;
        }

        public Task<OASISResult<IWeb3NFTTransactionResponse>> MintNFTAsync(IMintWeb3NFTRequest request)
        {
            return MintOrdinalInscriptionAsync(request);
        }

        public OASISResult<IWeb3NFTTransactionResponse> BurnNFT(IBurnWeb3NFTRequest request)
        {
            return BurnNFTAsync(request).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> BurnNFTAsync(IBurnWeb3NFTRequest request)
        {
            if (request?.WaitTillNFTBurnt == true &&
                (request.WaitForNFTToBurnInSeconds <= 0 || request.AttemptToBurnEveryXSeconds <= 0))
            {
                var invalid = new OASISResult<IWeb3NFTTransactionResponse>();
                OASISErrorHandling.HandleError(ref invalid, "Bitcoin burn confirmation requires a positive timeout and polling interval.");
                return invalid;
            }

            var identifier = string.IsNullOrWhiteSpace(request?.TokenId) ? request?.NFTTokenAddress : request.TokenId;
            var result = await MoveOrdinalInscriptionAsync(identifier, null, null, request?.OwnerPrivateKey, request?.OwnerSeedPhrase,
                null, burn: true);
            if (result.IsError || request?.WaitTillNFTBurnt != true)
                return result;

            var transactionId = result.Result.TransactionResult;
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(request.WaitForNFTToBurnInSeconds));
            try
            {
                while (!timeout.IsCancellationRequested)
                {
                    var observed = await ReadOrdinalInscriptionAsync(identifier).WaitAsync(timeout.Token);
                    if (!observed.IsError &&
                        observed.Result.MetaData.TryGetValue("Burned", out var burned) && burned == "true" &&
                        observed.Result.MetaData["Satpoint"].StartsWith(transactionId + ":", StringComparison.Ordinal))
                    {
                        observed.Result.MetaData["BurnTransactionId"] = transactionId;
                        result.Result.Web3NFT = observed.Result;
                        return result;
                    }

                    await Task.Delay(TimeSpan.FromSeconds(request.AttemptToBurnEveryXSeconds), timeout.Token);
                }
            }
            catch (OperationCanceledException) when (timeout.IsCancellationRequested)
            {
                // The acknowledged transaction ID remains available for subsequent confirmation.
            }

            result.ErrorCode = "BITCOIN_CONFIRMATION_PENDING";
            OASISErrorHandling.HandleError(ref result, $"Bitcoin Core acknowledged burn {transactionId}. Its confirmation is still pending.");
            return result;
        }

        public OASISResult<IWeb3NFT> LoadOnChainNFTData(string nftTokenAddress)
        {
            return LoadOnChainNFTDataAsync(nftTokenAddress).Result;
        }

        public Task<OASISResult<IWeb3NFT>> LoadOnChainNFTDataAsync(string nftTokenAddress)
            => ReadOrdinalInscriptionAsync(nftTokenAddress);
    }
}
