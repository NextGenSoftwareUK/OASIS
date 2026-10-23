using System;
using System.IO;
using System.Data;
using System.Text;
using System.Linq;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Google.Cloud.Storage.V1;
using Google.Cloud.Firestore;
using Google.Cloud.BigQuery.V2;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS
{
    public partial class GoogleCloudOASIS
    {
        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar)
        {
            return SaveAvatarAsync(avatar).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
        {
            OASISResult<IAvatarDetail> result = new OASISResult<IAvatarDetail>();
            try
            {
                ArgumentNullException.ThrowIfNull(avatarDetail);
                var publication=NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.StoredRecordPublicationSnapshot<AvatarDetail>.Capture(avatarDetail);
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<bool,IAvatarDetail>(activateResult);
                    }
                }

                if (!_enableFirestore)
                {
                    OASISErrorHandling.HandleError(ref result, "Firestore is not enabled");
                    return result;
                }

                // Save avatar detail to Firestore with FULL property mapping
                var snapshot=publication.Proposal;
                if(snapshot.Id==Guid.Empty) snapshot.Id=Guid.NewGuid();
                snapshot.ProviderUniqueStorageKey ??=new Dictionary<Core.Enums.ProviderType,string>();
                snapshot.ProviderUniqueStorageKey[Core.Enums.ProviderType.GoogleCloudOASIS]=snapshot.Id.ToString();
                var docRef = _firestoreDb.Collection("avatarDetails").Document(snapshot.Id.ToString());
                var avatarDetailData = EncodeAvatarDetail(snapshot);
                
                var committed=await WriteFirestoreRecordAsync<AvatarDetail>(docRef,avatarDetailData,IsVersionControlEnabled,publication.ValidateUnchanged);
                ValidateFirestoreCallerPublication(publication.ValidateUnchanged,committed.Id);
                avatarDetail.Version=committed.Version;avatarDetail.VersionId=committed.VersionId;avatarDetail.PreviousVersionId=committed.PreviousVersionId;
                avatarDetail.Id=snapshot.Id;
                avatarDetail.ProviderUniqueStorageKey ??=new Dictionary<Core.Enums.ProviderType,string>();
                avatarDetail.ProviderUniqueStorageKey[Core.Enums.ProviderType.GoogleCloudOASIS]=snapshot.Id.ToString();
                
                result.Result = avatarDetail;
                result.IsSaved = true;
                result.SavedCount = 1;
                result.IsError = false;
                result.Message = "Avatar detail saved successfully to Google Cloud Firestore with full property mapping";
            }
            catch (Exception ex)
            {
                HandleFirestoreSaveError(ref result, $"Error saving avatar detail to Google Cloud: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail)
        {
            return SaveAvatarDetailAsync(avatarDetail).Result;
        }

        public override Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
            => DeleteCanonicalAvatarAsync(id,softDelete);

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
        {
            return DeleteAvatarAsync(id, softDelete).Result;
        }

        public override Task<OASISResult<IHolon>> LoadHolonAsync(Guid id,bool loadChildren=true,bool recursive=true,int maxChildDepth=0,bool continueOnError=true,bool loadChildrenFromProvider=false,int version=0)
            => ReadCanonicalHolonByIdAsync(id,loadChildren,recursive,maxChildDepth,loadChildrenFromProvider,version);

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();
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
                return await SaveHolonRecordAsync(holon,null);
            }
            catch(Exception cause){HandleFirestoreSaveError(ref result,$"Error saving holon to Google Cloud: {cause.Message}",cause);return result;}
        }

        protected virtual Task<OASISResult<IHolon>> SavePlannedHolonAsync(NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.PlannedHolonWrite write)
            =>SaveHolonRecordAsync(write.Snapshot,write);

        private async Task<OASISResult<IHolon>> SaveHolonRecordAsync(IHolon holon,NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.PlannedHolonWrite plan)
        {
            var result=new OASISResult<IHolon>();
            try{
                var source=holon;
                var publication=NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.StoredRecordPublicationSnapshot<Holon>.Capture(source);
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<bool,IHolon>(activateResult);
                    }
                }

                if (!_enableFirestore)
                {
                    OASISErrorHandling.HandleError(ref result, "Firestore is not enabled");
                    return result;
                }

                holon=publication.Proposal;
                if (holon.Id == Guid.Empty)
                    holon.Id = Guid.NewGuid();
                holon.ProviderUniqueStorageKey ??= new Dictionary<Core.Enums.ProviderType, string>();
                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.GoogleCloudOASIS] = holon.Id.ToString();

                // Save holon to Firestore with FULL property mapping
                var docRef = _firestoreDb.Collection("holons").Document(holon.Id.ToString());
                var holonData=CreateFirestoreHolonFields(holon,NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.OasisJson.Serialize(holon));
                var committed=await WriteFirestoreRecordAsync<Holon>(docRef,holonData,IsVersionControlEnabled,publication.ValidateUnchanged,plan);
                ValidateFirestoreCallerPublication(publication.ValidateUnchanged,committed.Id);
                source.Version=committed.Version;source.VersionId=committed.VersionId;source.PreviousVersionId=committed.PreviousVersionId;
                if(source is Holon caller)caller.ChildRoleManifestJson=committed.ChildRoleManifestJson;
                source.Id=holon.Id;
                source.ProviderUniqueStorageKey ??= new Dictionary<Core.Enums.ProviderType,string>();
                source.ProviderUniqueStorageKey[Core.Enums.ProviderType.GoogleCloudOASIS]=holon.Id.ToString();
                var revision=CosmicConcurrency.GetProviderRevisionId(committed,Core.Enums.ProviderType.GoogleCloudOASIS);
                if(revision.HasValue)CosmicConcurrency.SetProviderRevisionId(source,Core.Enums.ProviderType.GoogleCloudOASIS,revision.Value);
                
                result.Result = source;
                result.IsSaved = true;
                result.SavedCount = 1;
                result.IsError = false;
                result.Message = "Holon saved successfully to Google Cloud Firestore with full property mapping";
            }
            catch (Exception ex)
            {
                HandleFirestoreSaveError(ref result, $"Error saving holon to Google Cloud: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!Enum.IsDefined(typeof(HolonType),type))
                    throw new ArgumentOutOfRangeException(nameof(type));
                if (maxChildDepth < 0 || curentChildDepth < 0)
                    throw new ArgumentOutOfRangeException(nameof(maxChildDepth));
                var ready = await EnsureDetailStoreAsync(version);
                if (ready.IsError)
                    return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<bool, IEnumerable<IHolon>>(ready);
                var snapshot = await _firestoreDb.Collection("holons").GetSnapshotAsync();
                var records = snapshot.Documents.Select(doc => ReadFirestoreHistoryAsync<Holon>(doc.ToDictionary(),doc.Id,version).GetAwaiter().GetResult())
                    .Where(row => row!=null&&row.DeletedDate==DateTime.MinValue).Cast<IHolon>().ToList();
                var childPool=loadChildren&&loadChildrenFromProvider&&version>0
                    ? snapshot.Documents.Select(doc=>DecodeHolon(doc.ToDictionary(),doc.Id)).Where(row=>row.DeletedDate==DateTime.MinValue).Cast<IHolon>().ToList()
                    : records;
                var project = NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.HolonGraphProjection.Create(
                    childPool, loadChildren, recursive, maxChildDepth, curentChildDepth, loadChildrenFromProvider,
                    source => NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.OasisJson.Deserialize<Holon>(
                        NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.OasisJson.Serialize(source)));
                result.Result = records.Where(row => type == HolonType.All || row.HolonType == type).Select(project).ToList();
                result.LoadedCount=result.Result.Count();result.IsLoaded = true;
            }
            catch (Exception cause) { OASISErrorHandling.HandleError(ref result, cause.Message, cause); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        // Additional missing methods with full object mapping
        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            if (!Guid.TryParse(providerKey, out var id) || id == Guid.Empty)
            {
                var invalid = new OASISResult<IHolon>();
                var cause = new ArgumentException("A nonempty GUID Firestore holon storage key is required.", nameof(providerKey));
                OASISErrorHandling.HandleError(ref invalid, cause.Message, cause);
                return invalid;
            }
            return await LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => await ReadCanonicalParentHolonsAsync(id,type,loadChildren,recursive,maxChildDepth,curentChildDepth,continueOnError,loadChildrenFromProvider,version);

    }
}
