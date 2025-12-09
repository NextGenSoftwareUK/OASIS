using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Connectors.QuickBooks;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Connectors.QuickBooks.Models;

namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Controllers
{
    /// <summary>
    /// Controller for QuickBooks OAuth2 authentication flow.
    /// </summary>
    [ApiController]
    [Route("api/shipexpro/quickbooks")]
    public class QuickBooksAuthController : ControllerBase
    {
        private readonly QuickBooksOAuthService _oauthService;

        public QuickBooksAuthController(QuickBooksOAuthService oauthService)
        {
            _oauthService = oauthService ?? throw new ArgumentNullException(nameof(oauthService));
        }

        /// <summary>
        /// Start OAuth flow - redirects merchant to QuickBooks authorization page.
        /// </summary>
        /// <param name="merchantId">Merchant identifier</param>
        /// <param name="state">Optional state parameter for CSRF protection</param>
        /// <returns>Authorization URL</returns>
        [HttpGet("authorize")]
        public ActionResult GetAuthorizationUrl([FromQuery] Guid merchantId, [FromQuery] string state = null)
        {
            var authUrl = _oauthService.GetAuthorizationUrl(merchantId, state);
            return Ok(new { AuthorizationUrl = authUrl });
        }

        /// <summary>
        /// OAuth callback endpoint - receives authorization code from QuickBooks.
        /// </summary>
        /// <param name="code">Authorization code</param>
        /// <param name="realmId">Company ID from QuickBooks</param>
        /// <param name="state">State parameter (should match the one sent)</param>
        /// <param name="merchantId">Merchant identifier</param>
        /// <returns>Success result</returns>
        [HttpGet("callback")]
        public async Task<ActionResult<OASISResult<bool>>> OAuthCallback(
            [FromQuery] string code,
            [FromQuery] string realmId,
            [FromQuery] string state,
            [FromQuery] Guid merchantId)
        {
            if (string.IsNullOrEmpty(code))
            {
                return BadRequest(new OASISResult<bool>
                {
                    IsError = true,
                    Message = "Authorization code is required"
                });
            }

            if (string.IsNullOrEmpty(realmId))
            {
                return BadRequest(new OASISResult<bool>
                {
                    IsError = true,
                    Message = "Realm ID is required"
                });
            }

            // Exchange code for tokens
            var tokenResult = await _oauthService.ExchangeCodeForTokensAsync(code, realmId);
            if (tokenResult.IsError)
            {
                return BadRequest(tokenResult);
            }

            // Store tokens securely
            var storeResult = await _oauthService.StoreTokensAsync(merchantId, tokenResult.Result);
            if (storeResult.IsError)
            {
                return BadRequest(storeResult);
            }

            return Ok(new OASISResult<bool>(true)
            {
                Message = "QuickBooks connection successful"
            });
        }

        /// <summary>
        /// Refresh access token for a merchant.
        /// </summary>
        /// <param name="merchantId">Merchant identifier</param>
        /// <returns>Success result</returns>
        [HttpPost("refresh-token")]
        public async Task<ActionResult<OASISResult<bool>>> RefreshToken([FromQuery] Guid merchantId)
        {
            // Get refresh token from secret vault (implementation depends on Agent F)
            // For now, return not implemented
            return StatusCode(501, new OASISResult<bool>
            {
                IsError = true,
                Message = "Token refresh requires secret vault service (Agent F)"
            });
        }
    }
}




