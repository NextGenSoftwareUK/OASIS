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
        /// When true this provider stores a new record per save and links to the previous
        /// version (blockchain-style) instead of updating in place.
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
                var doc = await GetDocAsync<Avatar>(CollAvatars, id.ToString());
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
                var doc = await FindOneAsync<Avatar>(CollAvatars, "Username", username);
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
                var doc = await FindOneAsync<Avatar>(CollAvatars, "Email", email);
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
                var docs = await GetAllDocsAsync<Avatar>(CollAvatars);
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
                    var avatar = await GetDocAsync<Avatar>(CollAvatars, id.ToString());
                    if (avatar == null)throw new System.Collections.Generic.KeyNotFoundException("BigQuery avatar to delete was not found.");
                    else
                    {
                        avatar.DeletedDate = DateTime.UtcNow;
                        await UpsertDocAsync(CollAvatars, id.ToString(), avatar);
                    }
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
                var doc = await GetDocAsync<AvatarDetail>(CollAvatarDetails, id.ToString());
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
                var doc = await FindOneAsync<AvatarDetail>(CollAvatarDetails, "Username", username);
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
                var doc = await FindOneAsync<AvatarDetail>(CollAvatarDetails, "Email", email);
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
                var docs = await GetAllDocsAsync<AvatarDetail>(CollAvatarDetails);
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
                var doc = await GetDocAsync<Holon>(CollHolons, id.ToString());
                if (doc == null)
                    OASISErrorHandling.HandleError(ref result, $"BigQueryOASIS: Holon {id} not found.");
                else if (doc.IsDeleted)
                    OASISErrorHandling.HandleError(ref result, $"BigQueryOASIS: Holon {id} has been deleted.");
                else
                    CompleteRead(result,doc);
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
                var all = await FindAllAsync<Holon>(CollHolons, "ParentHolonId", id.ToString());
                CompleteRows<IHolon>(result,all.FindAll(h => !h.IsDeleted && (holonType == HolonType.All || h.HolonType == holonType)));
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
                var docs = await GetAllDocsAsync<Holon>(CollHolons);
                CompleteRows<IHolon>(result,docs.FindAll(h => !h.IsDeleted && (holonType == HolonType.All || h.HolonType == holonType)));
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
            => SaveAcknowledgedRecordAsync(holon,CollHolons);

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var saved = new List<IHolon>();
            var acknowledged=new List<string>();var failures=new List<Exception>();var count=0;
            try
            {
                ArgumentNullException.ThrowIfNull(holons);
                var roots=System.Linq.Enumerable.ToArray(holons);
                if(System.Linq.Enumerable.Any(roots,root=>root==null))throw new ArgumentException("BigQuery bulk roots must not contain null.",nameof(holons));
                foreach (var h in roots)
                {
                    var r = await SaveHolonAsync(h, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
                    count=checked(count+r.SavedCount);
                    if(r.SavedCount>0){
                        var id=r.Exception is BigQueryRecordReconciliationException native?native.RecordId:r.Result?.Id??h.Id;
                        if(id!=Guid.Empty)acknowledged.Add(id.ToString());
                    }
                    if(r.IsError||!r.IsSaved||r.Result==null){
                        var cause=r.Exception??new System.IO.InvalidDataException("BigQuery member returned no complete save acknowledgement.");failures.Add(cause);
                        if(!continueOnError||NativeMutationReconciliation.IsRequired(cause))break;
                    }else saved.Add(r.Result);
                }
            }
            catch (Exception ex)
            {
                failures.Add(ex);
            }
            result.Result=saved;result.SavedCount=count;result.IsSaved=failures.Count==0;
            if(failures.Count>0){
                Exception cause=failures.Count==1?failures[0]:new AggregateException(failures);
                if(acknowledged.Count>0)cause=new NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.KeyValuePartialMutationException(Core.Enums.ProviderType.BigQueryOASIS,acknowledged,cause);
                OASISErrorHandling.HandleError(ref result,"BigQuery bulk save failed; reconcile acknowledged records and original member outcomes before replay.",cause);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).GetAwaiter().GetResult();

        public async Task<OASISResult<bool>> DeleteHolonSoftAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (softDelete)
                {
                    var holon = await GetDocAsync<Holon>(CollHolons, id.ToString());
                    if (holon == null)throw new System.Collections.Generic.KeyNotFoundException("BigQuery holon to delete was not found.");
                    else
                    {
                        holon.DeletedDate = DateTime.UtcNow;
                        await UpsertDocAsync(CollHolons, id.ToString(), holon);
                    }
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
            var result = new OASISResult<IAvatar>();
            if (Guid.TryParse(providerKey, out var id))
                return await LoadAvatarAsync(id, version);

            var all = await LoadAllAvatarsAsync(version);
            if (all.IsError || all.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, all.Message);
                return result;
            }

            foreach (var avatar in all.Result)
            {
                if (avatar.ProviderUniqueStorageKey != null
                    && avatar.ProviderUniqueStorageKey.TryGetValue(ProviderType.Value, out var key)
                    && key == providerKey)
                {
                    result.Result = avatar;
                    return result;
                }
            }

            OASISErrorHandling.HandleError(ref result, $"Avatar with provider key '{providerKey}' not found.");
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
            var result = new OASISResult<IHolon>();
            if (Guid.TryParse(providerKey, out var id))
                return await LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);

            var all = await LoadAllHolonsAsync();
            if (all.IsError || all.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, all.Message);
                return result;
            }

            foreach (var holon in all.Result)
            {
                if (holon.ProviderUniqueStorageKey != null
                    && holon.ProviderUniqueStorageKey.TryGetValue(ProviderType.Value, out var key)
                    && key == providerKey)
                {
                    result.Result = holon;
                    return result;
                }
            }

            OASISErrorHandling.HandleError(ref result, $"Holon with provider key '{providerKey}' not found.");
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            if (Guid.TryParse(providerKey, out var id))
                return await LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);

            var parent = await LoadHolonAsync(providerKey);
            if (parent.IsError || parent.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, parent.Message);
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
            if (saved.IsError)
                OASISErrorHandling.HandleError(ref result, saved.Message);
            else
                result.Result = true;
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
            => ExportAllAsync(version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
            => await LoadAllHolonsAsync(HolonType.All, true, true, 0, 0, true, false, version);

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
            => ExportAllDataForAvatarByIdAsync(avatarId, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var all = await LoadAllHolonsAsync(HolonType.All, true, true, 0, 0, true, false, version);
            if (all.IsError || all.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, all.Message);
                return result;
            }

            var owned = new List<IHolon>();
            foreach (var holon in all.Result)
            {
                if (holon.CreatedByAvatarId == avatarId)
                    owned.Add(holon);
            }

            result.Result = owned;
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
            => ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var avatar = await LoadAvatarByUsernameAsync(avatarUsername, version);
            if (avatar.IsError || avatar.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, avatar.Message);
                return result;
            }
            return await ExportAllDataForAvatarByIdAsync(avatar.Result.Id, version);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
            => ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var avatar = await LoadAvatarByEmailAsync(avatarEmailAddress, version);
            if (avatar.IsError || avatar.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, avatar.Message);
                return result;
            }
            return await ExportAllDataForAvatarByIdAsync(avatar.Result.Id, version);
        }

        // ─── Search ──────────────────────────────────────────────────────────────

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
            var searchResults = new SearchResults();

            try
            {
                var groups = searchParams?.SearchGroups ?? new List<ISearchGroupBase>();
                var wantAvatars = groups.Count == 0 || groups.Exists(g => g.SearchAvatars);
                var wantHolons = groups.Count == 0 || groups.Exists(g => g.SearchHolons);

                var matchedAvatars = new Dictionary<Guid, IAvatar>();
                var matchedHolons = new Dictionary<Guid, IHolon>();

                // ── Avatars ──────────────────────────────────────────────────
                if (wantAvatars)
                {
                    var avatars = await LoadAllAvatarsAsync(version);
                    if (avatars.IsError && !continueOnError)
                    {
                        OASISErrorHandling.HandleError(ref result, avatars.Message);
                        return result;
                    }

                    foreach (var avatar in avatars.Result ?? new List<IAvatar>())
                    {
                        if (avatar == null) continue;
                        if (searchParams != null && searchParams.SearchOnlyForCurrentAvatar
                            && searchParams.AvatarId != Guid.Empty && avatar.Id != searchParams.AvatarId)
                            continue;

                        if (groups.Count == 0 || AvatarMatchesAnyGroup(avatar, groups))
                            matchedAvatars[avatar.Id] = avatar;
                    }
                }

                // ── Holons ───────────────────────────────────────────────────
                if (wantHolons)
                {
                    var holons = await LoadAllHolonsAsync(HolonType.All, loadChildren, recursive, maxChildDepth, 0, continueOnError, false, version);
                    if (holons.IsError && !continueOnError)
                    {
                        OASISErrorHandling.HandleError(ref result, holons.Message);
                        return result;
                    }

                    foreach (var holon in holons.Result ?? new List<IHolon>())
                    {
                        if (holon == null) continue;

                        if (searchParams != null)
                        {
                            if (searchParams.SearchOnlyForCurrentAvatar && searchParams.AvatarId != Guid.Empty
                                && holon.CreatedByAvatarId != searchParams.AvatarId)
                                continue;

                            if (searchParams.ParentId != Guid.Empty && holon.ParentHolonId != searchParams.ParentId)
                                continue;

                            if (!MetaDataMatches(holon, searchParams.FilterByMetaData, searchParams.MetaKeyValuePairMatchMode))
                                continue;
                        }

                        if (groups.Count == 0 || HolonMatchesAnyGroup(holon, groups))
                            matchedHolons[holon.Id] = holon;
                    }
                }

                searchResults.SearchResultAvatars = new List<IAvatar>(matchedAvatars.Values);
                searchResults.SearchResultHolons = new List<IHolon>(matchedHolons.Values);
                searchResults.NumberOfResults = searchResults.SearchResultAvatars.Count + searchResults.SearchResultHolons.Count;

                result.Result = searchResults;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"BigQueryOASIS: SearchAsync failed: {ex.Message}");
            }

            return result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
            => SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).GetAwaiter().GetResult();

        private static bool Contains(string source, string query)
            => !string.IsNullOrEmpty(source) && source.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;

        private static bool MetaDataMatches(IHolon holon, Dictionary<string, string> filter, MetaKeyValuePairMatchMode mode)
        {
            if (filter == null || filter.Count == 0) return true;
            if (holon.MetaData == null) return false;

            var matched = 0;
            foreach (var pair in filter)
            {
                if (holon.MetaData.TryGetValue(pair.Key, out var value) && value?.ToString() == pair.Value)
                    matched++;
            }

            return mode == MetaKeyValuePairMatchMode.All ? matched == filter.Count : matched > 0;
        }

        private static bool AvatarMatchesAnyGroup(IAvatar avatar, List<ISearchGroupBase> groups)
        {
            foreach (var group in groups)
            {
                if (!group.SearchAvatars) continue;

                var text = group as ISearchTextGroup;
                var query = text?.SearchQuery;
                if (string.IsNullOrWhiteSpace(query)) return true;

                var p = group.AvatarSearchParams;

                // No field flags set - match the natural identity fields.
                if (p == null)
                {
                    if (Contains(avatar.Username, query) || Contains(avatar.Email, query)
                        || Contains(avatar.FirstName, query) || Contains(avatar.LastName, query))
                        return true;
                    continue;
                }

                if (p.Username && Contains(avatar.Username, query)) return true;
                if (p.Email && Contains(avatar.Email, query)) return true;
                if (p.FirstName && Contains(avatar.FirstName, query)) return true;
                if (p.LastName && Contains(avatar.LastName, query)) return true;
                if (p.Title && Contains(avatar.Title, query)) return true;
                if (p.AvatarId && Contains(avatar.Id.ToString(), query)) return true;
                if (text != null && text.SearchIds && Contains(avatar.Id.ToString(), query)) return true;

                if (text != null && text.SearchProviderKeys && avatar.ProviderUniqueStorageKey != null)
                {
                    foreach (var key in avatar.ProviderUniqueStorageKey.Values)
                        if (Contains(key, query)) return true;
                }

                // Flags present but none of them matched a searchable field - fall
                // back to identity fields so a query is never silently dropped.
                if (!p.Username && !p.Email && !p.FirstName && !p.LastName && !p.Title && !p.AvatarId)
                {
                    if (Contains(avatar.Username, query) || Contains(avatar.Email, query))
                        return true;
                }
            }

            return false;
        }

        private static bool HolonMatchesAnyGroup(IHolon holon, List<ISearchGroupBase> groups)
        {
            foreach (var group in groups)
            {
                if (!group.SearchHolons) continue;

                if (group.HolonType != HolonType.All && holon.HolonType != group.HolonType)
                    continue;

                var text = group as ISearchTextGroup;
                var query = text?.SearchQuery;
                if (string.IsNullOrWhiteSpace(query)) return true;

                var p = group.HolonSearchParams;

                if (p == null)
                {
                    if (Contains(holon.Name, query) || Contains(holon.Description, query))
                        return true;
                    continue;
                }

                if (p.Name && Contains(holon.Name, query)) return true;
                if (p.Description && Contains(holon.Description, query)) return true;
                if (text != null && text.SearchIds && Contains(holon.Id.ToString(), query)) return true;

                if (p.MetaData && holon.MetaData != null)
                {
                    foreach (var kvp in holon.MetaData)
                        if (Contains(kvp.Key, query) || Contains(kvp.Value?.ToString(), query)) return true;
                }

                if ((p.ProviderUniqueStorageKey || (text != null && text.SearchProviderKeys))
                    && holon.ProviderUniqueStorageKey != null)
                {
                    foreach (var key in holon.ProviderUniqueStorageKey.Values)
                        if (Contains(key, query)) return true;
                }

                if (!p.Name && !p.Description && !p.MetaData && !p.ProviderUniqueStorageKey)
                {
                    if (Contains(holon.Name, query) || Contains(holon.Description, query))
                        return true;
                }
            }

            return false;
        }

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

        private async Task<List<T>> SqlAsync<T>(string sql,params BigQueryParameter[] parameters) where T : class
        {
            var list = new List<T>();
            var results = await _client!.ExecuteQueryAsync(sql, parameters,new QueryOptions{UseLegacySql=false});
            await foreach (var row in results.GetRowsAsync())
            {
                var raw = row["doc"]?.ToString();
                if (string.IsNullOrWhiteSpace(raw))throw new System.IO.InvalidDataException("BigQuery stored record row has no canonical document.");
                var item = Core.Serialization.StoredRecordJson.DeserializeStored<T>(raw);
                if(item==null)throw new System.IO.InvalidDataException("BigQuery stored record document must not be null.");
                list.Add(item);
            }
            return list;
        }

        private async Task<T?> GetDocAsync<T>(string table, string id) where T : class
        {
            var rows = await SqlAsync<T>($"SELECT doc FROM {Table(table)} WHERE id = @id LIMIT 2",StringParameter("id",id));
            return UniqueRecord(rows);
        }

        private static T? UniqueRecord<T>(List<T> rows) where T : class
        {
            if(rows.Count>1)throw new System.IO.InvalidDataException("BigQuery point selector matched multiple stored records.");
            return rows.Count==0?null:rows[0];
        }

        private sealed record BigQueryWriteAcknowledgement(string JobId,string Location);
        private async Task<BigQueryWriteAcknowledgement> UpsertDocAsync<T>(string table, string id, T obj)
        {
            var (doc, u, e, p) = Shred(obj);
            var sql = $@"MERGE {Table(table)} t
                USING (SELECT @id AS id, @doc AS doc, @username AS username,
                              @email AS email, @parent AS parent_holon_id) s
                ON t.id = s.id
                WHEN MATCHED THEN UPDATE SET doc = s.doc, username = s.username, email = s.email,
                                             parent_holon_id = s.parent_holon_id, updated_at = CURRENT_TIMESTAMP()
                WHEN NOT MATCHED THEN INSERT (id, doc, username, email, parent_holon_id, updated_at)
                                      VALUES (s.id, s.doc, s.username, s.email, s.parent_holon_id, CURRENT_TIMESTAMP())";
            var client=_client??throw new InvalidOperationException("BigQuery client is not activated.");
            var parameters=new[]{StringParameter("id",id),StringParameter("doc",doc),StringParameter("username",u),StringParameter("email",e),StringParameter("parent",p)};
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

        private async Task DeleteDocAsync(string table, string id)
            => await ExecuteOneRowMutationAsync(_client??throw new InvalidOperationException("BigQuery client is not activated."),id,$"DELETE FROM {Table(table)} WHERE id = @id",new[]{StringParameter("id",id)});

        private async Task<T?> FindOneAsync<T>(string table, string field, string value) where T : class
        {
            var rows = await SqlAsync<T>($"SELECT doc FROM {Table(table)} WHERE {FieldColumn(field)} = @value LIMIT 2",StringParameter("value",value));
            return UniqueRecord(rows);
        }

        private async Task<List<T>> FindAllAsync<T>(string table, string field, string value) where T : class
            => await SqlAsync<T>($"SELECT doc FROM {Table(table)} WHERE {FieldColumn(field)} = @value",StringParameter("value",value));

        private async Task<List<T>> GetAllDocsAsync<T>(string table) where T : class
            => await SqlAsync<T>($"SELECT doc FROM {Table(table)}");
    }
}
