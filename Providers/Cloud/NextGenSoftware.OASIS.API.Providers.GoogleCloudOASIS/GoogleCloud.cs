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

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Google Cloud provider is not activated");
                    return result;
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
                    OASISErrorHandling.HandleError(ref result, "Google Cloud provider is not activated");
                    return result;
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
                    OASISErrorHandling.HandleError(ref result, "Google Cloud provider is not activated");
                    return result;
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
                    OASISErrorHandling.HandleError(ref result, "Google Cloud provider is not activated");
                    return result;
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
                    ["address"] = avatar.Address,
                    ["country"] = avatar.Country,
                    ["postcode"] = avatar.Postcode,
                    ["mobile"] = avatar.Mobile,
                    ["landline"] = avatar.Landline,
                    ["title"] = avatar.Title,
                    ["dob"] = avatar.DOB.HasValue ? Timestamp.FromDateTime(avatar.DOB.Value) : null,
                    ["avatarType"] = avatar.AvatarType.ToString(),
                    ["karmaAkashicRecords"] = avatar.KarmaAkashicRecords,
                    ["level"] = avatar.Level,
                    ["xp"] = avatar.XP,
                    ["hp"] = avatar.HP,
                    ["mana"] = avatar.Mana,
                    ["stamina"] = avatar.Stamina,
                    ["description"] = avatar.Description,
                    ["website"] = avatar.Website,
                    ["language"] = avatar.Language,
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
                    OASISErrorHandling.HandleError(ref result, "Google Cloud provider is not activated");
                    return result;
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
                    ["firstName"] = avatarDetail.FirstName,
                    ["lastName"] = avatarDetail.LastName,
                    ["createdDate"] = Timestamp.FromDateTime(avatarDetail.CreatedDate),
                    ["modifiedDate"] = Timestamp.FromDateTime(avatarDetail.ModifiedDate),
                    // Map ALL AvatarDetail properties to Google Cloud fields
                    ["address"] = avatarDetail.Address,
                    ["country"] = avatarDetail.Country,
                    ["postcode"] = avatarDetail.Postcode,
                    ["mobile"] = avatarDetail.Mobile,
                    ["landline"] = avatarDetail.Landline,
                    ["title"] = avatarDetail.Title,
                    ["dob"] = avatarDetail.DOB.HasValue ? Timestamp.FromDateTime(avatarDetail.DOB.Value) : null,
                    ["avatarType"] = avatarDetail.AvatarType.ToString(),
                    ["karmaAkashicRecords"] = avatarDetail.KarmaAkashicRecords,
                    ["level"] = avatarDetail.Level,
                    ["xp"] = avatarDetail.XP,
                    ["hp"] = avatarDetail.HP,
                    ["mana"] = avatarDetail.Mana,
                    ["stamina"] = avatarDetail.Stamina,
                    ["description"] = avatarDetail.Description,
                    ["website"] = avatarDetail.Website,
                    ["language"] = avatarDetail.Language,
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
                    OASISErrorHandling.HandleError(ref result, "Google Cloud provider is not activated");
                    return result;
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
                        ["isDeleted"] = true,
                        ["deletedDate"] = Timestamp.FromDateTime(DateTime.Now),
                        ["deletedByAvatarId"] = AvatarManager.LoggedInAvatar?.Id.ToString()
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
                    OASISErrorHandling.HandleError(ref result, "Google Cloud provider is not activated");
                    return result;
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
                        ParentId = holonData.GetValueOrDefault("parentId") != null ? Guid.Parse(holonData.GetValueOrDefault("parentId").ToString()) : (Guid?)null,
                        ProviderKey = holonData.GetValueOrDefault("providerKey")?.ToString(),
                        PreviousVersionId = holonData.GetValueOrDefault("previousVersionId") != null ? Guid.Parse(holonData.GetValueOrDefault("previousVersionId").ToString()) : (Guid?)null,
                        NextVersionId = holonData.GetValueOrDefault("nextVersionId") != null ? Guid.Parse(holonData.GetValueOrDefault("nextVersionId").ToString()) : (Guid?)null,
                        IsChanged = Convert.ToBoolean(holonData.GetValueOrDefault("isChanged") ?? false),
                        IsNew = Convert.ToBoolean(holonData.GetValueOrDefault("isNew") ?? false),
                        IsDeleted = Convert.ToBoolean(holonData.GetValueOrDefault("isDeleted") ?? false),
                        DeletedByAvatarId = holonData.GetValueOrDefault("deletedByAvatarId") != null ? Guid.Parse(holonData.GetValueOrDefault("deletedByAvatarId").ToString()) : (Guid?)null,
                        DeletedDate = holonData.GetValueOrDefault("deletedDate") != null ? ((Timestamp)holonData.GetValueOrDefault("deletedDate")).ToDateTime() : (DateTime?)null,
                        CreatedByAvatarId = holonData.GetValueOrDefault("createdByAvatarId") != null ? Guid.Parse(holonData.GetValueOrDefault("createdByAvatarId").ToString()) : (Guid?)null,
                        ModifiedByAvatarId = holonData.GetValueOrDefault("modifiedByAvatarId") != null ? Guid.Parse(holonData.GetValueOrDefault("modifiedByAvatarId").ToString()) : (Guid?)null,
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

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Google Cloud provider is not activated");
                    return result;
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
                    ["parentId"] = holon.ParentId?.ToString(),
                    ["providerKey"] = holon.ProviderKey,
                    ["previousVersionId"] = holon.PreviousVersionId?.ToString(),
                    ["nextVersionId"] = holon.NextVersionId?.ToString(),
                    ["isChanged"] = holon.IsChanged,
                    ["isNew"] = holon.IsNew,
                    ["isDeleted"] = holon.IsDeleted,
                    ["deletedByAvatarId"] = holon.DeletedByAvatarId?.ToString(),
                    ["deletedDate"] = holon.DeletedDate.HasValue ? Timestamp.FromDateTime(holon.DeletedDate.Value) : null,
                    ["createdByAvatarId"] = holon.CreatedByAvatarId?.ToString(),
                    ["modifiedByAvatarId"] = holon.ModifiedByAvatarId?.ToString(),
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

        public override OASISResult<IHolon> SaveHolon(IHolon holon)
        {
            return SaveHolonAsync(holon).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(int version = 0)
        {
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Google Cloud provider is not activated");
                    return result;
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
                            ParentId = holonData.GetValueOrDefault("parentId") != null ? Guid.Parse(holonData.GetValueOrDefault("parentId").ToString()) : (Guid?)null,
                            ProviderKey = holonData.GetValueOrDefault("providerKey")?.ToString(),
                            PreviousVersionId = holonData.GetValueOrDefault("previousVersionId") != null ? Guid.Parse(holonData.GetValueOrDefault("previousVersionId").ToString()) : (Guid?)null,
                            NextVersionId = holonData.GetValueOrDefault("nextVersionId") != null ? Guid.Parse(holonData.GetValueOrDefault("nextVersionId").ToString()) : (Guid?)null,
                            IsChanged = Convert.ToBoolean(holonData.GetValueOrDefault("isChanged") ?? false),
                            IsNew = Convert.ToBoolean(holonData.GetValueOrDefault("isNew") ?? false),
                            IsDeleted = Convert.ToBoolean(holonData.GetValueOrDefault("isDeleted") ?? false),
                            DeletedByAvatarId = holonData.GetValueOrDefault("deletedByAvatarId") != null ? Guid.Parse(holonData.GetValueOrDefault("deletedByAvatarId").ToString()) : (Guid?)null,
                            DeletedDate = holonData.GetValueOrDefault("deletedDate") != null ? ((Timestamp)holonData.GetValueOrDefault("deletedDate")).ToDateTime() : (DateTime?)null,
                            CreatedByAvatarId = holonData.GetValueOrDefault("createdByAvatarId") != null ? Guid.Parse(holonData.GetValueOrDefault("createdByAvatarId").ToString()) : (Guid?)null,
                            ModifiedByAvatarId = holonData.GetValueOrDefault("modifiedByAvatarId") != null ? Guid.Parse(holonData.GetValueOrDefault("modifiedByAvatarId").ToString()) : (Guid?)null,
                            // Map Google Cloud specific data to custom properties
                            CustomData = new Dictionary<string, object>
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

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(int version = 0)
        {
            return LoadAllHolonsAsync(version).Result;
        }
    }
}
