using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.Web6.Core.Managers;

namespace NextGenSoftware.OASIS.Web6.WebAPI.Controllers
{
    /// <summary>
    /// Per-avatar provider API key vault.
    /// Keys are stored encrypted in COSMIC ORM avatar MetaData — never logged or returned in plaintext.
    /// POST   /v1/keys           — store/replace a key for a provider
    /// GET    /v1/keys           — list providers that have a stored key (no values returned)
    /// DELETE /v1/keys/{provider}— remove a stored key
    /// </summary>
    [ApiController]
    [Route("v1/keys")]
    public class KeysController : Web6ControllerBase
    {
        public record UpsertKeyRequest(string Provider, string ApiKey);

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> UpsertKey([FromBody] UpsertKeyRequest body)
        {
            if (string.IsNullOrWhiteSpace(body?.Provider) || string.IsNullOrWhiteSpace(body.ApiKey))
                return BadRequest(new { error = "Provider and ApiKey are required." });

            var vault = new KeyVaultManager(AvatarId, OASISDNA);
            var result = await vault.SaveProviderKeyAsync(body.Provider.ToLowerInvariant(), body.ApiKey);
            return result.IsError ? BadRequest(result) : NoContent();
        }

        [HttpGet]
        [ProducesResponseType(typeof(System.Collections.Generic.List<string>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ListProviders()
        {
            var vault = new KeyVaultManager(AvatarId, OASISDNA);
            var result = await vault.ListStoredProvidersAsync();
            return result.IsError ? BadRequest(result) : Ok(result.Result);
        }

        [HttpDelete("{provider}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> DeleteKey(string provider)
        {
            if (string.IsNullOrWhiteSpace(provider))
                return BadRequest(new { error = "Provider is required." });

            var vault = new KeyVaultManager(AvatarId, OASISDNA);
            var result = await vault.DeleteProviderKeyAsync(provider.ToLowerInvariant());
            return result.IsError ? BadRequest(result) : NoContent();
        }
    }
}
