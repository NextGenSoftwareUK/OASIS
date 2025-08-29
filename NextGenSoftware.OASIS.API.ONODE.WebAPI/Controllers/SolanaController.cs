using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Request;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.DTOs.Requests;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.DTOs.Responses;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Services.Solana;
using NextGenSoftware.OASIS.Common;
using Solnet.Metaplex;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SolanaController : OASISControllerBase
    {
        private SolanaOASIS _solanaOASIS = null;

        private SolanaOASIS SolanaOASIS
        {
            get
            {
                if (_solanaOASIS == null)
                {
                    OASISResult<IOASISStorageProvider> result = Task.Run(async () => await OASISBootLoader.OASISBootLoader.GetAndActivateStorageProviderAsync(ProviderType.SolanaOASIS)).Result;

                    if (result.IsError)
                        OASISErrorHandling.HandleError(ref result, string.Concat("Error calling OASISBootLoader.OASISBootLoader.GetAndActivateProvider(ProviderType.SolanaOASIS). Error details: ", result.Message));

                    _solanaOASIS = (SolanaOASIS)result.Result;
                }

                return _solanaOASIS;
            }
        }

        /// <summary>
        /// Mint NFT (non-fungible token)
        /// </summary>
        /// <param name="request">Mint Public Key Account, and Mint Decimals for Mint NFT</param>
        /// <returns>Mint NFT Transaction Hash</returns>
        [HttpPost]
        [Route("Mint")]
        public async Task<OASISResult<MintNftResult>> MintNft([FromBody] MintNFTTransactionRequestForProvider request)
        {
            // Convert the request to the format expected by SolanaOASIS
            var nftRequest = new NextGenSoftware.OASIS.API.Core.Objects.NFT.Request.MintNFTTransactionRequestForProvider
            {
                Title = request.Title,
                Description = request.Description ?? request.Symbol, // Use Description if provided, otherwise fallback to Symbol
                Symbol = request.Symbol,
                JSONUrl = request.JSONUrl,
                // Add required fields that SolanaOASIS expects
                MintWalletAddress = request.MintWalletAddress,
                MintedByAvatarId = request.MintedByAvatarId,
                ImageUrl = request.ImageUrl,
                ThumbnailUrl = request.ThumbnailUrl,
                Price = request.Price,
                Discount = request.Discount,
                MemoText = request.MemoText,
                NumberToMint = request.NumberToMint,
                StoreNFTMetaDataOnChain = request.StoreNFTMetaDataOnChain,
                MetaData = request.MetaData ?? new Dictionary<string, object> { { "symbol", request.Symbol }, { "jsonUrl", request.JSONUrl } }
            };

            var result = await SolanaOASIS.MintNFTAsync(nftRequest);
            
            if (result.IsError)
            {
                return new OASISResult<MintNftResult>
                {
                    IsError = true,
                    Message = result.Message
                };
            }

            // Convert the result to MintNftResult format
            return new OASISResult<MintNftResult>
            {
                IsError = false,
                Result = new MintNftResult
                {
                    MintAccount = result.Result?.TransactionResult ?? "Unknown",
                    Network = "Solana",
                    TransactionHash = result.Result?.TransactionResult ?? "Unknown"
                }
            };
        }

        /// <summary>
        /// Handles a transaction between accounts with a specific Lampposts size
        /// </summary>
        /// <param name="request">FromAccount(Public Key) and ToAccount(Public Key)
        /// between which the transaction will be carried out</param>
        /// <returns>Send Transaction Hash</returns>
        [HttpPost]
        [Route("Send")]
        public async Task<OASISResult<SendTransactionResult>> SendTransaction([FromBody] SendTransactionRequest request)
        {
            // Convert the request to the format expected by SolanaOASIS
            var nftRequest = new NextGenSoftware.OASIS.API.Core.Objects.NFT.Request.NFTWalletTransactionRequest
            {
                FromWalletAddress = request.FromAccount.PublicKey,
                ToWalletAddress = request.ToAccount.PublicKey,
                Amount = request.Lampposts,
                MemoText = request.MemoText
            };

            var result = await SolanaOASIS.SendNFTAsync(nftRequest);
            
            if (result.IsError)
            {
                return new OASISResult<SendTransactionResult>
                {
                    IsError = true,
                    Message = result.Message
                };
            }

            // Convert the result to SendTransactionResult format
            return new OASISResult<SendTransactionResult>
            {
                IsError = false,
                Result = new SendTransactionResult
                {
                    TransactionHash = result.Result?.TransactionResult ?? "Unknown"
                }
            };
        }
    }
}