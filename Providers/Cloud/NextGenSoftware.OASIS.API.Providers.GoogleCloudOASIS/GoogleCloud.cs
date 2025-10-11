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
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS
{
    public class GoogleCloudOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider
    {
        private StorageClient _storageClient;
        private FirestoreDb _firestoreDb;
        private BigQueryClient _bigQueryClient;
        private readonly string _projectId;
        private readonly string _bucketName;
        private readonly string _credentialsPath;
        private readonly string _firestoreDatabaseId;
        private readonly string _bigQueryDatasetId;
        private readonly bool _enableStorage;
        private readonly bool _enableFirestore;
        private readonly bool _enableBigQuery;

        public GoogleCloudOASIS(string projectId = null, string bucketName = null, string credentialsPath = null, 
                               string firestoreDatabaseId = null, string bigQueryDatasetId = null,
                               bool enableStorage = true, bool enableFirestore = true, bool enableBigQuery = true)
        {
            this.ProviderName = "GoogleCloudOASIS";
            this.ProviderDescription = "GoogleCloudOASIS Provider";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.GoogleCloudOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
            
            _projectId = projectId ?? Environment.GetEnvironmentVariable("GOOGLE_CLOUD_PROJECT") ?? "oasis-project";
            _bucketName = bucketName ?? Environment.GetEnvironmentVariable("GOOGLE_CLOUD_BUCKET") ?? "oasis-storage";
            _credentialsPath = credentialsPath;
            _firestoreDatabaseId = firestoreDatabaseId ?? "(default)";
            _bigQueryDatasetId = bigQueryDatasetId ?? "oasis_data";
            _enableStorage = enableStorage;
            _enableFirestore = enableFirestore;
            _enableBigQuery = enableBigQuery;
        }

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
                    _firestoreDb = FirestoreDb.Create(_projectId, _firestoreDatabaseId);
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
                _firestoreDb?.Dispose();
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
                    OASISErrorHandling.HandleError(ref result, "Google Cloud provider is not activated");
                    return result;
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
                        Address = avatarData.GetValueOrDefault("address")?.ToString(),
                        Country = avatarData.GetValueOrDefault("country")?.ToString(),
                        Postcode = avatarData.GetValueOrDefault("postcode")?.ToString(),
                        Mobile = avatarData.GetValueOrDefault("mobile")?.ToString(),
                        Landline = avatarData.GetValueOrDefault("landline")?.ToString(),
                        Title = avatarData.GetValueOrDefault("title")?.ToString(),
                        DOB = avatarData.GetValueOrDefault("dob") != null ? 
                              ((Timestamp)avatarData.GetValueOrDefault("dob")).ToDateTime() : (DateTime?)null,
                        AvatarType = Enum.TryParse<AvatarType>(avatarData.GetValueOrDefault("avatarType")?.ToString(), out var avatarType) ? avatarType : AvatarType.Human,
                        KarmaAkashicRecords = Convert.ToInt32(avatarData.GetValueOrDefault("karmaAkashicRecords") ?? 0),
                        Level = Convert.ToInt32(avatarData.GetValueOrDefault("level") ?? 1),
                        XP = Convert.ToInt32(avatarData.GetValueOrDefault("xp") ?? 0),
                        HP = Convert.ToInt32(avatarData.GetValueOrDefault("hp") ?? 100),
                        Mana = Convert.ToInt32(avatarData.GetValueOrDefault("mana") ?? 0),
                        Stamina = Convert.ToInt32(avatarData.GetValueOrDefault("stamina") ?? 0),
                        Description = avatarData.GetValueOrDefault("description")?.ToString(),
                        Website = avatarData.GetValueOrDefault("website")?.ToString(),
                        Language = avatarData.GetValueOrDefault("language")?.ToString(),
                        ProviderWallets = new List<IProviderWallet>(),
                        // Map Google Cloud specific data to custom properties
                        CustomData = new Dictionary<string, object>
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
    }
}
