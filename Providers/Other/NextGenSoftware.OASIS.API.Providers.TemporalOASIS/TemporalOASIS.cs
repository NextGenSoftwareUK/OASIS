using System;using System.Collections.Generic;using System.Threading.Tasks;using Temporalio.Client;using NextGenSoftware.OASIS.API.Core;using NextGenSoftware.OASIS.API.Core.Enums;using NextGenSoftware.OASIS.API.Core.Helpers;using NextGenSoftware.OASIS.API.Core.Holons;using NextGenSoftware.OASIS.API.Core.Interfaces;using NextGenSoftware.OASIS.API.Core.Interfaces.Search;using NextGenSoftware.OASIS.API.Core.Objects;using NextGenSoftware.OASIS.API.Core.Objects.Search;using NextGenSoftware.OASIS.Common;
namespace NextGenSoftware.OASIS.API.Providers.TemporalOASIS
{
    public class TemporalOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider, IOASISBlockchainStorageProvider
    {
        private readonly string _host;
        private readonly string _namespace;
        private readonly string _taskQueue;
        private ITemporalClient _client;
        private bool _isActivated;
        public TemporalOASIS(string host = "localhost:7233", string ns = "default", string taskQueue = "oasis-queue")
        {
            _host = host; _namespace = ns; _taskQueue = taskQueue;
            ProviderName = "TemporalOASIS"; ProviderDescription = "Temporal workflow orchestration platform state store provider.";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.TemporalOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Infrastructure);
        }
        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var r = new OASISResult<bool>();
            try
            {
                _client = await TemporalClient.ConnectAsync(new TemporalClientConnectOptions(_host) { Namespace = _namespace });
                _isActivated = true; r.Result = true; r.Message = $"TemporalOASIS activated — {_host}/{_namespace}";
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Temporal activation failed: {ex.Message}", ex); }
            return r;
        }
        public override async Task<OASISResult<bool>> DeActivateProviderAsync() { _isActivated = false; _client = null; return new OASISResult<bool> { Result = true }; }
        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;
        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;
        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string workflowId, int v = 0)
        {
            var r = new OASISResult<IAvatar>();
            try
            {
                var handle = _client.GetWorkflowHandle(workflowId);
                var desc = await handle.DescribeAsync();
                var avatar = new Avatar { Username = workflowId };
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.TemporalOASIS] = workflowId;
                avatar.MetaData["workflow_id"] = workflowId;
                avatar.MetaData["run_id"] = desc.RunId;
                avatar.MetaData["status"] = desc.Status.ToString();
                avatar.MetaData["workflow_type"] = desc.WorkflowType;
                r.Result = avatar;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Temporal LoadAvatar failed: {ex.Message}", ex); }
            return r;
        }
        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string key, bool lc = true, bool rec = true, int md = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IHolon>();
            try
            {
                var handle = _client.GetWorkflowHandle(key);
                var desc = await handle.DescribeAsync();
                var holon = new Holon { Name = $"Temporal Workflow {key}" };
                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.TemporalOASIS] = key;
                holon.MetaData["workflow_id"] = key;
                holon.MetaData["run_id"] = desc.RunId;
                holon.MetaData["status"] = desc.Status.ToString();
                holon.MetaData["task_queue"] = desc.TaskQueue;
                holon.MetaData["start_time"] = desc.StartTime.ToString("O");
                r.Result = holon;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Temporal LoadHolon failed: {ex.Message}", ex); }
            return r;
        }
        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon h, bool sc = true, bool rec = true, int md = 0, bool coe = true, bool scop = false)
        {
            var r = new OASISResult<IHolon>();
            try
            {
                if (h.Id == Guid.Empty) h.Id = Guid.NewGuid();
                var workflowType = h.MetaData.ContainsKey("workflow_type") ? h.MetaData["workflow_type"] : "OASISWorkflow";
                var workflowId = h.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.TemporalOASIS)
                    ? h.ProviderUniqueStorageKey[Core.Enums.ProviderType.TemporalOASIS]
                    : h.Id.ToString();
                var handle = await _client.StartWorkflowAsync(
                    workflowType,
                    new[] { h.Name },
                    new WorkflowOptions { Id = workflowId, TaskQueue = _taskQueue });
                h.ProviderUniqueStorageKey[Core.Enums.ProviderType.TemporalOASIS] = handle.Id;
                r.Result = h;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Temporal SaveHolon failed: {ex.Message}", ex); }
            return r;
        }
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
    }
}
