using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
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
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS
{
    /// <summary>
    /// OASIS provider for Microsoft Azure Blob Storage.
    /// Holons are stored as JSON blobs in a configurable container.
    /// The blob name (providerKey) is the holon's OASIS ID (GUID) or a caller-supplied key.
    /// Avatars are stored in a separate "avatars" container, keyed by avatar ID or username.
    /// SDK: Azure.Storage.Blobs v12
    /// </summary>
    public partial class AzureStorageOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider, IProviderTransportFailureClassifier
    {
        // These storage APIs pass no caller token to Azure, so TaskCanceledException
        // returned by their SDK call belongs to the SDK's transport deadline.
        // If caller-token support is introduced, this boundary must carry that context.
        public bool IsProviderTransportFailure(Exception failure)=>failure is TaskCanceledException;
        private readonly BlobServiceClient _serviceClient;
        private readonly string _holonContainer;
        private readonly string _avatarContainer;
        private readonly string _detailContainer;

        public AzureStorageOASIS(
            string connectionString,
            string holonContainer = "oasis-holons",
            string avatarContainer = "oasis-avatars",
            string avatarDetailContainer = "oasis-avatar-details")
        {
            var options = new BlobClientOptions();
            options.Retry.MaxRetries = 0;
            _serviceClient = new BlobServiceClient(connectionString, options);
            _holonContainer = holonContainer;
            _avatarContainer = avatarContainer;
            _detailContainer = avatarDetailContainer;
            if(_detailContainer==_avatarContainer || _detailContainer==_holonContainer || _holonContainer==_avatarContainer)
                throw new ArgumentException("Holons, avatars and avatar details require three distinct blob containers.",nameof(avatarDetailContainer));

            ProviderName = "AzureStorageOASIS";
            ProviderDescription = "Azure Blob Storage Provider — JSON holons and avatars as blobs";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.AzureStorageOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Cloud));
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage));
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                var holons = _serviceClient.GetBlobContainerClient(_holonContainer);
                await holons.CreateIfNotExistsAsync(PublicAccessType.None);
                var avatars = _serviceClient.GetBlobContainerClient(_avatarContainer);
                await avatars.CreateIfNotExistsAsync(PublicAccessType.None);
                await _serviceClient.GetBlobContainerClient(_detailContainer).CreateIfNotExistsAsync(PublicAccessType.None);
                IsProviderActivated = true; result.Result = true; result.Message = "AzureStorageOASIS activated.";
            }
            catch (Exception ex) { IsProviderActivated=false; result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }
        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;
        public override async Task<OASISResult<bool>> DeActivateProviderAsync() { IsProviderActivated = false; return new OASISResult<bool>(true); }
        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        // ─── Helpers ─────────────────────────────────────────────────────────────

        private static string HolonKey(IHolon holon)
        {
            var customKey = (holon as Holon)?.CustomKey;
            return !string.IsNullOrEmpty(customKey) ? customKey : holon.Id.ToString();
        }

        private static async Task<string> ReadBlobAsync(BlobClient blob)
        {
            var download = await blob.DownloadContentAsync();
            return download.Value.Content.ToString();
        }

        private static T? Deserialise<T>(string json)
        {
            return NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.OasisJson.DeserializeStored<T>(json);
        }

        private static string Serialise<T>(T obj) =>
            NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.OasisJson.Serialize(obj);

        // ─── Holons ──────────────────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                ArgumentNullException.ThrowIfNull(holon);
                if(maxChildDepth<0) throw new ArgumentOutOfRangeException(nameof(maxChildDepth));
                if(saveChildren)
                {
                    var graph=await SaveHolonsAsync(new[] {holon},true,recursive,maxChildDepth,0,continueOnError,saveChildrenOnProvider);
                    var converted=OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<IHolon>,IHolon>(graph);
                    if(!graph.IsError) converted.Result=holon;
                    return converted;
                }
                var publication=StoredRecordPublicationSnapshot<Holon>.Capture(holon);
                return await SaveCapturedHolonAsync(holon,publication.Proposal,publication.ValidateUnchanged);
            }
            catch (Exception ex) { HandleBlobSaveError(ref result, ex); }
            return result;
        }
        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;

        protected virtual Task<OASISResult<IHolon>> SaveCapturedHolonAsync(IHolon source,Holon staged,Action validatePublication)
            =>SaveCapturedHolonRecordAsync(source,staged,validatePublication,null);

        protected virtual Task<OASISResult<IHolon>> SavePlannedHolonAsync(PlannedHolonWrite write,Action validatePublication)
            =>SaveCapturedHolonRecordAsync(write.Source,write.Snapshot,validatePublication,write);

        private async Task<OASISResult<IHolon>> SaveCapturedHolonRecordAsync(IHolon source,Holon staged,Action validatePublication,PlannedHolonWrite? plan)
        {
            var result=new OASISResult<IHolon>();
            try{
                validatePublication();
                if(staged.Id==Guid.Empty)staged.Id=Guid.NewGuid();
                var key=HolonKey(staged);
                staged.ProviderUniqueStorageKey??=new Dictionary<Core.Enums.ProviderType,string>();
                staged.ProviderUniqueStorageKey[Core.Enums.ProviderType.AzureStorageOASIS]=key;
                var blob=_serviceClient.GetBlobContainerClient(_holonContainer).GetBlobClient(key);
                var committed=await WriteVersionedBlobAsync(blob,staged,IsVersionControlEnabled,validatePublication,plan);
                source.Id=staged.Id;source.ParentHolonId=staged.ParentHolonId;
                PublishBlobVersion(source,committed);
                if(source is Holon caller)caller.ChildRoleManifestJson=committed.ChildRoleManifestJson;
                source.ProviderUniqueStorageKey??=new Dictionary<Core.Enums.ProviderType,string>();
                source.ProviderUniqueStorageKey[Core.Enums.ProviderType.AzureStorageOASIS]=key;
                result.Result=source;result.IsSaved=true;result.Message=$"AzureStorageOASIS: Saved holon blob '{key}'.";
            }catch(Exception cause){HandleBlobSaveError(ref result,cause);}
            return result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var saved = new List<IHolon>();
            var failures=new List<Exception>();
            try {
                var publications=new Dictionary<IHolon,StoredRecordPublicationSnapshot<Holon>>(ReferenceEqualityComparer.Instance);
                var plan=HolonGraphWritePlan.Create(holons,saveChildren,recursive,maxChildDepth,curentChildDepth,
                    source=>publications.Add(source,StoredRecordPublicationSnapshot<Holon>.Capture(source)));
                if(plan.Nodes.Select(node=>HolonKey(node.Snapshot)).Distinct(StringComparer.Ordinal).Count()!=plan.Nodes.Count)
                    throw new ArgumentException("Selected Azure graph has duplicate native blob keys.");
                var failedParents=new HashSet<Guid>();
                foreach(var node in plan.Nodes) {
                    if(failedParents.Contains(node.Snapshot.ParentHolonId)) {
                        failedParents.Add(node.Snapshot.Id);failures.Add(new InvalidOperationException("Descendant skipped because its parent failed."));continue;
                    }
                    var written=await SavePlannedHolonAsync(node,publications[node.Source].ValidateUnchanged);
                    if(written.IsError || written.Result==null) {
                        failedParents.Add(node.Snapshot.Id);
                        if(!result.IsError) result=OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IHolon,IEnumerable<IHolon>>(written);
                        result.IsError=true;
                        failures.Add(written.Exception ?? new InvalidOperationException(written.Message ?? "Azure item returned no acknowledgement."));
                        if(!continueOnError || NativeMutationReconciliation.IsRequired(written.Exception)) break;
                    }
                    else {
                        saved.Add(written.Result);
                    }
                }
                result.Result=failures.Count==0 ? plan.Roots:saved;result.IsSaved=failures.Count==0;
                if(failures.Count>0) {
                    var cause=failures.Count==1 ? failures[0]:new AggregateException(failures);
                    result.Exception=saved.Count>0 ? new AzureBlobPartialMutationException(saved.Select(row=>row.Id),cause):cause;
                }
            }
            catch(Exception cause) {
                result.Exception=saved.Count>0 ? new AzureBlobPartialMutationException(saved.Select(row=>row.Id),cause):cause;
                result.IsSaved=false;OASISErrorHandling.HandleError(ref result,cause.Message);
            }
            return result;
        }
        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => await ReadCanonicalHolonAsync(providerKey,loadChildren,recursive,maxChildDepth,continueOnError,loadChildrenFromProvider,version);

        private async Task<OASISResult<IHolon>> ReadCanonicalHolonAsync(string providerKey,bool loadChildren,bool recursive,int maxChildDepth,bool continueOnError,bool loadChildrenFromProvider,int version,Guid? expectedId=null)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                ValidateDetailVersion(version);
                if(maxChildDepth<0) throw new ArgumentOutOfRangeException(nameof(maxChildDepth));
                if(string.IsNullOrWhiteSpace(providerKey)) throw new ArgumentException("A nonblank Azure holon key is required.");
                var blob = _serviceClient.GetBlobContainerClient(_holonContainer).GetBlobClient(providerKey);
                var json = await ReadBlobAsync(blob);
                var holon = Deserialise<Holon>(json) ?? throw new InvalidDataException("Azure holon record is null.");
                if(holon.Id==Guid.Empty) throw new InvalidDataException("Azure holon record has no identity.");
                if(HolonKey(holon)!=providerKey || expectedId.HasValue && holon.Id!=expectedId.Value)
                    throw new InvalidDataException("Azure holon identity does not match blob key.");
                holon = await ReadBlobHistoryAsync<Holon>(json, version);
                if (holon == null) { result.IsError=true;result.ErrorCode=OASISResultErrorCodes.RecordNotFound;result.Message="Azure holon version not found.";return result; }
                if (HolonKey(holon)!=providerKey || expectedId.HasValue && holon.Id!=expectedId.Value)
                    throw new InvalidDataException("Historical holon identity does not match blob key.");
                holon.ProviderUniqueStorageKey ??= new Dictionary<Core.Enums.ProviderType,string>();
                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.AzureStorageOASIS] = providerKey;
                IEnumerable<IHolon> records=new[] {holon};
                if(loadChildren && loadChildrenFromProvider) {
                    var loaded=await LoadAllHolonsAsync(loadChildren:false,version:0);
                    if(loaded.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<IHolon>,IHolon>(loaded);
                    records=loaded.Result;
                }
                var project=NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.HolonGraphProjection.Create(records,loadChildren,recursive,maxChildDepth,0,loadChildrenFromProvider,
                    source=>Deserialise<Holon>(Serialise(source))!);
                result.Result = project(holon);result.IsLoaded=true;
            }
            catch (Azure.RequestFailedException cause) when(cause.Status==404 && cause.ErrorCode=="BlobNotFound") {
                result.IsError=true;result.ErrorCode=OASISResultErrorCodes.RecordNotFound;result.Message="Azure holon blob not found.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }
        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => await ReadCanonicalHolonAsync(id.ToString(), loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version,id);
        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var holons = new List<IHolon>();
            try
            {
                ValidateDetailVersion(version);
                if(maxChildDepth<0 || curentChildDepth<0) throw new ArgumentOutOfRangeException(nameof(maxChildDepth));
                var container = _serviceClient.GetBlobContainerClient(_holonContainer);
                await foreach (var item in container.GetBlobsAsync())
                {
                    var r = await LoadHolonAsync(item.Name, loadChildren:false, version:version);
                    if (r.IsError && r.ErrorCode==OASISResultErrorCodes.RecordNotFound && r.Exception==null && version>0) continue;
                    if(r.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IHolon,IEnumerable<IHolon>>(r);
                    if(r.Result==null) throw new InvalidDataException("Azure holon read returned no record.");
                    holons.Add(r.Result);
                }
                IEnumerable<IHolon> childRecords = holons;
                if (version>0 && loadChildren && loadChildrenFromProvider) {
                    var latest = await LoadAllHolonsAsync(loadChildren:false,version:0);
                    if(latest.IsError) return latest;
                    childRecords = latest.Result;
                }
                var project=NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.HolonGraphProjection.Create(childRecords,loadChildren,recursive,maxChildDepth,curentChildDepth,loadChildrenFromProvider,
                    source=>Deserialise<Holon>(Serialise(source))!);
                result.Result = holons.Where(row=>type==HolonType.All || row.HolonType==type).Select(project).ToList();result.IsLoaded=true;
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }
        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                ValidateDetailVersion(version);
                var parent=await LoadHolonAsync(providerKey,loadChildren:false,version:0);
                if(parent.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IHolon,IEnumerable<IHolon>>(parent);
                return await LoadHolonsForParentAsync(parent.Result.Id,type,loadChildren,recursive,maxChildDepth,curentChildDepth,continueOnError,loadChildrenFromProvider,version);
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var loaded=await LoadAllHolonsAsync(HolonType.All,loadChildren,recursive,maxChildDepth,curentChildDepth,continueOnError,loadChildrenFromProvider,version);
            if(loaded.IsError) return loaded;
            loaded.Result=loaded.Result.Where(row=>row.ParentHolonId==id && (type==HolonType.All || row.HolonType==type)).ToList();
            return loaded;
        }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
            => DeleteCanonicalHolonAsync(id.ToString(),id);
        public override OASISResult<IHolon> DeleteHolon(Guid id) => DeleteHolonAsync(id).Result;

        public override Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
            => DeleteCanonicalHolonAsync(providerKey);
        public override OASISResult<IHolon> DeleteHolon(string providerKey) => DeleteHolonAsync(providerKey).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        { return await ReadMetadataAsync(metaKey==null ? null! : new Dictionary<string,string> {{metaKey,metaValue}},MetaKeyValuePairMatchMode.All,type,loadChildren,recursive,maxChildDepth,curentChildDepth,continueOnError,loadChildrenFromProvider,version); }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        { return await ReadMetadataAsync(metaKeyValuePairs,metaKeyValuePairMatchMode,type,loadChildren,recursive,maxChildDepth,curentChildDepth,continueOnError,loadChildrenFromProvider,version); }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        { return await ReadSearchAsync(searchParams,loadChildren,recursive,maxChildDepth,continueOnError,version); }
        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
            => SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;

        // ─── Avatars ──────────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
            => await LoadAvatarByProviderKeyAsync(id.ToString(), version);
        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0) => LoadAvatarAsync(id, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
            =>await ReadAvatarByKeyAsync(providerKey,version);

        private async Task<OASISResult<IAvatar>> ReadAvatarByKeyAsync(string providerKey,int version,bool includeDeleted=false)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                ValidateDetailVersion(version);
                if(!Guid.TryParse(providerKey,out var requestedId) || requestedId==Guid.Empty) throw new ArgumentException("Azure avatar key must be a nonempty GUID.");
                var blob = _serviceClient.GetBlobContainerClient(_avatarContainer).GetBlobClient(providerKey);
                var json = await ReadBlobAsync(blob);
                var avatar = Deserialise<Avatar>(json) ?? throw new InvalidDataException("Azure avatar record is null.");
                if(avatar.Id==Guid.Empty) throw new InvalidDataException("Azure avatar record has no identity.");
                if(avatar.Id!=requestedId) throw new InvalidDataException("Azure avatar identity does not match blob key.");
                avatar = await ReadBlobHistoryAsync<Avatar>(json,version);
                if(avatar==null) {result.IsError=true;result.ErrorCode=OASISResultErrorCodes.RecordNotFound;result.Message="Azure avatar version not found.";return result;}
                if(avatar.IsDeleted&&!includeDeleted) {result.IsError=true;result.ErrorCode=OASISResultErrorCodes.RecordNotFound;result.Message="Azure avatar is deleted.";return result;}
                avatar.ProviderUniqueStorageKey ??= new Dictionary<Core.Enums.ProviderType,string>();
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.AzureStorageOASIS] = providerKey;
                result.Result = avatar;result.IsLoaded=true;
            }
            catch (Azure.RequestFailedException cause) when(cause.Status==404 && cause.ErrorCode=="BlobNotFound") {
                result.IsError=true;result.ErrorCode=OASISResultErrorCodes.RecordNotFound;result.Message="Azure avatar blob not found.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }
        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0) => LoadAvatarByProviderKeyAsync(providerKey, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
            => await ReadSelectedAvatarAsync(avatarUsername,false,version);
        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0) => LoadAvatarByUsernameAsync(avatarUsername, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
            => await ReadSelectedAvatarAsync(avatarEmail,true,version);
        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0) => LoadAvatarByEmailAsync(avatarEmail, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
            =>await ReadAvatarCollectionAsync(version);

        private async Task<OASISResult<IEnumerable<IAvatar>>> ReadAvatarCollectionAsync(int version,bool includeDeleted=false)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            var avatars = new List<IAvatar>();
            try
            {
                ValidateDetailVersion(version);
                var container = _serviceClient.GetBlobContainerClient(_avatarContainer);
                await foreach (var item in container.GetBlobsAsync())
                {
                    var r = await ReadAvatarByKeyAsync(item.Name,version,includeDeleted);
                    if(r.IsError) {
                        if(r.ErrorCode==OASISResultErrorCodes.RecordNotFound && r.Exception==null) continue;
                        return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IAvatar,IEnumerable<IAvatar>>(r);
                    }
                    if(r.Result==null) throw new InvalidDataException("Azure avatar read returned no record.");
                    if(includeDeleted||!r.Result.IsDeleted) avatars.Add(r.Result);
                }
                result.Result = avatars;result.IsLoaded=true;
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }
        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0) => LoadAllAvatarsAsync(version).Result;

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                ArgumentNullException.ThrowIfNull(avatar);
                var source=avatar;
                var publication=StoredRecordPublicationSnapshot<Avatar>.Capture(source);
                avatar=publication.Proposal;
                if(avatar.Id==Guid.Empty) avatar.Id=Guid.NewGuid();
                var key = avatar.Id.ToString();
                avatar.ProviderUniqueStorageKey ??= new Dictionary<Core.Enums.ProviderType,string>();
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.AzureStorageOASIS]=key;
                var container = _serviceClient.GetBlobContainerClient(_avatarContainer);
                var blob = container.GetBlobClient(key);
                var committed = await WriteVersionedBlobAsync(blob, (Avatar)avatar, IsVersionControlEnabled,publication.ValidateUnchanged);
                source.Id=avatar.Id;
                PublishBlobVersion(source, committed);
                source.ProviderUniqueStorageKey ??= new Dictionary<Core.Enums.ProviderType,string>();
                source.ProviderUniqueStorageKey[Core.Enums.ProviderType.AzureStorageOASIS] = key;
                result.Result = source;result.IsSaved=true; result.Message = $"AzureStorageOASIS: Saved avatar blob '{key}'.";
            }
            catch (Exception ex) { HandleBlobSaveError(ref result, ex); }
            return result;
        }
        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar) => SaveAvatarAsync(avatar).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        { return await ReadDetailAsync(id,version); }
        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) => LoadAvatarDetailAsync(id, version).Result;
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
        { return await ReadSelectedDetailAsync(avatarEmail,true,version); }
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0) => LoadAvatarDetailByEmailAsync(avatarEmail, version).Result;
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
        { return await ReadSelectedDetailAsync(avatarUsername,false,version); }
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0) => LoadAvatarDetailByUsernameAsync(avatarUsername, version).Result;
        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        { return await ReadAllDetailsAsync(version); }
        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0) => LoadAllAvatarDetailsAsync(version).Result;
        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatar)
        { return await WriteDetailAsync(avatar); }
        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatar) => SaveAvatarDetailAsync(avatar).Result;

        public override Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
            => ApplyAvatarLifecycleAsync(id,softDelete);
        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) => DeleteAvatarAsync(id, softDelete).Result;
        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try { if(!Guid.TryParse(providerKey,out var id) || id==Guid.Empty) throw new ArgumentException("Azure avatar key must be a nonempty GUID.");return await DeleteAvatarAsync(id,softDelete); }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }
        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true) => DeleteAvatarAsync(providerKey, softDelete).Result;
        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
            => await DeleteSelectedAvatarAsync(ReadSelectedAvatarAsync(avatarEmail,true,0,includeDeleted:true),softDelete,profile=>string.Equals(profile.Email,avatarEmail,StringComparison.OrdinalIgnoreCase));
        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true) => DeleteAvatarByEmailAsync(avatarEmail, softDelete).Result;
        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
            => await DeleteSelectedAvatarAsync(ReadSelectedAvatarAsync(avatarUsername,false,0,includeDeleted:true),softDelete,profile=>string.Equals(profile.Username,avatarUsername,StringComparison.OrdinalIgnoreCase));
        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true) => DeleteAvatarByUsernameAsync(avatarUsername, softDelete).Result;

        // ─── Import / Export ──────────────────────────────────────────────────────
        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        { var saved=await SaveHolonsAsync(holons,continueOnError:false);var result=OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<IHolon>,bool>(saved);result.Result=!saved.IsError && saved.IsSaved;return result; }
        public override OASISResult<bool> Import(IEnumerable<IHolon> holons) => ImportAsync(holons).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
            => await ExportOwnedHolonsAsync(avatarId,version);
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0) => ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
            => await ExportSelectedAvatarAsync(avatarUsername,false,version);
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0) => ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
            => await ExportSelectedAvatarAsync(avatarEmailAddress,true,version);
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0) => ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
            => await LoadAllHolonsAsync(loadChildrenFromProvider:true,version: version);
        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0) => ExportAllAsync(version).Result;

        // ─── IOASISNETProvider ────────────────────────────────────────────────────
        public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
            => ReadNearbyAsync(geoLat,geoLong,radiusInMeters,()=>LoadAllAvatarsAsync()).GetAwaiter().GetResult();
        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
            => ReadNearbyAsync(geoLat,geoLong,radiusInMeters,()=>LoadAllHolonsAsync(HolonType.All,loadChildren:false),Type).GetAwaiter().GetResult();
    }
}
