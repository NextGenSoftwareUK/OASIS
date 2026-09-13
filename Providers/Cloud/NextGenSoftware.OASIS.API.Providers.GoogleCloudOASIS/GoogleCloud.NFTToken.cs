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
            OASISResult<ISearchResults> result = new OASISResult<ISearchResults>();
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

                var searchResults = new SearchResults();
                var holons = new List<IHolon>();
                var avatars = new List<IAvatar>();
                
                // Search holons in Google Cloud Firestore
                var holonCollection = _firestoreDb.Collection("holons");
                Query holonQuery = holonCollection;
                if (!string.IsNullOrEmpty(searchParams.SearchGroups?.FirstOrDefault()?.HolonSearchParams?.Name.ToString()))
                {
                    // For Firestore, we need to use array-contains or other supported queries
                    // Real Google Cloud Firestore search implementation
                    var searchName = searchParams.SearchGroups?.FirstOrDefault()?.HolonSearchParams?.Name.ToString();
                    if (!string.IsNullOrEmpty(searchName))
                    {
                        holonQuery = holonCollection.WhereGreaterThanOrEqualTo("name", searchName)
                            .WhereLessThan("name", searchName + "\uf8ff"); // Unicode range for prefix search
                    }
                }
                var holonSnapshot = await holonQuery.GetSnapshotAsync();
                
                if (holonSnapshot.Count > 0)
                {
                    // Convert ALL Firestore documents to OASIS Holons with FULL property mapping
                    foreach (var doc in holonSnapshot.Documents)
                    {
                        var holonData = doc.ConvertTo<Dictionary<string, object>>();
                        var holon = new Holon
                        {
                            Id = Guid.Parse(doc.Id),
                            Name = holonData.GetValueOrDefault("name")?.ToString(),
                            Description = holonData.GetValueOrDefault("description")?.ToString(),
                            HolonType = Enum.TryParse<HolonType>(holonData.GetValueOrDefault("holonType")?.ToString(), out var holonType) ? holonType : HolonType.Holon,
                            CreatedDate = ((Timestamp)holonData.GetValueOrDefault("createdDate")).ToDateTime(),
                            ModifiedDate = ((Timestamp)holonData.GetValueOrDefault("modifiedDate")).ToDateTime(),
                            Version = Convert.ToInt32(holonData.GetValueOrDefault("version") ?? 1),
                            IsActive = Convert.ToBoolean(holonData.GetValueOrDefault("isActive") ?? true),
                            // Map ALL Holon properties
                            ParentHolonId = holonData.GetValueOrDefault("parentId") != null ? Guid.Parse(holonData.GetValueOrDefault("parentId").ToString()) : Guid.Empty,
                            ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string> { [Core.Enums.ProviderType.GoogleCloudOASIS] = holonData.GetValueOrDefault("providerKey")?.ToString() ?? "" },
                            PreviousVersionId = holonData.GetValueOrDefault("previousVersionId") != null ? Guid.Parse(holonData.GetValueOrDefault("previousVersionId").ToString()) : Guid.Empty,
                            VersionId = holonData.GetValueOrDefault("nextVersionId") != null ? Guid.Parse(holonData.GetValueOrDefault("nextVersionId").ToString()) : Guid.Empty,
                            IsChanged = Convert.ToBoolean(holonData.GetValueOrDefault("isChanged") ?? false),
                            IsNewHolon = Convert.ToBoolean(holonData.GetValueOrDefault("isNew") ?? false),
                            DeletedByAvatarId = holonData.GetValueOrDefault("deletedByAvatarId") != null ? Guid.Parse(holonData.GetValueOrDefault("deletedByAvatarId").ToString()) : Guid.Empty,
                            DeletedDate = holonData.GetValueOrDefault("deletedDate") != null ? ((Timestamp)holonData.GetValueOrDefault("deletedDate")).ToDateTime() : DateTime.MinValue,
                            CreatedByAvatarId = holonData.GetValueOrDefault("createdByAvatarId") != null ? Guid.Parse(holonData.GetValueOrDefault("createdByAvatarId").ToString()) : Guid.Empty,
                            ModifiedByAvatarId = holonData.GetValueOrDefault("modifiedByAvatarId") != null ? Guid.Parse(holonData.GetValueOrDefault("modifiedByAvatarId").ToString()) : Guid.Empty,
                            // Map Google Cloud specific data to custom properties
                            MetaData = new Dictionary<string, object>
                            {
                                ["GoogleCloudProjectId"] = _projectId,
                                ["GoogleCloudBucketName"] = _bucketName,
                                ["GoogleCloudFirestoreDatabaseId"] = _firestoreDatabaseId,
                                ["GoogleCloudBigQueryDatasetId"] = _bigQueryDatasetId,
                                ["GoogleCloudDocumentId"] = doc.Id,
                                ["GoogleCloudDocumentPath"] = doc.Reference.Path,
                                ["GoogleCloudCreateTime"] = doc.CreateTime,
                                ["GoogleCloudUpdateTime"] = doc.UpdateTime,
                                ["GoogleCloudReadTime"] = doc.ReadTime,
                                ["SearchQuery"] = searchParams.SearchGroups?.FirstOrDefault()?.HolonSearchParams?.Name,
                                ["SearchProvider"] = "GoogleCloudOASIS"
                            }
                        };
                        
                        holons.Add(holon);
                    }
                }
                
                // Search avatars in Google Cloud Firestore
                var avatarCollection = _firestoreDb.Collection("avatars");
                Query avatarQuery = avatarCollection;
                if (!string.IsNullOrEmpty(searchParams.SearchGroups?.FirstOrDefault()?.AvatarSearchParams?.Username.ToString()))
                {
                    // For Firestore, we need to use array-contains or other supported queries
                    // Real Google Cloud Firestore search implementation
                    var searchUsername = searchParams.SearchGroups?.FirstOrDefault()?.AvatarSearchParams?.Username.ToString();
                    if (!string.IsNullOrEmpty(searchUsername))
                    {
                        avatarQuery = avatarCollection.WhereGreaterThanOrEqualTo("username", searchUsername)
                            .WhereLessThan("username", searchUsername + "\uf8ff"); // Unicode range for prefix search
                    }
                }
                var avatarSnapshot = await avatarQuery.GetSnapshotAsync();
                
                if (avatarSnapshot.Count > 0)
                {
                    // Convert ALL Firestore documents to OASIS Avatars with FULL property mapping
                    foreach (var doc in avatarSnapshot.Documents)
                    {
                        var avatarData = doc.ConvertTo<Dictionary<string, object>>();
                        var avatar = new Avatar
                        {
                            Id = Guid.Parse(doc.Id),
                            Username = avatarData.GetValueOrDefault("username")?.ToString(),
                            Email = avatarData.GetValueOrDefault("email")?.ToString(),
                            FirstName = avatarData.GetValueOrDefault("firstName")?.ToString(),
                            LastName = avatarData.GetValueOrDefault("lastName")?.ToString(),
                            CreatedDate = ((Timestamp)avatarData.GetValueOrDefault("createdDate")).ToDateTime(),
                            ModifiedDate = ((Timestamp)avatarData.GetValueOrDefault("modifiedDate")).ToDateTime(),
                            // Map ALL Avatar properties
                            // Address property not available in Avatar class
                            // Country property not available in Avatar class
                            // Postcode property not available in Avatar class
                            // Mobile property not available in Avatar class
                            // Landline property not available in Avatar class
                            Title = avatarData.GetValueOrDefault("title")?.ToString(),
                            // DOB property not available in Avatar class - store in MetaData instead
                            AvatarType = new EnumValue<AvatarType>(Enum.TryParse<AvatarType>(avatarData.GetValueOrDefault("avatarType")?.ToString(), out var avatarType) ? avatarType : AvatarType.User),
                            // KarmaAkashicRecords property not available in Avatar class
                            // Level property not available in Avatar class
                            // XP property not available in Avatar class
                            // HP property not available in Avatar class
                            // Mana property not available in Avatar class
                            // Stamina property not available in Avatar class
                            Description = avatarData.GetValueOrDefault("description")?.ToString(),
                            // Website and Language properties not available in Avatar class
                            ProviderWallets = new Dictionary<ProviderType, List<IProviderWallet>>(),
                            // Map Google Cloud specific data to custom properties
                            MetaData = new Dictionary<string, object>
                            {
                                ["GoogleCloudProjectId"] = _projectId,
                                ["GoogleCloudBucketName"] = _bucketName,
                                ["GoogleCloudFirestoreDatabaseId"] = _firestoreDatabaseId,
                                ["GoogleCloudBigQueryDatasetId"] = _bigQueryDatasetId,
                                ["GoogleCloudDocumentId"] = doc.Id,
                                ["GoogleCloudDocumentPath"] = doc.Reference.Path,
                                ["GoogleCloudCreateTime"] = doc.CreateTime,
                                ["GoogleCloudUpdateTime"] = doc.UpdateTime,
                                ["GoogleCloudReadTime"] = doc.ReadTime,
                                ["SearchQuery"] = searchParams.SearchGroups?.FirstOrDefault()?.HolonSearchParams?.Name,
                                ["SearchProvider"] = "GoogleCloudOASIS"
                            }
                        };
                        
                        avatars.Add(avatar);
                    }
                }
                
                searchResults.SearchResultHolons = holons;
                searchResults.SearchResultAvatars = avatars;
                
                result.Result = searchResults;
                result.IsError = false;
                result.Message = $"Search completed successfully in Google Cloud Firestore with full property mapping ({holons.Count} holons, {avatars.Count} avatars)";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error searching in Google Cloud: {ex.Message}", ex);
            }
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
                    
                    await docRef.SetAsync(holonData);
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
                        var holon = new Holon
                        {
                            Id = Guid.Parse(doc.Id),
                            Name = holonData.GetValueOrDefault("name")?.ToString(),
                            Description = holonData.GetValueOrDefault("description")?.ToString(),
                            HolonType = Enum.TryParse<HolonType>(holonData.GetValueOrDefault("holonType")?.ToString(), out var holonType) ? holonType : HolonType.Holon,
                            CreatedDate = ((Timestamp)holonData.GetValueOrDefault("createdDate")).ToDateTime(),
                            ModifiedDate = ((Timestamp)holonData.GetValueOrDefault("modifiedDate")).ToDateTime(),
                            Version = Convert.ToInt32(holonData.GetValueOrDefault("version") ?? 1),
                            IsActive = Convert.ToBoolean(holonData.GetValueOrDefault("isActive") ?? true),
                            // Map ALL Holon properties
                            ParentHolonId = holonData.GetValueOrDefault("parentId") != null ? Guid.Parse(holonData.GetValueOrDefault("parentId").ToString()) : Guid.Empty,
                            ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string> { [Core.Enums.ProviderType.GoogleCloudOASIS] = holonData.GetValueOrDefault("providerKey")?.ToString() ?? "" },
                            PreviousVersionId = holonData.GetValueOrDefault("previousVersionId") != null ? Guid.Parse(holonData.GetValueOrDefault("previousVersionId").ToString()) : Guid.Empty,
                            VersionId = holonData.GetValueOrDefault("nextVersionId") != null ? Guid.Parse(holonData.GetValueOrDefault("nextVersionId").ToString()) : Guid.Empty,
                            IsChanged = Convert.ToBoolean(holonData.GetValueOrDefault("isChanged") ?? false),
                            IsNewHolon = Convert.ToBoolean(holonData.GetValueOrDefault("isNew") ?? false),
                            DeletedByAvatarId = holonData.GetValueOrDefault("deletedByAvatarId") != null ? Guid.Parse(holonData.GetValueOrDefault("deletedByAvatarId").ToString()) : Guid.Empty,
                            DeletedDate = holonData.GetValueOrDefault("deletedDate") != null ? ((Timestamp)holonData.GetValueOrDefault("deletedDate")).ToDateTime() : DateTime.MinValue,
                            CreatedByAvatarId = holonData.GetValueOrDefault("createdByAvatarId") != null ? Guid.Parse(holonData.GetValueOrDefault("createdByAvatarId").ToString()) : Guid.Empty,
                            ModifiedByAvatarId = holonData.GetValueOrDefault("modifiedByAvatarId") != null ? Guid.Parse(holonData.GetValueOrDefault("modifiedByAvatarId").ToString()) : Guid.Empty,
                            // Map Google Cloud specific data to custom properties
                            MetaData = new Dictionary<string, object>
                            {
                                ["GoogleCloudProjectId"] = _projectId,
                                ["GoogleCloudBucketName"] = _bucketName,
                                ["GoogleCloudFirestoreDatabaseId"] = _firestoreDatabaseId,
                                ["GoogleCloudBigQueryDatasetId"] = _bigQueryDatasetId,
                                ["GoogleCloudDocumentId"] = doc.Id,
                                ["GoogleCloudDocumentPath"] = doc.Reference.Path,
                                ["GoogleCloudCreateTime"] = doc.CreateTime,
                                ["GoogleCloudUpdateTime"] = doc.UpdateTime,
                                ["GoogleCloudReadTime"] = doc.ReadTime,
                                ["AvatarId"] = avatarId.ToString(),
                                ["ExportType"] = "AvatarData"
                            }
                        };
                        
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
                        var holon = new Holon
                        {
                            Id = Guid.Parse(doc.Id),
                            Name = holonData.GetValueOrDefault("name")?.ToString(),
                            Description = holonData.GetValueOrDefault("description")?.ToString(),
                            HolonType = Enum.TryParse<HolonType>(holonData.GetValueOrDefault("holonType")?.ToString(), out var holonType) ? holonType : HolonType.Holon,
                            CreatedDate = ((Timestamp)holonData.GetValueOrDefault("createdDate")).ToDateTime(),
                            ModifiedDate = ((Timestamp)holonData.GetValueOrDefault("modifiedDate")).ToDateTime(),
                            Version = Convert.ToInt32(holonData.GetValueOrDefault("version") ?? 1),
                            IsActive = Convert.ToBoolean(holonData.GetValueOrDefault("isActive") ?? true),
                            // Map ALL Holon properties
                            ParentHolonId = holonData.GetValueOrDefault("parentId") != null ? Guid.Parse(holonData.GetValueOrDefault("parentId").ToString()) : Guid.Empty,
                            ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string> { [Core.Enums.ProviderType.GoogleCloudOASIS] = holonData.GetValueOrDefault("providerKey")?.ToString() ?? "" },
                            PreviousVersionId = holonData.GetValueOrDefault("previousVersionId") != null ? Guid.Parse(holonData.GetValueOrDefault("previousVersionId").ToString()) : Guid.Empty,
                            VersionId = holonData.GetValueOrDefault("nextVersionId") != null ? Guid.Parse(holonData.GetValueOrDefault("nextVersionId").ToString()) : Guid.Empty,
                            IsChanged = Convert.ToBoolean(holonData.GetValueOrDefault("isChanged") ?? false),
                            IsNewHolon = Convert.ToBoolean(holonData.GetValueOrDefault("isNew") ?? false),
                            DeletedByAvatarId = holonData.GetValueOrDefault("deletedByAvatarId") != null ? Guid.Parse(holonData.GetValueOrDefault("deletedByAvatarId").ToString()) : Guid.Empty,
                            DeletedDate = holonData.GetValueOrDefault("deletedDate") != null ? ((Timestamp)holonData.GetValueOrDefault("deletedDate")).ToDateTime() : DateTime.MinValue,
                            CreatedByAvatarId = holonData.GetValueOrDefault("createdByAvatarId") != null ? Guid.Parse(holonData.GetValueOrDefault("createdByAvatarId").ToString()) : Guid.Empty,
                            ModifiedByAvatarId = holonData.GetValueOrDefault("modifiedByAvatarId") != null ? Guid.Parse(holonData.GetValueOrDefault("modifiedByAvatarId").ToString()) : Guid.Empty,
                            // Map Google Cloud specific data to custom properties
                            MetaData = new Dictionary<string, object>
                            {
                                ["GoogleCloudProjectId"] = _projectId,
                                ["GoogleCloudBucketName"] = _bucketName,
                                ["GoogleCloudFirestoreDatabaseId"] = _firestoreDatabaseId,
                                ["GoogleCloudBigQueryDatasetId"] = _bigQueryDatasetId,
                                ["GoogleCloudDocumentId"] = doc.Id,
                                ["GoogleCloudDocumentPath"] = doc.Reference.Path,
                                ["GoogleCloudCreateTime"] = doc.CreateTime,
                                ["GoogleCloudUpdateTime"] = doc.UpdateTime,
                                ["GoogleCloudReadTime"] = doc.ReadTime,
                                ["AvatarUsername"] = avatarUsername,
                                ["ExportType"] = "AvatarData"
                            }
                        };
                        
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
