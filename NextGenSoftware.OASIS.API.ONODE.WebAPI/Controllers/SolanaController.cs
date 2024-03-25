using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.DTOs.Requests;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.DTOs.Responses;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Services.Solana;
using NextGenSoftware.OASIS.Common;
using Solnet.Metaplex;

namespace NextGenSoftware.OASIS.API.ONode.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SolanaController : OASISControllerBase
    {
        private readonly ISolanaService _solanaService;

        public SolanaController(ISolanaService solanaService)
        {
            _solanaService = solanaService;
        }

        /// <summary>
        /// Mint NFT (non-fungible token)
        /// </summary>
        /// <param name="request">Mint Public Key Account, and Mint Decimals for Mint NFT</param>
        /// <returns>Mint NFT Transaction Hash</returns>
        [HttpPost]
        [Route("Mint")]
        public async Task<OASISResult<MintNftResult>> MintNft([FromBody] MintNftRequest request)
        {
            return await _solanaService.MintNftAsync(request);
        }

        /// <summary>
        /// Exchange MintAccount (Public Key) between FromAccount (Public Key) and ToAccount (Public Key)
        /// </summary>
        /// <param name="request">Exchange Token Request</param>
        /// <returns>Exchange Transaction Hash</returns>
        [HttpPost]
        [Route("Exchange")]
        public async Task<OASISResult<ExchangeTokenResult>> ExchangeToken([FromBody] ExchangeTokenRequest request)
        {
            return await _solanaService.ExchangeTokens(request);
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
            return await _solanaService.SendTransaction(request);
        }

        /// <summary>
        /// Returns NFT Metadata that specified in NFT Account Address
        /// </summary>
        /// <param name="request">NFT Account Address</param>
        /// <returns>NFT Metadata</returns>
        [HttpGet]
        [Route("GetNftMetadata")]
        public async Task<OASISResult<GetNftMetadataResult>> GetNftMetadata(GetNftMetadataRequest request)
        {
            return await _solanaService.GetNftMetadata(request);
        }

        /// <summary>
        /// Returns Account and Balances of token wallet that specified in OwnerAccount (Public Key)
        /// </summary>
        /// <param name="request">OwnerAccount(Public Key)</param>
        /// <returns>Accounts and Balances of token wallet</returns>
        [HttpGet]
        [Route("GetNftWallet")]
        public async Task<OASISResult<GetNftWalletResult>> GetNftWallet(GetNftWalletRequest request)
        {
            return await _solanaService.GetNftWallet(request);
        }
    }
}