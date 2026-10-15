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
                ArgumentNullException.ThrowIfNull(avatarDetail);
                if(avatarDetail.Id==Guid.Empty) avatarDetail.Id=Guid.NewGuid();
                var docRef = _firestoreDb.Collection("avatarDetails").Document(avatarDetail.Id.ToString());
                var avatarDetailData = EncodeAvatarDetail(avatarDetail);
                
                await ObserveFirestoreMutationAsync(()=>docRef.SetAsync(avatarDetailData),docRef.Path);
                
                result.Result = avatarDetail;
                result.IsSaved = true;
                result.IsError = false;
                result.Message = "Avatar detail saved successfully to Google Cloud Firestore with full property mapping";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving avatar detail to Google Cloud: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail)
        {
            return SaveAvatarDetailAsync(avatarDetail).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Google Cloud provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (!_enableFirestore)
                {
                    OASISErrorHandling.HandleError(ref result, "Firestore is not enabled");
                    return result;
                }

                // Delete avatar from Firestore
                var docRef = _firestoreDb.Collection("avatars").Document(id.ToString());
                
                if (softDelete)
                {
                    // Soft delete - mark as deleted
                    var updateData = new Dictionary<string, object>
                    {
                        ["IsDeleted"] = true,
                        ["DeletedDate"] = Timestamp.FromDateTime(DateTime.UtcNow),
                        ["DeletedByAvatarId"] = Guid.Empty
                    };
                    await docRef.UpdateAsync(updateData);
                }
                else
                {
                    // Hard delete - remove document
                    await docRef.DeleteAsync();
                }
                
                result.Result = true;
                result.IsError = false;
                result.Message = $"Avatar {(softDelete ? "soft" : "hard")} deleted successfully from Google Cloud Firestore";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar from Google Cloud: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
        {
            return DeleteAvatarAsync(id, softDelete).Result;
        }

        public override Task<OASISResult<IHolon>> LoadHolonAsync(Guid id,bool loadChildren=true,bool recursive=true,int maxChildDepth=0,bool continueOnError=true,bool loadChildrenFromProvider=false,int version=0)
            => ReadCanonicalHolonByIdAsync(id,maxChildDepth,version);

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Google Cloud provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (!_enableFirestore)
                {
                    OASISErrorHandling.HandleError(ref result, "Firestore is not enabled");
                    return result;
                }

                if (holon.Id == Guid.Empty)
                    holon.Id = Guid.NewGuid();
                holon.ProviderUniqueStorageKey ??= new Dictionary<Core.Enums.ProviderType, string>();
                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.GoogleCloudOASIS] = holon.Id.ToString();

                // Save holon to Firestore with FULL property mapping
                var docRef = _firestoreDb.Collection("holons").Document(holon.Id.ToString());
                var holonData = new Dictionary<string, object>
                {
                    ["id"] = holon.Id.ToString(),
                    ["name"] = holon.Name,
                    ["description"] = holon.Description,
                    ["holonType"] = holon.HolonType.ToString(),
                    ["createdDate"] = ToFirestoreTimestamp(holon.CreatedDate),
                    ["modifiedDate"] = ToFirestoreTimestamp(holon.ModifiedDate),
                    ["version"] = holon.Version,
                    ["isActive"] = holon.IsActive,
                    // Map ALL Holon properties to Google Cloud fields
                    ["parentId"] = holon.ParentHolonId.ToString(),
                    ["providerKey"] = holon.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.GoogleCloudOASIS) ? holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.GoogleCloudOASIS] : "",
                    ["previousVersionId"] = holon.PreviousVersionId.ToString(),
                    ["nextVersionId"] = holon.VersionId.ToString(),
                    ["isChanged"] = holon.IsChanged,
                    ["isNew"] = holon.IsNewHolon,
                    ["isDeleted"] = !holon.IsActive,
                    ["deletedByAvatarId"] = holon.DeletedByAvatarId.ToString(),
                    ["deletedDate"] = ToFirestoreTimestamp(holon.DeletedDate),
                    ["createdByAvatarId"] = holon.CreatedByAvatarId.ToString(),
                    ["modifiedByAvatarId"] = holon.ModifiedByAvatarId.ToString(),
                    // Map Google Cloud specific metadata
                    ["googleCloudProjectId"] = _projectId,
                    ["googleCloudBucketName"] = _bucketName,
                    ["googleCloudFirestoreDatabaseId"] = _firestoreDatabaseId,
                    ["googleCloudBigQueryDatasetId"] = _bigQueryDatasetId,
                    ["savedAt"] = Timestamp.GetCurrentTimestamp()
                };
                
                holonData["data_json"]=NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.OasisJson.Serialize(holon);
                await ObserveFirestoreMutationAsync(()=>docRef.SetAsync(holonData),docRef.Path);
                
                result.Result = holon;
                result.IsError = false;
                result.Message = "Holon saved successfully to Google Cloud Firestore with full property mapping";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving holon to Google Cloud: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Google Cloud provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (!_enableFirestore)
                {
                    OASISErrorHandling.HandleError(ref result, "Firestore is not enabled");
                    return result;
                }

                // Load all holons from Firestore
                var query = _firestoreDb.Collection("holons");
                var snapshot = await query.GetSnapshotAsync();
                
                if (snapshot.Count > 0)
                {
                    var holons = new List<IHolon>();
                    
                    // Convert ALL Firestore documents to OASIS Holons with FULL property mapping
                    foreach (var doc in snapshot.Documents)
                    {
                        var holonData = doc.ConvertTo<Dictionary<string, object>>();
                    var holon = DecodeHolon(holonData,doc.Id);
                        
                        holons.Add(holon);
                    }
                    
                    result.Result = holons;
                    result.IsError = false;
                    result.Message = $"Holons loaded successfully from Google Cloud Firestore with full property mapping ({holons.Count} holons)";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "No holons found in Google Cloud Firestore");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons from Google Cloud: {ex.Message}", ex);
            }
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
        {
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Google Cloud provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (!_enableFirestore)
                {
                    OASISErrorHandling.HandleError(ref result, "Firestore is not enabled");
                    return result;
                }

                // Load holons for parent from Firestore
                var query = _firestoreDb.Collection("holons").WhereEqualTo("parentId", id.ToString());
                var snapshot = await query.GetSnapshotAsync();
                
                if (snapshot.Count > 0)
                {
                    var holons = new List<IHolon>();
                    
                    // Convert ALL Firestore documents to OASIS Holons with FULL property mapping
                    foreach (var doc in snapshot.Documents)
                    {
                        var holonData = doc.ConvertTo<Dictionary<string, object>>();
                    var holon = DecodeHolon(holonData,doc.Id);
                        
                        holons.Add(holon);
                    }
                    
                    result.Result = holons;
                    result.IsError = false;
                    result.Message = $"Holons for parent loaded successfully from Google Cloud Firestore with full property mapping ({holons.Count} holons)";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "No holons found for parent in Google Cloud Firestore");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent from Google Cloud: {ex.Message}", ex);
            }
            return result;
        }

    }
}
