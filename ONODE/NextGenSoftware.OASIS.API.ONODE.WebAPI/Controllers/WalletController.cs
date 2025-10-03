using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallets.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    /// <summary>
    /// Wallet management endpoints for cryptocurrency and digital asset operations.
    /// Provides comprehensive wallet functionality including transactions, balances, and multi-chain support.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class WalletController : OASISControllerBase
    {
        private WalletManager _walletManager = null;

        public WalletManager WalletManager
        {
            get
            {
                if (_walletManager == null)
                {
                    OASISResult<IOASISStorageProvider> result = Task.Run(OASISBootLoader.OASISBootLoader.GetAndActivateDefaultStorageProviderAsync).Result;

                    if (result.IsError)
                        OASISErrorHandling.HandleError(ref result, string.Concat("Error calling OASISBootLoader.OASISBootLoader.GetAndActivateDefaultStorageProvider(). Error details: ", result.Message));

                    _walletManager = new WalletManager(result.Result);
                }

                return _walletManager;
            }
        }

        ///// <summary>
        /////     Clear's the KeyManager's internal cache of keys.
        ///// </summary>
        ///// <returns></returns>
        //[Authorize]
        //[HttpPost("clear_cache")]
        //public OASISResult<bool> ClearCache()
        //{
        //    return WalletManager.ClearCache();
        //}

        /// <summary>
        ///     Send's a given token to the target provider.
        /// </summary>
        /// <param name="request">The wallet transaction request containing token details and recipient information.</param>
        /// <returns>OASIS result containing the transaction response or error details.</returns>
        /// <response code="200">Token sent successfully</response>
        /// <response code="400">Error sending token</response>
        /// <response code="401">Unauthorized - authentication required</response>
        [Authorize]
        [HttpPost("send_token")]
        [ProducesResponseType(typeof(OASISResult<ITransactionRespone>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status401Unauthorized)]
        public async Task<OASISResult<ITransactionRespone>> SendTokenAsync(IWalletTransactionRequest request)
        {
            return await WalletManager.SendTokenAsync(request);
        }

        //TODO: Need to copy all of the WalletManager functions over to here ASAP!
    }
}