using System.Threading;using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;using NextGenSoftware.Utilities;using System;using System.Collections.Generic;using System.Net.Http;using System.Threading.Tasks;using Newtonsoft.Json.Linq;using NextGenSoftware.OASIS.API.Core;using NextGenSoftware.OASIS.API.Core.Enums;using NextGenSoftware.OASIS.API.Core.Helpers;using NextGenSoftware.OASIS.API.Core.Holons;using NextGenSoftware.OASIS.API.Core.Interfaces;using NextGenSoftware.OASIS.API.Core.Interfaces.Search;using NextGenSoftware.OASIS.API.Core.Objects;using NextGenSoftware.OASIS.API.Core.Objects.Search;using NextGenSoftware.OASIS.Common;
namespace NextGenSoftware.OASIS.API.Providers.TheSandboxOASIS
{
    public class TheSandboxOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider, IOASISBlockchainStorageProvider
    {
        private readonly HttpClient _http;
        private bool _isActivated;
        public TheSandboxOASIS(string baseUrl = "https://api.sandbox.game/v1")
        {
            _http = new HttpClient { BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/") };
            ProviderName = "TheSandboxOASIS"; ProviderDescription = "The Sandbox metaverse LAND and user profile data provider.";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.TheSandboxOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Spatial);
        }
        private async Task<JObject> GetAsync(string path) { var r = await _http.GetAsync(path); r.EnsureSuccessStatusCode(); return JObject.Parse(await r.Content.ReadAsStringAsync()); }
        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var r = new OASISResult<bool>();
            try { await GetAsync("lands?limit=1"); _isActivated = true; r.Result = true; r.Message = "TheSandboxOASIS activated"; }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"The Sandbox activation failed: {ex.Message}", ex); }
            return r;
        }
        public override async Task<OASISResult<bool>> DeActivateProviderAsync() { _isActivated = false; _http.Dispose(); return new OASISResult<bool> { Result = true }; }
        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;
        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;
        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string username, int v = 0)
        {
            var r = new OASISResult<IAvatar>();
            try
            {
                var json = await GetAsync($"users/{Uri.EscapeDataString(username)}");
                var user = json["user"] ?? json;
                var avatar = new Avatar { Username = username };
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.TheSandboxOASIS] = username;
                avatar.FirstName = user["name"]?.ToString() ?? username;
                avatar.MetaData["wallet_address"] = user["ethAddress"]?.ToString() ?? "";
                avatar.MetaData["avatar_url"] = user["avatarUrl"]?.ToString() ?? "";
                avatar.MetaData["land_count"] = user["landCount"]?.ToString() ?? "0";
                r.Result = avatar;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"The Sandbox LoadAvatar failed: {ex.Message}", ex); }
            return r;
        }
        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string key, bool lc = true, bool rec = true, int md = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IHolon>();
            try
            {
                // key = land token ID
                var json = await GetAsync($"lands/{Uri.EscapeDataString(key)}");
                var land = json["land"] ?? json;
                var holon = new Holon { Name = land["name"]?.ToString() ?? $"LAND #{key}" };
                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.TheSandboxOASIS] = key;
                holon.MetaData["token_id"] = key;
                holon.MetaData["x"] = land["x"]?.ToString() ?? "0";
                holon.MetaData["y"] = land["y"]?.ToString() ?? "0";
                holon.MetaData["owner"] = land["owner"]?["ethAddress"]?.ToString() ?? "";
                holon.MetaData["description"] = land["description"]?.ToString() ?? "";
                r.Result = holon;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"The Sandbox LoadHolon failed: {ex.Message}", ex); }
            return r;
        }
        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon h, bool sc = true, bool rec = true, int md = 0, bool coe = true, bool scop = false) { if (h.Id == Guid.Empty) h.Id = Guid.NewGuid(); return new OASISResult<IHolon> { Result = h, Message = "The Sandbox is a read-only data API; LAND must be transferred on-chain." }; }
        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int v = 0) { var r = new OASISResult<IAvatar>(); r.Result = new Avatar { Id = id }; return r; }
        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string u, int v = 0) => await LoadAvatarByProviderKeyAsync(u, v);
        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string e, int v = 0) { var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar a) { if (a.Id == Guid.Empty) a.Id = Guid.NewGuid(); return new OASISResult<IAvatar> { Result = a }; }
        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool s = true) => new OASISResult<bool> { Result = true };
        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string k, bool s = true) => new OASISResult<bool> { Result = true };
        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string e, bool s = true) => new OASISResult<bool> { Result = true };
        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string u, bool s = true) => new OASISResult<bool> { Result = true };
        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int v = 0) => new OASISResult<IEnumerable<IAvatar>> { Result = new List<IAvatar>() };
        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool lc = true, bool rec = true, int md = 0, bool coe = true, bool lcfp = false, int v = 0) { var r = new OASISResult<IHolon>(); r.Result = new Holon { Id = id }; return r; }
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int v = 0) { var r = new OASISResult<IAvatarDetail>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string e, int v = 0) { var r = new OASISResult<IAvatarDetail>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string u, int v = 0) { var r = new OASISResult<IAvatarDetail>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail ad) => new OASISResult<IAvatarDetail> { Result = ad };
        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int v = 0) => new OASISResult<IEnumerable<IAvatarDetail>> { Result = new List<IAvatarDetail>() };
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType ht = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) => new OASISResult<IEnumerable<IHolon>> { Result = new List<IHolon>() };
        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool sc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool scop = false) { var l = new List<IHolon>(); foreach (var h in holons) { var sr = await SaveHolonAsync(h); if (sr.Result != null) l.Add(sr.Result); } return new OASISResult<IEnumerable<IHolon>> { Result = l }; }
        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams sp, bool lc = true, bool rec = true, int md = 0, bool coe = true, int v = 0) => new OASISResult<ISearchResults> { Result = new SearchResults() };
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) => new OASISResult<IEnumerable<IHolon>> { Result = new List<IHolon>() };
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string k, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) => new OASISResult<IEnumerable<IHolon>> { Result = new List<IHolon>() };
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string mk, string mv, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) => new OASISResult<IEnumerable<IHolon>> { Result = new List<IHolon>() };
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> m, MetaKeyValuePairMatchMode mm, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) => new OASISResult<IEnumerable<IHolon>> { Result = new List<IHolon>() };
        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id) => new OASISResult<IHolon> { Result = new Holon { Id = id } };
        public override OASISResult<IAvatar> LoadAvatar(Guid id, int v = 0) => LoadAvatarAsync(id, v).Result;
        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string k, int v = 0) => LoadAvatarByProviderKeyAsync(k, v).Result;
        public override OASISResult<IAvatar> LoadAvatarByUsername(string u, int v = 0) => LoadAvatarByUsernameAsync(u, v).Result;
        public override OASISResult<IAvatar> SaveAvatar(IAvatar a) => SaveAvatarAsync(a).Result;
        public override OASISResult<bool> DeleteAvatar(Guid id, bool s = true) => DeleteAvatarAsync(id, s).Result;
        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int v = 0) => LoadAllAvatarsAsync(v).Result;
        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int v = 0) => LoadAvatarDetailAsync(id, v).Result;
        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail ad) => SaveAvatarDetailAsync(ad).Result;
        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int v = 0) => LoadAllAvatarDetailsAsync(v).Result;
        public override OASISResult<IHolon> LoadHolon(Guid id, bool lc = true, bool rec = true, int md = 0, bool coe = true, bool lcfp = false, int v = 0) => LoadHolonAsync(id, lc, rec, md, coe, lcfp, v).Result;
        public override OASISResult<IHolon> LoadHolon(string k, bool lc = true, bool rec = true, int md = 0, bool coe = true, bool lcfp = false, int v = 0) => LoadHolonAsync(k, lc, rec, md, coe, lcfp, v).Result;
        public override OASISResult<IHolon> SaveHolon(IHolon h, bool sc = true, bool rec = true, int md = 0, bool coe = true, bool scop = false) => SaveHolonAsync(h, sc, rec, md, coe, scop).Result;
        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> h, bool sc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool scop = false) => SaveHolonsAsync(h, sc, rec, md, cd, coe, scop).Result;
        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType ht = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) => LoadAllHolonsAsync(ht, lc, rec, md, cd, coe, lcfp, v).Result;
        public override OASISResult<ISearchResults> Search(ISearchParams sp, bool lc = true, bool rec = true, int md = 0, bool coe = true, int v = 0) => SearchAsync(sp, lc, rec, md, coe, v).Result;
        public override OASISResult<IHolon> DeleteHolon(Guid id) => DeleteHolonAsync(id).Result;
        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string k) { var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid id, int v = 0) { var r = new OASISResult<IEnumerable<IHolon>>(); r.Result = new List<IHolon>(); return r; }
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string u, int v = 0) { var r = new OASISResult<IEnumerable<IHolon>>(); r.Result = new List<IHolon>(); return r; }
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string e, int v = 0) { var r = new OASISResult<IEnumerable<IHolon>>(); r.Result = new List<IHolon>(); return r; }
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int v = 0) { var r = new OASISResult<IEnumerable<IHolon>>(); r.Result = new List<IHolon>(); return r; }
        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> h) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public override OASISResult<IAvatar> LoadAvatarByEmail(string e, int v = 0) => LoadAvatarByEmailAsync(e, v).Result;
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string e, int v = 0) => LoadAvatarDetailByEmailAsync(e, v).Result;
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string u, int v = 0) => LoadAvatarDetailByUsernameAsync(u, v).Result;
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) => LoadHolonsForParentAsync(id, t, lc, rec, md, cd, coe, lcfp, v).Result;
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string k, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) => LoadHolonsForParentAsync(k, t, lc, rec, md, cd, coe, lcfp, v).Result;
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string mk, string mv, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) => LoadHolonsByMetaDataAsync(mk, mv, t, lc, rec, md, cd, coe, lcfp, v).Result;
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> m, MetaKeyValuePairMatchMode mm, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) => LoadHolonsByMetaDataAsync(m, mm, t, lc, rec, md, cd, coe, lcfp, v).Result;
        public override OASISResult<IHolon> DeleteHolon(string k) { var r = DeleteHolonAsync(k).Result; return new OASISResult<IHolon> { IsError = r.IsError, Message = r.Message }; }
        public override OASISResult<bool> Import(IEnumerable<IHolon> h) => ImportAsync(h).Result;
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid id, int v = 0) => ExportAllDataForAvatarByIdAsync(id, v).Result;
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string u, int v = 0) => ExportAllDataForAvatarByUsernameAsync(u, v).Result;
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string e, int v = 0) => ExportAllDataForAvatarByEmailAsync(e, v).Result;
        public override OASISResult<IEnumerable<IHolon>> ExportAll(int v = 0) => ExportAllAsync(v).Result;
        public override OASISResult<bool> DeleteAvatar(string k, bool s = true) => DeleteAvatarAsync(k, s).Result;
        public override OASISResult<bool> DeleteAvatarByEmail(string e, bool s = true) => DeleteAvatarByEmailAsync(e, s).Result;
        public override OASISResult<bool> DeleteAvatarByUsername(string u, bool s = true) => DeleteAvatarByUsernameAsync(u, s).Result;
        public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long lat, long lng, int v = 0) { var r = new OASISResult<IEnumerable<IAvatar>>(); r.Result = new List<IAvatar>(); return r; }
        public async Task<OASISResult<IEnumerable<IAvatar>>> GetAvatarsNearMeAsync(long lat, long lng, int v = 0) { var r = new OASISResult<IEnumerable<IAvatar>>(); r.Result = new List<IAvatar>(); return r; }
        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long lat, long lng, int v = 0, HolonType t = HolonType.All) { var r = new OASISResult<IEnumerable<IHolon>>(); r.Result = new List<IHolon>(); return r; }
        public async Task<OASISResult<IEnumerable<IHolon>>> GetHolonsNearMeAsync(long lat, long lng, HolonType t, int v = 0) { var r = new OASISResult<IEnumerable<IHolon>>(); r.Result = new List<IHolon>(); return r; }
        public OASISResult<ITransactionResponse> SendToken(ISendWeb3TokenRequest req) { var r = new OASISResult<ITransactionResponse>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public async Task<OASISResult<ITransactionResponse>> SendTokenAsync(ISendWeb3TokenRequest req) { var r = new OASISResult<ITransactionResponse>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public OASISResult<ITransactionResponse> MintToken(IMintWeb3TokenRequest req) { var r = new OASISResult<ITransactionResponse>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public async Task<OASISResult<ITransactionResponse>> MintTokenAsync(IMintWeb3TokenRequest req) { var r = new OASISResult<ITransactionResponse>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public OASISResult<ITransactionResponse> BurnToken(IBurnWeb3TokenRequest req) { var r = new OASISResult<ITransactionResponse>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public async Task<OASISResult<ITransactionResponse>> BurnTokenAsync(IBurnWeb3TokenRequest req) { var r = new OASISResult<ITransactionResponse>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public OASISResult<ITransactionResponse> LockToken(ILockWeb3TokenRequest req) { var r = new OASISResult<ITransactionResponse>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public async Task<OASISResult<ITransactionResponse>> LockTokenAsync(ILockWeb3TokenRequest req) { var r = new OASISResult<ITransactionResponse>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public OASISResult<ITransactionResponse> UnlockToken(IUnlockWeb3TokenRequest req) { var r = new OASISResult<ITransactionResponse>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public async Task<OASISResult<ITransactionResponse>> UnlockTokenAsync(IUnlockWeb3TokenRequest req) { var r = new OASISResult<ITransactionResponse>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public OASISResult<double> GetBalance(IGetWeb3WalletBalanceRequest req) { var r = new OASISResult<double>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public async Task<OASISResult<double>> GetBalanceAsync(IGetWeb3WalletBalanceRequest req) { var r = new OASISResult<double>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public OASISResult<IList<IWalletTransaction>> GetTransactions(IGetWeb3TransactionsRequest req) { var r = new OASISResult<IList<IWalletTransaction>>(); r.Result = new List<IWalletTransaction>(); return r; }
        public async Task<OASISResult<IList<IWalletTransaction>>> GetTransactionsAsync(IGetWeb3TransactionsRequest req) { var r = new OASISResult<IList<IWalletTransaction>>(); r.Result = new List<IWalletTransaction>(); return r; }
        public OASISResult<IKeyPairAndWallet> GenerateKeyPair() { var r = new OASISResult<IKeyPairAndWallet>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public async Task<OASISResult<IKeyPairAndWallet>> GenerateKeyPairAsync() { var r = new OASISResult<IKeyPairAndWallet>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public async Task<OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>> CreateAccountAsync(CancellationToken token = default) { var r = new OASISResult<(string, string, string)>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public async Task<OASISResult<(string PublicKey, string PrivateKey)>> RestoreAccountAsync(string seedPhrase, CancellationToken token = default) { var r = new OASISResult<(string, string)>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public async Task<OASISResult<BridgeTransactionResponse>> WithdrawAsync(decimal amount, string senderAddr, string senderKey) { var r = new OASISResult<BridgeTransactionResponse>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public async Task<OASISResult<BridgeTransactionResponse>> DepositAsync(decimal amount, string receiverAddr) { var r = new OASISResult<BridgeTransactionResponse>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public async Task<OASISResult<BridgeTransactionStatus>> GetTransactionStatusAsync(string txHash, CancellationToken token = default) { var r = new OASISResult<BridgeTransactionStatus>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
    }
}
