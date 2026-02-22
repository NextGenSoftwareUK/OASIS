using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Request;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Interfaces;
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

    /// <summary>
    /// Helper to inspect top token holders (e.g. for $SAINT) and find a wallet's rank. Uses same RPC as /ordain.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class TokenHoldersController : OASISControllerBase
    {
        private readonly ITopTokenHoldersService _topTokenHoldersService;
        private readonly IWalletPfpService _walletPfpService;

        public TokenHoldersController(ITopTokenHoldersService topTokenHoldersService, IWalletPfpService walletPfpService = null)
        {
            _topTokenHoldersService = topTokenHoldersService;
            _walletPfpService = walletPfpService;
        }

        /// <summary>
        /// Get top holders for a token mint (e.g. $SAINT). Optionally pass wallet to get that wallet's rank (1-based). When includePfp=true, resolves each holder's profile image via Helius DAS (first NFT image).
        /// </summary>
        [AllowAnonymous]
        [HttpGet]
        [Route("top-holders")]
        public async Task<ActionResult<TopHoldersResponse>> GetTopHolders([FromQuery] string mint, [FromQuery] int topN = 50, [FromQuery] string wallet = null, [FromQuery] bool includePfp = false)
        {
            if (string.IsNullOrWhiteSpace(mint))
                return BadRequest("mint query parameter required (e.g. 9VTxmdpCD9dKsJZaBccHsBwYcebGV6iCZtKA8et5pump).");
            if (topN <= 0 || topN > 200)
                topN = 50;
            var holders = await _topTokenHoldersService.GetTopHoldersAsync(mint.Trim(), topN).ConfigureAwait(false);
            if (holders == null || holders.Count == 0)
                return Ok(new TopHoldersResponse { Mint = mint, Holders = new List<HolderEntry>(), WalletRank = null, Message = "No holders returned (RPC may have failed or mint invalid)." });
            var list = new List<HolderEntry>();
            var searchWallet = wallet?.Trim();
            int? rank = null;
            for (var i = 0; i < holders.Count; i++)
            {
                var (addr, balance) = holders[i];
                list.Add(new HolderEntry { Rank = i + 1, WalletAddress = addr, Balance = balance });
                if (!string.IsNullOrEmpty(searchWallet) && string.Equals(addr, searchWallet, StringComparison.OrdinalIgnoreCase))
                    rank = i + 1;
            }

            if (includePfp && _walletPfpService != null)
            {
                var tasks = list.Select(async entry =>
                {
                    var url = await _walletPfpService.GetPfpUrlAsync(entry.WalletAddress).ConfigureAwait(false);
                    entry.ImageUrl = url;
                });
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }

            return Ok(new TopHoldersResponse
            {
                Mint = mint,
                Holders = list,
                WalletRank = rank,
                Message = rank.HasValue ? $"Wallet {searchWallet} is rank #{rank}." : (string.IsNullOrEmpty(searchWallet) ? null : $"Wallet {searchWallet} is not in the top {topN} holders.")
            });
        }
    }

    public class TopHoldersResponse
    {
        public string Mint { get; set; }
        public List<HolderEntry> Holders { get; set; }
        public int? WalletRank { get; set; }
        public string Message { get; set; }
    }

    public class HolderEntry
    {
        public int Rank { get; set; }
        public string WalletAddress { get; set; }
        public decimal Balance { get; set; }
        /// <summary>Profile picture URL (e.g. first NFT image) when requested via includePfp=true.</summary>
        public string ImageUrl { get; set; }
    }
}