using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Common;
using System.Text.Json;
using System.Threading.Tasks;
using Google.Cloud.BigQuery.V2;
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

namespace NextGenSoftware.OASIS.API.Providers.BigQueryOASIS
{
    /// <summary>
    /// OASIS provider for Google BigQuery - the serverless data warehouse.
    ///
    /// Writes go through MERGE so they stay idempotent, and the dataset plus the three
    /// OASIS tables are created on activation if absent. Credentials come from the
    /// ambient Google application-default credentials.
    ///
    /// Tables: {project}.{dataset}.oasis_avatars / _avatar_details / _holons
    /// </summary>
    public partial class BigQueryOASIS : OASISStorageProviderBase, IOASISDBStorageProvider
    {
        /// <summary>
        /// Retains logical versions in the shared same-record history envelope.
        /// Existing archives continue to be preserved when this option is disabled.
        /// </summary>
        public bool IsVersionControlEnabled { get; set; }

        private readonly string _projectId;
        private readonly string _dataset;
        private BigQueryClient? _client;

        private const string CollAvatars = "oasis_avatars";
        private const string CollAvatarDetails = "oasis_avatar_details";
        private const string CollHolons = "oasis_holons";

        public BigQueryOASIS(string projectId, string dataset = "oasis")
        {
            _projectId = ValidateSqlIdentifier(projectId,nameof(projectId));
            _dataset = ValidateSqlIdentifier(dataset,nameof(dataset));
            ProviderName = "BigQueryOASIS";
            ProviderDescription = "Google BigQuery storage through the official managed client";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.BigQueryOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageLocalAndNetwork);
        }

        public string GetProviderVersion()=>typeof(BigQueryOASIS).Assembly.GetName().Version!.ToString();

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                _client = await BigQueryClient.CreateAsync(_projectId);
                await _client.GetOrCreateDatasetAsync(_dataset);

                foreach (var table in new[] { CollAvatars, CollAvatarDetails, CollHolons })
                {
                    await _client.ExecuteQueryAsync(
                        $@"CREATE TABLE IF NOT EXISTS `{_projectId}.{_dataset}.{table}` (
                             id STRING NOT NULL, doc STRING, username STRING,
                             email STRING, parent_holon_id STRING, updated_at TIMESTAMP)",
                        parameters: null);
                }

                IsProviderActivated = true;
                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"BigQueryOASIS: ActivateProviderAsync failed: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<bool> ActivateProvider()
            => ActivateProviderAsync().GetAwaiter().GetResult();

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            
            IsProviderActivated = false;
            result.Result = true;
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> DeActivateProvider()
            => DeActivateProviderAsync().GetAwaiter().GetResult();

        // ─── Avatars ────────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var doc = await GetDocAsync<Avatar>(CollAvatars, id.ToString(),version);
                if (doc == null)
                    OASISErrorHandling.HandleError(ref result, $"BigQueryOASIS: Avatar {id} not found.");
                else if (doc.IsDeleted)
                    OASISErrorHandling.HandleError(ref result, $"BigQueryOASIS: Avatar {id} has been deleted.");
                else
                    CompleteRead(result,doc);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"BigQueryOASIS: LoadAvatarAsync failed: {ex.Message}",ex);
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0)
            => LoadAvatarAsync(id, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string username, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var doc = await FindOneAsync<Avatar>(CollAvatars, "Username", username,version);
                if (doc == null || doc.IsDeleted)
                    OASISErrorHandling.HandleError(ref result, $"BigQueryOASIS: Avatar with username '{username}' not found.");
                else
                    CompleteRead(result,doc);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"BigQueryOASIS: LoadAvatarByUsernameAsync failed: {ex.Message}",ex);
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string username, int version = 0)
            => LoadAvatarByUsernameAsync(username, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string email, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var doc = await FindOneAsync<Avatar>(CollAvatars, "Email", email,version);
                if (doc == null || doc.IsDeleted)
                    OASISErrorHandling.HandleError(ref result, $"BigQueryOASIS: Avatar with email '{email}' not found.");
                else
                    CompleteRead(result,doc);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"BigQueryOASIS: LoadAvatarByEmailAsync failed: {ex.Message}",ex);
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string email, int version = 0)
            => LoadAvatarByEmailAsync(email, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                var docs = await GetAllDocsAsync<Avatar>(CollAvatars,version);
                CompleteRows<IAvatar>(result,docs.FindAll(a => !a.IsDeleted));
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"BigQueryOASIS: LoadAllAvatarsAsync failed: {ex.Message}",ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
            => LoadAllAvatarsAsync(version).GetAwaiter().GetResult();

        public override Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
            => SaveAcknowledgedRecordAsync(avatar,CollAvatars);

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar)
            => SaveAvatarAsync(avatar).GetAwaiter().GetResult();

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (softDelete)
                {
                    await SoftDeleteRecordAsync<Avatar>(CollAvatars,id);
                }
                else
                {
                    await DeleteDocAsync(CollAvatars, id.ToString());
                }
                result.Result = true;
                result.IsDeleted = true;
                result.DeletedCount = 1;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"BigQueryOASIS: DeleteAvatarAsync failed: {ex.Message}",ex);
            }
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
            => DeleteAvatarAsync(id, softDelete).GetAwaiter().GetResult();

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string username, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                var avatar = await FindOneAsync<Avatar>(CollAvatars, "Username", username);
                if (avatar == null)
                    OASISErrorHandling.HandleError(ref result, $"BigQueryOASIS: Avatar '{username}' not found.");
                else
                    return await DeleteAvatarAsync(avatar.Id, softDelete);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"BigQueryOASIS: DeleteAvatarByUsernameAsync failed: {ex.Message}",ex);
            }
            return result;
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string username, bool softDelete = true)
            => DeleteAvatarByUsernameAsync(username, softDelete).GetAwaiter().GetResult();

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string email, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                var avatar = await FindOneAsync<Avatar>(CollAvatars, "Email", email);
                if (avatar == null)
                    OASISErrorHandling.HandleError(ref result, $"BigQueryOASIS: Avatar with email '{email}' not found.");
                else
                    return await DeleteAvatarAsync(avatar.Id, softDelete);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"BigQueryOASIS: DeleteAvatarByEmailAsync failed: {ex.Message}",ex);
            }
            return result;
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string email, bool softDelete = true)
            => DeleteAvatarByEmailAsync(email, softDelete).GetAwaiter().GetResult();

        // ─── AvatarDetails ──────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var doc = await GetDocAsync<AvatarDetail>(CollAvatarDetails, id.ToString(),version);
                if (doc == null || doc.IsDeleted)
                    OASISErrorHandling.HandleError(ref result, $"BigQueryOASIS: AvatarDetail {id} not found.");
                else
                    CompleteRead(result,doc);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"BigQueryOASIS: LoadAvatarDetailAsync failed: {ex.Message}",ex);
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
            => LoadAvatarDetailAsync(id, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string username, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var doc = await FindOneAsync<AvatarDetail>(CollAvatarDetails, "Username", username,version);
                if (doc == null || doc.IsDeleted)
                    OASISErrorHandling.HandleError(ref result, $"BigQueryOASIS: AvatarDetail '{username}' not found.");
                else
                    CompleteRead(result,doc);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"BigQueryOASIS: LoadAvatarDetailByUsernameAsync failed: {ex.Message}",ex);
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string username, int version = 0)
            => LoadAvatarDetailByUsernameAsync(username, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string email, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var doc = await FindOneAsync<AvatarDetail>(CollAvatarDetails, "Email", email,version);
                if (doc == null || doc.IsDeleted)
                    OASISErrorHandling.HandleError(ref result, $"BigQueryOASIS: AvatarDetail '{email}' not found.");
                else
                    CompleteRead(result,doc);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"BigQueryOASIS: LoadAvatarDetailByEmailAsync failed: {ex.Message}",ex);
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string email, int version = 0)
            => LoadAvatarDetailByEmailAsync(email, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            try
            {
                var docs = await GetAllDocsAsync<AvatarDetail>(CollAvatarDetails,version);
                CompleteRows<IAvatarDetail>(result,docs.FindAll(detail => !detail.IsDeleted));
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"BigQueryOASIS: LoadAllAvatarDetailsAsync failed: {ex.Message}",ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
            => LoadAllAvatarDetailsAsync(version).GetAwaiter().GetResult();

        public override Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail detail)
            => SaveAcknowledgedRecordAsync(detail,CollAvatarDetails);

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail detail)
            => SaveAvatarDetailAsync(detail).GetAwaiter().GetResult();

        // ─── Holons ─────────────────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                ValidateGraphDepth(maxChildDepth);
                var doc = await GetDocAsync<Holon>(CollHolons, id.ToString(),version);
                if (doc == null)
                    OASISErrorHandling.HandleError(ref result, $"BigQueryOASIS: Holon {id} not found.");
                else if (doc.IsDeleted)
                    OASISErrorHandling.HandleError(ref result, $"BigQueryOASIS: Holon {id} has been deleted.");
                else
                    CompleteRead(result,(await ProjectGraphRootsAsync(new[]{doc},loadChildren,recursive,maxChildDepth,0,loadChildrenFromProvider)).Single());
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"BigQueryOASIS: LoadHolonAsync failed: {ex.Message}",ex);
            }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                ValidateGraphDepth(maxChildDepth,curentChildDepth);
                var all = await FindAllAsync<Holon>(CollHolons, "ParentHolonId", id.ToString(),version);
                CompleteRows<IHolon>(result,await ProjectGraphRootsAsync(all.FindAll(h => !h.IsDeleted && (holonType == HolonType.All || h.HolonType == holonType)),loadChildren,recursive,maxChildDepth,curentChildDepth,loadChildrenFromProvider,rootsAreChildren:true));
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"BigQueryOASIS: LoadHolonsForParentAsync failed: {ex.Message}",ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsForParentAsync(id, holonType, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                ValidateGraphDepth(maxChildDepth,curentChildDepth);
                var docs = await GetAllDocsAsync<Holon>(CollHolons,version);
                var active=docs.FindAll(h=>!h.IsDeleted);
                CompleteRows<IHolon>(result,await ProjectGraphRootsAsync(active.Where(h=>holonType == HolonType.All || h.HolonType == holonType),loadChildren,recursive,maxChildDepth,curentChildDepth,loadChildrenFromProvider,providerPool:version==0?active:null));
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"BigQueryOASIS: LoadAllHolonsAsync failed: {ex.Message}",ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadAllHolonsAsync(holonType, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).GetAwaiter().GetResult();

        public override Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveSelectedGraphAsync(holon,saveChildren,recursive,maxChildDepth);

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).GetAwaiter().GetResult();

        public override Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => WriteGraphAsync(holons,saveChildren,recursive,maxChildDepth,curentChildDepth);

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).GetAwaiter().GetResult();

        public async Task<OASISResult<bool>> DeleteHolonSoftAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (softDelete)
                {
                    await SoftDeleteRecordAsync<Holon>(CollHolons,id);
                }
                else
                {
                    await DeleteDocAsync(CollHolons, id.ToString());
                }
                result.Result = true;
                result.IsDeleted = true;
                result.DeletedCount = 1;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"BigQueryOASIS: DeleteHolonAsync failed: {ex.Message}",ex);
            }
            return result;
        }

        public OASISResult<bool> DeleteHolonSoft(Guid id, bool softDelete = true)
            => DeleteHolonSoftAsync(id, softDelete).GetAwaiter().GetResult();

        // ─── Remaining IOASISStorageProvider surface ─────────────────────────────

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
            => LoadAvatarByProviderKeyAsync(providerKey, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            if(Guid.TryParse(providerKey,out var id))return await LoadAvatarAsync(id,version);
            var result=new OASISResult<IAvatar>();
            try
            {
                if(string.IsNullOrWhiteSpace(providerKey))throw new ArgumentException("Provider key must be nonempty.",nameof(providerKey));
                var all=await LoadAllAvatarsAsync(version);
                if(all.IsError||all.Result==null)throw all.Exception??new System.IO.InvalidDataException("BigQuery avatar source did not complete.");
                var selected=UniqueRecord(all.Result.Where(row=>row.ProviderUniqueStorageKey!=null&&row.ProviderUniqueStorageKey.TryGetValue(ProviderType.Value,out var key)&&key==providerKey).ToList());
                if(selected==null)OASISErrorHandling.HandleError(ref result,"BigQuery avatar provider key was not found.");
                else CompleteRead(result,selected);
            }
            catch(Exception ex){OASISErrorHandling.HandleError(ref result,"BigQuery avatar provider-key read failed.",ex);}
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
            => DeleteAvatarAsync(providerKey, softDelete).GetAwaiter().GetResult();

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            var avatar = await LoadAvatarByProviderKeyAsync(providerKey);
            if (avatar.IsError || avatar.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, avatar.Message,avatar.Exception);
                return result;
            }
            return await DeleteAvatarAsync(avatar.Result.Id, softDelete);
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            if(Guid.TryParse(providerKey,out var id))return await LoadHolonAsync(id,loadChildren,recursive,maxChildDepth,continueOnError,loadChildrenFromProvider,version);
            var result=new OASISResult<IHolon>();
            try
            {
                ValidateGraphDepth(maxChildDepth);
                if(string.IsNullOrWhiteSpace(providerKey))throw new ArgumentException("Provider key must be nonempty.",nameof(providerKey));
                var active=(await GetAllDocsAsync<Holon>(CollHolons,version)).Where(row=>!row.IsDeleted).ToList();
                var selected=UniqueRecord(active.Where(row=>row.ProviderUniqueStorageKey!=null&&row.ProviderUniqueStorageKey.TryGetValue(ProviderType.Value,out var key)&&key==providerKey).ToList());
                if(selected==null)OASISErrorHandling.HandleError(ref result,"BigQuery holon provider key was not found.");
                else CompleteRead(result,(await ProjectGraphRootsAsync(new[]{selected},loadChildren,recursive,maxChildDepth,0,loadChildrenFromProvider,providerPool:version==0?active:null)).Single());
            }
            catch(Exception ex){OASISErrorHandling.HandleError(ref result,"BigQuery holon provider-key read failed.",ex);}
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            if (Guid.TryParse(providerKey, out var id))
                return await LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);

            var parent = await LoadHolonAsync(providerKey,loadChildren:false);
            if (parent.IsError || parent.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, parent.Message,parent.Exception);
                return result;
            }
            return await LoadHolonsForParentAsync(parent.Result.Id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id)
            => DeleteHolonAsync(id).GetAwaiter().GetResult();

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var result = new OASISResult<IHolon>();
            var loaded = await LoadHolonAsync(id);
            if (loaded.IsError || loaded.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, loaded.Message,loaded.Exception);
                return result;
            }

            var deleted = await DeleteHolonSoftAsync(id, true);
            if (deleted.IsError)
                OASISErrorHandling.HandleError(ref result, deleted.Message,deleted.Exception);
            else
            {
                result.Result = loaded.Result;
                result.IsDeleted=deleted.IsDeleted;
                result.DeletedCount=deleted.DeletedCount;
            }

            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey)
            => DeleteHolonAsync(providerKey).GetAwaiter().GetResult();

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            var result = new OASISResult<IHolon>();
            var holon = await LoadHolonAsync(providerKey);
            if (holon.IsError || holon.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, holon.Message,holon.Exception);
                return result;
            }
            return await DeleteHolonAsync(holon.Result.Id);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                return await LoadHolonsByMetaDataAsync(new Dictionary<string,string>(StringComparer.Ordinal){{metaKey,metaValue}},MetaKeyValuePairMatchMode.All,type,loadChildren,recursive,maxChildDepth,curentChildDepth,continueOnError,loadChildrenFromProvider,version);
            }
            catch(Exception ex)
            {
                OASISErrorHandling.HandleError(ref result,"BigQuery metadata query failed.",ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if(metaKeyValuePairMatchMode!=MetaKeyValuePairMatchMode.All && metaKeyValuePairMatchMode!=MetaKeyValuePairMatchMode.Any)
                    throw new ArgumentOutOfRangeException(nameof(metaKeyValuePairMatchMode));
                var requested=metaKeyValuePairs==null?null:new Dictionary<string,string>(metaKeyValuePairs,StringComparer.Ordinal);
                var all = await LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
                if(all.IsError || all.Result==null)
                {
                    OASISErrorHandling.HandleError(ref result,all.Message,all.Exception);
                    return result;
                }
                CompleteRows(result,all.Result.Where(holon=>NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.StoredRecordSearch.MatchesMetadata(holon.MetaData,requested,metaKeyValuePairMatchMode)));
            }
            catch(Exception ex)
            {
                OASISErrorHandling.HandleError(ref result,"BigQuery metadata query failed.",ex);
            }
            return result;
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
            => ImportAsync(holons).GetAwaiter().GetResult();

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            var result = new OASISResult<bool>();
            var saved = await SaveHolonsAsync(holons);
            result.SavedCount=saved.SavedCount;
            if (saved.IsError)
                OASISErrorHandling.HandleError(ref result, saved.Message,saved.Exception);
            else
            {
                result.Result = true;
                result.IsSaved=saved.IsSaved;
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
            => ExportAllAsync(version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            if(version!=-1)return await LoadAllHolonsAsync(HolonType.All,true,true,0,0,true,true,version);
            var result=new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var versions=await ReadRowsAsync<Holon>($"SELECT doc FROM {Table(CollHolons)}",Array.Empty<BigQueryParameter>(),version:-1,exportAllVersions:true);
                CompleteRows<IHolon>(result,versions.Where(row=>!row.IsDeleted));
            }
            catch(Exception cause){OASISErrorHandling.HandleError(ref result,"BigQuery all-version export failed.",cause);}
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
            => ExportAllDataForAvatarByIdAsync(avatarId, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            var result=new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if(avatarId==Guid.Empty)throw new ArgumentException("Avatar export requires a nonempty owner.",nameof(avatarId));
                if(version < -1)throw new ArgumentOutOfRangeException(nameof(version));
                if(version==-1)
                {
                    var all=await ExportAllAsync(-1);
                    if(all.IsError)return all;
                    CompleteRows(result,all.Result.Where(row=>row.CreatedByAvatarId==avatarId));
                }
                else
                {
                    var owned=(await GetAllDocsAsync<Holon>(CollHolons,version)).Where(row=>!row.IsDeleted&&row.CreatedByAvatarId==avatarId).Cast<IHolon>().ToList();
                    var pool=version==0?owned:(await GetAllDocsAsync<Holon>(CollHolons)).Where(row=>!row.IsDeleted&&row.CreatedByAvatarId==avatarId).Cast<IHolon>().ToList();
                    CompleteRows(result,await ProjectGraphRootsAsync(owned,true,true,0,0,true,providerPool:pool));
                }
            }
            catch(Exception ex){OASISErrorHandling.HandleError(ref result,"BigQuery owner export failed.",ex);}
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
            => ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).GetAwaiter().GetResult();

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
            => ExportOwnerByIdentityAsync(avatarUsername,false,version);

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
            => ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).GetAwaiter().GetResult();

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
            => ExportOwnerByIdentityAsync(avatarEmailAddress,true,version);

        private async Task<OASISResult<IEnumerable<IHolon>>> ExportOwnerByIdentityAsync(string identity,bool email,int version)
        {
            var result=new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if(version < -1)throw new ArgumentOutOfRangeException(nameof(version));
                var avatar=email?await LoadAvatarByEmailAsync(identity):await LoadAvatarByUsernameAsync(identity);
                if(avatar.IsError||avatar.Result==null)
                {
                    OASISErrorHandling.HandleError(ref result,avatar.Message,avatar.Exception);
                    return result;
                }
                return await ExportAllDataForAvatarByIdAsync(avatar.Result.Id,version);
            }
            catch(Exception cause){OASISErrorHandling.HandleError(ref result,"BigQuery owner identity export failed.",cause);}
            return result;
        }

        // ─── Search ──────────────────────────────────────────────────────────────

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
            try
            {
                NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.StoredRecordSearch.Validate(searchParams,version,maxChildDepth,supportsHistory:true);
                searchParams=Core.Serialization.StoredRecordJson.Project<SearchParams>(searchParams);
                NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.StoredRecordSearch.Validate(searchParams,version,maxChildDepth,supportsHistory:true);
                var groups=searchParams.SearchGroups??new List<ISearchGroupBase>();
                var wantsAvatars=groups.Count==0||groups.Any(group=>group.SearchAvatars||!group.SearchHolons);
                var wantsHolons=groups.Count==0||groups.Any(group=>group.SearchHolons||!group.SearchAvatars);
                var holons=new List<IHolon>();
                if(wantsHolons||searchParams.ParentId!=Guid.Empty)
                {
                    var loaded=await LoadAllHolonsAsync(HolonType.All,false,recursive,maxChildDepth,0,continueOnError,false,wantsHolons?version:0);
                    if(loaded.IsError||loaded.Result==null)
                        throw loaded.Exception??new System.IO.InvalidDataException("BigQuery search holon source did not complete.");
                    holons.AddRange(loaded.Result);
                }
                var graphPool=holons;
                if(version>0&&wantsHolons&&(loadChildren||searchParams.ParentId!=Guid.Empty))
                    graphPool=(await GetAllDocsAsync<Holon>(CollHolons)).Where(row=>!row.IsDeleted).Cast<IHolon>().ToList();
                var found=new SearchResults();
                if(wantsAvatars)
                {
                    var loaded=await LoadAllAvatarsAsync(version);
                    if(loaded.IsError||loaded.Result==null)
                        throw loaded.Exception??new System.IO.InvalidDataException("BigQuery search avatar source did not complete.");
                    found.SearchResultAvatars.AddRange(loaded.Result.Where(row=>NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.StoredRecordSearch.Matches(row,true,searchParams,graphPool)));
                }
                if(wantsHolons)
                    found.SearchResultHolons.AddRange(await ProjectGraphRootsAsync(holons.Where(row=>NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.StoredRecordSearch.Matches(row,false,searchParams,graphPool)),loadChildren,recursive,maxChildDepth,0,true,providerPool:graphPool));
                found.NumberOfResults=found.SearchResultAvatars.Count+found.SearchResultHolons.Count;
                result.Result=found;result.IsLoaded=true;result.LoadedCount=found.NumberOfResults;
            }
            catch(Exception ex)
            {
                OASISErrorHandling.HandleError(ref result,"BigQuery search failed; no complete result is available.",ex);
            }
            return result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
            => SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).GetAwaiter().GetResult();

        // ─── Backend helpers ─────────────────────────────────────────────────────

        private static void CompleteRead<T>(OASISResult<T> result,T record)
        {result.Result=record;result.IsLoaded=true;result.LoadedCount=1;}

        private static void CompleteRows<T>(OASISResult<IEnumerable<T>> result,IEnumerable<T> rows)
        {var selected=System.Linq.Enumerable.ToArray(rows);result.Result=selected;result.IsLoaded=true;result.LoadedCount=selected.Length;}

        private static string FieldColumn(string field) => field switch
        {
            "Username" => "username",
            "Email" => "email",
            "ParentHolonId" => "parent_holon_id",
            _ => throw new ArgumentException("Unsupported BigQuery indexed field.",nameof(field))
        };

        private static string ValidateSqlIdentifier(string value,string parameter)
        {
            if(string.IsNullOrWhiteSpace(value)||value.IndexOfAny(new[]{'`','\\'})>=0||System.Linq.Enumerable.Any(value,char.IsControl))
                throw new ArgumentException("BigQuery identifier cannot contain SQL quoting,escape or control characters.",parameter);
            return value;
        }

        private static BigQueryParameter StringParameter(string name,string value)=>new(name,BigQueryDbType.String,value);

        private static (string doc, string username, string email, string parent) Shred<T>(T obj)
        {
            var json = Core.Serialization.StoredRecordJson.Serialize(obj);
            string u = "", e = "", p = "";
            using var d = JsonDocument.Parse(json);
            if (d.RootElement.TryGetProperty("Username", out var uu) && uu.ValueKind == JsonValueKind.String) u = uu.GetString() ?? "";
            if (d.RootElement.TryGetProperty("Email", out var ee) && ee.ValueKind == JsonValueKind.String) e = ee.GetString() ?? "";
            if (d.RootElement.TryGetProperty("ParentHolonId", out var pp) && pp.ValueKind == JsonValueKind.String) p = pp.GetString() ?? "";
            return (json, u, e, p);
        }

        private string Table(string t) => $"`{_projectId}.{_dataset}.{t}`";

        private async Task<List<T>> SqlAsync<T>(string sql,params BigQueryParameter[] parameters) where T : class,IHolonBase
            => await ReadRowsAsync<T>(sql,parameters);

        private async Task<List<T>> ReadRowsAsync<T>(string sql,IEnumerable<BigQueryParameter> parameters,Action<T,string>? observe=null,int version=0,bool exportAllVersions=false) where T : class,IHolonBase
        {
            if(exportAllVersions?version!=-1:version<0)throw new ArgumentOutOfRangeException(nameof(version));
            var list = new List<T>();
            var results = await _client!.ExecuteQueryAsync(sql, parameters,new QueryOptions{UseLegacySql=false});
            await foreach (var row in results.GetRowsAsync())
            {
                var raw = row["doc"]?.ToString();
                if (string.IsNullOrWhiteSpace(raw))throw new System.IO.InvalidDataException("BigQuery stored record row has no canonical document.");
                if(exportAllVersions){list.AddRange(ReadAllRecordVersions<T>(raw));continue;}
                var item = await DecodeRecordVersionAsync<T>(raw,version);
                if(item==null)continue;
                observe?.Invoke(item,raw);
                list.Add(item);
            }
            return list;
        }

        private async Task<T?> GetDocAsync<T>(string table, string id,int version=0) where T : class,IHolonBase
        {
            ValidatePointIdentity(Guid.Parse(id));
            var rows = await ReadRowsAsync<T>($"SELECT doc FROM {Table(table)} WHERE id = @id LIMIT 2",new[]{StringParameter("id",id)},version:version);
            ValidateIndexedSelection(rows,"Id",id);
            return UniqueRecord(rows);
        }

        private static T? UniqueRecord<T>(List<T> rows) where T : class
        {
            if(rows.Count>1)throw new System.IO.InvalidDataException("BigQuery point selector matched multiple stored records.");
            return rows.Count==0?null:rows[0];
        }

        private sealed record BigQueryWriteAcknowledgement(string JobId,string Location);
        private async Task<BigQueryWriteAcknowledgement> UpsertDocAsync<T>(string table, string id, T obj,bool checkOriginal=false,string? expectedDoc=null,string? documentJson=null)
        {
            var (doc, u, e, p) = Shred(obj);
            if(documentJson!=null)doc=documentJson;
            var sql = $@"MERGE {Table(table)} t
                USING (SELECT @id AS id, @doc AS doc, @username AS username,
                              @email AS email, @parent AS parent_holon_id) s
                ON t.id = s.id
                WHEN MATCHED{(checkOriginal?" AND @expected_exists AND t.doc = @expected_doc":"")} THEN UPDATE SET doc = s.doc, username = s.username, email = s.email,
                                             parent_holon_id = s.parent_holon_id, updated_at = CURRENT_TIMESTAMP()
                WHEN NOT MATCHED{(checkOriginal?" AND NOT @expected_exists":"")} THEN INSERT (id, doc, username, email, parent_holon_id, updated_at)
                                      VALUES (s.id, s.doc, s.username, s.email, s.parent_holon_id, CURRENT_TIMESTAMP())";
            var client=_client??throw new InvalidOperationException("BigQuery client is not activated.");
            var parameters=new List<BigQueryParameter>{StringParameter("id",id),StringParameter("doc",doc),StringParameter("username",u),StringParameter("email",e),StringParameter("parent",p)};
            if(checkOriginal){parameters.Add(new BigQueryParameter("expected_exists",BigQueryDbType.Bool,expectedDoc!=null));parameters.Add(new BigQueryParameter("expected_doc",BigQueryDbType.String,expectedDoc));}
            return await ExecuteOneRowMutationAsync(client,id,sql,parameters);
        }

        private async Task<BigQueryWriteAcknowledgement> ExecuteOneRowMutationAsync(BigQueryClient client,string id,string sql,IEnumerable<BigQueryParameter> parameters)
        {
            // Supply the native job identity before dispatch so even a lost response
            // has a concrete request to reconcile. This is not a success receipt.
            var jobId="oasis-record-"+Guid.NewGuid().ToString("N");string location="";var acknowledged=false;
            try{
                var completed=await client.ExecuteQueryAsync(sql,parameters,new QueryOptions{UseLegacySql=false,JobId=jobId});
                var reference=completed?.JobReference;
                var bound=reference?.ProjectId==_projectId&&reference.JobId==jobId;
                if(bound)location=reference!.Location??"";
                acknowledged=bound&&completed?.NumDmlAffectedRows==1;
                if(!acknowledged){
                    var invalid=new System.IO.InvalidDataException("BigQuery record mutation requires exactly one affected row and its original project/job identity.");
                    if(reference!=null&&!bound){invalid.Data["UnboundProjectId"]=reference.ProjectId??"";invalid.Data["UnboundJobId"]=reference.JobId??"";}
                    throw invalid;
                }
                return new(jobId,location);
            }catch(Exception cause){throw new BigQueryRecordReconciliationException(Guid.Parse(id),_projectId,jobId,location,acknowledged,cause);}
        }

        private async Task DeleteDocAsync(string table, string id,string? expectedDoc=null)
        {
            ValidatePointIdentity(Guid.Parse(id));
            var parameters=new List<BigQueryParameter>{StringParameter("id",id)};
            if(expectedDoc!=null)parameters.Add(StringParameter("expected_doc",expectedDoc));
            await ExecuteOneRowMutationAsync(_client??throw new InvalidOperationException("BigQuery client is not activated."),id,$"DELETE FROM {Table(table)} WHERE id = @id{(expectedDoc==null?"":" AND doc = @expected_doc")}",parameters);
        }

        private async Task<T?> FindOneAsync<T>(string table, string field, string value,int version=0) where T : class,IHolonBase
        {
            var column=FieldColumn(field);
            if(string.IsNullOrWhiteSpace(value))throw new ArgumentException("BigQuery identity selector must not be blank.",nameof(value));
            var rows = version==0?await ReadRowsAsync<T>($"SELECT doc FROM {Table(table)} WHERE {column} = @value LIMIT 2",new[]{StringParameter("value",value)},version:version):
                (await GetAllDocsAsync<T>(table,version)).Where(row=>string.Equals(IndexedRecordValue(row,field),value,StringComparison.Ordinal)).ToList();
            ValidateIndexedSelection(rows,field,value);
            return UniqueRecord(rows);
        }

        private async Task<List<T>> FindAllAsync<T>(string table, string field, string value,int version=0) where T : class,IHolonBase
        {
            var rows=version==0?await SqlAsync<T>($"SELECT doc FROM {Table(table)} WHERE {FieldColumn(field)} = @value",StringParameter("value",value)):
                (await GetAllDocsAsync<T>(table,version)).Where(row=>string.Equals(IndexedRecordValue(row,field),value,StringComparison.Ordinal)).ToList();
            ValidateIndexedSelection(rows,field,value);
            return rows;
        }

        private async Task<List<T>> GetAllDocsAsync<T>(string table,int version=0) where T : class,IHolonBase
            => await ReadRowsAsync<T>($"SELECT doc FROM {Table(table)}",Array.Empty<BigQueryParameter>(),version:version);
    }
}
