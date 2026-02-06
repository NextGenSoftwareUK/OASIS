using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Request;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services;
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
        private readonly ISolanaSplTokenBalanceService _splTokenBalanceService;

        public SolanaController(ISolanaService solanaService, ISolanaSplTokenBalanceService splTokenBalanceService)
        {
            _solanaService = solanaService;
            _splTokenBalanceService = splTokenBalanceService;
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

        /// <summary>
        /// Get SPL token balance for a wallet's associated token account (ATA) for a given mint.
        /// Uses mainnet RPC when configured in OASIS_DNA (SolanaOASIS.MainnetConnectionString).
        /// AllowAnonymous so the Telegram bot (or other callers) can gate actions by token balance without auth.
        /// </summary>
        /// <param name="wallet">Owner wallet public key (base58).</param>
        /// <param name="mint">Token mint public key (base58).</param>
        /// <returns>Balance as decimal (human-readable amount).</returns>
        [AllowAnonymous]
        [HttpGet]
        [Route("spl-token-balance")]
        [ProducesResponseType(typeof(OASISResult<decimal>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISResult<decimal>> GetSplTokenBalance([FromQuery] string wallet, [FromQuery] string mint)
        {
            return await _splTokenBalanceService.GetBalanceAsync(wallet, mint).ConfigureAwait(false);
        }
    }
}