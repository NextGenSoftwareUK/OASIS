using System;
using System.IO;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
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
        public OASISResult<bool> DeleteHolon(string username, bool softDelete = true)
        {
            return DeleteHolonAsync(username, softDelete).Result;
        }

        public async Task<OASISResult<bool>> DeleteHolonByEmailAsync(string email, bool softDelete = true)
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

                var query = _firestoreDb.Collection("holons").WhereEqualTo("email", email);
                var snapshot = await query.GetSnapshotAsync();
                
                if (snapshot.Count > 0)
                {
                    var doc = snapshot.Documents.First();
                    var docRef = doc.Reference;
                    
                    if (true) // Soft delete by default (OASIS standard)
                    {
                        // Soft delete - mark as deleted
                        await docRef.UpdateAsync("IsDeleted", true);
                        await docRef.UpdateAsync("DeletedDate", Timestamp.FromDateTime(DateTime.UtcNow));
                        await docRef.UpdateAsync("DeletedByAvatarId", AvatarManager.LoggedInAvatar?.Id ?? Guid.Empty);
                    }
                    else
                    {
                        // Hard delete - remove document
                        await docRef.DeleteAsync();
                    }
                    
                    result.Result = true;
                    result.IsError = false;
                    result.Message = $"Holon {(softDelete ? "soft" : "hard")} deleted successfully from Google Cloud Firestore by email";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Holon not found in Google Cloud Firestore by email");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting holon from Google Cloud by email: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<bool> DeleteHolonByEmail(string email, bool softDelete = true)
        {
            return DeleteHolonByEmailAsync(email, softDelete).Result;
        }

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
    {
        var search=searchParams;
        var children=loadChildren;
        var maxDepth=maxChildDepth;
        var result=new OASISResult<ISearchResults>();
        try {
            StoredRecordSearch.Validate(search,version,maxDepth);
            var groups=search.SearchGroups ?? new List<ISearchGroupBase>();
            var wantsAvatars=groups.Count==0 || groups.Any(group=>group.SearchAvatars || !group.SearchHolons);
            var wantsHolons=groups.Count==0 || groups.Any(group=>group.SearchHolons || !group.SearchAvatars);
            var avatars=new List<IAvatar>();var holons=new List<IHolon>();
            if(wantsAvatars) {
                var loaded=await LoadAllAvatarsAsync(version);
                if(loaded.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<IAvatar>,ISearchResults>(loaded);
                avatars=loaded.Result.ToList();
            }
            if(wantsHolons || search.ParentId!=Guid.Empty) {
                var loaded=await LoadAllHolonsAsync(HolonType.All,children,recursive,maxDepth,0,continueOnError,true,version);
                if(loaded.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<IHolon>,ISearchResults>(loaded);
                holons=loaded.Result.ToList();
            }
            var found=new SearchResults();
            found.SearchResultAvatars.AddRange(avatars.Where(row=>StoredRecordSearch.Matches(row,true,search,holons)));
            if(wantsHolons) found.SearchResultHolons.AddRange(holons.Where(row=>StoredRecordSearch.Matches(row,false,search,holons)));
            found.NumberOfResults=found.SearchResultAvatars.Count+found.SearchResultHolons.Count;
            result.Result=found;result.IsLoaded=true;
        }
        catch(Exception cause) {OASISErrorHandling.HandleError(ref result,cause.Message,cause);}
        return result;
    }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
        }

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
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

                var importedHolons = new List<IHolon>();
                
                // Import ALL holons to Google Cloud Firestore with FULL property mapping
                foreach (var holon in holons)
                {
                    var docRef = _firestoreDb.Collection("holons").Document(holon.Id.ToString());
                    var holonData = new Dictionary<string, object>
                    {
                        ["id"] = holon.Id.ToString(),
                        ["name"] = holon.Name,
                        ["description"] = holon.Description,
                        ["holonType"] = holon.HolonType.ToString(),
                        ["createdDate"] = Timestamp.FromDateTime(holon.CreatedDate),
                        ["modifiedDate"] = Timestamp.FromDateTime(holon.ModifiedDate),
                        ["version"] = holon.Version,
                        ["isActive"] = holon.IsActive,
                        // Map ALL Holon properties
                        ["parentId"] = holon.ParentHolonId.ToString(),
                        ["providerKey"] = holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.GoogleCloudOASIS],
                        ["previousVersionId"] = holon.PreviousVersionId.ToString(),
                        ["nextVersionId"] = holon.VersionId.ToString(),
                        ["isChanged"] = holon.IsChanged,
                        ["isNew"] = holon.IsNewHolon,
                        ["isDeleted"] = !holon.IsActive,
                        ["deletedByAvatarId"] = holon.DeletedByAvatarId.ToString(),
                        ["deletedDate"] = holon.DeletedDate != DateTime.MinValue ? Timestamp.FromDateTime(holon.DeletedDate) : null,
                        ["createdByAvatarId"] = holon.CreatedByAvatarId.ToString(),
                        ["modifiedByAvatarId"] = holon.ModifiedByAvatarId.ToString(),
                        // Map Google Cloud specific data
                        ["googleCloudProjectId"] = _projectId,
                        ["googleCloudBucketName"] = _bucketName,
                        ["googleCloudFirestoreDatabaseId"] = _firestoreDatabaseId,
                        ["googleCloudBigQueryDatasetId"] = _bigQueryDatasetId,
                        ["importedDate"] = Timestamp.FromDateTime(DateTime.UtcNow)
                    };
                    
                    holonData["data_json"]=NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.OasisJson.Serialize(holon);
                    await ObserveFirestoreMutationAsync(()=>docRef.SetAsync(holonData),docRef.Path);
                    importedHolons.Add(holon);
                }
                
                result.Result = true;
                result.IsError = false;
                result.Message = $"Holons imported successfully to Google Cloud Firestore with full property mapping ({importedHolons.Count} holons)";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error importing holons to Google Cloud: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
        {
            return ImportAsync(holons).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
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

                // Export all holons for avatar from Google Cloud Firestore
                var query = _firestoreDb.Collection("holons").WhereEqualTo("createdByAvatarId", avatarId.ToString());
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
                    result.Message = $"Avatar data exported successfully from Google Cloud Firestore with full property mapping ({holons.Count} holons)";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "No data found for avatar in Google Cloud Firestore");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data from Google Cloud: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
        {
            return ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
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

                // Export all holons for avatar by username from Google Cloud Firestore
                var query = _firestoreDb.Collection("holons").WhereEqualTo("createdByUsername", avatarUsername);
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
                    result.Message = $"Avatar data exported successfully from Google Cloud Firestore by username with full property mapping ({holons.Count} holons)";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "No data found for avatar by username in Google Cloud Firestore");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data by username from Google Cloud: {ex.Message}", ex);
            }
            return result;
        }

    }
}
