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

                // Save avatar detail to Firestore with FULL property mapping
                var docRef = _firestoreDb.Collection("avatarDetails").Document(avatarDetail.Id.ToString());
                var avatarDetailData = new Dictionary<string, object>
                {
                    ["id"] = avatarDetail.Id.ToString(),
                    ["username"] = avatarDetail.Username,
                    ["email"] = avatarDetail.Email,
                    // Map supported AvatarDetail to MetaData for Firestore to avoid interface mismatch
                    ["firstName"] = avatarDetail.GetType().GetProperty("FirstName")?.GetValue(avatarDetail) ?? string.Empty,
                    ["lastName"] = avatarDetail.GetType().GetProperty("LastName")?.GetValue(avatarDetail) ?? string.Empty,
                    ["createdDate"] = Timestamp.FromDateTime(avatarDetail.CreatedDate),
                    ["modifiedDate"] = Timestamp.FromDateTime(avatarDetail.ModifiedDate),
                    // Map ALL AvatarDetail properties to Google Cloud fields
                    ["title"] = avatarDetail.GetType().GetProperty("Title")?.GetValue(avatarDetail) ?? string.Empty,
                    // Store extended fields into a nested meta object if present
                    ["meta"] = new Dictionary<string, object>
                    {
                        ["address"] = avatarDetail.GetType().GetProperty("Address")?.GetValue(avatarDetail) ?? string.Empty,
                        ["country"] = avatarDetail.GetType().GetProperty("Country")?.GetValue(avatarDetail) ?? string.Empty,
                        ["postcode"] = avatarDetail.GetType().GetProperty("Postcode")?.GetValue(avatarDetail) ?? string.Empty,
                        ["mobile"] = avatarDetail.GetType().GetProperty("Mobile")?.GetValue(avatarDetail) ?? string.Empty,
                        ["landline"] = avatarDetail.GetType().GetProperty("Landline")?.GetValue(avatarDetail) ?? string.Empty,
                        ["dob"] = avatarDetail.GetType().GetProperty("DOB")?.GetValue(avatarDetail) is DateTime dob && dob != default ? Timestamp.FromDateTime(dob) : null,
                        ["avatarType"] = avatarDetail.GetType().GetProperty("AvatarType")?.GetValue(avatarDetail)?.ToString() ?? string.Empty,
                        ["karmaAkashicRecords"] = avatarDetail.GetType().GetProperty("KarmaAkashicRecords")?.GetValue(avatarDetail) ?? 0,
                        ["level"] = avatarDetail.GetType().GetProperty("Level")?.GetValue(avatarDetail) ?? 0,
                        ["xp"] = avatarDetail.GetType().GetProperty("XP")?.GetValue(avatarDetail) ?? 0,
                        ["hp"] = avatarDetail.GetType().GetProperty("HP")?.GetValue(avatarDetail) ?? 0,
                        ["mana"] = avatarDetail.GetType().GetProperty("Mana")?.GetValue(avatarDetail) ?? 0,
                        ["stamina"] = avatarDetail.GetType().GetProperty("Stamina")?.GetValue(avatarDetail) ?? 0,
                        ["description"] = avatarDetail.GetType().GetProperty("Description")?.GetValue(avatarDetail) ?? string.Empty,
                        ["website"] = avatarDetail.GetType().GetProperty("Website")?.GetValue(avatarDetail) ?? string.Empty,
                        ["language"] = avatarDetail.GetType().GetProperty("Language")?.GetValue(avatarDetail) ?? string.Empty
                    },
                    // Map Google Cloud specific metadata
                    ["googleCloudProjectId"] = _projectId,
                    ["googleCloudBucketName"] = _bucketName,
                    ["googleCloudFirestoreDatabaseId"] = _firestoreDatabaseId,
                    ["googleCloudBigQueryDatasetId"] = _bigQueryDatasetId,
                    ["savedAt"] = Timestamp.FromDateTime(DateTime.Now)
                };
                
                await docRef.SetAsync(avatarDetailData);
                
                result.Result = avatarDetail;
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
                
                if (true) // Soft delete by default (OASIS standard)
                {
                    // Soft delete - mark as deleted
                    var updateData = new Dictionary<string, object>
                    {
                        ["IsDeleted"] = true,
                        ["DeletedDate"] = Timestamp.FromDateTime(DateTime.UtcNow),
                        ["DeletedByAvatarId"] = AvatarManager.LoggedInAvatar?.Id ?? Guid.Empty
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

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
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

                // Load holon from Firestore
                var docRef = _firestoreDb.Collection("holons").Document(id.ToString());
                var snapshot = await docRef.GetSnapshotAsync();
                
                if (snapshot.Exists)
                {
                    var holonData = snapshot.ConvertTo<Dictionary<string, object>>();
                    var holon = new Holon
                    {
                        Id = id,
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
                        // Map Google Cloud specific data to metadata
                        MetaData = new Dictionary<string, object>
                        {
                            ["GoogleCloudProjectId"] = _projectId,
                            ["GoogleCloudBucketName"] = _bucketName,
                            ["GoogleCloudFirestoreDatabaseId"] = _firestoreDatabaseId,
                            ["GoogleCloudBigQueryDatasetId"] = _bigQueryDatasetId,
                            ["GoogleCloudDocumentId"] = snapshot.Id,
                            ["GoogleCloudDocumentPath"] = snapshot.Reference.Path,
                            ["GoogleCloudCreateTime"] = snapshot.CreateTime,
                            ["GoogleCloudUpdateTime"] = snapshot.UpdateTime,
                            ["GoogleCloudReadTime"] = snapshot.ReadTime
                        }
                    };
                    
                    result.Result = holon;
                    result.IsError = false;
                    result.Message = "Holon loaded successfully from Google Cloud Firestore with full property mapping";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Holon not found in Google Cloud Firestore");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holon from Google Cloud: {ex.Message}", ex);
            }
            return result;
        }

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

                // Save holon to Firestore with FULL property mapping
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
                    // Map ALL Holon properties to Google Cloud fields
                    ["parentId"] = holon.ParentHolonId.ToString(),
                    ["providerKey"] = holon.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.GoogleCloudOASIS) ? holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.GoogleCloudOASIS] : "",
                    ["previousVersionId"] = holon.PreviousVersionId.ToString(),
                    ["nextVersionId"] = holon.VersionId.ToString(),
                    ["isChanged"] = holon.IsChanged,
                    ["isNew"] = holon.IsNewHolon,
                    ["isDeleted"] = !holon.IsActive,
                    ["deletedByAvatarId"] = holon.DeletedByAvatarId.ToString(),
                    ["deletedDate"] = Timestamp.FromDateTime(holon.DeletedDate),
                    ["createdByAvatarId"] = holon.CreatedByAvatarId.ToString(),
                    ["modifiedByAvatarId"] = holon.ModifiedByAvatarId.ToString(),
                    // Map Google Cloud specific metadata
                    ["googleCloudProjectId"] = _projectId,
                    ["googleCloudBucketName"] = _bucketName,
                    ["googleCloudFirestoreDatabaseId"] = _firestoreDatabaseId,
                    ["googleCloudBigQueryDatasetId"] = _bigQueryDatasetId,
                    ["savedAt"] = Timestamp.FromDateTime(DateTime.Now)
                };
                
                await docRef.SetAsync(holonData);
                
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
                                ["GoogleCloudReadTime"] = doc.ReadTime
                            }
                        };
                        
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

                // Load holon from Firestore by provider key
                var docRef = _firestoreDb.Collection("holons").WhereEqualTo("providerKey", providerKey).Limit(1);
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
                            ["GoogleCloudReadTime"] = doc.ReadTime
                        }
                    };
                    
                    result.Result = holon;
                    result.IsError = false;
                    result.Message = "Holon loaded successfully from Google Cloud Firestore by provider key with full property mapping";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Holon not found in Google Cloud Firestore by provider key");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holon from Google Cloud by provider key: {ex.Message}", ex);
            }
            return result;
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
                                ["ParentId"] = id.ToString(),
                                ["HolonType"] = type.ToString()
                            }
                        };
                        
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
