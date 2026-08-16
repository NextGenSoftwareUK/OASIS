using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Keys;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    /// <summary>Provider key management — link, generate and retrieve avatar keys and wallets.</summary>
    [Route("api/avatar")]
    [ApiController]
    public class AvatarKeyController : OASISControllerBase
    {
        private static KeyManager Keys => KeyManager.Instance;

        /// <summary>
        /// Links a provider public key (wallet address) to an avatar identified by their avatar ID.
        /// </summary>
        [HttpPost("link-public-key/by-id")]
        [Authorize]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<IProviderWallet>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status400BadRequest)]
        public OASISHttpResponseMessage<IProviderWallet> LinkProviderPublicKeyToAvatarById([FromBody] LinkProviderKeyToAvatarParams p)
        {
            if (!TryParseAvatarId(p?.AvatarID, out var avatarId, out var err))
                return HttpResponseHelper.FormatResponse(new OASISResult<IProviderWallet> { IsError = true, Message = err }, HttpStatusCode.BadRequest);
            if (!TryParseProviderType(p.ProviderType, out var providerType, out err))
                return HttpResponseHelper.FormatResponse(new OASISResult<IProviderWallet> { IsError = true, Message = err }, HttpStatusCode.BadRequest);

            var result = Keys.LinkProviderPublicKeyToAvatarById(p.WalletId, avatarId, providerType, p.ProviderKey, p.WalletAddress, p.WalletAddressSegwitP2SH, p.ShowSecretRecoveryWords);
            return HttpResponseHelper.FormatResponse(result, result.IsError ? HttpStatusCode.BadRequest : HttpStatusCode.OK);
        }

        /// <summary>
        /// Links a provider public key (wallet address) to an avatar identified by their username.
        /// </summary>
        [HttpPost("link-public-key/by-username")]
        [Authorize]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<IProviderWallet>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status400BadRequest)]
        public OASISHttpResponseMessage<IProviderWallet> LinkProviderPublicKeyToAvatarByUsername([FromBody] LinkProviderKeyToAvatarParams p)
        {
            if (string.IsNullOrWhiteSpace(p?.AvatarUsername))
                return HttpResponseHelper.FormatResponse(new OASISResult<IProviderWallet> { IsError = true, Message = "AvatarUsername is required." }, HttpStatusCode.BadRequest);
            if (!TryParseProviderType(p.ProviderType, out var providerType, out var err))
                return HttpResponseHelper.FormatResponse(new OASISResult<IProviderWallet> { IsError = true, Message = err }, HttpStatusCode.BadRequest);

            var result = Keys.LinkProviderPublicKeyToAvatarByUsername(p.WalletId, p.AvatarUsername, providerType, p.ProviderKey, p.WalletAddress, p.WalletAddressSegwitP2SH, p.ShowSecretRecoveryWords);
            return HttpResponseHelper.FormatResponse(result, result.IsError ? HttpStatusCode.BadRequest : HttpStatusCode.OK);
        }

        /// <summary>
        /// Generates a new private/public key pair with a wallet address for the given provider type.
        /// </summary>
        [HttpPost("generate-key-pair")]
        [Authorize]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<IKeyPairAndWallet>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status400BadRequest)]
        public OASISHttpResponseMessage<IKeyPairAndWallet> GenerateKeyPairWithWalletAddress([FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = Keys.GenerateKeyPairWithWalletAddress(providerType);
            return HttpResponseHelper.FormatResponse(result, result.IsError ? HttpStatusCode.BadRequest : HttpStatusCode.OK);
        }

        /// <summary>
        /// Gets the unique storage key for an avatar (by avatar ID) for the given provider type.
        /// </summary>
        [HttpGet("provider-storage-key/by-id/{avatarId}/{providerType}")]
        [Authorize]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status400BadRequest)]
        public OASISHttpResponseMessage<string> GetProviderStorageKeyById(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            var result = Keys.GetProviderUniqueStorageKeyForAvatarById(avatarId, providerType);
            return HttpResponseHelper.FormatResponse(result, result.IsError ? HttpStatusCode.BadRequest : HttpStatusCode.OK);
        }

        /// <summary>
        /// Gets the unique storage key for an avatar (by username) for the given provider type.
        /// </summary>
        [HttpGet("provider-storage-key/by-username/{username}/{providerType}")]
        [Authorize]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status400BadRequest)]
        public OASISHttpResponseMessage<string> GetProviderStorageKeyByUsername(string username, ProviderType providerType = ProviderType.Default)
        {
            var result = Keys.GetProviderUniqueStorageKeyForAvatarByUsername(username, providerType);
            return HttpResponseHelper.FormatResponse(result, result.IsError ? HttpStatusCode.BadRequest : HttpStatusCode.OK);
        }

        /// <summary>
        /// Gets all public keys stored on an avatar (by avatar ID) for the given provider type.
        /// </summary>
        [HttpGet("provider-public-keys/by-id/{avatarId}/{providerType}")]
        [Authorize]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<List<string>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status400BadRequest)]
        public OASISHttpResponseMessage<List<string>> GetProviderPublicKeysById(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            var result = Keys.GetProviderPublicKeysForAvatarById(avatarId, providerType);
            return HttpResponseHelper.FormatResponse(result, result.IsError ? HttpStatusCode.BadRequest : HttpStatusCode.OK);
        }

        /// <summary>
        /// Gets all public keys stored on an avatar (by username) for the given provider type.
        /// </summary>
        [HttpGet("provider-public-keys/by-username/{username}/{providerType}")]
        [Authorize]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<List<string>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status400BadRequest)]
        public OASISHttpResponseMessage<List<string>> GetProviderPublicKeysByUsername(string username, ProviderType providerType = ProviderType.Default)
        {
            var result = Keys.GetProviderPublicKeysForAvatarByUsername(username, providerType);
            return HttpResponseHelper.FormatResponse(result, result.IsError ? HttpStatusCode.BadRequest : HttpStatusCode.OK);
        }

        /// <summary>
        /// Gets all private keys stored on an avatar (by avatar ID) for the given provider type. Wizard only.
        /// </summary>
        [HttpGet("provider-private-keys/by-id/{avatarId}/{providerType}")]
        [Authorize]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<List<string>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status400BadRequest)]
        public OASISHttpResponseMessage<List<string>> GetProviderPrivateKeysById(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            var result = Keys.GetProviderPrivateKeysForAvatarById(avatarId, providerType);
            return HttpResponseHelper.FormatResponse(result, result.IsError ? HttpStatusCode.BadRequest : HttpStatusCode.OK);
        }

        /// <summary>
        /// Gets all private keys stored on an avatar (by username) for the given provider type. Wizard only.
        /// </summary>
        [HttpGet("provider-private-keys/by-username/{username}/{providerType}")]
        [Authorize]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<List<string>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status400BadRequest)]
        public OASISHttpResponseMessage<List<string>> GetProviderPrivateKeysByUsername(string username, ProviderType providerType = ProviderType.Default)
        {
            var result = Keys.GetProviderPrivateKeysForAvatarByUsername(username, providerType);
            return HttpResponseHelper.FormatResponse(result, result.IsError ? HttpStatusCode.BadRequest : HttpStatusCode.OK);
        }

        private static bool TryParseAvatarId(string raw, out Guid id, out string error)
        {
            if (string.IsNullOrWhiteSpace(raw) || !Guid.TryParse(raw, out id))
            {
                id = Guid.Empty;
                error = $"AvatarID '{raw}' is not a valid GUID.";
                return false;
            }
            error = null;
            return true;
        }

        private static bool TryParseProviderType(string raw, out ProviderType providerType, out string error)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                providerType = ProviderType.Default;
                error = null;
                return true;
            }
            if (!Enum.TryParse(raw, true, out providerType))
            {
                error = $"ProviderType '{raw}' is invalid.";
                return false;
            }
            error = null;
            return true;
        }
    }
}
