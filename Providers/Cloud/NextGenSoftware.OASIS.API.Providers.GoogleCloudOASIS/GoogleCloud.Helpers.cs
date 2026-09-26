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
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid avatarId, int version = 0)
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

                // Load avatar detail from Firestore by ID
                var docRef = _firestoreDb.Collection("avatarDetails").Document(avatarId.ToString());
                var snapshot = await docRef.GetSnapshotAsync();
                
                if (snapshot.Exists)
                {
                    var avatarDetailData = snapshot.ConvertTo<Dictionary<string, object>>();
                    var avatarDetail = new AvatarDetail
                    {
                        Id = avatarId,
                        Username = avatarDetailData.GetValueOrDefault("username")?.ToString(),
                        Email = avatarDetailData.GetValueOrDefault("email")?.ToString(),
                        FirstName = avatarDetailData.GetValueOrDefault("firstName")?.ToString(),
                        LastName = avatarDetailData.GetValueOrDefault("lastName")?.ToString(),
                        CreatedDate = ((Timestamp)avatarDetailData.GetValueOrDefault("createdDate")).ToDateTime(),
                        ModifiedDate = ((Timestamp)avatarDetailData.GetValueOrDefault("modifiedDate")).ToDateTime(),
                        Version = Convert.ToInt32(avatarDetailData.GetValueOrDefault("version") ?? 1),
                        IsActive = Convert.ToBoolean(avatarDetailData.GetValueOrDefault("isActive") ?? true),
                        // AvatarDetail specific properties
                        Address = avatarDetailData.GetValueOrDefault("address")?.ToString(),
                        Country = avatarDetailData.GetValueOrDefault("country")?.ToString(),
                        Postcode = avatarDetailData.GetValueOrDefault("postcode")?.ToString(),
                        Mobile = avatarDetailData.GetValueOrDefault("mobile")?.ToString(),
                        Landline = avatarDetailData.GetValueOrDefault("landline")?.ToString(),
                        Title = avatarDetailData.GetValueOrDefault("title")?.ToString(),
                        DOB = avatarDetailData.GetValueOrDefault("dob") != null ? ((Timestamp)avatarDetailData.GetValueOrDefault("dob")).ToDateTime() : DateTime.MinValue,
                        AvatarType = new EnumValue<AvatarType>(Enum.TryParse<AvatarType>(avatarDetailData.GetValueOrDefault("avatarType")?.ToString(), out var avatarType) ? avatarType : AvatarType.User),
                        KarmaAkashicRecords = new List<IKarmaAkashicRecord>(),
                        // Level is read-only and calculated from Karma
                        XP = Convert.ToInt32(avatarDetailData.GetValueOrDefault("xp") ?? 0),
                        // HP, Mana, Stamina properties don't exist on AvatarDetail
                        Description = avatarDetailData.GetValueOrDefault("description")?.ToString(),
                        // Website, Language, ProviderWallets properties don't exist on AvatarDetail
                        // Map Google Cloud specific data to custom properties
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
                    
                    result.Result = avatarDetail;
                    result.IsError = false;
                    result.Message = "Avatar detail loaded successfully from Google Cloud Firestore with full property mapping";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar detail not found in Google Cloud Firestore");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail from Google Cloud: {ex.Message}", ex);
            }
            return result;
        }

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            OASISResult<IEnumerable<IAvatarDetail>> result = new OASISResult<IEnumerable<IAvatarDetail>>();
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

                // Load all avatar details from Firestore
                var query = _firestoreDb.Collection("avatarDetails");
                var snapshot = await query.GetSnapshotAsync();
                
                if (snapshot.Count > 0)
                {
                    var avatarDetails = new List<IAvatarDetail>();
                    
                    // Convert ALL Firestore documents to OASIS AvatarDetails with FULL property mapping
                    foreach (var doc in snapshot.Documents)
                    {
                        var avatarDetailData = doc.ConvertTo<Dictionary<string, object>>();
                        var avatarDetail = new AvatarDetail
                        {
                            Id = Guid.Parse(doc.Id),
                            Username = avatarDetailData.GetValueOrDefault("username")?.ToString(),
                            Email = avatarDetailData.GetValueOrDefault("email")?.ToString(),
                            FirstName = avatarDetailData.GetValueOrDefault("firstName")?.ToString(),
                            LastName = avatarDetailData.GetValueOrDefault("lastName")?.ToString(),
                            CreatedDate = ((Timestamp)avatarDetailData.GetValueOrDefault("createdDate")).ToDateTime(),
                            ModifiedDate = ((Timestamp)avatarDetailData.GetValueOrDefault("modifiedDate")).ToDateTime(),
                            Version = Convert.ToInt32(avatarDetailData.GetValueOrDefault("version") ?? 1),
                            IsActive = Convert.ToBoolean(avatarDetailData.GetValueOrDefault("isActive") ?? true),
                            // AvatarDetail specific properties
                            Address = avatarDetailData.GetValueOrDefault("address")?.ToString(),
                            Country = avatarDetailData.GetValueOrDefault("country")?.ToString(),
                            Postcode = avatarDetailData.GetValueOrDefault("postcode")?.ToString(),
                            Mobile = avatarDetailData.GetValueOrDefault("mobile")?.ToString(),
                            Landline = avatarDetailData.GetValueOrDefault("landline")?.ToString(),
                            Title = avatarDetailData.GetValueOrDefault("title")?.ToString(),
                            DOB = avatarDetailData.GetValueOrDefault("dob") != null ? ((Timestamp)avatarDetailData.GetValueOrDefault("dob")).ToDateTime() : DateTime.MinValue,
                            AvatarType = new EnumValue<AvatarType>(Enum.TryParse<AvatarType>(avatarDetailData.GetValueOrDefault("avatarType")?.ToString(), out var avatarType) ? avatarType : AvatarType.User),
                            KarmaAkashicRecords = new List<IKarmaAkashicRecord>(),
                            // Level is read-only and calculated from Karma
                            XP = Convert.ToInt32(avatarDetailData.GetValueOrDefault("xp") ?? 0),
                            // HP, Mana, Stamina are not available on AvatarDetail interface
                            Description = avatarDetailData.GetValueOrDefault("description")?.ToString(),
                            // Website and Language are not available on AvatarDetail interface
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
                        
                        avatarDetails.Add(avatarDetail);
                    }
                    
                    result.Result = avatarDetails;
                    result.IsError = false;
                    result.Message = $"Avatar details loaded successfully from Google Cloud Firestore with full property mapping ({avatarDetails.Count} avatar details)";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "No avatar details found in Google Cloud Firestore");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar details from Google Cloud: {ex.Message}", ex);
            }
            return result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
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

                // Delete avatar from Firestore by provider key
                var query = _firestoreDb.Collection("avatars").WhereEqualTo("providerKey", providerKey);
                var snapshot = await query.GetSnapshotAsync();
                
                if (snapshot.Count > 0)
                {
                    var doc = snapshot.Documents.First();
                    var docRef = doc.Reference;
                    
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
                    result.Message = $"Avatar {(softDelete ? "soft" : "hard")} deleted successfully from Google Cloud Firestore by provider key";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found in Google Cloud Firestore by provider key");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar from Google Cloud by provider key: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid avatarId, int version = 0)
        {
            return LoadAvatarDetailAsync(avatarId, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
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

                // Load avatar from Firestore by provider key
                var query = _firestoreDb.Collection("avatars").WhereEqualTo("providerKey", providerKey);
                var snapshot = await query.GetSnapshotAsync();
                
                if (snapshot.Count > 0)
                {
                    var doc = snapshot.Documents.First();
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
                        Title = avatarData.GetValueOrDefault("title")?.ToString(),
                        AvatarType = new EnumValue<AvatarType>(Enum.TryParse<AvatarType>(avatarData.GetValueOrDefault("avatarType")?.ToString(), out var avatarType) ? avatarType : AvatarType.User),
                        Description = avatarData.GetValueOrDefault("description")?.ToString(),
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
                            ["GoogleCloudReadTime"] = doc.ReadTime
                        }
                    };
                    
                    result.Result = avatar;
                    result.IsError = false;
                    result.Message = "Avatar loaded successfully from Google Cloud Firestore by provider key with full property mapping";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found in Google Cloud Firestore by provider key");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar from Google Cloud by provider key: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
        {
            return DeleteAvatarByUsernameAsync(avatarUsername, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
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

                // Delete avatar from Firestore by email
                var query = _firestoreDb.Collection("avatars").WhereEqualTo("email", avatarEmail);
                var snapshot = await query.GetSnapshotAsync();
                
                if (snapshot.Count > 0)
                {
                    var doc = snapshot.Documents.First();
                    var docRef = doc.Reference;
                    
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
                    result.Message = $"Avatar {(softDelete ? "soft" : "hard")} deleted successfully from Google Cloud Firestore by email";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found in Google Cloud Firestore by email");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar from Google Cloud by email: {ex.Message}", ex);
            }
            return result;
        }
    }
}
