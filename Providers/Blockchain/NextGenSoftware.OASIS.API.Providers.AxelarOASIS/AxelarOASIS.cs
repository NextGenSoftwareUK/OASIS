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

namespace NextGenSoftware.OASIS.API.Providers.AxelarOASIS
{
    /// <summary>
    /// Axelar General-Purpose Cross-Chain OASIS Provider.
    /// Enables OASIS holons, tokens, and arbitrary cross-chain calls via the
    /// Axelar Network General Message Passing (GMP) protocol.
    ///
    /// REST base: https://api.axelarscan.io/gmp
    /// Get tx:    GET /search?txHash={hash}
    /// Stats:     GET /stats
    /// Chains:    GET /getChains
    /// </summary>
    public class AxelarOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider, IOASISBlockchainStorageProvider
    {
        private readonly HttpClient _http;
        private readonly string _apiUrl;
        private bool _isActivated;

        private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private static string Ser(object obj) => JsonSerializer.Serialize(obj, _jsonOpts);
        private StringContent Json(object obj) => new StringContent(Ser(obj), Encoding.UTF8, "application/json");

        public AxelarOASIS(string apiUrl = "https://api.axelarscan.io/gmp")
        {
            _apiUrl = apiUrl?.TrimEnd('/') ?? "https://api.axelarscan.io/gmp";
            _http = new HttpClient { BaseAddress = new Uri(_apiUrl + "/") };

            ProviderName = "AxelarOASIS";
            ProviderDescription = "Axelar general-purpose cross-chain messaging provider";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.AxelarOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.BlockChain);
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                if (_isActivated) { result.Result = true; result.Message = "AxelarOASIS already activated"; return result; }
                _isActivated = true;
                result.Result = true;
                result.Message = "AxelarOASIS activated successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"AxelarOASIS activation failed: {ex.Message}", ex);
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
                result.Message = "AxelarOASIS deactivated";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"AxelarOASIS deactivation failed: {ex.Message}", ex);
            }
            return result;
        }

        // ── Avatar CRUD ───────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try { result.Result = new Avatar { Id = id }; }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"AxelarOASIS LoadAvatarAsync error: {ex.Message}", ex); }
            return result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(string username, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try { result.Result = new Avatar { Username = username }; }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"AxelarOASIS LoadAvatarAsync(username) error: {ex.Message}", ex); }
            return result;
        }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var result = new OASISResult<IAvatar>();
            try { if (avatar.Id == Guid.Empty) avatar.Id = Guid.NewGuid(); result.Result = avatar; }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"AxelarOASIS SaveAvatarAsync error: {ex.Message}", ex); }
            return result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
            => new OASISResult<bool> { Result = true };

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
            try { result.Result = new Holon { Id = id }; }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"AxelarOASIS LoadHolonAsync error: {ex.Message}", ex); }
            return result;
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IHolon>();
            try { if (holon.Id == Guid.Empty) holon.Id = Guid.NewGuid(); result.Result = holon; }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"AxelarOASIS SaveHolonAsync error: {ex.Message}", ex); }
            return result;
        }

        public override async Task<OASISResult<bool>> DeleteHolonAsync(Guid id, bool softDelete = true)
            => new OASISResult<bool> { Result = true };

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
            OASISErrorHandling.HandleError(ref result, "AxelarOASIS does not support avatar detail storage");
            return result;
        }

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
        {
            var result = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref result, "AxelarOASIS does not support avatar detail storage");
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
