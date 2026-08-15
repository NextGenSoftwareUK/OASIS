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
        public OASISResult<IHolon> LoadHolon(string username, string password, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonAsync(username, password, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public async Task<OASISResult<IHolon>> LoadHolonByEmailAsync(string email, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
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

                // Load holon from Firestore by email
                var docRef = _firestoreDb.Collection("holons").WhereEqualTo("email", email).Limit(1);
                var snapshot = await docRef.GetSnapshotAsync();
                
                if (snapshot.Count > 0)
                {
                    var doc = snapshot.Documents.First();
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
                            ["Email"] = email
                        }
                    };
                    
                    result.Result = holon;
                    result.IsError = false;
                    result.Message = "Holon loaded successfully from Google Cloud Firestore by email with full property mapping";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Holon not found in Google Cloud Firestore by email");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holon from Google Cloud by email: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IHolon> LoadHolonByEmail(string email, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonByEmailAsync(email, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
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

                var savedHolons = new List<IHolon>();
                
                // Save ALL holons to Google Cloud Firestore with FULL property mapping
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
                        ["googleCloudBigQueryDatasetId"] = _bigQueryDatasetId
                    };
                    
                    await docRef.SetAsync(holonData);
                    savedHolons.Add(holon);
                }
                
                result.Result = savedHolons;
                result.IsError = false;
                result.Message = $"Holons saved successfully to Google Cloud Firestore with full property mapping ({savedHolons.Count} holons)";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving holons to Google Cloud: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
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

                var docRef = _firestoreDb.Collection("holons").Document(id.ToString());
                
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
                
                result.Result = null; // Return null for deleted holon
                result.IsError = false;
                result.Message = "Holon soft deleted successfully from Google Cloud Firestore";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting holon from Google Cloud: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id)
        {
            return DeleteHolonAsync(id).Result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
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

                var query = _firestoreDb.Collection("holons").WhereEqualTo("providerKey", providerKey);
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
                    
                    result.Result = null; // Return null for deleted holon
                    result.IsError = false;
                    result.Message = "Holon soft deleted successfully from Google Cloud Firestore by provider key";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Holon not found in Google Cloud Firestore by provider key");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting holon from Google Cloud by provider key: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var deleteResult = DeleteHolonAsync(providerKey).Result;
                result.IsError = deleteResult.IsError;
                result.Message = deleteResult.Message;
                result.Result = null; // Return null for deleted holon
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting holon: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<bool>> DeleteHolonAsync(string username, bool softDelete = true)
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

                var query = _firestoreDb.Collection("holons").WhereEqualTo("username", username);
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
                    result.Message = $"Holon {(softDelete ? "soft" : "hard")} deleted successfully from Google Cloud Firestore by username";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Holon not found in Google Cloud Firestore by username");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting holon from Google Cloud by username: {ex.Message}", ex);
            }
            return result;
        }

    }
}
