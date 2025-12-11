using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
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
        public async Task<OASISResult<MintNftResult>> MintNft([FromBody] MintWeb3NFTRequest request)
        {
            return await _solanaService.MintNftAsync(request);
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
    }
}