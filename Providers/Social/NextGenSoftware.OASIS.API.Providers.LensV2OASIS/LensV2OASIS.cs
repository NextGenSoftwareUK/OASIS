using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
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

namespace NextGenSoftware.OASIS.API.Providers.LensV2OASIS
{
    /// <summary>
    /// OASIS provider for Lens Protocol v2 — on-chain social graph on Polygon zkEVM.
    /// API: https://api-v2.lens.dev/ (GraphQL)
    /// Public reads require no auth; writes require an access token.
    /// </summary>
    public class LensV2OASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider
    {
        private readonly HttpClient _http;
        private readonly string _accessToken;
        private bool _isActivated;
        private const string LensApi = "https://api-v2.lens.dev/";

        public LensV2OASIS(string accessToken = "")
        {
            _accessToken = accessToken;
            _http = new HttpClient { BaseAddress = new Uri(LensApi) };
            _http.DefaultRequestHeaders.Add("Accept", "application/json");
            if (!string.IsNullOrEmpty(accessToken))
                _http.DefaultRequestHeaders.Add("x-access-token", accessToken);

            ProviderName = "LensV2OASIS";
            ProviderDescription = "Lens Protocol v2 social graph provider — profiles and publications as holons";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.LensV2OASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Network);
            ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Network));
        }

        private async Task<JsonElement> GraphQLAsync(string query)
        {
            var payload = new { query };
            var response = await _http.PostAsJsonAsync("", payload);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<JsonElement>();
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                var body = await GraphQLAsync("{ ping }");
                _isActivated = true; result.Result = true; result.Message = "LensV2OASIS activated.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }
        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;
        public override async Task<OASISResult<bool>> DeActivateProviderAsync() { _isActivated = false; return await Task.FromResult(new OASISResult<bool>(true)); }
        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        // ─── Avatars (Lens profiles) ──────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            // providerKey = Lens handle e.g. "lens/stani"
            var result = new OASISResult<IAvatar>();
            try
            {
                var q = $@"{{ profile(request: {{forHandle: ""{providerKey}""}}) {{ id handle {{ fullHandle }} ownedBy {{ address }} stats {{ followers following }} }} }}";
                var body = await GraphQLAsync(q);
                if (body.TryGetProperty("data", out var data) && data.TryGetProperty("profile", out var profile) && profile.ValueKind != JsonValueKind.Null)
                {
                    var avatar = new Avatar();
                    avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.LensV2OASIS] = providerKey;
                    if (profile.TryGetProperty("id", out var id)) avatar.MetaData["lens_profile_id"] = id.GetString() ?? "";
                    if (profile.TryGetProperty("handle", out var h) && h.TryGetProperty("fullHandle", out var fh)) avatar.Username = fh.GetString();
                    if (profile.TryGetProperty("ownedBy", out var ob) && ob.TryGetProperty("address", out var addr)) avatar.MetaData["lens_address"] = addr.GetString() ?? "";
                    if (profile.TryGetProperty("stats", out var stats))
                    {
                        if (stats.TryGetProperty("followers", out var f)) avatar.MetaData["lens_followers"] = f.GetInt32().ToString();
                        if (stats.TryGetProperty("following", out var fo)) avatar.MetaData["lens_following"] = fo.GetInt32().ToString();
                    }
                    result.Result = avatar;
                }
                else OASISErrorHandling.HandleError(ref result, $"LensV2OASIS: Profile '{providerKey}' not found.");
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }
        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0) => LoadAvatarByProviderKeyAsync(providerKey, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
            => await LoadAvatarByProviderKeyAsync(avatarUsername, version);
        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0) => LoadAvatarByUsernameAsync(avatarUsername, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        { var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, "LensV2OASIS: Use LoadAvatarByProviderKey(handle)."); return await Task.FromResult(r); }
        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0) => LoadAvatarAsync(id, version).Result;
        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        { var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, "LensV2OASIS: Use LoadAvatarByProviderKey(handle)."); return await Task.FromResult(r); }
        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0) => LoadAvatarByEmailAsync(avatarEmail, version).Result;
        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        { var r = new OASISResult<IEnumerable<IAvatar>>(); OASISErrorHandling.HandleError(ref r, "LensV2OASIS: LoadAllAvatars not supported."); return await Task.FromResult(r); }
        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0) => LoadAllAvatarsAsync(version).Result;
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        { var r = new OASISResult<IAvatarDetail>(); OASISErrorHandling.HandleError(ref r, "LensV2OASIS: Not supported."); return await Task.FromResult(r); }
        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) => LoadAvatarDetailAsync(id, version).Result;
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
        { var r = new OASISResult<IAvatarDetail>(); OASISErrorHandling.HandleError(ref r, "LensV2OASIS: Not supported."); return await Task.FromResult(r); }
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0) => LoadAvatarDetailByEmailAsync(avatarEmail, version).Result;
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
        { var r = new OASISResult<IAvatarDetail>(); OASISErrorHandling.HandleError(ref r, "LensV2OASIS: Not supported."); return await Task.FromResult(r); }
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0) => LoadAvatarDetailByUsernameAsync(avatarUsername, version).Result;
        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        { var r = new OASISResult<IEnumerable<IAvatarDetail>>(); OASISErrorHandling.HandleError(ref r, "LensV2OASIS: Not supported."); return await Task.FromResult(r); }
        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0) => LoadAllAvatarDetailsAsync(version).Result;
        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        { var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, "LensV2OASIS: Profiles are created on-chain via Lens contract."); return await Task.FromResult(r); }
        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar) => SaveAvatarAsync(avatar).Result;
        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatar)
        { var r = new OASISResult<IAvatarDetail>(); OASISErrorHandling.HandleError(ref r, "LensV2OASIS: Not supported."); return await Task.FromResult(r); }
        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatar) => SaveAvatarDetailAsync(avatar).Result;
        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "LensV2OASIS: Not supported."); return await Task.FromResult(r); }
        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) => DeleteAvatarAsync(id, softDelete).Result;
        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "LensV2OASIS: Not supported."); return await Task.FromResult(r); }
        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true) => DeleteAvatarAsync(providerKey, softDelete).Result;
        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "LensV2OASIS: Not supported."); return await Task.FromResult(r); }
        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true) => DeleteAvatarByEmailAsync(avatarEmail, softDelete).Result;
        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "LensV2OASIS: Not supported."); return await Task.FromResult(r); }
        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true) => DeleteAvatarByUsernameAsync(avatarUsername, softDelete).Result;

        // ─── Holons (publications) ────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var q = $@"{{ publication(request: {{forId: ""{providerKey}""}}) {{ id __typename ... on Post {{ metadata {{ ... on TextOnlyMetadataV3 {{ content }} }} }} }} }}";
                var body = await GraphQLAsync(q);
                if (body.TryGetProperty("data", out var data) && data.TryGetProperty("publication", out var pub) && pub.ValueKind != JsonValueKind.Null)
                {
                    var holon = new Holon();
                    holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.LensV2OASIS] = providerKey;
                    if (pub.TryGetProperty("id", out var id)) holon.Name = id.GetString();
                    if (pub.TryGetProperty("metadata", out var md) && md.TryGetProperty("content", out var c)) holon.Description = c.GetString();
                    result.Result = holon;
                }
                else OASISErrorHandling.HandleError(ref result, $"LensV2OASIS: Publication '{providerKey}' not found.");
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }
        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        { var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, "LensV2OASIS: Use LoadHolonAsync(publicationId)."); return await Task.FromResult(r); }
        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            // providerKey = Lens profile id
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var q = $@"{{ publications(request: {{where: {{from: [""{providerKey}""], publicationTypes: [POST]}}, limit: Fifty}}) {{ items {{ id __typename }} }} }}";
                var body = await GraphQLAsync(q);
                var holons = new List<IHolon>();
                if (body.TryGetProperty("data", out var data) && data.TryGetProperty("publications", out var pubs) && pubs.TryGetProperty("items", out var items))
                    foreach (var item in items.EnumerateArray())
                    {
                        var holon = new Holon();
                        if (item.TryGetProperty("id", out var id)) { holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.LensV2OASIS] = id.GetString() ?? ""; holon.Name = id.GetString(); }
                        holons.Add(holon);
                    }
                result.Result = holons;
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "LensV2OASIS: Use LoadHolonsForParentAsync(profileId)."); return await Task.FromResult(r); }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "LensV2OASIS: LoadAllHolons not supported — too broad. Query by profile."); return await Task.FromResult(r); }
        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            if (string.IsNullOrEmpty(_accessToken))
            { var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, "LensV2OASIS: Write operations require an access token from Lens auth."); return r; }
            // Build createOnchainPostTypedData mutation
            var result = new OASISResult<IHolon>();
            try
            {
                var content = holon.Description ?? holon.Name ?? "";
                var q = $@"mutation {{ createOnchainPostTypedData(request: {{contentURI: ""ar://{Uri.EscapeDataString(content)}""}}) {{ id }} }}";
                var body = await GraphQLAsync(q);
                if (body.TryGetProperty("data", out var data) && data.TryGetProperty("createOnchainPostTypedData", out var post) && post.TryGetProperty("id", out var id))
                { holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.LensV2OASIS] = id.GetString() ?? ""; result.Result = holon; }
                else OASISErrorHandling.HandleError(ref result, "LensV2OASIS: createOnchainPostTypedData failed.");
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }
        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "LensV2OASIS: Bulk save not supported."); return await Task.FromResult(r); }
        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        { var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, "LensV2OASIS: On-chain publications are immutable."); return await Task.FromResult(r); }
        public override OASISResult<IHolon> DeleteHolon(Guid id) => DeleteHolonAsync(id).Result;
        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        { var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, "LensV2OASIS: On-chain publications are immutable."); return await Task.FromResult(r); }
        public override OASISResult<IHolon> DeleteHolon(string providerKey) => DeleteHolonAsync(providerKey).Result;

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
            try
            {
                var query = searchParams.SearchGroups?.OfType<SearchTextGroup>().FirstOrDefault()?.SearchQuery ?? "";
                var q = $@"{{ searchProfiles(request: {{query: ""{query}"", limit: Ten}}) {{ items {{ id handle {{ fullHandle }} }} }} }}";
                var body = await GraphQLAsync(q);
                var holons = new List<IHolon>();
                if (body.TryGetProperty("data", out var data) && data.TryGetProperty("searchProfiles", out var sp) && sp.TryGetProperty("items", out var items))
                    foreach (var item in items.EnumerateArray())
                    {
                        var holon = new Holon();
                        if (item.TryGetProperty("id", out var id)) holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.LensV2OASIS] = id.GetString() ?? "";
                        if (item.TryGetProperty("handle", out var h) && h.TryGetProperty("fullHandle", out var fh)) holon.Name = fh.GetString();
                        holons.Add(holon);
                    }
                var sr = new SearchResults(); sr.SearchResultHolons = holons; result.Result = sr;
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }
        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
            => SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "LensV2OASIS: MetaData search not supported."); return await Task.FromResult(r); }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "LensV2OASIS: MetaData search not supported."); return await Task.FromResult(r); }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        // ─── Import / Export ──────────────────────────────────────────────────────
        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "LensV2OASIS: Import not supported."); return await Task.FromResult(r); }
        public override OASISResult<bool> Import(IEnumerable<IHolon> holons) => ImportAsync(holons).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "LensV2OASIS: Export not supported."); return await Task.FromResult(r); }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0) => ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "LensV2OASIS: Export not supported."); return await Task.FromResult(r); }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0) => ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "LensV2OASIS: Export not supported."); return await Task.FromResult(r); }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0) => ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "LensV2OASIS: Export not supported."); return await Task.FromResult(r); }
        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0) => ExportAllAsync(version).Result;

        // ─── IOASISNETProvider ────────────────────────────────────────────────────
        public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        { var r = new OASISResult<IEnumerable<IAvatar>>(); OASISErrorHandling.HandleError(ref r, "LensV2OASIS: Geolocation not supported."); return r; }
        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
        { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "LensV2OASIS: Geolocation not supported."); return r; }
    }
}
