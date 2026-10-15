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
using Google.Api.Gax;
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
                    var firestoreBuilder = new FirestoreDbBuilder
                    {
                        ProjectId = _projectId,
                        DatabaseId = _firestoreDatabaseId,
                        EmulatorDetection = EmulatorDetection.EmulatorOrProduction
                    };
                    if (!string.IsNullOrWhiteSpace(_credentialsPath))
                        firestoreBuilder.CredentialsPath = _credentialsPath;
                    _firestoreDb = firestoreBuilder.Build();

                    // Client construction does not contact Firestore. Perform a real,
                    // reversible write so an unreachable or unauthorized provider can
                    // never be advertised to HyperDrive as active.
                    var activationProbe = _firestoreDb.Collection("_oasis_health").Document(Guid.NewGuid().ToString("N"));
                    await activationProbe.SetAsync(new Dictionary<string, object>
                    {
                        ["provider"] = "GoogleCloudOASIS",
                        ["checkedAt"] = Timestamp.GetCurrentTimestamp()
                    });
                    await activationProbe.DeleteAsync();
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
        public override Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
            => ReadCanonicalAvatarAsync(id,version);

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0)
        {
            return LoadAvatarAsync(id, version).Result;
        }

        public override Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
            => ReadCanonicalAvatarSelectorAsync(avatarEmail,true,version);

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
        {
            return LoadAvatarByEmailAsync(avatarEmail, version).Result;
        }

        public override Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
            => ReadCanonicalAvatarSelectorAsync(avatarUsername,false,version);

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
            => ReadCanonicalAvatarsAsync(version);

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
        {
            return LoadAllAvatarsAsync(version).Result;
        }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            try
            {
                ArgumentNullException.ThrowIfNull(avatar);
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<bool,IAvatar>(activateResult);
                    }
                }

                if (!_enableFirestore)
                {
                    OASISErrorHandling.HandleError(ref result, "Firestore is not enabled");
                    return result;
                }

                if (avatar.Id == Guid.Empty)
                    avatar.Id = Guid.NewGuid();
                avatar.ProviderUniqueStorageKey ??= new Dictionary<Core.Enums.ProviderType, string>();
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.GoogleCloudOASIS] = avatar.Id.ToString();

                // Save avatar to Firestore with FULL property mapping
                var docRef = _firestoreDb.Collection("avatars").Document(avatar.Id.ToString());
                var avatarData = new Dictionary<string, object>
                {
                    ["id"] = avatar.Id.ToString(),
                    ["username"] = avatar.Username,
                    ["email"] = avatar.Email,
                    ["isActive"] = avatar.IsActive,
                    ["data_json"] = NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.OasisJson.Serialize(avatar)
                };
                
                await ObserveFirestoreMutationAsync(()=>docRef.SetAsync(avatarData),docRef.Path);
                
                result.Result = avatar;
                result.IsSaved = true;
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
