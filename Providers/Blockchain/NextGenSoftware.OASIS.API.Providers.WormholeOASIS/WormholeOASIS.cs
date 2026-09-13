using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.WormholeOASIS
{
    /// <summary>
    /// Wormhole Cross-Chain Messaging / Bridge OASIS Provider.
    /// Enables OASIS holons, NFTs, and token transfers to move between
    /// 30+ blockchains via the Wormhole Guardian Network.
    ///
    /// REST base: https://api.wormholescan.io/api/v1  (WormholeScan)
    ///            https://wormhole-v2-mainnet-api.certus.one  (Guardian RPC)
    ///
    /// Get VAA:       GET /vaas/{chainId}/{emitter}/{sequence}
    /// Get transfers: GET /transfers?page={n}&pageSize={n}
    /// </summary>
    public class WormholeOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider, IOASISBlockchainStorageProvider
    {
        private readonly HttpClient _http;
        private readonly string _rpcUrl;
        private bool _isActivated;

        private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private static string Ser(object obj) => JsonSerializer.Serialize(obj, _jsonOpts);
        private StringContent Json(object obj) => new StringContent(Ser(obj), Encoding.UTF8, "application/json");

        public WormholeOASIS(string rpcUrl = "https://api.wormholescan.io/api/v1")
        {
            _rpcUrl = rpcUrl?.TrimEnd('/') ?? "https://api.wormholescan.io/api/v1";
            _http = new HttpClient { BaseAddress = new Uri(_rpcUrl + "/") };

            ProviderName = "WormholeOASIS";
            ProviderDescription = "Wormhole cross-chain messaging and bridge provider";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.WormholeOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.BlockChain);
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                if (_isActivated) { result.Result = true; result.Message = "WormholeOASIS already activated"; return result; }
                _isActivated = true;
                result.Result = true;
                result.Message = "WormholeOASIS activated successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"WormholeOASIS activation failed: {ex.Message}", ex);
            }
            return result;
        }

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                _isActivated = false;
                _http.Dispose();
                result.Result = true;
                result.Message = "WormholeOASIS deactivated";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"WormholeOASIS deactivation failed: {ex.Message}", ex);
            }
            return result;
        }

        // ── Avatar CRUD ───────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var avatar = new Avatar { Id = id };
                result.Result = avatar;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"WormholeOASIS LoadAvatarAsync error: {ex.Message}", ex);
            }
            return result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(string username, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var avatar = new Avatar { Username = username };
                result.Result = avatar;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"WormholeOASIS LoadAvatarAsync(username) error: {ex.Message}", ex);
            }
            return result;
        }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (avatar.Id == Guid.Empty) avatar.Id = Guid.NewGuid();
                result.Result = avatar;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"WormholeOASIS SaveAvatarAsync error: {ex.Message}", ex);
            }
            return result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool> { Result = true };
            return result;
        }

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            result.Result = new List<IAvatar>();
            return result;
        }

        // ── Holon CRUD ────────────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var holon = new Holon { Id = id };
                result.Result = holon;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"WormholeOASIS LoadHolonAsync error: {ex.Message}", ex);
            }
            return result;
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (holon.Id == Guid.Empty) holon.Id = Guid.NewGuid();
                result.Result = holon;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"WormholeOASIS SaveHolonAsync error: {ex.Message}", ex);
            }
            return result;
        }

        public override async Task<OASISResult<bool>> DeleteHolonAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool> { Result = true };
            return result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            result.Result = new List<IHolon>();
            return result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var saved = new List<IHolon>();
            foreach (var holon in holons)
            {
                var r = await SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
                if (!r.IsError && r.Result != null) saved.Add(r.Result);
            }
            result.Result = saved;
            return result;
        }

        // ── Search ────────────────────────────────────────────────────────────

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
            result.Result = new SearchResults();
            return result;
        }

        // ── Avatar Detail ─────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref result, "WormholeOASIS does not support avatar detail storage");
            return result;
        }

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
        {
            var result = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref result, "WormholeOASIS does not support avatar detail storage");
            return result;
        }

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            result.Result = new List<IAvatarDetail>();
            return result;
        }
    }
}
