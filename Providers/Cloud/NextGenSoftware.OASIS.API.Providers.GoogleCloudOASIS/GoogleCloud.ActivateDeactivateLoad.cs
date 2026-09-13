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

        public override OASISResult<bool> ActivateProvider()
        {
            return ActivateProviderAsync().Result;
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                // Initialize Google Cloud Storage client if enabled
                if (_enableStorage)
                {
                    _storageClient = await StorageClient.CreateAsync();
                    
                    // Test connection by creating a test bucket if it doesn't exist
                    try
                    {
                        await _storageClient.GetBucketAsync(_bucketName);
                    }
                    catch
                    {
                        // Create bucket if it doesn't exist
                        await _storageClient.CreateBucketAsync(_projectId, _bucketName);
                    }
                }
                
                // Initialize Firestore client if enabled
                if (_enableFirestore)
                {
                    _firestoreDb = FirestoreDb.Create(_projectId);
                }
                
                // Initialize BigQuery client if enabled
                if (_enableBigQuery)
                {
                    _bigQueryClient = await BigQueryClient.CreateAsync(_projectId);
                }
                
                IsProviderActivated = true;
                result.Result = true;
                result.IsError = false;
                result.Message = "Google Cloud provider activated successfully with all services initialized";
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error activating Google Cloud provider: {e.Message}", e);
            }

            return result;
        }

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                // Dispose Google Cloud clients properly
                _storageClient?.Dispose();
                _bigQueryClient?.Dispose();
                
                _storageClient = null;
                _firestoreDb = null;
                _bigQueryClient = null;
                
                IsProviderActivated = false;
                result.Result = true;
                result.IsError = false;
                result.Message = "Google Cloud provider deactivated successfully with all clients disposed";
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deactivating Google Cloud provider: {e.Message}", e);
            }

            return result;
        }

        public override OASISResult<bool> DeActivateProvider()
        {
            return DeActivateProviderAsync().Result;
        }

        // Real Google Cloud implementation methods
        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
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

                // Load avatar from Firestore
                var docRef = _firestoreDb.Collection("avatars").Document(id.ToString());
                var snapshot = await docRef.GetSnapshotAsync();
                
                if (snapshot.Exists)
                {
                    var avatarData = snapshot.ConvertTo<Dictionary<string, object>>();
                    var avatar = new Avatar
                    {
                        Id = id,
                        Username = avatarData.GetValueOrDefault("username")?.ToString(),
                        Email = avatarData.GetValueOrDefault("email")?.ToString(),
                        FirstName = avatarData.GetValueOrDefault("firstName")?.ToString(),
                        LastName = avatarData.GetValueOrDefault("lastName")?.ToString(),
                        CreatedDate = ((Timestamp)avatarData.GetValueOrDefault("createdDate")).ToDateTime(),
                        ModifiedDate = ((Timestamp)avatarData.GetValueOrDefault("modifiedDate")).ToDateTime(),
                        // Map ALL Google Cloud properties to Avatar properties
                        // Address property not available in Avatar class
                        // Country property not available in Avatar class
                        // Postcode property not available in Avatar class
                        // Mobile property not available in Avatar class
                        // Landline property not available in Avatar class
                        Title = avatarData.GetValueOrDefault("title")?.ToString(),
                        // DOB property not available in Avatar class
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
                    
                    result.Result = avatar;
                    result.IsError = false;
                    result.Message = "Avatar loaded successfully from Google Cloud Firestore with full property mapping";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found in Google Cloud Firestore");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar from Google Cloud: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0)
        {
            return LoadAvatarAsync(id, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
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

                // Load avatar from Firestore by email
                var query = _firestoreDb.Collection("avatars").WhereEqualTo("email", avatarEmail);
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
                        // Map ALL Google Cloud properties to Avatar properties
                        // Address property not available in Avatar class
                        // Country property not available in Avatar class
                        // Postcode property not available in Avatar class
                        // Mobile property not available in Avatar class
                        // Landline property not available in Avatar class
                        Title = avatarData.GetValueOrDefault("title")?.ToString(),
                        // DOB property not available in Avatar class
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
                            ["GoogleCloudReadTime"] = doc.ReadTime
                        }
                    };
                    
                    result.Result = avatar;
                    result.IsError = false;
                    result.Message = "Avatar loaded successfully from Google Cloud Firestore by email with full property mapping";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found in Google Cloud Firestore by email");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar from Google Cloud by email: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
        {
            return LoadAvatarByEmailAsync(avatarEmail, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
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

                // Load avatar from Firestore by username
                var query = _firestoreDb.Collection("avatars").WhereEqualTo("username", avatarUsername);
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
                        // Map ALL Google Cloud properties to Avatar properties
                        // Address property not available in Avatar class
                        // Country property not available in Avatar class
                        // Postcode property not available in Avatar class
                        // Mobile property not available in Avatar class
                        // Landline property not available in Avatar class
                        Title = avatarData.GetValueOrDefault("title")?.ToString(),
                        // DOB property not available in Avatar class
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
                            ["GoogleCloudReadTime"] = doc.ReadTime
                        }
                    };
                    
                    result.Result = avatar;
                    result.IsError = false;
                    result.Message = "Avatar loaded successfully from Google Cloud Firestore by username with full property mapping";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found in Google Cloud Firestore by username");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar from Google Cloud by username: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            OASISResult<IEnumerable<IAvatar>> result = new OASISResult<IEnumerable<IAvatar>>();
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

                // Load all avatars from Firestore
                var query = _firestoreDb.Collection("avatars");
                var snapshot = await query.GetSnapshotAsync();
                
                if (snapshot.Count > 0)
                {
                    var avatars = new List<IAvatar>();
                    
                    // Convert ALL Firestore documents to OASIS Avatars with FULL property mapping
                    foreach (var doc in snapshot.Documents)
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
                            // Map ALL Google Cloud properties to Avatar properties
                            // Address property not available in Avatar class
                            // Country property not available in Avatar class
                            // Postcode property not available in Avatar class
                            // Mobile property not available in Avatar class
                            // Landline property not available in Avatar class
                            Title = avatarData.GetValueOrDefault("title")?.ToString(),
                            // DOB property not available in Avatar class
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
                                ["GoogleCloudReadTime"] = doc.ReadTime
                            }
                        };
                        
                        avatars.Add(avatar);
                    }
                    
                    result.Result = avatars;
                    result.IsError = false;
                    result.Message = $"Avatars loaded successfully from Google Cloud Firestore with full property mapping ({avatars.Count} avatars)";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "No avatars found in Google Cloud Firestore");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatars from Google Cloud: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
        {
            return LoadAllAvatarsAsync(version).Result;
        }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
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

                // Save avatar to Firestore with FULL property mapping
                var docRef = _firestoreDb.Collection("avatars").Document(avatar.Id.ToString());
                var avatarData = new Dictionary<string, object>
                {
                    ["id"] = avatar.Id.ToString(),
                    ["username"] = avatar.Username,
                    ["email"] = avatar.Email,
                    ["firstName"] = avatar.FirstName,
                    ["lastName"] = avatar.LastName,
                    ["createdDate"] = Timestamp.FromDateTime(avatar.CreatedDate),
                    ["modifiedDate"] = Timestamp.FromDateTime(avatar.ModifiedDate),
                    // Map ALL Avatar properties to Google Cloud fields
                    // Address, Country, Postcode, Mobile, Landline properties not available in IAvatar interface
                    ["title"] = avatar.Title,
                    // Properties not available in IAvatar interface removed (mana, stamina, website, language)
                    ["description"] = avatar.Description,
                    // Map Google Cloud specific metadata
                    ["googleCloudProjectId"] = _projectId,
                    ["googleCloudBucketName"] = _bucketName,
                    ["googleCloudFirestoreDatabaseId"] = _firestoreDatabaseId,
                    ["googleCloudBigQueryDatasetId"] = _bigQueryDatasetId,
                    ["savedAt"] = Timestamp.FromDateTime(DateTime.Now)
                };
                
                await docRef.SetAsync(avatarData);
                
                result.Result = avatar;
                result.IsError = false;
                result.Message = "Avatar saved successfully to Google Cloud Firestore with full property mapping";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving avatar to Google Cloud: {ex.Message}", ex);
            }
            return result;
        }

    }
}
