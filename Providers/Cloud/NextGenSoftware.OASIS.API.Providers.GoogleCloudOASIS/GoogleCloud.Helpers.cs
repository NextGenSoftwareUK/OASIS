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
        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid avatarId,int version=0) => ReadCanonicalDetailByIdAsync(avatarId,version);

        public override Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version=0) => ReadCanonicalDetailsAsync(version);

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
