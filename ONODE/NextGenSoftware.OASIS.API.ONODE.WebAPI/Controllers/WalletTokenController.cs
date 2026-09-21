using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    /// <summary>
    /// Extended wallet token operations: burn, lock, unlock, import by secret phrase.
    /// Routes share the api/wallet prefix with WalletController.
    /// </summary>
    [Route("api/wallet")]
    [ApiController]
    public class WalletTokenController : OASISControllerBase
    {
        private WalletManager _walletManager;

        private WalletManager WalletManager
        {
            get
            {
                if (_walletManager == null)
                {
                    OASISResult<IOASISStorageProvider> result = Task.Run(OASISBootLoader.OASISBootLoader.GetAndActivateDefaultStorageProviderAsync).Result;
                    if (result.IsError)
                        OASISErrorHandling.HandleError(ref result, $"Error activating default storage provider: {result.Message}");
                    _walletManager = new WalletManager(result.Result);
                }
                return _walletManager;
            }
        }


        // ─────────────────────────────────────────────────────────────────────────
        // Burn Token
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Burns (destroys) a token on the given blockchain provider.
        /// </summary>
        [Authorize]
        [HttpPost("burn-token")]
        [ProducesResponseType(typeof(OASISResult<ITransactionResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status401Unauthorized)]
        public async Task<OASISResult<ITransactionResponse>> BurnToken([FromBody] BurnWeb4TokenRequest request)
        {
            return await WalletManager.BurnTokenAsync(request);
        }


        // ─────────────────────────────────────────────────────────────────────────
        // Lock Token
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Locks a token on the given blockchain provider so it cannot be transferred.
        /// </summary>
        [Authorize]
        [HttpPost("lock-token")]
        [ProducesResponseType(typeof(OASISResult<ITransactionResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status401Unauthorized)]
        public async Task<OASISResult<ITransactionResponse>> LockToken([FromBody] LockWeb4TokenRequest request)
        {
            return await WalletManager.LockTokenAsync(request);
        }


        // ─────────────────────────────────────────────────────────────────────────
        // Unlock Token
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Unlocks a previously locked token on the given blockchain provider.
        /// </summary>
        [Authorize]
        [HttpPost("unlock-token")]
        [ProducesResponseType(typeof(OASISResult<ITransactionResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status401Unauthorized)]
        public async Task<OASISResult<ITransactionResponse>> UnlockToken([FromBody] UnlockWeb4TokenRequest request)
        {
            return await WalletManager.UnlockTokenAsync(request);
        }


        // ─────────────────────────────────────────────────────────────────────────
        // Import Wallet by Secret Phrase
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Imports a wallet for an avatar by secret phrase (by avatar ID).
        /// </summary>
        [Authorize]
        [HttpPost("avatar/{avatarId}/import/secret-phrase")]
        [ProducesResponseType(typeof(OASISResult<IProviderWallet>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISResult<IProviderWallet>> ImportWalletUsingSecretPhraseById(Guid avatarId, [FromBody] string secretPhrase,
            ProviderType providerType = ProviderType.Default)
        {
            return await WalletManager.ImportWalletUsingSecretPhaseByIdAsync(avatarId, secretPhrase, providerType);
        }

        /// <summary>
        /// Imports a wallet for an avatar by secret phrase (by username).
        /// </summary>
        [Authorize]
        [HttpPost("avatar/username/{username}/import/secret-phrase")]
        [ProducesResponseType(typeof(OASISResult<IProviderWallet>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISResult<IProviderWallet>> ImportWalletUsingSecretPhraseByUsername(string username, [FromBody] string secretPhrase,
            ProviderType providerType = ProviderType.Default)
        {
            return await WalletManager.ImportWalletUsingSecretPhaseByUsernameAsync(username, secretPhrase, providerType);
        }

        /// <summary>
        /// Imports a wallet for an avatar by secret phrase (by email).
        /// </summary>
        [Authorize]
        [HttpPost("avatar/email/{email}/import/secret-phrase")]
        [ProducesResponseType(typeof(OASISResult<IProviderWallet>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISResult<IProviderWallet>> ImportWalletUsingSecretPhraseByEmail(string email, [FromBody] string secretPhrase,
            ProviderType providerType = ProviderType.Default)
        {
            return await WalletManager.ImportWalletUsingSecretPhaseByEmailAsync(email, secretPhrase, providerType);
        }
    }
}
